<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();

// 1. Cập nhật bảng menu_items với cấu trúc menu chuẩn MVC
$db->exec("
    TRUNCATE TABLE menu_items;
    INSERT INTO menu_items (MenuID, Title, Route, Icon, ParentID, OrderIndex, RequiredPermission, IsVisible) VALUES
    (1, 'Bảng Điều Khiển', 'dashboard', 'bi-speedometer2', NULL, 1, '', 1),
    (2, 'Cơ cấu & Cây Đơn vị', 'donvi', 'bi-diagram-3-fill', NULL, 2, '', 1),
    (3, 'Quản Lý Nhân Sự', '', 'bi-people-fill', NULL, 3, 'NHANSU', 1),
    (4, 'Hồ sơ Nhân viên', 'employees', 'bi-person-badge', 3, 4, 'NHANSU', 1),
    (5, 'Hợp đồng Lao động', 'employees', 'bi-file-earmark-text', 3, 5, 'NHANSU', 1),
    (6, 'Hộp Thư Phê Duyệt', 'approvals', 'bi-envelope-paper-fill', NULL, 6, '', 1),
    (7, 'Tiền Lương & Đãi Ngộ', '', 'bi-cash-coin', NULL, 7, 'LUONG', 1),
    (8, 'Bảng lương 3P', 'payroll', 'bi-wallet2', 7, 8, 'LUONG', 1),
    (9, 'Tham số Pháp lý', 'payroll/legal_params', 'bi-shield-check', 7, 9, 'LUONG', 1),
    (10, 'Hệ Thống & Kiểm Toán', '', 'bi-gear-fill', NULL, 10, 'HE_THONG', 1),
    (11, 'Nhật ký Kiểm toán', 'audit', 'bi-journal-text', 10, 11, 'HE_THONG', 1),
    (12, 'UI Kitchen Sink', 'kitchen-sink', 'bi-palette-fill', NULL, 12, '', 1);
");

// 2. Cập nhật role_permissions cho 7 vai trò
// Lấy danh sách PermissionID theo ModuleCode
$perms = $db->query("SELECT PermissionID, ModuleCode, ActionCode FROM permissions")->fetchAll(PDO::FETCH_ASSOC);
$permMap = [];
foreach ($perms as $p) {
    $permMap[$p['ModuleCode']][] = $p['PermissionID'];
}

// Xóa role_permissions cũ và nạp lại chuẩn
$db->exec("DELETE FROM role_permissions WHERE RoleID BETWEEN 1 AND 7");

// Role 1: ADMIN (Tất cả quyền)
$allPermIds = array_column($perms, 'PermissionID');
$stmt = $db->prepare("INSERT INTO role_permissions (RoleID, PermissionID) VALUES (1, ?)");
foreach ($allPermIds as $pid) {
    $stmt->execute([$pid]);
}

// Role 2: BOD (Tất cả quyền xem / View)
$stmt = $db->prepare("INSERT INTO role_permissions (RoleID, PermissionID) VALUES (2, ?)");
foreach ($perms as $p) {
    if (in_array($p['ActionCode'], ['View', 'Export', 'Approve'])) {
        $stmt->execute([$p['PermissionID']]);
    }
}

// Role 3: HR_TAPDOAN (NHANSU, TUYEN_DUNG, DAO_TAO, CHAM_CONG, LUONG, BAO_CAO)
$hrTapDoanModules = ['NHANSU', 'TUYEN_DUNG', 'DAO_TAO', 'CHAM_CONG', 'LUONG', 'BAO_CAO'];
$stmt = $db->prepare("INSERT INTO role_permissions (RoleID, PermissionID) VALUES (3, ?)");
foreach ($perms as $p) {
    if (in_array($p['ModuleCode'], $hrTapDoanModules)) {
        $stmt->execute([$p['PermissionID']]);
    }
}

// Role 4: HR_DONVI (NHANSU, CHAM_CONG, DAO_TAO, TUYEN_DUNG)
$hrDonViModules = ['NHANSU', 'CHAM_CONG', 'DAO_TAO', 'TUYEN_DUNG'];
$stmt = $db->prepare("INSERT INTO role_permissions (RoleID, PermissionID) VALUES (4, ?)");
foreach ($perms as $p) {
    if (in_array($p['ModuleCode'], $hrDonViModules)) {
        $stmt->execute([$p['PermissionID']]);
    }
}

// Role 5: KETOAN_LUONG (LUONG, CHAM_CONG, BAO_CAO)
$keToanModules = ['LUONG', 'CHAM_CONG', 'BAO_CAO'];
$stmt = $db->prepare("INSERT INTO role_permissions (RoleID, PermissionID) VALUES (5, ?)");
foreach ($perms as $p) {
    if (in_array($p['ModuleCode'], $keToanModules)) {
        $stmt->execute([$p['PermissionID']]);
    }
}

// Role 6: QUANLY_TRUCTIEP (NHANSU view, CHAM_CONG)
$quanLyModules = ['NHANSU', 'CHAM_CONG'];
$stmt = $db->prepare("INSERT INTO role_permissions (RoleID, PermissionID) VALUES (6, ?)");
foreach ($perms as $p) {
    if (in_array($p['ModuleCode'], $quanLyModules)) {
        $stmt->execute([$p['PermissionID']]);
    }
}

// Role 7: NHANVIEN (CHAM_CONG view, LUONG view)
$nhanVienPerms = $db->query("SELECT PermissionID FROM permissions WHERE ActionCode = 'View' AND ModuleCode IN ('CHAM_CONG', 'LUONG')")->fetchAll(PDO::FETCH_COLUMN);
$stmt = $db->prepare("INSERT INTO role_permissions (RoleID, PermissionID) VALUES (7, ?)");
foreach ($nhanVienPerms as $pid) {
    $stmt->execute([$pid]);
}

echo "Successfully synchronized menu_items and role_permissions for all 7 roles!\n";
