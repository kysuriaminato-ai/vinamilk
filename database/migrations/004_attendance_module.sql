-- =====================================================================
-- VINAMILK ENTERPRISE HRM - MIGRATION 004: MODULE CHẤM CÔNG (ATTENDANCE)
-- =====================================================================

-- 1. Bổ sung các cột phân loại OT và quên quẹt vào cham_cong_ngay
ALTER TABLE `cham_cong_ngay`
    ADD COLUMN IF NOT EXISTS `ot_ngay_thuong` DECIMAL(4,2) DEFAULT 0.00 AFTER `gio_lam_them`,
    ADD COLUMN IF NOT EXISTS `ot_ngay_nghi` DECIMAL(4,2) DEFAULT 0.00 AFTER `ot_ngay_thuong`,
    ADD COLUMN IF NOT EXISTS `ot_ngay_le` DECIMAL(4,2) DEFAULT 0.00 AFTER `ot_ngay_nghi`,
    ADD COLUMN IF NOT EXISTS `quen_quet` TINYINT(1) DEFAULT 0 AFTER `ve_som_phut`;

-- 2. Bảng lưu trữ log quẹt thẻ thô từ máy chấm công (Raw Biometric Swipes)
CREATE TABLE IF NOT EXISTS `quet_the_tho` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv` VARCHAR(20) NOT NULL,
    `thoi_gian_quet` DATETIME NOT NULL,
    `ma_may` VARCHAR(50) DEFAULT 'MCC-01',
    `loai_quet` ENUM('VAO', 'RA', 'KHONG_XAC_DINH') DEFAULT 'KHONG_XAC_DINH',
    `nguon_import` VARCHAR(150) NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_quet_manv_time` (`ma_nv`, `thoi_gian_quet`),
    KEY `idx_quet_time` (`thoi_gian_quet`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Bảng công tháng tổng hợp (Monthly Timesheet Summary) & Quy trình duyệt 3 bước
CREATE TABLE IF NOT EXISTS `bang_cong_thang` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `thang` TINYINT NOT NULL,
    `nam` SMALLINT NOT NULL,
    `ma_nv` VARCHAR(20) NOT NULL,
    `ma_dv` VARCHAR(20) NOT NULL,
    `cong_chuan_thang` DECIMAL(5,2) DEFAULT 26.00,
    `cong_thuc_te` DECIMAL(5,2) DEFAULT 0.00,
    `nghi_phep_huong_luong` DECIMAL(5,2) DEFAULT 0.00,
    `nghi_le_tet` DECIMAL(5,2) DEFAULT 0.00,
    `nghi_khong_luong` DECIMAL(5,2) DEFAULT 0.00,
    `nghi_om_thai_san` DECIMAL(5,2) DEFAULT 0.00,
    `tong_cong_tinh_luong` DECIMAL(5,2) DEFAULT 0.00,
    `gio_ot_ngay_thuong` DECIMAL(5,2) DEFAULT 0.00,
    `gio_ot_ngay_nghi` DECIMAL(5,2) DEFAULT 0.00,
    `gio_ot_ngay_le` DECIMAL(5,2) DEFAULT 0.00,
    `tong_gio_ot` DECIMAL(5,2) DEFAULT 0.00,
    `gio_lam_dem` DECIMAL(5,2) DEFAULT 0.00,
    `di_muon_ve_som_lan` INT DEFAULT 0,
    `quen_quet_the_lan` INT DEFAULT 0,
    `trang_thai` ENUM('KHOI_TAO', 'TO_TRUONG_XAC_NHAN', 'QUAN_DOC_DUYET', 'KHOA_CONG') DEFAULT 'KHOI_TAO',
    `nguoi_xac_nhan` VARCHAR(50) NULL,
    `ngay_xac_nhan` DATETIME NULL,
    `nguoi_duyet` VARCHAR(50) NULL,
    `ngay_duyet` DATETIME NULL,
    `nguoi_khoa` VARCHAR(50) NULL,
    `ngay_khoa` DATETIME NULL,
    `canh_bao_ot` VARCHAR(255) NULL,
    `ghi_chu` TEXT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_bangcong_thang_nam_nv` (`thang`, `nam`, `ma_nv`),
    KEY `idx_bangcong_dv` (`ma_dv`, `nam`, `thang`),
    KEY `idx_bangcong_trangthai` (`trang_thai`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Bảng phiếu điều chỉnh công sau khi đã khóa công (Audit Trail)
CREATE TABLE IF NOT EXISTS `dieu_chinh_cong` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv` VARCHAR(20) NOT NULL,
    `ngay_cong` DATE NOT NULL,
    `thang` TINYINT NOT NULL,
    `nam` SMALLINT NOT NULL,
    `loai_dieu_chinh` ENUM('BO_SUNG_GIO_QUET', 'DIEU_CHINH_GIO_OT', 'CHUYEN_LOAI_CONG', 'DIEU_CHINH_CONG_NGAY') NOT NULL,
    `gia_tri_cu` VARCHAR(255) NOT NULL,
    `gia_tri_moi` VARCHAR(255) NOT NULL,
    `ly_do` TEXT NOT NULL,
    `nguoi_de_xuat` VARCHAR(50) NOT NULL,
    `nguoi_phe_duyet` VARCHAR(50) NULL,
    `ngay_phe_duyet` DATETIME NULL,
    `trang_thai` ENUM('CHO_DUYET', 'DA_DUYET', 'TU_CHOI') DEFAULT 'CHO_DUYET',
    `ghi_chu` TEXT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_dc_nv_ngay` (`ma_nv`, `ngay_cong`),
    KEY `idx_dc_thang_nam` (`thang`, `nam`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. Bổ sung cấu hình giới hạn giờ làm thêm (OT Limits) theo Bộ luật Lao động 2019
INSERT INTO `cau_hinh_luong` (`ma_tham_so`, `ten_tham_so`, `gia_tri`, `don_vi_tinh`, `ngay_hieu_luc`, `van_ban_phap_ly`, `ghi_chu`)
VALUES 
    ('OT_MAX_HOURS_MONTH', 'Giới hạn giờ làm thêm tối đa trong tháng', 40.0000, 'giờ/tháng', '2021-01-01', 'Điều 107 BLLĐ 2019', 'Tối đa 40 giờ/tháng'),
    ('OT_MAX_HOURS_YEAR', 'Giới hạn giờ làm thêm tối đa trong năm', 200.0000, 'giờ/năm', '2021-01-01', 'Điều 107 BLLĐ 2019', 'Tối đa 200 giờ/năm thông thường'),
    ('OT_MAX_HOURS_YEAR_SPECIAL', 'Giới hạn giờ làm thêm ngành sữa & chế biến thực phẩm', 300.0000, 'giờ/năm', '2021-01-01', 'Điều 107 BLLĐ 2019', 'Ngành chế biến sữa và bảo quản thực phẩm được tối đa 300h/năm'),
    ('OT_RATE_WEEKDAY', 'Tỷ lệ trả lương làm thêm giờ ngày thường', 1.5000, 'hệ số', '2021-01-01', 'Điều 98 BLLĐ 2019', '150% tiền lương'),
    ('OT_RATE_WEEKEND', 'Tỷ lệ trả lương làm thêm giờ ngày nghỉ hàng tuần', 2.0000, 'hệ số', '2021-01-01', 'Điều 98 BLLĐ 2019', '200% tiền lương'),
    ('OT_RATE_HOLIDAY', 'Tỷ lệ trả lương làm thêm giờ ngày lễ tết', 3.0000, 'hệ số', '2021-01-01', 'Điều 98 BLLĐ 2019', '300% tiền lương'),
    ('NIGHT_SHIFT_RATE', 'Phụ cấp làm việc ban đêm (22:00 - 06:00)', 0.3000, 'hệ số', '2021-01-01', 'Điều 98 BLLĐ 2019', 'Thêm 30% tiền lương theo ca')
ON DUPLICATE KEY UPDATE `gia_tri` = VALUES(`gia_tri`);
