-- =====================================================================
-- VINAMILK ENTERPRISE HRM - MIGRATION 003: MODULE HỒ SƠ NHÂN SỰ
-- =====================================================================
-- Bổ sung bảng tài liệu scan, khen thưởng kỷ luật và chuẩn hóa trạng thái hợp đồng

-- 1. Bảng lưu trữ tài liệu scan nhân sự (lưu ngoài public/storage/uploads/documents)
CREATE TABLE IF NOT EXISTS `tai_lieu_nhan_su` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv` VARCHAR(20) NOT NULL,
    `ten_tai_lieu` VARCHAR(255) NOT NULL,
    `loai_tai_lieu` ENUM('CCCD_CMND', 'SO_YEU_LY_LICH', 'BANG_CAP_CHUNG_CHI', 'HOP_DONG_SCAN', 'GIAY_KHAM_SUC_KHOE', 'GIAY_KHAI_SINH_CON', 'KHAC') NOT NULL DEFAULT 'KHAC',
    `file_path` VARCHAR(500) NOT NULL,
    `file_name_original` VARCHAR(255) NOT NULL,
    `file_size` INT NOT NULL,
    `mime_type` VARCHAR(100) NOT NULL,
    `ngay_upload` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `nguoi_upload` VARCHAR(50) NULL,
    `ghi_chu` TEXT NULL,
    KEY `idx_tailieu_manv` (`ma_nv`),
    KEY `idx_tailieu_loai` (`loai_tai_lieu`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Bảng khen thưởng và kỷ luật (KT-KL)
CREATE TABLE IF NOT EXISTS `khen_thuong_ky_luat` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv` VARCHAR(20) NOT NULL,
    `loai` ENUM('KHEN_THUONG', 'KY_LUAT') NOT NULL,
    `hinh_thuc` VARCHAR(150) NOT NULL,
    `so_quyet_dinh` VARCHAR(100) NULL,
    `ngay_quyet_dinh` DATE NOT NULL,
    `ngay_hieu_luc` DATE NOT NULL,
    `co_quan_ban_hanh` VARCHAR(200) DEFAULT 'Công ty CP Sữa Việt Nam',
    `nguoi_ky` VARCHAR(100) NULL,
    `ly_do` TEXT NOT NULL,
    `so_tien` DECIMAL(15,2) DEFAULT 0,
    `ghi_chu` TEXT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_ktkl_manv` (`ma_nv`),
    KEY `idx_ktkl_loai` (`loai`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Tạo thư mục bảo mật lưu ngoài public nếu cần (thực hiện qua PHP)
