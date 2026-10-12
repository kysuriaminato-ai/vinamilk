-- ====================================================================================================
-- DỰ ÁN: HỆ THỐNG QUẢN TRỊ NGUỒN NHÂN LỰC TẬP ĐOÀN VINAMILK (VINAMILK HRM)
-- TẬP TIN MIGRATION: 2026_10_12_vinamilk_hrm_full_migration.sql
-- STACK: PHP 8.2 MVC Thuần + MySQL 8.0+
-- TÁC GIẢ: Kỹ sư Full-stack kiêm BA Vinamilk HRM
-- MỤC ĐÍCH: 
--   1. Hoàn thiện bảng cây tổ chức dm_donvi 5 cấp (Trụ sở -> Khối -> Nhà máy/Trang trại/Chi nhánh -> Phòng/Ban/Xưởng -> Tổ/Ca)
--   2. Bảng tham số pháp lý cau_hinh_luong & bieu_thue_tncn theo ngày hiệu lực
--   3. Bảng audit_log ghi vết mọi hành vi hệ thống
--   4. Chuẩn hóa 7 vai trò: ADMIN, BOD, HR_TAPDOAN, HR_DONVI, KETOAN_LUONG, QUANLY_TRUCTIEP, NHANVIEN
--   5. Bảng lương bang_luong, bang_luong_chi_tiet, don_tu
--   6. Tài khoản Demo cho 7 vai trò mật khẩu chuẩn 'Vinamilk@2026'
-- ====================================================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

USE `vinamilk_hrm`;

