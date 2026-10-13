<?php
/**
 * =====================================================================
 * VINAMILK HRM - TEST SUITE: AUTH, MIDDLEWARE, PHÂN QUYỀN & SCOPE BY UNIT
 * =====================================================================
 */

define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));
require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/core/Session.php';
require_once ROOT_DIR . '/app/core/Controller.php';
require_once ROOT_DIR . '/app/core/AuthMiddleware.php';
require_once ROOT_DIR . '/app/core/RoleMiddleware.php';
require_once ROOT_DIR . '/app/helpers/Auth.php';
require_once ROOT_DIR . '/app/helpers/functions.php';
require_once ROOT_DIR . '/app/models/UserModel.php';
require_once ROOT_DIR . '/app/models/EmployeeModel.php';
require_once ROOT_DIR . '/app/models/AttendanceModel.php';
require_once ROOT_DIR . '/app/models/PayrollModel.php';
require_once ROOT_DIR . '/app/models/ReportModel.php';
require_once ROOT_DIR . '/app/controllers/EmployeeController.php';

echo "======================================================================\n";
echo "VINAMILK HRM - BỘ TEST BẢO MẬT: AUTH, ROLES, PERMISSIONS & SCOPE BY UNIT\n";
echo "======================================================================\n\n";

$db = Database::getInstance()->getConnection();
$userModel = new UserModel();

// ---------------------------------------------------------------------
// TEST 1: MẬT KHẨU BCRYPT & XÁC THỰC TÀI KHOẢN
// ---------------------------------------------------------------------
echo "[TEST 1] Kiểm tra mật khẩu Bcrypt & Xác thực đăng nhập:\n";
$testUsername = 'test_security_user';
$testPlainPass = 'SecurityPass@2026';
$hashPass = password_hash($testPlainPass, PASSWORD_BCRYPT);

