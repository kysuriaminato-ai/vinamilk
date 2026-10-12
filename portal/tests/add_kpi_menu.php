<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();

$exists = $pdo->query("SELECT COUNT(*) FROM menu_items WHERE Route = 'kpi-dictionary'")->fetchColumn();
if (!$exists) {
    $sql = "INSERT INTO menu_items (Title, Route, Icon, ParentID, OrderIndex, RequiredPermission, IsVisible, CreatedAt)
            VALUES ('Từ Điển Chỉ Số KPI', 'kpi-dictionary', 'bi-book-half', NULL, 15, NULL, 1, NOW())";
    $pdo->exec($sql);
    echo "Added kpi-dictionary to menu_items!\n";
} else {
    echo "kpi-dictionary already in menu_items!\n";
}
