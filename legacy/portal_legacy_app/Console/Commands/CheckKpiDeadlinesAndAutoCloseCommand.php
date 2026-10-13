<?php

namespace App\Console\Commands;

use App\Modules\Kpi\Models\KpiPeriod;
use App\Modules\Kpi\Services\KpiService;
use Illuminate\Console\Command;

class CheckKpiDeadlinesAndAutoCloseCommand extends Command
{
    protected $signature = 'vinamilk:kpi-deadline-check {--auto-close : Tự động chuyển NotCompleted các nhiệm vụ của kỳ đã hết hạn}';

    protected $description = 'Nhắc hạn nhiệm vụ KPI còn 3 ngày và tự động chuyển NotCompleted cho nhiệm vụ chưa nộp khi hết kỳ';

    public function handle(KpiService $kpiService): int
    {
        $this->info("Bắt đầu tiến trình kiểm tra hạn KPI...");

        // 1. Quét và gửi thông báo nhắc hạn còn 3 ngày
        $reminders = $kpiService->sendDueReminderNotifications();
        $this->info("Đã gửi {$reminders} thông báo nhắc hạn hoàn thành nhiệm vụ KPI (còn 3 ngày).");

        // 2. Tự động đóng các kỳ đã qua ngày kết thúc
        if ($this->option('autoClose')) {
            $today = now()->toDateString();
            $expiredPeriods = KpiPeriod::where('status', 'OPEN')
                ->where('end_date', '<', $today)
                ->get();

            foreach ($expiredPeriods as $period) {
                $closedCount = $kpiService->autoCloseUncompletedTasks($period->id);
                $period->update(['status' => 'EVALUATING']);
                $this->info("Kỳ [{$period->period_code}] đã hết hạn: Đã chuyển {$closedCount} nhiệm vụ chưa nộp sang NotCompleted (0 điểm).");
            }
        }

        $this->info("Hoàn tất tiến trình KPI!");
        return Command::SUCCESS;
    }
}
