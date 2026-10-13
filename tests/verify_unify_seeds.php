<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();

$tablesToCheck = [
    'hop_dong_lao_dong',
    'ca_lam_viec',
    'phan_ca',
    'cham_cong_ngay',
    'don_nghi_phep',
    'de_xuat_tuyen_dung',
    'dm_khoa_dao_tao',
    'chung_chi_nhan_vien',
    'cap_phat_tai_san',
    'dinh_bien',
    'cau_hinh_luong',
    'audit_log',
    'su_kien_nhan_su',
    'don_tu',
    'bang_luong',
    'bang_luong_chi_tiet',
    'bieu_thue_tncn'
];

echo "========================================================\n";
echo "KIỂM TRA DỮ LIỆU SEED CÁC BẢNG UNIFIED TRONG MYSQL 8\n";
echo "========================================================\n";

foreach ($tablesToCheck as $tbl) {
    $count = (int)$db->query("SELECT COUNT(*) FROM `$tbl`")->fetchColumn();
    echo sprintf("%-25s : %d bản ghi\n", $tbl, $count);
}
