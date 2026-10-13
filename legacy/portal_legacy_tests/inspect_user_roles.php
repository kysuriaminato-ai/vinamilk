<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();
$users = $db->query("SELECT ID, Username, RoleID, RoleCode FROM users WHERE Username IN ('admin', 'bod.ceo', 'hr.tapdoan', 'hr.nhamay', 'ketoan.luong', 'quanly.sx', 'nhanvien.ess')")->fetchAll(PDO::FETCH_ASSOC);
print_r($users);

echo "Permissions by module:\n";
$perms = $db->query("SELECT ModuleCode, COUNT(*) as cnt FROM permissions GROUP BY ModuleCode")->fetchAll(PDO::FETCH_ASSOC);
print_r($perms);

echo "Role_permissions mapping by role:\n";
$rp = $db->query("SELECT r.RoleID, r.RoleCode, p.ModuleCode, COUNT(*) as cnt FROM role_permissions rp JOIN roles r ON rp.RoleID = r.RoleID JOIN permissions p ON rp.PermissionID = p.PermissionID GROUP BY r.RoleID, r.RoleCode, p.ModuleCode")->fetchAll(PDO::FETCH_ASSOC);
print_r($rp);
