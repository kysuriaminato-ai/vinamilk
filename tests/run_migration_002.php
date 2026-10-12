<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));
require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();

echo "1. Checking and updating `users` table columns...\n";
$userCols = $db->query("DESCRIBE users")->fetchAll(PDO::FETCH_COLUMN);

if (!in_array('LockoutUntil', $userCols)) {
    $db->exec("ALTER TABLE users ADD COLUMN LockoutUntil DATETIME NULL DEFAULT NULL AFTER LoginAttempts");
    echo "  + Added `LockoutUntil` column\n";
} else {
    echo "  = `LockoutUntil` column already exists\n";
}

if (!in_array('MustChangePassword', $userCols)) {
    $db->exec("ALTER TABLE users ADD COLUMN MustChangePassword TINYINT(1) NOT NULL DEFAULT 0 AFTER LockoutUntil");
    echo "  + Added `MustChangePassword` column\n";
} else {
    echo "  = `MustChangePassword` column already exists\n";
}

echo "2. Checking and updating `permissions` table columns...\n";
$permCols = $db->query("DESCRIBE permissions")->fetchAll(PDO::FETCH_COLUMN);

if (!in_array('PermissionCode', $permCols)) {
    $db->exec("ALTER TABLE permissions ADD COLUMN PermissionCode VARCHAR(100) NULL AFTER PermissionID, ADD UNIQUE KEY uq_perm_code (PermissionCode)");
    echo "  + Added `PermissionCode` column\n";
} else {
    echo "  = `PermissionCode` column already exists\n";
}

echo "3. Populating standardized `PermissionCode` (module.action)...\n";
// Cập nhật các quyền hiện có
$perms = $db->query("SELECT PermissionID, ModuleCode, ActionCode FROM permissions")->fetchAll(PDO::FETCH_ASSOC);
$stmtUpdate = $db->prepare("UPDATE permissions SET PermissionCode = :code WHERE PermissionID = :id");

foreach ($perms as $p) {
    $m = strtolower(str_replace('_', '', $p['ModuleCode']));
    if ($p['ModuleCode'] === 'TUYEN_DUNG') $m = 'tuyendung';
    if ($p['ModuleCode'] === 'CHAM_CONG')  $m = 'chamcong';
    if ($p['ModuleCode'] === 'DAO_TAO')    $m = 'daotao';
    if ($p['ModuleCode'] === 'BAO_CAO')    $m = 'baocao';
    if ($p['ModuleCode'] === 'HE_THONG')   $m = 'hethong';

    $act = strtolower($p['ActionCode']);
    if ($act === 'add') $act = 'create';
    $code = $m . '.' . $act;

    $stmtUpdate->execute([':code' => $code, ':id' => $p['PermissionID']]);
}

// Bổ sung các quyền nhạy cảm *.sensitive nếu chưa có
$sensitivePerms = [
    [
        'code' => 'nhansu.sensitive',
        'module' => 'NHANSU',
        'module_name' => 'Quản lý Nhân sự',
        'act' => 'Sensitive',
        'desc' => 'Xem dữ liệu nhạy cảm nhân sự (CCCD, Số tài khoản ngân hàng)'
    ],
    [
        'code' => 'luong.sensitive',
        'module' => 'LUONG',
        'module_name' => 'Bảng lương',
        'act' => 'Sensitive',
        'desc' => 'Xem chi tiết mức lương, các khoản phụ cấp và tài khoản nhận lương'
    ]
];

