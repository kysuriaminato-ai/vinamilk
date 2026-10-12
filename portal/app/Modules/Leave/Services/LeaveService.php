<?php

namespace App\Modules\Leave\Services;

use App\Models\AuditLog;
use App\Models\Notification;
use App\Models\User;
use App\Modules\Attendance\Models\AttendanceDay;
use App\Modules\Employee\Models\Employee;
use App\Modules\Leave\Models\LeaveBalance;
use App\Modules\Leave\Models\LeaveRequest;
use App\Modules\Leave\Models\LeaveType;
use App\Modules\Organization\Models\OrgUnit;
use Carbon\Carbon;
use DomainException;
use Illuminate\Http\UploadedFile;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\DB;

class LeaveService
{
    /**
     * Khởi tạo hoặc cập nhật số dư phép năm theo thâm niên (Điều 113, 114 BLLĐ 2019).
     * - Tiêu chuẩn cơ bản: 12 ngày phép năm.
     * - Phép thâm niên: Cứ đủ 05 năm làm việc tại Vinamilk thì được cộng thêm 01 ngày phép.
     */
    public function initializeLeaveBalance(Employee $employee, int $year): LeaveBalance
    {
        $hireDate = Carbon::parse($employee->hire_date);
        $startOfYear = Carbon::createFromDate($year, 1, 1);

        $yearsOfService = $hireDate->diffInYears($startOfYear);
        $seniorityDays = floor($yearsOfService / 5.0);

        return LeaveBalance::updateOrCreate(
            ['employee_id' => $employee->id, 'year' => $year],
            [
                'annual_leave_quota'   => 12.0,
                'seniority_leave_days' => (float)$seniorityDays,
                'transferred_days'     => 0.0,
                'used_days'            => 0.0,
            ]
        );
    }

    /**
     * Tìm Quản lý trực tiếp (Line Manager) theo Cây tổ chức (Organizational Tree).
     * Đảm bảo nguyên tắc SoD: Nếu nhân viên chính là Trưởng đơn vị hoặc đơn vị khuyết quản lý,
     * hệ thống tự động leo lên cây cha (parent_id) cho đến khi tìm được Quản lý cấp trên.
     */
    public function findDirectManager(Employee $employee): ?Employee
    {
        $assignment = $employee->currentAssignment;
        if (!$assignment || !$assignment->org_unit_id) {
            return null;
        }

        $currentOrg = OrgUnit::find($assignment->org_unit_id);

        while ($currentOrg) {
            if ($currentOrg->manager_id && $currentOrg->manager_id !== $employee->id) {
                return Employee::find($currentOrg->manager_id);
            }

            // Nếu nhân viên chính là manager hoặc đơn vị chưa có manager, leo lên đơn vị cha
            if (!$currentOrg->parent_id) {
                break;
            }
            $currentOrg = OrgUnit::find($currentOrg->parent_id);
        }

        return null;
    }

