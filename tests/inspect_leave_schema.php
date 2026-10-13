<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();

echo "=== DESCRIBE phan_ca ===\n";
$cols = $pdo->query("DESCRIBE phan_ca")->fetchAll(PDO::FETCH_ASSOC);
foreach ($cols as $c) {
    echo "   - {$c['Field']} ({$c['Type']})\n";
}

echo "\n=== DESCRIBE ca_lam_viec ===\n";
$cols = $pdo->query("DESCRIBE ca_lam_viec")->fetchAll(PDO::FETCH_ASSOC);
foreach ($cols as $c) {
    echo "   - {$c['Field']} ({$c['Type']})\n";
}

echo "\n=== SAMPLE ca_lam_viec ===\n";
$shifts = $pdo->query("SELECT * FROM ca_lam_viec")->fetchAll(PDO::FETCH_ASSOC);
print_r($shifts);
