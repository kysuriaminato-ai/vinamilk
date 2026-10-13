<?php
/**
 * =====================================================================
 * VINAMILK HRM - THỰC THI MIGRATION 003 & TẠO THƯ MỤC UPLOAD NGOÀI PUBLIC
 * =====================================================================
 */

require_once __DIR__ . '/../app/config/config.php';
require_once __DIR__ . '/../app/core/Database.php';

$db = Database::getInstance();

echo "1. Chạy migration 003_employee_module.sql..." . PHP_EOL;
$sql = file_get_contents(__DIR__ . '/../database/migrations/003_employee_module.sql');
$pdo = $db->getConnection();
$pdo->exec($sql);
echo "   -> Đã tạo bảng tai_lieu_nhan_su & khen_thuong_ky_luat thành công!" . PHP_EOL;

// 2. Tạo thư mục storage/uploads/documents ngoài public/
echo "2. Khởi tạo thư mục bảo mật ngoài public/..." . PHP_EOL;
$storageDir = ROOT_DIR . DIRECTORY_SEPARATOR . 'storage' . DIRECTORY_SEPARATOR . 'uploads' . DIRECTORY_SEPARATOR . 'documents';
if (!is_dir($storageDir)) {
    mkdir($storageDir, 0755, true);
}

// Tạo file .htaccess để ngăn truy cập HTTP trực tiếp
$htaccessFile = ROOT_DIR . DIRECTORY_SEPARATOR . 'storage' . DIRECTORY_SEPARATOR . '.htaccess';
if (!file_exists($htaccessFile)) {
    file_put_contents($htaccessFile, "Order Allow,Deny\nDeny from all\n");
}
echo "   -> Thư mục lưu trữ: " . $storageDir . PHP_EOL;

// 3. Seed dữ liệu mẫu cho khen thưởng, kỷ luật, hợp đồng, chứng chỉ, tài sản
echo "3. Bổ sung dữ liệu mẫu cho các tab hồ sơ..." . PHP_EOL;

// Lấy danh sách nhân viên tiêu biểu
$sampleEmployees = $db->query("SELECT MaNV, HoTen, MaDV FROM nhansu LIMIT 10")->fetchAll(PDO::FETCH_ASSOC);

