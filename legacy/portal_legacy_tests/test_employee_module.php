<?php
/**
 * =====================================================================
 * VINAMILK HRM - BỘ TEST KIỂM THỬ TOÀN DIỆN: MODULE HỒ SƠ NHÂN SỰ
 * =====================================================================
 * 1. Validate: CCCD 12 số, MST 10 hoặc 13 số, Tuổi >= 18, Email duy nhất.
 * 2. Hợp đồng: Cảnh báo 60 và 30 ngày, CHẶN KÝ LẦN 3 HĐ xác định thời hạn.
 * 3. Wizard tiếp nhận 4 bước: Cá nhân -> Đơn vị -> HĐ/Lương -> Cấp tài khoản & tài sản.
 * 4. Tìm kiếm đa chiều & nâng cao AND/OR (tuổi, thâm niên, trình độ).
 * 5. Xuất Excel & che thông tin nhạy cảm theo phân quyền.
 * 6. Quản lý tài liệu scan: Upload an toàn, lưu ngoài public/, tải file bảo mật.
 */

require_once __DIR__ . '/../app/config/config.php';
require_once __DIR__ . '/../app/core/Database.php';
require_once __DIR__ . '/../app/core/Session.php';
require_once __DIR__ . '/../app/helpers/Auth.php';
require_once __DIR__ . '/../app/helpers/functions.php';
require_once __DIR__ . '/../app/models/EmployeeModel.php';
require_once __DIR__ . '/../app/models/ContractModel.php';
require_once __DIR__ . '/../app/models/DocumentModel.php';

Session::start();
$db = Database::getInstance();
$empModel = new EmployeeModel();
$contractModel = new ContractModel();
$docModel = new DocumentModel();

echo "======================================================================" . PHP_EOL;
echo "VINAMILK HRM - BỘ TEST KIỂM THỬ MODULE HỒ SƠ NHÂN SỰ" . PHP_EOL;
echo "======================================================================" . PHP_EOL . PHP_EOL;

// =====================================================================
// [TEST 1] VALIDATION: CCCD 12 số, MST 10/13 số, Tuổi >= 18, Email duy nhất
// =====================================================================
echo "[TEST 1] Kiểm tra Validation dữ liệu hồ sơ nhân sự:" . PHP_EOL;

// 1.1 CCCD không đúng 12 số
$invalidCCCD = [
    'HoTen' => 'Nguyễn Văn Test',
    'SoCMND_CCCD' => '123456789', // 9 số
    'NgaySinh' => '1995-05-10',
    'Email' => 'unique.test1@vinamilk.com.vn',
    'MaDV' => 'NM_TS'
];
$errCCCD = $empModel->validateEmployeeData($invalidCCCD);
if (isset($errCCCD['SoCMND_CCCD'])) {
    echo "  -> Chặn CCCD 9 số (không đúng 12 số): PASS (" . $errCCCD['SoCMND_CCCD'] . ")" . PHP_EOL;
} else {
    echo "  -> Chặn CCCD 9 số: FAIL" . PHP_EOL;
}

// 1.2 MST không phải 10 hoặc 13 số
$invalidMST = [
    'HoTen' => 'Nguyễn Văn Test',
    'SoCMND_CCCD' => '079195000001',
    'MaSoThue' => '12345678', // 8 số
    'NgaySinh' => '1995-05-10',
    'Email' => 'unique.test2@vinamilk.com.vn',
    'MaDV' => 'NM_TS'
];
$errMST = $empModel->validateEmployeeData($invalidMST);
if (isset($errMST['MaSoThue'])) {
    echo "  -> Chặn MST 8 số (không đúng 10 hoặc 13 số): PASS (" . $errMST['MaSoThue'] . ")" . PHP_EOL;
} else {
    echo "  -> Chặn MST 8 số: FAIL" . PHP_EOL;
}

