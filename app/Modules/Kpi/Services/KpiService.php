<?php

namespace App\Modules\Kpi\Services;

use App\Models\AuditLog;
use App\Models\Notification;
use App\Models\User;
use App\Modules\Employee\Models\Employee;
use App\Modules\Kpi\Models\KpiAssignment;
use App\Modules\Kpi\Models\KpiEmployeeSummary;
use App\Modules\Kpi\Models\KpiPeriod;
use App\Modules\Kpi\Models\KpiScoringConfig;
use Carbon\Carbon;
use DomainException;
use Illuminate\Support\Collection;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\DB;

class KpiService
{
    /**
     * Lãnh đạo giao nhiệm vụ KPI cho nhân viên.
     * Ràng buộc:
     * - Trọng số từ 1% đến 100%.
     * - Tổng trọng số các nhiệm vụ của 1 nhân viên trong 1 kỳ không được vượt quá 100%.
     * - Người giao (Evaluator) phải khác người thực hiện (SoD).
     */
    public function createTask(array $data, User $evaluatorUser): KpiAssignment
    {
        return DB::transaction(function () use ($data, $evaluatorUser) {
            $period = KpiPeriod::findOrFail($data['kpi_period_id']);

            if (in_array($period->status, ['LOCKED', 'CLOSED'])) {
                throw new DomainException("Kỳ đánh giá KPI [{$period->period_name}] đã đóng hoặc khóa, không thể giao thêm nhiệm vụ.");
            }

            $employeeId = (int)$data['employee_id'];
            $evaluatorId = $evaluatorUser->employee_id ?? (int)($data['evaluator_id'] ?? 0);

            // NGUYÊN TẮC SOD: Lãnh đạo không được tự giao việc và tự chấm điểm cho chính mình
            if ($evaluatorId > 0 && $evaluatorId === $employeeId) {
                throw new DomainException("Nguyên tắc kiểm soát nội bộ (SoD): Quản lý không được tự giao chỉ tiêu và đánh giá KPI của chính mình.");
            }

            $weight = (float)$data['weight_percentage'];
            if ($weight <= 0 || $weight > 100) {
                throw new DomainException("Trọng số nhiệm vụ phải nằm trong khoảng từ 1% đến 100%.");
            }

            // Kiểm tra tổng trọng số hiện tại của nhân viên trong kỳ
            $currentTotalWeight = KpiAssignment::where('kpi_period_id', $period->id)
                ->where('employee_id', $employeeId)
                ->sum('weight_percentage');

            if (($currentTotalWeight + $weight) > 100.0) {
                $availableWeight = 100.0 - $currentTotalWeight;
                throw new DomainException("Tổng trọng số nhiệm vụ trong kỳ sẽ vượt quá 100% (Hiện có: {$currentTotalWeight}%, Còn lại: {$availableWeight}%, Đăng ký: {$weight}%).");
            }

            $baselineScore = KpiScoringConfig::getValue('BASELINE_SCORE', 100.00);

            $task = KpiAssignment::create([
                'kpi_period_id'      => $period->id,
                'employee_id'        => $employeeId,
                'evaluator_id'       => $evaluatorId,
                'title'              => trim($data['title']),
                'description'        => trim($data['description'] ?? ''),
                'target_metric'      => trim($data['target_metric']),
                'weight_percentage'  => $weight,
                'due_date'           => Carbon::parse($data['due_date'])->toDateString(),
                'baseline_score'     => (float)($data['baseline_score'] ?? $baselineScore),
                'time_score'         => 0.00,
                'manager_adjustment' => 0.00,
                'status'             => 'Assigned',
            ]);

            // Gửi Notification thông báo cho nhân viên
            $employee = Employee::with('user')->find($employeeId);
            if ($employee?->user) {
                Notification::create([
                    'user_id'  => $employee->user->id,
                    'title'    => "Nhiệm vụ KPI mới: {$task->title}",
                    'content'  => "Bạn được giao nhiệm vụ KPI [{$task->title}] (Trọng số: {$task->weight_percentage}%, Hạn chót: {$task->due_date->format('d/m/Y')}).",
                    'type'     => 'TASK_ASSIGNED',
                    'link_url' => "/kpi/my-tasks/{$task->id}",
                ]);
            }

            return $task;
        });
    }

