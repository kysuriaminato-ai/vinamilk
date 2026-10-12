<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));
require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';

$db = Database::getInstance()->getConnection();

// Đảm bảo có hợp đồng sắp hết hạn trong 30 ngày (tính từ 2026-10-12)
$db->exec("
    INSERT INTO hop_dong_lao_dong (ma_nv, so_hd, loai_hd, tu_ngay, den_ngay, luong_can_ban, trang_thai)
    VALUES 
    ('VNM-0004', 'HDLD/2024/VNM-HR04-GH', 'XAC_DINH_THOI_HAN', '2024-11-01', '2026-10-28', 18500000, 'HIEU_LUC'),
    ('VNM-0002', 'HDLD/2025/VNM-SX02-TN', 'THU_VIEC', '2026-08-10', '2026-11-05', 12000000, 'HIEU_LUC')
    ON DUPLICATE KEY UPDATE den_ngay = VALUES(den_ngay), trang_thai = 'HIEU_LUC';
");

// Đảm bảo có chứng chỉ sắp hết hạn trong 45 ngày (tính từ 2026-10-12)
$db->exec("
    INSERT INTO chung_chi_nhan_vien (ma_nv, loai_chung_chi, ten_chung_chi, so_hieu_chung_chi, noi_cap, ngay_cap, ngay_het_han, trang_thai, canh_bao_truoc_ngay)
    VALUES 
    ('VNM-0002', 'THE_XANH_ATTP', 'Thẻ Xanh An Toàn Vệ Sinh Thực Phẩm Sữa Tươi Tiệt Trùng', 'TX-VNM-2025-089', 'Chi cục ATVSTP Bình Dương', '2025-11-05', '2026-11-02', 'CON_HIEU_LUC', 30),
    ('VNM-0004', 'ATLD_VE_SINH_LD', 'Chứng nhận ATLĐ & Vệ sinh môi trường Nhà máy VNM', 'ATLĐ-2024-041', 'Trung tâm ATLĐ Miền Nam', '2024-11-15', '2026-11-18', 'CON_HIEU_LUC', 30)
    ON DUPLICATE KEY UPDATE ngay_het_han = VALUES(ngay_het_han), trang_thai = 'CON_HIEU_LUC';
");

// Đảm bảo có thêm đơn từ chờ duyệt
$db->exec("
    INSERT INTO don_tu (ma_don, ma_nv, loai_don, tieu_de, noi_dung, tu_ngay, den_ngay, so_ngay, trang_thai, created_at)
    VALUES 
    ('REQ-20261012-002', 'VNM-0004', 'DE_XUAT_TANG_LUONG', 'Đề xuất nâng bậc lương kỹ thuật viên quý 4', 'Đạt KPI xuất sắc 2 kỳ liên tiếp, hoàn thành dự án tối ưu dây chuyền', '2026-11-01', NULL, NULL, 'CHO_DUYET', '2026-10-12 09:30:00')
    ON DUPLICATE KEY UPDATE trang_thai = 'CHO_DUYET';
");

echo "Seeded notification test records successfully!\n";
