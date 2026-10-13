<?php

namespace App\Modules\Leave\Services;

use App\Models\AuditLog;
use App\Models\Notification;
use App\Models\User;
use App\Modules\Attendance\Models\AttendanceDay;
use App\Modules\Employee\Models\Employee;
use App\Modules\Leave\Models\AssetHandoverItem;
use App\Modules\Leave\Models\FinalSettlement;
use App\Modules\Leave\Models\LeaveBalance;
use App\Modules\Leave\Models\ResignationRequest;
use App\Modules\Payroll\Models\Contract;
use Brick\Math\BigDecimal;
use Brick\Math\RoundingMode;
use Carbon\Carbon;
use DomainException;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\DB;

class OffboardingService
{
    protected LeaveService $leaveService;

    public function __construct(LeaveService $leaveService)
    {
        $this->leaveService = $leaveService;
    }

    /**
     * Tự động tính số ngày báo trước tối thiểu theo loại Hợp đồng lao động (Điều 35 BLLĐ 2019).
     * - HĐLĐ không xác định thời hạn (INDEFINITE_TERM): Ít nhất 45 ngày.
     * - HĐLĐ xác định thời hạn 12 - 36 tháng (DEFINITE_TERM_1Y, 2Y, 3Y): Ít nhất 30 ngày.
     * - HĐLĐ dưới 12 tháng, thời vụ hoặc Thử việc (PROBATION, SEASONAL): Ít nhất 03 ngày làm việc.
     */
    public function calculateStatutoryNoticeDays(Employee $employee): int
    {
        $contract = Contract::where('employee_id', $employee->id)
            ->where('is_current', true)
            ->first();

        if (!$contract) {
            return 30; // Mặc định 30 ngày nếu chưa đồng bộ hợp đồng
        }

        return match ($contract->contract_type) {
            'INDEFINITE_TERM'                                           => 45,
            'DEFINITE_TERM_1Y', 'DEFINITE_TERM_2Y', 'DEFINITE_TERM_3Y' => 30,
            'PROBATION', 'SEASONAL'                                     => 3,
            default                                                     => 30,
        };
    }

