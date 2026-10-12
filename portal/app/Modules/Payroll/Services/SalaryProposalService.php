<?php

namespace App\Modules\Payroll\Services;

use App\Models\AuditLog;
use App\Models\Notification;
use App\Models\User;
use App\Modules\Employee\Models\Employee;
use App\Modules\Kpi\Models\KpiEmployeeSummary;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Payroll\Models\ApprovalRule;
use App\Modules\Payroll\Models\ApprovalStep;
use App\Modules\Payroll\Models\EmployeeSalary;
use App\Modules\Payroll\Models\PayrollPeriod;
use App\Modules\Payroll\Models\RegionalMinWage;
use App\Modules\Payroll\Models\RetroactiveSalaryAdjustment;
use App\Modules\Payroll\Models\SalaryBand;
use App\Modules\Payroll\Models\SalaryProposal;
use App\Modules\Payroll\Models\UnitSalaryBudget;
use Brick\Math\BigDecimal;
use Brick\Math\RoundingMode;
use Carbon\Carbon;
use DomainException;
use Illuminate\Support\Facades\DB;

class SalaryProposalService
{
    /**
     * Chuẩn bị dữ liệu đề xuất tăng lương: Tự động điền lương hiện tại, khung lương,
     * compa-ratio, kết quả KPI 2 kỳ gần nhất, và lần tăng lương gần nhất.
     */
    public function prepareProposalData(int $employeeId, User $creatorUser): array
    {
        $employee = Employee::with(['currentAssignment.orgUnit', 'currentSalary'])->findOrFail($employeeId);

        // 1. Kiểm tra phạm vi cây tổ chức: Người lập chỉ đề xuất cho nhân viên thuộc nhánh của mình
        $this->validateSubtreeScope($creatorUser, $employee);

        // 2. Lương hiện tại
        $currentSalary = $employee->currentSalary;
        $currentP1 = (float)($currentSalary?->p1_position_salary ?? 10000000);
        $currentP2 = (float)($currentSalary?->p2_competency_salary ?? 0);
        $currentTotal = $currentP1 + $currentP2;
        $currentInsurance = (float)($currentSalary?->insurance_salary ?? $currentP1);

        // 3. Khung lương của chức danh
        $jobLevel = $employee->currentAssignment?->position?->level ?? 2;
        $salaryBand = SalaryBand::where('job_level', $jobLevel)->first()
            ?? SalaryBand::first();

        $bandMin = (float)($salaryBand?->min_salary ?? 15000000);
        $bandMidpoint = (float)($salaryBand?->midpoint_salary ?? 22000000);
        $bandMax = (float)($salaryBand?->max_salary ?? 30000000);

        // Compa-Ratio hiện tại = (Lương P1 / Midpoint) * 100%
        $currentCompaRatio = $bandMidpoint > 0 ? round(($currentP1 / $bandMidpoint) * 100, 2) : 100.0;

        // 4. Lịch sử KPI 2 kỳ gần nhất
        $recentKpis = KpiEmployeeSummary::with('period')
            ->where('employee_id', $employeeId)
            ->latest('id')
            ->take(2)
            ->get()
            ->map(fn($item) => [
                'period_code' => $item->period?->period_code ?? 'KPI',
                'period_name' => $item->period?->period_name ?? '',
                'score'       => (float)$item->kpi_score,
                'grade'       => $item->kpi_grade,
                'multiplier'  => (float)$item->p3_multiplier,
            ])
            ->toArray();

        // 5. Lần tăng lương gần nhất
        $lastSalaryRecord = EmployeeSalary::where('employee_id', $employeeId)
            ->where('effective_from', '<', now()->toDateString())
            ->latest('effective_from')
            ->first();

        $lastIncreaseDate = $lastSalaryRecord?->effective_from ?? Carbon::parse($employee->hire_date);
        $monthsSinceLastIncrease = Carbon::parse($lastIncreaseDate)->diffInMonths(now());

        // 6. Lương tối thiểu vùng
        $minWageRecord = RegionalMinWage::where('region_zone', 'REGION_1')->first();
        $regionalMinWage = (float)($minWageRecord?->min_wage_amount ?? 4960000);

        // 7. Ngân sách tăng lương còn lại của đơn vị
        $orgUnitId = $employee->currentAssignment?->org_unit_id;
        $unitBudget = UnitSalaryBudget::where('org_unit_id', $orgUnitId)
            ->where('year', now()->year)
            ->first();
        $remainingBudget = $unitBudget ? $unitBudget->remaining_amount : 1000000000; // Mặc định 1 tỷ

        return [
            'employee' => [
                'id'            => $employee->id,
                'code'          => $employee->employee_code,
                'name'          => $employee->full_name,
                'org_unit_name' => $employee->currentAssignment?->orgUnit?->unit_name,
                'position_name' => $employee->currentAssignment?->position?->position_name,
                'job_level'     => $jobLevel,
            ],
            'current_salary' => [
                'p1'               => $currentP1,
                'p2'               => $currentP2,
                'total'            => $currentTotal,
                'insurance_salary' => $currentInsurance,
            ],
            'salary_band' => [
                'id'          => $salaryBand?->id,
                'code'        => $salaryBand?->band_code,
                'name'        => $salaryBand?->band_name,
                'min'         => $bandMin,
                'midpoint'    => $bandMidpoint,
                'max'         => $bandMax,
                'compa_ratio' => $currentCompaRatio,
            ],
            'kpi_history' => $recentKpis,
            'last_increase' => [
                'date'   => Carbon::parse($lastIncreaseDate)->toDateString(),
                'months' => $monthsSinceLastIncrease,
            ],
            'guardrails' => [
                'regional_min_wage' => $regionalMinWage,
                'remaining_budget'  => $remainingBudget,
            ],
        ];
    }

