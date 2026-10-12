<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();
$sql = file_get_contents(__DIR__ . '/../database/migrations/005_payroll_3p_service.sql');

// Tách các câu lệnh theo dấu chấm phẩy
$queries = explode(';', $sql);
$successCount = 0;

foreach ($queries as $q) {
    $q = trim($q);
    if (!empty($q)) {
        try {
            $pdo->exec($q);
            $successCount++;
        } catch (PDOException $e) {
            echo "Error executing query: " . substr($q, 0, 50) . "...\n";
            echo "Message: " . $e->getMessage() . "\n";
        }
    }
}

echo "Migration 005 executed successfully! ({$successCount} statements executed)\n";
