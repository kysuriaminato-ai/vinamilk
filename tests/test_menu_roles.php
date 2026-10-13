<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));
require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();

function getMenuForRole(PDO $db, int $roleId, string $roleCode): array {
    if (strtoupper($roleCode) === 'ADMIN') {
        $stmt = $db->query("SELECT * FROM menu_items WHERE IsVisible = 1 ORDER BY OrderIndex ASC");
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    $sql = "SELECT DISTINCT m.*
            FROM menu_items m
            WHERE m.IsVisible = 1
              AND (
                m.RequiredPermission = '' OR m.RequiredPermission IS NULL
                OR EXISTS (
                    SELECT 1 
                    FROM role_permissions rp 
                    JOIN permissions p ON rp.PermissionID = p.PermissionID 
                    WHERE rp.RoleID = :role_id 
                      AND p.ModuleCode = m.RequiredPermission
                )
              )
            ORDER BY m.OrderIndex ASC";

    $stmt = $db->prepare($sql);
    $stmt->execute([':role_id' => $roleId]);
    return $stmt->fetchAll(PDO::FETCH_ASSOC);
}

for ($rid = 1; $rid <= 7; $rid++) {
    $roleRow = $db->query("SELECT RoleName, RoleCode FROM roles WHERE RoleID = $rid")->fetch(PDO::FETCH_ASSOC);
    $menus = getMenuForRole($db, $rid, $roleRow['RoleCode']);
    echo "=== ROLE $rid: {$roleRow['RoleName']} ({$roleRow['RoleCode']}) ===\n";
    foreach ($menus as $m) {
        $prefix = $m['ParentID'] ? '  └── ' : '• ';
        echo "{$prefix}{$m['Title']} (Route: {$m['Route']}, Req: {$m['RequiredPermission']})\n";
    }
    echo "\n";
}