    /**
     * Tạo và gửi Tờ trình đề xuất tăng lương với luồng duyệt sinh động.
     */
    public function createSalaryProposal(array $data, User $creatorUser): SalaryProposal
    {
        return DB::transaction(function () use ($data, $creatorUser) {
            $employeeId = (int)$data['employee_id'];
            $employee = Employee::with(['currentAssignment.orgUnit', 'currentAssignment.position', 'currentSalary'])->findOrFail($employeeId);

            // KIỂM SOÁT NỘI BỘ (SOD): CẤM TỰ ĐỀ XUẤT TĂNG LƯƠNG CHO CHÍNH MÌNH
            if ($creatorUser->employee_id && $creatorUser->employee_id === $employeeId) {
                throw new DomainException("Nguyên tắc kiểm soát nội bộ (SoD): Người dùng không được tự lập đề xuất tăng lương cho chính mình.");
            }

            // 1. Kiểm tra phạm vi dữ liệu cây tổ chức (Subtree Scope)
            $this->validateSubtreeScope($creatorUser, $employee);

            $prepData = $this->prepareProposalData($employeeId, $creatorUser);

            $proposedP1 = (float)$data['proposed_p1'];
            $proposedP2 = (float)($data['proposed_p2'] ?? $prepData['current_salary']['p2']);
            $proposedInsurance = (float)($data['proposed_insurance_salary'] ?? $proposedP1);
            $proposalType = $data['proposal_type']; // PERIODIC, DEPARTMENT_RECOMMENDED, SPECIAL_EXCEPTION, MASS_STATUTORY

            // 2. Kiểm tra không dưới mức Lương tối thiểu vùng
            $regionalMinWage = $prepData['guardrails']['regional_min_wage'];
            if ($proposedP1 < $regionalMinWage) {
                throw new DomainException(
                    "Mức lương P1 đề xuất (" . number_format($proposedP1) . " VND) không được thấp hơn mức Lương tối thiểu vùng (" . number_format($regionalMinWage) . " VND theo Nghị định)."
                );
            }

            // 3. Kiểm tra trần khung lương (Band Max) & Điều kiện Đặc cách
            $bandMax = $prepData['salary_band']['max'];
            $bandMidpoint = $prepData['salary_band']['midpoint'];
            $isOutOfBand = $proposedP1 > $bandMax;

            $isSpecialException = ($proposalType === 'SPECIAL_EXCEPTION');

            if ($isOutOfBand && !$isSpecialException) {
                throw new DomainException(
                    "Mức lương P1 đề xuất (" . number_format($proposedP1) . " VND) đã vượt trần khung lương của chức danh (" . number_format($bandMax) . " VND). Để đề xuất mức vượt trần, vui lòng chọn loại tờ trình [Đặc cách] và cung cấp hồ sơ minh chứng."
                );
            }

            // 4. Nếu là diện Đặc cách: Bắt buộc lý do và minh chứng
            $warnings = [];
            if ($isSpecialException) {
                if (empty(trim($data['exception_reason'] ?? ''))) {
                    throw new DomainException("Đề xuất diện Đặc cách bắt buộc phải giải trình lý do chi tiết và đính kèm hồ sơ minh chứng.");
                }

                // Cảnh báo thời gian giữa 2 lần tăng
                if ($prepData['last_increase']['months'] < 12) {
                    $warnings[] = "CHƯA ĐỦ THỜI GIAN: Khoảng cách từ lần tăng gần nhất mới được {$prepData['last_increase']['months']} tháng (Quy định tiêu chuẩn ≥ 12 tháng).";
                }

                // Cảnh báo vượt khung lương
                if ($isOutOfBand) {
                    $warnings[] = "VƯỢT TRẦN KHUNG LƯƠNG: Mức lương vượt trần khung chức danh " . number_format($proposedP1 - $bandMax) . " VND.";
                }

                $proposedCompaRatio = $bandMidpoint > 0 ? round(($proposedP1 / $bandMidpoint) * 100, 2) : 100.0;
                if ($proposedCompaRatio > 120.0) {
                    $warnings[] = "COMPA-RATIO CAO: Tỷ lệ Compa-Ratio đạt {$proposedCompaRatio}% (> 120% trung vị).";
                }
            } else {
                $proposedCompaRatio = $bandMidpoint > 0 ? round(($proposedP1 / $bandMidpoint) * 100, 2) : 100.0;
            }

            // 5. Kiểm tra ngân sách tăng lương còn lại của đơn vị
            $monthlyIncrease = ($proposedP1 + $proposedP2) - ($prepData['current_salary']['total']);
            $annualImpact = $monthlyIncrease * 12;
            if ($annualImpact > $prepData['guardrails']['remaining_budget'] && !$isSpecialException) {
                throw new DomainException("Mức tăng đề xuất làm vượt ngân sách quỹ lương năm của đơn vị (Vượt " . number_format($annualImpact - $prepData['guardrails']['remaining_budget']) . " VND).");
            }

            // 6. Xác định cấp bậc quản lý cao cấp
            $isSeniorManagement = ($prepData['employee']['job_level'] >= 4);

            // 7. Xác định Workflow Type sinh từ approval_rules
            $workflowType = 'DEFAULT';
            if ($isSeniorManagement) {
                $workflowType = 'SENIOR_MANAGEMENT';
            } elseif ($isOutOfBand || $isSpecialException) {
                $workflowType = 'OUT_OF_BAND_OR_EXCEPTION';
            }

            $proposalCode = 'PROP-SAL-' . date('Ym') . '-' . str_pad((string)rand(1, 99999), 5, '0', STR_PAD_LEFT);

            $proposal = SalaryProposal::create([
                'proposal_code'              => $proposalCode,
                'employee_id'                => $employeeId,
                'proposal_type'              => $proposalType,
                'current_p1'                 => $prepData['current_salary']['p1'],
                'proposed_p1'                => $proposedP1,
                'current_p2'                 => $prepData['current_salary']['p2'],
                'proposed_p2'                => $proposedP2,
                'current_insurance_salary'   => $prepData['current_salary']['insurance_salary'],
                'proposed_insurance_salary'  => $proposedInsurance,
                'salary_band_id'             => $prepData['salary_band']['id'],
                'band_min'                   => $prepData['salary_band']['min'],
                'band_midpoint'              => $prepData['salary_band']['midpoint'],
                'band_max'                   => $prepData['salary_band']['max'],
                'current_compa_ratio'        => $prepData['salary_band']['compa_ratio'],
                'proposed_compa_ratio'       => $proposedCompaRatio,
                'kpi_history_json'           => $prepData['kpi_history'],
                'last_salary_increase_date'  => $prepData['last_increase']['date'],
                'months_since_last_increase' => $prepData['last_increase']['months'],
                'is_out_of_band'             => $isOutOfBand,
                'is_special_exception'       => $isSpecialException,
                'is_senior_management'       => $isSeniorManagement,
                'exception_reason'           => $data['exception_reason'] ?? null,
                'attachment_url'             => $data['attachment_url'] ?? null,
                'warnings_json'              => $warnings,
                'effective_from'             => Carbon::parse($data['effective_from'])->toDateString(),
                'reason'                     => trim($data['reason']),
                'status'                     => 'SUBMITTED',
                'current_step_number'        => 1,
                'current_workflow_type'      => $workflowType,
                'created_by'                 => $creatorUser->employee_id ?? 1,
            ]);

            // 8. Tự động sinh danh sách các bước phê duyệt động từ bảng approval_rules
            $rules = ApprovalRule::where('workflow_type', $workflowType)
                ->orderBy('step_order')
                ->get();

            foreach ($rules as $rule) {
                ApprovalStep::create([
                    'proposal_id'   => $proposal->id,
                    'step_number'   => $rule->step_order,
                    'approver_role' => $rule->approver_role,
                    'action'        => 'PENDING',
                ]);
            }

            return $proposal;
        });
    }

