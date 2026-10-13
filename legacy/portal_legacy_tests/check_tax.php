<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();
$rows = $db->query('SELECT * FROM bieu_thue_tncn')->fetchAll(PDO::FETCH_ASSOC);
echo "Count: " . count($rows) . "\n";
foreach ($rows as $r) {
    echo "ID: {$r['id']} | Bac: {$r['bac_thue']} | Tu: {$r['thu_nhap_tu']} | Den: {$r['thu_nhap_den']} | Suat: {$r['thue_suat']}\n";
}