    /**
     * Nhân viên bắt đầu thực hiện nhiệm vụ (Assigned -> InProgress).
     */
    public function startTask(int $taskId, User $user): KpiAssignment
    {
        $task = KpiAssignment::findOrFail($taskId);

        if ($user->employee_id && $user->employee_id !== $task->employee_id) {
            throw new DomainException("Bạn không có quyền thao tác trên nhiệm vụ của nhân sự khác.");
        }

        if ($task->status === 'Assigned') {
            $task->update(['status' => 'InProgress']);
        }

        return $task;
    }

    /**
     * Nhân viên nộp kết quả thực hiện nhiệm vụ (InProgress/Rejected -> PendingApproval).
     * Động cơ tự động tính điểm thời gian SLA (Time Score):
     * - Hoàn thành sớm: +2 điểm/ngày, tối đa +10 điểm.
     * - Hoàn thành trễ: -5 điểm/ngày.
     */
    public function submitTask(int $taskId, string $actualResult, ?Carbon $completedDate = null, ?User $user = null): KpiAssignment
    {
        return DB::transaction(function () use ($taskId, $actualResult, $completedDate, $user) {
            $task = KpiAssignment::with(['evaluator.user', 'employee'])->findOrFail($taskId);

            if ($user && $user->employee_id && $user->employee_id !== $task->employee_id) {
                throw new DomainException("Bạn không phải người được giao nhiệm vụ này.");
            }

            // Cho phép nộp từ Assigned, InProgress, hoặc Rejected (nộp lại sau khi bị trả về)
            if (!in_array($task->status, ['Assigned', 'InProgress', 'Rejected'])) {
                throw new DomainException("Không thể nộp kết quả khi nhiệm vụ đang ở trạng thái '{$task->status}'.");
            }

            if (empty(trim($actualResult))) {
                throw new DomainException("Báo cáo kết quả thực hiện không được để trống.");
            }

            $completed = ($completedDate ?? now())->startOfDay();
            $dueDate = Carbon::parse($task->due_date)->startOfDay();

            // Đọc cấu hình từ bảng kpi_scoring_configs
            $earlyBonusPerDay = KpiScoringConfig::getValue('EARLY_BONUS_PER_DAY', 2.0);
            $maxEarlyBonus = KpiScoringConfig::getValue('MAX_EARLY_BONUS', 10.0);
            $latePenaltyPerDay = KpiScoringConfig::getValue('LATE_PENALTY_PER_DAY', 5.0);

            $timeScore = 0.0;
            if ($completed->lessThan($dueDate)) {
                // Hoàn thành sớm hạn
                $earlyDays = $completed->diffInDays($dueDate);
                $calculatedBonus = $earlyDays * $earlyBonusPerDay;
                $timeScore = min($maxEarlyBonus, $calculatedBonus);
            } elseif ($completed->greaterThan($dueDate)) {
                // Hoàn thành trễ hạn
                $lateDays = $dueDate->diffInDays($completed);
                $timeScore = -($lateDays * $latePenaltyPerDay);
            }

            $task->update([
                'actual_result'   => trim($actualResult),
                'completed_date'  => $completed->toDateString(),
                'submission_date' => now(),
                'time_score'      => $timeScore,
                'status'          => 'PendingApproval',
            ]);

            // Gửi thông báo đến Lãnh đạo thẩm định
            if ($task->evaluator?->user) {
                Notification::create([
                    'user_id'  => $task->evaluator->user->id,
                    'title'    => "Báo cáo kết quả KPI cần duyệt từ {$task->employee->full_name}",
                    'content'  => "Nhân viên {$task->employee->full_name} đã nộp kết quả nhiệm vụ [{$task->title}]. Điểm thời gian tạm tính: " . ($timeScore >= 0 ? "+{$timeScore}" : "{$timeScore}"),
                    'type'     => 'APPROVAL_REQUEST',
                    'link_url' => "/kpi/kanban?task_id={$task->id}",
                ]);
            }

            return $task;
        });
    }

