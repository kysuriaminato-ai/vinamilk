-- ====================================================================================================
-- DỰ ÁN: HỆ THỐNG QUẢN LÝ NGUỒN NHÂN LỰC TẬP ĐOÀN VINAMILK (VINAMILK HRM)
-- TẬP TIN: database/migrations/001_unify.sql
-- MỤC ĐÍCH: 
--   1. Hợp nhất toàn bộ schema giữa mã nguồn PHP (app/models/*.php) và cơ sở dữ liệu MySQL 8.
--   2. Bổ sung các bảng nghiệp vụ cốt lõi theo quy ước tiếng Việt không dấu:
--      - hop_dong_lao_dong (Lịch sử HĐ, loại, số HĐ, từ ngày, đến ngày, lương, phụ cấp, trạng thái)
--      - ca_lam_viec (Danh mục ca, giờ vào/ra, hệ số ca, ca đêm)
--      - phan_ca (Lịch phân ca nhân sự theo ngày và đơn vị)
--      - cham_cong_ngay (Dữ liệu chấm công chi tiết giờ vào/ra, OT, đi trễ về sớm theo ca)
--      - don_nghi_phep (Đơn xin nghỉ phép, các loại nghỉ, trừ quỹ phép, luồng duyệt)
--      - de_xuat_tuyen_dung (Đề xuất định biên tuyển dụng nhân sự từ các nhà máy/trang trại/chi nhánh)
--      - dm_khoa_dao_tao (Danh mục khóa đào tạo nội bộ và bên ngoài của Học viện Vinamilk)
--      - chung_chi_nhan_vien (Hồ sơ chứng chỉ: Thẻ xanh ATTP, ATLĐ, PCCC, Visa chuyên gia, ngày hết hạn)
--      - cap_phat_tai_san (Cấp phát và thu hồi trang thiết bị: Laptop, PPE, thẻ từ, xe ô tô)
--      - dinh_bien (Kế hoạch định biên nhân sự theo Đơn vị, Năm, Chức vụ và Ngân sách lương)
--      - cau_hinh_luong (Bảng tham số pháp lý tiền lương theo ngày hiệu lực)
--      - audit_log (Nhật ký kiểm toán an ninh hệ thống)
--      - su_kien_nhan_su (Dòng thời gian biến động nhân sự: tuyển dụng, thuyên chuyển, tăng lương, nghỉ việc)
--      - don_tu, bang_luong, bang_luong_chi_tiet, bieu_thue_tncn, user_scopes, documents
--   3. Bổ sung các cột thiếu vào bảng nhansu và users để code chạy 100% không lỗi.
--   4. Nạp bộ dữ liệu mẫu (Seed Data) chuẩn thực tế Vinamilk và 7 tài khoản demo.
-- ====================================================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

USE `vinamilk_hrm`;

-- ====================================================================================================
-- PHẦN 1: CẬP NHẬT CÁC CỘT THIẾU TRONG BẢNG CỐT LÕI (nhansu & users)
-- ====================================================================================================

-- 1.1 Bổ sung cột cho bảng nhansu
ALTER TABLE `nhansu` 
    ADD COLUMN IF NOT EXISTS `VungLuong` TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Vùng lương tối thiểu (1, 2, 3, 4 theo NĐ 74/2024)' AFTER `MaDV`,
    ADD COLUMN IF NOT EXISTS `MaNV_QuanLy` VARCHAR(20) NULL COMMENT 'Mã nhân viên quản lý trực tiếp' AFTER `VungLuong`,
    ADD COLUMN IF NOT EXISTS `ChucVu` VARCHAR(100) NULL COMMENT 'Tên chức vụ hiển thị nhanh' AFTER `MaCV`;

-- 1.2 Bổ sung cột cho bảng users
ALTER TABLE `users` 
    ADD COLUMN IF NOT EXISTS `RoleCode` VARCHAR(50) NULL COMMENT 'Mã vai trò chuẩn (ADMIN, BOD, HR_TAPDOAN, HR_DONVI, KETOAN_LUONG, QUANLY_TRUCTIEP, NHANVIEN)' AFTER `RoleID`,
    ADD COLUMN IF NOT EXISTS `MaDV` VARCHAR(20) NULL COMMENT 'Mã đơn vị phụ trách trực tiếp' AFTER `MaNV`;

-- ====================================================================================================
-- PHẦN 2: TẠO CÁC BẢNG NGHIỆP VỤ MỚI HỢP NHẤT (QUY ƯỚC TIẾNG VIỆT KHÔNG DẤU)
-- ====================================================================================================

-- ----------------------------------------------------------------------------------------------------
-- 2.1 Bảng hop_dong_lao_dong: Quản lý lịch sử và quá trình ký hợp đồng lao động
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `hop_dong_lao_dong`;
CREATE TABLE `hop_dong_lao_dong` (
    `id`                    BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv`                 VARCHAR(20) NOT NULL COMMENT 'Mã nhân viên ký hợp đồng',
    `so_hd`                 VARCHAR(50) NOT NULL UNIQUE COMMENT 'Số hiệu hợp đồng (VD: HDLD/2026/VNM-0001)',
    `loai_hd`               ENUM('THU_VIEC', 'XAC_DINH_THOI_HAN_12T', 'XAC_DINH_THOI_HAN_24T', 'XAC_DINH_THOI_HAN_36T', 'KHONG_XAC_DINH_THOI_HAN', 'KHOAN_VIEC', 'CONG_TAC_VIEN') NOT NULL COMMENT 'Loại hợp đồng theo BLLĐ 2019',
    `ngay_ky`               DATE NOT NULL COMMENT 'Ngày ký kết hợp đồng',
    `tu_ngay`               DATE NOT NULL COMMENT 'Ngày bắt đầu có hiệu lực',
    `den_ngay`              DATE NULL DEFAULT NULL COMMENT 'Ngày hết hạn (NULL nếu không xác định thời hạn)',
    `chuc_danh`             VARCHAR(100) NOT NULL COMMENT 'Chức danh công việc theo HĐ',
    `dia_diem_lam_viec`     VARCHAR(255) NULL COMMENT 'Địa điểm làm việc',
    `luong_can_ban`         DECIMAL(15, 2) NOT NULL DEFAULT 0.00 COMMENT 'Mức lương cơ bản thỏa thuận',
    `luong_dong_bh`         DECIMAL(15, 2) NOT NULL DEFAULT 0.00 COMMENT 'Mức lương đóng BHXH/BHYT/BHTN',
    `phu_cap_trach_nhiem`   DECIMAL(15, 2) NOT NULL DEFAULT 0.00 COMMENT 'Phụ cấp trách nhiệm',
    `phu_cap_an_ca`         DECIMAL(15, 2) NOT NULL DEFAULT 0.00 COMMENT 'Phụ cấp tiền cơm ăn ca',
    `phu_cap_khac`          DECIMAL(15, 2) NOT NULL DEFAULT 0.00 COMMENT 'Các khoản phụ cấp bổ sung khác',
    `ty_le_huong_luong`     DECIMAL(5, 2) NOT NULL DEFAULT 100.00 COMMENT '% hưởng lương (VD: 85% thử việc)',
    `nguoi_dai_dien_ky`     VARCHAR(100) NULL COMMENT 'Đại diện Vinamilk ký kết',
    `chuc_vu_nguoi_ky`      VARCHAR(100) NULL COMMENT 'Chức vụ người đại diện ký',
    `file_hop_dong`         VARCHAR(255) NULL COMMENT 'Đường dẫn file scan hợp đồng PDF',
    `trang_thai`            ENUM('HIEU_LUC', 'HET_HAN', 'DA_CHAM_DUT', 'CHO_KY', 'HUY_BO') NOT NULL DEFAULT 'HIEU_LUC' COMMENT 'Trạng thái hợp đồng',
    `ghi_chu`               TEXT NULL,
    `created_at`            DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY `idx_hdld_manv` (`ma_nv`),
    KEY `idx_hdld_loai` (`loai_hd`),
    KEY `idx_hdld_hieuluc` (`tu_ngay`, `den_ngay`),
    KEY `idx_hdld_trang_thai` (`trang_thai`),
    CONSTRAINT `fk_hdld_nhansu` FOREIGN KEY (`ma_nv`) REFERENCES `nhansu` (`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Lịch sử hợp đồng lao động nhân viên';

-- ----------------------------------------------------------------------------------------------------
-- 2.2 Bảng ca_lam_viec: Danh mục các ca làm việc tiêu chuẩn và sản xuất
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `ca_lam_viec`;
CREATE TABLE `ca_lam_viec` (
    `ma_ca`                     VARCHAR(20) PRIMARY KEY COMMENT 'Mã ca (CA_HANH_CHINH, CA_1_SANG, CA_2_CHIEU, CA_3_DEM)',
    `ten_ca`                    VARCHAR(100) NOT NULL COMMENT 'Tên ca làm việc',
    `gio_bat_dau`               TIME NOT NULL COMMENT 'Giờ bắt đầu làm việc',
    `gio_ket_thuc`              TIME NOT NULL COMMENT 'Giờ kết thúc ca làm việc',
    `gio_nghi_giua_ca_bat_dau`  TIME NULL COMMENT 'Bắt đầu nghỉ giữa ca',
    `gio_nghi_giua_ca_ket_thuc` TIME NULL COMMENT 'Kết thúc nghỉ giữa ca',
    `so_gio_cong`               DECIMAL(4, 2) NOT NULL DEFAULT 8.00 COMMENT 'Số giờ công quy chuẩn của ca',
    `he_so_ca`                  DECIMAL(4, 2) NOT NULL DEFAULT 1.00 COMMENT 'Hệ số tính lương ca (1.0 ca thường, 1.3 ca đêm)',
    `la_ca_dem`                 TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1=Ca đêm (22:00 - 06:00 tính thêm phụ cấp đêm), 0=Ca ngày',
    `trang_thai`                ENUM('HOAT_DONG', 'NGUNG_AP_DUNG') NOT NULL DEFAULT 'HOAT_DONG',
    `created_at`                DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục ca làm việc nhà máy và văn phòng';

-- ----------------------------------------------------------------------------------------------------
-- 2.3 Bảng phan_ca: Lịch phân ca chi tiết theo nhân sự và ngày làm việc
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `phan_ca`;
CREATE TABLE `phan_ca` (
    `id`                BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv`             VARCHAR(20) NOT NULL COMMENT 'Mã nhân viên',
    `ngay_lam_viec`     DATE NOT NULL COMMENT 'Ngày làm việc được xếp ca',
    `ma_ca`             VARCHAR(20) NOT NULL COMMENT 'Mã ca được phân',
    `ma_dv`             VARCHAR(20) NULL COMMENT 'Đơn vị / Phân xưởng làm việc',
    `ghi_chu`           VARCHAR(255) NULL,
    `nguoi_phan_ca`     VARCHAR(50) NULL COMMENT 'Quản lý hoặc Tổ trưởng xếp ca',
    `trang_thai`        ENUM('DA_PHAN_CA', 'DOI_CA', 'NGHI_PHEP', 'NGHI_BU') NOT NULL DEFAULT 'DA_PHAN_CA',
    `created_at`        DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_phanca_nv_ngay` (`ma_nv`, `ngay_lam_viec`),
    KEY `idx_phanca_ngay` (`ngay_lam_viec`),
    KEY `idx_phanca_maca` (`ma_ca`),
    KEY `idx_phanca_madv` (`ma_dv`),
    CONSTRAINT `fk_phanca_nhansu` FOREIGN KEY (`ma_nv`) REFERENCES `nhansu` (`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_phanca_maca` FOREIGN KEY (`ma_ca`) REFERENCES `ca_lam_viec` (`ma_ca`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Lịch phân ca làm việc của nhân sự';

-- ----------------------------------------------------------------------------------------------------
-- 2.4 Bảng cham_cong_ngay: Dữ liệu chấm công hàng ngày chi tiết theo ca
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `cham_cong_ngay`;
CREATE TABLE `cham_cong_ngay` (
    `id`                    BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv`                 VARCHAR(20) NOT NULL COMMENT 'Mã nhân viên',
    `ngay`                  DATE NOT NULL COMMENT 'Ngày chấm công',
    `ma_ca`                 VARCHAR(20) NULL COMMENT 'Mã ca làm việc thực tế',
    `gio_vao`               TIME NULL COMMENT 'Giờ quẹt thẻ vào ca',
    `gio_ra`                TIME NULL COMMENT 'Giờ quẹt thẻ ra ca',
    `di_muon_phut`          INT NOT NULL DEFAULT 0 COMMENT 'Số phút đi trễ',
    `ve_som_phut`           INT NOT NULL DEFAULT 0 COMMENT 'Số phút về sớm',
    `gio_lam_them`          DECIMAL(4, 2) NOT NULL DEFAULT 0.00 COMMENT 'Số giờ làm thêm ngoài giờ (OT)',
    `gio_lam_dem`           DECIMAL(4, 2) NOT NULL DEFAULT 0.00 COMMENT 'Số giờ làm trong khung ca đêm',
    `cong_chuan`            DECIMAL(4, 2) NOT NULL DEFAULT 1.00 COMMENT 'Công chuẩn theo lịch',
    `cong_thuc_te`          DECIMAL(4, 2) NOT NULL DEFAULT 0.00 COMMENT 'Công thực tế được hưởng (1.0, 0.5, 0)',
    `loai_cong`             ENUM('DI_LAM', 'NGHI_PHEP_NAM', 'NGHI_KHONG_LUONG', 'NGHI_OM_BHXH', 'NGHI_THAI_SAN', 'NGHI_LE_TET', 'NGHI_KHONG_PHEP', 'CONG_TAC') NOT NULL DEFAULT 'DI_LAM',
    `dia_diem_cham_cong`    VARCHAR(150) NULL COMMENT 'Vị trí máy chấm công / GPS',
    `trang_thai_duyet`      ENUM('HOP_LE', 'CHO_GIAI_TRINH', 'DA_GIAI_TRINH', 'KHOA_DU_LIEU') NOT NULL DEFAULT 'HOP_LE',
    `ghi_chu`               VARCHAR(255) NULL,
    `created_at`            DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_ccngay_nv_ngay` (`ma_nv`, `ngay`),
    KEY `idx_ccngay_ngay` (`ngay`),
    KEY `idx_ccngay_loaicong` (`loai_cong`),
    KEY `idx_ccngay_trangthai` (`trang_thai_duyet`),
    CONSTRAINT `fk_ccngay_nhansu` FOREIGN KEY (`ma_nv`) REFERENCES `nhansu` (`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_ccngay_maca` FOREIGN KEY (`ma_ca`) REFERENCES `ca_lam_viec` (`ma_ca`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chấm công chi tiết hàng ngày theo ca';

-- ----------------------------------------------------------------------------------------------------
-- 2.5 Bảng don_nghi_phep: Đơn xin nghỉ phép và luồng xét duyệt
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `don_nghi_phep`;
CREATE TABLE `don_nghi_phep` (
    `id`                BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_don`            VARCHAR(50) NOT NULL UNIQUE COMMENT 'Mã đơn (VD: LEAVE-2026-0001)',
    `ma_nv`             VARCHAR(20) NOT NULL COMMENT 'Mã nhân viên xin nghỉ',
    `loai_nghi`         ENUM('PHEP_NAM', 'VIEC_RIENG_CO_LUONG', 'OM_BHXH', 'THAI_SAN', 'KHONG_LUONG', 'NGHI_BU', 'KHAC') NOT NULL COMMENT 'Loại nghỉ',
    `tu_ngay`           DATE NOT NULL COMMENT 'Nghỉ từ ngày',
    `den_ngay`          DATE NOT NULL COMMENT 'Nghỉ đến ngày',
    `so_ngay_nghi`      DECIMAL(4, 2) NOT NULL COMMENT 'Tổng số ngày công xin nghỉ',
    `ly_do`             TEXT NOT NULL COMMENT 'Lý do xin nghỉ phép',
    `file_dinh_kem`     VARCHAR(255) NULL COMMENT 'Giấy tờ minh chứng (C70-HD, giấy ra viện, v.v.)',
    `nguoi_ban_giao`    VARCHAR(20) NULL COMMENT 'Mã nhân viên nhận bàn giao công việc',
    `nguoi_duyet`       VARCHAR(50) NULL COMMENT 'Username hoặc MaNV người duyệt',
    `ngay_duyet`        DATETIME NULL,
    `y_kien_duyet`      TEXT NULL,
    `trang_thai`        ENUM('CHO_DUYET', 'DA_DUYET', 'TU_CHOI', 'DA_HUY') NOT NULL DEFAULT 'CHO_DUYET',
    `created_at`        DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY `idx_donnghi_manv` (`ma_nv`),
    KEY `idx_donnghi_loai` (`loai_nghi`),
    KEY `idx_donnghi_ngay` (`tu_ngay`, `den_ngay`),
    KEY `idx_donnghi_trangthai` (`trang_thai`),
    CONSTRAINT `fk_donnghi_nhansu` FOREIGN KEY (`ma_nv`) REFERENCES `nhansu` (`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Đơn xin nghỉ phép nhân viên';

-- ----------------------------------------------------------------------------------------------------
-- 2.6 Bảng de_xuat_tuyen_dung: Tờ trình đề xuất bổ sung nhân sự của các đơn vị
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `de_xuat_tuyen_dung`;
CREATE TABLE `de_xuat_tuyen_dung` (
    `id`                    INT AUTO_INCREMENT PRIMARY KEY,
    `ma_de_xuat`            VARCHAR(50) NOT NULL UNIQUE COMMENT 'Mã đề xuất (VD: REQ-REC-2026-01)',
    `ma_dv`                 VARCHAR(20) NOT NULL COMMENT 'Đơn vị / Nhà máy / Chi nhánh đề xuất',
    `vi_tri_chuc_danh`      VARCHAR(150) NOT NULL COMMENT 'Vị trí công việc cần tuyển',
    `so_luong_can_tuyen`    INT NOT NULL DEFAULT 1 COMMENT 'Số lượng cần tuyển',
    `ly_do_tuyen`           ENUM('MO_RONG_SXKD', 'THAY_THE_NGHI_VIEC', 'BO_SUNG_THEO_DINH_BIEN', 'DU_AN_MOI', 'THOI_VU') NOT NULL,
    `ngay_can_nhan_su`      DATE NOT NULL COMMENT 'Hạn chót nhân sự phải vào làm',
    `muc_luong_du_kien_tu`  DECIMAL(15, 2) NULL,
    `muc_luong_du_kien_den` DECIMAL(15, 2) NULL,
    `yeu_cau_trinh_do`      VARCHAR(100) NULL,
    `yeu_cau_kinh_nghiem`   VARCHAR(255) NULL,
    `mo_ta_cong_viec`       TEXT NULL,
    `nguoi_de_xuat`         VARCHAR(20) NOT NULL COMMENT 'MaNV người lập đề xuất',
    `ngay_de_xuat`          DATE NOT NULL,
    `nguoi_phe_duyet`       VARCHAR(50) NULL,
    `ngay_phe_duyet`        DATETIME NULL,
    `trang_thai`            ENUM('CHO_DUYET', 'HR_TIEP_NHAN', 'DANG_DANG_TUYEN', 'HOAN_THANH', 'TU_CHOI', 'HUY') NOT NULL DEFAULT 'CHO_DUYET',
    `ghi_chu`               TEXT NULL,
    `created_at`            DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_dxtd_madv` (`ma_dv`),
    KEY `idx_dxtd_trangthai` (`trang_thai`),
    CONSTRAINT `fk_dxtd_donvi` FOREIGN KEY (`ma_dv`) REFERENCES `dm_donvi` (`MaDV`) ON UPDATE CASCADE,
    CONSTRAINT `fk_dxtd_nguoilap` FOREIGN KEY (`nguoi_de_xuat`) REFERENCES `nhansu` (`MaNV`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Đề xuất tuyển dụng nhân sự';

-- ----------------------------------------------------------------------------------------------------
-- 2.7 Bảng dm_khoa_dao_tao: Danh mục các khóa học Học viện Vinamilk Academy
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `dm_khoa_dao_tao`;
CREATE TABLE `dm_khoa_dao_tao` (
    `ma_khoa`                   VARCHAR(30) PRIMARY KEY COMMENT 'Mã khóa học (VD: VNM-ATTP-01, VNM-LEAN-02)',
    `ten_khoa`                  VARCHAR(200) NOT NULL COMMENT 'Tên khóa đào tạo',
    `nhom_dao_tao`              ENUM('HOI_NHAP', 'CHUYEN_MON_KY_THUAT', 'AN_TOAN_LAO_DONG', 'VE_SINH_ATTP', 'KY_NANG_MEM', 'QUAN_LY_LANH_DAO', 'CHUYEN_DOI_SO') NOT NULL,
    `thoi_luong_gio`            INT NOT NULL DEFAULT 8 COMMENT 'Thời lượng học (giờ)',
    `hinh_thuc`                 ENUM('NOI_BO', 'THUE_NGOAI', 'E_LEARNING', 'ON_THE_JOB') NOT NULL DEFAULT 'NOI_BO',
    `don_vi_dao_tao`            VARCHAR(150) NULL DEFAULT 'Vinamilk Academy',
    `chi_phi_du_kien`           DECIMAL(15, 2) DEFAULT 0.00,
    `yeu_cau_dau_ra`            VARCHAR(255) NULL,
    `co_cap_chung_chi`          TINYINT(1) NOT NULL DEFAULT 1,
    `thoi_han_chung_chi_thang`  INT NULL DEFAULT 12 COMMENT 'Hiệu lực chứng chỉ (12 tháng = 1 năm)',
    `trang_thai`                ENUM('DANG_AP_DUNG', 'TAM_DUNG', 'DONG') NOT NULL DEFAULT 'DANG_AP_DUNG',
    `created_at`                DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục khóa đào tạo Vinamilk';

-- ----------------------------------------------------------------------------------------------------
-- 2.8 Bảng chung_chi_nhan_vien: Quản lý chứng chỉ chuyên môn, ATTP, ATLĐ, Visa
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `chung_chi_nhan_vien`;
CREATE TABLE `chung_chi_nhan_vien` (
    `id`                    BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv`                 VARCHAR(20) NOT NULL COMMENT 'Mã nhân viên sở hữu chứng chỉ',
    `loai_chung_chi`        ENUM('THE_XANH_ATTP', 'ATLD_VE_SINH_LD', 'PCCC_CUU_HO', 'CHUNG_CHI_CHUYEN_MON', 'VISA_GIAY_PHEP_LD', 'BANG_LAI_XE_NANG', 'KHAC') NOT NULL,
    `ten_chung_chi`         VARCHAR(200) NOT NULL COMMENT 'Tên chứng chỉ / Giấy chứng nhận',
    `so_hieu_chung_chi`     VARCHAR(100) NULL COMMENT 'Số hiệu bằng / thẻ',
    `noi_cap`               VARCHAR(150) NULL COMMENT 'Cơ quan / Đơn vị cấp chứng chỉ',
    `ngay_cap`              DATE NOT NULL COMMENT 'Ngày cấp',
    `ngay_het_han`          DATE NULL COMMENT 'Ngày hết hạn (để cảnh báo gia hạn)',
    `file_scan`             VARCHAR(255) NULL COMMENT 'File ảnh scan / PDF',
    `trang_thai`            ENUM('CON_HIEU_LUC', 'SAP_HET_HAN', 'DA_HET_HAN', 'DA_GIA_HAN') NOT NULL DEFAULT 'CON_HIEU_LUC',
    `canh_bao_truoc_ngay`   INT NOT NULL DEFAULT 30 COMMENT 'Số ngày nhắc trước hạn',
    `ghi_chu`               TEXT NULL,
    `created_at`            DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_ccnv_manv` (`ma_nv`),
    KEY `idx_ccnv_loai` (`loai_chung_chi`),
    KEY `idx_ccnv_hethan` (`ngay_het_han`),
    KEY `idx_ccnv_trangthai` (`trang_thai`),
    CONSTRAINT `fk_ccnv_nhansu` FOREIGN KEY (`ma_nv`) REFERENCES `nhansu` (`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hồ sơ chứng chỉ an toàn và giấy phép nhân viên';

-- ----------------------------------------------------------------------------------------------------
-- 2.9 Bảng cap_phat_tai_san: Theo dõi cấp phát tài sản, PPE, thiết bị làm việc
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `cap_phat_tai_san`;
CREATE TABLE `cap_phat_tai_san` (
    `id`                    BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv`                 VARCHAR(20) NOT NULL COMMENT 'Mã nhân viên nhận tài sản',
    `loai_tai_san`          ENUM('BHLD_PPE', 'MAY_TINH_LAPTOP', 'DIEN_THOAI_SIM', 'THE_TU_RA_VAO', 'XE_O_TO_XE_MAY', 'DUNG_CU_DO_NGHE', 'KHAC') NOT NULL,
    `ten_tai_san`           VARCHAR(200) NOT NULL COMMENT 'Tên trang thiết bị / tài sản',
    `ma_dinh_danh_serial`   VARCHAR(100) NULL COMMENT 'Số serial / Service tag / Biển số',
    `so_luong`              INT NOT NULL DEFAULT 1,
    `don_vi_tinh`           VARCHAR(30) NOT NULL DEFAULT 'Cái',
    `ngay_cap_phat`         DATE NOT NULL COMMENT 'Ngày giao tài sản',
    `ngay_thu_hoi_du_kien`  DATE NULL,
    `ngay_thu_hoi_thuc_te`  DATE NULL,
    `tinh_trang_khi_cap`    VARCHAR(150) NOT NULL DEFAULT 'Mới 100%',
    `tinh_trang_khi_thu_hoi` VARCHAR(150) NULL,
    `nguoi_giao`            VARCHAR(50) NULL,
    `nguoi_nhan_ky`         VARCHAR(50) NULL,
    `trang_thai`            ENUM('DANG_SU_DUNG', 'DA_THU_HOI', 'BAO_HONG', 'BAO_MAT', 'CHO_CAP_PHAT') NOT NULL DEFAULT 'DANG_SU_DUNG',
    `ghi_chu`               TEXT NULL,
    `created_at`            DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_cpts_manv` (`ma_nv`),
    KEY `idx_cpts_loai` (`loai_tai_san`),
    KEY `idx_cpts_trangthai` (`trang_thai`),
    CONSTRAINT `fk_cpts_nhansu` FOREIGN KEY (`ma_nv`) REFERENCES `nhansu` (`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Cấp phát tài sản và trang thiết bị bảo hộ nhân viên';

-- ----------------------------------------------------------------------------------------------------
-- 2.10 Bảng dinh_bien: Định biên nhân sự hàng năm theo cơ cấu đơn vị
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `dinh_bien`;
CREATE TABLE `dinh_bien` (
    `id`                    INT AUTO_INCREMENT PRIMARY KEY,
    `ma_dv`                 VARCHAR(20) NOT NULL COMMENT 'Mã đơn vị',
    `nam`                   INT NOT NULL COMMENT 'Năm áp dụng định biên',
    `ma_cv`                 VARCHAR(10) NULL COMMENT 'Chức vụ theo định biên',
    `so_luong_dinh_bien`    INT NOT NULL DEFAULT 1 COMMENT 'Số lượng nhân sự tối đa được duyệt',
    `so_luong_hien_co`      INT NOT NULL DEFAULT 0 COMMENT 'Số nhân sự hiện tại',
    `ngan_sach_luong_nam`   DECIMAL(18, 2) NULL DEFAULT 0.00 COMMENT 'Tổng ngân sách lương năm được cấp',
    `nguoi_phe_duyet`       VARCHAR(50) NULL,
    `ngay_duyet`            DATE NULL,
    `trang_thai`            ENUM('HIEU_LUC', 'DANG_LAP_KE_HOACH', 'DA_DONG') NOT NULL DEFAULT 'HIEU_LUC',
    `ghi_chu`               TEXT NULL,
    `created_at`            DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_dinhbien_dv_nam_cv` (`ma_dv`, `nam`, `ma_cv`),
    KEY `idx_dinhbien_nam` (`nam`),
    CONSTRAINT `fk_dinhbien_donvi` FOREIGN KEY (`ma_dv`) REFERENCES `dm_donvi` (`MaDV`) ON UPDATE CASCADE,
    CONSTRAINT `fk_dinhbien_chucvu` FOREIGN KEY (`ma_cv`) REFERENCES `dm_chucvu` (`MaCV`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Định biên nhân sự và ngân sách đơn vị hàng năm';

-- ----------------------------------------------------------------------------------------------------
-- 2.11 Bảng cau_hinh_luong: Tham số pháp lý tiền lương theo ngày hiệu lực
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `cau_hinh_luong` (
    `id`                INT AUTO_INCREMENT PRIMARY KEY,
    `ma_tham_so`        VARCHAR(50) NOT NULL COMMENT 'Mã tham số duy nhất',
    `ten_tham_so`       VARCHAR(255) NOT NULL COMMENT 'Tên diễn giải chi tiết tham số',
    `gia_tri`           DECIMAL(15, 4) NOT NULL COMMENT 'Giá trị số học của tham số',
    `don_vi_tinh`       VARCHAR(50) NOT NULL DEFAULT 'VND' COMMENT 'Đơn vị tính (VND, %, NGUOI, HESO)',
    `ngay_hieu_luc`     DATE NOT NULL COMMENT 'Ngày bắt đầu có hiệu lực pháp lý',
    `ngay_het_han`      DATE NULL DEFAULT NULL COMMENT 'Ngày hết hiệu lực (NULL = Hiện hành)',
    `van_ban_phap_ly`   VARCHAR(255) NULL COMMENT 'Căn cứ pháp lý',
    `ghi_chu`           TEXT NULL,
    `created_at`        DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_tham_so_hieu_luc` (`ma_tham_so`, `ngay_hieu_luc`),
    KEY `idx_cau_hinh_ngay` (`ngay_hieu_luc`, `ngay_het_han`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng tham số pháp lý tiền lương theo ngày hiệu lực';

-- ----------------------------------------------------------------------------------------------------
-- 2.12 Bảng audit_log: Nhật ký kiểm toán an ninh và tuân thủ
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `audit_log` (
    `id`            BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv`         VARCHAR(50) NULL COMMENT 'Mã nhân viên thực hiện thao tác',
    `username`      VARCHAR(100) NOT NULL COMMENT 'Tên đăng nhập hệ thống',
    `hanh_dong`     VARCHAR(100) NOT NULL COMMENT 'Hành động: CREATE, UPDATE, DELETE, APPROVE, REJECT, CALCULATE_PAYROLL, LOGIN',
    `bang_tac_dong` VARCHAR(100) NOT NULL COMMENT 'Tên bảng bị tác động',
    `ban_ghi_id`    VARCHAR(100) NOT NULL COMMENT 'ID của bản ghi',
    `du_lieu_cu`    LONGTEXT NULL COMMENT 'Dữ liệu trước thay đổi (JSON)',
    `du_lieu_moi`   LONGTEXT NULL COMMENT 'Dữ liệu sau thay đổi (JSON)',
    `ip_address`    VARCHAR(50) NOT NULL DEFAULT '127.0.0.1',
    `user_agent`    VARCHAR(500) NULL,
    `thoi_gian`     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_audit_user` (`username`),
    KEY `idx_audit_action` (`hanh_dong`),
    KEY `idx_audit_table` (`bang_tac_dong`, `ban_ghi_id`),
    KEY `idx_audit_time` (`thoi_gian`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Nhật ký kiểm toán an ninh hệ thống';

-- ----------------------------------------------------------------------------------------------------
-- 2.13 Bảng su_kien_nhan_su: Biến động nhân sự (SCD Type 2 / Analytics Timeline)
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `su_kien_nhan_su`;
CREATE TABLE `su_kien_nhan_su` (
    `id`                BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_nv`             VARCHAR(20) NOT NULL COMMENT 'Mã nhân viên phát sinh biến động',
    `loai_su_kien`      ENUM('TUYEN_MOI', 'HET_THU_VIEC', 'KY_HOP_DONG', 'THUYEN_CHUYEN_DON_VI', 'BO_NHIEM_CHUC_VU', 'MIEN_NHIEM', 'TANG_LUONG', 'KHEN_THUONG', 'KY_LUAT', 'NGHI_VIEC_TU_NGUYEN', 'SA_THAI', 'NGHI_HUU') NOT NULL,
    `ngay_hieu_luc`     DATE NOT NULL COMMENT 'Ngày sự kiện có hiệu lực',
    `don_vi_cu`         VARCHAR(20) NULL COMMENT 'Đơn vị cũ',
    `don_vi_moi`        VARCHAR(20) NULL COMMENT 'Đơn vị mới',
    `chuc_vu_cu`        VARCHAR(10) NULL COMMENT 'Chức vụ cũ',
    `chuc_vu_moi`       VARCHAR(10) NULL COMMENT 'Chức vụ mới',
    `luong_cu`          DECIMAL(15, 2) NULL COMMENT 'Mức lương cũ',
    `luong_moi`         DECIMAL(15, 2) NULL COMMENT 'Mức lương mới',
    `he_so_cu`          DECIMAL(5, 2) NULL,
    `he_so_moi`         DECIMAL(5, 2) NULL,
    `so_quyet_dinh`     VARCHAR(100) NULL COMMENT 'Số quyết định / tờ trình',
    `ngay_quyet_dinh`   DATE NULL,
    `nguoi_ky`          VARCHAR(100) NULL,
    `ly_do_bien_dong`   TEXT NULL,
    `nguoi_ghi_nhan`    VARCHAR(50) NULL,
    `created_at`        DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_skns_manv` (`ma_nv`),
    KEY `idx_skns_loai` (`loai_su_kien`),
    KEY `idx_skns_ngay` (`ngay_hieu_luc`),
    CONSTRAINT `fk_skns_nhansu` FOREIGN KEY (`ma_nv`) REFERENCES `nhansu` (`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_skns_dv_cu` FOREIGN KEY (`don_vi_cu`) REFERENCES `dm_donvi` (`MaDV`) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT `fk_skns_dv_moi` FOREIGN KEY (`don_vi_moi`) REFERENCES `dm_donvi` (`MaDV`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Nhật ký biến động nhân sự phục vụ phân tích HR';

-- ----------------------------------------------------------------------------------------------------
-- 2.14 Bảng don_tu: Đơn từ tổng hợp dùng trong ApprovalModel.php
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `don_tu`;
CREATE TABLE `don_tu` (
    `id`            BIGINT AUTO_INCREMENT PRIMARY KEY,
    `ma_don`        VARCHAR(50) NOT NULL UNIQUE,
    `ma_nv`         VARCHAR(20) NOT NULL,
    `loai_don`      ENUM('NGHI_PHEP', 'CONG_TAC', 'LAM_THEM_GIO', 'XAC_NHAN_CONG', 'DE_XUAT_TANG_LUONG', 'NGHI_VIEC') NOT NULL DEFAULT 'NGHI_PHEP',
    `tieu_de`       VARCHAR(255) NOT NULL,
    `noi_dung`      TEXT NOT NULL,
    `tu_ngay`       DATETIME NULL,
    `den_ngay`      DATETIME NULL,
    `so_ngay`       DECIMAL(4, 2) DEFAULT 1.00,
    `trang_thai`    ENUM('CHO_DUYET', 'DA_DUYET', 'TU_CHOI', 'DA_HUY') NOT NULL DEFAULT 'CHO_DUYET',
    `nguoi_duyet`   VARCHAR(50) NULL,
    `ngay_duyet`    DATETIME NULL,
    `y_kien_duyet`  TEXT NULL,
    `file_dinh_kem` VARCHAR(255) NULL,
    `created_at`    DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY `idx_dontu_manv` (`ma_nv`),
    KEY `idx_dontu_trangthai` (`trang_thai`),
    CONSTRAINT `fk_dontu_nhansu` FOREIGN KEY (`ma_nv`) REFERENCES `nhansu` (`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Đơn từ và quy trình phê duyệt trực tuyến';

-- ----------------------------------------------------------------------------------------------------
-- 2.15 Bảng bang_luong & bang_luong_chi_tiet: Dùng trong PayrollModel.php
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `bang_luong` (
    `id`                INT AUTO_INCREMENT PRIMARY KEY,
    `ky_luong`          VARCHAR(7) NOT NULL COMMENT 'Kỳ lương YYYY-MM (VD: 2026-10)',
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
    `ten_dv`                VARCHAR(150) NULL,
    `vung_luong`            TINYINT NOT NULL DEFAULT 1,
    `luong_p1_hop_dong`     DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `luong_p2_hieu_suat`    DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `luong_p3_kpi`          DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `phu_cap`               DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `luong_ot`              DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `tong_thu_nhap`         DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `luong_dong_bh`         DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `bhxh_nv`               DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `bhyt_nv`               DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `bhtn_nv`               DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `tong_bao_hiem_nv`      DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `giam_tru_ban_than`     DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `giam_tru_phu_thuoc`    DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `thu_nhap_tinh_thue`    DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `thue_tncn`             DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `thuc_linh`             DECIMAL(15, 2) NOT NULL DEFAULT 0,
    `created_at`            DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_blct_bl` (`bang_luong_id`),
    KEY `idx_blct_nv` (`ma_nv`),
    CONSTRAINT `fk_blct_bangluong` FOREIGN KEY (`bang_luong_id`) REFERENCES `bang_luong` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng lương chi tiết từng nhân viên';

-- ----------------------------------------------------------------------------------------------------
-- 2.16 Bảng bieu_thue_tncn: Biểu thuế TNCN lũy tiến 7 bậc
-- ----------------------------------------------------------------------------------------------------
DROP TABLE IF EXISTS `bieu_thue_tncn`;
CREATE TABLE `bieu_thue_tncn` (
    `id`            INT AUTO_INCREMENT PRIMARY KEY,
    `bac_thue`      INT NOT NULL,
    `thu_nhap_tu`   DECIMAL(15, 2) NOT NULL,
    `thu_nhap_den`  DECIMAL(15, 2) NULL,
    `thue_suat`     DECIMAL(5, 2) NOT NULL,
    `so_tru_nhanh`  DECIMAL(15, 2) NOT NULL DEFAULT 0.00,
    `ngay_hieu_luc` DATE NOT NULL DEFAULT '2009-01-01',
    `ngay_het_han`  DATE NULL DEFAULT NULL,
    UNIQUE KEY `uk_bieu_thue_bac_ngay` (`bac_thue`, `ngay_hieu_luc`),
    KEY `idx_bieu_thue_bac` (`bac_thue`),
    KEY `idx_bieu_thue_ngay` (`ngay_hieu_luc`, `ngay_het_han`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Biểu thuế TNCN lũy tiến 7 bậc';

-- ----------------------------------------------------------------------------------------------------
-- 2.17 Bảng user_scopes: Dùng trong UserScope.php
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `user_scopes` (
    `id`                INT AUTO_INCREMENT PRIMARY KEY,
    `user_id`           INT NOT NULL,
    `org_unit_id`       VARCHAR(20) NOT NULL,
    `include_sub_units` TINYINT(1) NOT NULL DEFAULT 1,
    `created_at`        DATETIME DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_uscope_user` (`user_id`),
    KEY `idx_uscope_unit` (`org_unit_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Phạm vi phân quyền cây đơn vị cho user';

-- ----------------------------------------------------------------------------------------------------
-- 2.18 Bảng documents: Dùng trong Document.php
-- ----------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `documents` (
    `id`            BIGINT AUTO_INCREMENT PRIMARY KEY,
    `employee_id`   VARCHAR(20) NOT NULL,
    `document_name` VARCHAR(255) NOT NULL,
    `document_type` VARCHAR(100) NOT NULL,
    `file_path`     VARCHAR(500) NOT NULL,
    `file_size_kb`  INT NOT NULL DEFAULT 0,
    `expiry_date`   DATE NULL,
    `created_at`    DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME NULL,
    KEY `idx_doc_emp` (`employee_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Tài liệu đính kèm hồ sơ nhân viên';

-- ====================================================================================================
-- PHẦN 3: NẠP DỮ LIỆU SEED TỐI THIỂU CHUẨN THỰC TẾ VINAMILK
-- ====================================================================================================

-- 3.1 Nạp tham số pháp lý cau_hinh_luong
INSERT INTO `cau_hinh_luong` (`ma_tham_so`, `ten_tham_so`, `gia_tri`, `don_vi_tinh`, `ngay_hieu_luc`, `ngay_het_han`, `van_ban_phap_ly`, `ghi_chu`)
VALUES
('LUONG_CO_SO', 'Mức lương cơ sở áp dụng đóng BHXH/BHYT', 2340000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 73/2024/NĐ-CP', 'Áp dụng từ 01/07/2024'),
('LUONG_TT_VUNG_1', 'Lương tối thiểu Vùng I (TP.HCM, Bình Dương, Hà Nội)', 4960000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 74/2024/NĐ-CP', 'Áp dụng Trụ sở & Khối Nhà máy Mega Bình Dương'),
('LUONG_TT_VUNG_2', 'Lương tối thiểu Vùng II (Đà Nẵng, Cần Thơ, Nha Trang, Tiên Sơn)', 4410000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 74/2024/NĐ-CP', 'Áp dụng Chi nhánh & Nhà máy Tiên Sơn'),
('LUONG_TT_VUNG_3', 'Lương tối thiểu Vùng III (Tây Ninh, Nghệ An, Lâm Đồng)', 3860000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 74/2024/NĐ-CP', 'Áp dụng Trang trại Green Farm'),
('LUONG_TT_VUNG_4', 'Lương tối thiểu Vùng IV (Khu vực nông thôn còn lại)', 3450000.0000, 'VND', '2024-07-01', NULL, 'Nghị định 74/2024/NĐ-CP', 'Địa bàn vùng sâu'),
('TYLE_BHXH_NV', 'Tỷ lệ trích nộp BHXH người lao động', 0.0800, '%', '2020-01-01', NULL, 'Luật BHXH 2014', '8% trên quỹ lương đóng bảo hiểm'),
('TYLE_BHYT_NV', 'Tỷ lệ trích nộp BHYT người lao động', 0.0150, '%', '2020-01-01', NULL, 'Luật BHYT', '1.5% trên quỹ lương đóng bảo hiểm'),
('TYLE_BHTN_NV', 'Tỷ lệ trích nộp BHTN người lao động', 0.0100, '%', '2020-01-01', NULL, 'Luật Việc làm 2013', '1.0% trên quỹ lương đóng bảo hiểm'),
('TYLE_BHXH_DN', 'Tỷ lệ trích nộp BHXH doanh nghiệp chịu', 0.1750, '%', '2020-01-01', NULL, 'Luật BHXH 2014', '17.5% quỹ lương'),
('TYLE_BHYT_DN', 'Tỷ lệ trích nộp BHYT doanh nghiệp chịu', 0.0300, '%', '2020-01-01', NULL, 'Luật BHYT', '3% quỹ lương'),
('TYLE_BHTN_DN', 'Tỷ lệ trích nộp BHTN doanh nghiệp chịu', 0.0100, '%', '2020-01-01', NULL, 'Luật Việc làm 2013', '1% quỹ lương'),
('TYLE_KINHPHI_CD', 'Tỷ lệ kinh phí công đoàn doanh nghiệp chịu', 0.0200, '%', '2020-01-01', NULL, 'Luật Công đoàn', '2% quỹ lương'),
('GIAM_TRU_BAN_THAN', 'Mức giảm trừ gia cảnh bản thân', 11000000.0000, 'VND', '2020-07-01', NULL, 'Nghị quyết 954/2020/UBTVQH14', '11.000.000 đ/tháng'),
('GIAM_TRU_PHU_THUOC', 'Mức giảm trừ gia cảnh mỗi người phụ thuộc', 4400000.0000, 'VND', '2020-07-01', NULL, 'Nghị quyết 954/2020/UBTVQH14', '4.400.000 đ/tháng/người'),
('TRAN_BHXH_HE_SO', 'Hệ số trần đóng BHXH/BHYT (lần lương cơ sở)', 20.0000, 'HESO', '2020-01-01', NULL, 'Luật BHXH', 'Tối đa 20 lần mức lương cơ sở = 46.800.000đ'),
('TRAN_BHTN_HE_SO', 'Hệ số trần đóng BHTN (lần lương tối thiểu vùng)', 20.0000, 'HESO', '2020-01-01', NULL, 'Luật Việc làm', 'Tối đa 20 lần lương tối thiểu vùng')
ON DUPLICATE KEY UPDATE `gia_tri` = VALUES(`gia_tri`), `ten_tham_so` = VALUES(`ten_tham_so`);

-- 3.2 Nạp biểu thuế TNCN 7 bậc
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

-- 3.3 Nạp danh mục ca làm việc tiêu chuẩn của Nhà máy & Khối văn phòng
INSERT INTO `ca_lam_viec` (`ma_ca`, `ten_ca`, `gio_bat_dau`, `gio_ket_thuc`, `gio_nghi_giua_ca_bat_dau`, `gio_nghi_giua_ca_ket_thuc`, `so_gio_cong`, `he_so_ca`, `la_ca_dem`, `trang_thai`)
VALUES
('CA_HANH_CHINH', 'Ca Hành Chính Văn Phòng (08:00 - 17:00)', '08:00:00', '17:00:00', '12:00:00', '13:00:00', 8.00, 1.00, 0, 'HOAT_DONG'),
('CA_1_SANG', 'Ca 1 Sáng Sản Xuất (06:00 - 14:00)', '06:00:00', '14:00:00', '11:00:00', '11:30:00', 8.00, 1.00, 0, 'HOAT_DONG'),
('CA_2_CHIEU', 'Ca 2 Chiều Sản Xuất (14:00 - 22:00)', '14:00:00', '22:00:00', '18:00:00', '18:30:00', 8.00, 1.00, 0, 'HOAT_DONG'),
('CA_3_DEM', 'Ca 3 Đêm Sản Xuất (22:00 - 06:00)', '22:00:00', '06:00:00', '02:00:00', '02:30:00', 8.00, 1.30, 1, 'HOAT_DONG'),
('CA_GAY_TT', 'Ca Gãy Vắt Sữa Trang Trại (04:30 - 08:30 & 14:30 - 18:30)', '04:30:00', '18:30:00', '08:30:00', '14:30:00', 8.00, 1.15, 0, 'HOAT_DONG')
ON DUPLICATE KEY UPDATE `ten_ca` = VALUES(`ten_ca`);

-- 3.4 Nạp danh mục khóa đào tạo Vinamilk Academy
INSERT INTO `dm_khoa_dao_tao` (`ma_khoa`, `ten_khoa`, `nhom_dao_tao`, `thoi_luong_gio`, `hinh_thuc`, `don_vi_dao_tao`, `chi_phi_du_kien`, `yeu_cau_dau_ra`, `co_cap_chung_chi`, `thoi_han_chung_chi_thang`, `trang_thai`)
VALUES
('VNM-HOINHAP-01', 'Đào tạo Định hướng Hội nhập Văn hóa Doanh nghiệp Vinamilk', 'HOI_NHAP', 16, 'NOI_BO', 'Vinamilk Academy', 0.00, 'Đạt bài kiểm tra 80/100', 1, 36, 'DANG_AP_DUNG'),
('VNM-ATTP-01', 'Quy chuẩn Thực hành Vệ sinh An toàn Thực phẩm & HACCP/ISO 22000', 'VE_SINH_ATTP', 8, 'NOI_BO', 'Ban Đảm bảo Chất lượng QA', 500000.00, 'Cấp Thẻ Xanh ATTP', 1, 12, 'DANG_AP_DUNG'),
('VNM-ATLD-01', 'Huấn luyện An toàn Vệ sinh Lao động Nhóm 3 (Vận hành Máy móc, Hóa chất)', 'AN_TOAN_LAO_DONG', 24, 'THUE_NGOAI', 'Trung tâm ATLĐ TP.HCM', 1200000.00, 'Cấp Chứng chỉ ATLĐ', 1, 24, 'DANG_AP_DUNG'),
('VNM-LEAN-01', 'Phương pháp Quản trị Tinh gọn Lean Six Sigma trong Nhà máy Mega', 'CHUYEN_MON_KY_THUAT', 40, 'NOI_BO', 'Khối Sản xuất', 2500000.00, 'Chứng chỉ Green Belt Lean', 1, 36, 'DANG_AP_DUNG'),
('VNM-DIGITAL-01', 'Chuyển đổi số và Ứng dụng AI/IoT trong Giám sát Bò sữa & Chuỗi lạnh', 'CHUYEN_DOI_SO', 16, 'E_LEARNING', 'Vinamilk Academy', 800000.00, 'Chứng chỉ Hoàn thành', 1, 12, 'DANG_AP_DUNG')
ON DUPLICATE KEY UPDATE `ten_khoa` = VALUES(`ten_khoa`);

-- 3.5 Cập nhật thông tin vùng lương và quản lý mẫu cho nhansu
UPDATE `nhansu` SET `VungLuong` = 1, `ChucVu` = 'Tổng Giám Đốc' WHERE `MaNV` = 'VNM-0001';
UPDATE `nhansu` SET `VungLuong` = 1, `ChucVu` = 'Giám Đốc Sản Xuất', `MaNV_QuanLy` = 'VNM-0001' WHERE `MaNV` = 'VNM-0002';
UPDATE `nhansu` SET `VungLuong` = 1, `ChucVu` = 'Trưởng Phòng Nhân Sự', `MaNV_QuanLy` = 'VNM-0001' WHERE `MaNV` = 'VNM-0004';
UPDATE `nhansu` SET `VungLuong` = 1, `ChucVu` = 'Kế Toán Trưởng', `MaNV_QuanLy` = 'VNM-0001' WHERE `MaNV` = 'VNM-0024';
UPDATE `nhansu` SET `VungLuong` = 1, `ChucVu` = 'Kỹ Sư Vận Hành Tetra Pak', `MaNV_QuanLy` = 'VNM-0002' WHERE `MaNV` = 'VNM-0016';

-- 3.6 Seed Hợp đồng lao động mẫu
INSERT INTO `hop_dong_lao_dong` (`ma_nv`, `so_hd`, `loai_hd`, `ngay_ky`, `tu_ngay`, `den_ngay`, `chuc_danh`, `dia_diem_lam_viec`, `luong_can_ban`, `luong_dong_bh`, `phu_cap_trach_nhiem`, `phu_cap_an_ca`, `trang_thai`)
VALUES
('VNM-0001', 'HDLD/2020/VNM-CEO', 'KHONG_XAC_DINH_THOI_HAN', '2020-01-01', '2020-01-01', NULL, 'Tổng Giám Đốc', 'Trụ sở Quận 7, TP.HCM', 120000000.00, 46800000.00, 30000000.00, 1500000.00, 'HIEU_LUC'),
('VNM-0002', 'HDLD/2022/VNM-SX01', 'KHONG_XAC_DINH_THOI_HAN', '2022-03-01', '2022-03-01', NULL, 'Giám Đốc Sản Xuất Mega', 'NM Mega Bình Dương', 45000000.00, 35000000.00, 8000000.00, 1500000.00, 'HIEU_LUC'),
('VNM-0004', 'HDLD/2023/VNM-HR01', 'XAC_DINH_THOI_HAN_36T', '2023-06-01', '2023-06-01', '2026-05-31', 'Trưởng Phòng Nhân Sự NM', 'NM Mega Bình Dương', 25000000.00, 20000000.00, 4000000.00, 1500000.00, 'HIEU_LUC'),
('VNM-0016', 'HDLD/2024/VNM-VH16', 'XAC_DINH_THOI_HAN_12T', '2024-10-01', '2024-10-01', '2025-09-30', 'Kỹ Sư Vận Hành Chiết Rót', 'NM Mega Bình Dương', 16000000.00, 14000000.00, 2000000.00, 1500000.00, 'HIEU_LUC')
ON DUPLICATE KEY UPDATE `so_hd` = VALUES(`so_hd`);

-- 3.7 Seed Phân ca & Chấm công ngày mẫu
INSERT INTO `phan_ca` (`ma_nv`, `ngay_lam_viec`, `ma_ca`, `ma_dv`, `nguoi_phan_ca`, `trang_thai`)
VALUES
('VNM-0016', '2026-10-12', 'CA_1_SANG', 'NM_BD_XUONG1', 'quanly.sx', 'DA_PHAN_CA'),
('VNM-0002', '2026-10-12', 'CA_HANH_CHINH', 'NM_BD', 'admin', 'DA_PHAN_CA')
ON DUPLICATE KEY UPDATE `ma_ca` = VALUES(`ma_ca`);

INSERT INTO `cham_cong_ngay` (`ma_nv`, `ngay`, `ma_ca`, `gio_vao`, `gio_ra`, `di_muon_phut`, `ve_som_phut`, `gio_lam_them`, `cong_thuc_te`, `loai_cong`, `trang_thai_duyet`)
VALUES
('VNM-0016', '2026-10-12', 'CA_1_SANG', '05:55:00', '14:05:00', 0, 0, 0.00, 1.00, 'DI_LAM', 'HOP_LE'),
('VNM-0002', '2026-10-12', 'CA_HANH_CHINH', '07:58:00', '17:15:00', 0, 0, 0.00, 1.00, 'DI_LAM', 'HOP_LE')
ON DUPLICATE KEY UPDATE `gio_vao` = VALUES(`gio_vao`);

-- 3.8 Seed Đơn nghỉ phép & Đơn từ mẫu
INSERT INTO `don_nghi_phep` (`ma_don`, `ma_nv`, `loai_nghi`, `tu_ngay`, `den_ngay`, `so_ngay_nghi`, `ly_do`, `nguoi_duyet`, `trang_thai`)
VALUES
('LEAVE-2026-001', 'VNM-0016', 'PHEP_NAM', '2026-10-20', '2026-10-21', 2.00, 'Nghỉ giải quyết việc gia đình', 'quanly.sx', 'CHO_DUYET')
ON DUPLICATE KEY UPDATE `ma_don` = VALUES(`ma_don`);

INSERT INTO `don_tu` (`ma_don`, `ma_nv`, `loai_don`, `tieu_de`, `noi_dung`, `tu_ngay`, `den_ngay`, `so_ngay`, `trang_thai`)
VALUES
('REQ-20261012-001', 'VNM-0016', 'NGHI_PHEP', 'Đơn xin nghỉ phép năm 2 ngày', 'Xin nghỉ phép năm việc riêng có bàn giao cho đồng nghiệp trong ca', '2026-10-20 08:00:00', '2026-10-21 17:00:00', 2.00, 'CHO_DUYET')
ON DUPLICATE KEY UPDATE `ma_don` = VALUES(`ma_don`);

-- 3.9 Seed Đề xuất tuyển dụng mẫu
INSERT INTO `de_xuat_tuyen_dung` (`ma_de_xuat`, `ma_dv`, `vi_tri_chuc_danh`, `so_luong_can_tuyen`, `ly_do_tuyen`, `ngay_can_nhan_su`, `muc_luong_du_kien_tu`, `muc_luong_du_kien_den`, `nguoi_de_xuat`, `ngay_de_xuat`, `trang_thai`)
VALUES
('REQ-REC-2026-01', 'NM_BD_XUONG1', 'Kỹ Sư Vận Hành Hệ Thống Tiệt Trùng Tetra Pak', 2, 'MO_RONG_SXKD', '2026-11-15', 15000000.00, 20000000.00, 'VNM-0002', '2026-10-10', 'CHO_DUYET')
ON DUPLICATE KEY UPDATE `ma_de_xuat` = VALUES(`ma_de_xuat`);

-- 3.10 Seed Chứng chỉ nhân viên (Thẻ xanh ATTP, ATLĐ, Visa)
INSERT INTO `chung_chi_nhan_vien` (`ma_nv`, `loai_chung_chi`, `ten_chung_chi`, `so_hieu_chung_chi`, `noi_cap`, `ngay_cap`, `ngay_het_han`, `trang_thai`, `canh_bao_truoc_ngay`)
VALUES
('VNM-0016', 'THE_XANH_ATTP', 'Thẻ Xanh Vệ Sinh An Toàn Thực Phẩm Nhà Máy Sữa', 'TX-ATTP-2026-091', 'Sở Y Tế Bình Dương', '2026-01-10', '2027-01-10', 'CON_HIEU_LUC', 30),
('VNM-0016', 'ATLD_VE_SINH_LD', 'Chứng nhận Huấn luyện An toàn Lao động Nhóm 3', 'ATLD-2025-4421', 'Vinamilk HSE Department', '2025-06-15', '2027-06-15', 'CON_HIEU_LUC', 45),
('VNM-0001', 'VISA_GIAY_PHEP_LD', 'Visa Công Tác & Thẻ APEC Doanh Nhân Quốc Tế', 'APEC-VN-998822', 'Cục Quản lý Xuất Nhập Cảnh', '2024-03-01', '2027-03-01', 'CON_HIEU_LUC', 60)
ON DUPLICATE KEY UPDATE `ten_chung_chi` = VALUES(`ten_chung_chi`);

-- 3.11 Seed Cấp phát tài sản & PPE
INSERT INTO `cap_phat_tai_san` (`ma_nv`, `loai_tai_san`, `ten_tai_san`, `ma_dinh_danh_serial`, `so_luong`, `don_vi_tinh`, `ngay_cap_phat`, `tinh_trang_khi_cap`, `trang_thai`)
VALUES
('VNM-0016', 'BHLD_PPE', 'Bộ Trang Phục Phòng Sạch Chiết Rót & Giày Chống Tĩnh Điện', 'PPE-VNM-2026-88', 2, 'Bộ', '2026-01-15', 'Mới 100%', 'DANG_SU_DUNG'),
('VNM-0016', 'THE_TU_RA_VAO', 'Thẻ Từ Thông Minh RFID Cổng Phân Xưởng Chiết Rót', 'RFID-BD-0016', 1, 'Thẻ', '2026-01-15', 'Mới 100%', 'DANG_SU_DUNG'),
('VNM-0002', 'MAY_TINH_LAPTOP', 'Laptop Dell Latitude 7440 Core i7 Doanh Nhân', 'DELL-TAG-892K11', 1, 'Máy', '2024-05-10', 'Mới 100%', 'DANG_SU_DUNG')
ON DUPLICATE KEY UPDATE `ten_tai_san` = VALUES(`ten_tai_san`);

-- 3.12 Seed Định biên nhân sự
INSERT INTO `dinh_bien` (`ma_dv`, `nam`, `ma_cv`, `so_luong_dinh_bien`, `so_luong_hien_co`, `ngan_sach_luong_nam`, `trang_thai`)
VALUES
('NM_BD', 2026, 'CV01', 1, 1, 600000000.00, 'HIEU_LUC'),
('NM_BD_XUONG1', 2026, 'CV04', 15, 12, 3500000000.00, 'HIEU_LUC'),
('PB_NHANSU', 2026, 'CV03', 8, 6, 2200000000.00, 'HIEU_LUC'),
('PB_TAICHINH', 2026, 'CV03', 10, 8, 2800000000.00, 'HIEU_LUC')
ON DUPLICATE KEY UPDATE `so_luong_dinh_bien` = VALUES(`so_luong_dinh_bien`);

-- 3.13 Seed Sự kiện biến động nhân sự (Analytics Timeline)
INSERT INTO `su_kien_nhan_su` (`ma_nv`, `loai_su_kien`, `ngay_hieu_luc`, `don_vi_cu`, `don_vi_moi`, `chuc_vu_cu`, `chuc_vu_moi`, `luong_cu`, `luong_moi`, `so_quyet_dinh`, `ly_do_bien_dong`)
VALUES
('VNM-0016', 'TUYEN_MOI', '2024-10-01', NULL, 'NM_BD_XUONG1', NULL, 'CV04', 0.00, 14000000.00, 'QD-TD-2024-101', 'Tuyển mới kỹ sư vận hành máy tiệt trùng'),
('VNM-0016', 'TANG_LUONG', '2026-04-01', 'NM_BD_XUONG1', 'NM_BD_XUONG1', 'CV04', 'CV04', 14000000.00, 16000000.00, 'QD-TL-2026-042', 'Tăng lương định kỳ theo KPI xuất sắc'),
('VNM-0002', 'BO_NHIEM_CHUC_VU', '2024-01-01', 'NM_BD', 'NM_BD', 'CV02', 'CV01', 35000000.00, 45000000.00, 'QD-BN-2024-001', 'Bổ nhiệm phụ trách điều hành khối nhà máy')
ON DUPLICATE KEY UPDATE `so_quyet_dinh` = VALUES(`so_quyet_dinh`);

-- 3.14 Cập nhật tài khoản Demo đúng với 7 vai trò chuẩn (Mật khẩu Bcrypt: Vinamilk@2026)
-- Bcrypt hash: $2y$10$tZ2E7f1eUu3o1g7e.4FwxeA5J7qV7kFf5zC9xK4m2P7y8W6n5Q1.
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

SET FOREIGN_KEY_CHECKS = 1;