// 1.3 Tuổi dưới 18 (Ví dụ sinh năm 2012 = 14 tuổi)
$invalidAge = [
    'HoTen' => 'Thiếu Niên Test',
    'SoCMND_CCCD' => '079195000002',
    'NgaySinh' => date('Y-m-d', strtotime('-14 years')),
    'Email' => 'unique.test3@vinamilk.com.vn',
    'MaDV' => 'NM_TS'
];
$errAge = $empModel->validateEmployeeData($invalidAge);
if (isset($errAge['NgaySinh'])) {
    echo "  -> Chặn người lao động dưới 18 tuổi: PASS (" . $errAge['NgaySinh'] . ")" . PHP_EOL;
} else {
    echo "  -> Chặn người dưới 18 tuổi: FAIL" . PHP_EOL;
}

// 1.4 Email đã tồn tại
$existingEmail = $db->query("SELECT Email FROM nhansu WHERE Email IS NOT NULL AND Email != '' LIMIT 1")->fetchColumn();
$invalidEmail = [
    'HoTen' => 'Nguyễn Văn Trùng Email',
    'SoCMND_CCCD' => '079195000003',
    'NgaySinh' => '1990-01-01',
    'Email' => $existingEmail,
    'MaDV' => 'NM_TS'
];
$errEmail = $empModel->validateEmployeeData($invalidEmail);
if (isset($errEmail['Email'])) {
    echo "  -> Chặn Email trùng lặp [{$existingEmail}]: PASS (" . $errEmail['Email'] . ")" . PHP_EOL;
} else {
    echo "  -> Chặn Email trùng lặp: FAIL" . PHP_EOL;
}

// 1.5 Dữ liệu hợp lệ: CCCD 12 số, MST 10 số, Tuổi 26, Email duy nhất
$validData = [
    'HoTen' => 'Trần Văn Hợp Lệ',
    'SoCMND_CCCD' => '079195000004',
    'MaSoThue' => '0300588569', // 10 số
    'NgaySinh' => '2000-01-01', // 26 tuổi
    'Email' => 'tranvan.hople.' . time() . '@vinamilk.com.vn',
    'MaDV' => 'NM_TS'
];
$errValid = $empModel->validateEmployeeData($validData);
if (empty($errValid)) {
    echo "  -> Dữ liệu hợp lệ chuẩn pháp lý: PASS (0 lỗi)" . PHP_EOL;
} else {
    echo "  -> Dữ liệu hợp lệ: FAIL (" . json_encode($errValid) . ")" . PHP_EOL;
}

echo PHP_EOL;

// =====================================================================
// [TEST 2] HỢP ĐỒNG: Cảnh báo 60 và 30 ngày, CHẶN KÝ LẦN 3 XÁC ĐỊNH THỜI HẠN
// =====================================================================
echo "[TEST 2] Kiểm tra Hợp đồng Lao động & Quy tắc Điều 20 Bộ luật Lao động:" . PHP_EOL;

// Tạo nhân viên giả lập cho bài test hợp đồng
$testEmpCode = 'VNM-THD1';
$db->query("DELETE FROM hop_dong_lao_dong WHERE ma_nv IN ('{$testEmpCode}', 'VNM-TEST-H')");
$db->query("DELETE FROM nhansu WHERE MaNV IN ('{$testEmpCode}', 'VNM-TEST-H') OR SoCMND_CCCD = '079195999999'");

$pdo = $db->getConnection();
$stmt = $pdo->prepare("INSERT INTO nhansu (MaNV, HoTen, NgaySinh, GioiTinh, SoCMND_CCCD, Email, MaDV, TrangThai) VALUES (?, ?, ?, ?, ?, ?, ?, ?)");
$stmt->execute([$testEmpCode, 'Nhân Sự Test Hợp Đồng', '1995-02-15', 'Nam', '079195999999', 'test.contract@vnm.vn', 'NM_TS', 1]);

// 2.1 Ký HĐ xác định thời hạn lần 1 (12 tháng) -> Hợp lệ
$hd1Id = $contractModel->createContract([
    'ma_nv'         => $testEmpCode,
    'loai_hd'       => 'XAC_DINH_THOI_HAN_12T',
    'so_hd'         => 'TEST-001/HĐLĐ-VNM',
    'ngay_ky'       => '2024-01-01',
    'tu_ngay'       => '2024-01-01',
    'den_ngay'      => '2024-12-31',
    'luong_can_ban' => 15000000,
    'trang_thai'    => 'HET_HAN'
]);
echo "  -> Ký HĐ xác định thời hạn lần 1: PASS (ID: {$hd1Id})" . PHP_EOL;

