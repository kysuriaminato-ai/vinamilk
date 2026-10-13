<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();
$cnt = $pdo->query("SELECT COUNT(*) FROM fact_headcount_monthly")->fetchColumn();
echo "Total rows in fact_headcount_monthly: $cnt\n";

$periods = $pdo->query("SELECT DISTINCT ky_nam_thang FROM fact_headcount_monthly ORDER BY ky_nam_thang ASC")->fetchAll(PDO::FETCH_COLUMN);
echo "Periods: " . implode(', ', $periods) . "\n";

$sample = $pdo->query("SELECT * FROM v_fact_headcount_summary WHERE ky_nam_thang = '2026-10' LIMIT 5")->fetchAll(PDO::FETCH_ASSOC);
print_r($sample);