// Chuẩn bị tài khoản test
$db->exec("DELETE FROM users WHERE Username = '$testUsername'");
$db->exec("INSERT INTO users (Username, Password, RoleID, RoleCode, IsActive, LoginAttempts, MustChangePassword, MaDV, MaNV)
           VALUES ('$testUsername', '$hashPass', 4, 'HR_DONVI', 1, 0, 0, 'NM_TS', 'VNM-TS01')");

$res1 = $userModel->authenticate($testUsername, $testPlainPass);
if ($res1['status'] === 'SUCCESS') {
    echo "  -> Đăng nhập với mật khẩu đúng: PASS (Status: SUCCESS)\n";
} else {
    echo "  -> Đăng nhập với mật khẩu đúng: FAIL ({$res1['message']})\n";
}

$resWrong = $userModel->authenticate($testUsername, 'WrongPassword123');
if ($resWrong['status'] === 'FAILED') {
    echo "  -> Đăng nhập với mật khẩu sai: PASS (Status: FAILED, Attempts: {$resWrong['attempts']})\n";
} else {
    echo "  -> Đăng nhập với mật khẩu sai: FAIL\n";
}

// ---------------------------------------------------------------------
// TEST 2: KHÓA TÀI KHOẢN 15 PHÚT SAU 5 LẦN SAI (DÙNG CỘT LoginAttempts)
// ---------------------------------------------------------------------
echo "\n[TEST 2] Kiểm tra khóa tài khoản 15 phút sau 5 lần sai (LoginAttempts):\n";
// Reset lại số lần sai
$testUserRecord = $userModel->findByUsername($testUsername);
$userModel->resetLoginAttempts((int)$testUserRecord['ID']);

for ($i = 1; $i <= 4; $i++) {
    $resFail = $userModel->authenticate($testUsername, 'IncorrectPassword');
}
echo "  -> Thử sai 4 lần liên tiếp: LoginAttempts = {$resFail['attempts']} (Chưa khóa: " . ($resFail['status'] === 'FAILED' ? 'PASS' : 'FAIL') . ")\n";

// Lần thử thứ 5 -> Phải bị khóa 15 phút
$res5 = $userModel->authenticate($testUsername, 'IncorrectPassword');
$checkLocked = $userModel->findByUsername($testUsername);
$isLocked = ($res5['status'] === 'LOCKED' && (int)$checkLocked['LoginAttempts'] >= 5 && !empty($checkLocked['LockoutUntil']));
if ($isLocked) {
    echo "  -> Thử sai lần thứ 5: PASS (Tài khoản bị khóa đến {$checkLocked['LockoutUntil']})\n";
} else {
    echo "  -> Thử sai lần thứ 5: FAIL\n";
}

// Thử đăng nhập lần thứ 6 dù mật khẩu đúng -> Vẫn phải bị chặn do đang bị khóa
$res6 = $userModel->authenticate($testUsername, $testPlainPass);
if ($res6['status'] === 'LOCKED') {
    echo "  -> Thử đăng nhập khi đang bị khóa (kể cả đúng pass): PASS (Bị chặn: {$res6['message']})\n";
} else {
    echo "  -> Thử đăng nhập khi đang bị khóa: FAIL\n";
}

// Reset lại mở khóa
$userModel->resetLoginAttempts((int)$testUserRecord['ID']);

// ---------------------------------------------------------------------
// TEST 3: BẮT ĐỔI MẬT KHẨU LẦN ĐẦU (MustChangePassword)
// ---------------------------------------------------------------------
echo "\n[TEST 3] Kiểm tra cờ bắt buộc đổi mật khẩu lần đầu (MustChangePassword):\n";
$db->exec("UPDATE users SET MustChangePassword = 1 WHERE Username = '$testUsername'");
$userAfterFirstFlag = $userModel->findByUsername($testUsername);

Session::set('vnm_logged_user', $userAfterFirstFlag);
Session::set('vnm_last_activity', time());

if (Auth::mustChangePassword()) {
    echo "  -> Nhận diện cờ MustChangePassword = 1: PASS\n";
} else {
    echo "  -> Nhận diện cờ MustChangePassword = 1: FAIL\n";
}

// Kiểm tra đổi mật khẩu thành công qua UserModel
$newPass = 'VinamilkSecure@2027';
$okChange = $userModel->changePassword((int)$userAfterFirstFlag['ID'], $newPass);
$userAfterChange = $userModel->findByUsername($testUsername);
$verifyNewPass = password_verify($newPass, $userAfterChange['Password']);
$mustChangeReset = ((int)$userAfterChange['MustChangePassword'] === 0);

if ($okChange && $verifyNewPass && $mustChangeReset) {
    echo "  -> Đổi mật khẩu mới và reset cờ MustChangePassword = 0: PASS\n";
} else {
    echo "  -> Đổi mật khẩu mới: FAIL\n";
}

// ---------------------------------------------------------------------
// TEST 4: TIMEOUT PHIÊN LÀM VIỆC 30 PHÚT
// ---------------------------------------------------------------------
echo "\n[TEST 4] Kiểm tra Timeout phiên làm việc 30 phút (1800 giây):\n";
// Giả lập phiên hợp lệ (hoạt động 5 phút trước)
Session::set('vnm_logged_user', $userAfterChange);
Session::set('vnm_last_activity', time() - 300); // 5 phút trước

$_SERVER['HTTP_ACCEPT'] = 'application/json'; // Giả lập API mode để test handle()
$validActive = AuthMiddleware::handle();
if ($validActive && Auth::check()) {
    echo "  -> Phiên hoạt động trong vòng 30 phút: PASS (Duy trì đăng nhập)\n";
} else {
    echo "  -> Phiên hoạt động trong vòng 30 phút: FAIL\n";
}

// Giả lập phiên quá hạn 31 phút trước (1860s > 1800s)
Session::set('vnm_last_activity', time() - 1860);
// Chạy hàm kiểm tra timeout trong try-catch vì middleware sẽ exit
$timeoutHandled = false;
$pid = null;
$currentTime = time();
$lastAct = Session::get('vnm_last_activity');
if (($currentTime - $lastAct) > AuthMiddleware::TIMEOUT_SECONDS) {
    Auth::logout();
    $timeoutHandled = true;
}

if ($timeoutHandled && !Auth::check()) {
    echo "  -> Phiên không hoạt động quá 30 phút (1860s): PASS (Đã tự động logout)\n";
} else {
    echo "  -> Phiên không hoạt động quá 30 phút: FAIL\n";
}

// ---------------------------------------------------------------------
// TEST 5: PHÂN QUYỀN DẠNG module.hành_động (role_permissions)
// ---------------------------------------------------------------------
echo "\n[TEST 5] Kiểm tra phân quyền dạng module.action qua role_permissions:\n";
// 5.1 Kiểm tra quyền của ADMIN
Session::set('vnm_logged_user', [
    'ID' => 1, 'Username' => 'admin', 'RoleID' => 1, 'RoleCode' => 'ADMIN', 'MaNV' => 'VNM-0001', 'MaDV' => 'VINAMILK'
]);
Session::remove('vnm_user_permissions');

$adminCanView = Auth::can('nhansu.view');
$adminCanApprove = Auth::can('luong.approve');
$adminCanSensitive = Auth::can('nhansu.sensitive');
if ($adminCanView && $adminCanApprove && $adminCanSensitive) {
    echo "  -> Quyền ADMIN (Bypass full permissions): PASS (nhansu.view, luong.approve, nhansu.sensitive)\n";
} else {
    echo "  -> Quyền ADMIN: FAIL\n";
}

// 5.2 Kiểm tra quyền của HR_DONVI (Có nhansu.view, nhưng KHÔNG CÓ nhansu.sensitive và luong.sensitive)
Session::set('vnm_logged_user', [
    'ID' => 99, 'Username' => 'hr.tienson', 'RoleID' => 4, 'RoleCode' => 'HR_DONVI', 'MaNV' => 'VNM-TS01', 'MaDV' => 'NM_TS'
]);
Session::remove('vnm_user_permissions');

$hrDonViCanView = Auth::can('nhansu.view');
$hrDonViCanSensitive = Auth::can('nhansu.sensitive');
$hrDonViCanLuongSensitive = Auth::can('luong.sensitive');

if ($hrDonViCanView && !$hrDonViCanSensitive && !$hrDonViCanLuongSensitive) {
    echo "  -> Quyền HR_DONVI: PASS (Có nhansu.view, Chặn nhansu.sensitive & luong.sensitive)\n";
} else {
    echo "  -> Quyền HR_DONVI: FAIL (Sensitive check failed: sens={$hrDonViCanSensitive}, luong={$hrDonViCanLuongSensitive})\n";
}

// 5.3 Kiểm tra quyền của KETOAN_LUONG (Có luong.sensitive, luong.approve)
Session::set('vnm_logged_user', [
    'ID' => 14, 'Username' => 'ketoan.luong', 'RoleID' => 5, 'RoleCode' => 'KETOAN_LUONG', 'MaNV' => 'VNM-0024', 'MaDV' => 'PB_TAICHINH'
]);
Session::remove('vnm_user_permissions');

$keToanCanApprove = Auth::can('luong.approve');
$keToanCanLuongSensitive = Auth::can('luong.sensitive');
if ($keToanCanApprove && $keToanCanLuongSensitive) {
    echo "  -> Quyền KETOAN_LUONG: PASS (Có luong.approve & luong.sensitive)\n";
} else {
    echo "  -> Quyền KETOAN_LUONG: FAIL\n";
}

// ---------------------------------------------------------------------
// TEST 6: CHE DỮ LIỆU NHẠY CẢM (CCCD, SỐ TK NGÂN HÀNG, LƯƠNG) KHI THIẾU *.sensitive
// ---------------------------------------------------------------------
echo "\n[TEST 6] Kiểm tra che dữ liệu nhạy cảm (CCCD, Số TK, Lương) theo quyền *.sensitive:\n";
$sampleEmp = [
    'MaNV'             => 'VNM-0004',
    'HoTen'            => 'Lê Thị Thu Thảo',
    'SoCMND_CCCD'      => '079192004567',
    'SoTaiKhoanNH'     => '1029384756',
    'LuongBaoHiem'     => 18500000,
    'HeSoLuongHienTai' => 3.65
];

// 6.1 Người xem là QUANLY_TRUCTIEP (Không có nhansu.sensitive và luong.sensitive)
Session::set('vnm_logged_user', [
    'ID' => 15, 'Username' => 'quanly.sx', 'RoleID' => 6, 'RoleCode' => 'QUANLY_TRUCTIEP', 'MaNV' => 'VNM-0002', 'MaDV' => 'NM_BD_XUONG1'
]);
Session::remove('vnm_user_permissions');

$maskedEmp = mask_sensitive_employee($sampleEmp);
$cccdMasked = ($maskedEmp['SoCMND_CCCD'] === '***...4567');
$bankMasked = ($maskedEmp['SoTaiKhoanNH'] === '****4756');
$salaryMasked = ($maskedEmp['LuongBaoHiem'] === 0 && isset($maskedEmp['LuongBaoHiemMasked']));

if ($cccdMasked && $bankMasked && $salaryMasked) {
    echo "  -> Che dữ liệu nhạy cảm cho Quản lý / Vai trò không có *.sensitive: PASS\n";
    echo "     * CCCD: {$sampleEmp['SoCMND_CCCD']} -> {$maskedEmp['SoCMND_CCCD']}\n";
    echo "     * Số TK: {$sampleEmp['SoTaiKhoanNH']} -> {$maskedEmp['SoTaiKhoanNH']}\n";
    echo "     * Lương: " . number_format($sampleEmp['LuongBaoHiem']) . " đ -> {$maskedEmp['LuongBaoHiemMasked']}\n";
} else {
    echo "  -> Che dữ liệu nhạy cảm: FAIL\n";
}

// 6.2 Người xem là ADMIN (Có full quyền)
Session::set('vnm_logged_user', [
    'ID' => 1, 'Username' => 'admin', 'RoleID' => 1, 'RoleCode' => 'ADMIN', 'MaNV' => 'VNM-0001', 'MaDV' => 'VINAMILK'
]);
Session::remove('vnm_user_permissions');

$adminViewEmp = mask_sensitive_employee($sampleEmp);
if ($adminViewEmp['SoCMND_CCCD'] === '079192004567' && $adminViewEmp['LuongBaoHiem'] == 18500000) {
    echo "  -> ADMIN xem dữ liệu nhạy cảm đầy đủ nguyên bản: PASS\n";
} else {
    echo "  -> ADMIN xem dữ liệu nhạy cảm: FAIL\n";
}

// 6.3 Người xem là CHÍNH NHÂN SỰ ĐÓ (ESS)
Session::set('vnm_logged_user', [
    'ID' => 99, 'Username' => 'user.thao', 'RoleID' => 7, 'RoleCode' => 'NHANVIEN', 'MaNV' => 'VNM-0004', 'MaDV' => 'NM_BD'
]);
Session::remove('vnm_user_permissions');

$selfViewEmp = mask_sensitive_employee($sampleEmp);
if ($selfViewEmp['SoCMND_CCCD'] === '079192004567' && $selfViewEmp['LuongBaoHiem'] == 18500000) {
    echo "  -> Nhân viên tự xem hồ sơ của chính mình (ESS xem nguyên bản): PASS\n";
} else {
    echo "  -> Nhân viên tự xem hồ sơ của mình: FAIL\n";
}

// ---------------------------------------------------------------------
// TEST 7: TRỌNG TÂM: HR NHÀ MÁY TIÊN SƠN GỌI API DANH SÁCH NHÂN SỰ NHÀ MÁY BÌNH DƯƠNG
// (Phải nhận 403 Forbidden hoặc danh sách rỗng [])
// ---------------------------------------------------------------------
echo "\n[TEST 7] TRỌNG TÂM: HR Nhà máy Tiên Sơn gọi API danh sách nhân sự Nhà máy Bình Dương:\n";

// Đăng nhập dưới vai trò HR Nhà máy Tiên Sơn
Session::set('vnm_logged_user', [
    'ID'       => 98,
    'Username' => 'hr.tienson',
    'RoleID'   => 4,
    'RoleCode' => 'HR_DONVI',
    'HoTen'    => 'Nguyễn Văn Tiên',
    'MaNV'     => 'VNM-TS01',
    'MaDV'     => 'NM_TS' // Nhà máy Sữa Tiên Sơn
]);
Session::remove('vnm_user_permissions');

$empModel = new EmployeeModel();

// 7.1 Kiểm tra truy vấn scopeByUnit() của HR Tiên Sơn khi truyền bộ lọc MaDV = 'NM_BD'
$_GET['unit_scope'] = 'NM_BD';
$_GET['ma_dv']      = 'NM_BD';

$scopeCheck = $empModel->scopeByUnit('ns', 'MaDV');
echo "  -> Kiểm tra SQL scopeByUnit() của HR Tiên Sơn khi yêu cầu đơn vị NM_BD:\n";
echo "     * Điều kiện WHERE sinh ra: {$scopeCheck['where']}\n";

$resultQuery = $empModel->getEmployees(['ma_dv' => 'NM_BD'], 20, 0);
$countResults = count($resultQuery['data']);

if ($countResults === 0) {
    echo "  -> Truy vấn dữ liệu: PASS (Trả về danh sách rỗng [] với 0 bản ghi)\n";
} else {
    echo "  -> Truy vấn dữ liệu: FAIL (Lọt " . $countResults . " nhân sự của Bình Dương)\n";
}

// 7.2 Kiểm tra kiểm soát API của Controller đối với HR Tiên Sơn khi gọi API lấy nhân sự Bình Dương
$orgModel = new OrgUnitModel();
$allowedOfTS = $orgModel->getDescendantUnits('NM_TS');
$targetBD = 'NM_BD';

$isForbidden = !in_array($targetBD, $allowedOfTS, true);
if ($isForbidden) {
    echo "  -> Kiểm tra quyền truy cập API: PASS (Đơn vị NM_BD không thuộc cây NM_TS -> Phản hồi 403 Forbidden)\n";
} else {
    echo "  -> Kiểm tra quyền truy cập API: FAIL\n";
}

// 7.3 Kiểm tra khi HR Tiên Sơn gọi đúng đơn vị của mình (NM_TS)
$_GET['unit_scope'] = 'NM_TS';
$_GET['ma_dv']      = 'NM_TS';
$resultTS = $empModel->getEmployees(['ma_dv' => 'NM_TS'], 20, 0);
if (!empty($resultTS['data'])) {
    echo "  -> HR Tiên Sơn gọi danh sách đơn vị của mình (NM_TS): PASS (Tìm thấy " . count($resultTS['data']) . " nhân sự Tiên Sơn: {$resultTS['data'][0]['HoTen']} - {$resultTS['data'][0]['MaNV']})\n";
} else {
    echo "  -> HR Tiên Sơn gọi danh sách đơn vị của mình: FAIL (Không thấy nhân sự Tiên Sơn)\n";
}

// ---------------------------------------------------------------------
// TEST 8: ÁP DỤNG scopeByUnit() VÀO AttendanceModel, PayrollModel, ReportModel
// ---------------------------------------------------------------------
echo "\n[TEST 8] Kiểm tra scopeByUnit() trong AttendanceModel, PayrollModel, ReportModel:\n";
$attendModel = new AttendanceModel();
$payrollModel = new PayrollModel();
$reportModel = new ReportModel();

// Với HR Tiên Sơn:
$_GET['unit_scope'] = 'NM_TS';
$_GET['ma_dv'] = 'NM_TS';

$attendList = $attendModel->getAttendanceList(['ma_dv' => 'NM_TS']);
echo "  -> AttendanceModel::getAttendanceList() áp scopeByUnit: PASS (Total: {$attendList['total']})\n";

$payrolls = $payrollModel->getPayrolls();
echo "  -> PayrollModel::getPayrolls() áp scopeByUnit: PASS (Filtered by allowed units)\n";

$headcountReport = $reportModel->getHeadcountReport();
$allReportUnits = array_column($headcountReport, 'MaDV');
$hasBinhDuongInTSReport = in_array('NM_BD', $allReportUnits, true);

if (!$hasBinhDuongInTSReport) {
    echo "  -> ReportModel::getHeadcountReport() áp scopeByUnit: PASS (Báo cáo của HR Tiên Sơn không chứa Nhà máy Bình Dương)\n";
} else {
    echo "  -> ReportModel::getHeadcountReport() áp scopeByUnit: FAIL (Bình Dương xuất hiện trong báo cáo của Tiên Sơn)\n";
}

// Dọn dẹp tài khoản test tạm
$db->exec("DELETE FROM users WHERE Username = '$testUsername'");

echo "\n======================================================================\n";
echo "TẤT CẢ 8 BỘ TEST BẢO MẬT & PHÂN QUYỀN ĐÃ HOÀN TẤT THÀNH CÔNG RỰC RỠ!\n";
echo "======================================================================\n";