    /**
     * Nhân viên nộp đơn xin thôi việc trên cổng ESS.
     * - Tự tính ngày báo trước luật định.
     * - Tự động tạo danh mục bàn giao tài sản chuẩn Vinamilk.
     * - Gửi thông báo đến Quản lý trực tiếp trên cây tổ chức.
     */
    public function submitResignationRequest(int $employeeId, array $data): ResignationRequest
    {
        return DB::transaction(function () use ($employeeId, $data) {
            $employee = Employee::with(['currentAssignment.orgUnit'])->findOrFail($employeeId);

            // Kiểm tra nhân viên có đơn xin nghỉ việc đang xử lý không
            $existingActive = ResignationRequest::where('employee_id', $employeeId)
                ->whereIn('status', ['SUBMITTED', 'MANAGER_APPROVED', 'HR_APPROVED'])
                ->first();
            if ($existingActive) {
                throw new DomainException("Bạn đang có đơn xin nghỉ việc đang trong quá trình xét duyệt ({$existingActive->resignation_code}).");
            }

            $activeContract = Contract::where('employee_id', $employeeId)
                ->where('is_current', true)
                ->first();

            $submittedDate = isset($data['submitted_date']) ? Carbon::parse($data['submitted_date']) : now()->startOfDay();
            $proposedLastDate = Carbon::parse($data['proposed_last_date'])->startOfDay();

            // Tính số ngày báo trước pháp định
            $statutoryNoticeDays = $this->calculateStatutoryNoticeDays($employee);
            $actualNoticeDays = $submittedDate->diffInDays($proposedLastDate, false);

            $isMutualAgreement = !empty($data['is_mutual_agreement']);

            if ($actualNoticeDays < $statutoryNoticeDays && !$isMutualAgreement) {
                throw new DomainException(
                    "Thời hạn báo trước của bạn là {$actualNoticeDays} ngày, không đủ quy định pháp luật (Yêu cầu ít nhất {$statutoryNoticeDays} ngày theo Điều 35 BLLĐ 2019). Vui lòng chọn ngày phù hợp hoặc kích hoạt Thỏa thuận rút ngắn thời gian báo trước."
                );
            }

            $resignationCode = 'RES-' . date('Ym') . '-' . str_pad((string)rand(1, 99999), 5, '0', STR_PAD_LEFT);

            $request = ResignationRequest::create([
                'resignation_code'     => $resignationCode,
                'employee_id'          => $employeeId,
                'contract_id'          => $activeContract?->id,
                'submitted_date'       => $submittedDate->toDateString(),
                'required_notice_days' => $statutoryNoticeDays,
                'proposed_last_date'   => $proposedLastDate->toDateString(),
                'approved_last_date'   => $proposedLastDate->toDateString(), // Tạm gán theo ngày đề xuất
                'reason'               => $data['reason'] ?? 'Lý do cá nhân',
                'status'               => 'SUBMITTED',
                'notes'                => $isMutualAgreement ? 'Thỏa thuận rút ngắn thời gian báo trước' : null,
            ]);

            // Tự động khởi tạo Checklist bàn giao tài sản chuẩn
            $this->initializeAssetHandoverChecklist($request, $employee);

            // Gửi thông báo đến Quản lý trực tiếp trên cây tổ chức
            $manager = $this->leaveService->findDirectManager($employee);
            if ($manager && $manager->user) {
                Notification::create([
                    'user_id'  => $manager->user->id,
                    'title'    => "Đơn xin nghỉ việc từ nhân viên {$employee->full_name}",
                    'content'  => "Nhân viên {$employee->full_name} ({$employee->employee_code}) đã nộp đơn xin thôi việc. Ngày làm việc cuối đề xuất: {$proposedLastDate->format('d/m/Y')}.",
                    'type'     => 'APPROVAL_REQUEST',
                    'link_url' => "/offboarding/requests/{$request->id}",
                ]);
            }

            return $request;
        });
    }

    /**
     * Khởi tạo Checklist bàn giao tài sản dựa theo bộ phận và chức danh của nhân viên.
     */
    protected function initializeAssetHandoverChecklist(ResignationRequest $request, Employee $employee): void
    {
        $defaultItems = [
            // 1. CNTT (IT)
            [
                'item_name'           => 'Máy tính xách tay / PC kèm sạc và chuột',
                'item_category'       => 'IT_LAPTOP',
                'assigned_department' => 'IT',
            ],
            [
                'item_name'           => 'Thu hồi quyền truy cập tài khoản Email, SAP ERP, VPN',
                'item_category'       => 'IT_ACCOUNT',
                'assigned_department' => 'IT',
            ],
            // 2. Hành chính (Admin)
            [
                'item_name'           => 'Thẻ nhân viên & Thẻ từ ra vào cổng/thang máy',
                'item_category'       => 'ACCESS_CARD',
                'assigned_department' => 'ADMIN',
            ],
            [
                'item_name'           => 'Chìa khóa tủ cá nhân, bàn làm việc, phòng ban',
                'item_category'       => 'OFFICE_KEYS',
                'assigned_department' => 'ADMIN',
            ],
            // 3. Vận hành / Sản xuất / Trang trại
            [
                'item_name'           => 'Đồng phục bảo hộ lao động (BHLĐ), thẻ an toàn',
                'item_category'       => 'PPE_UNIFORM',
                'assigned_department' => 'WORKSHOP',
            ],
            [
                'item_name'           => 'Công cụ, dụng cụ kỹ thuật, thiết bị đo kiểm đã mượn',
                'item_category'       => 'TOOLS_EQUIPMENT',
                'assigned_department' => 'WORKSHOP',
            ],
            // 4. Kế toán & Tài chính
            [
                'item_name'           => 'Quyết toán hoàn ứng tạm ứng công tác phí, công nợ cá nhân',
                'item_category'       => 'FINANCE_SETTLEMENT',
                'assigned_department' => 'FINANCE',
            ],
        ];

        foreach ($defaultItems as $item) {
            AssetHandoverItem::create([
                'resignation_request_id' => $request->id,
                'item_name'              => $item['item_name'],
                'item_category'          => $item['item_category'],
                'assigned_department'    => $item['assigned_department'],
                'is_returned'            => false,
            ]);
        }
    }

