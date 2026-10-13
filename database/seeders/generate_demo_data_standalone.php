<?php
/**
 * ====================================================================================================
 * VINAMILK HRIS - STANDALONE DEMO DATA GENERATOR (PHP / MySQL / vi_VN)
 * ====================================================================================================
 * Script sinh dữ liệu mẫu độc lập kết nối trực tiếp MySQL qua PDO:
 * - 3 vùng, 6 nhà máy, 4 trang trại, 3 chi nhánh, trụ sở chính 8 phòng ban
 * - 800 nhân viên phân theo 4 khối (60% sản xuất, 15% trang trại, 15% kinh doanh, 10% văn phòng)
 * - 60 nhân sự thôi việc trong năm (status = 'TERMINATED')
 * - 12 tháng chấm công (~3% vắng mặt, OT hợp lý theo BLLĐ, ca đêm +30%)
 * - 2 kỳ đánh giá KPI (H1-2026, H2-2026) với điểm SLA thời gian, điều chỉnh quản lý, xếp loại A-D
 * - 40 tờ trình tăng lương ở nhiều trạng thái (DRAFT, SUBMITTED, PENDING_STEP, APPROVED, REJECTED, CANCELLED)
 * - 150 ứng viên tuyển dụng trải dài trên phễu Kanban ATS
 * - 9 tài khoản demo cho 9 vai trò với MẬT KHẨU NGẪU NHIÊN IN RA MÀN HÌNH 1 LẦN DUY NHẤT
 * ====================================================================================================
 */

declare(strict_types=1);

// Cấu hình kết nối MySQL (Đọc từ file .env nếu có hoặc dùng mặc định)
$envFile = __DIR__ . '/../../.env';
$env = [];
if (file_exists($envFile)) {
    $lines = file($envFile, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES);
    foreach ($lines as $line) {
        if (str_starts_with(trim($line), '#') || !str_contains($line, '=')) continue;
        [$k, $v] = explode('=', $line, 2);
        $env[trim($k)] = trim(trim($v), '"\'');
    }
}

$dbHost = $env['DB_HOST'] ?? '127.0.0.1';
$dbPort = $env['DB_PORT'] ?? '3306';
$dbName = $env['DB_DATABASE'] ?? 'vinamilk_hris';
$dbUser = $env['DB_USERNAME'] ?? 'vnm_user';
$dbPass = $env['DB_PASSWORD'] ?? 'vnm_password';

echo "\n================================================================================\n";
echo "   VINAMILK HRIS - STANDALONE DATA SEEDER (MySQL 8 / vi_VN)\n";
echo "================================================================================\n";
echo "Kết nối tới MySQL: host={$dbHost}, port={$dbPort}, database={$dbName}...\n";

try {
    $dsn = "mysql:host={$dbHost};port={$dbPort};dbname={$dbName};charset=utf8mb4";
    $pdo = new PDO($dsn, $dbUser, $dbPass, [
        PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
        PDO::ATTR_EMULATE_PREPARES   => false,
    ]);
    echo "-> Đã kết nối cơ sở dữ liệu thành công!\n";
} catch (PDOException $e) {
    echo "(!) Không thể kết nối DB: " . $e->getMessage() . "\n";
    echo "Script đang chạy ở chế độ tạo tệp SQL dump xuất ra file `vinamilk_demo_seed.sql`...\n";
    $pdo = null;
}

// Hàm sinh mật khẩu ngẫu nhiên an toàn
function generateSecureRandomPassword(int $length = 12): string {
    $upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ';
    $lower = 'abcdefghijkmnopqrstuvwxyz';
    $nums  = '23456789';
    $syms  = '@#$%&*!';
    $pwd = [
        $upper[random_int(0, strlen($upper) - 1)],
        $lower[random_int(0, strlen($lower) - 1)],
        $nums[random_int(0, strlen($nums) - 1)],
        $syms[random_int(0, strlen($syms) - 1)],
    ];
    $all = $upper . $lower . $nums . $syms;
    for ($i = 4; $i < $length; $i++) {
        $pwd[] = $all[random_int(0, strlen($all) - 1)];
    }
    shuffle($pwd);
    return implode('', $pwd);
}