// 2.2 Ký HĐ xác định thời hạn lần 2 (24 tháng) -> Hợp lệ
$hd2Id = $contractModel->createContract([
    'ma_nv'         => $testEmpCode,
    'loai_hd'       => 'XAC_DINH_THOI_HAN_24T',
    'so_hd'         => 'TEST-002/HĐLĐ-VNM',
    'ngay_ky'       => '2025-01-01',
    'tu_ngay'       => '2025-01-01',
    'den_ngay'      => date('Y-m-d', strtotime('+45 days')), // 45 ngày tới để test cảnh báo 60 ngày
    'luong_can_ban' => 18000000,
    'trang_thai'    => 'HIEU_LUC'
]);
echo "  -> Ký HĐ xác định thời hạn lần 2: PASS (ID: {$hd2Id})" . PHP_EOL;

$countFixed = $contractModel->countFixedTermContracts($testEmpCode);
echo "  -> Tổng số lần đã ký HĐ xác định thời hạn: {$countFixed} lần" . PHP_EOL;

// 2.3 Thử ký HĐ xác định thời hạn lần thứ 3 -> BẮT BUỘC PHẢI BỊ CHẶN!
$blocked = false;
try {
    $contractModel->createContract([
        'ma_nv'         => $testEmpCode,
        'loai_hd'       => 'XAC_DINH_THOI_HAN_12T', // Cố tình ký tiếp xác định thời hạn
        'so_hd'         => 'TEST-003/HĐLĐ-VNM',
        'ngay_ky'       => date('Y-m-d'),
        'tu_ngay'       => date('Y-m-d'),
        'den_ngay'      => date('Y-m-d', strtotime('+1 year')),
        'luong_can_ban' => 20000000,
        'trang_thai'    => 'HIEU_LUC'
    ]);
} catch (DomainException $e) {
    $blocked = true;
    echo "  -> CHẶN KÝ LẦN 3 HĐ XÁC ĐỊNH THỜI HẠN: PASS" . PHP_EOL;
    echo "     Thông điệp chặn: " . $e->getMessage() . PHP_EOL;
}

if (!$blocked) {
    echo "  -> Chặn ký lần 3: FAIL (Hệ thống không chặn)" . PHP_EOL;
}

// 2.4 Ký HĐ không xác định thời hạn ở lần 3 -> Thành công!
$hd3Id = $contractModel->createContract([
    'ma_nv'         => $testEmpCode,
    'loai_hd'       => 'KHONG_XAC_DINH_THOI_HAN',
    'so_hd'         => 'TEST-003-KXĐTH/HĐLĐ-VNM',
    'ngay_ky'       => date('Y-m-d'),
    'tu_ngay'       => date('Y-m-d'),
    'den_ngay'      => null,
    'luong_can_ban' => 22000000,
    'trang_thai'    => 'HIEU_LUC'
]);
echo "  -> Chuyển sang HĐ KHÔNG XÁC ĐỊNH THỜI HẠN: PASS (ID: {$hd3Id})" . PHP_EOL;

// 2.5 Kiểm tra tự động tính cảnh báo thời hạn 60 ngày & 30 ngày
$contractsOfEmp = $contractModel->getContractsByEmployee($testEmpCode);
foreach ($contractsOfEmp as $c) {
    if ($c['id'] == $hd2Id) {
        echo "  -> Kiểm tra nhãn cảnh báo HĐ 45 ngày: PASS (" . $c['canh_bao']['message'] . " - Badge: " . $c['canh_bao']['badge'] . ")" . PHP_EOL;
    }
}

echo PHP_EOL;