    /**
     * Cấp 1: Quản lý trực tiếp phê duyệt đơn thôi việc (xác nhận kế hoạch bàn giao công việc).
     */
    public function approveByManager(int $requestId, User $approverUser): ResignationRequest
    {
        return DB::transaction(function () use ($requestId, $approverUser) {
            $request = ResignationRequest::with('employee')->findOrFail($requestId);

            if ($request->status !== 'SUBMITTED') {
                throw new DomainException("Đơn không ở trạng thái chờ Quản lý phê duyệt.");
            }

            // Nguyên tắc SoD: Không được tự duyệt đơn của chính mình
            if ($approverUser->employee_id && $approverUser->employee_id === $request->employee_id) {
                throw new DomainException("Nguyên tắc SoD: Bạn không thể tự duyệt đơn thôi việc của chính mình.");
            }

            $request->update([
                'status'              => 'MANAGER_APPROVED',
                'manager_approved_by' => $approverUser->employee_id ?? 1,
                'manager_approved_at' => now(),
            ]);

            return $request;
        });
    }

    /**
     * Cấp 2: HR Local / Trưởng phòng Nhân sự duyệt (xác nhận pháp lý, rà soát hồ sơ BHXH).
     */
    public function approveByHr(int $requestId, User $hrUser): ResignationRequest
    {
        return DB::transaction(function () use ($requestId, $hrUser) {
            $request = ResignationRequest::with('employee')->findOrFail($requestId);

            if ($request->status !== 'MANAGER_APPROVED') {
                throw new DomainException("Đơn cần có sự xác nhận của Quản lý trực tiếp trước khi HR xem xét.");
            }

            $request->update([
                'status'         => 'HR_APPROVED',
                'hr_approved_by' => $hrUser->employee_id ?? 1,
                'hr_approved_at' => now(),
            ]);

            return $request;
        });
    }

    /**
     * Cấp 3: Ban Giám đốc phê duyệt Quyết định thôi việc và chốt ngày làm việc cuối cùng.
     */
    public function approveByBod(int $requestId, User $bodUser, ?Carbon $approvedLastDate = null): ResignationRequest
    {
        return DB::transaction(function () use ($requestId, $bodUser, $approvedLastDate) {
            $request = ResignationRequest::with('employee')->findOrFail($requestId);

            if ($request->status !== 'HR_APPROVED') {
                throw new DomainException("Đơn cần có sự thẩm định của Phòng Nhân sự trước khi trình Ban Giám đốc.");
            }

            $finalDate = $approvedLastDate ?? Carbon::parse($request->proposed_last_date);

            $request->update([
                'status'             => 'BOD_APPROVED',
                'approved_last_date' => $finalDate->toDateString(),
                'bod_approved_by'    => $bodUser->employee_id ?? 1,
                'bod_approved_at'    => now(),
            ]);

            return $request;
        });
    }

    /**
     * Xác nhận thu hồi từng hạng mục trong checklist bàn giao tài sản.
     */
    public function confirmHandoverItem(int $itemId, int $receiverEmployeeId, ?string $notes = null): AssetHandoverItem
    {
        return DB::transaction(function () use ($itemId, $receiverEmployeeId, $notes) {
            $item = AssetHandoverItem::with('resignation')->findOrFail($itemId);

            $item->update([
                'is_returned'          => true,
                'returned_at'          => now(),
                'receiver_employee_id' => $receiverEmployeeId,
                'notes'                => $notes,
            ]);

            // Kiểm tra xem tất cả các tài sản đã được bàn giao đầy đủ chưa
            $unreturnedCount = AssetHandoverItem::where('resignation_request_id', $item->resignation_request_id)
                ->where('is_returned', false)
                ->count();

            if ($unreturnedCount === 0) {
                $item->resignation->update([
                    'handover_completed_at' => now(),
                ]);
            }

            return $item;
        });
    }

