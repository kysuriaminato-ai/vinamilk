<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/models/UserModel.php';

$userModel = new UserModel();
$accounts = [
    'admin'        => 'ADMIN',
    'bod.ceo'      => 'BOD',
    'hr.tapdoan'   => 'HR_TAPDOAN',
    'hr.nhamay'    => 'HR_DONVI',
    'ketoan.luong' => 'KETOAN_LUONG',
    'quanly.sx'    => 'QUANLY_TRUCTIEP',
    'nhanvien.ess' => 'NHANVIEN'
];

$password = 'Vinamilk@2026';
echo "========================================================\n";
echo "XÁC THỰC 7 TÀI KHOẢN DEMO VỚI MẬT KHẨU Vinamilk@2026\n";
echo "========================================================\n";

foreach ($accounts as $username => $expectedRole) {
    $user = $userModel->authenticate($username, $password);
    if ($user && $user['RoleCode'] === $expectedRole) {
        echo " [OK] {$username} ({$user['RoleCode']}) - MaNV: {$user['MaNV']} - MaDV: {$user['MaDV']}\n";
    } else {
        echo " [FAIL] {$username} - Auth failed!\n";
    }
}