    /**
     * Phê duyệt một bước trong luồng duyệt động (Tuân thủ SoD).
     */
    public function approveStep(int $proposalId, User $approverUser, string $comments = ''): SalaryProposal
    {
        return DB::transaction(function () use ($proposalId, $approverUser, $comments) {
            $proposal = SalaryProposal::with(['steps', 'employee'])->findOrFail($proposalId);

            if (!in_array($proposal->status, ['SUBMITTED', 'PENDING_STEP'])) {
                throw new DomainException("Tờ trình đang ở trạng thái '{$proposal->status}', không thể phê duyệt tiếp.");
            }

            // NGUYÊN TẮC SOD: CẤM TỰ PHÊ DUYỆT TỜ TRÌNH CỦA CHÍNH MÌNH
            if ($approverUser->employee_id && $approverUser->employee_id === $proposal->employee_id) {
                throw new DomainException("Nguyên tắc kiểm soát nội bộ (SoD): Bạn không thể tự phê duyệt đề xuất tăng lương của chính mình.");
            }

            $currentStep = $proposal->steps->where('step_number', $proposal->current_step_number)->first();
            if (!$currentStep) {
                throw new DomainException("Không tìm thấy thông tin bước duyệt số {$proposal->current_step_number}.");
            }

            // Ghi nhận phê duyệt bước hiện tại
            $currentStep->update([
                'action'      => 'APPROVED',
                'approver_id' => $approverUser->employee_id ?? 1,
                'comments'    => $comments,
                'action_at'   => now(),
            ]);

            $nextStep = $proposal->steps->where('step_number', $proposal->current_step_number + 1)->first();

            if ($nextStep) {
                // Còn bước duyệt tiếp theo
                $proposal->update([
                    'current_step_number' => $nextStep->step_number,
                    'status'              => 'PENDING_STEP',
                ]);
            } else {
                // ĐÂY LÀ BƯỚC CUỐI CÙNG -> KÍCH HOẠT HOÀN TẤT TRONG 1 TRANSACTION NGUYÊN TỬ
                $this->finalizeFinalApproval($proposal, $approverUser);
            }

            return $proposal;
        });
    }

