-- =====================================================================
-- MIGRATION 008: CHỨNG CHỈ (ATTP/ATLĐ/THIẾT BỊ) - TÀI SẢN & PPE - OFFBOARDING CHECKLIST
-- =====================================================================

-- 1. BẢNG DANH MỤC TIÊU CHUẨN TRANG BỊ PPE & TÀI SẢN THEO VỊ TRÍ
CREATE TABLE IF NOT EXISTS `dm_tieuchuan_ppe` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `nhom_vi_tri` ENUM('NHA_MAY_SAN_XUAT', 'VAN_PHONG_DIEU_HANH', 'TRANG_TRAI_BO_SUA', 'KHO_LOGISTICS') NOT NULL,
  `ten_trang_bi` VARCHAR(200) NOT NULL,
  `loai_trang_bi` ENUM('BHLD_PPE', 'MAY_TINH_LAPTOP', 'DIEN_THOAI_SIM', 'THE_TU_RA_VAO', 'THIET_BI_CHUYEN_DUNG', 'KHAC') NOT NULL,
  `quy_cach_tieu_chuan` VARCHAR(255) NULL,
  `dinh_muc_so_luong` INT NOT NULL DEFAULT 1,
  `don_vi_tinh` VARCHAR(30) NOT NULL DEFAULT 'Bộ',
  `chu_ky_thay_the_thang` INT NOT NULL DEFAULT 12,
  `gia_tri_dinh_muc` DECIMAL(15,2) NOT NULL DEFAULT 0.00,
  `bat_buoc` TINYINT(1) NOT NULL DEFAULT 1,
  `ghi_chu` TEXT NULL,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  INDEX `idx_nhom_vi_tri` (`nhom_vi_tri`),
  INDEX `idx_loai_trang_bi` (`loai_trang_bi`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. BỔ SUNG CỘT CHO BẢNG cap_phat_tai_san
ALTER TABLE `cap_phat_tai_san` 
  ADD COLUMN IF NOT EXISTS `gia_tri_tai_san` DECIMAL(15,2) NOT NULL DEFAULT 0.00 AFTER `so_luong`,
  ADD COLUMN IF NOT EXISTS `gia_tri_khau_tru` DECIMAL(15,2) NOT NULL DEFAULT 0.00 AFTER `gia_tri_tai_san`,
  ADD COLUMN IF NOT EXISTS `da_ky_nhan_cap` TINYINT(1) NOT NULL DEFAULT 0 AFTER `nguoi_nhan_ky`,
  ADD COLUMN IF NOT EXISTS `ngay_ky_nhan_cap` DATETIME NULL AFTER `da_ky_nhan_cap`,
  ADD COLUMN IF NOT EXISTS `da_ky_nhan_thu_hoi` TINYINT(1) NOT NULL DEFAULT 0 AFTER `ngay_ky_nhan_cap`,
  ADD COLUMN IF NOT EXISTS `ngay_ky_nhan_thu_hoi` DATETIME NULL AFTER `da_ky_nhan_thu_hoi`,
  ADD COLUMN IF NOT EXISTS `ma_bien_ban` VARCHAR(50) NULL AFTER `ngay_ky_nhan_thu_hoi`,
  ADD COLUMN IF NOT EXISTS `chu_ky_hash` VARCHAR(255) NULL AFTER `ma_bien_ban`;

-- 3. BỔ SUNG CỘT CHO BẢNG chung_chi_nhan_vien
ALTER TABLE `chung_chi_nhan_vien`
  MODIFY COLUMN `loai_chung_chi` VARCHAR(50) NOT NULL,
  ADD COLUMN IF NOT EXISTS `la_the_xanh_attp` TINYINT(1) NOT NULL DEFAULT 0 AFTER `loai_chung_chi`,
  ADD COLUMN IF NOT EXISTS `ngay_kham_dinh_ky` DATE NULL AFTER `ngay_cap`,
  ADD COLUMN IF NOT EXISTS `co_so_y_te` VARCHAR(150) NULL AFTER `noi_cap`,
  ADD COLUMN IF NOT EXISTS `ket_qua_kham` VARCHAR(100) DEFAULT 'Đạt tiêu chuẩn ATTP' AFTER `co_so_y_te`;

-- 4. BẢNG QUY TRÌNH THÔI VIỆC OFFBOARDING 6 BƯỚC
CREATE TABLE IF NOT EXISTS `offboarding_process` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `ma_nv` VARCHAR(20) NOT NULL,
  `ma_ho_so` VARCHAR(50) UNIQUE NOT NULL,
  `ngay_de_xuat` DATE NOT NULL,
  `ngay_lam_viec_cuoi` DATE NOT NULL,
  `ly_do_nghi` VARCHAR(255) NOT NULL,
  `trang_thai` ENUM('KHOI_TAO', 'DANG_XU_LY', 'HOAN_TAT', 'DA_HUY') NOT NULL DEFAULT 'DANG_XU_LY',
  
  -- Bước 1: Bàn giao công việc
  `buoc_1_ban_giao_cong_viec` TINYINT(1) NOT NULL DEFAULT 0,
  `nguoi_xac_nhan_bg` VARCHAR(50) NULL,
  `ngay_xac_nhan_bg` DATETIME NULL,
  `noi_dung_ban_giao` TEXT NULL,

  -- Bước 2: Thu hồi tài sản
  `buoc_2_thu_hoi_tai_san` TINYINT(1) NOT NULL DEFAULT 0,
  `nguoi_xac_nhan_ts` VARCHAR(50) NULL,
  `ngay_xac_nhan_ts` DATETIME NULL,
  `so_tai_san_da_tra` INT NOT NULL DEFAULT 0,
  `so_tai_san_chua_tra` INT NOT NULL DEFAULT 0,
  `tong_tien_khau_tru_ts` DECIMAL(15,2) NOT NULL DEFAULT 0.00,
  `ghi_chu_tai_san` TEXT NULL,

  -- Bước 3: Khóa tài khoản
  `buoc_3_khoa_tai_khoan` TINYINT(1) NOT NULL DEFAULT 0,
  `nguoi_xac_nhan_tk` VARCHAR(50) NULL,
  `ngay_xac_nhan_tk` DATETIME NULL,
  `username_khoa` VARCHAR(50) NULL,
  `ghi_chu_khoa_tk` TEXT NULL,

  -- Bước 4: Chốt công
  `buoc_4_chot_cong` TINYINT(1) NOT NULL DEFAULT 0,
  `nguoi_xac_nhan_cong` VARCHAR(50) NULL,
  `ngay_xac_nhan_cong` DATETIME NULL,
  `so_cong_chot` DECIMAL(5,2) NOT NULL DEFAULT 0.00,
  `so_phep_con_lai` DECIMAL(5,2) NOT NULL DEFAULT 0.00,
  `ghi_chu_chot_cong` TEXT NULL,

  -- Bước 5: Quyết toán lương
  `buoc_5_quyet_toan_luong` TINYINT(1) NOT NULL DEFAULT 0,
  `nguoi_xac_nhan_luong` VARCHAR(50) NULL,
  `ngay_xac_nhan_luong` DATETIME NULL,
  `thuc_linh_quyet_toan` DECIMAL(15,2) NOT NULL DEFAULT 0.00,
  `khau_tru_tai_san_da_tru` DECIMAL(15,2) NOT NULL DEFAULT 0.00,
  `ghi_chu_luong` TEXT NULL,

  -- Bước 6: Chốt sổ BHXH
  `buoc_6_chot_bhxh` TINYINT(1) NOT NULL DEFAULT 0,
  `nguoi_xac_nhan_bhxh` VARCHAR(50) NULL,
  `ngay_xac_nhan_bhxh` DATETIME NULL,
  `so_so_bhxh` VARCHAR(50) NULL,
  `ngay_tra_so_bhxh` DATE NULL,
  `ghi_chu_bhxh` TEXT NULL,

  `ngay_hoan_tat` DATETIME NULL,
  `nguoi_lap_ho_so` VARCHAR(50) NULL,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  INDEX `idx_ma_nv` (`ma_nv`),
  INDEX `idx_trang_thai` (`trang_thai`),
  INDEX `idx_ngay_lam_viec_cuoi` (`ngay_lam_viec_cuoi`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. BẢNG KHẢO SÁT LÝ DO THÔI VIỆC (EXIT SURVEY - DỮ LIỆU ĐÀO TẠO AI)
CREATE TABLE IF NOT EXISTS `exit_interview_survey` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `offboarding_id` INT NULL,
  `ma_nv` VARCHAR(20) NOT NULL,
  `ngay_khao_sat` DATE NOT NULL,
  `hai_long_luong_thuong` TINYINT NOT NULL DEFAULT 3,
  `hai_long_quan_ly` TINYINT NOT NULL DEFAULT 3,
  `hai_long_co_hoi_thang_tien` TINYINT NOT NULL DEFAULT 3,
  `ap_luc_cong_viec_ot` TINYINT NOT NULL DEFAULT 3,
  `hai_long_moi_truong_dong_nghiep` TINYINT NOT NULL DEFAULT 3,
  `hai_long_chinh_sach_phuc_loi` TINYINT NOT NULL DEFAULT 3,
  `ly_do_chinh` ENUM('LUONG_THAP', 'QUAN_LY_TRUC_TIEP', 'KHONG_CO_CO_HOI_PHAT_TRIEN', 'AP_LUC_OT_CAO', 'MOI_TRUONG_LAM_VIEC', 'LY_DO_CA_NHAN_GIA_DINH', 'CO_HOI_VIEC_LAM_TOT_HON', 'CHUYEN_CHO_O', 'KHAC') NOT NULL,
  `cong_ty_moi_loai_hinh` VARCHAR(150) NULL,
  `muc_luong_moi_tang_phan_tram` DECIMAL(5,2) NULL,
  `dieu_gi_vnm_nen_cai_thien` TEXT NULL,
  `san_sang_quay_lai_vnm` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  INDEX `idx_ma_nv_survey` (`ma_nv`),
  INDEX `idx_ly_do_chinh` (`ly_do_chinh`),
  INDEX `idx_ngay_khao_sat` (`ngay_khao_sat`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. SEED DỮ LIỆU DANH MỤC TIÊU CHUẨN PPE
INSERT INTO `dm_tieuchuan_ppe` 
(`nhom_vi_tri`, `ten_trang_bi`, `loai_trang_bi`, `quy_cach_tieu_chuan`, `dinh_muc_so_luong`, `don_vi_tinh`, `chu_ky_thay_the_thang`, `gia_tri_dinh_muc`, `bat_buoc`, `ghi_chu`)
VALUES
-- Nhà máy sản xuất (Ủng, Áo, Mũ, Găng)
('NHA_MAY_SAN_XUAT', 'Ủng bảo hộ chống trượt & chống hóa chất CIP', 'BHLD_PPE', 'Ủng cao su trắng cổ cao chuyên dụng chế biến sữa, chống dầu mỡ EN ISO 20345', 1, 'Đôi', 6, 450000.00, 1, 'Bắt buộc khi vào khu vực chế biến & chiết rót'),
('NHA_MAY_SAN_XUAT', 'Áo blouse trắng phòng sạch tiệt trùng', 'BHLD_PPE', 'Vải cotton chống tĩnh điện, cài nút bấm kín cổ, chuẩn HACCP/FSSC 22000', 2, 'Bộ', 6, 350000.00, 1, 'Đồng phục phòng sạch tiệt trùng'),
('NHA_MAY_SAN_XUAT', 'Mũ trùm đầu & tóc tiệt trùng', 'BHLD_PPE', 'Mũ trùm kín tóc có lưới che gáy ngăn rơi dị vật vào sản phẩm', 3, 'Cái', 3, 80000.00, 1, 'Ngăn ngừa nguy cơ tóc rơi vào bồn sữa'),
('NHA_MAY_SAN_XUAT', 'Găng tay cao su nitrile an toàn thực phẩm', 'BHLD_PPE', 'Chuẩn FDA/ATTP, màu xanh dương nhận diện dị vật, chống thủng rách', 5, 'Hộp', 1, 120000.00, 1, 'Sử dụng liên tục trong ca vận hành chiết rót'),
('NHA_MAY_SAN_XUAT', 'Kính bảo hộ chống văng bắn hóa chất súc rửa', 'BHLD_PPE', 'Polycarbonate chống trầy, chống đọng sương, chuẩn ANSI Z87.1', 1, 'Cái', 12, 150000.00, 1, 'Trang bị cho công đoạn vệ sinh đường ống CIP'),

-- Văn phòng điều hành (Laptop, Thẻ từ, Thiết bị)
('VAN_PHONG_DIEU_HANH', 'Laptop Dell Latitude 7440 / ThinkPad T14s', 'MAY_TINH_LAPTOP', 'Core i7 Gen 13, 16GB RAM, 512GB SSD, Windows 11 Pro bản quyền Doanh nghiệp', 1, 'Chiếc', 36, 18500000.00, 1, 'Cấp phát cho khối văn phòng, cán bộ chuyên môn & quản lý'),
('VAN_PHONG_DIEU_HANH', 'Thẻ từ thông minh RFID Vinamilk Tower', 'THE_TU_RA_VAO', 'Thẻ chip RFID Mifare Desfire tích hợp kiểm soát cửa, thang máy & căn tin', 1, 'Thẻ', 36, 150000.00, 1, 'Thẻ định danh nhân viên tòa nhà hội sở & nhà máy'),
('VAN_PHONG_DIEU_HANH', 'Màn hình ngoài 24 inch Full HD IPS Dell', 'MAY_TINH_LAPTOP', 'Màn hình văn phòng chuyên dụng, cổng HDMI/Type-C chống mỏi mắt', 1, 'Chiếc', 36, 3200000.00, 0, 'Cấp cho nhân viên làm việc thiết kế, kế toán, dữ liệu'),
('VAN_PHONG_DIEU_HANH', 'Điện thoại di động & SIM nội bộ kết nối', 'DIEN_THOAI_SIM', 'Smartphone công vụ kèm gói cước data thoại nội bộ Vinamilk', 1, 'Bộ', 24, 3800000.00, 0, 'Dành cho quản lý cấp Phòng và Giám sát bán hàng'),

-- Trang trại bò sữa
('TRANG_TRAI_BO_SUA', 'Ủng bảo hộ chống thấm chuồng trại chuyên dụng', 'BHLD_PPE', 'Ủng đúc liền chống nước tiểu gia súc, rãnh chống trơn sâu', 1, 'Đôi', 6, 380000.00, 1, 'Bắt buộc tại chuồng nuôi & hố ủ chua'),
('TRANG_TRAI_BO_SUA', 'Bộ quần áo chống bám bẩn trang trại', 'BHLD_PPE', 'Vải Kaki dày thoáng khí, may phản quang an toàn', 2, 'Bộ', 6, 360000.00, 1, 'Trang bị lao động trang trại chuẩn GlobalGAP'),
('TRANG_TRAI_BO_SUA', 'Găng tay thú y & vắt sữa vô trùng', 'BHLD_PPE', 'Găng tay dài cánh tay chuyên dụng khám thú y và chuẩn bị vắt sữa', 2, 'Hộp', 1, 180000.00, 1, 'Vắt sữa đàn bò công nghệ cao');

-- 7. CẬP NHẬT DỮ LIỆU CHỨNG CHỈ CÓ SẴN
UPDATE `chung_chi_nhan_vien` 
SET `la_the_xanh_attp` = 1, `canh_bao_truoc_ngay` = 60, `ngay_kham_dinh_ky` = `ngay_cap`, `co_so_y_te` = `noi_cap`
WHERE `loai_chung_chi` = 'THE_XANH_ATTP';