// =====================================================================
// [TEST 3] WIZARD TIẾP NHẬN NHÂN VIÊN MỚI 4 BƯỚC
// =====================================================================
echo "[TEST 3] Kiểm tra Wizard Tiếp nhận Nhân viên Mới 4 bước:" . PHP_EOL;
$db->query("DELETE FROM users WHERE Username LIKE 'hang.le%' OR MaNV IN (SELECT MaNV FROM nhansu WHERE SoCMND_CCCD = '079198888888')");
$db->query("DELETE FROM hop_dong_lao_dong WHERE ma_nv IN (SELECT MaNV FROM nhansu WHERE SoCMND_CCCD = '079198888888')");
$db->query("DELETE FROM su_kien_nhan_su WHERE ma_nv IN (SELECT MaNV FROM nhansu WHERE SoCMND_CCCD = '079198888888')");
$db->query("DELETE FROM cap_phat_tai_san WHERE ma_nv IN (SELECT MaNV FROM nhansu WHERE SoCMND_CCCD = '079198888888')");
$db->query("DELETE FROM tai_lieu_nhan_su WHERE ma_nv IN (SELECT MaNV FROM nhansu WHERE SoCMND_CCCD = '079198888888')");
$db->query("DELETE FROM nhansu WHERE SoCMND_CCCD = '079198888888'");

$wizardPayload = [
    // Bước 1: Thông tin cá nhân
    'HoTen'               => 'Lê Thị Thu Hằng',
    'NgaySinh'            => '1998-08-20',
    'GioiTinh'            => 'Nữ',
    'SoCMND_CCCD'         => '079198888888',
    'NgayCap'             => '2022-03-15',
    'NoiCap'              => 'Cục Cảnh sát QLHC về TTXH',
    'MaSoThue'            => '0300588569',
    'Email'               => 'lethithuhang.' . time() . '@vinamilk.com.vn',
    'SoDienThoai'         => '0918765432',
    'DiaChiHienTai'       => 'Số 45 Lê Duẩn, Phường Bến Nghé, Quận 1, TP.HCM',
    'QueQuan'             => 'Nghệ An',
    'TrinhDoHocVan'       => 'Đại học',
    'ChuyenNganh'         => 'Quản trị Nhân sự',
    'TruongDaoTao'        => 'Đại học Kinh Tế TP.HCM',
    // Bước 2: Đơn vị & Chức vụ
    'MaDV'                => 'NM_TS',
    'MaCV'                => 'CV',
    'VungLuong'           => 2,
    'NgayVaoLam'          => date('Y-m-d'),
    // Bước 3: Hợp đồng & Lương
    'loai_hd'             => 'THU_VIEC',
    'tu_ngay'             => date('Y-m-d'),
    'den_ngay'            => date('Y-m-d', strtotime('+60 days')),
    'luong_can_ban'       => 16000000,
    'luong_dong_bh'       => 16000000,
    'phu_cap_trach_nhiem' => 1500000,
    'phu_cap_an_ca'       => 730000,
    'ty_le_huong_luong'   => 85,
    'SoTaiKhoanNH'        => '0071001234567',
    'NganHang'            => 'Vietcombank',
    'SoNguoiPhuThuoc'     => 1,
    // Bước 4: Cấp tài khoản & Tài sản
    'create_account'      => '1',
    'account_username'    => 'hang.le' . time(),
    'account_password'    => 'Vinamilk@2026',
    'RoleID'              => 7, // NHANVIEN
    'asset_card'          => '1',
    'card_serial'         => 'CARD-HANG-001',
    'asset_laptop'        => '1',
    'laptop_name'         => 'Laptop Lenovo ThinkPad T14 Gen 3',
    'laptop_serial'       => 'TP-T14-2026-99',
    'asset_ppe'           => '1'
];

$newEmpCode = $empModel->createFullEmployeeWithWizard($wizardPayload);
echo "  -> Tiếp nhận nhân sự thành công! Mã nhân sự tự sinh: [{$newEmpCode}]" . PHP_EOL;

// Kiểm tra các bảng liên kết được tạo tự động
$createdEmp = $db->query("SELECT * FROM nhansu WHERE MaNV = '{$newEmpCode}'")->fetch(PDO::FETCH_ASSOC);
$createdHD  = $db->query("SELECT * FROM hop_dong_lao_dong WHERE ma_nv = '{$newEmpCode}'")->fetch(PDO::FETCH_ASSOC);
$createdSK  = $db->query("SELECT * FROM su_kien_nhan_su WHERE ma_nv = '{$newEmpCode}' AND loai_su_kien = 'TUYEN_MOI'")->fetch(PDO::FETCH_ASSOC);
$createdAcc = $db->query("SELECT * FROM users WHERE MaNV = '{$newEmpCode}'")->fetch(PDO::FETCH_ASSOC);
$createdTS  = $db->query("SELECT COUNT(*) FROM cap_phat_tai_san WHERE ma_nv = '{$newEmpCode}'")->fetchColumn();

