<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();
$sql = file_get_contents(__DIR__ . '/../database/migrations/007_ai_risk_score.sql');

$queries = explode(';', $sql);
$success = 0;

foreach ($queries as $q) {
    $q = trim($q);
    if (!empty($q)) {
        try {
            $pdo->exec($q);
            $success++;
        } catch (PDOException $e) {
            echo "Error: " . $e->getMessage() . "\n";
        }
    }
}

echo "Migration 007 completed successfully ($success queries executed)!\n";
