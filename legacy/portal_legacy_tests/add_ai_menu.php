<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();
$exists = $pdo->query("SELECT COUNT(*) FROM menu_items WHERE Route = 'ai-expert'")->fetchColumn();
if (!$exists) {
    $sql = "INSERT INTO menu_items (Title, Route, Icon, ParentID, OrderIndex, RequiredPermission, IsVisible, CreatedAt)
            VALUES ('AI Dự Báo Rủi Ro & Định Biên', 'ai-expert', 'bi-cpu-fill', NULL, 16, NULL, 1, NOW())";
    $pdo->exec($sql);
    echo "Added ai-expert to menu_items!\n";
} else {
    echo "ai-expert already in menu_items!\n";
}
