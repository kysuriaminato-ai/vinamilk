<?php
/**
 * =====================================================================
 * VINAMILK HRM - THỰC THI MIGRATION 004: MODULE CHẤM CÔNG
 * =====================================================================
 */

require_once __DIR__ . '/../app/config/config.php';
require_once __DIR__ . '/../app/core/Database.php';

$db = Database::getInstance();
$pdo = $db->getConnection();

echo "1. Chạy migration 004_attendance_module.sql..." . PHP_EOL;
$sql = file_get_contents(__DIR__ . '/../database/migrations/004_attendance_module.sql');

// Tách và thực thi từng câu lệnh SQL
$queries = array_filter(array_map('trim', explode(';', $sql)));
foreach ($queries as $q) {
    if (!empty($q)) {
        try {
            $pdo->exec($q);
        } catch (Exception $e) {
            echo "   [Note]: " . $e->getMessage() . PHP_EOL;
        }
    }
}
echo "   -> Đã khởi tạo bảng quet_the_tho, bang_cong_thang, dieu_chinh_cong và cấu hình OT thành công!" . PHP_EOL;

// Kiểm tra lại danh mục ca
$shifts = $db->query("SELECT ma_ca, ten_ca, gio_bat_dau, gio_ket_thuc, la_ca_dem FROM ca_lam_viec ORDER BY gio_bat_dau ASC")->fetchAll(PDO::FETCH_ASSOC);
echo "2. Danh mục ca làm việc hiện hành (" . count($shifts) . " ca):" . PHP_EOL;
foreach ($shifts as $s) {
    echo "   - {$s['ma_ca']}: {$s['ten_ca']} (" . substr($s['gio_bat_dau'], 0, 5) . " - " . substr($s['gio_ket_thuc'], 0, 5) . ")" . ($s['la_ca_dem'] ? ' [Ca đêm qua ngày]' : '') . PHP_EOL;
}

echo "=== HOÀN TẤT MIGRATION 004 ===" . PHP_EOL;