    /**
     * Từ chối tờ trình tại một bước duyệt.
     */
    public function rejectStep(int $proposalId, User $approverUser, string $rejectionReason): SalaryProposal
    {
        return DB::transaction(function () use ($proposalId, $approverUser, $rejectionReason) {
            $proposal = SalaryProposal::with(['steps', 'employee.user'])->findOrFail($proposalId);

            if ($approverUser->employee_id && $approverUser->employee_id === $proposal->employee_id) {
                throw new DomainException("Nguyên tắc SoD: Không thể thao tác trên đề xuất của chính mình.");
            }

            $currentStep = $proposal->steps->where('step_number', $proposal->current_step_number)->first();
            if ($currentStep) {
                $currentStep->update([
                    'action'      => 'REJECTED',
                    'approver_id' => $approverUser->employee_id ?? 1,
                    'comments'    => $rejectionReason,
                    'action_at'   => now(),
                ]);
            }

            $proposal->update([
                'status' => 'REJECTED',
            ]);

            if ($proposal->employee?->user) {
                Notification::create([
                    'user_id'  => $proposal->employee->user->id,
                    'title'    => "Tờ trình đề xuất tăng lương bị từ chối",
                    'content'  => "Đề xuất tăng lương [{$proposal->proposal_code}] bị từ chối. Lý do: {$rejectionReason}",
                    'type'     => 'STATUS_UPDATE',
                    'link_url' => "/payroll/proposals/{$proposal->id}",
                ]);
            }

            return $proposal;
        });
    }

