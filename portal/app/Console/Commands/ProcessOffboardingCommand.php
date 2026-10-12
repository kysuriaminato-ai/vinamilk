<?php

namespace App\Console\Commands;

use App\Models\Notification;
use App\Modules\Leave\Models\FinalSettlement;
use App\Modules\Leave\Services\OffboardingService;
use Carbon\Carbon;
use Illuminate\Console\Command;

class ProcessOffboardingCommand extends Command
{
    /**
     * Tên lệnh Artisan để đăng ký trong Scheduler
     */
    protected $signature = 'vinamilk:process-offboarding';

    /**
     * Mô tả lệnh
     */
    protected $description = 'Tự động khóa tài khoản sau ngày làm việc cuối và kiểm tra cảnh báo hạn thanh toán quyết toán thôi việc 14 ngày làm việc';

    public function handle(OffboardingService $offboardingService): int
    {
        $this->info("Bắt đầu tiến trình xử lý thôi việc tự động...");

        // 1. Tự động khóa tài khoản ESS của nhân viên sau ngày làm việc cuối cùng
        $deactivated = $offboardingService->deactivateResignedAccounts();
        $this->info("Đã vô hiệu hóa tài khoản và cập nhật trạng thái RESIGNED cho {$deactivated} nhân sự.");

        // 2. Rà soát hạn thanh toán kỳ lương quyết toán 14 ngày làm việc (Điều 48 BLLĐ 2019)
        $today = now()->startOfDay();
        $pendingSettlements = FinalSettlement::with('employee')
            ->whereIn('payment_status', ['PENDING', 'APPROVED'])
            ->get();

        $alertsSent = 0;
        foreach ($pendingSettlements as $stl) {
            $deadline = Carbon::parse($stl->payment_deadline_date)->startOfDay();
            $daysRemaining = $today->diffInDays($deadline, false);

            if ($daysRemaining < 0) {
                // Quá hạn thanh toán pháp định
                $stl->update(['payment_status' => 'OVERDUE']);

                Notification::create([
                    'user_id'  => 1, // Kế toán trưởng / Admin
                    'title'    => "CẢNH BÁO QUÁ HẠN: Quyết toán thôi việc {$stl->employee->full_name}",
                    'content'  => "Hồ sơ quyết toán {$stl->settlement_code} của nhân viên {$stl->employee->full_name} đã QUÁ HẠN thanh toán luật định (Hạn chót: {$deadline->format('d/m/Y')}). Căn cứ Điều 48 BLLĐ 2019, đề nghị phòng Kế toán chi trả gấp.",
                    'type'     => 'SYSTEM_ALERT',
                    'link_url' => "/payroll/settlements/{$stl->id}",
                ]);
                $alertsSent++;
            } elseif ($daysRemaining <= 3) {
                // Sắp đến hạn trong vòng 3 ngày
                Notification::create([
                    'user_id'  => 1, // Kế toán trưởng
                    'title'    => "NHẮC HẠN THANH TOÁN: Quyết toán thôi việc {$stl->employee->full_name}",
                    'content'  => "Hồ sơ quyết toán {$stl->settlement_code} của nhân viên {$stl->employee->full_name} còn {$daysRemaining} ngày làm việc trước khi hết hạn (Hạn chót: {$deadline->format('d/m/Y')}).",
                    'type'     => 'SYSTEM_ALERT',
                    'link_url' => "/payroll/settlements/{$stl->id}",
                ]);
                $alertsSent++;
            }
        }

        $this->info("Đã gửi {$alertsSent} cảnh báo thời hạn thanh toán.");
        $this->info("Hoàn tất tiến trình offboarding!");

        return Command::SUCCESS;
    }
}