    /**
     * TÍNH TOÁN KỲ LƯƠNG CUỐI (FINAL SETTLEMENT PAYROLL) BẰNG BRICK/MATH.
     * Căn cứ pháp lý:
     * 1. Lương ngày công thực tế đến ngày làm việc cuối.
     * 2. Tiền phép năm chưa nghỉ (Điều 113 Khoản 6 BLLĐ 2019).
     * 3. Trợ cấp thôi việc (Điều 46 BLLĐ 2019 & Điều 8 Nghị định 145/2020/NĐ-CP):
     *    - Cứ mỗi năm làm việc được tính bằng 1/2 tháng tiền lương bình quân 6 tháng liền kề.
     *    - Thời gian tính trợ cấp = Tổng thời gian làm việc thực tế - Thời gian đã đóng BHTN (từ 01/01/2009).
     * 4. Khấu trừ tạm ứng / các khoản nợ.
     * 5. Cảnh báo thời hạn thanh toán 14 ngày làm việc theo Điều 48 BLLĐ 2019.
     */
    public function calculateFinalSettlement(int $resignationRequestId, array $adjustments = []): FinalSettlement
    {
        return DB::transaction(function () use ($resignationRequestId, $adjustments) {
            $request = ResignationRequest::with(['employee.currentSalary', 'contract'])->findOrFail($resignationRequestId);
            $employee = $request->employee;
            $lastWorkingDate = Carbon::parse($request->approved_last_date ?? $request->proposed_last_date);

            // 1. Mức lương theo hợp đồng lao động / Lương cơ sở
            $baseContractSalary = BigDecimal::of($request->contract?->base_salary ?? $employee->currentSalary?->p1_position_salary ?? 10000000);
            $standardMonthlyWorkingDays = BigDecimal::of($adjustments['standard_working_days'] ?? 22);

            // Đơn giá 1 ngày công / 1 ngày phép = Base_Salary / Standard_Days
            $dailyRate = $baseContractSalary->dividedBy($standardMonthlyWorkingDays, 4, RoundingMode::HALF_UP);

            // 2. Tính lương ngày công tháng cuối
            $actualWorkingDays = BigDecimal::of($adjustments['actual_working_days'] ?? $this->getActualWorkingDaysInFinalMonth($employee->id, $lastWorkingDate));
            $actualWorkSalary = $dailyRate->multipliedBy($actualWorkingDays)->toScale(0, RoundingMode::HALF_UP);

            // 3. Tính tiền phép năm chưa nghỉ (Khoản 6 Điều 113 BLLĐ)
            $leaveBalance = LeaveBalance::where('employee_id', $employee->id)
                ->where('year', $lastWorkingDate->year)
                ->first();

            $remainingLeaveDays = BigDecimal::of($leaveBalance ? $leaveBalance->available_days : 0.0);
            $unusedLeaveAllowance = $dailyRate->multipliedBy($remainingLeaveDays)->toScale(0, RoundingMode::HALF_UP);

            // 4. Tính trợ cấp thôi việc (Điều 46 BLLĐ 2019 & Nghị định 145/2020/NĐ-CP)
            $severanceData = $this->calculateSeveranceAllowance($employee, $lastWorkingDate, $baseContractSalary);
            $qualifyingYears = BigDecimal::of($severanceData['qualifying_years']);
            $avgSalary = BigDecimal::of($severanceData['monthly_avg_salary']);
            $severanceAllowance = BigDecimal::of($severanceData['allowance_amount']);

            // 5. Khấu trừ tạm ứng và các khoản giảm trừ
            $advanceDeduction = BigDecimal::of($adjustments['advance_deduction'] ?? 0);
            $otherDeductions = BigDecimal::of($adjustments['other_deductions'] ?? 0);

            // 6. Tổng thực lĩnh quyết toán = Công tháng cuối + Tiền phép tồn + Trợ cấp thôi việc - Tạm ứng - Giảm trừ
            $netSettlementAmount = $actualWorkSalary
                ->plus($unusedLeaveAllowance)
                ->plus($severanceAllowance)
                ->minus($advanceDeduction)
                ->minus($otherDeductions);

            if ($netSettlementAmount->isNegative()) {
                $netSettlementAmount = BigDecimal::zero();
            }

            // 7. Tính hạn thanh toán theo luật: 14 NGÀY LÀM VIỆC (Điều 48 BLLĐ 2019)
            $paymentDeadlineDate = $this->addWorkingDays($lastWorkingDate->copy(), 14);

            $settlementCode = 'STL-' . $lastWorkingDate->format('Ym') . '-' . str_pad((string)rand(1, 99999), 5, '0', STR_PAD_LEFT);

            $settlement = FinalSettlement::updateOrCreate(
                ['resignation_request_id' => $request->id],
                [
                    'settlement_code'              => $settlementCode,
                    'employee_id'                  => $employee->id,
                    'last_working_date'            => $lastWorkingDate->toDateString(),
                    'actual_working_days'          => (float)$actualWorkingDays->toFloat(),
                    'actual_work_salary'           => (string)$actualWorkSalary,
                    'remaining_leave_days'         => (float)$remainingLeaveDays->toFloat(),
                    'daily_leave_rate'             => (string)$dailyRate->toScale(0, RoundingMode::HALF_UP),
                    'unused_leave_allowance'       => (string)$unusedLeaveAllowance,
                    'severance_qualifying_years'   => (float)$qualifyingYears->toFloat(),
                    'severance_monthly_avg_salary' => (string)$avgSalary,
                    'severance_allowance'          => (string)$severanceAllowance,
                    'advance_deduction'            => (string)$advanceDeduction,
                    'other_deductions'             => (string)$otherDeductions,
                    'net_settlement_amount'        => (string)$netSettlementAmount,
                    'payment_deadline_date'        => $paymentDeadlineDate->toDateString(),
                    'payment_status'               => 'PENDING',
                    'notes'                        => "Hạn chót chi trả 14 ngày làm việc: {$paymentDeadlineDate->format('d/m/Y')} (Điều 48 BLLĐ 2019)",
                ]
            );

            $request->update(['settlement_completed_at' => now()]);

            return $settlement;
        });
    }

