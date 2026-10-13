<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();

function showFullCols($pdo, $table) {
    echo "=== COLUMNS FOR: $table ===\n";
    $stmt = $pdo->query("SHOW FULL COLUMNS FROM $table");
    while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
        echo sprintf("%-22s %-40s %s\n", $row['Field'], $row['Type'], $row['Comment'] ?? '');
    }
    echo "\n";
}

showFullCols($pdo, 'dm_donvi');
showFullCols($pdo, 'so_du_phep');