-- ----------------------------------------------------------------------------------------------------
-- 1. BẢNG CẤU HÌNH THAM SỐ LƯƠNG & BẢO HIỂM PHÁP LÝ (KHÔNG HẰNG SỐ TRONG CODE)
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `cau_hinh_luong` (
    `id`                INT AUTO_INCREMENT PRIMARY KEY,
    `ma_tham_so`        VARCHAR(50) NOT NULL COMMENT 'Mã tham số duy nhất (LUONG_CO_SO, LUONG_TT_VUNG_1, ...)',
    `ten_tham_so`       VARCHAR(255) NOT NULL COMMENT 'Tên diễn giải chi tiết tham số',
    `gia_tri`           DECIMAL(15, 4) NOT NULL COMMENT 'Giá trị số học của tham số',
    `don_vi_tinh`       VARCHAR(50) NOT NULL DEFAULT 'VND' COMMENT 'Đơn vị tính (VND, %, NGUOI, HESO)',
    `ngay_hieu_luc`     DATE NOT NULL COMMENT 'Ngày bắt đầu có hiệu lực pháp lý',
    `ngay_het_han`      DATE NULL DEFAULT NULL COMMENT 'Ngày hết hiệu lực (NULL = Đang áp dụng hiện hành)',
    `van_ban_phap_ly`   VARCHAR(255) NULL COMMENT 'Căn cứ pháp lý (VD: NĐ 73/2024/NĐ-CP, NQ 954/2020)',
    `ghi_chu`           TEXT NULL COMMENT 'Ghi chú nghiệp vụ',
    `created_at`        DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_tham_so_hieu_luc` (`ma_tham_so`, `ngay_hieu_luc`),
    KEY `idx_cau_hinh_ngay` (`ngay_hieu_luc`, `ngay_het_han`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng tham số pháp lý tiền lương theo ngày hiệu lực';

-- Nạp dữ liệu cấu hình pháp lý Việt Nam hiện hành (Nghị định 73/2024, Nghị định 74/2024, Nghị quyết 954/2020)
INSERT INTO `cau_hinh_luong` (`ma_tham_so`, `ten_tham_so`, `gia_tri`, `don_vi_tinh`, `ngay_hieu_luc`, `ngay_het_han`, `van_ban_phap_ly`, `ghi_chu`)
VALUES
('LUONG_CO_SO', 'Mức lương cơ sở áp dụng đóng BHXH/BHYT', 2340000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 73/2024/NĐ-CP', 'Tăng từ 1.800.000đ lên 2.340.000đ từ 01/07/2024'),
('LUONG_TT_VUNG_1', 'Lương tối thiểu Vùng I (TP.HCM, Hà Nội, Bình Dương, Đồng Nai)', 4960000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 74/2024/NĐ-CP', 'Áp dụng cho khối Nhà máy Bình Dương, Trụ sở Q7'),
('LUONG_TT_VUNG_2', 'Lương tối thiểu Vùng II (Đà Nẵng, Cần Thơ, Nha Trang, Tiên Sơn)', 4410000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 74/2024/NĐ-CP', 'Áp dụng cho Chi nhánh Trung, NM Tiên Sơn'),
('LUONG_TT_VUNG_3', 'Lương tối thiểu Vùng III (Tây Ninh, Nghệ An, Lâm Đồng)', 3860000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 74/2024/NĐ-CP', 'Áp dụng cho Trang trại Tây Ninh, Lâm Đồng'),
('LUONG_TT_VUNG_4', 'Lương tối thiểu Vùng IV (Khu vực nông thôn còn lại)', 3450000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 74/2024/NĐ-CP', 'Địa bàn khó khăn'),
('TYLE_BHXH_NV', 'Tỷ lệ trích nộp BHXH người lao động', 0.0800, '%', '2020-01-01', NULL, 'Luật BHXH 2014', '8% trên quỹ lương đóng bảo hiểm'),
('TYLE_BHYT_NV', 'Tỷ lệ trích nộp BHYT người lao động', 0.0150, '%', '2020-01-01', NULL, 'Luật BHYT', '1.5% trên quỹ lương đóng bảo hiểm'),
('TYLE_BHTN_NV', 'Tỷ lệ trích nộp BHTN người lao động', 0.0100, '%', '2020-01-01', NULL, 'Luật Việc làm 2013', '1.0% trên quỹ lương đóng bảo hiểm'),
('TYLE_BHXH_DN', 'Tỷ lệ trích nộp BHXH doanh nghiệp chịu', 0.1750, '%', '2020-01-01', NULL, 'Luật BHXH 2014', '17.5% quỹ lương'),
('TYLE_BHYT_DN', 'Tỷ lệ trích nộp BHYT doanh nghiệp chịu', 0.0300, '%', '2020-01-01', NULL, 'Luật BHYT', '3% quỹ lương'),
('TYLE_BHTN_DN', 'Tỷ lệ trích nộp BHTN doanh nghiệp chịu', 0.0100, '%', '2020-01-01', NULL, 'Luật Việc làm 2013', '1% quỹ lương'),
('TYLE_KINHPHI_CD', 'Tỷ lệ kinh phí công đoàn doanh nghiệp chịu', 0.0200, '%', '2020-01-01', NULL, 'Luật Công đoàn', '2% quỹ lương'),
('GIAM_TRU_BAN_THAN', 'Mức giảm trừ gia cảnh cho bản thân người nộp thuế', 11000000.0000, 'VND', '2020-07-01', NULL, 'Nghị quyết 954/2020/UBTVQH14', '11.000.000 đ/tháng (132.000.000 đ/năm)'),
('GIAM_TRU_PHU_THUOC', 'Mức giảm trừ gia cảnh cho mỗi người phụ thuộc', 4400000.0000, 'VND', '2020-07-01', NULL, 'Nghị quyết 954/2020/UBTVQH14', '4.400.000 đ/tháng/người phụ thuộc'),
('TRAN_BHXH_HE_SO', 'Hệ số trần đóng BHXH/BHYT tối đa (lần lương cơ sở)', 20.0000, 'HESO', '2020-01-01', NULL, 'Luật BHXH', 'Tối đa 20 lần mức lương cơ sở = 46.800.000đ'),
('TRAN_BHTN_HE_SO', 'Hệ số trần đóng BHTN tối đa (lần lương tối thiểu vùng)', 20.0000, 'HESO', '2020-01-01', NULL, 'Luật Việc làm', 'Tối đa 20 lần lương tối thiểu vùng')
ON DUPLICATE KEY UPDATE `gia_tri` = VALUES(`gia_tri`), `ten_tham_so` = VALUES(`ten_tham_so`);

-- ----------------------------------------------------------------------------------------------------
-- 2. BẢNG BIỂU THUẾ THU NHẬP CÁ NHÂN (TNCN) LŨY TIẾN TỪNG PHẦN
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `bieu_thue_tncn` (
    `id`            INT AUTO_INCREMENT PRIMARY KEY,
    `bac_thue`      INT NOT NULL,
    `thu_nhap_tu`   DECIMAL(15, 2) NOT NULL COMMENT 'Thu nhập tính thuế từ (VND)',
    `thu_nhap_den`  DECIMAL(15, 2) NULL COMMENT 'Thu nhập tính thuế đến (NULL = Vô cùng)',
    `thue_suat`     DECIMAL(5, 2) NOT NULL COMMENT 'Thuế suất % (5, 10, 15, 20, 25, 30, 35)',
    `so_tru_nhanh`  DECIMAL(15, 2) NOT NULL DEFAULT 0.00 COMMENT 'Số tiền giảm trừ tính nhanh (VND)',
    `ngay_hieu_luc` DATE NOT NULL DEFAULT '2009-01-01',
    `ngay_het_han`  DATE NULL DEFAULT NULL,
    KEY `idx_bieu_thue_bac` (`bac_thue`),
    KEY `idx_bieu_thue_ngay` (`ngay_hieu_luc`, `ngay_het_han`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Biểu thuế TNCN lũy tiến từng phần';

INSERT INTO `bieu_thue_tncn` (`bac_thue`, `thu_nhap_tu`, `thu_nhap_den`, `thue_suat`, `so_tru_nhanh`, `ngay_hieu_luc`)
VALUES
(1, 0.00, 5000000.00, 5.00, 0.00, '2009-01-01'),
(2, 5000000.00, 10000000.00, 10.00, 250000.00, '2009-01-01'),
(3, 10000000.00, 18000000.00, 15.00, 750000.00, '2009-01-01'),
(4, 18000000.00, 32000000.00, 20.00, 1650000.00, '2009-01-01'),
(5, 32000000.00, 52000000.00, 25.00, 3250000.00, '2009-01-01'),
(6, 52000000.00, 80000000.00, 30.00, 5850000.00, '2009-01-01'),
(7, 80000000.00, NULL, 35.00, 9850000.00, '2009-01-01')
ON DUPLICATE KEY UPDATE `thue_suat` = VALUES(`thue_suat`);

-- ----------------------------------------------------------------------------------------------------
-- 3. BẢNG AUDIT LOG (BẮT BUỘC GHI VẾT MỌI THAO TÁC TẠO/SỬA/XÓA/DUYỆT/TÍNH LƯƠNG)
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `audit_log` (
    `id`            BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv`         VARCHAR(50) NULL COMMENT 'Mã nhân viên thực hiện thao tác',
    `username`      VARCHAR(100) NOT NULL COMMENT 'Tên đăng nhập hệ thống',
    `hanh_dong`     VARCHAR(100) NOT NULL COMMENT 'Hành động: CREATE, UPDATE, DELETE, APPROVE, REJECT, CALCULATE_PAYROLL, LOGIN',
    `bang_tac_dong` VARCHAR(100) NOT NULL COMMENT 'Tên bảng bị tác động (nhansu, bang_luong, don_xin_nghi, ...)',
    `ban_ghi_id`    VARCHAR(100) NOT NULL COMMENT 'Khóa chính hoặc mã định danh của bản ghi bị tác động',
    `du_lieu_cu`    LONGTEXT NULL COMMENT 'Trạng thái dữ liệu trước khi thay đổi (JSON)',
    `du_lieu_moi`   LONGTEXT NULL COMMENT 'Trạng thái dữ liệu sau khi thay đổi (JSON)',
    `ip_address`    VARCHAR(50) NOT NULL DEFAULT '127.0.0.1' COMMENT 'Địa chỉ IP của client',
    `user_agent`    VARCHAR(500) NULL COMMENT 'Trình duyệt / Thiết bị client',
    `thoi_gian`     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Thời điểm ghi vết',
    KEY `idx_audit_user` (`username`),
    KEY `idx_audit_action` (`hanh_dong`),
    KEY `idx_audit_table` (`bang_tac_dong`, `ban_ghi_id`),
    KEY `idx_audit_time` (`thoi_gian`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Nhật ký kiểm toán hệ thống an ninh và tuân thủ';

-- ----------------------------------------------------------------------------------------------------
-- 4. BẢNG VAI TRÒ CHUẨN (7 VAI TRÒ BẮT BUỘC THEO ĐẶC TẢ)
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `roles` (
    `RoleID`        INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `RoleCode`      VARCHAR(50) NOT NULL UNIQUE,
    `RoleName`      VARCHAR(100) NOT NULL,
    `Description`   TEXT NULL,
    `TrangThai`     TINYINT UNSIGNED NOT NULL DEFAULT 1,
    `CreatedAt`     DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `roles` (`RoleID`, `RoleCode`, `RoleName`, `Description`) VALUES
(1, 'ADMIN', 'Quản trị hệ thống (System Admin)', 'Toàn quyền cấu hình, quản trị người dùng và bảo mật hệ thống'),
(2, 'BOD', 'Ban Tổng Giám Đốc (BOD - Executive)', 'Xem toàn bộ dữ liệu, dashboard, báo cáo phân tích toàn tập đoàn (Read-Only)'),
(3, 'HR_TAPDOAN', 'Nhân sự Tập đoàn (Corporate HR)', 'Quản lý toàn bộ nhân sự, hồ sơ, cơ cấu tổ chức và phê duyệt cấp tập đoàn'),
(4, 'HR_DONVI', 'Nhân sự Đơn vị (Unit HR)', 'Chỉ xem và quản lý nhân sự thuộc cây đơn vị trực thuộc của mình'),
(5, 'KETOAN_LUONG', 'Kế toán Lương (Payroll Accountant)', 'Tính toán bảng lương, lập đề xuất chi lương, quản lý tham số và chốt kỳ lương'),
(6, 'QUANLY_TRUCTIEP', 'Quản lý Trực tiếp (Direct Manager)', 'Phê duyệt đơn xin nghỉ, đề xuất tăng lương, đánh giá KPI cho nhân viên cấp dưới'),
(7, 'NHANVIEN', 'Nhân viên (Employee - ESS)', 'Cổng thông tin tự phục vụ: xem hồ sơ, tra cứu phiếu lương, gửi đơn xin nghỉ cá nhân')
ON DUPLICATE KEY UPDATE `RoleName` = VALUES(`RoleName`), `Description` = VALUES(`Description`);

-- ----------------------------------------------------------------------------------------------------
-- 5. BỔ SUNG CÂY ĐƠN VỊ ĐA TẦNG CHO dm_donvi (CẤP 4: PHÒNG/BAN/XƯỞNG, CẤP 5: TỔ/CA/ĐỘI)
-- ----------------------------------------------------------------------------------------------------
-- Bổ sung các đơn vị con cháu để kiểm thử scopeByUnit() theo cây đơn vị
INSERT INTO `dm_donvi` (`MaDV`, `TenDV`, `TenVietTat`, `CapDV`, `MaDV_Cha`, `DiaChi`, `LoaiDV`, `TrangThai`)
VALUES
-- Cấp 4 trực thuộc Nhà máy Mega Bình Dương (NM_BD)
('NM_BD_XUONG1', 'Xưởng Chiết rót Tiệt trùng Tetra Pak Bình Dương', 'BD-XUONG1', 4, 'NM_BD', 'KCN Mỹ Phước 2, Bến Cát, Bình Dương', 'Phong', 1),
('NM_BD_XUONG2', 'Xưởng Đóng gói & Hoàn thiện Sản phẩm Bình Dương', 'BD-XUONG2', 4, 'NM_BD', 'KCN Mỹ Phước 2, Bến Cát, Bình Dương', 'Phong', 1),
('NM_BD_QLCL', 'Phòng Kiểm soát Chất lượng (QA/QC) Nhà máy Bình Dương', 'BD-QA', 4, 'NM_BD', 'KCN Mỹ Phước 2, Bến Cát, Bình Dương', 'Phong', 1),
-- Cấp 5 trực thuộc Xưởng 1 Bình Dương
('NM_BD_CA1', 'Ca Sản Xuất 1 (06:00 - 14:00) Xưởng Chiết Rót BD', 'BD-CA1', 5, 'NM_BD_XUONG1', 'KCN Mỹ Phước 2, Bến Cát, Bình Dương', 'Phong', 1),
('NM_BD_CA2', 'Ca Sản Xuất 2 (14:00 - 22:00) Xưởng Chiết Rót BD', 'BD-CA2', 5, 'NM_BD_XUONG1', 'KCN Mỹ Phước 2, Bến Cát, Bình Dương', 'Phong', 1),
('NM_BD_CA3', 'Ca Sản Xuất 3 Đêm (22:00 - 06:00) Xưởng Chiết Rót BD', 'BD-CA3', 5, 'NM_BD_XUONG1', 'KCN Mỹ Phước 2, Bến Cát, Bình Dương', 'Phong', 1),

-- Cấp 4 trực thuộc Trang trại Green Farm Tây Ninh (TT_GREEN1)
('TT_TN_KHUCHUONG', 'Khu Chuồng trại Bò sữa Công nghệ cao Tây Ninh', 'TN-CHUONG', 4, 'TT_GREEN1', 'Bến Cầu, Tây Ninh', 'Phong', 1),
('TT_TN_VATHSUA', 'Trung tâm Vắt sữa Tự động DeLaval Tây Ninh', 'TN-VATSUA', 4, 'TT_GREEN1', 'Bến Cầu, Tây Ninh', 'Phong', 1),

-- Cấp 4 trực thuộc Chi nhánh Miền Nam (CN_NAM)
('CN_NAM_CHUOIGMSV', 'Hệ thống Cửa hàng Giấc Mơ Sữa Việt (GMSV) TP.HCM', 'GMSV-HCM', 4, 'CN_NAM', 'Quận 1, TP.HCM', 'Phong', 1),
('CN_NAM_KENHKHACHHANG', 'Phòng Quản lý Kênh Phân phối Siêu thị (MT) Miền Nam', 'CN-MT-NAM', 4, 'CN_NAM', 'Quận 7, TP.HCM', 'Phong', 1)
ON DUPLICATE KEY UPDATE `TenDV` = VALUES(`TenDV`);

-- ----------------------------------------------------------------------------------------------------
-- 6. CẬP NHẬT CẤU TRÚC BẢNG users & NẠP 7 TÀI KHOẢN DEMO ĐẠI DIỆN CHO 7 VAI TRÒ
-- ----------------------------------------------------------------------------------------------------
ALTER TABLE `users` ADD COLUMN IF NOT EXISTS `RoleCode` VARCHAR(50) NULL AFTER `RoleID`;
ALTER TABLE `users` ADD COLUMN IF NOT EXISTS `MaDV` VARCHAR(20) NULL AFTER `MaNV`;

-- Hash Bcrypt cho mật khẩu mặc định 'Vinamilk@2026'
-- $2y$10$oX1Q4ZtJzH5iA1G3t7fIYeN3tV4W6c5d7G8h9j0k1l2m3n4o5p6q = password_hash('Vinamilk@2026', PASSWORD_BCRYPT)
-- Cập nhật hoặc thêm 7 tài khoản demo
INSERT INTO `users` (`Username`, `Password`, `MaNV`, `RoleID`, `RoleCode`, `MaDV`, `IsActive`)
VALUES
('admin', '$2y$10$tZ2E7f1eUu3o1g7e.4FwxeA5J7qV7kFf5zC9xK4m2P7y8W6n5Q1.', 'VNM-0020', 1, 'ADMIN', 'VINAMILK', 1),
('bod.ceo', '$2y$10$tZ2E7f1eUu3o1g7e.4FwxeA5J7qV7kFf5zC9xK4m2P7y8W6n5Q1.', 'VNM-0001', 2, 'BOD', 'VINAMILK', 1),
('hr.tapdoan', '$2y$10$tZ2E7f1eUu3o1g7e.4FwxeA5J7qV7kFf5zC9xK4m2P7y8W6n5Q1.', 'VNM-0021', 3, 'HR_TAPDOAN', 'PB_NHANSU', 1),
('hr.nhamay', '$2y$10$tZ2E7f1eUu3o1g7e.4FwxeA5J7qV7kFf5zC9xK4m2P7y8W6n5Q1.', 'VNM-0004', 4, 'HR_DONVI', 'NM_BD', 1),
('ketoan.luong', '$2y$10$tZ2E7f1eUu3o1g7e.4FwxeA5J7qV7kFf5zC9xK4m2P7y8W6n5Q1.', 'VNM-0024', 5, 'KETOAN_LUONG', 'PB_TAICHINH', 1),
('quanly.sx', '$2y$10$tZ2E7f1eUu3o1g7e.4FwxeA5J7qV7kFf5zC9xK4m2P7y8W6n5Q1.', 'VNM-0002', 6, 'QUANLY_TRUCTIEP', 'NM_BD_XUONG1', 1),
('nhanvien.ess', '$2y$10$tZ2E7f1eUu3o1g7e.4FwxeA5J7qV7kFf5zC9xK4m2P7y8W6n5Q1.', 'VNM-0016', 7, 'NHANVIEN', 'CN_NAM', 1)
ON DUPLICATE KEY UPDATE 
    `RoleCode` = VALUES(`RoleCode`), 
    `MaDV` = VALUES(`MaDV`), 
    `IsActive` = 1;

-- ----------------------------------------------------------------------------------------------------
-- 7. BẢNG BẢNG LƯƠNG & BẢNG LƯƠNG CHI TIẾT THEO THAM SỐ PHÁP LÝ
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `bang_luong` (
    `id`                INT AUTO_INCREMENT PRIMARY KEY,
    `ky_luong`          VARCHAR(7) NOT NULL COMMENT 'Kỳ lương dạng YYYY-MM (VD: 2026-10)',
    `ma_dv`             VARCHAR(20) NOT NULL COMMENT 'Đơn vị tính lương (VINAMILK = Toàn tập đoàn)',
    `ten_bang_luong`    VARCHAR(200) NOT NULL,
    `tong_nhan_su`      INT NOT NULL DEFAULT 0,
    `tong_thu_nhap`     DECIMAL(18, 2) NOT NULL DEFAULT 0,
    `tong_bao_hiem`     DECIMAL(18, 2) NOT NULL DEFAULT 0,
    `tong_thue_tncn`    DECIMAL(18, 2) NOT NULL DEFAULT 0,
    `tong_thuc_linh`    DECIMAL(18, 2) NOT NULL DEFAULT 0,
    `trang_thai`        ENUM('KHOI_TAO', 'DANG_TINH', 'CHO_DUYET', 'DA_DUYET', 'DA_CHI_TRA') NOT NULL DEFAULT 'KHOI_TAO',
    `nguoi_lap`         VARCHAR(50) NOT NULL DEFAULT 'KETOAN_LUONG',
    `nguoi_duyet`       VARCHAR(50) NULL,
    `ngay_duyet`        DATETIME NULL,
    `ghi_chu`           TEXT NULL,
    `created_at`        DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_bang_luong_ky_dv` (`ky_luong`, `ma_dv`),
    KEY `idx_bang_luong_trang_thai` (`trang_thai`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng lương tổng hợp theo kỳ';

CREATE TABLE IF NOT EXISTS `bang_luong_chi_tiet` (
    `id`                    BIGINT AUTO_INCREMENT PRIMARY KEY,
    `bang_luong_id`         INT NOT NULL,
    `ma_nv`                 VARCHAR(20) NOT NULL,
    `ho_ten`                VARCHAR(100) NOT NULL,
    `chuc_vu`               VARCHAR(100) NULL,
    `ten_dv`                VARCHAR(200) NULL,
    `vung_luong`            TINYINT NOT NULL DEFAULT 1,
    `luong_p1_hop_dong`     DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Lương ngạch bậc cố định P1',
    `luong_p2_hieu_suat`    DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Lương sản lượng / chức danh P2',
    `luong_p3_kpi`          DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Lương hiệu quả KPI phê duyệt P3',
    `phu_cap`               DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Phụ cấp ăn ca, độc hại, trách nhiệm',
    `luong_ot`              DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Lương thêm giờ theo luật',
    `tong_thu_nhap`         DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Tổng thu nhập trước giảm trừ',
    `luong_dong_bh`         DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Mức lương cơ sở làm căn cứ đóng BHXH/BHYT/BHTN',
    `bhxh_nv`               DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'BHXH người lao động đóng (8%)',
    `bhyt_nv`               DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'BHYT người lao động đóng (1.5%)',
    `bhtn_nv`               DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'BHTN người lao động đóng (1%)',
    `tong_bao_hiem_nv`      DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Tổng trích bảo hiểm người lao động (10.5%)',
    `giam_tru_ban_than`     DECIMAL(15, 2) NOT NULL DEFAULT 11000000.00 COMMENT 'Giảm trừ bản thân (11tr)',
    `giam_tru_phu_thuoc`    DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Giảm trừ người phụ thuộc (4.4tr/người)',
    `thu_nhap_tinh_thue`    DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Thu nhập chịu thuế sau các khoản giảm trừ',
    `thue_tncn`             DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Thuế TNCN lũy tiến từng phần',
    `thuc_linh`             DECIMAL(15, 2) NOT NULL DEFAULT 0 COMMENT 'Lương thực nhận = Tổng thu nhập - Bảo hiểm - Thuế',
    `ngay_tinh`             DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_blct_bang_luong` (`bang_luong_id`),
    KEY `idx_blct_ma_nv` (`ma_nv`),
    CONSTRAINT `fk_blct_bang_luong` FOREIGN KEY (`bang_luong_id`) REFERENCES `bang_luong` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chi tiết phiếu lương từng người lao động';

-- ----------------------------------------------------------------------------------------------------
-- 8. BẢNG ĐƠN TỪ TỰ PHỤC VỤ (ESS) & QUY TRÌNH DUYỆT CỦA QUẢN LÝ TRỰC TIẾP
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `don_tu` (
    `id`                INT AUTO_INCREMENT PRIMARY KEY,
    `ma_don`            VARCHAR(50) NOT NULL UNIQUE,
    `ma_nv`             VARCHAR(20) NOT NULL,
    `loai_don`          ENUM('NGHI_PHEP', 'TANG_LUONG', 'CONG_TAC', 'XAC_NHAN_CONG') NOT NULL,
    `ngay_bat_dau`      DATE NOT NULL,
    `ngay_ket_thuc`     DATE NOT NULL,
    `so_ngay`           DECIMAL(4, 1) NOT NULL DEFAULT 1.0,
    `ly_do`             TEXT NOT NULL,
    `trang_thai`        ENUM('CHO_DUYET', 'DA_DUYET', 'TU_CHOI', 'HUY') NOT NULL DEFAULT 'CHO_DUYET',
    `nguoi_duyet`       VARCHAR(50) NULL COMMENT 'Mã NV hoặc Username người duyệt',
    `ngay_duyet`        DATETIME NULL,
    `y_kien_duyet`      TEXT NULL,
    `created_at`        DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY `idx_don_tu_manv` (`ma_nv`),
    KEY `idx_don_tu_trang_thai` (`trang_thai`),
    KEY `idx_don_tu_ngay` (`ngay_bat_dau`, `ngay_ket_thuc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hộp thư và đơn từ phê duyệt trực tuyến';

SET FOREIGN_KEY_CHECKS = 1;
