-- =====================================================================
-- VINAMILK HRM - MIGRATION 006: FACT HEADCOUNT MONTHLY & HR ANALYTICS
-- =====================================================================

-- 1. BẢNG FACT SNAPSHOT NHÂN SỰ HẰNG THÁNG (fact_headcount_monthly)
CREATE TABLE IF NOT EXISTS `fact_headcount_monthly` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ky_nam_thang` VARCHAR(7) NOT NULL COMMENT 'Định dạng YYYY-MM, ví dụ 2026-10',
    `ngay_snapshot` DATE NOT NULL COMMENT 'Ngày chốt số liệu (ngày cuối tháng)',
    `ma_dv` VARCHAR(20) NOT NULL COMMENT 'Mã đơn vị theo dm_donvi',
    `headcount_dau_ky` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Số nhân sự đầu kỳ',
    `tuyen_moi` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Số nhân sự tuyển mới trong tháng',
    `nghi_viec` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Số nhân sự thôi việc trong tháng',
    `headcount_cuoi_ky` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Số nhân sự cuối kỳ (đang làm việc)',
    `headcount_binh_quan` DECIMAL(8,2) NOT NULL DEFAULT 0.00 COMMENT '(Đầu kỳ + Cuối kỳ) / 2',
    `dinh_bien` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Số lượng định biên được duyệt',
    `ty_le_lap_day` DECIMAL(5,2) NOT NULL DEFAULT 0.00 COMMENT '(Headcount cuối kỳ / Định biên) * 100',
    `tong_quy_luong` DECIMAL(18,2) NOT NULL DEFAULT 0.00 COMMENT 'Tổng quỹ lương tháng (VNĐ)',
    `tong_gio_ot` DECIMAL(10,2) NOT NULL DEFAULT 0.00 COMMENT 'Tổng giờ làm thêm trong tháng',
    `gio_ot_binh_quan` DECIMAL(6,2) NOT NULL DEFAULT 0.00 COMMENT 'Giờ OT bình quân / người',
    `tong_ngay_cong_chuan` DECIMAL(10,2) NOT NULL DEFAULT 0.00 COMMENT 'Tổng ngày công chuẩn kỳ',
    `so_ngay_vang_mat` DECIMAL(10,2) NOT NULL DEFAULT 0.00 COMMENT 'Tổng số ngày nghỉ phép/ốm/vắng mặt',
    `ty_le_vang_mat` DECIMAL(5,2) NOT NULL DEFAULT 0.00 COMMENT '(Vắng mặt / Ngày công chuẩn) * 100',
    `nghi_viec_12t` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Tổng số người thôi việc 12 tháng trượt',
    `headcount_avg_12t` DECIMAL(8,2) NOT NULL DEFAULT 0.00 COMMENT 'Headcount bình quân 12 tháng trượt',
    `ty_le_nghi_viec_12t` DECIMAL(5,2) NOT NULL DEFAULT 0.00 COMMENT 'Tỷ lệ nghỉ việc 12 tháng trượt (%)',
    `ghi_chu` VARCHAR(255) NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uq_ky_dv` (`ky_nam_thang`, `ma_dv`),
    KEY `idx_ky` (`ky_nam_thang`),
    KEY `idx_dv` (`ma_dv`),
    CONSTRAINT `fk_fhc_donvi` FOREIGN KEY (`ma_dv`) REFERENCES `dm_donvi` (`MaDV`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. VIEW TỔNG HỢP THEO ĐƠN VỊ VÀ CẤU TRÚC CÂY (v_fact_headcount_summary)
DROP VIEW IF EXISTS `v_fact_headcount_summary`;
CREATE VIEW `v_fact_headcount_summary` AS
SELECT 
    f.ky_nam_thang,
    f.ngay_snapshot,
    f.ma_dv,
    dv.TenDV,
    dv.TenVietTat,
    dv.CapDV,
    dv.MaDV_Cha,
    dv.LoaiDV,
    f.headcount_dau_ky,
    f.tuyen_moi,
    f.nghi_viec,
    f.headcount_cuoi_ky,
    f.headcount_binh_quan,
    f.dinh_bien,
    f.ty_le_lap_day,
    f.tong_quy_luong,
    f.tong_gio_ot,
    f.gio_ot_binh_quan,
    f.so_ngay_vang_mat,
    f.ty_le_vang_mat,
    f.nghi_viec_12t,
    f.headcount_avg_12t,
    f.ty_le_nghi_viec_12t
FROM fact_headcount_monthly f
INNER JOIN dm_donvi dv ON f.ma_dv = dv.MaDV;