    /**
     * Lãnh đạo thẩm định, áp dụng điều chỉnh điểm và phê duyệt chính thức (Approved).
     * Ràng buộc:
     * - Người duyệt phải khác người thực hiện (SoD).
     * - Điều chỉnh vượt +/- 20 điểm bắt buộc phải nhập lý do (adjustment_reason).
     * - Điểm chốt = clamp(baseline_score + time_score + manager_adjustment, 0, 120).
     */
    public function approveTask(
        int $taskId,
        User $evaluatorUser,
        float $managerAdjustment = 0.0,
        ?string $adjustmentReason = null,
        ?string $feedback = null
    ): KpiAssignment {
        return DB::transaction(function () use ($taskId, $evaluatorUser, $managerAdjustment, $adjustmentReason, $feedback) {
            $task = KpiAssignment::with('employee.user')->findOrFail($taskId);

            if ($task->status !== 'PendingApproval') {
                throw new DomainException("Nhiệm vụ chưa được nhân viên nộp kết quả hoặc đã được chốt (Trạng thái: {$task->status}).");
            }

            // KIỂM SOÁT NỘI BỘ (SOD): Người duyệt phải khác người thực hiện
            if ($evaluatorUser->employee_id && $evaluatorUser->employee_id === $task->employee_id) {
                throw new DomainException("Nguyên tắc SoD: Bạn không thể tự thẩm định và phê duyệt nhiệm vụ KPI của chính mình.");
            }

            // BẮT BUỘC NHẬP LÝ DO NẾU ĐIỀU CHỈNH VƯỢT +/- 20 ĐIỂM
            $threshold = KpiScoringConfig::getValue('REASON_REQUIRED_THRESHOLD', 20.0);
            if (abs($managerAdjustment) > $threshold && empty(trim($adjustmentReason ?? ''))) {
                throw new DomainException("Điều chỉnh lãnh đạo vượt mức ±{$threshold} điểm bắt buộc phải nhập lý do thẩm định cụ thể.");
            }

            // Đọc cấu hình sàn và trần điểm nhiệm vụ (0 đến 120 điểm)
            $minScore = KpiScoringConfig::getValue('MIN_TASK_SCORE', 0.0);
            $maxScore = KpiScoringConfig::getValue('MAX_TASK_SCORE', 120.0);

            $rawScore = $task->baseline_score + $task->time_score + $managerAdjustment;
            $finalScore = max($minScore, min($maxScore, $rawScore));

            $task->update([
                'manager_adjustment' => $managerAdjustment,
                'adjustment_reason'  => $adjustmentReason ? trim($adjustmentReason) : null,
                'final_score'        => $finalScore,
                'feedback'           => $feedback ? trim($feedback) : "Lãnh đạo đã duyệt điểm nhiệm vụ.",
                'evaluation_date'    => now(),
                'status'             => 'Approved',
            ]);

            // Tự động tính toán lại điểm tổng kết KPI kỳ cho nhân viên
            $this->recalculatePeriodKpiScore($task->kpi_period_id, $task->employee_id);

            // Gửi thông báo đến nhân viên
            if ($task->employee?->user) {
                Notification::create([
                    'user_id'  => $task->employee->user->id,
                    'title'    => "Nhiệm vụ KPI đã được phê duyệt",
                    'content'  => "Nhiệm vụ [{$task->title}] đã được phê duyệt với điểm số chính thức: {$finalScore}/100.",
                    'type'     => 'STATUS_UPDATE',
                    'link_url' => "/kpi/my-tasks/{$task->id}",
                ]);
            }

            return $task;
        });
    }

