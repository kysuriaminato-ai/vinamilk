<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();
$sql = file_get_contents(__DIR__ . '/../database/migrations/008_certificates_ppe_offboarding.sql');

$queries = explode(';', $sql);
$success = 0;
$errors = 0;

foreach ($queries as $q) {
    $q = trim($q);
    if (!empty($q)) {
        try {
            $pdo->exec($q);
            $success++;
        } catch (PDOException $e) {
            echo "Error executing query: " . $e->getMessage() . "\n";
            $errors++;
        }
    }
}

echo "Migration 008 completed: $success successful, $errors errors.\n";
