<?php
/**
 * =====================================================================
 * VINAMILK HRM - CRONJOB SNAPSHOT FACT HEADCOUNT MONTHLY
 * =====================================================================
 * Tần suất chạy: 00:05 ngày 1 hằng tháng (Crontab: 5 0 1 * *)
 * Cách chạy thủ công qua terminal:
 *   php cron/snapshot_headcount.php
 *   php cron/snapshot_headcount.php 2026-10
 *   php cron/snapshot_headcount.php --seed-history
 */

define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/services/HeadcountSnapshotService.php';

echo "======================================================================\n";
echo "VINAMILK ENTERPRISE HRM - BẮT ĐẦU CHẠY JOB CRON SNAPSHOT NHÂN SỰ THÁNG\n";
echo "Thời gian hệ thống: " . date('Y-m-d H:i:s') . "\n";
echo "======================================================================\n";

$service = new HeadcountSnapshotService();

// Kiểm tra tham số dòng lệnh
$arg = $argv[1] ?? null;

if ($arg === '--seed-history') {
    echo "[CRON] Chế độ Seed lịch sử 12 tháng gần nhất (2025-11 đến 2026-10)...\n";
    $count = $service->seedHistoricalSnapshots(12);
    echo "[CRON SUCCESS] Đã tạo thành công snapshot cho $count kỳ tháng gần nhất!\n";
    exit(0);
}

$kyLuong = !empty($arg) ? $arg : date('Y-m', strtotime('first day of last month'));

echo "[CRON] Tiến hành chốt snapshot kỳ tháng: [{$kyLuong}]...\n";
$res = $service->takeSnapshot($kyLuong, true);

echo "[CRON SUCCESS] Đã chốt snapshot kỳ [{$res['period']}] tại ngày [{$res['snapshot']}]:\n";
echo "  -> Tổng số đơn vị xử lý: {$res['total_units']}\n";
echo "  -> Hoàn tất lưu dữ liệu vào fact_headcount_monthly & cập nhật view v_fact_headcount_summary!\n";
echo "======================================================================\n";
exit(0);
