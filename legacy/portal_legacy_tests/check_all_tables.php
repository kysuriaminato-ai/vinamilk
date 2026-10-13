<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();
$tables = $db->query("SHOW TABLES")->fetchAll(PDO::FETCH_COLUMN);
foreach ($tables as $t) {
    $cols = $db->query("SHOW COLUMNS FROM `$t`")->fetchAll(PDO::FETCH_COLUMN);
    echo "$t: " . implode(', ', $cols) . "\n";
}
