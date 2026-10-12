<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();

// 1. Hợp đồng sắp hết hạn
$expiringContracts = $db->query("
    SELECT hd.*, ns.HoTen, ns.MaDV 
    FROM hop_dong_lao_dong hd 
    JOIN nhansu ns ON hd.ma_nv = ns.MaNV 
    WHERE hd.trang_thai = 'HIEU_LUC' 
      AND hd.den_ngay IS NOT NULL 
      AND hd.den_ngay <= DATE_ADD(CURDATE(), INTERVAL 90 DAY)
    ORDER BY hd.den_ngay ASC LIMIT 5
")->fetchAll(PDO::FETCH_ASSOC);

// 2. Đơn chờ duyệt
$pendingRequests = $db->query("
    SELECT dt.*, ns.HoTen 
    FROM don_tu dt 
    JOIN nhansu ns ON dt.ma_nv = ns.MaNV 
    WHERE dt.trang_thai = 'CHO_DUYET' 
    ORDER BY dt.created_at DESC LIMIT 5
")->fetchAll(PDO::FETCH_ASSOC);

// 3. Chứng chỉ sắp hết hạn
$expiringCerts = $db->query("
    SELECT cc.*, ns.HoTen 
    FROM chung_chi_nhan_vien cc 
    JOIN nhansu ns ON cc.ma_nv = ns.MaNV 
    WHERE cc.ngay_het_han IS NOT NULL 
      AND cc.ngay_het_han <= DATE_ADD(CURDATE(), INTERVAL 90 DAY)
    ORDER BY cc.ngay_het_han ASC LIMIT 5
")->fetchAll(PDO::FETCH_ASSOC);

echo "Contracts expiring: " . count($expiringContracts) . "\n";
echo "Pending requests: " . count($pendingRequests) . "\n";
echo "Expiring certificates: " . count($expiringCerts) . "\n";