if (!empty($sampleEmployees)) {
    $firstEmp = $sampleEmployees[0]['MaNV']; // VNM-0001
    
    // Seed khen thưởng kỷ luật nếu chưa có
    $checkKT = $db->query("SELECT COUNT(*) FROM khen_thuong_ky_luat WHERE ma_nv = '{$firstEmp}'")->fetchColumn();
    if ($checkKT == 0) {
        $pdo->exec("INSERT INTO khen_thuong_ky_luat (ma_nv, loai, hinh_thuc, so_quyet_dinh, ngay_quyet_dinh, ngay_hieu_luc, co_quan_ban_hanh, nguoi_ky, ly_do, so_tien) VALUES
        ('{$firstEmp}', 'KHEN_THUONG', 'Bằng khen cấp Tổng Công ty', '120/QĐ-VNM-2025', '2025-12-20', '2025-12-20', 'Công ty CP Sữa Việt Nam', 'Mai Kiều Liên', 'Đạt danh hiệu Chiến sĩ thi đua cấp cơ sở xuất sắc năm 2025', 10000000),
        ('{$firstEmp}', 'KHEN_THUONG', 'Giấy khen Giám đốc Khối', '45/QĐ-SX-2024', '2024-06-15', '2024-06-15', 'Khối Sản xuất', 'Nguyễn Quốc Khánh', 'Sáng kiến cải tiến quy trình đóng gói tiệt trùng UHT', 3000000)");
        echo "   -> Đã seed khen thưởng cho {$firstEmp}" . PHP_EOL;
    }

    // Seed hợp đồng lao động nếu chưa có
    $checkHD = $db->query("SELECT COUNT(*) FROM hop_dong_lao_dong WHERE ma_nv = '{$firstEmp}'")->fetchColumn();
    if ($checkHD == 0) {
        $pdo->exec("INSERT INTO hop_dong_lao_dong (ma_nv, so_hd, loai_hd, ngay_ky, tu_ngay, den_ngay, chuc_danh, dia_diem_lam_viec, luong_can_ban, luong_dong_bh, phu_cap_trach_nhiem, phu_cap_an_ca, ty_le_huong_luong, nguoi_dai_dien_ky, chuc_vu_nguoi_ky, trang_thai) VALUES
        ('{$firstEmp}', '001/HĐLĐ-VNM-2023', 'THU_VIEC', '2023-01-01', '2023-01-01', '2023-02-28', 'Kỹ sư công nghệ thực phẩm', 'Trụ sở chính Vinamilk', 15000000, 15000000, 1000000, 730000, 85.00, 'Bùi Thị Hương', 'Giám đốc Nhân sự', 'HET_HAN'),
        ('{$firstEmp}', '085/HĐLĐ-VNM-2023', 'XAC_DINH_THOI_HAN_12T', '2023-03-01', '2023-03-01', '2024-02-29', 'Kỹ sư công nghệ thực phẩm', 'Trụ sở chính Vinamilk', 18000000, 18000000, 2000000, 730000, 100.00, 'Bùi Thị Hương', 'Giám đốc Nhân sự', 'HET_HAN'),
        ('{$firstEmp}', '120/HĐLĐ-VNM-2024', 'XAC_DINH_THOI_HAN_24T', '2024-03-01', '2024-03-01', '2026-02-28', 'Kỹ sư trưởng', 'Trụ sở chính Vinamilk', 25000000, 25000000, 3000000, 730000, 100.00, 'Bùi Thị Hương', 'Giám đốc Nhân sự', 'HIEU_LUC')");
        echo "   -> Đã seed 3 hợp đồng lao động mẫu cho {$firstEmp}" . PHP_EOL;
    }

    // Seed chứng chỉ cho nhân viên
    $checkCC = $db->query("SELECT COUNT(*) FROM chung_chi_nhan_vien WHERE ma_nv = '{$firstEmp}'")->fetchColumn();
    if ($checkCC == 0) {
        $pdo->exec("INSERT INTO chung_chi_nhan_vien (ma_nv, loai_chung_chi, ten_chung_chi, so_hieu_chung_chi, noi_cap, ngay_cap, ngay_het_han, trang_thai, canh_bao_truoc_ngay) VALUES
        ('{$firstEmp}', 'THE_XANH_ATTP', 'Thẻ xanh An toàn Vệ sinh Thực phẩm', 'ATTP-HCM-2024/991', 'Sở Y tế TP.HCM', '2024-05-10', '2026-05-10', 'CON_HIEU_LUC', 30),
        ('{$firstEmp}', 'ATLD_VE_SINH_LD', 'Chứng nhận An toàn Lao động Nhóm 2', 'ATLĐ-VNM-2025/12', 'Trung tâm ATLĐ Vinamilk', '2025-01-15', '2027-01-15', 'CON_HIEU_LUC', 30)");
        echo "   -> Đã seed chứng chỉ ATTP & ATLĐ cho {$firstEmp}" . PHP_EOL;
    }

    // Seed tài sản cấp phát
    $checkTS = $db->query("SELECT COUNT(*) FROM cap_phat_tai_san WHERE ma_nv = '{$firstEmp}'")->fetchColumn();
    if ($checkTS == 0) {
        $pdo->exec("INSERT INTO cap_phat_tai_san (ma_nv, loai_tai_san, ten_tai_san, ma_dinh_danh_serial, so_luong, don_vi_tinh, ngay_cap_phat, tinh_trang_khi_cap, nguoi_giao, trang_thai) VALUES
        ('{$firstEmp}', 'MAY_TINH_LAPTOP', 'Laptop Dell Latitude 5430 Core i7', 'VNM-IT-LT-2023-045', 1, 'Chiếc', '2023-01-05', 'Mới 100% nguyên hộp', 'Trần IT', 'DANG_SU_DUNG'),
        ('{$firstEmp}', 'THE_TU_RA_VAO', 'Thẻ từ nhân viên tích hợp thang máy & bãi xe', 'CARD-883921', 1, 'Thẻ', '2023-01-02', 'Hoạt động tốt', 'Lễ tân', 'DANG_SU_DUNG'),
        ('{$firstEmp}', 'BHLD_PPE', 'Bộ đồ bảo hộ phòng sạch tiệt trùng tiêu chuẩn sữa', 'PPE-VNM-XL', 2, 'Bộ', '2023-01-05', 'Mới cấp phát', 'Kho bảo hộ', 'DANG_SU_DUNG')");
        echo "   -> Đã seed tài sản cấp phát cho {$firstEmp}" . PHP_EOL;
    }
}

echo "=== HOÀN TẤT MIGRATION 003 ===" . PHP_EOL;