$stmtInsert = $db->prepare("INSERT INTO permissions (PermissionCode, ModuleCode, ModuleName, ActionCode, Description) 
                            VALUES (:code, :module, :module_name, :act, :desc)
                            ON DUPLICATE KEY UPDATE Description = VALUES(Description)");

foreach ($sensitivePerms as $sp) {
    $stmtInsert->execute([
        ':code' => $sp['code'],
        ':module' => $sp['module'],
        ':module_name' => $sp['module_name'],
        ':act' => $sp['act'],
        ':desc' => $sp['desc']
    ]);
    echo "  + Ensured sensitive permission: {$sp['code']}\n";
}

echo "4. Synchronizing `role_permissions` for all roles...\n";
// Lấy toàn bộ permission map code -> id
$allPerms = $db->query("SELECT PermissionCode, PermissionID FROM permissions")->fetchAll(PDO::FETCH_KEY_PAIR);

// Role 1: ADMIN (Toàn bộ quyền bao gồm *.sensitive)
$db->exec("DELETE FROM role_permissions WHERE RoleID = 1");
$stmtRP = $db->prepare("INSERT INTO role_permissions (RoleID, PermissionID) VALUES (?, ?)");
foreach ($allPerms as $code => $pid) {
    $stmtRP->execute([1, $pid]);
}

// Role 2: BOD (Xem, Báo cáo, Phê duyệt, Xem lương nhạy cảm)
$db->exec("DELETE FROM role_permissions WHERE RoleID = 2");
$bodPerms = ['nhansu.view', 'nhansu.export', 'chamcong.view', 'chamcong.export', 'luong.view', 'luong.export', 'luong.approve', 'luong.sensitive', 'baocao.view', 'baocao.export', 'tuyendung.view', 'daotao.view'];
foreach ($bodPerms as $code) {
    if (isset($allPerms[$code])) $stmtRP->execute([2, $allPerms[$code]]);
}

// Role 3: HR_TAPDOAN (Toàn bộ nhân sự, chấm công, tuyển dụng, đào tạo, báo cáo, nhansu.sensitive, luong.sensitive)
$db->exec("DELETE FROM role_permissions WHERE RoleID = 3");
$hrTapDoanPerms = [
    'nhansu.view', 'nhansu.create', 'nhansu.edit', 'nhansu.delete', 'nhansu.export', 'nhansu.sensitive',
    'chamcong.view', 'chamcong.create', 'chamcong.edit', 'chamcong.approve', 'chamcong.export',
    'luong.view', 'luong.export', 'luong.sensitive',
    'tuyendung.view', 'tuyendung.create', 'tuyendung.edit', 'tuyendung.delete', 'tuyendung.approve',
    'daotao.view', 'daotao.create', 'daotao.edit',
    'baocao.view', 'baocao.export'
];
foreach ($hrTapDoanPerms as $code) {
    if (isset($allPerms[$code])) $stmtRP->execute([3, $allPerms[$code]]);
}

// Role 4: HR_DONVI (Chỉ phân hệ nhân sự, chấm công, tuyển dụng, đào tạo trong đơn vị mình. KHÔNG CÓ *.sensitive)
$db->exec("DELETE FROM role_permissions WHERE RoleID = 4");
$hrDonViPerms = [
    'nhansu.view', 'nhansu.create', 'nhansu.edit', 'nhansu.export',
    'chamcong.view', 'chamcong.create', 'chamcong.edit', 'chamcong.approve',
    'tuyendung.view', 'tuyendung.create', 'tuyendung.edit',
    'daotao.view', 'daotao.create',
    'baocao.view'
];
foreach ($hrDonViPerms as $code) {
    if (isset($allPerms[$code])) $stmtRP->execute([4, $allPerms[$code]]);
}

// Role 5: KETOAN_LUONG (Toàn bộ phân hệ lương, tính lương, duyệt chi, xem dữ liệu nhạy cảm lương, chấm công)
$db->exec("DELETE FROM role_permissions WHERE RoleID = 5");
$keToanPerms = [
    'luong.view', 'luong.create', 'luong.edit', 'luong.approve', 'luong.export', 'luong.sensitive',
    'nhansu.view',
    'chamcong.view', 'chamcong.export',
    'baocao.view', 'baocao.export'
];
foreach ($keToanPerms as $code) {
    if (isset($allPerms[$code])) $stmtRP->execute([5, $allPerms[$code]]);
}

// Role 6: QUANLY_TRUCTIEP (Chỉ xem và duyệt cấp dưới trực tiếp, KHÔNG có *.sensitive)
$db->exec("DELETE FROM role_permissions WHERE RoleID = 6");
$quanLyPerms = [
    'nhansu.view',
    'chamcong.view', 'chamcong.approve',
    'baocao.view'
];
foreach ($quanLyPerms as $code) {
    if (isset($allPerms[$code])) $stmtRP->execute([6, $allPerms[$code]]);
}

// Role 7: NHANVIEN (Chỉ xem dữ liệu cá nhân của mình)
$db->exec("DELETE FROM role_permissions WHERE RoleID = 7");
$nhanVienPerms = [
    'chamcong.view',
    'luong.view'
];
foreach ($nhanVienPerms as $code) {
    if (isset($allPerms[$code])) $stmtRP->execute([7, $allPerms[$code]]);
}

echo "5. Ensuring Tiên Sơn and Bình Dương demo accounts...\n";
// Kiểm tra nhân sự Tiên Sơn
$checkTSNV = $db->query("SELECT MaNV FROM nhansu WHERE MaNV = 'VNM-TS01'")->fetch();
if (!$checkTSNV) {
    $db->exec("INSERT INTO nhansu (MaNV, HoTen, NgaySinh, GioiTinh, SoCMND_CCCD, Email, SoDienThoai, NgayVaoLam, MaDV, ChucVu, SoTaiKhoanNH, NganHang, LuongBaoHiem, TrangThai)
               VALUES ('VNM-TS01', 'Nguyễn Văn Tiên', '1988-05-12', 'Nam', '027088001122', 'tien.nv@vinamilk.com.vn', '0912345678', '2021-03-01', 'NM_TS', 'Chuyên viên Nhân sự', '1903456789012', 'Techcombank', 16500000, 1)");
    echo "  + Inserted sample employee VNM-TS01 for Nhà máy Tiên Sơn\n";
}

// Kiểm tra user hr.tienson
$checkUserTS = $db->query("SELECT ID FROM users WHERE Username = 'hr.tienson'")->fetch();
if (!$checkUserTS) {
    $passHash = password_hash('Vinamilk@2026', PASSWORD_BCRYPT);
    $db->exec("INSERT INTO users (Username, Password, MaNV, MaDV, RoleID, RoleCode, IsActive, LoginAttempts, MustChangePassword)
               VALUES ('hr.tienson', '$passHash', 'VNM-TS01', 'NM_TS', 4, 'HR_DONVI', 1, 0, 0)");
    echo "  + Created user `hr.tienson` (Role: HR_DONVI, MaDV: NM_TS)\n";
} else {
    echo "  = User `hr.tienson` already exists\n";
}

// Đảm bảo hr.nhamay ở Nhà máy Bình Dương
$checkUserBD = $db->query("SELECT ID FROM users WHERE Username = 'hr.nhamay'")->fetch();
if ($checkUserBD) {
    $db->exec("UPDATE users SET MaDV = 'NM_BD', RoleID = 4, RoleCode = 'HR_DONVI' WHERE Username = 'hr.nhamay'");
    echo "  = Updated user `hr.nhamay` to MaDV: NM_BD\n";
}

echo "\nMigration 002 completed successfully!\n";
