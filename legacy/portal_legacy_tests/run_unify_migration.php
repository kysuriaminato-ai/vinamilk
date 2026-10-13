<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

try {
    $db = Database::getInstance()->getConnection();
    $sql = file_get_contents(ROOT_DIR . '/database/migrations/001_unify.sql');
    $db->exec($sql);
    echo "SUCCESS: Migration 001_unify.sql executed successfully!\n";
    
    // Check tables in database
    $stmt = $db->query("SHOW TABLES");
    $tables = $stmt->fetchAll(PDO::FETCH_COLUMN);
    echo "Total tables in vinamilk_hrm: " . count($tables) . "\n";
    echo "Tables: " . implode(', ', $tables) . "\n";
} catch (Exception $e) {
    echo "FAILED: " . $e->getMessage() . "\n";
}
