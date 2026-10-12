<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();
$exists = $pdo->query("SHOW TABLES LIKE 'ai_risk_score'")->rowCount() > 0;
echo "ai_risk_score exists: " . ($exists ? "YES" : "NO") . "\n";
