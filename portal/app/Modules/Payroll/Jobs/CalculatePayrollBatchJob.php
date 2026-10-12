<?php

namespace App\Modules\Payroll\Jobs;

use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Payroll\Models\PayrollPeriod;
use App\Modules\Payroll\Services\PayrollEngineService;
use Illuminate\Bus\Queueable;
use Illuminate\Contracts\Queue\ShouldQueue;
use Illuminate\Foundation\Bus\Dispatchable;
use Illuminate\Queue\InteractsWithQueue;
use Illuminate\Queue\SerializesModels;
use Illuminate\Support\Facades\Cache;
use Illuminate\Support\Facades\Log;

class CalculatePayrollBatchJob implements ShouldQueue
{
    use Dispatchable, InteractsWithQueue, Queueable, SerializesModels;

    public int $timeout = 3600; // 1 giờ cho lô lớn

    public function __construct(
        public int $periodId,
        public ?int $orgUnitId = null,
        public ?int $employeeId = null,
        public int $chunkSize = 1000
    ) {}

    public function handle(PayrollEngineService $payrollService): void
    {
        $period = PayrollPeriod::findOrFail($this->periodId);

        if (!$period->canRecalculate()) {
            Log::warning("Bỏ qua Job tính lương: Kỳ lương {$period->period_code} đã bị khóa hoặc đã chi.");
            return;
        }

        // Trường hợp 1: Chạy lại cho 1 nhân viên duy nhất
        if ($this->employeeId) {
            $employee = Employee::findOrFail($this->employeeId);
            $payrollService->recalculateEmployee($period, $employee);
            return;
        }

        // Trường hợp 2: Chạy lại cho 1 đơn vị tổ chức
        if ($this->orgUnitId) {
            $orgUnit = OrgUnit::findOrFail($this->orgUnitId);
            $payrollService->recalculateOrgUnit($period, $orgUnit, true);
            return;
        }

        // Trường hợp 3: Chạy toàn bộ tập đoàn theo lô 1.000 người có thanh tiến độ
        $payrollService->calculatePeriod($period, function ($processed, $total, $percent) use ($period) {
            Cache::put("payroll_batch_progress_{$period->id}", [
                'period_code' => $period->period_code,
                'total'       => $total,
                'processed'   => $processed,
                'percent'     => $percent,
                'status'      => 'PROCESSING',
                'updated_at'  => now()->toIso8601String(),
            ], 3600);
        });

        Cache::put("payroll_batch_progress_{$period->id}", [
            'period_code' => $period->period_code,
            'total'       => Employee::active()->count(),
            'processed'   => Employee::active()->count(),
            'percent'     => 100,
            'status'      => 'COMPLETED',
            'updated_at'  => now()->toIso8601String(),
        ], 3600);
    }
}