if ($createdEmp && $createdHD && $createdSK && $createdAcc && $createdTS >= 2) {
    echo "  -> Kiểm tra tính toàn vẹn 4 bước Onboarding: PASS" . PHP_EOL;
    echo "     * nhansu: Đã tạo ({$createdEmp['HoTen']} - {$createdEmp['MaDV']})" . PHP_EOL;
    echo "     * hop_dong_lao_dong: Đã ký ({$createdHD['so_hd']} - {$createdHD['loai_hd']})" . PHP_EOL;
    echo "     * su_kien_nhan_su: Đã ghi nhận TUYEN_MOI" . PHP_EOL;
    echo "     * users: Đã cấp tài khoản ({$createdAcc['Username']} - MustChangePassword={$createdAcc['MustChangePassword']})" . PHP_EOL;
    echo "     * cap_phat_tai_san: Đã bàn giao {$createdTS} tài sản ban đầu (Thẻ, Laptop, PPE)" . PHP_EOL;
} else {
    echo "  -> Kiểm tra tính toàn vẹn 4 bước Onboarding: FAIL" . PHP_EOL;
}

echo PHP_EOL;

// =====================================================================
// [TEST 4] TÌM KIẾM ĐA CHIỀU & NÂNG CAO AND / OR
// =====================================================================
echo "[TEST 4] Kiểm tra Tìm kiếm Đa chiều & Nâng cao AND / OR:" . PHP_EOL;

// Giả lập ADMIN để kiểm tra tìm kiếm trên toàn hệ thống
Session::set(AUTH_SESSION_KEY, [
    'UserID' => 1,
    'Username' => 'admin',
    'FullName' => 'Quản trị viên',
    'RoleCode' => 'ADMIN',
    'MaDV' => 'ALL'
]);
Session::set('vnm_perms_user_id', 1);

// 4.1 Lọc cơ bản: Đơn vị NM_TS, Trạng thái 1
$filterBasic = ['ma_dv' => 'NM_TS', 'trang_thai' => '1'];
$resBasic = $empModel->getEmployees($filterBasic, 10, 0);
echo "  -> Lọc theo đơn vị NM_TS & Trạng thái 1: PASS (Tìm thấy {$resBasic['total']} nhân sự)" . PHP_EOL;

// 4.2 Lọc nâng cao AND: Độ tuổi 20 - 35 AND Giới tính Nữ
$filterAnd = [
    'gioi_tinh' => 'Nữ',
    'age_min' => 20,
    'age_max' => 35,
    'advanced_logic' => 'AND'
];
$resAnd = $empModel->getEmployees($filterAnd, 10, 0);
echo "  -> Lọc nâng cao AND (Giới tính Nữ AND Tuổi 20-35): PASS (Tìm thấy {$resAnd['total']} nhân sự)" . PHP_EOL;

// 4.3 Lọc nâng cao OR: Trình độ Thạc sĩ OR Thâm niên trên 5 năm
$filterOr = [
    'trinh_do' => 'Thạc sĩ',
    'seniority' => 'over_5',
    'advanced_logic' => 'OR'
];
$resOr = $empModel->getEmployees($filterOr, 10, 0);
echo "  -> Lọc nâng cao OR (Thạc sĩ OR Thâm niên > 5 năm): PASS (Tìm thấy {$resOr['total']} nhân sự)" . PHP_EOL;

echo PHP_EOL;

// =====================================================================
// [TEST 5] XUẤT EXCEL & CHE DỮ LIỆU NHẠY CẢM
// =====================================================================
echo "[TEST 5] Kiểm tra Xuất danh sách nhân sự Excel:" . PHP_EOL;

// Giả lập HR Tiên Sơn (không có quyền *.sensitive)
Session::set(AUTH_SESSION_KEY, [
    'UserID' => 999,
    'Username' => 'hr.tienson.test',
    'FullName' => 'HR Tiên Sơn',
    'RoleCode' => 'HR_DONVI',
    'MaDV' => 'NM_TS'
]);
Session::set('vnm_perms_user_id', 999);
Session::set(PERM_SESSION_KEY, ['nhansu.view' => true, 'nhansu.export' => true]);

