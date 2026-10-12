<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();
$sql = file_get_contents(__DIR__ . '/../database/migrations/006_fact_headcount_dashboard.sql');

$queries = explode(';', $sql);
$success = 0;

foreach ($queries as $q) {
    $q = trim($q);
    if (!empty($q)) {
        try {
            $pdo->exec($q);
            $success++;
        } catch (PDOException $e) {
            echo "Error executing query: " . substr($q, 0, 60) . "...\n";
            echo "Message: " . $e->getMessage() . "\n";
        }
    }
}

echo "Migration 006 completed successfully with $success statements executed!\n";