    /**
     * Nộp đơn xin nghỉ phép trên cổng ESS (Employee Self-Service).
     * Kiểm tra quỹ phép, tài liệu y tế đính kèm, xác định quản lý duyệt từ cây tổ chức.
     */
    public function submitLeaveRequest(int $employeeId, array $data, ?UploadedFile $attachment = null): LeaveRequest
    {
        return DB::transaction(function () use ($employeeId, $data, $attachment) {
            $employee = Employee::with(['currentAssignment.orgUnit'])->findOrFail($employeeId);
            $leaveType = LeaveType::findOrFail($data['leave_type_id']);

            $startDate = Carbon::parse($data['start_date']);
            $endDate = Carbon::parse($data['end_date']);
            $leaveDays = (float)($data['leave_days'] ?? ($startDate->diffInDays($endDate) + 1));

            if ($endDate->lessThan($startDate)) {
                throw new DomainException("Ngày kết thúc nghỉ không được trước ngày bắt đầu.");
            }

            // 1. Kiểm tra quỹ phép năm nếu loại nghỉ có cờ trừ quỹ phép
            if ($leaveType->deduct_annual_balance) {
                $balance = LeaveBalance::where('employee_id', $employeeId)
                    ->where('year', $startDate->year)
                    ->first();

                if (!$balance) {
                    $balance = $this->initializeLeaveBalance($employee, $startDate->year);
                }

                $availableDays = $balance->annual_leave_quota + $balance->seniority_leave_days + $balance->transferred_days - $balance->used_days;

                if ($availableDays < $leaveDays) {
                    throw new DomainException(
                        "Số dư quỹ phép năm không đủ (Còn khả dụng: {$availableDays} ngày, Đăng ký: {$leaveDays} ngày)."
                    );
                }
            }

            // 2. Kiểm tra chứng từ đính kèm (giấy ra viện, giấy khám thai, C65-HD, chứng nhận kết hôn...)
            $attachmentUrl = null;
            if ($leaveType->requires_attachment) {
                if (!$attachment && empty($data['attachment_url'])) {
                    throw new DomainException("Loại nghỉ [{$leaveType->type_name}] bắt buộc đính kèm văn bản chứng từ minh chứng.");
                }
                if ($attachment) {
                    $attachmentUrl = $attachment->store('leave/attachments', 'public');
                } else {
                    $attachmentUrl = $data['attachment_url'];
                }
            }

            // 3. Khởi tạo mã đơn và tạo bản ghi
            $requestCode = 'LR-' . date('Ym') . '-' . str_pad((string)rand(1, 99999), 5, '0', STR_PAD_LEFT);

            $request = LeaveRequest::create([
                'request_code'   => $requestCode,
                'employee_id'    => $employeeId,
                'leave_type_id'  => $leaveType->id,
                'start_date'     => $startDate->toDateString(),
                'end_date'       => $endDate->toDateString(),
                'leave_days'     => $leaveDays,
                'reason'         => $data['reason'] ?? '',
                'attachment_url' => $attachmentUrl,
                'status'         => 'PENDING',
            ]);

            // 4. Tìm Quản lý trực tiếp theo cây tổ chức để gửi thông báo phê duyệt
            $manager = $this->findDirectManager($employee);
            if ($manager && $manager->user) {
                Notification::create([
                    'user_id'  => $manager->user->id,
                    'title'    => "Đơn xin nghỉ phép mới từ {$employee->full_name}",
                    'content'  => "Nhân viên {$employee->full_name} ({$employee->employee_code}) gửi đơn nghỉ [{$leaveType->type_name}] từ ngày {$startDate->format('d/m/Y')} đến {$endDate->format('d/m/Y')} ({$leaveDays} ngày).",
                    'type'     => 'APPROVAL_REQUEST',
                    'link_url' => "/leave/requests/{$request->id}",
                ]);
            }

            return $request;
        });
    }