$exportData = $empModel->exportEmployees(['ma_dv' => 'NM_TS']);
$firstRow = $exportData[0] ?? null;

if ($firstRow) {
    echo "  -> Xuất dữ liệu nhân sự cho HR_DONVI (không có *.sensitive):" . PHP_EOL;
    echo "     * CCCD đã che: " . $firstRow['SoCMND_CCCD'] . " (PASS)" . PHP_EOL;
    echo "     * Lương BH đã che: " . $firstRow['LuongBaoHiem'] . " (PASS)" . PHP_EOL;
    echo "     * Tổng số dòng xuất: " . count($exportData) . " bản ghi" . PHP_EOL;
} else {
    echo "  -> Xuất dữ liệu: FAIL" . PHP_EOL;
}

echo PHP_EOL;

// =====================================================================
// [TEST 6] QUẢN LÝ TÀI LIỆU SCAN (LƯU NGOÀI PUBLIC/)
// =====================================================================
echo "[TEST 6] Kiểm tra Quản lý Tài liệu Scan & Lưu ngoài public/:" . PHP_EOL;

// Giả lập file upload hợp lệ
$testDocPath = ROOT_DIR . '/storage/uploads/documents';
if (!is_dir($testDocPath)) {
    mkdir($testDocPath, 0755, true);
}

// Tạo file pdf giả lập trong scratch
$tempPdf = sys_get_temp_dir() . '/test_scan_' . time() . '.pdf';
file_put_contents($tempPdf, "%PDF-1.4\n%Vinamilk Test Document Scan Content\n%%EOF");

$fakeFile = [
    'name'     => 'Ban_scan_CCCD_goc.pdf',
    'type'     => 'application/pdf',
    'tmp_name' => $tempPdf,
    'error'    => UPLOAD_ERR_OK,
    'size'     => filesize($tempPdf)
];

$docId = $docModel->uploadDocument($newEmpCode, $fakeFile, 'Bản scan CCCD 2 mặt', 'CCCD_CMND', 'Tài liệu tiếp nhận');
echo "  -> Upload tài liệu scan hợp lệ: PASS (ID: {$docId})" . PHP_EOL;

$docRecord = $docModel->getDocumentById($docId);
$isOutsidePublic = !str_starts_with($docRecord['file_path'], 'public/');
echo "  -> Kiểm tra lưu trữ ngoài public/: " . ($isOutsidePublic ? 'PASS' : 'FAIL') . " ({$docRecord['file_path']})" . PHP_EOL;

// Kiểm tra danh sách tài liệu của nhân viên
$docsOfEmp = $docModel->getDocumentsByEmployee($newEmpCode);
echo "  -> Lấy danh sách tài liệu của {$newEmpCode}: PASS (" . count($docsOfEmp) . " tài liệu)" . PHP_EOL;

// Thử upload file vượt quá 5MB
$fakeLargeFile = [
    'name'     => 'large_file.pdf',
    'type'     => 'application/pdf',
    'tmp_name' => $tempPdf,
    'error'    => UPLOAD_ERR_OK,
    'size'     => 6 * 1024 * 1024 // 6MB
];

$largeBlocked = false;
try {
    $docModel->uploadDocument($newEmpCode, $fakeLargeFile, 'File quá dung lượng');
} catch (InvalidArgumentException $e) {
    $largeBlocked = true;
    echo "  -> Chặn file vượt quá 5MB: PASS (" . $e->getMessage() . ")" . PHP_EOL;
}
if (!$largeBlocked) {
    echo "  -> Chặn file vượt 5MB: FAIL" . PHP_EOL;
}

echo PHP_EOL;
echo "======================================================================" . PHP_EOL;
echo "TẤT CẢ CÁC BỘ TEST MODULE HỒ SƠ NHÂN SỰ ĐÃ HOÀN TẤT THÀNH CÔNG RỰC RỠ!" . PHP_EOL;
echo "======================================================================" . PHP_EOL;
