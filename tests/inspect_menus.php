<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();
echo "--- MENU ITEMS ---\n";
$menus = $db->query("SELECT * FROM menu_items ORDER BY OrderIndex")->fetchAll(PDO::FETCH_ASSOC);
foreach ($menus as $m) {
    echo "ID: {$m['MenuID']} | Title: {$m['Title']} | Route: {$m['Route']} | ParentID: {$m['ParentID']} | ReqPerm: {$m['RequiredPermission']} | Visible: {$m['IsVisible']}\n";
}

echo "\n--- PERMISSIONS SAMPLE ---\n";
$perms = $db->query("SELECT * FROM permissions LIMIT 10")->fetchAll(PDO::FETCH_ASSOC);
foreach ($perms as $p) {
    echo "PermID: {$p['PermissionID']} | Mod: {$p['ModuleCode']} | Act: {$p['ActionCode']} | Desc: {$p['Description']}\n";
}

echo "\n--- ROLES ---\n";
$roles = $db->query("SELECT * FROM roles")->fetchAll(PDO::FETCH_ASSOC);
foreach ($roles as $r) {
    $rpCount = $db->query("SELECT COUNT(*) FROM role_permissions WHERE RoleID = " . (int)$r['RoleID'])->fetchColumn();
    echo "RoleID: {$r['RoleID']} | Code: {$r['RoleCode']} | Name: {$r['RoleName']} | Perms count: {$rpCount}\n";
}