    /**
     * Phê duyệt đơn xin nghỉ phép.
     * Áp dụng nguyên tắc SoD (Quản lý không được tự duyệt đơn của bản thân).
     * Tự động trừ quỹ phép năm và cập nhật dữ liệu bảng chấm công attendance_days.
     */
    public function approveLeaveRequest(int $requestId, User $approverUser): LeaveRequest
    {
        return DB::transaction(function () use ($requestId, $approverUser) {
            $request = LeaveRequest::with(['employee.currentAssignment', 'leaveType'])->findOrFail($requestId);

            if ($request->status !== 'PENDING') {
                throw new DomainException("Đơn nghỉ phép này đã được xử lý (Trạng thái hiện tại: {$request->status}).");
            }

            // KIỂM SOÁT NỘI BỘ (SOD): CẤM TỰ PHÊ DUYỆT ĐƠN NGHỈ PHÉP CỦA CHÍNH MÌNH
            if ($approverUser->employee_id && $approverUser->employee_id === $request->employee_id) {
                throw new DomainException('Nguyên tắc Segregation of Duties (SoD): Người dùng không được tự phê duyệt đơn xin nghỉ phép của chính mình.');
            }

            $request->update([
                'status'      => 'APPROVED',
                'approved_by' => $approverUser->employee_id ?? 1,
                'approved_at' => now(),
            ]);

            // 1. Trừ quỹ phép năm nếu loại nghỉ có cờ deduct_annual_balance
            if ($request->leaveType->deduct_annual_balance) {
                $balance = LeaveBalance::where('employee_id', $request->employee_id)
                    ->where('year', $request->start_date->year)
                    ->first();

                if ($balance) {
                    $balance->increment('used_days', $request->leave_days);
                }
            }

            // 2. Xác định trạng thái công tương ứng trong attendance_days
            $statusMapping = 'LEAVE_PAID';
            $actualHours = 8.0;

            if ($request->leaveType->type_code === 'UNPAID') {
                $statusMapping = 'LEAVE_UNPAID';
                $actualHours = 0.0;
            } elseif ($request->leaveType->social_insurance_coverage) {
                $statusMapping = 'LEAVE_INSURANCE';
                $actualHours = 0.0;
            } elseif (!$request->leaveType->is_paid) {
                $statusMapping = 'LEAVE_UNPAID';
                $actualHours = 0.0;
            }

            // 3. Cập nhật từng ngày nghỉ vào bảng attendance_days
            $currentDate = $request->start_date->copy();
            while ($currentDate->lessThanOrEqualTo($request->end_date)) {
                // Bỏ qua ngày nghỉ cuối tuần nếu ca hành chính (Thứ 7, CN)
                $isWeekend = $currentDate->isWeekend();

                AttendanceDay::updateOrCreate(
                    [
                        'employee_id' => $request->employee_id,
                        'work_date'   => $currentDate->toDateString(),
                    ],
                    [
                        'status'         => $statusMapping,
                        'actual_hours'   => $isWeekend ? 0.0 : $actualHours,
                        'standard_hours' => 8.0,
                    ]
                );

                $currentDate->addDay();
            }

            // Gửi thông báo cho nhân viên
            if ($request->employee->user) {
                Notification::create([
                    'user_id'  => $request->employee->user->id,
                    'title'    => "Đơn xin nghỉ phép đã được phê duyệt",
                    'content'  => "Đơn xin nghỉ [{$request->leaveType->type_name}] từ ngày {$request->start_date->format('d/m/Y')} của bạn đã được phê duyệt.",
                    'type'     => 'STATUS_UPDATE',
                    'link_url' => "/leave/requests/{$request->id}",
                ]);
            }

            return $request;
        });
    }

    /**
     * Từ chối đơn xin nghỉ phép.
     */
    public function rejectLeaveRequest(int $requestId, User $approverUser, string $rejectionReason): LeaveRequest
    {
        return DB::transaction(function () use ($requestId, $approverUser, $rejectionReason) {
            $request = LeaveRequest::with('employee.user')->findOrFail($requestId);

            if ($request->status !== 'PENDING') {
                throw new DomainException("Đơn nghỉ phép này đã được xử lý.");
            }

            if ($approverUser->employee_id && $approverUser->employee_id === $request->employee_id) {
                throw new DomainException('Nguyên tắc SoD: Không được tự thao tác trên đơn nghỉ phép của chính mình.');
            }

            $request->update([
                'status'           => 'REJECTED',
                'approved_by'      => $approverUser->employee_id ?? 1,
                'approved_at'      => now(),
                'rejection_reason' => $rejectionReason,
            ]);

            if ($request->employee->user) {
                Notification::create([
                    'user_id'  => $request->employee->user->id,
                    'title'    => "Đơn xin nghỉ phép bị từ chối",
                    'content'  => "Đơn nghỉ [{$request->leaveType->type_name}] bị từ chối. Lý do: {$rejectionReason}",
                    'type'     => 'STATUS_UPDATE',
                    'link_url' => "/leave/requests/{$request->id}",
                ]);
            }

            return $request;
        });
    }

