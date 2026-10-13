<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();
try {
    $stmt = $db->query('SHOW CREATE TABLE dm_khoa_dao_tao');
    print_r($stmt->fetch(PDO::FETCH_ASSOC));
} catch (Exception $e) {
    echo "dm_khoa_dao_tao error: " . $e->getMessage() . "\n";
}
