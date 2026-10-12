<?php
require_once __DIR__ . '/../app/core/Database.php';

$pdo = Database::getInstance()->getConnection();

echo "=== SEEDING MODULE 008 DATA ===\n";

// 1. Cập nhật và bổ sung các chứng chỉ nhân viên
// Lấy một số nhân viên nhà máy và văn phòng
$stmt = $pdo->query("SELECT MaNV, HoTen, MaDV, ChucVu FROM nhansu LIMIT 10");
$emps = $stmt->fetchAll(PDO::FETCH_ASSOC);

if (empty($emps)) {
    die("No employees found to seed!\n");
}

$firstEmp = $emps[0]['MaNV']; // e.g. VNM-0001
$factoryEmp1 = 'VNM-0016';    // Công nhân sản xuất NM Thống Nhất / Bình Dương
$factoryEmp2 = 'VNM-0017';    // Nhân viên vận hành dây chuyền Tetra Pak
$officeEmp1  = 'VNM-0002';    // Trưởng phòng Marketing / Chuyên viên văn phòng

// Đảm bảo VNM-0016, VNM-0017 tồn tại trong DB, nếu không lấy mã có sẵn
$manvs = array_column($emps, 'MaNV');
$empTarget1 = in_array('VNM-0016', $manvs) ? 'VNM-0016' : ($manvs[1] ?? $manvs[0]);
$empTarget2 = in_array('VNM-0017', $manvs) ? 'VNM-0017' : ($manvs[2] ?? $manvs[0]);
$empTarget3 = in_array('VNM-0003', $manvs) ? 'VNM-0003' : ($manvs[3] ?? $manvs[0]);