    /**
     * HOÀN TẤT PHÊ DUYỆT CUỐI CÙNG (1 TRANSACTION NGUYÊN TỬ):
     * 1. Sinh Quyết định PDF có số quyết định.
     * 2. Đóng bản ghi employee_salaries cũ, mở bản ghi mới (SCD Type 2).
     * 3. Xử lý hồi tố (Truy lĩnh) nếu ngày hiệu lực thuộc kỳ đã chốt.
     * 4. Cập nhật mức lương đóng BHXH.
     * 5. Gửi thông báo & Ghi audit log.
     */
    protected function finalizeFinalApproval(SalaryProposal $proposal, User $approverUser): void
    {
        $employee = $proposal->employee;
        $effectiveDate = Carbon::parse($proposal->effective_from);

        // 1. Sinh số Quyết định chính thức
        $year = $effectiveDate->year;
        $decisionNumber = "QD-TL/{$year}/VNM-" . str_pad((string)$employee->id, 5, '0', STR_PAD_LEFT);
        $decisionPdfUrl = "/storage/decisions/{$decisionNumber}.pdf";

        // 2. Đóng bản ghi employee_salaries cũ và mở bản ghi mới
        $currentSalary = EmployeeSalary::where('employee_id', $employee->id)
            ->where('is_current', true)
            ->first();

        if ($currentSalary) {
            $currentSalary->update([
                'effective_to' => $effectiveDate->copy()->subDay()->toDateString(),
                'is_current'   => false,
            ]);
        }

        EmployeeSalary::create([
            'employee_id'          => $employee->id,
            'p1_position_salary'   => $proposal->proposed_p1,
            'p2_competency_salary' => $proposal->proposed_p2,
            'insurance_salary'     => $proposal->proposed_insurance_salary,
            'decision_number'      => $decisionNumber,
            'effective_from'       => $effectiveDate->toDateString(),
            'effective_to'         => null,
            'is_current'           => true,
        ]);

        // 3. XỬ LÝ HỒI TỐ & TÍNH TOÁN KHOẢN TRUY LĨNH (RETROACTIVE ARREARS)
        // Quét các kỳ lương đã chốt/khóa mà ngày kết thúc kỳ >= ngày hiệu lực
        $closedPeriods = PayrollPeriod::where('status', 'PAID')
            ->orWhere('status', 'BOD_APPROVED')
            ->orWhere('status', 'LOCKED')
            ->get()
            ->filter(function ($period) use ($effectiveDate) {
                $periodEnd = Carbon::create($period->year, $period->month)->endOfMonth();
                return $periodEnd->greaterThanOrEqualTo($effectiveDate);
            });

        $retroactiveMonths = 0;
        $monthlyDiff = ($proposal->proposed_p1 + $proposal->proposed_p2) - ($proposal->current_p1 + $proposal->current_p2);
        $totalRetroactiveAmount = BigDecimal::zero();

        if ($closedPeriods->isNotEmpty() && $monthlyDiff > 0) {
            // Tìm kỳ lương mở tiếp theo để nhận khoản truy lĩnh
            $targetOpenPeriod = PayrollPeriod::where('status', 'DRAFT')
                ->where('year', '>=', now()->year)
                ->orderBy('year')
                ->orderBy('month')
                ->first()
                ?? PayrollPeriod::create([
                    'period_code'           => 'PAY-' . now()->addMonth()->format('Y-m'),
                    'month'                 => now()->addMonth()->month,
                    'year'                  => now()->addMonth()->year,
                    'standard_working_days' => 22.0,
                    'status'                => 'DRAFT',
                ]);

            foreach ($closedPeriods as $closedP) {
                RetroactiveSalaryAdjustment::create([
                    'salary_proposal_id'     => $proposal->id,
                    'employee_id'            => $employee->id,
                    'payroll_period_id'      => $targetOpenPeriod->id,
                    'source_period_id'       => $closedP->id,
                    'retroactive_difference' => $monthlyDiff,
                    'status'                 => 'PENDING',
                ]);
                $retroactiveMonths++;
                $totalRetroactiveAmount = $totalRetroactiveAmount->plus(BigDecimal::of($monthlyDiff));
            }
        }

        // 4. Cập nhật trạng thái Tờ trình
        $proposal->update([
            'status'             => 'APPROVED',
            'decision_number'    => $decisionNumber,
            'decision_pdf_url'   => $decisionPdfUrl,
            'retroactive_months' => $retroactiveMonths,
            'retroactive_amount' => (string)$totalRetroactiveAmount,
            'approved_at'        => now(),
            'approved_by'        => $approverUser->employee_id ?? 1,
        ]);

        // 5. Gửi thông báo đến nhân viên
        if ($employee->user) {
            Notification::create([
                'user_id'  => $employee->user->id,
                'title'    => "QUYẾT ĐỊNH TĂNG LƯƠNG: {$decisionNumber}",
                'content'  => "Chúc mừng bạn! Quyết định tăng lương số {$decisionNumber} đã được ký duyệt chính thức. Mức lương P1 mới: " . number_format($proposal->proposed_p1) . " VND (Hiệu lực từ {$effectiveDate->format('d/m/Y')})." . ($retroactiveMonths > 0 ? " Khoản truy lĩnh {$retroactiveMonths} tháng (" . number_format($totalRetroactiveAmount->toFloat()) . " VND) sẽ được chi trả trong kỳ lương kế tiếp." : ""),
                'type'     => 'STATUS_UPDATE',
                'link_url' => $decisionPdfUrl,
            ]);
        }

        // 6. Ghi Audit Log bất biến
        AuditLog::create([
            'table_name'   => 'salary_proposals',
            'record_id'    => $proposal->id,
            'action'       => 'FINAL_APPROVAL_SALARY',
            'performed_by' => $approverUser->id,
            'new_values'   => [
                'decision_number'    => $decisionNumber,
                'effective_from'     => $effectiveDate->toDateString(),
                'proposed_p1'        => $proposal->proposed_p1,
                'retroactive_months' => $retroactiveMonths,
                'retroactive_amount' => (string)$totalRetroactiveAmount,
            ],
        ]);
    }

    /**
     * Xác thực người lập chỉ được tạo đề xuất cho nhân sự thuộc cây tổ chức nhánh mình.
     */
    protected function validateSubtreeScope(User $creatorUser, Employee $employee): void
    {
        if (!$creatorUser->employee_id) return; // Super admin

        $creator = Employee::with('currentAssignment.orgUnit')->find($creatorUser->employee_id);
        $creatorPath = $creator?->currentAssignment?->orgUnit?->path;
        $employeePath = $employee->currentAssignment?->orgUnit?->path;

        if ($creatorPath && $employeePath) {
            if (!str_starts_with($employeePath, $creatorPath)) {
                throw new DomainException("Phạm vi dữ liệu không hợp lệ: Bạn chỉ có quyền lập đề xuất cho nhân viên thuộc nhánh tổ chức của mình.");
            }
        }
    }
}
