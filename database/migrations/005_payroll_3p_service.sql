-- =====================================================================
-- VINAMILK HRM - MIGRATION 005: 3P PAYROLL SERVICE & PHÁP LÝ THUẾ 2026
-- =====================================================================

-- 1. Bảng điểm KPI tháng của nhân sự (Dùng tính lương P3 động, không hằng số)
CREATE TABLE IF NOT EXISTS `kpi` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv` VARCHAR(20) NOT NULL,
    `thang` TINYINT NOT NULL,
    `nam` SMALLINT NOT NULL,
    `diem_kpi` DECIMAL(5,2) NOT NULL DEFAULT 100.00,
    `he_so_kpi` DECIMAL(4,2) NOT NULL DEFAULT 1.00,
    `xep_loai` ENUM('XUAT_SAC', 'TOT', 'HOAN_THANH', 'CAN_CAI_THIEN', 'KHONG_HOAN_THANH') DEFAULT 'HOAN_THANH',
    `nguoi_danh_gia` VARCHAR(50) NULL,
    `ngay_danh_gia` DATE NULL,
    `ghi_chu` TEXT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_kpi_nv_ky` (`ma_nv`, `thang`, `nam`),
    KEY `idx_kpi_ky` (`nam`, `thang`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Cập nhật ngày hết hạn của cấu hình thuế cũ và seed mức giảm trừ mới hiệu lực 01/01/2026
UPDATE `cau_hinh_luong` 
SET `ngay_het_han` = '2025-12-31' 
WHERE `ma_tham_so` IN ('GIAM_TRU_BAN_THAN', 'GIAM_TRU_PHU_THUOC') AND `ngay_hieu_luc` = '2020-07-01';

-- Seed giảm trừ bản thân (15,5 triệu) và người phụ thuộc (6,2 triệu) từ 01/01/2026
INSERT INTO `cau_hinh_luong` (`ma_tham_so`, `ten_tham_so`, `gia_tri`, `don_vi_tinh`, `ngay_hieu_luc`, `ngay_het_han`, `ghi_chu`, `van_ban_phap_ly`)
VALUES 
('GIAM_TRU_BAN_THAN', 'Mức giảm trừ gia cảnh bản thân (Luật Thuế TNCN sửa đổi)', 15500000.0000, 'VND', '2026-01-01', NULL, 'Giảm trừ gia cảnh cho chính người nộp thuế từ 01/01/2026', 'Luật Thuế TNCN sửa đổi 2025/2026'),
('GIAM_TRU_PHU_THUOC', 'Mức giảm trừ người phụ thuộc (Luật Thuế TNCN sửa đổi)', 6200000.0000, 'VND', '2026-01-01', NULL, 'Giảm trừ cho mỗi người phụ thuộc hợp pháp từ 01/01/2026', 'Luật Thuế TNCN sửa đổi 2025/2026'),
('PHU_CAP_AN_CA_MIEN_THUE', 'Mức phụ cấp ăn ca tối đa không chịu thuế TNCN', 730000.0000, 'VND', '2020-01-01', NULL, 'Phụ cấp tiền ăn giữa ca miễn thuế TNCN', 'Thông tư 26/2016/TT-BLĐTBXH & TT 111/2013/TT-BTC')
ON DUPLICATE KEY UPDATE `gia_tri` = VALUES(`gia_tri`), `ngay_hieu_luc` = VALUES(`ngay_hieu_luc`), `van_ban_phap_ly` = VALUES(`van_ban_phap_ly`);

-- 3. Cập nhật ngày hết hạn biểu thuế 7 bậc cũ (2009-2025) và thêm biểu thuế 5 bậc hiệu lực từ 01/01/2026
UPDATE `bieu_thue_tncn` 
SET `ngay_het_han` = '2025-12-31' 
WHERE `ngay_hieu_luc` = '2009-01-01' AND `ngay_het_han` IS NULL;

-- Thêm biểu thuế lũy tiến 5 bậc từ 2026
INSERT INTO `bieu_thue_tncn` (`bac_thue`, `thu_nhap_tu`, `thu_nhap_den`, `thue_suat`, `so_tru_nhanh`, `ngay_hieu_luc`, `ngay_het_han`)
VALUES 
(1, 0.00, 10000000.00, 5.00, 0.00, '2026-01-01', NULL),
(2, 10000000.00, 30000000.00, 10.00, 500000.00, '2026-01-01', NULL),
(3, 30000000.00, 60000000.00, 20.00, 3500000.00, '2026-01-01', NULL),
(4, 60000000.00, 100000000.00, 30.00, 9500000.00, '2026-01-01', NULL),
(5, 100000000.00, NULL, 35.00, 14500000.00, '2026-01-01', NULL)
ON DUPLICATE KEY UPDATE `thue_suat` = VALUES(`thue_suat`), `so_tru_nhanh` = VALUES(`so_tru_nhanh`);

-- 4. Bổ sung các trạng thái phê duyệt mới cho bảng_luong
ALTER TABLE `bang_luong`
    MODIFY COLUMN `trang_thai` ENUM('NHAP', 'DA_TINH', 'KETOAN_DUYET', 'GIAMDOC_DUYET', 'DA_CHI', 'KHOI_TAO', 'DANG_TINH', 'CHO_DUYET', 'DA_DUYET', 'DA_CHI_TRA') DEFAULT 'NHAP';

ALTER TABLE `bang_luong`
    ADD COLUMN IF NOT EXISTS `nguoi_ketoan_duyet` VARCHAR(50) NULL AFTER `nguoi_lap`,
    ADD COLUMN IF NOT EXISTS `ngay_ketoan_duyet` DATETIME NULL AFTER `nguoi_ketoan_duyet`,
    ADD COLUMN IF NOT EXISTS `nguoi_giamdoc_duyet` VARCHAR(50) NULL AFTER `nguoi_duyet`,
    ADD COLUMN IF NOT EXISTS `ngay_giamdoc_duyet` DATETIME NULL AFTER `nguoi_giamdoc_duyet`,
    ADD COLUMN IF NOT EXISTS `ngay_chi_tra` DATETIME NULL AFTER `ngay_giamdoc_duyet`;

-- 5. Bổ sung các cột chi tiết 3P, OT phân loại, ăn ca, khấu trừ vào bang_luong_chi_tiet
ALTER TABLE `bang_luong_chi_tiet`
    ADD COLUMN IF NOT EXISTS `cong_chuan` DECIMAL(5,2) DEFAULT 26.00 AFTER `vung_luong`,
    ADD COLUMN IF NOT EXISTS `cong_thuc_te` DECIMAL(5,2) DEFAULT 26.00 AFTER `cong_chuan`,
    ADD COLUMN IF NOT EXISTS `bac_luong` VARCHAR(50) NULL AFTER `cong_thuc_te`,
    ADD COLUMN IF NOT EXISTS `he_so_bac` DECIMAL(4,2) DEFAULT 1.00 AFTER `bac_luong`,
    ADD COLUMN IF NOT EXISTS `diem_kpi` DECIMAL(5,2) DEFAULT 100.00 AFTER `luong_p2_hieu_suat`,
    ADD COLUMN IF NOT EXISTS `he_so_kpi` DECIMAL(4,2) DEFAULT 1.00 AFTER `diem_kpi`,
    ADD COLUMN IF NOT EXISTS `tien_ot_thuong` DECIMAL(15,2) DEFAULT 0.00 AFTER `luong_ot`,
    ADD COLUMN IF NOT EXISTS `tien_ot_nghi` DECIMAL(15,2) DEFAULT 0.00 AFTER `tien_ot_thuong`,
    ADD COLUMN IF NOT EXISTS `tien_ot_le` DECIMAL(15,2) DEFAULT 0.00 AFTER `tien_ot_nghi`,
    ADD COLUMN IF NOT EXISTS `tien_ot_dem` DECIMAL(15,2) DEFAULT 0.00 AFTER `tien_ot_le`,
    ADD COLUMN IF NOT EXISTS `ot_mien_thue` DECIMAL(15,2) DEFAULT 0.00 AFTER `tien_ot_dem`,
    ADD COLUMN IF NOT EXISTS `phu_cap_an_ca` DECIMAL(15,2) DEFAULT 0.00 AFTER `phu_cap`,
    ADD COLUMN IF NOT EXISTS `an_ca_mien_thue` DECIMAL(15,2) DEFAULT 0.00 AFTER `phu_cap_an_ca`,
    ADD COLUMN IF NOT EXISTS `thu_nhap_chiu_thue` DECIMAL(15,2) DEFAULT 0.00 AFTER `tong_thu_nhap`,
    ADD COLUMN IF NOT EXISTS `khau_tru_tam_ung` DECIMAL(15,2) DEFAULT 0.00 AFTER `thue_tncn`,
    ADD COLUMN IF NOT EXISTS `khau_tru_tai_san` DECIMAL(15,2) DEFAULT 0.00 AFTER `khau_tru_tam_ung`,
    ADD COLUMN IF NOT EXISTS `tong_khau_tru` DECIMAL(15,2) DEFAULT 0.00 AFTER `khau_tru_tai_san`;