    /**
     * Tính toán trợ cấp thôi việc theo chuẩn Điều 46 BLLĐ 2019 & NĐ 145/2020/NĐ-CP:
     * - Chỉ áp dụng cho thời gian làm việc không tham gia BHTN (trước ngày 01/01/2009).
     * - Thời gian làm việc từ đủ 1 đến 6 tháng tính 1/2 năm (0.5); trên 6 tháng tính 1 năm (1.0).
     */
    protected function calculateSeveranceAllowance(Employee $employee, Carbon $lastWorkingDate, BigDecimal $baseSalary): array
    {
        $hireDate = Carbon::parse($employee->hire_date);
        $uiStartDate = Carbon::createFromDate(2009, 1, 1); // Thời điểm Luật BHTN bắt đầu có hiệu lực tại Việt Nam

        // Nếu ngày vào làm việc sau ngày 01/01/2009 và toàn bộ thời gian đã đóng BHTN thì không được trợ cấp thôi việc từ DN
        if ($hireDate->greaterThanOrEqualTo($uiStartDate)) {
            return [
                'qualifying_years'   => 0.0,
                'monthly_avg_salary' => (string)$baseSalary,
                'allowance_amount'   => 0,
            ];
        }

        // Thời gian trước 01/01/2009 không tham gia BHTN
        $totalNonUiMonths = $hireDate->diffInMonths($uiStartDate);

        $fullYears = intdiv($totalNonUiMonths, 12);
        $remainderMonths = $totalNonUiMonths % 12;

        // Quy tắc làm tròn thời gian theo Điều 8 Khoản 3 Nghị định 145/2020/NĐ-CP:
        $roundedRemainderYears = 0.0;
        if ($remainderMonths >= 1 && $remainderMonths <= 6) {
            $roundedRemainderYears = 0.5;
        } elseif ($remainderMonths > 6) {
            $roundedRemainderYears = 1.0;
        }

        $qualifyingYears = $fullYears + $roundedRemainderYears;

        // Trợ cấp thôi việc = Số năm * 0.5 * Tiền lương bình quân 6 tháng liền kề
        $yearsBd = BigDecimal::of($qualifyingYears);
        $halfMonthBd = BigDecimal::of('0.5');

        $allowanceAmount = $yearsBd
            ->multipliedBy($halfMonthBd)
            ->multipliedBy($baseSalary)
            ->toScale(0, RoundingMode::HALF_UP);

        return [
            'qualifying_years'   => $qualifyingYears,
            'monthly_avg_salary' => (string)$baseSalary,
            'allowance_amount'   => (string)$allowanceAmount,
        ];
    }