    /**
     * Lãnh đạo từ chối báo cáo kết quả và trả lại yêu cầu nhân viên làm lại (Rejected).
     */
    public function rejectTask(int $taskId, User $evaluatorUser, string $rejectionReason): KpiAssignment
    {
        return DB::transaction(function () use ($taskId, $evaluatorUser, $rejectionReason) {
            $task = KpiAssignment::with('employee.user')->findOrFail($taskId);

            if ($task->status !== 'PendingApproval') {
                throw new DomainException("Nhiệm vụ không ở trạng thái chờ duyệt.");
            }

            if ($evaluatorUser->employee_id && $evaluatorUser->employee_id === $task->employee_id) {
                throw new DomainException("Nguyên tắc SoD: Không thể thao tác trên nhiệm vụ của chính mình.");
            }

            if (empty(trim($rejectionReason))) {
                throw new DomainException("Bắt buộc phải nhập lý do trả lại kết quả để nhân viên khắc phục.");
            }

            $task->update([
                'status'           => 'Rejected',
                'rejection_reason' => trim($rejectionReason),
                'evaluation_date'  => now(),
            ]);

            if ($task->employee?->user) {
                Notification::create([
                    'user_id'  => $task->employee->user->id,
                    'title'    => "Nhiệm vụ KPI bị trả lại yêu cầu bổ sung",
                    'content'  => "Nhiệm vụ [{$task->title}] bị trả lại. Lý do: {$rejectionReason}. Vui lòng hoàn thiện và nộp lại.",
                    'type'     => 'STATUS_UPDATE',
                    'link_url' => "/kpi/my-tasks/{$task->id}",
                ]);
            }

            return $task;
        });
    }

    /**
     * Đóng nhiệm vụ (Closed) sau khi kỳ kết thúc.
     */
    public function closeTask(int $taskId): KpiAssignment
    {
        $task = KpiAssignment::findOrFail($taskId);
        if ($task->status === 'Approved') {
            $task->update(['status' => 'Closed']);
        }
        return $task;
    }

    /**
     * Tự động chuyển các nhiệm vụ chưa nộp cuối kỳ sang NotCompleted với điểm = 0.
     */
    public function autoCloseUncompletedTasks(int $kpiPeriodId): int
    {
        $uncompletedTasks = KpiAssignment::where('kpi_period_id', $kpiPeriodId)
            ->whereIn('status', ['Assigned', 'InProgress', 'Rejected', 'PendingApproval'])
            ->get();

        $count = 0;
        foreach ($uncompletedTasks as $task) {
            $task->update([
                'status'      => 'NotCompleted',
                'final_score' => 0.00,
                'feedback'    => 'Nhiệm vụ không hoàn thành trước khi kỳ đánh giá KPI kết thúc.',
            ]);
            $this->recalculatePeriodKpiScore($kpiPeriodId, $task->employee_id);
            $count++;
        }

        return $count;
    }