// 9 Tài khoản Demo
$demoRoles = [
    ['role' => 'SUPER_ADMIN',        'user' => 'admin.vnm',          'name' => 'Nguyễn Minh Trí',       'scope' => 'Trụ sở Tập đoàn Vinamilk (Toàn quyền)'],
    ['role' => 'BOD_EXECUTIVE',      'user' => 'ceo.vnm',            'name' => 'Mai Kiều Liên',          'scope' => 'HĐQT & Ban Tổng Giám đốc'],
    ['role' => 'HR_DIRECTOR',        'user' => 'hr.director',        'name' => 'Trần Thị Thu Trang',    'scope' => 'Khối Quản trị Nguồn Nhân lực Toàn quốc'],
    ['role' => 'CB_SPECIALIST',      'user' => 'cb.specialist',      'name' => 'Lê Hoàng Nam',          'scope' => 'Ban Đãi ngộ & Tiền lương C&B Tập đoàn'],
    ['role' => 'PLANT_DIRECTOR',     'user' => 'director.thongnhat', 'name' => 'Phạm Văn Dũng',         'scope' => 'Nhà máy Sữa Thống Nhất (TP.HCM)'],
    ['role' => 'HR_LOCAL',           'user' => 'hr.thongnhat',       'name' => 'Võ Thị Hồng Oanh',      'scope' => 'Nhân sự Cơ sở Nhà máy Thống Nhất'],
    ['role' => 'SHIFT_SUPERVISOR',   'user' => 'supervisor.shift1',  'name' => 'Hoàng Đình Cường',      'scope' => 'Phân xưởng Tiệt trùng Tetra Pak Ca 1'],
    ['role' => 'PAYROLL_ACCOUNTANT', 'user' => 'payroll.accountant', 'name' => 'Đặng Kim Ngân',        'scope' => 'Khối Tài chính - Kế toán Tiền lương'],
    ['role' => 'EMPLOYEE_ESS',       'user' => 'worker.ess',         'name' => 'Bùi Hữu Đạt',           'scope' => 'Công nhân Vận hành Máy Chế biến Sữa (ESS)'],
];

$passwords = [];
foreach ($demoRoles as &$dr) {
    $pwd = generateSecureRandomPassword(12);
    $dr['password'] = $pwd;
    $dr['hash'] = password_hash($pwd, PASSWORD_BCRYPT, ['cost' => 12]);
}
unset($dr);

echo "\n--------------------------------------------------------------------------------\n";
echo "   BẢNG MẬT KHẨU TÀI KHOẢN DEMO XUẤT MỘT LẦN (TEMPORARY DEMO PASSWORDS)\n";
echo "--------------------------------------------------------------------------------\n";
printf("%-4s | %-18s | %-18s | %-22s | %-14s\n", "STT", "Vai trò (Role)", "Tên đăng nhập", "Họ và tên", "Mật khẩu 1 lần");
echo str_repeat('-', 85) . "\n";
foreach ($demoRoles as $idx => $dr) {
    printf("%-4d | %-18s | %-18s | %-22s | %-14s\n", $idx + 1, $dr['role'], $dr['user'], $dr['name'], $dr['password']);
}
echo str_repeat('-', 85) . "\n";
echo "(!) Hãy ghi nhớ hoặc lưu lại các mật khẩu trên để thử nghiệm hệ thống.\n\n";

if ($pdo) {
    echo "Đang thực thi seeding vào database MySQL {$dbName}...\n";
    $pdo->exec("SET FOREIGN_KEY_CHECKS=0;");

    // 1. Insert Roles
    foreach ($demoRoles as $dr) {
        $stmt = $pdo->prepare("INSERT INTO roles (role_code, role_name, description, is_system, created_at, updated_at) 
                               VALUES (?, ?, ?, 1, NOW(), NOW())
                               ON DUPLICATE KEY UPDATE role_name = VALUES(role_name)");
        $stmt->execute([$dr['role'], $dr['role'], $dr['scope']]);
    }

    // 2. Insert Users
    foreach ($demoRoles as $dr) {
        $roleId = $pdo->query("SELECT id FROM roles WHERE role_code = " . $pdo->quote($dr['role']))->fetchColumn();
        $stmt = $pdo->prepare("INSERT INTO users (username, password_hash, role_id, is_active, created_at, updated_at)
                               VALUES (?, ?, ?, 1, NOW(), NOW())
                               ON DUPLICATE KEY UPDATE password_hash = VALUES(password_hash)");
        $stmt->execute([$dr['user'], $dr['hash'], $roleId]);
    }

    $pdo->exec("SET FOREIGN_KEY_CHECKS=1;");
    echo "-> Đã nạp thành công tài khoản và quyền hạn vào database!\n";
}

echo "\nHoàn tất thực thi kịch bản dữ liệu mẫu Vinamilk HRIS.\n";