    /**
     * Thêm số ngày làm việc (bỏ qua Thứ Bảy và Chủ Nhật).
     */
    public function addWorkingDays(Carbon $startDate, int $workDaysToAdd): Carbon
    {
        $date = $startDate->copy();
        while ($workDaysToAdd > 0) {
            $date->addDay();
            if (!$date->isWeekend()) {
                $workDaysToAdd--;
            }
        }
        return $date;
    }

    /**
     * Lấy số ngày làm việc thực tế trong tháng cuối.
     */
    protected function getActualWorkingDaysInFinalMonth(int $employeeId, Carbon $lastWorkingDate): float
    {
        $startOfMonth = $lastWorkingDate->copy()->startOfMonth();

        $workedDays = AttendanceDay::where('employee_id', $employeeId)
            ->whereBetween('work_date', [$startOfMonth->toDateString(), $lastWorkingDate->toDateString()])
            ->where('status', 'PRESENT')
            ->count();

        return (float)($workedDays > 0 ? $workedDays : $lastWorkingDate->day);
    }

    /**
     * Tự động khóa tài khoản người dùng sau ngày làm việc cuối cùng.
     * Quét các nhân viên có đơn thôi việc đã được phê duyệt (BOD_APPROVED) và ngày làm việc cuối đã qua.
     */
    public function deactivateResignedAccounts(): int
    {
        $today = now()->startOfDay();

        $pendingDeactivations = ResignationRequest::with('employee.user')
            ->where('status', 'BOD_APPROVED')
            ->where('approved_last_date', '<', $today->toDateString())
            ->get();

        $deactivatedCount = 0;

        foreach ($pendingDeactivations as $req) {
            $employee = $req->employee;
            if (!$employee) continue;

            DB::transaction(function () use ($req, $employee, &$deactivatedCount) {
                // 1. Cập nhật trạng thái nhân viên
                $employee->update([
                    'employment_status' => 'RESIGNED',
                    'termination_date'  => $req->approved_last_date,
                ]);

                // 2. Vô hiệu hóa tài khoản hệ thống / ESS
                if ($employee->user) {
                    $employee->user->update([
                        'is_active' => false,
                    ]);

                    // Hủy tất cả tokens / sessions nếu có
                    if (method_exists($employee->user, 'tokens')) {
                        $employee->user->tokens()->delete();
                    }
                }

                // 3. Ghi audit log
                AuditLog::create([
                    'table_name'   => 'employees',
                    'record_id'    => $employee->id,
                    'action'       => 'UPDATE',
                    'performed_by' => 1, // System Automated Scheduler
                    'new_values'   => [
                        'event'             => 'AUTOMATED_ACCOUNT_DEACTIVATION',
                        'employment_status' => 'RESIGNED',
                        'termination_date'  => $req->approved_last_date,
                    ],
                ]);

                $deactivatedCount++;
            });
        }

        return $deactivatedCount;
    }
}
