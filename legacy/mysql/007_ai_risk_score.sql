-- =====================================================================
-- VINAMILK HRM - MIGRATION 007: AI RISK SCORE & TURNOVER PREDICTION
-- =====================================================================

CREATE TABLE IF NOT EXISTS `ai_risk_score` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv` VARCHAR(20) NOT NULL COMMENT 'Mã nhân viên theo nhansu',
    `ky_danh_gia` VARCHAR(7) NOT NULL COMMENT 'Kỳ đánh giá YYYY-MM, ví dụ 2026-10',
    `xac_suat_nghi_viec` DECIMAL(6,4) NOT NULL DEFAULT 0.0000 COMMENT 'Xác suất dự báo nghỉ việc (0.0000 - 1.0000)',
    `muc_do_rui_ro` ENUM('CAO', 'TRUNG_BINH', 'THAP') NOT NULL DEFAULT 'THAP' COMMENT 'Phân loại mức độ rủi ro',
    `ly_do_1` VARCHAR(255) NOT NULL COMMENT 'Yếu tố rủi ro hàng đầu 1',
    `ly_do_2` VARCHAR(255) NOT NULL COMMENT 'Yếu tố rủi ro hàng đầu 2',
    `ly_do_3` VARCHAR(255) NOT NULL COMMENT 'Yếu tố rủi ro hàng đầu 3',
    `khuyen_nghi_hanh_dong` TEXT NOT NULL COMMENT 'Đề xuất hành động giữ chân nhân sự của AI',
    `features_snapshot_json` TEXT NULL COMMENT 'JSON snapshot các đặc trưng đầu vào phục vụ kiểm toán mô hình',
    `mo_hinh_ap_dung` VARCHAR(100) NOT NULL DEFAULT 'Logistic_Regression_v1.0',
    `ghi_chu` VARCHAR(255) NOT NULL DEFAULT 'Mô hình huấn luyện trên dữ liệu giả lập',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uq_nv_ky` (`ma_nv`, `ky_danh_gia`),
    KEY `idx_ky` (`ky_danh_gia`),
    KEY `idx_mucdo` (`muc_do_rui_ro`),
    CONSTRAINT `fk_ai_risk_nhansu` FOREIGN KEY (`ma_nv`) REFERENCES `nhansu` (`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng chấm điểm rủi ro nghỉ việc AI Vinamilk HRM';
