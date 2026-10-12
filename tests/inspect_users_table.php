<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();
$stmt = $pdo->query("DESCRIBE users");
while ($r = $stmt->fetch(PDO::FETCH_ASSOC)) {
    echo sprintf("%-20s %-20s %s\n", $r['Field'], $r['Type'], $r['Null']);
}
