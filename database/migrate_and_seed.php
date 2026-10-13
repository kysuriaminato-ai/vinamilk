<?php
/**
 * VINAMILK HRM - DATABASE MIGRATION & SEED RUNNER
 * Tự động kết nối MySQL, tạo database (nếu chưa có), nạp SQL Schema và chạy seed 300 nhân sự.
 */

declare(strict_types=1);

require_once __DIR__ . '/../app/config/config.php';
require_once __DIR__ . '/../app/core/Database.php';

echo "\n================================================================================\n";
echo "   VINAMILK HRIS - DATABASE MIGRATIONS & SEED RUNNER\n";
echo "================================================================================\n";

$host = DB_HOST;
$port = DB_PORT;
$dbName = DB_NAME;
$user = DB_USER;
$pass = DB_PASS;

echo "Kết nối MySQL {$host}:{$port} với database: {$dbName}...\n";

try {
    // 1. Tạo database nếu chưa tồn tại
    $pdoRoot = new PDO("mysql:host={$host};port={$port};charset=utf8mb4", $user, $pass, [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION
    ]);
    $pdoRoot->exec("CREATE DATABASE IF NOT EXISTS `{$dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
    echo "-> Đảm bảo cơ sở dữ liệu `{$dbName}` tồn tại: THÀNH CÔNG!\n";

    // 2. Kết nối trực tiếp vào database
    $pdo = new PDO("mysql:host={$host};port={$port};dbname={$dbName};charset=utf8mb4", $user, $pass, [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION
    ]);
    $pdo->exec("SET FOREIGN_KEY_CHECKS = 0;");

    // 3. Import các file SQL nền tảng
    $sqlFiles = [
        __DIR__ . '/vinamilk_hrm.sql',
        __DIR__ . '/migrations/2026_10_12_vinamilk_hrm_full_migration.sql'
    ];

    foreach ($sqlFiles as $file) {
        if (!file_exists($file)) continue;
        $fileName = basename($file);
        echo "Đang import SQL: {$fileName} ...\n";
        $content = file_get_contents($file);
        // Loại bỏ lệnh USE / CREATE DATABASE nếu có để tránh xung đột
        $pdo->exec($content);
        echo "-> Đã nạp thành công: {$fileName}\n";
    }

    $pdo->exec("SET FOREIGN_KEY_CHECKS = 1;");
    echo "-> Hoàn tất import toàn bộ SQL schemas!\n\n";

    // 4. Chạy seeder 300 nhân viên & 24 tháng chấm công/lương
    echo "Bắt đầu chạy Seeder 300 nhân sự & 24 tháng lương/công...\n";
    require __DIR__ . '/seeders/seed_300_employees.php';

} catch (Throwable $e) {
    echo "\n(!) Lỗi khi chạy migration & seed: " . $e->getMessage() . "\n";
    exit(1);
}