    /**
     * Tính toán Tổng điểm KPI kỳ của một nhân viên:
     * Điểm KPI kỳ = Σ(Điểm nhiệm vụ × Trọng số %).
     * - Chỉ các nhiệm vụ Approved mới tính điểm.
     * - Nhiệm vụ NotCompleted tính 0 điểm.
     * - Quy đổi hệ số Lương P3 (A+ = 1.25, A = 1.15, B = 1.00, C = 0.85, D = 0.00).
     */
    public function recalculatePeriodKpiScore(int $kpiPeriodId, int $employeeId): KpiEmployeeSummary
    {
        $tasks = KpiAssignment::where('kpi_period_id', $kpiPeriodId)
            ->where('employee_id', $employeeId)
            ->get();

        $totalWeight = (float)$tasks->sum('weight_percentage');
        $periodScore = 0.0;

        foreach ($tasks as $task) {
            $weightRatio = $task->weight_percentage / 100.0;
            if ($task->status === 'Approved' || $task->status === 'Closed') {
                $score = (float)($task->final_score ?? 0.0);
                $periodScore += ($score * $weightRatio);
            } elseif ($task->status === 'NotCompleted') {
                $periodScore += (0.0 * $weightRatio);
            }
        }

        $periodScore = round($periodScore, 2);

        // Quy đổi xếp loại và hệ số P3
        [$grade, $p3Multiplier] = match (true) {
            $periodScore >= 110.0 => ['A+', 1.25],
            $periodScore >= 100.0 => ['A', 1.15],
            $periodScore >= 90.0  => ['B', 1.00],
            $periodScore >= 75.0  => ['C', 0.85],
            default               => ['D', 0.00],
        };

        return KpiEmployeeSummary::updateOrCreate(
            [
                'kpi_period_id' => $kpiPeriodId,
                'employee_id'   => $employeeId,
            ],
            [
                'total_weight_percentage' => $totalWeight,
                'kpi_score'               => $periodScore,
                'kpi_grade'               => $grade,
                'p3_multiplier'           => $p3Multiplier,
                'status'                  => 'DRAFT',
            ]
        );
    }

    /**
     * Xuất dữ liệu điểm KPI đã duyệt sang phân hệ Payroll (Tính lương hiệu suất P3).
     */
    public function exportPeriodKpiToPayroll(int $kpiPeriodId, User $exporterUser): array
    {
        return DB::transaction(function () use ($kpiPeriodId, $exporterUser) {
            $period = KpiPeriod::findOrFail($kpiPeriodId);

            $summaries = KpiEmployeeSummary::where('kpi_period_id', $kpiPeriodId)->get();
            $exportedCount = 0;

            foreach ($summaries as $summary) {
                $summary->update([
                    'status'      => 'EXPORTED_TO_PAYROLL',
                    'exported_at' => now(),
                    'exported_by' => $exporterUser->employee_id ?? 1,
                ]);
                $exportedCount++;
            }

            AuditLog::create([
                'table_name'   => 'kpi_employee_summaries',
                'record_id'    => $kpiPeriodId,
                'action'       => 'UPDATE',
                'performed_by' => $exporterUser->id,
                'new_values'   => [
                    'event'          => 'EXPORT_KPI_TO_PAYROLL',
                    'period_code'    => $period->period_code,
                    'exported_count' => $exportedCount,
                ],
            ]);

            return [
                'period_code'    => $period->period_code,
                'exported_count' => $exportedCount,
            ];
        });
    }

    /**
     * Quét và gửi thông báo nhắc hạn nhiệm vụ khi còn 3 ngày làm việc.
     */
    public function sendDueReminderNotifications(): int
    {
        $targetDate = now()->addDays(3)->toDateString();

        $urgentTasks = KpiAssignment::with('employee.user')
            ->whereIn('status', ['Assigned', 'InProgress'])
            ->whereDate('due_date', $targetDate)
            ->get();

        $sentCount = 0;
        foreach ($urgentTasks as $task) {
            if ($task->employee?->user) {
                Notification::create([
                    'user_id'  => $task->employee->user->id,
                    'title'    => "NHẮC HẠN KPI: Nhiệm vụ [{$task->title}] còn 3 ngày",
                    'content'  => "Nhiệm vụ KPI [{$task->title}] sẽ đến hạn vào ngày {$task->due_date->format('d/m/Y')}. Vui lòng khẩn trương hoàn tất và nộp báo cáo kết quả.",
                    'type'     => 'DEADLINE_REMINDER',
                    'link_url' => "/kpi/my-tasks/{$task->id}",
                ]);
                $sentCount++;
            }
        }

        return $sentCount;
    }
}
