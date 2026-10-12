-- =====================================================================
-- VINAMILK HRM - Hệ thống Quản lý Nguồn nhân lực
-- Công ty Cổ phần Sữa Việt Nam (VNM)
-- =====================================================================
-- Phiên bản: 1.0 | PHP 8.x MVC + MySQL + PDO
-- Charset: utf8mb4_unicode_ci
-- Tác giả: Vinamilk HRM Development Team
-- Ngày tạo: 2026-09-28
-- =====================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = 'STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION';

-- Xóa database cũ nếu tồn tại và tạo mới
DROP DATABASE IF EXISTS `vinamilk_hrm`;
CREATE DATABASE `vinamilk_hrm` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `vinamilk_hrm`;

-- =====================================================================
-- PHẦN 1: BẢNG DANH MỤC (dm_*)
-- =====================================================================

-- ---------------------------------------------------------------------
-- 1.1 Bảng dm_donvi - Danh mục đơn vị (cấu trúc cây phân cấp)
-- ---------------------------------------------------------------------
CREATE TABLE `dm_donvi` (
    `MaDV`      VARCHAR(20)     NOT NULL    COMMENT 'Mã đơn vị (PK)',
    `TenDV`     NVARCHAR(200)   NOT NULL    COMMENT 'Tên đơn vị',
    `TenVietTat` VARCHAR(20)    DEFAULT NULL COMMENT 'Tên viết tắt',
    `CapDV`     TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Cấp đơn vị (1=Công ty, 2=Khối, 3=Phòng/NM/TT/CN)',
    `MaDV_Cha`  VARCHAR(20)     DEFAULT NULL COMMENT 'Mã đơn vị cha (self-ref)',
    `DiaChi`    NVARCHAR(500)   DEFAULT NULL COMMENT 'Địa chỉ',
    `SoDT`      VARCHAR(20)     DEFAULT NULL COMMENT 'Số điện thoại',
    `Email`     VARCHAR(100)    DEFAULT NULL COMMENT 'Email đơn vị',
    `LoaiDV`    ENUM('CongTy','Khoi','VanPhong','NhaMay','TrangTrai','ChiNhanh','Phong','TrungTam') NOT NULL DEFAULT 'Phong' COMMENT 'Loại đơn vị',
    `SoNhanSu`  INT UNSIGNED    DEFAULT 0   COMMENT 'Số nhân sự hiện tại',
    `TrangThai` TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT '1=Hoạt động, 0=Ngừng',
    `ThuTu`     INT UNSIGNED    DEFAULT 0   COMMENT 'Thứ tự hiển thị',
    `CreatedAt` DATETIME        DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaDV`),
    KEY `idx_donvi_cha` (`MaDV_Cha`),
    KEY `idx_donvi_loai` (`LoaiDV`),
    KEY `idx_donvi_cap` (`CapDV`),
    CONSTRAINT `fk_donvi_cha` FOREIGN KEY (`MaDV_Cha`) REFERENCES `dm_donvi`(`MaDV`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục đơn vị tổ chức phân cấp cây';

-- ---------------------------------------------------------------------
-- 1.2 Bảng dm_chucvu - Danh mục chức vụ
-- ---------------------------------------------------------------------
CREATE TABLE `dm_chucvu` (
    `MaCV`      VARCHAR(10)     NOT NULL    COMMENT 'Mã chức vụ (PK)',
    `TenCV`     NVARCHAR(100)   NOT NULL    COMMENT 'Tên chức vụ',
    `HeSoCV`    DECIMAL(5,2)    DEFAULT 0.00 COMMENT 'Hệ số chức vụ',
    `PhuCapCV`  DECIMAL(15,0)   DEFAULT 0   COMMENT 'Phụ cấp chức vụ (VNĐ)',
    `CapBac`    TINYINT UNSIGNED DEFAULT 0  COMMENT 'Cấp bậc (1=cao nhất)',
    `TrangThai` TINYINT UNSIGNED NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME        DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaCV`),
    KEY `idx_chucvu_capbac` (`CapBac`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục chức vụ';

-- ---------------------------------------------------------------------
-- 1.3 Bảng dm_ngach - Ngạch công chức / chức danh nghề nghiệp
-- ---------------------------------------------------------------------
CREATE TABLE `dm_ngach` (
    `MaNgach`       VARCHAR(10)     NOT NULL    COMMENT 'Mã ngạch (PK)',
    `TenNgach`      NVARCHAR(200)   NOT NULL    COMMENT 'Tên ngạch/chức danh',
    `Nhom`          VARCHAR(10)     DEFAULT NULL COMMENT 'Nhóm ngạch (A1, A2, B, C...)',
    `HeSoKhoiDiem`  DECIMAL(5,2)    DEFAULT 1.00 COMMENT 'Hệ số lương khởi điểm',
    `HeSoMax`       DECIMAL(5,2)    DEFAULT 10.00 COMMENT 'Hệ số lương tối đa',
    `TrangThai`     TINYINT UNSIGNED NOT NULL DEFAULT 1,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaNgach`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Ngạch công chức / chức danh nghề nghiệp';

-- ---------------------------------------------------------------------
-- 1.4 Bảng dm_trinhdo - Danh mục trình độ (đa loại)
-- ---------------------------------------------------------------------
CREATE TABLE `dm_trinhdo` (
    `MaTD`      VARCHAR(10)     NOT NULL    COMMENT 'Mã trình độ (PK)',
    `TenTD`     NVARCHAR(200)   NOT NULL    COMMENT 'Tên trình độ',
    `LoaiTD`    ENUM('HocVan','ChuyenMon','TinHoc','NgoaiNgu','LyLuanChinhTri') NOT NULL COMMENT 'Loại trình độ',
    `ThuTu`     INT UNSIGNED    DEFAULT 0,
    `TrangThai` TINYINT UNSIGNED NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaTD`),
    KEY `idx_trinhdo_loai` (`LoaiTD`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục trình độ đa loại';

-- ---------------------------------------------------------------------
-- 1.5 Bảng dm_ktkl - Danh mục hình thức khen thưởng / kỷ luật
-- ---------------------------------------------------------------------
CREATE TABLE `dm_ktkl` (
    `MaKTKL`        VARCHAR(10)     NOT NULL    COMMENT 'Mã hình thức KTKL (PK)',
    `Loai`          ENUM('KhenThuong','KyLuat') NOT NULL COMMENT 'Khen thưởng hoặc Kỷ luật',
    `TenHinhThuc`   NVARCHAR(200)   NOT NULL    COMMENT 'Tên hình thức',
    `MucThuongPhat` DECIMAL(15,0)   DEFAULT 0   COMMENT 'Mức thưởng (+) hoặc phạt (-) VNĐ',
    `MoTa`          NVARCHAR(500)   DEFAULT NULL,
    `TrangThai`     TINYINT UNSIGNED NOT NULL DEFAULT 1,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaKTKL`),
    KEY `idx_ktkl_loai` (`Loai`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục hình thức khen thưởng / kỷ luật';

-- =====================================================================
-- PHẦN 2: BẢNG NHÂN SỰ LÕI
-- =====================================================================

-- ---------------------------------------------------------------------
-- 2.1 Bảng nhansu - Hồ sơ nhân viên (kế thừa bảng soyeu HUHA)
-- ---------------------------------------------------------------------
CREATE TABLE `nhansu` (
    `MaNV`              VARCHAR(10)     NOT NULL    COMMENT 'Mã nhân viên (PK, VD: VNM-0001)',
    `HoTen`             NVARCHAR(100)   NOT NULL    COMMENT 'Họ và tên đầy đủ',
    `TenThuongGoi`      NVARCHAR(50)    DEFAULT NULL COMMENT 'Tên thường gọi',
    `NgaySinh`          DATE            NOT NULL    COMMENT 'Ngày sinh',
    `GioiTinh`          ENUM('Nam','Nữ') NOT NULL   COMMENT 'Giới tính',
    `SoCMND_CCCD`       VARCHAR(20)     NOT NULL    COMMENT 'Số CMND/CCCD',
    `NgayCap`           DATE            DEFAULT NULL COMMENT 'Ngày cấp CMND/CCCD',
    `NoiCap`            NVARCHAR(200)   DEFAULT NULL COMMENT 'Nơi cấp CMND/CCCD',
    `QueQuan`           NVARCHAR(300)   DEFAULT NULL COMMENT 'Quê quán',
    `NoiDKKTTru`        NVARCHAR(300)   DEFAULT NULL COMMENT 'Nơi đăng ký hộ khẩu thường trú',
    `DiaChiHienTai`     NVARCHAR(300)   DEFAULT NULL COMMENT 'Địa chỉ hiện tại',
    `DanToc`            NVARCHAR(50)    DEFAULT N'Kinh' COMMENT 'Dân tộc',
    `TonGiao`           NVARCHAR(50)    DEFAULT N'Không' COMMENT 'Tôn giáo',
    `TinhTrangHonNhan`  ENUM('Độc thân','Đã kết hôn','Ly hôn','Góa') DEFAULT 'Độc thân' COMMENT 'Tình trạng hôn nhân',
    `Email`             VARCHAR(100)    DEFAULT NULL COMMENT 'Email công việc',
    `SoDienThoai`       VARCHAR(20)     DEFAULT NULL COMMENT 'Số điện thoại',
    `AnhChanDung`       VARCHAR(255)    DEFAULT NULL COMMENT 'Đường dẫn ảnh chân dung',
    `NgayVaoLam`        DATE            NOT NULL    COMMENT 'Ngày vào làm',
    `MaDV`              VARCHAR(20)     NOT NULL    COMMENT 'Mã đơn vị hiện tại (FK)',
    `MaCV`              VARCHAR(10)     DEFAULT NULL COMMENT 'Mã chức vụ hiện tại (FK)',
    `MaNgach`           VARCHAR(10)     DEFAULT NULL COMMENT 'Mã ngạch hiện tại (FK)',
    `HeSoLuongHienTai`  DECIMAL(5,2)    DEFAULT 1.00 COMMENT 'Hệ số lương hiện tại',
    `BacLuongHienTai`   INT UNSIGNED    DEFAULT 1   COMMENT 'Bậc lương hiện tại',
    `TrinhDoHocVan`     NVARCHAR(50)    DEFAULT NULL COMMENT 'Trình độ học vấn cao nhất',
    `ChuyenNganh`       NVARCHAR(200)   DEFAULT NULL COMMENT 'Chuyên ngành đào tạo',
    `TruongDaoTao`      NVARCHAR(200)   DEFAULT NULL COMMENT 'Trường đào tạo',
    `DangVien`          TINYINT(1)      DEFAULT 0   COMMENT '1=Đảng viên, 0=Không',
    `DoanVien`          TINYINT(1)      DEFAULT 0   COMMENT '1=Đoàn viên CĐ, 0=Không',
    `SoTaiKhoanNH`      VARCHAR(30)     DEFAULT NULL COMMENT 'Số tài khoản ngân hàng',
    `NganHang`          NVARCHAR(100)   DEFAULT NULL COMMENT 'Tên ngân hàng',
    `MaSoThue`          VARCHAR(20)     DEFAULT NULL COMMENT 'Mã số thuế TNCN',
    `SoNguoiPhuThuoc`   INT UNSIGNED    DEFAULT 0   COMMENT 'Số người phụ thuộc',
    `LienHeKhanCap`     NVARCHAR(300)   DEFAULT NULL COMMENT 'Liên hệ khẩn cấp (Tên - QH - SĐT)',
    `ViTriLuuHoSo`      NVARCHAR(200)   DEFAULT NULL COMMENT 'Vị trí lưu hồ sơ giấy',
    `LoaiHopDong`       NVARCHAR(50)    DEFAULT NULL COMMENT 'Loại hợp đồng hiện tại',
    `NgayHetHanHD`      DATE            DEFAULT NULL COMMENT 'Ngày hết hạn hợp đồng',
    `LuongBaoHiem`      DECIMAL(15,0)   DEFAULT 0   COMMENT 'Mức lương đóng BHXH',
    `TrangThai`         TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT '1=Đang làm, 2=Thử việc, 3=Nghỉ việc, 4=Nghỉ hưu',
    `GhiChu`            TEXT            DEFAULT NULL COMMENT 'Ghi chú bổ sung',
    `CreatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaNV`),
    UNIQUE KEY `uk_nhansu_cccd` (`SoCMND_CCCD`),
    UNIQUE KEY `uk_nhansu_email` (`Email`),
    KEY `idx_nhansu_donvi` (`MaDV`),
    KEY `idx_nhansu_chucvu` (`MaCV`),
    KEY `idx_nhansu_ngach` (`MaNgach`),
    KEY `idx_nhansu_trangthai` (`TrangThai`),
    KEY `idx_nhansu_hoten` (`HoTen`),
    KEY `idx_nhansu_ngayvaolam` (`NgayVaoLam`),
    CONSTRAINT `fk_nhansu_donvi` FOREIGN KEY (`MaDV`) REFERENCES `dm_donvi`(`MaDV`) ON UPDATE CASCADE,
    CONSTRAINT `fk_nhansu_chucvu` FOREIGN KEY (`MaCV`) REFERENCES `dm_chucvu`(`MaCV`) ON UPDATE CASCADE,
    CONSTRAINT `fk_nhansu_ngach` FOREIGN KEY (`MaNgach`) REFERENCES `dm_ngach`(`MaNgach`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng nhân sự lõi - kế thừa soyeu HUHA HRM';

-- =====================================================================
-- PHẦN 3: BẢNG QUÁ TRÌNH & BIẾN ĐỘNG (qt_*)
-- =====================================================================

-- ---------------------------------------------------------------------
-- 3.1 Bảng qt_congtac - Quá trình công tác
-- ---------------------------------------------------------------------
CREATE TABLE `qt_congtac` (
    `ID`            INT UNSIGNED    AUTO_INCREMENT  COMMENT 'ID tự tăng (PK)',
    `MaNV`          VARCHAR(10)     NOT NULL        COMMENT 'Mã nhân viên (FK)',
    `TuNgay`        DATE            NOT NULL        COMMENT 'Từ ngày',
    `DenNgay`       DATE            DEFAULT NULL    COMMENT 'Đến ngày (NULL=hiện tại)',
    `DonViCongTac`  NVARCHAR(200)   NOT NULL        COMMENT 'Đơn vị công tác',
    `ChucVu`        NVARCHAR(100)   DEFAULT NULL    COMMENT 'Chức vụ/vị trí',
    `CongViecChinh` NVARCHAR(500)   DEFAULT NULL    COMMENT 'Công việc chính',
    `GhiChu`        NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`ID`),
    KEY `idx_qtct_manv` (`MaNV`),
    KEY `idx_qtct_tungay` (`TuNgay`),
    CONSTRAINT `fk_qtct_nhansu` FOREIGN KEY (`MaNV`) REFERENCES `nhansu`(`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quá trình công tác';

-- ---------------------------------------------------------------------
-- 3.2 Bảng qt_bonhiem - Quá trình bổ nhiệm
-- ---------------------------------------------------------------------
CREATE TABLE `qt_bonhiem` (
    `ID`                INT UNSIGNED    AUTO_INCREMENT,
    `MaNV`              VARCHAR(10)     NOT NULL,
    `SoQuyetDinh`       VARCHAR(50)     NOT NULL    COMMENT 'Số quyết định bổ nhiệm',
    `NgayQuyetDinh`     DATE            NOT NULL    COMMENT 'Ngày ký quyết định',
    `ChucVuCu`          NVARCHAR(100)   DEFAULT NULL COMMENT 'Chức vụ cũ',
    `ChucVuMoi`         NVARCHAR(100)   NOT NULL    COMMENT 'Chức vụ mới',
    `DonVi`             NVARCHAR(200)   DEFAULT NULL COMMENT 'Đơn vị bổ nhiệm',
    `NguoiKy`           NVARCHAR(100)   DEFAULT NULL COMMENT 'Người ký quyết định',
    `GhiChu`            NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`ID`),
    KEY `idx_qtbn_manv` (`MaNV`),
    CONSTRAINT `fk_qtbn_nhansu` FOREIGN KEY (`MaNV`) REFERENCES `nhansu`(`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quá trình bổ nhiệm';

-- ---------------------------------------------------------------------
-- 3.3 Bảng qt_luong - Quá trình diễn biến lương
-- ---------------------------------------------------------------------
CREATE TABLE `qt_luong` (
    `ID`            INT UNSIGNED    AUTO_INCREMENT,
    `MaNV`          VARCHAR(10)     NOT NULL,
    `TuNgay`        DATE            NOT NULL        COMMENT 'Ngày bắt đầu hưởng',
    `HeSoLuong`     DECIMAL(5,2)    NOT NULL        COMMENT 'Hệ số lương',
    `BacLuong`      INT UNSIGNED    DEFAULT 1       COMMENT 'Bậc lương',
    `VuotKhung`     DECIMAL(5,2)    DEFAULT 0.00    COMMENT '% vượt khung',
    `NgayHuong`     DATE            DEFAULT NULL     COMMENT 'Ngày bắt đầu hưởng',
    `SoQD`          VARCHAR(50)     DEFAULT NULL     COMMENT 'Số quyết định',
    `GhiChu`        NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`ID`),
    KEY `idx_qtl_manv` (`MaNV`),
    KEY `idx_qtl_tungay` (`TuNgay`),
    CONSTRAINT `fk_qtl_nhansu` FOREIGN KEY (`MaNV`) REFERENCES `nhansu`(`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quá trình diễn biến lương';

-- ---------------------------------------------------------------------
-- 3.4 Bảng qt_daotao - Quá trình đào tạo
-- ---------------------------------------------------------------------
CREATE TABLE `qt_daotao` (
    `ID`                INT UNSIGNED    AUTO_INCREMENT,
    `MaNV`              VARCHAR(10)     NOT NULL,
    `TenKhoaHoc`        NVARCHAR(300)   NOT NULL    COMMENT 'Tên khóa học / chương trình',
    `CoSoDaoTao`        NVARCHAR(200)   DEFAULT NULL COMMENT 'Cơ sở đào tạo',
    `HinhThuc`          ENUM('ChinhQuy','TaiChuc','TuXa','NganHan','Online') DEFAULT 'ChinhQuy' COMMENT 'Hình thức đào tạo',
    `TuNgay`            DATE            DEFAULT NULL,
    `DenNgay`           DATE            DEFAULT NULL,
    `BangCap_ChungChi`  NVARCHAR(200)   DEFAULT NULL COMMENT 'Bằng cấp/chứng chỉ đạt được',
    `XepLoai`           NVARCHAR(50)    DEFAULT NULL COMMENT 'Xếp loại (Giỏi, Khá, TB...)',
    `GhiChu`            NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`ID`),
    KEY `idx_qtdt_manv` (`MaNV`),
    CONSTRAINT `fk_qtdt_nhansu` FOREIGN KEY (`MaNV`) REFERENCES `nhansu`(`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quá trình đào tạo';

-- ---------------------------------------------------------------------
-- 3.5 Bảng qt_ktkl - Quá trình khen thưởng / kỷ luật
-- ---------------------------------------------------------------------
CREATE TABLE `qt_ktkl` (
    `ID`                INT UNSIGNED    AUTO_INCREMENT,
    `MaNV`              VARCHAR(10)     NOT NULL,
    `MaKTKL`            VARCHAR(10)     DEFAULT NULL COMMENT 'FK -> dm_ktkl',
    `SoQD`              VARCHAR(50)     DEFAULT NULL COMMENT 'Số quyết định',
    `NgayQD`            DATE            NOT NULL     COMMENT 'Ngày quyết định',
    `HinhThuc`          NVARCHAR(200)   NOT NULL     COMMENT 'Hình thức KT/KL',
    `LyDo`              NVARCHAR(500)   DEFAULT NULL COMMENT 'Lý do cụ thể',
    `CapKhenThuong`     NVARCHAR(100)   DEFAULT NULL COMMENT 'Cấp khen thưởng (Công ty, Bộ, NN)',
    `GiaTri`            DECIMAL(15,0)   DEFAULT 0    COMMENT 'Giá trị thưởng (+) / phạt (-)',
    `GhiChu`            NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`ID`),
    KEY `idx_qtktkl_manv` (`MaNV`),
    KEY `idx_qtktkl_ngay` (`NgayQD`),
    CONSTRAINT `fk_qtktkl_nhansu` FOREIGN KEY (`MaNV`) REFERENCES `nhansu`(`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_qtktkl_dm` FOREIGN KEY (`MaKTKL`) REFERENCES `dm_ktkl`(`MaKTKL`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quá trình khen thưởng / kỷ luật';

-- ---------------------------------------------------------------------
-- 3.6 Bảng qt_thuyenchuyen - Quá trình thuyên chuyển
-- ---------------------------------------------------------------------
CREATE TABLE `qt_thuyenchuyen` (
    `ID`            INT UNSIGNED    AUTO_INCREMENT,
    `MaNV`          VARCHAR(10)     NOT NULL,
    `SoQD`          VARCHAR(50)     DEFAULT NULL     COMMENT 'Số quyết định',
    `DonViCu`       VARCHAR(20)     DEFAULT NULL     COMMENT 'Mã đơn vị cũ',
    `DonViMoi`      VARCHAR(20)     NOT NULL         COMMENT 'Mã đơn vị mới',
    `ChucVuCu`      NVARCHAR(100)   DEFAULT NULL,
    `ChucVuMoi`     NVARCHAR(100)   DEFAULT NULL,
    `NgayHieuLuc`   DATE            NOT NULL         COMMENT 'Ngày hiệu lực',
    `LyDo`          NVARCHAR(500)   DEFAULT NULL,
    `GhiChu`        NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`ID`),
    KEY `idx_qttc_manv` (`MaNV`),
    KEY `idx_qttc_ngay` (`NgayHieuLuc`),
    CONSTRAINT `fk_qttc_nhansu` FOREIGN KEY (`MaNV`) REFERENCES `nhansu`(`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_qttc_dvcu` FOREIGN KEY (`DonViCu`) REFERENCES `dm_donvi`(`MaDV`) ON UPDATE CASCADE,
    CONSTRAINT `fk_qttc_dvmoi` FOREIGN KEY (`DonViMoi`) REFERENCES `dm_donvi`(`MaDV`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quá trình thuyên chuyển';

-- =====================================================================
-- PHẦN 4: BẢNG CHẤM CÔNG & TIỀN LƯƠNG
-- =====================================================================

-- ---------------------------------------------------------------------
-- 4.1 Bảng chamcong - Chấm công tháng
-- ---------------------------------------------------------------------
CREATE TABLE `chamcong` (
    `MaCC`              INT UNSIGNED    AUTO_INCREMENT  COMMENT 'Mã chấm công (PK)',
    `MaNV`              VARCHAR(10)     NOT NULL        COMMENT 'Mã nhân viên (FK)',
    `Thang`             TINYINT UNSIGNED NOT NULL       COMMENT 'Tháng (1-12)',
    `Nam`               SMALLINT UNSIGNED NOT NULL      COMMENT 'Năm',
    `SoNgayCongChuan`   DECIMAL(4,1)    DEFAULT 22.0   COMMENT 'Số ngày công chuẩn',
    `NgayCongThucTe`    DECIMAL(4,1)    DEFAULT 0.0    COMMENT 'Ngày công thực tế',
    `NghiPhep`          DECIMAL(4,1)    DEFAULT 0.0    COMMENT 'Số ngày nghỉ phép (hưởng lương)',
    `NghiKhongLuong`    DECIMAL(4,1)    DEFAULT 0.0    COMMENT 'Số ngày nghỉ không lương',
    `NghiLe`            DECIMAL(4,1)    DEFAULT 0.0    COMMENT 'Số ngày nghỉ lễ',
    `LamThemGio`        DECIMAL(5,1)    DEFAULT 0.0    COMMENT 'Số giờ làm thêm',
    `DiTreVeSom`        INT UNSIGNED    DEFAULT 0       COMMENT 'Số lần đi trễ/về sớm',
    `TrangThaiDuyet`    ENUM('ChoDuyet','DaDuyet','TuChoi') DEFAULT 'ChoDuyet' COMMENT 'Trạng thái duyệt',
    `NguoiDuyet`        VARCHAR(10)     DEFAULT NULL    COMMENT 'Người duyệt (MaNV)',
    `NgayDuyet`         DATETIME        DEFAULT NULL,
    `GhiChu`            NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaCC`),
    UNIQUE KEY `uk_chamcong_nv_thang` (`MaNV`, `Thang`, `Nam`),
    KEY `idx_cc_thangnam` (`Thang`, `Nam`),
    CONSTRAINT `fk_cc_nhansu` FOREIGN KEY (`MaNV`) REFERENCES `nhansu`(`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng chấm công tổng hợp tháng';

-- ---------------------------------------------------------------------
-- 4.2 Bảng bangluong - Bảng lương tháng
-- ---------------------------------------------------------------------
CREATE TABLE `bangluong` (
    `MaBL`              INT UNSIGNED    AUTO_INCREMENT  COMMENT 'Mã bảng lương (PK)',
    `MaNV`              VARCHAR(10)     NOT NULL        COMMENT 'Mã nhân viên (FK)',
    `Thang`             TINYINT UNSIGNED NOT NULL,
    `Nam`               SMALLINT UNSIGNED NOT NULL,
    `LuongCoBan`        DECIMAL(15,0)   DEFAULT 0       COMMENT 'Lương cơ bản (P1)',
    `HeSoLuong`         DECIMAL(5,2)    DEFAULT 1.00    COMMENT 'Hệ số lương',
    `PhuCapChucVu`      DECIMAL(15,0)   DEFAULT 0       COMMENT 'Phụ cấp chức vụ',
    `PhuCapKhac`        DECIMAL(15,0)   DEFAULT 0       COMMENT 'Phụ cấp khác (ăn trưa, xăng xe, ĐT)',
    `LuongNangLuc`      DECIMAL(15,0)   DEFAULT 0       COMMENT 'Lương năng lực (P2)',
    `ThuongKPI`         DECIMAL(15,0)   DEFAULT 0       COMMENT 'Thưởng KPI/hiệu quả (P3)',
    `ThuongKhac`        DECIMAL(15,0)   DEFAULT 0       COMMENT 'Thưởng khác (Lễ, Tết, đột xuất)',
    `TongThuNhap`       DECIMAL(15,0)   DEFAULT 0       COMMENT 'Tổng thu nhập trước khấu trừ',
    `KhauTruBHXH`       DECIMAL(15,0)   DEFAULT 0       COMMENT 'Khấu trừ BHXH (10.5%)',
    `ThueTNCN`          DECIMAL(15,0)   DEFAULT 0       COMMENT 'Thuế TNCN',
    `KhauTruKhac`       DECIMAL(15,0)   DEFAULT 0       COMMENT 'Khấu trừ khác',
    `ThucLinh`          DECIMAL(15,0)   DEFAULT 0       COMMENT 'Thực lĩnh',
    `SoNgayCong`        DECIMAL(4,1)    DEFAULT 22.0    COMMENT 'Số ngày công tính lương',
    `TrangThaiDuyet`    ENUM('ChuaTinh','DaTinh','KTDuyet','DaChi') DEFAULT 'ChuaTinh' COMMENT 'Trạng thái',
    `NguoiDuyet`        VARCHAR(10)     DEFAULT NULL,
    `NgayDuyet`         DATETIME        DEFAULT NULL,
    `GhiChu`            NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaBL`),
    UNIQUE KEY `uk_bangluong_nv_thang` (`MaNV`, `Thang`, `Nam`),
    KEY `idx_bl_thangnam` (`Thang`, `Nam`),
    KEY `idx_bl_trangthai` (`TrangThaiDuyet`),
    CONSTRAINT `fk_bl_nhansu` FOREIGN KEY (`MaNV`) REFERENCES `nhansu`(`MaNV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bảng lương tháng (3P: P1 + P2 + P3)';

-- =====================================================================
-- PHẦN 5: BẢNG TUYỂN DỤNG
-- =====================================================================

-- ---------------------------------------------------------------------
-- 5.1 Bảng tintuyendung - Tin tuyển dụng
-- ---------------------------------------------------------------------
CREATE TABLE `tintuyendung` (
    `MaTT`          INT UNSIGNED    AUTO_INCREMENT,
    `TieuDe`        NVARCHAR(300)   NOT NULL        COMMENT 'Tiêu đề tin tuyển dụng',
    `MaDV`          VARCHAR(20)     NOT NULL        COMMENT 'Đơn vị cần tuyển',
    `ViTriTuyen`    NVARCHAR(200)   NOT NULL        COMMENT 'Vị trí cần tuyển',
    `SoLuong`       INT UNSIGNED    DEFAULT 1       COMMENT 'Số lượng cần tuyển',
    `MoTaCongViec`  TEXT            DEFAULT NULL     COMMENT 'Mô tả công việc',
    `YeuCau`        TEXT            DEFAULT NULL     COMMENT 'Yêu cầu ứng viên',
    `QuyenLoi`      TEXT            DEFAULT NULL     COMMENT 'Quyền lợi',
    `MucLuong`      NVARCHAR(100)   DEFAULT NULL     COMMENT 'Mức lương (khoảng)',
    `NoiLamViec`    NVARCHAR(200)   DEFAULT NULL     COMMENT 'Nơi làm việc',
    `HanNop`        DATE            DEFAULT NULL     COMMENT 'Hạn nộp hồ sơ',
    `NguoiTao`      VARCHAR(10)     DEFAULT NULL     COMMENT 'Mã NV người tạo',
    `TrangThai`     ENUM('Nhap','ChoDuyet','DaDuyet','DangTuyen','DaDong','HuyBo') DEFAULT 'Nhap',
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaTT`),
    KEY `idx_ttd_donvi` (`MaDV`),
    KEY `idx_ttd_trangthai` (`TrangThai`),
    CONSTRAINT `fk_ttd_donvi` FOREIGN KEY (`MaDV`) REFERENCES `dm_donvi`(`MaDV`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Tin tuyển dụng';

-- ---------------------------------------------------------------------
-- 5.2 Bảng hoso_ungvien - Hồ sơ ứng viên
-- ---------------------------------------------------------------------
CREATE TABLE `hoso_ungvien` (
    `MaUV`          INT UNSIGNED    AUTO_INCREMENT,
    `MaTT`          INT UNSIGNED    NOT NULL        COMMENT 'FK -> tintuyendung',
    `HoTen`         NVARCHAR(100)   NOT NULL,
    `NgaySinh`      DATE            DEFAULT NULL,
    `GioiTinh`      ENUM('Nam','Nữ') DEFAULT NULL,
    `Email`         VARCHAR(100)    DEFAULT NULL,
    `SoDienThoai`   VARCHAR(20)     DEFAULT NULL,
    `DiaChi`        NVARCHAR(300)   DEFAULT NULL,
    `TrinhDo`       NVARCHAR(100)   DEFAULT NULL     COMMENT 'Trình độ học vấn',
    `ChuyenNganh`   NVARCHAR(200)   DEFAULT NULL,
    `KinhNghiem`    NVARCHAR(500)   DEFAULT NULL     COMMENT 'Kinh nghiệm tóm tắt',
    `FileCV`        VARCHAR(255)    DEFAULT NULL     COMMENT 'Đường dẫn file CV',
    `NguonHoSo`     NVARCHAR(100)   DEFAULT NULL     COMMENT 'Nguồn hồ sơ (Website, Vietnamworks, Referral...)',
    `NgayNop`       DATE            DEFAULT NULL,
    `TrangThai`     ENUM('MoiNop','SangLoc','PhongVan','TrungTuyen','LoaiTuVong1','LoaiTuVong2','TuChoi') DEFAULT 'MoiNop',
    `GhiChu`        NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaUV`),
    KEY `idx_hsuv_matt` (`MaTT`),
    KEY `idx_hsuv_trangthai` (`TrangThai`),
    CONSTRAINT `fk_hsuv_ttd` FOREIGN KEY (`MaTT`) REFERENCES `tintuyendung`(`MaTT`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Hồ sơ ứng viên';

-- ---------------------------------------------------------------------
-- 5.3 Bảng ketqua_phongvan - Kết quả phỏng vấn
-- ---------------------------------------------------------------------
CREATE TABLE `ketqua_phongvan` (
    `MaKQ`          INT UNSIGNED    AUTO_INCREMENT,
    `MaUV`          INT UNSIGNED    NOT NULL        COMMENT 'FK -> hoso_ungvien',
    `VongPhongVan`  TINYINT UNSIGNED DEFAULT 1      COMMENT 'Vòng PV (1=HR, 2=Chuyên môn)',
    `NgayPhongVan`  DATE            NOT NULL,
    `NguoiPhongVan` NVARCHAR(200)   DEFAULT NULL     COMMENT 'Người phỏng vấn',
    `DiemDanhGia`   DECIMAL(4,1)    DEFAULT NULL     COMMENT 'Điểm đánh giá (0-10)',
    `NhanXet`       TEXT            DEFAULT NULL     COMMENT 'Nhận xét chi tiết',
    `KetQua`        ENUM('Dat','KhongDat','ChoXem') DEFAULT 'ChoXem' COMMENT 'Kết quả',
    `GhiChu`        NVARCHAR(500)   DEFAULT NULL,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`MaKQ`),
    KEY `idx_kqpv_mauv` (`MaUV`),
    CONSTRAINT `fk_kqpv_ungvien` FOREIGN KEY (`MaUV`) REFERENCES `hoso_ungvien`(`MaUV`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Kết quả phỏng vấn';

-- =====================================================================
-- PHẦN 6: BẢNG QUẢN TRỊ & PHÂN QUYỀN
-- =====================================================================

-- ---------------------------------------------------------------------
-- 6.1 Bảng roles - Vai trò
-- ---------------------------------------------------------------------
CREATE TABLE `roles` (
    `RoleID`        INT UNSIGNED    AUTO_INCREMENT,
    `RoleName`      VARCHAR(50)     NOT NULL    COMMENT 'Tên vai trò',
    `RoleCode`      VARCHAR(30)     NOT NULL    COMMENT 'Mã vai trò dùng trong code',
    `Description`   NVARCHAR(300)   DEFAULT NULL,
    `TrangThai`     TINYINT UNSIGNED NOT NULL DEFAULT 1,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`RoleID`),
    UNIQUE KEY `uk_roles_code` (`RoleCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Vai trò hệ thống';

-- ---------------------------------------------------------------------
-- 6.2 Bảng users - Tài khoản đăng nhập
-- ---------------------------------------------------------------------
CREATE TABLE `users` (
    `ID`            INT UNSIGNED    AUTO_INCREMENT,
    `Username`      VARCHAR(50)     NOT NULL    COMMENT 'Tên đăng nhập',
    `Password`      VARCHAR(255)    NOT NULL    COMMENT 'Mật khẩu hash bcrypt',
    `MaNV`          VARCHAR(10)     DEFAULT NULL COMMENT 'FK -> nhansu (liên kết nhân viên)',
    `RoleID`        INT UNSIGNED    NOT NULL    COMMENT 'FK -> roles',
    `IsActive`      TINYINT(1)      NOT NULL DEFAULT 1 COMMENT '1=Hoạt động, 0=Khóa',
    `LastLogin`     DATETIME        DEFAULT NULL,
    `LoginAttempts` INT UNSIGNED    DEFAULT 0,
    `Avatar`        VARCHAR(255)    DEFAULT NULL,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`ID`),
    UNIQUE KEY `uk_users_username` (`Username`),
    KEY `idx_users_manv` (`MaNV`),
    KEY `idx_users_role` (`RoleID`),
    CONSTRAINT `fk_users_nhansu` FOREIGN KEY (`MaNV`) REFERENCES `nhansu`(`MaNV`) ON UPDATE CASCADE,
    CONSTRAINT `fk_users_role` FOREIGN KEY (`RoleID`) REFERENCES `roles`(`RoleID`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Tài khoản đăng nhập';

-- ---------------------------------------------------------------------
-- 6.3 Bảng permissions - Quyền hạn
-- ---------------------------------------------------------------------
CREATE TABLE `permissions` (
    `PermissionID`  INT UNSIGNED    AUTO_INCREMENT,
    `ModuleCode`    VARCHAR(50)     NOT NULL    COMMENT 'Mã module (VD: NHANSU, LUONG, TUYEN_DUNG)',
    `ModuleName`    NVARCHAR(100)   NOT NULL    COMMENT 'Tên module hiển thị',
    `ActionCode`    VARCHAR(20)     NOT NULL    COMMENT 'Hành động (View, Add, Edit, Delete, Export, Approve)',
    `Description`   NVARCHAR(200)   DEFAULT NULL,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`PermissionID`),
    UNIQUE KEY `uk_perm_module_action` (`ModuleCode`, `ActionCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Quyền hạn chi tiết theo module';

-- ---------------------------------------------------------------------
-- 6.4 Bảng role_permissions - Liên kết vai trò - quyền
-- ---------------------------------------------------------------------
CREATE TABLE `role_permissions` (
    `RoleID`        INT UNSIGNED    NOT NULL,
    `PermissionID`  INT UNSIGNED    NOT NULL,
    `CreatedAt`     DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`RoleID`, `PermissionID`),
    CONSTRAINT `fk_rp_role` FOREIGN KEY (`RoleID`) REFERENCES `roles`(`RoleID`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_rp_perm` FOREIGN KEY (`PermissionID`) REFERENCES `permissions`(`PermissionID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Liên kết vai trò - quyền hạn';

-- ---------------------------------------------------------------------
-- 6.5 Bảng menu_items - Menu điều hướng + phân quyền
-- ---------------------------------------------------------------------
CREATE TABLE `menu_items` (
    `MenuID`            INT UNSIGNED    AUTO_INCREMENT,
    `Title`             NVARCHAR(100)   NOT NULL    COMMENT 'Tiêu đề menu',
    `Route`             VARCHAR(100)    DEFAULT NULL COMMENT 'Route/URL',
    `Icon`              VARCHAR(50)     DEFAULT NULL COMMENT 'Icon (emoji hoặc class)',
    `ParentID`          INT UNSIGNED    DEFAULT NULL COMMENT 'Menu cha (self-ref)',
    `OrderIndex`        INT UNSIGNED    DEFAULT 0   COMMENT 'Thứ tự sắp xếp',
    `RequiredPermission` VARCHAR(50)    DEFAULT NULL COMMENT 'ModuleCode cần có quyền View',
    `IsVisible`         TINYINT(1)      DEFAULT 1   COMMENT 'Hiện/ẩn',
    `CreatedAt`         DATETIME        DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`MenuID`),
    KEY `idx_menu_parent` (`ParentID`),
    CONSTRAINT `fk_menu_parent` FOREIGN KEY (`ParentID`) REFERENCES `menu_items`(`MenuID`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Menu điều hướng hệ thống';


-- =====================================================================
-- PHẦN 7: INSERT DỮ LIỆU MẪU (DUMMY DATA)
-- =====================================================================

-- =====================================================================
-- 7.1 Dữ liệu dm_donvi (Cơ cấu tổ chức Vinamilk)
-- =====================================================================
INSERT INTO `dm_donvi` (`MaDV`, `TenDV`, `TenVietTat`, `CapDV`, `MaDV_Cha`, `DiaChi`, `SoDT`, `Email`, `LoaiDV`, `SoNhanSu`, `ThuTu`) VALUES
-- Cấp 1: Công ty
('VINAMILK', 'Công ty CP Sữa Việt Nam', 'VINAMILK', 1, NULL, 'Số 10 Tân Trào, P. Tân Phú, Q.7, TP.HCM', '028-54155555', 'info@vinamilk.com.vn', 'CongTy', 10156, 1),
-- Cấp 2: Các Khối
('KHOI_SX', 'Khối Sản xuất', 'SX', 2, 'VINAMILK', NULL, NULL, NULL, 'Khoi', 5420, 10),
('KHOI_TT', 'Khối Phát triển Vùng nguyên liệu', 'TT', 2, 'VINAMILK', NULL, NULL, NULL, 'Khoi', 1850, 20),
('KHOI_KD', 'Khối Kinh doanh & Marketing', 'KD', 2, 'VINAMILK', NULL, NULL, NULL, 'Khoi', 1980, 30),
('KHOI_VP', 'Khối Văn phòng', 'VP', 2, 'VINAMILK', NULL, NULL, NULL, 'Khoi', 906, 40),
-- Cấp 3: Nhà máy (thuộc Khối SX)
('NM_TS', 'Nhà máy Sữa Tiên Sơn', 'NM-TS', 3, 'KHOI_SX', 'KCN Tiên Sơn, Bắc Ninh', '0222-3741234', 'nmts@vinamilk.com.vn', 'NhaMay', 820, 11),
('NM_NA', 'Nhà máy Sữa Nghệ An', 'NM-NA', 3, 'KHOI_SX', 'KCN Nam Cấm, Nghi Lộc, Nghệ An', '0238-3851234', 'nmna@vinamilk.com.vn', 'NhaMay', 650, 12),
('NM_BD', 'Nhà máy Sữa Bình Dương', 'NM-BD', 3, 'KHOI_SX', 'KCN Mỹ Phước, Bến Cát, Bình Dương', '0274-3651234', 'nmbd@vinamilk.com.vn', 'NhaMay', 780, 13),
('NM_DN', 'Nhà máy Sữa Đà Nẵng', 'NM-DN', 3, 'KHOI_SX', 'KCN Hòa Khánh, Đà Nẵng', '0236-3731234', 'nmdn@vinamilk.com.vn', 'NhaMay', 520, 14),
('NM_SG', 'Nhà máy Sữa Sài Gòn', 'NM-SG', 3, 'KHOI_SX', 'Q.Bình Tân, TP.HCM', '028-54161234', 'nmsg@vinamilk.com.vn', 'NhaMay', 900, 15),
('NM_TL', 'Nhà máy Sữa Thống Nhất', 'NM-TL', 3, 'KHOI_SX', 'Q.Tân Bình, TP.HCM', '028-38461234', 'nmtl@vinamilk.com.vn', 'NhaMay', 680, 16),
('NM_CT', 'Nhà máy Sữa Cần Thơ', 'NM-CT', 3, 'KHOI_SX', 'KCN Trà Nóc, Cần Thơ', '0292-3841234', 'nmct@vinamilk.com.vn', 'NhaMay', 540, 17),
('NM_DL', 'Nhà máy Sữa Đà Lạt', 'NM-DL', 3, 'KHOI_SX', 'KCN Phú Hội, Đức Trọng, Lâm Đồng', '0263-3551234', 'nmdl@vinamilk.com.vn', 'NhaMay', 530, 18),
-- Cấp 3: Trang trại (thuộc Khối TT)
('TT_GREEN1', 'Trang trại Green Farm Tây Ninh', 'TT-TN', 3, 'KHOI_TT', 'Trảng Bàng, Tây Ninh', '0276-3851234', 'tttn@vinamilk.com.vn', 'TrangTrai', 320, 21),
('TT_GREEN2', 'Trang trại Green Farm Hà Tĩnh', 'TT-HT', 3, 'KHOI_TT', 'Hương Sơn, Hà Tĩnh', '0239-3851234', 'ttht@vinamilk.com.vn', 'TrangTrai', 280, 22),
('TT_ORGANIC1', 'Trang trại Organic Đà Lạt', 'TT-DL', 3, 'KHOI_TT', 'TP. Đà Lạt, Lâm Đồng', '0263-3551235', 'ttdl@vinamilk.com.vn', 'TrangTrai', 350, 23),
('TT_ORGANIC2', 'Trang trại Organic Lâm Đồng', 'TT-LD', 3, 'KHOI_TT', 'Đức Trọng, Lâm Đồng', '0263-3551236', 'ttld@vinamilk.com.vn', 'TrangTrai', 300, 24),
('TT_RESEDA', 'Trang trại Reseda Thanh Hóa', 'TT-TH', 3, 'KHOI_TT', 'Thạch Thành, Thanh Hóa', '0237-3851234', 'ttth@vinamilk.com.vn', 'TrangTrai', 280, 25),
('TT_TQ', 'Trang trại Tuyên Quang', 'TT-TQ', 3, 'KHOI_TT', 'Sơn Dương, Tuyên Quang', '0207-3851234', 'tttq@vinamilk.com.vn', 'TrangTrai', 320, 26),
-- Cấp 3: Chi nhánh kinh doanh (thuộc Khối KD)
('CN_BAC', 'Chi nhánh miền Bắc', 'CN-HN', 3, 'KHOI_KD', 'Cầu Giấy, Hà Nội', '024-37741234', 'cnhn@vinamilk.com.vn', 'ChiNhanh', 580, 31),
('CN_TRUNG', 'Chi nhánh miền Trung', 'CN-DN', 3, 'KHOI_KD', 'TP. Đà Nẵng', '0236-3731235', 'cndn@vinamilk.com.vn', 'ChiNhanh', 420, 32),
('CN_NAM', 'Chi nhánh miền Nam', 'CN-SG', 3, 'KHOI_KD', 'Q.7, TP.HCM', '028-54151234', 'cnsg@vinamilk.com.vn', 'ChiNhanh', 620, 33),
('PB_XUATKHAU', 'Phòng Xuất khẩu', 'XK', 3, 'KHOI_KD', 'Q.7, TP.HCM', '028-54151235', 'export@vinamilk.com.vn', 'Phong', 180, 34),
('PB_TMDT', 'Phòng Thương mại điện tử', 'TMDT', 3, 'KHOI_KD', 'Q.7, TP.HCM', '028-54151236', 'ecom@vinamilk.com.vn', 'Phong', 180, 35),
-- Cấp 3: Phòng ban Văn phòng (thuộc Khối VP)
('PB_NHANSU', 'Phòng Nhân sự', 'NS', 3, 'KHOI_VP', 'Q.7, TP.HCM', '028-54151237', 'hr@vinamilk.com.vn', 'Phong', 85, 41),
('PB_TAICHINH', 'Phòng Tài chính - Kế toán', 'TCKT', 3, 'KHOI_VP', 'Q.7, TP.HCM', '028-54151238', 'finance@vinamilk.com.vn', 'Phong', 120, 42),
('PB_MARKETING', 'Phòng Marketing', 'MKT', 3, 'KHOI_VP', 'Q.7, TP.HCM', '028-54151239', 'marketing@vinamilk.com.vn', 'Phong', 95, 43),
('PB_RD', 'Trung tâm Nghiên cứu & Phát triển', 'R&D', 3, 'KHOI_VP', 'Q.7, TP.HCM', '028-54151240', 'rnd@vinamilk.com.vn', 'TrungTam', 180, 44),
('PB_IT', 'Phòng Công nghệ thông tin', 'IT', 3, 'KHOI_VP', 'Q.7, TP.HCM', '028-54151241', 'it@vinamilk.com.vn', 'Phong', 110, 45),
('PB_PHAPLUAT', 'Phòng Pháp luật', 'PL', 3, 'KHOI_VP', 'Q.7, TP.HCM', '028-54151242', 'legal@vinamilk.com.vn', 'Phong', 45, 46),
('PB_HANHCHINH', 'Phòng Hành chính', 'HC', 3, 'KHOI_VP', 'Q.7, TP.HCM', '028-54151243', 'admin@vinamilk.com.vn', 'Phong', 75, 47),
('PB_QLCL', 'Phòng Quản lý Chất lượng', 'QC', 3, 'KHOI_VP', 'Q.7, TP.HCM', '028-54151244', 'qc@vinamilk.com.vn', 'Phong', 96, 48);

-- =====================================================================
-- 7.2 Dữ liệu dm_chucvu
-- =====================================================================
INSERT INTO `dm_chucvu` (`MaCV`, `TenCV`, `HeSoCV`, `PhuCapCV`, `CapBac`) VALUES
('TGD',   'Tổng Giám đốc',           9.00, 25000000, 1),
('PTGD',  'Phó Tổng Giám đốc',       8.00, 20000000, 2),
('GD',    'Giám đốc Khối/Chức năng', 7.00, 15000000, 3),
('PGD',   'Phó Giám đốc',            6.00, 12000000, 4),
('GDNM',  'Giám đốc Nhà máy',        6.50, 12000000, 4),
('GDTT',  'Giám đốc Trang trại',      6.00, 10000000, 4),
('TP',    'Trưởng phòng',             5.00, 8000000,  5),
('PTP',   'Phó Trưởng phòng',         4.50, 6000000,  6),
('QL',    'Quản lý/Quản đốc',         4.00, 5000000,  7),
('TCA',   'Trưởng ca',                3.50, 4000000,  8),
('TT',    'Tổ trưởng',                3.00, 3000000,  9),
('CVN',   'Chuyên viên cao cấp',      3.50, 4000000,  10),
('CV',    'Chuyên viên',               2.67, 3000000,  11),
('NV',    'Nhân viên',                 2.34, 2000000,  12),
('CN',    'Công nhân',                 1.86, 1500000,  13);

-- =====================================================================
-- 7.3 Dữ liệu dm_ngach
-- =====================================================================
INSERT INTO `dm_ngach` (`MaNgach`, `TenNgach`, `Nhom`, `HeSoKhoiDiem`, `HeSoMax`) VALUES
('NG_A1_1', 'Chuyên viên cao cấp',          'A1', 6.20, 8.00),
('NG_A1_2', 'Kỹ sư cao cấp',               'A1', 6.20, 8.00),
('NG_A2',   'Chuyên viên chính',             'A2', 4.40, 6.78),
('NG_A3',   'Chuyên viên',                   'A3', 2.34, 4.98),
('NG_B',    'Cán sự',                        'B',  1.86, 4.06),
('NG_C1',   'Kỹ thuật viên',                 'C1', 1.65, 3.63),
('NG_C2',   'Nhân viên',                     'C2', 1.50, 3.33),
('NG_C3',   'Công nhân kỹ thuật',            'C3', 1.35, 3.03),
('NG_KS',   'Kỹ sư công nghệ thực phẩm',    'A3', 2.34, 4.98),
('NG_BS',   'Bác sĩ thú y',                 'A2', 4.40, 6.78);

-- =====================================================================
-- 7.4 Dữ liệu dm_trinhdo
-- =====================================================================
INSERT INTO `dm_trinhdo` (`MaTD`, `TenTD`, `LoaiTD`, `ThuTu`) VALUES
-- Học vấn
('HV_TS',   'Tiến sĩ',               'HocVan', 1),
('HV_THS',  'Thạc sĩ',               'HocVan', 2),
('HV_DH',   'Đại học',                'HocVan', 3),
('HV_CD',   'Cao đẳng',               'HocVan', 4),
('HV_TC',   'Trung cấp',              'HocVan', 5),
('HV_THPT', 'THPT',                   'HocVan', 6),
-- Chuyên môn
('CM_KTP',  'Kỹ thuật Thực phẩm',     'ChuyenMon', 1),
('CM_KCK',  'Kỹ thuật Cơ khí',        'ChuyenMon', 2),
('CM_CNSH', 'Công nghệ Sinh học',      'ChuyenMon', 3),
('CM_QTKD', 'Quản trị Kinh doanh',    'ChuyenMon', 4),
('CM_KT',   'Kế toán - Kiểm toán',    'ChuyenMon', 5),
('CM_TY',   'Thú y - Chăn nuôi',      'ChuyenMon', 6),
('CM_CNTT', 'Công nghệ thông tin',     'ChuyenMon', 7),
-- Tin học
('TH_A',    'Tin học văn phòng cơ bản', 'TinHoc', 1),
('TH_B',    'Tin học văn phòng nâng cao','TinHoc', 2),
('TH_IT',   'Chuyên ngành CNTT',        'TinHoc', 3),
-- Ngoại ngữ
('NN_A2',   'Tiếng Anh A2',            'NgoaiNgu', 1),
('NN_B1',   'Tiếng Anh B1',            'NgoaiNgu', 2),
('NN_B2',   'Tiếng Anh B2/IELTS 5.5+', 'NgoaiNgu', 3),
('NN_C1',   'Tiếng Anh C1/IELTS 7.0+', 'NgoaiNgu', 4),
('NN_TQ',   'Tiếng Trung HSK 4+',      'NgoaiNgu', 5),
('NN_NB',   'Tiếng Nhật JLPT N3+',     'NgoaiNgu', 6),
-- Lý luận chính trị
('LL_SC',   'Sơ cấp Lý luận CT',       'LyLuanChinhTri', 1),
('LL_TC',   'Trung cấp Lý luận CT',    'LyLuanChinhTri', 2),
('LL_CC',   'Cao cấp Lý luận CT',      'LyLuanChinhTri', 3);

-- =====================================================================
-- 7.5 Dữ liệu dm_ktkl
-- =====================================================================
INSERT INTO `dm_ktkl` (`MaKTKL`, `Loai`, `TenHinhThuc`, `MucThuongPhat`, `MoTa`) VALUES
('KT_BK',   'KhenThuong', 'Bằng khen Công ty',              5000000,  'Bằng khen của Tổng Giám đốc Vinamilk'),
('KT_CSTD', 'KhenThuong', 'Chiến sĩ thi đua cơ sở',        3000000,  'Danh hiệu CSTĐ cấp cơ sở hàng năm'),
('KT_LDTT', 'KhenThuong', 'Lao động tiên tiến',             2000000,  'Danh hiệu LĐTT hàng năm'),
('KT_ST',   'KhenThuong', 'Sáng kiến - Cải tiến',           10000000, 'Thưởng sáng kiến cải tiến sản xuất'),
('KT_TH',   'KhenThuong', 'Thưởng thành tích đột xuất',     5000000,  'Thưởng nóng cho thành tích xuất sắc'),
('KL_KT',   'KyLuat',     'Khiển trách',                    -500000,  'Hình thức kỷ luật nhẹ nhất'),
('KL_CC',   'KyLuat',     'Cảnh cáo',                       -2000000, 'Hình thức kỷ luật cảnh cáo'),
('KL_ST',   'KyLuat',     'Sa thải',                         0,       'Chấm dứt hợp đồng lao động');

-- =====================================================================
-- 7.6 Dữ liệu nhansu (55 nhân viên mẫu)
-- =====================================================================
INSERT INTO `nhansu` (`MaNV`, `HoTen`, `TenThuongGoi`, `NgaySinh`, `GioiTinh`, `SoCMND_CCCD`, `NgayCap`, `NoiCap`, `QueQuan`, `NoiDKKTTru`, `DiaChiHienTai`, `DanToc`, `TonGiao`, `TinhTrangHonNhan`, `Email`, `SoDienThoai`, `NgayVaoLam`, `MaDV`, `MaCV`, `MaNgach`, `HeSoLuongHienTai`, `BacLuongHienTai`, `TrinhDoHocVan`, `ChuyenNganh`, `TruongDaoTao`, `DangVien`, `DoanVien`, `SoTaiKhoanNH`, `NganHang`, `MaSoThue`, `SoNguoiPhuThuoc`, `LienHeKhanCap`, `ViTriLuuHoSo`, `LoaiHopDong`, `NgayHetHanHD`, `LuongBaoHiem`, `TrangThai`) VALUES

-- === BAN GIÁM ĐỐC & LÃNH ĐẠO CẤP CAO ===
('VNM-0001', 'Nguyễn Văn Hùng', 'Hùng', '1975-03-15', 'Nam', '079075012345', '2020-01-15', 'CA tỉnh Bắc Ninh', 'Tiên Du, Bắc Ninh', 'Tiên Sơn, Bắc Ninh', 'Tiên Sơn, Bắc Ninh', 'Kinh', 'Không', 'Đã kết hôn', 'hung.nv@vinamilk.com.vn', '0912345678', '2002-06-01', 'NM_TS', 'GDNM', 'NG_A1_2', 7.28, 6, 'Thạc sĩ', 'Kỹ thuật Thực phẩm', 'ĐH Bách Khoa HN', 1, 1, '0123456789', 'Vietcombank', '8012345678', 2, 'Trần Thị Mai - Vợ - 0987654321', 'Kho NM-TS / Tủ 01 / Ngăn A', 'Không thời hạn', NULL, 36000000, 1),

('VNM-0002', 'Trần Minh Quân', 'Quân', '1985-07-22', 'Nam', '079085023456', '2020-05-10', 'CA tỉnh Bắc Ninh', 'Từ Sơn, Bắc Ninh', 'TP. Từ Sơn, Bắc Ninh', 'TP. Từ Sơn, Bắc Ninh', 'Kinh', 'Không', 'Đã kết hôn', 'quan.tm@vinamilk.com.vn', '0923456789', '2010-03-15', 'NM_TS', 'QL', 'NG_A3', 3.66, 4, 'Đại học', 'Kỹ thuật Cơ khí', 'ĐH Bách Khoa HN', 1, 1, '0234567890', 'Techcombank', '8023456789', 1, 'Lê Thị Hoa - Vợ - 0976543210', 'Kho NM-TS / Tủ 01 / Ngăn B', '3 năm', '2027-03-14', 23400000, 1),

('VNM-0003', 'Lê Thị Hương', 'Hương', '1992-11-08', 'Nữ', '079092034567', '2020-03-20', 'CA tỉnh Bắc Ninh', 'Tiên Du, Bắc Ninh', 'Tiên Du, Bắc Ninh', 'Tiên Du, Bắc Ninh', 'Kinh', 'Không', 'Độc thân', 'huong.lt@vinamilk.com.vn', '0934567890', '2015-08-01', 'NM_TS', 'TT', 'NG_C1', 2.46, 3, 'Cao đẳng', 'Công nghệ Thực phẩm', 'CĐ Công nghiệp HN', 0, 1, '0345678901', 'Vietinbank', '8034567890', 0, 'Lê Văn Nam - Bố - 0965432109', 'Kho NM-TS / Tủ 02 / Ngăn A', '3 năm', '2027-07-31', 14820000, 1),

('VNM-0004', 'Phạm Đức Thắng', 'Thắng', '1995-04-18', 'Nam', '079095045678', '2020-07-15', 'CA tỉnh Bắc Ninh', 'Yên Phong, Bắc Ninh', 'Yên Phong, Bắc Ninh', 'Yên Phong, Bắc Ninh', 'Kinh', 'Không', 'Độc thân', 'thang.pd@vinamilk.com.vn', '0945678901', '2019-02-10', 'NM_TS', 'CN', 'NG_C3', 1.86, 2, 'Trung cấp', 'Vận hành Máy', 'TC Cơ điện Bắc Ninh', 0, 1, '0456789012', 'BIDV', '8045678901', 0, 'Phạm Văn Hải - Bố - 0954321098', 'Kho NM-TS / Tủ 03 / Ngăn C', '1 năm', '2027-02-09', 10000000, 1),

('VNM-0005', 'Nguyễn Thị Lan', 'Lan', '1998-09-25', 'Nữ', '079098056789', '2021-01-10', 'CA tỉnh Nghệ An', 'TP. Vinh, Nghệ An', 'TP. Vinh, Nghệ An', 'TP. Vinh, Nghệ An', 'Kinh', 'Không', 'Độc thân', 'lan.nt@vinamilk.com.vn', '0956789012', '2026-07-01', 'NM_NA', 'CN', 'NG_C3', 1.35, 1, 'THPT', NULL, NULL, 0, 0, '0567890123', 'Agribank', '8056789012', 0, 'Nguyễn Văn Tùng - Bố - 0943210987', 'Kho NM-NA / Tủ 05 / Ngăn A', 'Thử việc', '2026-08-31', 0, 2),

('VNM-0006', 'Hoàng Anh Tuấn', 'Tuấn', '1988-01-12', 'Nam', '079088067890', '2019-06-20', 'CA tỉnh Bình Dương', 'Dĩ An, Bình Dương', 'Dĩ An, Bình Dương', 'Dĩ An, Bình Dương', 'Kinh', 'Không', 'Đã kết hôn', 'tuan.ha@vinamilk.com.vn', '0967890123', '2012-05-15', 'NM_BD', 'TCA', 'NG_A3', 3.33, 3, 'Đại học', 'Công nghệ Sinh học', 'ĐH Khoa học Tự nhiên TP.HCM', 1, 1, '0678901234', 'MB Bank', '8067890123', 2, 'Lê Thị Thủy - Vợ - 0932109876', 'Kho NM-BD / Tủ 01 / Ngăn B', 'Không thời hạn', NULL, 19080000, 1),

('VNM-0007', 'Võ Minh Tâm', 'Tâm', '1993-06-30', 'Nữ', '079093078901', '2019-09-15', 'CA TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Q. Bình Thạnh, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'tam.vm@vinamilk.com.vn', '0978901234', '2017-09-01', 'NM_SG', 'CV', 'NG_A3', 3.00, 2, 'Đại học', 'Hóa Phân tích', 'ĐH Bách Khoa TP.HCM', 0, 1, '0789012345', 'ACB', '8078901234', 0, 'Võ Văn Tài - Bố - 0921098765', 'Kho NM-SG / Tủ 02 / Ngăn A', '3 năm', '2026-08-31', 16960000, 1),

('VNM-0008', 'Đặng Quốc Bảo', 'Bảo', '1990-12-05', 'Nam', '079090089012', '2019-12-10', 'CA tỉnh Lâm Đồng', 'TP. Đà Lạt, Lâm Đồng', 'TP. Đà Lạt, Lâm Đồng', 'TP. Đà Lạt, Lâm Đồng', 'Kinh', 'Không', 'Đã kết hôn', 'bao.dq@vinamilk.com.vn', '0989012345', '2016-01-10', 'NM_DL', 'CV', 'NG_KS', 3.33, 3, 'Đại học', 'Kỹ thuật Cơ điện', 'ĐH Đà Lạt', 0, 1, '0890123456', 'Sacombank', '8089012345', 1, 'Nguyễn Thị Hồng - Vợ - 0910987654', 'Kho NM-DL / Tủ 01 / Ngăn C', 'Không thời hạn', NULL, 18020000, 1),

-- === KHỐI TRANG TRẠI ===
('VNM-0009', 'Bùi Thanh Sơn', 'Sơn', '1982-08-20', 'Nam', '079082090123', '2018-04-15', 'CA tỉnh Tây Ninh', 'Trảng Bàng, Tây Ninh', 'Trảng Bàng, Tây Ninh', 'Tây Ninh', 'Kinh', 'Không', 'Đã kết hôn', 'son.bt@vinamilk.com.vn', '0990123456', '2008-04-01', 'TT_GREEN1', 'GDTT', 'NG_A2', 5.56, 4, 'Thạc sĩ', 'Thú y', 'ĐH Nông nghiệp HN', 1, 1, '0901234567', 'Vietcombank', '8090123456', 3, 'Trần Thị Linh - Vợ - 0909876543', 'Kho TT-TN / Tủ 01 / Ngăn A', 'Không thời hạn', NULL, 33920000, 1),

('VNM-0010', 'Trần Thị Ngọc', 'Ngọc', '1990-05-14', 'Nữ', '079090101234', '2019-05-20', 'CA tỉnh Tây Ninh', 'Gò Dầu, Tây Ninh', 'Gò Dầu, Tây Ninh', 'Gò Dầu, Tây Ninh', 'Kinh', 'Không', 'Độc thân', 'ngoc.tt@vinamilk.com.vn', '0901234567', '2015-06-15', 'TT_GREEN1', 'CV', 'NG_BS', 4.40, 1, 'Đại học', 'Thú y', 'ĐH Nông Lâm TP.HCM', 0, 1, '1012345678', 'BIDV', '8101234567', 0, 'Trần Văn Phúc - Anh - 0898765432', 'Kho TT-TN / Tủ 02 / Ngăn B', '3 năm', '2027-06-14', 19080000, 1),

('VNM-0011', 'Lê Hoàng Nam', 'Nam', '1993-02-28', 'Nam', '079093112345', '2020-02-15', 'CA tỉnh Lâm Đồng', 'TP. Đà Lạt, Lâm Đồng', 'TP. Đà Lạt, Lâm Đồng', 'TP. Đà Lạt, Lâm Đồng', 'Kinh', 'Không', 'Độc thân', 'nam.lh@vinamilk.com.vn', '0912345670', '2018-01-05', 'TT_ORGANIC1', 'CV', 'NG_KS', 3.00, 2, 'Đại học', 'Nông nghiệp Hữu cơ', 'ĐH Nông Lâm TP.HCM', 0, 1, '1123456789', 'Techcombank', '8112345678', 0, 'Lê Văn Trung - Bố - 0887654321', 'Kho TT-DL / Tủ 01 / Ngăn A', '3 năm', '2027-01-04', 16960000, 1),

('VNM-0012', 'Phạm Thị Vân', 'Vân', '1996-10-10', 'Nữ', '079096123456', '2020-04-10', 'CA tỉnh Lâm Đồng', 'Đức Trọng, Lâm Đồng', 'Đức Trọng, Lâm Đồng', 'Đức Trọng, Lâm Đồng', 'Kinh', 'Không', 'Độc thân', 'van.pt@vinamilk.com.vn', '0923456780', '2020-04-15', 'TT_ORGANIC2', 'NV', 'NG_C2', 1.86, 2, 'Cao đẳng', 'Chăn nuôi', 'CĐ Nông nghiệp Lâm Đồng', 0, 1, '1234567890', 'Agribank', '8123456789', 0, 'Phạm Văn Đông - Bố - 0876543210', 'Kho TT-LD / Tủ 02 / Ngăn C', '1 năm', '2027-04-14', 11660000, 1),

('VNM-0013', 'Nguyễn Công Minh', 'Minh', '1987-04-03', 'Nam', '079087134567', '2019-07-20', 'CA tỉnh Thanh Hóa', 'Thạch Thành, Thanh Hóa', 'Thạch Thành, Thanh Hóa', 'Thạch Thành, Thanh Hóa', 'Kinh', 'Không', 'Đã kết hôn', 'minh.nc@vinamilk.com.vn', '0934567891', '2013-07-20', 'TT_RESEDA', 'QL', 'NG_A3', 3.66, 4, 'Đại học', 'Chăn nuôi Thú y', 'ĐH Nông nghiệp HN', 1, 1, '1345678901', 'MB Bank', '8134567890', 2, 'Trần Thị Hà - Vợ - 0865432109', 'Kho TT-TH / Tủ 01 / Ngăn A', 'Không thời hạn', NULL, 21200000, 1),

-- === KHỐI KINH DOANH ===
('VNM-0014', 'Vũ Đình Khoa', 'Khoa', '1984-12-18', 'Nam', '079084145678', '2019-02-10', 'CA TP. Hà Nội', 'Cầu Giấy, Hà Nội', 'Cầu Giấy, Hà Nội', 'Cầu Giấy, Hà Nội', 'Kinh', 'Không', 'Đã kết hôn', 'khoa.vd@vinamilk.com.vn', '0945678902', '2007-02-01', 'CN_BAC', 'GD', 'NG_A2', 5.56, 4, 'Thạc sĩ', 'Quản trị Kinh doanh', 'ĐH Kinh tế Quốc dân', 1, 1, '1456789012', 'Vietcombank', '8145678901', 2, 'Nguyễn Thị Phương - Vợ - 0854321098', 'Kho CN-HN / Tủ 01 / Ngăn A', 'Không thời hạn', NULL, 31800000, 1),

('VNM-0015', 'Hoàng Thị Mai Anh', 'Anh', '1991-03-25', 'Nữ', '079091156789', '2020-05-15', 'CA TP. Hà Nội', 'Đống Đa, Hà Nội', 'Đống Đa, Hà Nội', 'Đống Đa, Hà Nội', 'Kinh', 'Không', 'Độc thân', 'anh.htm@vinamilk.com.vn', '0956789013', '2016-05-10', 'CN_BAC', 'CV', 'NG_A3', 3.00, 2, 'Đại học', 'Marketing', 'ĐH Thương mại', 0, 1, '1567890123', 'VPBank', '8156789012', 0, 'Hoàng Văn Tuấn - Bố - 0843210987', 'Kho CN-HN / Tủ 02 / Ngăn B', '3 năm', '2028-05-09', 15900000, 1),

('VNM-0016', 'Phan Thanh Tùng', 'Tùng', '1994-08-08', 'Nam', '079094167890', '2020-09-20', 'CA TP.HCM', 'Q.7, TP.HCM', 'Q.7, TP.HCM', 'Q.7, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'tung.pt@vinamilk.com.vn', '0967890124', '2019-09-01', 'CN_NAM', 'NV', 'NG_C2', 1.86, 2, 'Đại học', 'Kinh tế', 'ĐH Kinh tế TP.HCM', 0, 1, '1678901234', 'ACB', '8167890123', 0, 'Phan Văn Hải - Bố - 0832109876', 'Kho CN-SG / Tủ 03 / Ngăn A', '1 năm', '2027-08-31', 10600000, 1),

('VNM-0017', 'Đỗ Thị Hồng Nhung', 'Nhung', '1997-06-20', 'Nữ', '079097178901', '2021-03-10', 'CA TP.HCM', 'Tân Bình, TP.HCM', 'Tân Bình, TP.HCM', 'Tân Bình, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'nhung.dth@vinamilk.com.vn', '0978901235', '2021-03-15', 'CN_NAM', 'NV', 'NG_C2', 1.65, 1, 'Cao đẳng', 'Marketing', 'CĐ FPT TP.HCM', 0, 1, '1789012345', 'Techcombank', '8178901234', 0, 'Đỗ Văn Thành - Bố - 0821098765', 'Kho CN-SG / Tủ 04 / Ngăn B', '1 năm', '2027-03-14', 9010000, 1),

('VNM-0018', 'Lý Quang Vinh', 'Vinh', '1986-11-11', 'Nam', '079086189012', '2019-08-15', 'CA TP. Đà Nẵng', 'Hải Châu, Đà Nẵng', 'Hải Châu, Đà Nẵng', 'TP. Đà Nẵng', 'Kinh', 'Không', 'Đã kết hôn', 'vinh.lq@vinamilk.com.vn', '0989012346', '2014-08-20', 'CN_TRUNG', 'NV', 'NG_C2', 2.26, 3, 'THPT', NULL, NULL, 0, 1, '1890123456', 'BIDV', '8189012345', 2, 'Nguyễn Thị Loan - Vợ - 0810987654', 'Kho CN-DN / Tủ 02 / Ngăn C', 'Không thời hạn', NULL, 11660000, 1),

('VNM-0019', 'Trương Thị Bích Ngọc', 'Ngọc', '1992-02-14', 'Nữ', '079092190123', '2019-11-20', 'CA TP.HCM', 'Q.3, TP.HCM', 'Q.3, TP.HCM', 'Q.3, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'ngoc.ttb@vinamilk.com.vn', '0990123457', '2017-11-01', 'PB_XUATKHAU', 'CV', 'NG_A3', 3.33, 3, 'Đại học', 'Ngoại thương', 'ĐH Ngoại thương TP.HCM', 0, 1, '1901234567', 'Sacombank', '8190123456', 0, 'Trương Văn Hòa - Bố - 0809876543', 'Kho VP / Tủ 05 / Ngăn A', '3 năm', '2026-10-31', 19080000, 1),

-- === KHỐI VĂN PHÒNG - NHÂN SỰ ===
('VNM-0020', 'Nguyễn Thị Thanh Huyền', 'Huyền', '1983-09-05', 'Nữ', '079083201234', '2018-09-10', 'CA TP.HCM', 'Q.2, TP.HCM', 'Q.2, TP.HCM', 'Q.2, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'huyen.ntt@vinamilk.com.vn', '0901234568', '2005-01-10', 'PB_NHANSU', 'GD', 'NG_A1_1', 6.78, 3, 'Tiến sĩ', 'Quản trị Nhân sự', 'ĐH Kinh tế Quốc dân', 1, 1, '2012345678', 'Vietcombank', '8201234567', 2, 'Lê Minh Đức - Chồng - 0798765432', 'Kho VP / Tủ 01 / Ngăn A', 'Không thời hạn', NULL, 36000000, 1),

('VNM-0021', 'Lê Anh Dũng', 'Dũng', '1988-05-30', 'Nam', '079088212345', '2019-07-15', 'CA TP.HCM', 'Bình Thạnh, TP.HCM', 'Bình Thạnh, TP.HCM', 'Bình Thạnh, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'dung.la@vinamilk.com.vn', '0912345671', '2012-07-01', 'PB_NHANSU', 'TP', 'NG_A2', 4.74, 2, 'Thạc sĩ', 'Quản trị Kinh doanh', 'ĐH FPT', 0, 1, '2123456789', 'Techcombank', '8212345678', 1, 'Trần Thị Hằng - Vợ - 0787654321', 'Kho VP / Tủ 01 / Ngăn B', 'Không thời hạn', NULL, 26500000, 1),

('VNM-0022', 'Trần Hoàng Long', 'Long', '1990-10-22', 'Nam', '079090223456', '2020-03-10', 'CA TP.HCM', 'Q.9, TP.HCM', 'Q.9, TP.HCM', 'Q.9, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'long.th@vinamilk.com.vn', '0923456781', '2016-03-15', 'PB_NHANSU', 'CV', 'NG_A3', 3.33, 3, 'Đại học', 'Kế toán', 'ĐH Kinh tế TP.HCM', 0, 1, '2234567890', 'VPBank', '8223456789', 0, 'Trần Văn Thắng - Bố - 0776543210', 'Kho VP / Tủ 01 / Ngăn C', '3 năm', '2028-03-14', 19080000, 1),

('VNM-0023', 'Phạm Ngọc Ánh', 'Ánh', '1994-07-16', 'Nữ', '079094234567', '2020-01-20', 'CA TP.HCM', 'Gò Vấp, TP.HCM', 'Gò Vấp, TP.HCM', 'Gò Vấp, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'anh.pn@vinamilk.com.vn', '0934567892', '2019-01-07', 'PB_NHANSU', 'CV', 'NG_A3', 2.67, 1, 'Đại học', 'Tâm lý học', 'ĐH KHXH&NV TP.HCM', 0, 1, '2345678901', 'MB Bank', '8234567890', 0, 'Phạm Văn Tuấn - Bố - 0765432109', 'Kho VP / Tủ 01 / Ngăn D', '3 năm', '2028-01-06', 15900000, 1),

-- === KHỐI VĂN PHÒNG - TÀI CHÍNH ===
('VNM-0024', 'Đặng Minh Trí', 'Trí', '1980-03-10', 'Nam', '079080245678', '2018-03-15', 'CA TP.HCM', 'Q.1, TP.HCM', 'Q.1, TP.HCM', 'Q.1, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'tri.dm@vinamilk.com.vn', '0945678903', '2004-09-01', 'PB_TAICHINH', 'GD', 'NG_A1_1', 7.28, 6, 'Tiến sĩ', 'Tài chính', 'ĐH Kinh tế TP.HCM', 1, 1, '2456789012', 'Vietcombank', '8245678901', 2, 'Lê Thị Hạnh - Vợ - 0754321098', 'Kho VP / Tủ 02 / Ngăn A', 'Không thời hạn', NULL, 36000000, 1),

('VNM-0025', 'Hà Thị Phương Linh', 'Linh', '1991-12-01', 'Nữ', '079091256789', '2020-04-10', 'CA TP.HCM', 'Q.3, TP.HCM', 'Q.3, TP.HCM', 'Q.3, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'linh.htp@vinamilk.com.vn', '0956789014', '2014-04-15', 'PB_TAICHINH', 'TP', 'NG_A2', 4.74, 2, 'Thạc sĩ', 'Kế toán - Kiểm toán', 'ĐH Kinh tế TP.HCM', 0, 1, '2567890123', 'BIDV', '8256789012', 1, 'Nguyễn Minh Tuấn - Chồng - 0743210987', 'Kho VP / Tủ 02 / Ngăn B', 'Không thời hạn', NULL, 25440000, 1),

-- === KHỐI VĂN PHÒNG - MARKETING ===
('VNM-0026', 'Ngô Quang Hải', 'Hải', '1989-06-28', 'Nam', '079089267890', '2019-10-15', 'CA TP.HCM', 'Q.2, TP.HCM', 'Q.2, TP.HCM', 'Q.2, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'hai.nq@vinamilk.com.vn', '0967890125', '2013-10-01', 'PB_MARKETING', 'TP', 'NG_A2', 5.08, 3, 'Thạc sĩ', 'Marketing', 'ĐH RMIT', 0, 1, '2678901234', 'ACB', '8267890123', 1, 'Lê Thị Thanh - Vợ - 0732109876', 'Kho VP / Tủ 03 / Ngăn A', 'Không thời hạn', NULL, 29680000, 1),

('VNM-0027', 'Trịnh Thị Minh Châu', 'Châu', '1995-01-15', 'Nữ', '079095278901', '2020-06-10', 'CA TP.HCM', 'Thủ Đức, TP.HCM', 'Thủ Đức, TP.HCM', 'Thủ Đức, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'chau.ttm@vinamilk.com.vn', '0978901236', '2020-06-01', 'PB_MARKETING', 'CV', 'NG_A3', 2.67, 1, 'Đại học', 'Truyền thông Đa phương tiện', 'ĐH FPT', 0, 1, '2789012345', 'Techcombank', '8278901234', 0, 'Trịnh Văn Hùng - Bố - 0721098765', 'Kho VP / Tủ 03 / Ngăn B', '3 năm', '2026-05-31', 14820000, 1),

-- === KHỐI VĂN PHÒNG - R&D ===
('VNM-0028', 'Lương Đức Anh', 'Anh', '1987-09-12', 'Nam', '079087289012', '2019-01-15', 'CA TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'anh.ld@vinamilk.com.vn', '0989012347', '2010-01-15', 'PB_RD', 'GD', 'NG_A1_1', 6.78, 3, 'Tiến sĩ', 'Công nghệ Thực phẩm', 'ĐH Bách Khoa TP.HCM', 1, 1, '2890123456', 'Vietcombank', '8289012345', 2, 'Phạm Thị Hoa - Vợ - 0710987654', 'Kho VP / Tủ 04 / Ngăn A', 'Không thời hạn', NULL, 36000000, 1),

('VNM-0029', 'Cao Thị Thanh Trúc', 'Trúc', '1993-04-22', 'Nữ', '079093290123', '2020-07-15', 'CA TP.HCM', 'Q.Phú Nhuận, TP.HCM', 'Q.Phú Nhuận, TP.HCM', 'Q.Phú Nhuận, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'truc.ctt@vinamilk.com.vn', '0990123458', '2018-07-01', 'PB_RD', 'CV', 'NG_A3', 3.00, 2, 'Thạc sĩ', 'Vi sinh Thực phẩm', 'ĐH Bách Khoa TP.HCM', 0, 1, '2901234567', 'MB Bank', '8290123456', 0, 'Cao Văn Thành - Bố - 0709876543', 'Kho VP / Tủ 04 / Ngăn B', '3 năm', '2027-06-30', 18020000, 1),

-- === KHỐI VĂN PHÒNG - IT ===
('VNM-0030', 'Nguyễn Minh Phúc', 'Phúc', '1991-08-18', 'Nam', '079091301234', '2020-02-10', 'CA TP.HCM', 'Q.7, TP.HCM', 'Q.7, TP.HCM', 'Q.7, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'phuc.nm@vinamilk.com.vn', '0901234569', '2015-02-01', 'PB_IT', 'TP', 'NG_A2', 5.08, 3, 'Thạc sĩ', 'Khoa học Máy tính', 'ĐH Bách Khoa TP.HCM', 0, 1, '3012345678', 'Techcombank', '8301234567', 1, 'Lê Thị Minh - Vợ - 0698765432', 'Kho VP / Tủ 05 / Ngăn A', 'Không thời hạn', NULL, 29680000, 1),

('VNM-0031', 'Trần Đức Hoàng', 'Hoàng', '1994-11-05', 'Nam', '079094312345', '2020-06-20', 'CA TP.HCM', 'Q.Tân Phú, TP.HCM', 'Q.Tân Phú, TP.HCM', 'Q.Tân Phú, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'hoang.td@vinamilk.com.vn', '0912345672', '2019-06-15', 'PB_IT', 'CV', 'NG_A3', 3.00, 2, 'Đại học', 'CNTT', 'ĐH KHTN TP.HCM', 0, 1, '3123456789', 'VPBank', '8312345678', 0, 'Trần Văn Đạt - Bố - 0687654321', 'Kho VP / Tủ 05 / Ngăn B', '3 năm', '2028-06-14', 21200000, 1),

-- === KHỐI VĂN PHÒNG - CHẤT LƯỢNG ===
('VNM-0032', 'Lê Thị Kim Oanh', 'Oanh', '1986-07-30', 'Nữ', '079086323456', '2019-04-15', 'CA TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'oanh.ltk@vinamilk.com.vn', '0923456782', '2011-04-01', 'PB_QLCL', 'TP', 'NG_A2', 5.08, 3, 'Thạc sĩ', 'Quản lý Chất lượng', 'ĐH Bách Khoa HN', 1, 1, '3234567890', 'Sacombank', '8323456789', 1, 'Nguyễn Văn Hưng - Chồng - 0676543210', 'Kho VP / Tủ 06 / Ngăn A', 'Không thời hạn', NULL, 27560000, 1),

-- === THÊM NV BỔ SUNG CÁC ĐƠN VỊ ===
('VNM-0033', 'Phạm Tuấn Kiệt', 'Kiệt', '1996-02-28', 'Nam', '079096334567', '2021-08-10', 'CA tỉnh Bình Dương', 'Thuận An, Bình Dương', 'Thuận An, Bình Dương', 'Thuận An, Bình Dương', 'Kinh', 'Không', 'Độc thân', 'kiet.pt@vinamilk.com.vn', '0934567893', '2021-08-01', 'NM_BD', 'CN', 'NG_C3', 1.65, 1, 'Trung cấp', 'Cơ khí', 'TC Cơ khí Bình Dương', 0, 1, '3345678901', 'Agribank', '8334567890', 0, 'Phạm Văn Đức - Bố - 0665432109', 'Kho NM-BD / Tủ 03 / Ngăn B', '1 năm', '2027-07-31', 9540000, 1),

('VNM-0034', 'Nguyễn Hoàng Yến', 'Yến', '1999-05-12', 'Nữ', '079099345678', '2021-08-15', 'CA TP.HCM', 'Q.1, TP.HCM', 'Q.1, TP.HCM', 'Q.1, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'yen.nh@vinamilk.com.vn', '0945678904', '2026-08-01', 'PB_HANHCHINH', 'NV', 'NG_C2', 1.50, 1, 'Đại học', 'Quản trị Du lịch', 'ĐH Hoa Sen', 0, 0, '3456789012', 'BIDV', '8345678901', 0, 'Nguyễn Thị Lan - Mẹ - 0654321098', 'Kho VP / Tủ 07 / Ngăn A', 'Thử việc', '2026-09-30', 0, 2),

('VNM-0035', 'Vương Đình Phong', 'Phong', '1968-10-15', 'Nam', '079068356789', '2018-10-20', 'CA TP.HCM', 'Q.Tân Bình, TP.HCM', 'Q.Tân Bình, TP.HCM', 'Q.Tân Bình, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'phong.vd@vinamilk.com.vn', '0956789015', '1995-06-01', 'NM_TL', 'CVN', 'NG_A1_1', 8.00, 12, 'Tiến sĩ', 'Kỹ thuật Thực phẩm', 'ĐH Bách Khoa HN', 1, 1, '3567890123', 'Vietcombank', '8356789012', 3, 'Lê Thị Thu - Vợ - 0643210987', 'Kho NM-TN / Tủ 01 / Ngăn A', 'Không thời hạn', NULL, 36000000, 1),

-- Nhân viên Nghỉ hưu
('VNM-0036', 'Huỳnh Văn Đạt', 'Đạt', '1963-04-20', 'Nam', '079063367890', '2018-04-25', 'CA TP.HCM', 'Q.Gò Vấp, TP.HCM', 'Q.Gò Vấp, TP.HCM', 'Q.Gò Vấp, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'dat.hv@vinamilk.com.vn', '0967890126', '1993-01-01', 'NM_SG', 'PGD', 'NG_A1_2', 7.28, 6, 'Đại học', 'Kỹ thuật Cơ khí', 'ĐH Bách Khoa TP.HCM', 1, 0, '3678901234', 'Vietcombank', '8367890123', 0, 'Nguyễn Thị Bé - Vợ - 0632109876', 'Archive / AR-2024-012', NULL, NULL, 0, 4),

-- Nhân viên Đã nghỉ việc
('VNM-0037', 'Đoàn Thị Mỹ Linh', 'Linh', '1995-08-08', 'Nữ', '079095378901', '2020-08-20', 'CA TP.HCM', 'Q.12, TP.HCM', 'Q.12, TP.HCM', 'Q.12, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'linh.dtm@vinamilk.com.vn', '0978901237', '2020-03-01', 'CN_NAM', 'NV', 'NG_C2', 1.86, 2, 'Đại học', 'Quản trị', 'ĐH Mở TP.HCM', 0, 0, '3789012345', 'ACB', '8378901234', 0, 'Đoàn Văn Tài - Bố - 0621098765', 'Archive / AR-2025-005', NULL, NULL, 0, 3),

-- Thêm nhân viên bổ sung
('VNM-0038', 'Bùi Xuân Trường', 'Trường', '1991-03-17', 'Nam', '079091389012', '2020-09-10', 'CA TP. Cần Thơ', 'Ninh Kiều, Cần Thơ', 'Ninh Kiều, Cần Thơ', 'Ninh Kiều, Cần Thơ', 'Kinh', 'Không', 'Đã kết hôn', 'truong.bx@vinamilk.com.vn', '0989012348', '2015-09-01', 'NM_CT', 'TCA', 'NG_A3', 3.33, 3, 'Đại học', 'Công nghệ Thực phẩm', 'ĐH Cần Thơ', 0, 1, '3890123456', 'Vietinbank', '8389012345', 1, 'Nguyễn Thị Hồng - Vợ - 0610987654', 'Kho NM-CT / Tủ 01 / Ngăn B', 'Không thời hạn', NULL, 18020000, 1),

('VNM-0039', 'Lý Thị Mỹ Duyên', 'Duyên', '1993-12-25', 'Nữ', '079093390123', '2020-04-20', 'CA TP.HCM', 'Q.1, TP.HCM', 'Q.1, TP.HCM', 'Q.1, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'duyen.ltm@vinamilk.com.vn', '0990123459', '2018-04-15', 'PB_PHAPLUAT', 'CV', 'NG_A3', 3.33, 3, 'Thạc sĩ', 'Luật Kinh doanh', 'ĐH Luật TP.HCM', 0, 1, '3901234567', 'ACB', '8390123456', 0, 'Lý Văn Phát - Bố - 0609876543', 'Kho VP / Tủ 06 / Ngăn B', '3 năm', '2027-04-14', 23320000, 1),

('VNM-0040', 'Đinh Văn Bình', 'Bình', '1985-06-14', 'Nam', '079085401234', '2019-08-20', 'CA tỉnh Tuyên Quang', 'Sơn Dương, Tuyên Quang', 'Sơn Dương, Tuyên Quang', 'Sơn Dương, Tuyên Quang', 'Kinh', 'Không', 'Đã kết hôn', 'binh.dv@vinamilk.com.vn', '0901234570', '2012-08-01', 'TT_TQ', 'QL', 'NG_A3', 3.66, 4, 'Đại học', 'Chăn nuôi', 'ĐH Nông Lâm Thái Nguyên', 1, 1, '4012345678', 'BIDV', '8401234567', 2, 'Trần Thị Hoa - Vợ - 0598765432', 'Kho TT-TQ / Tủ 01 / Ngăn A', 'Không thời hạn', NULL, 23320000, 1),

('VNM-0041', 'Nguyễn Thị Bích Thảo', 'Thảo', '1992-09-03', 'Nữ', '079092412345', '2020-11-15', 'CA TP.HCM', 'Q.Phú Nhuận, TP.HCM', 'Q.Phú Nhuận, TP.HCM', 'Q.Phú Nhuận, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'thao.ntb@vinamilk.com.vn', '0912345673', '2019-11-01', 'PB_TMDT', 'QL', 'NG_A3', 3.33, 3, 'Đại học', 'TMĐT', 'ĐH Kinh tế TP.HCM', 0, 1, '4123456789', 'Techcombank', '8412345678', 0, 'Nguyễn Văn Khải - Bố - 0587654321', 'Kho VP / Tủ 05 / Ngăn C', '3 năm', '2028-10-31', 21200000, 1),

('VNM-0042', 'Tạ Quốc Huy', 'Huy', '1990-01-20', 'Nam', '079090423456', '2020-07-20', 'CA TP. Đà Nẵng', 'Hải Châu, Đà Nẵng', 'Hải Châu, Đà Nẵng', 'Hải Châu, Đà Nẵng', 'Kinh', 'Không', 'Độc thân', 'huy.tq@vinamilk.com.vn', '0923456783', '2016-07-15', 'NM_DN', 'CV', 'NG_KS', 3.33, 3, 'Đại học', 'Kỹ thuật Điện', 'ĐH Bách Khoa Đà Nẵng', 0, 1, '4234567890', 'VPBank', '8423456789', 0, 'Tạ Văn Minh - Bố - 0576543210', 'Kho NM-DN / Tủ 02 / Ngăn A', '3 năm', '2028-07-14', 16960000, 1),

('VNM-0043', 'Lê Phương Anh', 'Anh', '1997-11-08', 'Nữ', '079097434567', '2022-01-15', 'CA TP.HCM', 'Q.Tân Bình, TP.HCM', 'Q.Tân Bình, TP.HCM', 'Q.Tân Bình, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'anh.lp@vinamilk.com.vn', '0934567894', '2022-01-10', 'PB_NHANSU', 'CV', 'NG_A3', 2.34, 1, 'Đại học', 'Quản trị Nhân lực', 'ĐH Lao động Xã hội', 0, 1, '4345678901', 'MB Bank', '8434567890', 0, 'Lê Văn Dũng - Bố - 0565432109', 'Kho VP / Tủ 01 / Ngăn E', '3 năm', '2028-01-09', 13780000, 1),

('VNM-0044', 'Mai Xuân Đạt', 'Đạt', '1988-08-25', 'Nam', '079088445678', '2019-02-15', 'CA tỉnh Hà Tĩnh', 'Hương Sơn, Hà Tĩnh', 'Hương Sơn, Hà Tĩnh', 'Hương Sơn, Hà Tĩnh', 'Kinh', 'Không', 'Đã kết hôn', 'dat.mx@vinamilk.com.vn', '0945678905', '2014-02-01', 'TT_GREEN2', 'QL', 'NG_A3', 3.66, 4, 'Đại học', 'Chăn nuôi', 'ĐH Nông nghiệp HN', 1, 1, '4456789012', 'Agribank', '8445678901', 2, 'Nguyễn Thị Hương - Vợ - 0554321098', 'Kho TT-HT / Tủ 01 / Ngăn A', 'Không thời hạn', NULL, 21200000, 1),

('VNM-0045', 'Đặng Thị Hạnh', 'Hạnh', '1994-04-10', 'Nữ', '079094456789', '2020-07-10', 'CA TP.HCM', 'Q.10, TP.HCM', 'Q.10, TP.HCM', 'Q.10, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'hanh.dt@vinamilk.com.vn', '0956789016', '2020-07-01', 'PB_TAICHINH', 'NV', 'NG_A3', 2.34, 1, 'Đại học', 'Kế toán', 'ĐH Tài chính Marketing', 0, 1, '4567890123', 'Vietinbank', '8456789012', 0, 'Đặng Văn Thành - Bố - 0543210987', 'Kho VP / Tủ 02 / Ngăn C', '1 năm', '2027-06-30', 13780000, 1),

('VNM-0046', 'Hồ Sỹ Lâm', 'Lâm', '1992-07-19', 'Nam', '079092467890', '2020-10-15', 'CA tỉnh Nghệ An', 'TP. Vinh, Nghệ An', 'TP. Vinh, Nghệ An', 'TP. Vinh, Nghệ An', 'Kinh', 'Không', 'Đã kết hôn', 'lam.hs@vinamilk.com.vn', '0967890127', '2016-10-01', 'NM_NA', 'QL', 'NG_A3', 3.66, 4, 'Đại học', 'Công nghệ Thực phẩm', 'ĐH Vinh', 0, 1, '4678901234', 'BIDV', '8467890123', 1, 'Lê Thị Loan - Vợ - 0532109876', 'Kho NM-NA / Tủ 01 / Ngăn B', 'Không thời hạn', NULL, 21200000, 1),

('VNM-0047', 'Tôn Nữ Quỳnh Giao', 'Giao', '1990-12-12', 'Nữ', '079090478901', '2020-05-20', 'CA TP.HCM', 'Q.3, TP.HCM', 'Q.3, TP.HCM', 'Q.3, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'giao.tnq@vinamilk.com.vn', '0978901238', '2014-05-01', 'PB_HANHCHINH', 'TP', 'NG_A2', 4.74, 2, 'Thạc sĩ', 'Quản trị Văn phòng', 'ĐH KHXH&NV TP.HCM', 0, 1, '4789012345', 'ACB', '8478901234', 1, 'Trần Minh Tuấn - Chồng - 0521098765', 'Kho VP / Tủ 07 / Ngăn B', 'Không thời hạn', NULL, 23320000, 1),

('VNM-0048', 'Nguyễn Tiến Dũng', 'Dũng', '1995-06-06', 'Nam', '079095489012', '2020-09-15', 'CA TP.HCM', 'Q.12, TP.HCM', 'Q.12, TP.HCM', 'Q.12, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'dung.nt@vinamilk.com.vn', '0989012349', '2020-09-01', 'NM_SG', 'CN', 'NG_C3', 1.65, 1, 'Trung cấp', 'Cơ khí Chế tạo', 'TC Cơ khí TP.HCM', 0, 1, '4890123456', 'Sacombank', '8489012345', 0, 'Nguyễn Văn Tú - Bố - 0510987654', 'Kho NM-SG / Tủ 03 / Ngăn C', '1 năm', '2027-08-31', 10600000, 1),

('VNM-0049', 'Trịnh Minh Đức', 'Đức', '1993-09-22', 'Nam', '079093490123', '2020-11-10', 'CA TP.HCM', 'Thủ Đức, TP.HCM', 'Thủ Đức, TP.HCM', 'Thủ Đức, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'duc.tm@vinamilk.com.vn', '0990123460', '2018-11-01', 'PB_IT', 'CV', 'NG_A3', 3.00, 2, 'Đại học', 'Mạng máy tính', 'ĐH Công nghệ - ĐHQG HN', 0, 1, '4901234567', 'Techcombank', '8490123456', 0, 'Trịnh Văn Nam - Bố - 0509876543', 'Kho VP / Tủ 05 / Ngăn D', '3 năm', '2027-10-31', 20140000, 1),

('VNM-0050', 'Lê Thị Ngọc Diệp', 'Diệp', '1996-01-30', 'Nữ', '079096501234', '2022-04-15', 'CA TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Q.Bình Thạnh, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'diep.ltn@vinamilk.com.vn', '0901234571', '2022-04-01', 'PB_MARKETING', 'NV', 'NG_C2', 1.86, 2, 'Đại học', 'Báo chí', 'Học viện Báo chí Tuyên truyền', 0, 1, '5012345678', 'VPBank', '8501234567', 0, 'Lê Văn Bảo - Bố - 0498765432', 'Kho VP / Tủ 03 / Ngăn C', '1 năm', '2027-03-31', 12720000, 1),

('VNM-0051', 'Cao Minh Khôi', 'Khôi', '1997-03-14', 'Nam', '079097512345', '2026-08-20', 'CA tỉnh Bắc Ninh', 'Từ Sơn, Bắc Ninh', 'Từ Sơn, Bắc Ninh', 'Từ Sơn, Bắc Ninh', 'Kinh', 'Không', 'Độc thân', 'khoi.cm@vinamilk.com.vn', '0912345674', '2026-08-15', 'NM_TS', 'CN', 'NG_C3', 1.35, 1, 'Cao đẳng', 'Công nghệ Thực phẩm', 'CĐ Công nghiệp HN', 0, 0, '5123456789', 'MB Bank', '8512345678', 0, 'Cao Văn Long - Bố - 0487654321', 'Kho NM-TS / Tủ 05 / Ngăn A', 'Thử việc', '2026-10-14', 0, 2),

('VNM-0052', 'Phạm Thị Thanh Tâm', 'Tâm', '1989-10-05', 'Nữ', '079089523456', '2020-05-15', 'CA TP.HCM', 'Q.Phú Nhuận, TP.HCM', 'Q.Phú Nhuận, TP.HCM', 'Q.Phú Nhuận, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'tam.ptt@vinamilk.com.vn', '0923456784', '2015-05-01', 'PB_QLCL', 'CV', 'NG_A3', 3.33, 3, 'Đại học', 'Hóa Thực phẩm', 'ĐH KHTN TP.HCM', 0, 1, '5234567890', 'Sacombank', '8523456789', 1, 'Nguyễn Văn Hoàng - Chồng - 0476543210', 'Kho VP / Tủ 06 / Ngăn C', 'Không thời hạn', NULL, 18020000, 1),

('VNM-0053', 'Trịnh Vĩnh Tường', 'Tường', '1985-04-12', 'Nam', '079085031245', '2020-02-20', 'CA TP.HCM', 'Q.1, TP.HCM', 'Q.1, TP.HCM', 'Q.1, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'tuong.tv@vinamilk.com.vn', '0901234777', '2015-02-10', 'PB_NHANSU', 'CV', 'NG_A3', 3.33, 3, 'Thạc sĩ', 'Quản trị Kinh doanh', 'ĐH Kinh tế TP.HCM', 1, 1, '5312345678', 'Vietcombank', '8531234567', 2, 'Lê Thu Hà - Vợ - 0912345888', 'Kho VP / Tủ 01 / Ngăn F', 'Không thời hạn', NULL, 19080000, 1),

('VNM-0054', 'Đào Mỹ Kim', 'Kim', '1992-11-20', 'Nữ', '079092045612', '2020-05-25', 'CA TP.HCM', 'Q.7, TP.HCM', 'Q.7, TP.HCM', 'Q.7, TP.HCM', 'Kinh', 'Không', 'Độc thân', 'kim.dm@vinamilk.com.vn', '0909888777', '2018-05-15', 'CN_NAM', 'NV', 'NG_C2', 1.86, 2, 'Đại học', 'Marketing', 'ĐH Tôn Đức Thắng', 0, 1, '5412345678', 'Techcombank', '8541234567', 0, 'Đào Văn Phát - Bố - 0933444555', 'Kho CN-SG / Tủ 02 / Ngăn D', '3 năm', '2027-05-14', 12720000, 1),

('VNM-0055', 'Ngô Tấn Tài', 'Tài', '1990-08-08', 'Nam', '079090534567', '2020-08-10', 'CA TP.HCM', 'Q.Tân Bình, TP.HCM', 'Q.Tân Bình, TP.HCM', 'Q.Tân Bình, TP.HCM', 'Kinh', 'Không', 'Đã kết hôn', 'tai.nt@vinamilk.com.vn', '0988777666', '2013-03-01', 'PB_TAICHINH', 'CV', 'NG_A3', 3.66, 4, 'Đại học', 'Kế toán', 'ĐH Tài chính Marketing', 0, 1, '5512345678', 'BIDV', '8551234567', 1, 'Trần Thị Lan - Vợ - 0977666555', 'Kho VP / Tủ 02 / Ngăn D', 'Không thời hạn', NULL, 19080000, 1);

-- =====================================================================
-- 7.7 Dữ liệu qt_congtac (Quá trình công tác mẫu)
-- =====================================================================
INSERT INTO `qt_congtac` (`MaNV`, `TuNgay`, `DenNgay`, `DonViCongTac`, `ChucVu`, `CongViecChinh`) VALUES
('VNM-0001', '2002-06-01', '2008-12-31', 'Nhà máy Sữa Tiên Sơn', 'Kỹ sư Sản xuất', 'Vận hành dây chuyền sản xuất sữa tươi'),
('VNM-0001', '2009-01-01', '2015-06-30', 'Nhà máy Sữa Tiên Sơn', 'Quản đốc Phân xưởng', 'Quản lý phân xưởng chế biến'),
('VNM-0001', '2015-07-01', NULL, 'Nhà máy Sữa Tiên Sơn', 'Giám đốc Nhà máy', 'Điều hành toàn bộ hoạt động nhà máy'),
('VNM-0009', '2008-04-01', '2014-12-31', 'Trang trại Green Farm Tây Ninh', 'Bác sĩ Thú y', 'Chăm sóc sức khỏe đàn bò sữa'),
('VNM-0009', '2015-01-01', NULL, 'Trang trại Green Farm Tây Ninh', 'Giám đốc Trang trại', 'Điều hành trang trại, quản lý chất lượng sữa'),
('VNM-0014', '2007-02-01', '2012-12-31', 'Chi nhánh miền Bắc', 'Nhân viên Kinh doanh', 'Phát triển kênh phân phối miền Bắc'),
('VNM-0014', '2013-01-01', '2018-06-30', 'Chi nhánh miền Bắc', 'Trưởng phòng Kinh doanh', 'Quản lý đội ngũ bán hàng miền Bắc'),
('VNM-0014', '2018-07-01', NULL, 'Chi nhánh miền Bắc', 'Giám đốc Chi nhánh', 'Điều hành toàn bộ chi nhánh miền Bắc'),
('VNM-0020', '2005-01-10', '2010-12-31', 'Phòng Nhân sự', 'Chuyên viên Nhân sự', 'Tuyển dụng, đào tạo nhân viên mới'),
('VNM-0020', '2011-01-01', '2016-06-30', 'Phòng Nhân sự', 'Trưởng phòng Nhân sự', 'Quản lý phòng Nhân sự, xây dựng chính sách'),
('VNM-0020', '2016-07-01', NULL, 'Phòng Nhân sự', 'Giám đốc Nhân sự (CHRO)', 'Hoạch định chiến lược nhân sự toàn công ty'),
('VNM-0024', '2004-09-01', '2010-08-31', 'Phòng Tài chính - Kế toán', 'Kế toán trưởng', 'Quản lý sổ sách kế toán'),
('VNM-0024', '2010-09-01', NULL, 'Phòng Tài chính - Kế toán', 'Giám đốc Tài chính (CFO)', 'Hoạch định chiến lược tài chính công ty'),
('VNM-0035', '1995-06-01', '2005-12-31', 'Nhà máy Sữa Thống Nhất', 'Kỹ sư Sản xuất', 'Nghiên cứu cải tiến quy trình sản xuất'),
('VNM-0035', '2006-01-01', '2015-12-31', 'Nhà máy Sữa Thống Nhất', 'Trưởng phòng Kỹ thuật', 'Quản lý bộ phận kỹ thuật nhà máy'),
('VNM-0035', '2016-01-01', NULL, 'Nhà máy Sữa Thống Nhất', 'Cố vấn Kỹ thuật', 'Tư vấn kỹ thuật, đào tạo thế hệ kế cận');

-- =====================================================================
-- 7.8 Dữ liệu qt_bonhiem (Quá trình bổ nhiệm mẫu)
-- =====================================================================
INSERT INTO `qt_bonhiem` (`MaNV`, `SoQuyetDinh`, `NgayQuyetDinh`, `ChucVuCu`, `ChucVuMoi`, `DonVi`, `NguoiKy`) VALUES
('VNM-0001', 'QĐ-2009/VNM-NS', '2009-01-01', 'Kỹ sư Sản xuất', 'Quản đốc Phân xưởng', 'Nhà máy Sữa Tiên Sơn', 'Mai Kiều Liên'),
('VNM-0001', 'QĐ-2015/VNM-NS', '2015-07-01', 'Quản đốc Phân xưởng', 'Giám đốc Nhà máy', 'Nhà máy Sữa Tiên Sơn', 'Mai Kiều Liên'),
('VNM-0009', 'QĐ-2015/VNM-NS', '2015-01-01', 'Bác sĩ Thú y', 'Giám đốc Trang trại', 'TT Green Farm Tây Ninh', 'Nguyễn Hữu Dũng'),
('VNM-0014', 'QĐ-2013/VNM-NS', '2013-01-01', 'Nhân viên Kinh doanh', 'Trưởng phòng Kinh doanh', 'Chi nhánh miền Bắc', 'Mai Kiều Liên'),
('VNM-0014', 'QĐ-2018/VNM-NS', '2018-07-01', 'Trưởng phòng Kinh doanh', 'Giám đốc Chi nhánh', 'Chi nhánh miền Bắc', 'Nguyễn Hữu Dũng'),
('VNM-0020', 'QĐ-2011/VNM-NS', '2011-01-01', 'Chuyên viên Nhân sự', 'Trưởng phòng Nhân sự', 'Phòng Nhân sự', 'Mai Kiều Liên'),
('VNM-0020', 'QĐ-2016/VNM-NS', '2016-07-01', 'Trưởng phòng Nhân sự', 'Giám đốc Nhân sự (CHRO)', 'Phòng Nhân sự', 'Mai Kiều Liên'),
('VNM-0024', 'QĐ-2010/VNM-NS', '2010-09-01', 'Kế toán trưởng', 'Giám đốc Tài chính (CFO)', 'Phòng Tài chính - Kế toán', 'Mai Kiều Liên'),
('VNM-0021', 'QĐ-2018/VNM-NS', '2018-01-01', 'Chuyên viên Tuyển dụng', 'Trưởng phòng Tuyển dụng', 'Phòng Nhân sự', 'Nguyễn Thị Thanh Huyền'),
('VNM-0025', 'QĐ-2019/VNM-NS', '2019-01-01', 'Kế toán viên', 'Kế toán trưởng', 'Phòng Tài chính - Kế toán', 'Đặng Minh Trí'),
('VNM-0026', 'QĐ-2018/VNM-NS', '2018-04-01', 'Chuyên viên Marketing', 'Trưởng phòng Marketing', 'Phòng Marketing', 'Nguyễn Hữu Dũng'),
('VNM-0028', 'QĐ-2016/VNM-NS', '2016-01-01', 'Nghiên cứu viên chính', 'Giám đốc R&D', 'Trung tâm R&D', 'Mai Kiều Liên'),
('VNM-0030', 'QĐ-2020/VNM-NS', '2020-01-01', 'Chuyên viên IT', 'Trưởng phòng IT', 'Phòng CNTT', 'Nguyễn Hữu Dũng'),
('VNM-0032', 'QĐ-2017/VNM-NS', '2017-07-01', 'Chuyên viên QC', 'Trưởng phòng Chất lượng', 'Phòng QLCL', 'Lương Đức Anh'),
('VNM-0047', 'QĐ-2019/VNM-NS', '2019-01-01', 'Chuyên viên Hành chính', 'Trưởng phòng Hành chính', 'Phòng Hành chính', 'Nguyễn Hữu Dũng');

-- =====================================================================
-- 7.9 Dữ liệu qt_luong (Diễn biến lương mẫu)
-- =====================================================================
INSERT INTO `qt_luong` (`MaNV`, `TuNgay`, `HeSoLuong`, `BacLuong`, `VuotKhung`, `NgayHuong`, `SoQD`) VALUES
('VNM-0001', '2002-06-01', 2.34, 1, 0.00, '2002-06-01', 'QĐ-LNG-2002/001'),
('VNM-0001', '2005-06-01', 3.00, 2, 0.00, '2005-06-01', 'QĐ-LNG-2005/001'),
('VNM-0001', '2008-06-01', 3.66, 3, 0.00, '2008-06-01', 'QĐ-LNG-2008/001'),
('VNM-0001', '2011-06-01', 4.40, 4, 0.00, '2011-06-01', 'QĐ-LNG-2011/001'),
('VNM-0001', '2015-07-01', 5.56, 5, 0.00, '2015-07-01', 'QĐ-LNG-2015/001'),
('VNM-0001', '2018-07-01', 7.28, 6, 0.00, '2018-07-01', 'QĐ-LNG-2018/001'),
('VNM-0020', '2005-01-10', 2.34, 1, 0.00, '2005-01-10', 'QĐ-LNG-2005/020'),
('VNM-0020', '2008-01-10', 3.33, 2, 0.00, '2008-01-10', 'QĐ-LNG-2008/020'),
('VNM-0020', '2011-01-01', 4.40, 3, 0.00, '2011-01-01', 'QĐ-LNG-2011/020'),
('VNM-0020', '2014-01-01', 5.56, 4, 0.00, '2014-01-01', 'QĐ-LNG-2014/020'),
('VNM-0020', '2017-01-01', 6.78, 3, 0.00, '2017-01-01', 'QĐ-LNG-2017/020'),
('VNM-0024', '2004-09-01', 2.34, 1, 0.00, '2004-09-01', 'QĐ-LNG-2004/024'),
('VNM-0024', '2007-09-01', 3.66, 3, 0.00, '2007-09-01', 'QĐ-LNG-2007/024'),
('VNM-0024', '2010-09-01', 5.56, 4, 0.00, '2010-09-01', 'QĐ-LNG-2010/024'),
('VNM-0024', '2016-09-01', 7.28, 6, 0.00, '2016-09-01', 'QĐ-LNG-2016/024'),
('VNM-0035', '1995-06-01', 2.34, 1, 0.00, '1995-06-01', 'QĐ-LNG-1995/035'),
('VNM-0035', '1998-06-01', 3.00, 2, 0.00, '1998-06-01', 'QĐ-LNG-1998/035'),
('VNM-0035', '2001-06-01', 3.66, 3, 0.00, '2001-06-01', 'QĐ-LNG-2001/035'),
('VNM-0035', '2004-06-01', 4.40, 4, 0.00, '2004-06-01', 'QĐ-LNG-2004/035'),
('VNM-0035', '2007-06-01', 5.56, 5, 0.00, '2007-06-01', 'QĐ-LNG-2007/035'),
('VNM-0035', '2010-06-01', 6.20, 6, 0.00, '2010-06-01', 'QĐ-LNG-2010/035'),
('VNM-0035', '2013-06-01', 6.78, 7, 0.00, '2013-06-01', 'QĐ-LNG-2013/035'),
('VNM-0035', '2016-06-01', 7.28, 8, 0.00, '2016-06-01', 'QĐ-LNG-2016/035'),
('VNM-0035', '2019-06-01', 8.00, 12, 5.00, '2019-06-01', 'QĐ-LNG-2019/035');

-- =====================================================================
-- 7.10 Dữ liệu qt_daotao (Đào tạo mẫu)
-- =====================================================================
INSERT INTO `qt_daotao` (`MaNV`, `TenKhoaHoc`, `CoSoDaoTao`, `HinhThuc`, `TuNgay`, `DenNgay`, `BangCap_ChungChi`, `XepLoai`) VALUES
('VNM-0001', 'ISO 22000:2018 - Hệ thống quản lý ATTP', 'Bureau Veritas Vietnam', 'NganHan', '2019-03-10', '2019-03-14', 'Chứng chỉ ISO 22000', 'Đạt'),
('VNM-0001', 'HACCP - Phân tích mối nguy kiểm soát', 'SGS Vietnam', 'NganHan', '2018-06-05', '2018-06-08', 'Chứng chỉ HACCP', 'Đạt'),
('VNM-0001', 'Thạc sĩ Kỹ thuật Thực phẩm', 'ĐH Bách Khoa HN', 'ChinhQuy', '2007-09-01', '2009-06-30', 'Bằng Thạc sĩ', 'Giỏi'),
('VNM-0009', 'Thạc sĩ Thú y', 'ĐH Nông nghiệp HN', 'ChinhQuy', '2010-09-01', '2012-06-30', 'Bằng Thạc sĩ', 'Giỏi'),
('VNM-0009', 'An toàn sinh học trang trại', 'FAO Vietnam', 'NganHan', '2019-05-15', '2019-05-18', 'Chứng chỉ An toàn sinh học', 'Đạt'),
('VNM-0020', 'Tiến sĩ Quản trị Nhân sự', 'ĐH Kinh tế Quốc dân', 'ChinhQuy', '2012-09-01', '2016-06-30', 'Bằng Tiến sĩ', 'Xuất sắc'),
('VNM-0020', 'SHRM Senior Certified Professional', 'SHRM USA', 'Online', '2018-01-15', '2018-06-30', 'Chứng chỉ SHRM-SCP', 'Đạt'),
('VNM-0024', 'Tiến sĩ Tài chính', 'ĐH Kinh tế TP.HCM', 'ChinhQuy', '2010-09-01', '2015-06-30', 'Bằng Tiến sĩ', 'Giỏi'),
('VNM-0024', 'CFA Level III', 'CFA Institute', 'TuXa', '2016-01-01', '2018-12-31', 'Chứng chỉ CFA', 'Đạt'),
('VNM-0028', 'Tiến sĩ Công nghệ Thực phẩm', 'ĐH Bách Khoa TP.HCM', 'ChinhQuy', '2013-09-01', '2017-06-30', 'Bằng Tiến sĩ', 'Giỏi'),
('VNM-0030', 'PMP - Project Management Professional', 'PMI', 'Online', '2020-03-01', '2020-06-30', 'Chứng chỉ PMP', 'Đạt'),
('VNM-0030', 'AWS Solutions Architect Associate', 'Amazon Web Services', 'Online', '2021-01-15', '2021-04-30', 'Chứng chỉ AWS SAA', 'Đạt'),
('VNM-0032', 'ISO 22000 Lead Auditor', 'TÜV Rheinland', 'NganHan', '2016-09-05', '2016-09-12', 'Chứng chỉ Lead Auditor', 'Đạt'),
('VNM-0032', 'Six Sigma Green Belt', 'ASQ', 'Online', '2019-01-15', '2019-06-30', 'Chứng chỉ SSGB', 'Đạt'),
('VNM-0006', 'ISO 22000:2018', 'Bureau Veritas Vietnam', 'NganHan', '2020-04-15', '2020-04-18', 'Chứng chỉ ISO 22000', 'Đạt'),
('VNM-0006', 'GMP - Thực hành sản xuất tốt', 'SGS Vietnam', 'NganHan', '2019-08-10', '2019-08-12', 'Chứng chỉ GMP', 'Đạt'),
('VNM-0025', 'ACCA Qualification', 'ACCA UK', 'TuXa', '2016-01-01', '2019-12-31', 'Chứng chỉ ACCA', 'Đạt'),
('VNM-0026', 'Thạc sĩ Marketing', 'ĐH RMIT', 'ChinhQuy', '2015-09-01', '2017-06-30', 'Bằng Thạc sĩ', 'Khá');

-- =====================================================================
-- 7.11 Dữ liệu qt_ktkl (Khen thưởng kỷ luật mẫu)
-- =====================================================================
INSERT INTO `qt_ktkl` (`MaNV`, `MaKTKL`, `SoQD`, `NgayQD`, `HinhThuc`, `LyDo`, `CapKhenThuong`, `GiaTri`) VALUES
('VNM-0001', 'KT_BK', 'QĐ-KT-2025/001', '2025-01-15', 'Bằng khen Công ty', 'Hoàn thành xuất sắc kế hoạch sản xuất năm 2024', 'Công ty', 5000000),
('VNM-0001', 'KT_CSTD', 'QĐ-KT-2024/001', '2024-01-20', 'Chiến sĩ thi đua cơ sở', 'Đạt thành tích xuất sắc năm 2023', 'Công ty', 3000000),
('VNM-0009', 'KT_ST', 'QĐ-KT-2024/009', '2024-06-15', 'Sáng kiến - Cải tiến', 'Áp dụng công nghệ IoT giám sát sức khỏe bò sữa', 'Công ty', 10000000),
('VNM-0020', 'KT_LDTT', 'QĐ-KT-2025/020', '2025-01-15', 'Lao động tiên tiến', 'Hoàn thành tốt nhiệm vụ năm 2024', 'Công ty', 2000000),
('VNM-0024', 'KT_BK', 'QĐ-KT-2025/024', '2025-01-15', 'Bằng khen Công ty', 'Quản lý tài chính hiệu quả, tiết kiệm 5% ngân sách', 'Công ty', 5000000),
('VNM-0028', 'KT_ST', 'QĐ-KT-2025/028', '2025-03-20', 'Sáng kiến - Cải tiến', 'Phát triển sản phẩm sữa tươi Green Farm mới', 'Công ty', 10000000),
('VNM-0035', 'KT_BK', 'QĐ-KT-2024/035', '2024-09-02', 'Bằng khen Công ty', 'Cống hiến 30 năm cho ngành sữa Việt Nam', 'Bộ Công Thương', 5000000),
('VNM-0006', 'KT_LDTT', 'QĐ-KT-2025/006', '2025-01-15', 'Lao động tiên tiến', 'Hoàn thành tốt nhiệm vụ trưởng ca năm 2024', 'Công ty', 2000000),
('VNM-0022', 'KT_LDTT', 'QĐ-KT-2025/022', '2025-01-15', 'Lao động tiên tiến', 'Xây dựng hệ thống C&B hiệu quả', 'Công ty', 2000000),
('VNM-0016', 'KL_KT', 'QĐ-KL-2025/016', '2025-05-10', 'Khiển trách', 'Đi trễ 5 lần trong tháng 4/2025', 'Công ty', -500000),
('VNM-0033', 'KL_KT', 'QĐ-KL-2026/033', '2026-02-15', 'Khiển trách', 'Vi phạm quy trình an toàn lao động', 'Nhà máy', -500000),
('VNM-0030', 'KT_TH', 'QĐ-KT-2026/030', '2026-04-01', 'Thưởng thành tích đột xuất', 'Triển khai thành công hệ thống HRM mới', 'Công ty', 5000000);

-- =====================================================================
-- 7.12 Dữ liệu qt_thuyenchuyen (Thuyên chuyển mẫu)
-- =====================================================================
INSERT INTO `qt_thuyenchuyen` (`MaNV`, `SoQD`, `DonViCu`, `DonViMoi`, `ChucVuCu`, `ChucVuMoi`, `NgayHieuLuc`, `LyDo`) VALUES
('VNM-0006', 'QĐ-TC-2012/006', 'NM_SG', 'NM_BD', 'Công nhân Vận hành', 'Trưởng ca Sản xuất', '2012-05-15', 'Bổ nhiệm và điều chuyển tăng cường cho NM Bình Dương'),
('VNM-0013', 'QĐ-TC-2013/013', 'TT_GREEN1', 'TT_RESEDA', 'Bác sĩ Thú y', 'Trưởng trại bò sữa', '2013-07-20', 'Điều chuyển phụ trách trang trại mới'),
('VNM-0018', 'QĐ-TC-2016/018', 'CN_NAM', 'CN_TRUNG', 'Tài xế Giao hàng', 'Tài xế Giao hàng', '2016-01-01', 'Điều chuyển theo nguyện vọng về quê'),
('VNM-0042', 'QĐ-TC-2020/042', 'NM_SG', 'NM_DN', 'Kỹ sư Cơ điện', 'Kỹ sư Cơ điện', '2020-01-01', 'Tăng cường lực lượng kỹ thuật cho NM Đà Nẵng');

-- =====================================================================
-- 7.13 Dữ liệu chamcong (Chấm công tháng 8 & 9/2026)
-- =====================================================================
INSERT INTO `chamcong` (`MaNV`, `Thang`, `Nam`, `SoNgayCongChuan`, `NgayCongThucTe`, `NghiPhep`, `NghiKhongLuong`, `NghiLe`, `LamThemGio`, `DiTreVeSom`, `TrangThaiDuyet`) VALUES
-- Tháng 8/2026
('VNM-0001', 8, 2026, 22.0, 22.0, 0.0, 0.0, 0.0, 8.0, 0, 'DaDuyet'),
('VNM-0002', 8, 2026, 22.0, 21.0, 1.0, 0.0, 0.0, 4.0, 0, 'DaDuyet'),
('VNM-0003', 8, 2026, 22.0, 22.0, 0.0, 0.0, 0.0, 12.0, 0, 'DaDuyet'),
('VNM-0004', 8, 2026, 22.0, 21.5, 0.0, 0.5, 0.0, 16.0, 1, 'DaDuyet'),
('VNM-0006', 8, 2026, 22.0, 22.0, 0.0, 0.0, 0.0, 10.0, 0, 'DaDuyet'),
('VNM-0009', 8, 2026, 22.0, 22.0, 0.0, 0.0, 0.0, 6.0, 0, 'DaDuyet'),
('VNM-0014', 8, 2026, 22.0, 20.0, 2.0, 0.0, 0.0, 4.0, 0, 'DaDuyet'),
('VNM-0020', 8, 2026, 22.0, 21.0, 1.0, 0.0, 0.0, 8.0, 0, 'DaDuyet'),
('VNM-0024', 8, 2026, 22.0, 22.0, 0.0, 0.0, 0.0, 6.0, 0, 'DaDuyet'),
('VNM-0028', 8, 2026, 22.0, 21.0, 1.0, 0.0, 0.0, 10.0, 0, 'DaDuyet'),
('VNM-0030', 8, 2026, 22.0, 22.0, 0.0, 0.0, 0.0, 20.0, 0, 'DaDuyet'),
-- Tháng 9/2026 (tháng hiện tại - chờ duyệt)
('VNM-0001', 9, 2026, 22.0, 19.0, 0.0, 0.0, 1.0, 4.0, 0, 'ChoDuyet'),
('VNM-0002', 9, 2026, 22.0, 18.0, 0.0, 0.0, 1.0, 0.0, 0, 'ChoDuyet'),
('VNM-0003', 9, 2026, 22.0, 19.0, 0.0, 0.0, 1.0, 8.0, 0, 'ChoDuyet'),
('VNM-0006', 9, 2026, 22.0, 19.0, 0.0, 0.0, 1.0, 6.0, 0, 'ChoDuyet'),
('VNM-0020', 9, 2026, 22.0, 18.0, 1.0, 0.0, 1.0, 4.0, 0, 'ChoDuyet'),
('VNM-0030', 9, 2026, 22.0, 19.0, 0.0, 0.0, 1.0, 16.0, 0, 'ChoDuyet');

-- =====================================================================
-- 7.14 Dữ liệu bangluong (Bảng lương tháng 8/2026)
-- =====================================================================
INSERT INTO `bangluong` (`MaNV`, `Thang`, `Nam`, `LuongCoBan`, `HeSoLuong`, `PhuCapChucVu`, `PhuCapKhac`, `LuongNangLuc`, `ThuongKPI`, `ThuongKhac`, `TongThuNhap`, `KhauTruBHXH`, `ThueTNCN`, `KhauTruKhac`, `ThucLinh`, `SoNgayCong`, `TrangThaiDuyet`) VALUES
('VNM-0001', 8, 2026, 35000000, 7.28, 12000000, 8000000, 18000000, 12000000, 0, 85000000, 3780000, 8900000, 0, 72320000, 22.0, 'DaChi'),
('VNM-0002', 8, 2026, 22000000, 3.66, 5000000,  5000000, 12000000,  8000000, 0, 52000000, 2457000, 4200000, 0, 45343000, 21.0, 'DaChi'),
('VNM-0003', 8, 2026, 14000000, 2.46, 3000000,  3000000,  7000000,  5000000, 0, 32000000, 1556100, 1800000, 0, 28643900, 22.0, 'DaChi'),
('VNM-0004', 8, 2026,  9500000, 1.86, 1500000,  2500000,  3500000,  2500000, 0, 19500000, 1050000,  600000, 0, 17850000, 21.5, 'DaChi'),
('VNM-0006', 8, 2026, 18000000, 3.33, 4000000,  4500000,  9000000,  7000000, 0, 42500000, 2003400, 3200000, 0, 37296600, 22.0, 'DaChi'),
('VNM-0009', 8, 2026, 32000000, 5.56, 10000000, 7000000, 15000000, 10000000, 0, 74000000, 3561600, 7500000, 0, 62938400, 22.0, 'DaChi'),
('VNM-0014', 8, 2026, 30000000, 5.56, 15000000, 8000000, 15000000, 15000000, 0, 83000000, 3339000, 8600000, 0, 71061000, 20.0, 'DaChi'),
('VNM-0020', 8, 2026, 45000000, 6.78, 15000000, 10000000, 20000000, 15000000, 0, 105000000, 3780000, 13500000, 0, 87720000, 21.0, 'DaChi'),
('VNM-0024', 8, 2026, 48000000, 7.28, 15000000, 12000000, 22000000, 18000000, 0, 115000000, 3780000, 15800000, 0, 95420000, 22.0, 'DaChi'),
('VNM-0028', 8, 2026, 40000000, 6.78, 15000000, 8000000,  20000000, 12000000, 0, 95000000, 3780000, 11500000, 0, 79720000, 21.0, 'DaChi'),
('VNM-0030', 8, 2026, 28000000, 5.08, 8000000,  5000000, 15000000, 10000000, 0, 66000000, 3116400, 6200000, 0, 56683600, 22.0, 'DaChi');

-- =====================================================================
-- 7.15 Dữ liệu tintuyendung
-- =====================================================================
INSERT INTO `tintuyendung` (`TieuDe`, `MaDV`, `ViTriTuyen`, `SoLuong`, `MoTaCongViec`, `YeuCau`, `QuyenLoi`, `MucLuong`, `NoiLamViec`, `HanNop`, `NguoiTao`, `TrangThai`) VALUES
('Tuyển Kỹ sư Sản xuất - NM Tiên Sơn', 'NM_TS', 'Kỹ sư Sản xuất', 2, 'Vận hành và giám sát dây chuyền sản xuất sữa tươi tiệt trùng. Đảm bảo chất lượng sản phẩm theo tiêu chuẩn ISO 22000.', 'Tốt nghiệp ĐH ngành Công nghệ Thực phẩm hoặc tương đương. Có kinh nghiệm 2 năm trở lên. Ưu tiên có chứng chỉ ISO 22000, HACCP.', 'Lương cạnh tranh, BHXH đầy đủ, thưởng KPI, du lịch hàng năm, sản phẩm Vinamilk miễn phí.', '15-25 triệu', 'KCN Tiên Sơn, Bắc Ninh', '2026-10-31', 'VNM-0021', 'DangTuyen'),

('Tuyển Bác sĩ Thú y - TT Organic Đà Lạt', 'TT_ORGANIC1', 'Bác sĩ Thú y', 1, 'Chăm sóc sức khỏe đàn bò sữa organic. Giám sát chương trình tiêm phòng và điều trị bệnh. Theo dõi chất lượng sữa nguyên liệu.', 'Tốt nghiệp ĐH ngành Thú y. Có chứng chỉ hành nghề Thú y. Kinh nghiệm 3 năm trở lên tại trang trại bò sữa.', 'Lương 18-25 triệu, nhà ở tại trang trại, phương tiện đi lại, BHXH đầy đủ.', '18-25 triệu', 'TP. Đà Lạt, Lâm Đồng', '2026-11-15', 'VNM-0021', 'DangTuyen'),

('Tuyển Chuyên viên IT - Phòng CNTT', 'PB_IT', 'Lập trình viên PHP/MySQL', 1, 'Phát triển và bảo trì hệ thống HRM nội bộ. Xây dựng API RESTful, tích hợp hệ thống chấm công.', 'Tốt nghiệp ĐH CNTT, thành thạo PHP 8.x, MySQL, JavaScript. Kinh nghiệm MVC, Git.', 'Lương 18-28 triệu, linh hoạt giờ làm, đào tạo chứng chỉ quốc tế.', '18-28 triệu', 'Q.7, TP.HCM', '2026-10-15', 'VNM-0021', 'DangTuyen'),

('Tuyển Nhân viên Bán hàng - CN miền Nam', 'CN_NAM', 'Nhân viên Bán hàng', 5, 'Phát triển thị trường khu vực TP.HCM và các tỉnh miền Nam. Chăm sóc đại lý, giám sát trưng bày sản phẩm.', 'Tốt nghiệp Cao đẳng trở lên. Ngoại hình ưa nhìn, giao tiếp tốt. Ưu tiên có kinh nghiệm FMCG.', 'Lương cơ bản + hoa hồng, xe công tác, điện thoại.', '8-15 triệu + hoa hồng', 'TP.HCM & các tỉnh miền Nam', '2026-10-31', 'VNM-0043', 'DangTuyen'),

('Tuyển Kế toán viên - Phòng TC-KT', 'PB_TAICHINH', 'Kế toán viên', 1, 'Thực hiện nghiệp vụ kế toán tổng hợp, kiểm tra chứng từ, lập báo cáo tài chính hàng tháng/quý.', 'Tốt nghiệp ĐH Kế toán, Tài chính. Thành thạo Excel, phần mềm kế toán. Kinh nghiệm 1 năm.', 'Lương 12-18 triệu, BHXH, thưởng KPI, sản phẩm Vinamilk.', '12-18 triệu', 'Q.7, TP.HCM', '2026-11-30', 'VNM-0043', 'ChoDuyet');

-- =====================================================================
-- 7.16 Dữ liệu hoso_ungvien
-- =====================================================================
INSERT INTO `hoso_ungvien` (`MaTT`, `HoTen`, `NgaySinh`, `GioiTinh`, `Email`, `SoDienThoai`, `DiaChi`, `TrinhDo`, `ChuyenNganh`, `KinhNghiem`, `NguonHoSo`, `NgayNop`, `TrangThai`) VALUES
(1, 'Vũ Quang Minh', '1996-05-20', 'Nam', 'minh.vq@gmail.com', '0911222333', 'Bắc Ninh', 'Đại học', 'Công nghệ Thực phẩm', '3 năm tại nhà máy thực phẩm ABC', 'Vietnamworks', '2026-09-15', 'PhongVan'),
(1, 'Trần Thị Hải Yến', '1997-08-12', 'Nữ', 'yen.tth@gmail.com', '0922333444', 'Hải Dương', 'Đại học', 'Kỹ thuật Thực phẩm', '2 năm QC tại nhà máy bia Tiger', 'Website Vinamilk', '2026-09-18', 'SangLoc'),
(1, 'Nguyễn Hoàng Sơn', '1995-02-28', 'Nam', 'son.nh@gmail.com', '0933444555', 'Bắc Ninh', 'Đại học', 'Công nghệ Sinh học', '4 năm kỹ sư sản xuất tại Masan', 'Referral', '2026-09-10', 'PhongVan'),
(2, 'Lê Thị Phương Thảo', '1993-11-05', 'Nữ', 'thao.ltp@gmail.com', '0944555666', 'Đà Lạt', 'Đại học', 'Thú y', '5 năm trang trại bò sữa TH True Milk', 'TopCV', '2026-09-20', 'PhongVan'),
(3, 'Hoàng Đức Trung', '1998-03-15', 'Nam', 'trung.hd@gmail.com', '0955666777', 'TP.HCM', 'Đại học', 'CNTT', '2 năm PHP Developer tại FPT Software', 'LinkedIn', '2026-09-12', 'TrungTuyen'),
(3, 'Đỗ Minh Anh', '1999-07-22', 'Nữ', 'anh.dm@gmail.com', '0966777888', 'TP.HCM', 'Đại học', 'CNTT', '1 năm Intern + 1 năm Junior tại Tiki', 'Website Vinamilk', '2026-09-14', 'SangLoc'),
(4, 'Trương Văn Quốc', '2000-01-10', 'Nam', 'quoc.tv@gmail.com', '0977888999', 'TP.HCM', 'Cao đẳng', 'Kinh tế', 'Fresher, từng làm part-time bán hàng', 'Facebook', '2026-09-22', 'MoiNop'),
(4, 'Mai Thị Bích Phượng', '1998-04-18', 'Nữ', 'phuong.mtb@gmail.com', '0988999000', 'Bình Dương', 'Đại học', 'Marketing', '2 năm tại Unilever', 'Vietnamworks', '2026-09-21', 'SangLoc'),
(4, 'Đinh Trọng Đạt', '1997-09-30', 'Nam', 'dat.dt@gmail.com', '0999000111', 'TP.HCM', 'Đại học', 'QTKD', '3 năm sales tại Coca-Cola', 'Referral', '2026-09-19', 'PhongVan'),
(4, 'Lý Thị Thu Hương', '2001-06-14', 'Nữ', 'huong.ltt@gmail.com', '0900111222', 'Đồng Nai', 'Cao đẳng', 'Marketing', 'Fresher', 'TopCV', '2026-09-25', 'MoiNop');

-- =====================================================================
-- 7.17 Dữ liệu ketqua_phongvan
-- =====================================================================
INSERT INTO `ketqua_phongvan` (`MaUV`, `VongPhongVan`, `NgayPhongVan`, `NguoiPhongVan`, `DiemDanhGia`, `NhanXet`, `KetQua`) VALUES
(1, 1, '2026-09-22', 'Lê Anh Dũng (TP Tuyển dụng)', 7.5, 'Ứng viên có kinh nghiệm phù hợp, kỹ năng giao tiếp tốt, am hiểu quy trình sản xuất thực phẩm.', 'Dat'),
(1, 2, '2026-09-25', 'Nguyễn Văn Hùng (GĐ NM Tiên Sơn)', 8.0, 'Kiến thức chuyên môn vững, có chứng chỉ ISO 22000. Phù hợp vị trí kỹ sư sản xuất.', 'Dat'),
(3, 1, '2026-09-20', 'Lê Phương Anh (CV Tuyển dụng)', 8.5, 'Ứng viên có 4 năm kinh nghiệm tại Masan, am hiểu sâu quy trình sản xuất sữa. Tinh thần làm việc tốt.', 'Dat'),
(4, 1, '2026-09-25', 'Lê Anh Dũng (TP Tuyển dụng)', 8.0, 'Ứng viên có 5 năm kinh nghiệm trang trại bò sữa TH True Milk. Có đầy đủ chứng chỉ hành nghề.', 'Dat'),
(4, 2, '2026-09-27', 'Bùi Thanh Sơn (GĐ TT Green Farm TN)', 8.5, 'Kiến thức chuyên sâu về thú y bò sữa, kinh nghiệm thực tiễn phong phú.', 'Dat'),
(5, 1, '2026-09-18', 'Lê Phương Anh (CV Tuyển dụng)', 9.0, 'Kỹ năng PHP/MySQL xuất sắc. Có portfolio dự án MVC thực tế. Phù hợp tuyệt vời.', 'Dat'),
(5, 2, '2026-09-22', 'Nguyễn Minh Phúc (TP IT)', 9.5, 'Code clean, hiểu sâu design pattern. Có kinh nghiệm xây dựng hệ thống ERP. Đề xuất nhận ngay.', 'Dat'),
(9, 1, '2026-09-24', 'Lê Phương Anh (CV Tuyển dụng)', 7.0, 'Ứng viên có kinh nghiệm sales 3 năm tại Coca-Cola. Kỹ năng thuyết phục tốt.', 'Dat');

-- =====================================================================
-- 7.18 Dữ liệu roles
-- =====================================================================
INSERT INTO `roles` (`RoleID`, `RoleName`, `RoleCode`, `Description`) VALUES
(1, 'Quản trị viên', 'Admin', 'Toàn quyền quản trị hệ thống, cấu hình, phân quyền'),
(2, 'Quản lý Nhân sự', 'HR_Manager', 'Quản lý nhân sự, tuyển dụng, đào tạo, C&B'),
(3, 'Trưởng phòng/Quản lý', 'Manager', 'Quản lý nhân viên trong phòng ban, duyệt công, đánh giá'),
(4, 'Nhân viên', 'Employee', 'Xem thông tin cá nhân, chấm công, tra cứu lương');

-- =====================================================================
-- 7.19 Dữ liệu users (Mật khẩu mặc định: Vinamilk@2026)
-- Bcrypt hash của 'Vinamilk@2026'
-- =====================================================================
INSERT INTO `users` (`Username`, `Password`, `MaNV`, `RoleID`, `IsActive`) VALUES
('admin',       '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', NULL,        1, 1),
('huyen.ntt',   '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', 'VNM-0020',  1, 1),
('dung.la',     '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', 'VNM-0021',  2, 1),
('long.th',     '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', 'VNM-0022',  2, 1),
('anh.lp',      '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', 'VNM-0043',  2, 1),
('tri.dm',      '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', 'VNM-0024',  3, 1),
('hung.nv',     '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', 'VNM-0001',  3, 1),
('son.bt',      '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', 'VNM-0009',  3, 1),
('phuc.nm',     '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', 'VNM-0030',  3, 1),
('tung.pt',     '$2y$10$kFv3xmGhLQqrJHhVw0L0ZOYjvTjN6xLyGr0PJQ5vGK8qFzNq8MxGi', 'VNM-0016',  4, 1);

-- =====================================================================
-- 7.20 Dữ liệu permissions
-- =====================================================================
INSERT INTO `permissions` (`ModuleCode`, `ModuleName`, `ActionCode`, `Description`) VALUES
-- Module Nhân sự
('NHANSU', 'Quản lý Nhân sự', 'View', 'Xem danh sách và chi tiết nhân viên'),
('NHANSU', 'Quản lý Nhân sự', 'Add', 'Thêm hồ sơ nhân viên mới'),
('NHANSU', 'Quản lý Nhân sự', 'Edit', 'Sửa thông tin nhân viên'),
('NHANSU', 'Quản lý Nhân sự', 'Delete', 'Xóa hồ sơ nhân viên'),
('NHANSU', 'Quản lý Nhân sự', 'Export', 'Xuất dữ liệu nhân sự'),
-- Module Tuyển dụng
('TUYEN_DUNG', 'Tuyển dụng', 'View', 'Xem tin tuyển dụng và hồ sơ ứng viên'),
('TUYEN_DUNG', 'Tuyển dụng', 'Add', 'Tạo tin tuyển dụng mới'),
('TUYEN_DUNG', 'Tuyển dụng', 'Edit', 'Sửa tin tuyển dụng'),
('TUYEN_DUNG', 'Tuyển dụng', 'Delete', 'Xóa tin tuyển dụng'),
('TUYEN_DUNG', 'Tuyển dụng', 'Approve', 'Duyệt hồ sơ ứng viên'),
-- Module Chấm công
('CHAM_CONG', 'Chấm công', 'View', 'Xem bảng chấm công'),
('CHAM_CONG', 'Chấm công', 'Add', 'Nhập dữ liệu chấm công'),
('CHAM_CONG', 'Chấm công', 'Edit', 'Sửa dữ liệu chấm công'),
('CHAM_CONG', 'Chấm công', 'Approve', 'Duyệt bảng chấm công'),
('CHAM_CONG', 'Chấm công', 'Export', 'Xuất dữ liệu chấm công'),
-- Module Lương
('LUONG', 'Bảng lương', 'View', 'Xem bảng lương'),
('LUONG', 'Bảng lương', 'Add', 'Tính lương tháng'),
('LUONG', 'Bảng lương', 'Edit', 'Sửa bảng lương'),
('LUONG', 'Bảng lương', 'Approve', 'Duyệt bảng lương'),
('LUONG', 'Bảng lương', 'Export', 'Xuất bảng lương'),
-- Module Đào tạo
('DAO_TAO', 'Đào tạo', 'View', 'Xem thông tin đào tạo'),
('DAO_TAO', 'Đào tạo', 'Add', 'Thêm khóa đào tạo'),
('DAO_TAO', 'Đào tạo', 'Edit', 'Sửa khóa đào tạo'),
-- Module KTKL
('KTKL', 'Khen thưởng - Kỷ luật', 'View', 'Xem khen thưởng kỷ luật'),
('KTKL', 'Khen thưởng - Kỷ luật', 'Add', 'Thêm KTKL'),
('KTKL', 'Khen thưởng - Kỷ luật', 'Approve', 'Duyệt KTKL'),
-- Module Báo cáo
('BAO_CAO', 'Báo cáo', 'View', 'Xem báo cáo'),
('BAO_CAO', 'Báo cáo', 'Export', 'Xuất báo cáo'),
-- Module Hệ thống
('HE_THONG', 'Quản trị hệ thống', 'View', 'Xem cấu hình hệ thống'),
('HE_THONG', 'Quản trị hệ thống', 'Edit', 'Sửa cấu hình hệ thống');

-- =====================================================================
-- 7.21 Dữ liệu role_permissions (Phân quyền)
-- =====================================================================
-- Admin: Toàn quyền (tất cả permissions)
INSERT INTO `role_permissions` (`RoleID`, `PermissionID`)
SELECT 1, `PermissionID` FROM `permissions`;

-- HR_Manager: Nhân sự, Tuyển dụng, Chấm công, Lương, Đào tạo, KTKL, Báo cáo
INSERT INTO `role_permissions` (`RoleID`, `PermissionID`)
SELECT 2, `PermissionID` FROM `permissions` WHERE `ModuleCode` IN ('NHANSU', 'TUYEN_DUNG', 'CHAM_CONG', 'LUONG', 'DAO_TAO', 'KTKL', 'BAO_CAO');

-- Manager: Xem nhân sự, Chấm công (View, Approve), Xem lương, Xem Đào tạo, Xem KTKL
INSERT INTO `role_permissions` (`RoleID`, `PermissionID`)
SELECT 3, `PermissionID` FROM `permissions` WHERE 
    (`ModuleCode` = 'NHANSU' AND `ActionCode` = 'View') OR
    (`ModuleCode` = 'CHAM_CONG' AND `ActionCode` IN ('View', 'Approve')) OR
    (`ModuleCode` = 'LUONG' AND `ActionCode` = 'View') OR
    (`ModuleCode` = 'DAO_TAO' AND `ActionCode` = 'View') OR
    (`ModuleCode` = 'KTKL' AND `ActionCode` = 'View') OR
    (`ModuleCode` = 'BAO_CAO' AND `ActionCode` = 'View');

-- Employee: Xem nhân sự (bản thân), Xem chấm công (bản thân), Xem lương (bản thân)
INSERT INTO `role_permissions` (`RoleID`, `PermissionID`)
SELECT 4, `PermissionID` FROM `permissions` WHERE 
    (`ModuleCode` = 'NHANSU' AND `ActionCode` = 'View') OR
    (`ModuleCode` = 'CHAM_CONG' AND `ActionCode` = 'View') OR
    (`ModuleCode` = 'LUONG' AND `ActionCode` = 'View');

-- =====================================================================
-- 7.22 Dữ liệu menu_items
-- =====================================================================
INSERT INTO `menu_items` (`MenuID`, `Title`, `Route`, `Icon`, `ParentID`, `OrderIndex`, `RequiredPermission`, `IsVisible`) VALUES
(1,  'Dashboard',           '#/dashboard',    '📊', NULL, 1,  NULL,         1),
(2,  'Sơ đồ Tổ chức',      '#/orgchart',     '🏢', NULL, 2,  NULL,         1),
(3,  'Quản lý Nhân sự',    NULL,              '👥', NULL, 3,  'NHANSU',     1),
(4,  'Hồ sơ Nhân viên',    '#/employees',     '👤', 3,    1,  'NHANSU',     1),
(5,  'Tuyển dụng',          '#/recruitment',   '🎯', 3,    2,  'TUYEN_DUNG', 1),
(6,  'Hợp đồng',           '#/contracts',     '📋', 3,    3,  'NHANSU',     1),
(7,  'Đào tạo',            '#/training',      '🎓', 3,    4,  'DAO_TAO',    1),
(8,  'Danh mục',           '#/categories',    '📂', 3,    5,  'NHANSU',     1),
(9,  'Chấm công & Lương',  NULL,              '💰', NULL, 4,  'CHAM_CONG',  1),
(10, 'Chấm công',          '#/attendance',    '⏰', 9,    1,  'CHAM_CONG',  1),
(11, 'Bảng lương 3P',      '#/payroll',       '💰', 9,    2,  'LUONG',      1),
(12, 'Phân tích',          NULL,              '📈', NULL, 5,  'BAO_CAO',    1),
(13, 'Báo cáo & AI',       '#/reports',       '📈', 12,   1,  'BAO_CAO',    1),
(14, 'Hệ thống',           NULL,              '⚙️', NULL, 6,  'HE_THONG',   1),
(15, 'Phân quyền',         '#/roles',         '🔐', 14,   1,  'HE_THONG',   1);


-- =====================================================================
-- HOÀN TẤT
-- =====================================================================
SET FOREIGN_KEY_CHECKS = 1;

-- Thống kê sau khi import
SELECT 'Vinamilk HRM Database - Import thành công!' AS `Thông báo`;
SELECT TABLE_NAME AS `Bảng`, TABLE_ROWS AS `Số bản ghi (ước tính)` 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'vinamilk_hrm' 
ORDER BY TABLE_NAME;