// Seed chứng chỉ:
// Case 1: Thẻ xanh ATTP ĐÃ HẾT HẠN (để test CHẶN XẾP CA DÂY CHUYỀN)
$pdo->exec("DELETE FROM chung_chi_nhan_vien WHERE ma_nv = '{$empTarget1}' AND so_hieu_chung_chi = 'TX-EXPIRED-TEST'");
$pdo->exec("INSERT INTO chung_chi_nhan_vien 
(ma_nv, loai_chung_chi, la_the_xanh_attp, ten_chung_chi, so_hieu_chung_chi, noi_cap, co_so_y_te, ngay_cap, ngay_kham_dinh_ky, ngay_het_han, trang_thai, canh_bao_truoc_ngay, ket_qua_kham, ghi_chu)
VALUES 
('{$empTarget1}', 'THE_XANH_ATTP', 1, 'Thẻ Xanh Khám Sức Khỏe An Toàn Thực Phẩm Định Kỳ (Quá Hạn)', 'TX-EXPIRED-TEST', 'TT Y Tế Dĩ An', 'TT Y Tế Dĩ An', '2025-01-01', '2025-01-01', DATE_SUB(CURDATE(), INTERVAL 5 DAY), 'DA_HET_HAN', 60, 'Quá hạn khám định kỳ 12 tháng', 'Cần tái khám sức khỏe định kỳ khẩn cấp')");

// Case 2: Thẻ xanh ATTP SẮP HẾT HẠN TRONG 60 NGÀY (còn 25 ngày)
$pdo->exec("DELETE FROM chung_chi_nhan_vien WHERE ma_nv = '{$empTarget2}' AND so_hieu_chung_chi = 'TX-WARN-60D'");
$pdo->exec("INSERT INTO chung_chi_nhan_vien 
(ma_nv, loai_chung_chi, la_the_xanh_attp, ten_chung_chi, so_hieu_chung_chi, noi_cap, co_so_y_te, ngay_cap, ngay_kham_dinh_ky, ngay_het_han, trang_thai, canh_bao_truoc_ngay, ket_qua_kham, ghi_chu)
VALUES 
('{$empTarget2}', 'THE_XANH_ATTP', 1, 'Thẻ Xanh Vệ Sinh An Toàn Thực Phẩm Nhà Máy Sữa', 'TX-WARN-60D', 'Bệnh viện Đa khoa Quốc tế Becamex', 'BVQT Becamex', '2025-11-10', '2025-11-10', DATE_ADD(CURDATE(), INTERVAL 25 DAY), 'SAP_HET_HAN', 60, 'Đạt tiêu chuẩn ATTP theo TT 14/2013/TT-BYT', 'Hạn khám định kỳ sắp tới trong 25 ngày')");

// Case 3: Chứng chỉ ATLĐ & Vận hành thiết bị
$pdo->exec("DELETE FROM chung_chi_nhan_vien WHERE ma_nv = '{$empTarget2}' AND so_hieu_chung_chi = 'CC-TETRA-PAK-01'");
$pdo->exec("INSERT INTO chung_chi_nhan_vien 
(ma_nv, loai_chung_chi, la_the_xanh_attp, ten_chung_chi, so_hieu_chung_chi, noi_cap, ngay_cap, ngay_het_han, trang_thai, canh_bao_truoc_ngay, ghi_chu)
VALUES 
('{$empTarget2}', 'CHUNG_CHI_THIET_BI', 0, 'Chứng Chỉ Vận Hành Dây Chuyền Tiệt Trùng & Chiết Rót Tetra Pak A3', 'CC-TETRA-PAK-01', 'Viện Đào Tạo Kỹ Thuật Tetra Pak Thụy Điển', '2024-06-15', DATE_ADD(CURDATE(), INTERVAL 400 DAY), 'CON_HIEU_LUC', 60, 'Được phép đứng máy chiết rót hộp giấy vô trùng 1L/180ml')");

// Case 4: Chứng chỉ An toàn lao động ATLĐ Nhóm 3
$pdo->exec("DELETE FROM chung_chi_nhan_vien WHERE ma_nv = '{$empTarget2}' AND so_hieu_chung_chi = 'ATLD-NHOM3-VNM'");
$pdo->exec("INSERT INTO chung_chi_nhan_vien 
(ma_nv, loai_chung_chi, la_the_xanh_attp, ten_chung_chi, so_hieu_chung_chi, noi_cap, ngay_cap, ngay_het_han, trang_thai, canh_bao_truoc_ngay, ghi_chu)
VALUES 
('{$empTarget2}', 'ATLD_VE_SINH_LD', 0, 'Giấy Chứng Nhận Huấn Luyện An Toàn Vệ Sinh Lao Động Nhóm 3', 'ATLD-NHOM3-VNM', 'Cục An Toàn Lao Động - Bộ LĐTBXH', '2025-03-20', DATE_ADD(CURDATE(), INTERVAL 45 DAY), 'SAP_HET_HAN', 60, 'Huấn luyện ATLĐ vận hành thiết bị có yêu cầu nghiêm ngặt')");

echo "-> Seeded Certificates (ATTP expired, ATTP warning 60 days, Tetra Pak operating cert, ATLĐ group 3) successfully.\n";

// 2. Seed cấp phát tài sản & PPE có ký nhận và tài sản mất bồi thường
// Cấp phát PPE nhà máy cho empTarget2 (Đã ký nhận)
$pdo->exec("DELETE FROM cap_phat_tai_san WHERE ma_nv = '{$empTarget2}' AND ma_dinh_danh_serial LIKE 'PPE-SEED-%'");
$pdo->exec("INSERT INTO cap_phat_tai_san 
(ma_nv, loai_tai_san, ten_tai_san, ma_dinh_danh_serial, so_luong, don_vi_tinh, gia_tri_tai_san, gia_tri_khau_tru, ngay_cap_phat, tinh_trang_khi_cap, nguoi_giao, nguoi_nhan_ky, da_ky_nhan_cap, ngay_ky_nhan_cap, ma_bien_ban, trang_thai, ghi_chu)
VALUES 
('{$empTarget2}', 'BHLD_PPE', 'Ủng bảo hộ chống trượt & chống hóa chất CIP', 'PPE-SEED-UNG-01', 1, 'Đôi', 450000, 0, CURDATE(), 'Mới 100% nguyên hộp', 'Cán bộ Kho HSE', '{$empTarget2}', 1, NOW(), 'BB-CP-2026-NM01', 'DANG_SU_DUNG', 'Cấp phát định kỳ 6 tháng'),
('{$empTarget2}', 'BHLD_PPE', 'Áo blouse trắng phòng sạch tiệt trùng', 'PPE-SEED-BLOUSE-01', 2, 'Bộ', 350000, 0, CURDATE(), 'Mới 100% tiêu chuẩn FSSC', 'Cán bộ Kho HSE', '{$empTarget2}', 1, NOW(), 'BB-CP-2026-NM01', 'DANG_SU_DUNG', 'Đồng phục phòng vô trùng'),
('{$empTarget2}', 'BHLD_PPE', 'Mũ trùm đầu & tóc tiệt trùng', 'PPE-SEED-MU-01', 3, 'Cái', 80000, 0, CURDATE(), 'Mới 100%', 'Cán bộ Kho HSE', '{$empTarget2}', 1, NOW(), 'BB-CP-2026-NM01', 'DANG_SU_DUNG', 'Mũ trùm ngăn tóc rơi'),
('{$empTarget2}', 'BHLD_PPE', 'Găng tay cao su nitrile an toàn thực phẩm', 'PPE-SEED-GANG-01', 5, 'Hộp', 120000, 0, CURDATE(), 'Mới nguyên hộp', 'Cán bộ Kho HSE', '{$empTarget2}', 1, NOW(), 'BB-CP-2026-NM01', 'DANG_SU_DUNG', 'Sử dụng ca chiết rót')");

// Cấp phát tài sản văn phòng cho empTarget3: Laptop + Thẻ từ (Trong đó Laptop báo mất cần khấu trừ)
$pdo->exec("DELETE FROM cap_phat_tai_san WHERE ma_nv = '{$empTarget3}' AND ma_dinh_danh_serial LIKE 'TS-OFFICE-%'");
$pdo->exec("INSERT INTO cap_phat_tai_san 
(ma_nv, loai_tai_san, ten_tai_san, ma_dinh_danh_serial, so_luong, don_vi_tinh, gia_tri_tai_san, gia_tri_khau_tru, ngay_cap_phat, tinh_trang_khi_cap, nguoi_giao, nguoi_nhan_ky, da_ky_nhan_cap, ngay_ky_nhan_cap, ma_bien_ban, trang_thai, ghi_chu)
VALUES 
('{$empTarget3}', 'MAY_TINH_LAPTOP', 'Laptop Dell Latitude 7440 Core i7', 'TS-OFFICE-LT-01', 1, 'Chiếc', 18500000, 18500000, '2024-03-01', 'Mới 100%', 'IT Support', '{$empTarget3}', 1, '2024-03-01 09:00:00', 'BB-CP-2024-088', 'BAO_MAT', 'Nhân viên làm mất ngoài công ty, chưa hoàn trả khi thôi việc'),
('{$empTarget3}', 'THE_TU_RA_VAO', 'Thẻ từ thông minh RFID Vinamilk Tower', 'TS-OFFICE-RFID-01', 1, 'Thẻ', 150000, 150000, '2024-03-01', 'Hoạt động tốt', 'Lễ tân Tòa nhà', '{$empTarget3}', 1, '2024-03-01 09:00:00', 'BB-CP-2024-088', 'BAO_MAT', 'Mất cùng bóp/ví cá nhân, trừ vào lương quyết toán')");

echo "-> Seeded Assets & PPE (Factory & Office, signed records, unreturned penalty) successfully.\n";

// 3. Seed Offboarding & Exit Survey cho empTarget3
$pdo->exec("DELETE FROM exit_interview_survey WHERE ma_nv = '{$empTarget3}'");
$pdo->exec("DELETE FROM offboarding_process WHERE ma_nv = '{$empTarget3}'");

$pdo->exec("INSERT INTO offboarding_process 
(ma_nv, ma_ho_so, ngay_de_xuat, ngay_lam_viec_cuoi, ly_do_nghi, trang_thai, 
 buoc_1_ban_giao_cong_viec, nguoi_xac_nhan_bg, ngay_xac_nhan_bg, noi_dung_ban_giao,
 buoc_2_thu_hoi_tai_san, nguoi_xac_nhan_ts, ngay_xac_nhan_ts, so_tai_san_da_tra, so_tai_san_chua_tra, tong_tien_khau_tru_ts, ghi_chu_tai_san,
 buoc_3_khoa_tai_khoan, nguoi_xac_nhan_tk, ngay_xac_nhan_tk, username_khoa, ghi_chu_khoa_tk,
 buoc_4_chot_cong, nguoi_xac_nhan_cong, ngay_xac_nhan_cong, so_cong_chot, so_phep_con_lai, ghi_chu_chot_cong,
 buoc_5_quyet_toan_luong, nguoi_xac_nhan_luong, ngay_xac_nhan_luong, thuc_linh_quyet_toan, khau_tru_tai_san_da_tru, ghi_chu_luong,
 buoc_6_chot_bhxh, nguoi_xac_nhan_bhxh, ngay_xac_nhan_bhxh, so_so_bhxh, ngay_tra_so_bhxh, ghi_chu_bhxh,
 nguoi_lap_ho_so)
VALUES 
('{$empTarget3}', 'OFF-2026-0001', DATE_SUB(CURDATE(), INTERVAL 10 DAY), CURDATE(), 'Chuyển công tác sang công ty FDI đa quốc gia', 'DANG_XU_LY',
 1, 'Trần Quản Lý', DATE_SUB(NOW(), INTERVAL 5 DAY), 'Đã bàn giao đầy đủ tài liệu dự án, hợp đồng khách hàng và mã nguồn',
 1, 'Nguyễn Quản Trị TS', DATE_SUB(NOW(), INTERVAL 3 DAY), 0, 2, 18650000, 'Chưa hoàn trả Laptop Dell (18.5tr) và Thẻ RFID (150k), tự động chuyển sang khấu trừ lương quyết toán',
 1, 'IT Admin', DATE_SUB(NOW(), INTERVAL 2 DAY), 'user_{$empTarget3}', 'Đã vô hiệu hóa tài khoản AD và email doanh nghiệp',
 1, 'Cán Bộ Chấm Công', DATE_SUB(NOW(), INTERVAL 1 DAY), 22.0, 3.5, 'Chốt 22 ngày công thực tế, thanh toán thêm 3.5 ngày phép chưa nghỉ',
 0, NULL, NULL, 0, 18650000, 'Đang chờ Kế toán Lương duyệt quyết toán và trừ 18.650.000đ tiền tài sản',
 0, NULL, NULL, '7912345678', NULL, 'Đang làm thủ tục chốt sổ tại BHXH Quận 1',
 'HR Admin')");

$offId = $pdo->lastInsertId();

$pdo->exec("INSERT INTO exit_interview_survey 
(offboarding_id, ma_nv, ngay_khao_sat, hai_long_luong_thuong, hai_long_quan_ly, hai_long_co_hoi_thang_tien, ap_luc_cong_viec_ot, hai_long_moi_truong_dong_nghiep, hai_long_chinh_sach_phuc_loi, ly_do_chinh, cong_ty_moi_loai_hinh, muc_luong_moi_tang_phan_tram, dieu_gi_vnm_nen_cai_thien, san_sang_quay_lai_vnm)
VALUES 
({$offId}, '{$empTarget3}', CURDATE(), 2, 4, 2, 4, 5, 4, 'CO_HOI_VIEC_LAM_TOT_HON', 'Tập đoàn FDI Dinh dưỡng Đa quốc gia', 35.00, 'Cần cải thiện chế độ xét duyệt tăng lương linh hoạt hơn và giảm thiểu thủ tục phê duyệt giấy tờ hành chính.', 1)");

echo "-> Seeded Offboarding Checklist & Exit Survey successfully.\n";
echo "=== SEEDING COMPLETED ===\n";