    /**
     * Ghi nhận vắng mặt không phép (AWOL - Absent Without Leave).
     * - Trừ 100% công ngày vắng, trừ điểm thi đua/chuyên cần.
     * - Cảnh báo kỷ luật sa thải: Nếu tự ý bỏ việc cộng dồn từ 05 ngày trong thời hạn 30 ngày (Điều 125 Khoản 4 BLLĐ 2019),
     *   hệ thống tự động kích hoạt Cảnh báo khẩn cấp và đề xuất thành lập Hội đồng Kỷ luật Sa thải.
     */
    public function recordUnauthorizedAbsence(int $employeeId, Carbon $absenceDate, string $reason = ''): AttendanceDay
    {
        return DB::transaction(function () use ($employeeId, $absenceDate, $reason) {
            $employee = Employee::with('currentAssignment.orgUnit.manager.user')->findOrFail($employeeId);

            // 1. Cập nhật ngày công thành UNAUTHORIZED_ABSENCE (0 giờ công)
            $attDay = AttendanceDay::updateOrCreate(
                [
                    'employee_id' => $employeeId,
                    'work_date'   => $absenceDate->toDateString(),
                ],
                [
                    'status'         => 'UNAUTHORIZED_ABSENCE',
                    'actual_hours'   => 0.0,
                    'standard_hours' => 8.0,
                    'late_minutes'   => 0,
                    'early_minutes'  => 0,
                ]
            );

            // 2. Quét cửa sổ trượt 30 ngày gần nhất tính đến ngày vi phạm hiện tại
            $windowStart = $absenceDate->copy()->subDays(30);
            $cumulativeAwolDays = AttendanceDay::where('employee_id', $employeeId)
                ->where('status', 'UNAUTHORIZED_ABSENCE')
                ->whereBetween('work_date', [$windowStart->toDateString(), $absenceDate->toDateString()])
                ->count();

            // 3. RÀNG BUỘC PHÁP LÝ ĐIỀU 125 KHOẢN 4 BLLĐ: CỘNG DỒN >= 5 NGÀY TRONG 30 NGÀY
            if ($cumulativeAwolDays >= 5) {
                $manager = $this->findDirectManager($employee);
                $managerUser = $manager?->user;

                if ($managerUser) {
                    Notification::create([
                        'user_id'  => $managerUser->id,
                        'title'    => "CẢNH BÁO KỶ LUẬT SA THẢI: {$employee->full_name} tự ý bỏ việc {$cumulativeAwolDays} ngày",
                        'content'  => "Nhân viên {$employee->full_name} ({$employee->employee_code}) đã tự ý nghỉ không phép cộng dồn {$cumulativeAwolDays} ngày trong 30 ngày gần nhất. Căn cứ Khoản 4 Điều 125 Bộ luật Lao động 2019, trường hợp này đủ điều kiện áp dụng hình thức kỷ luật sa thải. Đề nghị kích hoạt Hội đồng Kỷ luật.",
                        'type'     => 'SYSTEM_ALERT',
                        'link_url' => "/employees/{$employee->id}/disciplinary",
                    ]);
                }

                AuditLog::create([
                    'table_name'   => 'attendance_days',
                    'record_id'    => $employeeId,
                    'action'       => 'UPDATE',
                    'performed_by' => Auth::id() ?? 1,
                    'new_values'   => [
                        'event'                  => 'DISMISSAL_THRESHOLD_REACHED',
                        'legal_basis'            => 'BLLD_2019_ARTICLE_125_CLAUSE_4',
                        'cumulative_awol_days'   => $cumulativeAwolDays,
                        'evaluation_window_days' => 30,
                    ],
                ]);
            }

            return $attDay;
        });
    }
}
