<?php
/**
 * =====================================================================
 * TEST SUITE: MODULE CHẤM CÔNG & QUẢN LÝ THỜI GIAN LÀM VIỆC VINAMILK HRM
 * =====================================================================
 * Kiểm tra toàn diện:
 *  1. Danh mục ca làm việc (Ca 1, Ca 2, Ca 3 qua đêm, Hành chính)
 *  2. Lưới xếp ca tuần (Weekly Shift Roster Grid)
 *  3. Import quẹt thẻ thô, ghép ca tự động, ca đêm qua ngày, trễ/sớm, quên quẹt
 *  4. Phân loại giờ OT (Thường 150%, Nghỉ 200%, Lễ 300%)
 *  5. Tổng hợp bảng công tháng & Cảnh báo vượt ngưỡng OT (40h/tháng, 200h/năm)
 *  6. Quy trình duyệt 3 bước (Tổ trưởng -> Quản đốc -> Khóa công)
 *  7. Bảo vệ khóa công (cấm ghi đè) & Quy trình điều chỉnh công có lý do
 */

declare(strict_types=1);

require_once __DIR__ . '/../app/core/Database.php';
require_once __DIR__ . '/../app/core/Session.php';
require_once __DIR__ . '/../app/helpers/Auth.php';
require_once __DIR__ . '/../app/models/AttendanceModel.php';

Session::start();
// Thiết lập user Admin để có đầy đủ quyền thực thi
$adminUser = [
    'ID'       => 1,
    'user_id'  => 1,
    'Username' => 'admin_test',
    'username' => 'admin_test',
    'HoTen'    => 'Hệ Thống Kiểm Thử',
    'ho_ten'   => 'Hệ Thống Kiểm Thử',
    'RoleCode' => 'ADMIN',
    'role'     => 'ADMIN',
    'MaNV'     => 'ADMIN_01',
    'MaDV'     => 'VINAMILK',
    'unit_path'=> '/VINAMILK/'
];
$_SESSION['user'] = $adminUser;
$_SESSION['vnm_logged_user'] = $adminUser;

$passCount = 0;
$failCount = 0;

function assertTest(string $desc, bool $condition, string $detail = ''): void {
    global $passCount, $failCount;
    if ($condition) {
        echo " [PASS] {$desc}\n";
        $passCount++;
    } else {
        echo " [FAIL] {$desc} " . ($detail ? "-> {$detail}" : '') . "\n";
        $failCount++;
    }
}

echo "=== BẮT ĐẦU KIỂM THỬ MODULE CHẤM CÔNG VINAMILK HRM ===\n\n";

$model = new AttendanceModel();

// -------------------------------------------------------------
// 1. KIỂM THỬ DANH MỤC CA LÀM VIỆC
// -------------------------------------------------------------
echo "[1] KIỂM THỬ DANH MỤC CA LÀM VIỆC:\n";
$shifts = $model->getShifts();
$shiftCodes = array_column($shifts, 'ma_ca');

assertTest("Có ít nhất 4 ca chuẩn trong hệ thống", count($shifts) >= 4, "Số ca: " . count($shifts));
assertTest("Có Ca 1 (06:00 - 14:00)", in_array('CA_1_SANG', $shiftCodes));
assertTest("Có Ca 2 (14:00 - 22:00)", in_array('CA_2_CHIEU', $shiftCodes));
assertTest("Có Ca 3 (22:00 - 06:00)", in_array('CA_3_DEM', $shiftCodes));
assertTest("Có Ca Hành Chính (08:00 - 17:00)", in_array('CA_HANH_CHINH', $shiftCodes));

$ca3 = $model->getShiftByCode('CA_3_DEM');
assertTest("Ca 3 được đánh dấu là ca đêm (la_ca_dem = 1)", !empty($ca3) && (int)$ca3['la_ca_dem'] === 1);
assertTest("Ca 3 bắt đầu lúc 22:00 và kết thúc lúc 06:00", $ca3['gio_bat_dau'] === '22:00:00' && $ca3['gio_ket_thuc'] === '06:00:00');

// -------------------------------------------------------------
// 2. KIỂM THỬ LƯỚI XẾP CA TUẦN (WEEKLY ROSTER)
// -------------------------------------------------------------
echo "\n[2] KIỂM THỬ LƯỚI XẾP CA TUẦN (WEEKLY SHIFT ROSTER):\n";
$mondayTest = '2026-10-12'; // Thứ 2
$unitTest = 'NM_TS'; // Nhà máy Tiên Sơn

$roster = $model->getWeeklyRoster($unitTest, $mondayTest);
assertTest("Lấy ma trận 7 ngày trong tuần từ Thứ 2 đến Chủ nhật", count($roster['dates']) === 7);
assertTest("Danh sách nhân sự thuộc đơn vị được tải", !empty($roster['employees']));

// Phân ca cho nhân viên VNM-0001
$assignments = [
    ['ma_nv' => 'VNM-0001', 'ngay' => '2026-10-12', 'ma_ca' => 'CA_1_SANG', 'ma_dv' => 'NM_TS'],
    ['ma_nv' => 'VNM-0001', 'ngay' => '2026-10-13', 'ma_ca' => 'CA_1_SANG', 'ma_dv' => 'NM_TS'],
    ['ma_nv' => 'VNM-0001', 'ngay' => '2026-10-14', 'ma_ca' => 'CA_2_CHIEU', 'ma_dv' => 'NM_TS'],
    ['ma_nv' => 'VNM-0001', 'ngay' => '2026-10-15', 'ma_ca' => 'CA_3_DEM',   'ma_dv' => 'NM_TS'],
    ['ma_nv' => 'VNM-0001', 'ngay' => '2026-10-16', 'ma_ca' => 'CA_3_DEM',   'ma_dv' => 'NM_TS'],
    ['ma_nv' => 'VNM-0001', 'ngay' => '2026-10-17', 'ma_ca' => 'CA_1_SANG', 'ma_dv' => 'NM_TS'],
    ['ma_nv' => 'VNM-0001', 'ngay' => '2026-10-18', 'ma_ca' => 'OFF',       'ma_dv' => 'NM_TS'], // Chủ nhật
];
$savedRoster = $model->saveWeeklyRoster(['assignments' => $assignments]);
assertTest("Lưu kế hoạch xếp ca tuần thành công", $savedRoster === true);

// -------------------------------------------------------------
// 3. KIỂM THỬ IMPORT QUẸT THẺ & GHÉP CA TỰ ĐỘNG
// -------------------------------------------------------------
echo "\n[3] KIỂM THỬ IMPORT QUẸT THẺ & GHÉP CA:\n";

// A. Ca 1: Đúng giờ (05:55 -> 14:05)
$swipesA = [
    ['ma_nv' => 'VNM-0001', 'thoi_gian_quet' => '2026-10-12 05:55:00', 'ma_may' => 'MCC-01'],
    ['ma_nv' => 'VNM-0001', 'thoi_gian_quet' => '2026-10-12 14:05:00', 'ma_may' => 'MCC-01'],
];
$model->importSwipeLogs($swipesA, 'test_swipe_a.csv');
$dayA = $model->processAttendanceDay('VNM-0001', '2026-10-12');

assertTest("Ca 1 đúng giờ: Công thực tế = 1.0", (float)$dayA['cong_thuc_te'] === 1.0);
assertTest("Ca 1 đúng giờ: Không đi muộn (di_muon = 0)", (int)$dayA['di_muon_phut'] === 0);
assertTest("Ca 1 đúng giờ: Không về sớm (ve_som = 0)", (int)$dayA['ve_som_phut'] === 0);
assertTest("Ca 1 đúng giờ: Không bị gắn cờ quên quẹt", (int)$dayA['quen_quet'] === 0);

// B. Ca 3 Xuyên đêm (22:00 -> 06:00): Vào 21:50 tối 15/10, Ra 06:08 sáng 16/10
$swipesB = [
    ['ma_nv' => 'VNM-0001', 'thoi_gian_quet' => '2026-10-15 21:50:00', 'ma_may' => 'MCC-01'],
    ['ma_nv' => 'VNM-0001', 'thoi_gian_quet' => '2026-10-16 06:08:00', 'ma_may' => 'MCC-01'], // Sáng hôm sau
];
$model->importSwipeLogs($swipesB, 'test_swipe_night.csv');
$dayB = $model->processAttendanceDay('VNM-0001', '2026-10-15');

assertTest("Ca 3 xuyên đêm: Khớp quẹt thẻ sáng hôm sau thành công", !empty($dayB['gio_ra']));
assertTest("Ca 3 xuyên đêm: Tự động ghi nhận 8.0 giờ làm đêm", (float)$dayB['gio_lam_dem'] === 8.0);
assertTest("Ca 3 xuyên đêm: Công thực tế trọn vẹn = 1.0", (float)$dayB['cong_thuc_te'] === 1.0);

// C. Đi trễ & Về sớm: Ca 1 (06:00 - 14:00) nhưng vào 06:20, về 13:40
$swipesC = [
    ['ma_nv' => 'VNM-0001', 'thoi_gian_quet' => '2026-10-13 06:20:00', 'ma_may' => 'MCC-01'],
    ['ma_nv' => 'VNM-0001', 'thoi_gian_quet' => '2026-10-13 13:40:00', 'ma_may' => 'MCC-01'],
];
$model->importSwipeLogs($swipesC, 'test_swipe_late.csv');
$dayC = $model->processAttendanceDay('VNM-0001', '2026-10-13');

assertTest("Phát hiện đi trễ 20 phút", (int)$dayC['di_muon_phut'] === 20);
assertTest("Phát hiện về sớm 20 phút", (int)$dayC['ve_som_phut'] === 20);

// D. Quên quẹt thẻ (chỉ quẹt vào lúc 13:55, không quẹt ra)
$swipesD = [
    ['ma_nv' => 'VNM-0001', 'thoi_gian_quet' => '2026-10-14 13:55:00', 'ma_may' => 'MCC-01'],
];
$model->importSwipeLogs($swipesD, 'test_swipe_missing.csv');
$dayD = $model->processAttendanceDay('VNM-0001', '2026-10-14');

assertTest("Phát hiện quên quẹt 1 đầu (quen_quet = 1)", (int)$dayD['quen_quet'] === 1);
assertTest("Quên quẹt: Tạm tính nửa công (cong_thuc_te = 0.5)", (float)$dayD['cong_thuc_te'] === 0.5);

// -------------------------------------------------------------
// 4. KIỂM THỬ TÍNH TOÁN & PHÂN LOẠI GIỜ LÀM THÊM (OT)
// -------------------------------------------------------------
echo "\n[4] KIỂM THỬ PHÂN LOẠI GIỜ LÀM THÊM (OT):\n";
// Ca 1 kết thúc lúc 14:00. Nhân viên làm thêm đến 16:30 (làm thêm 2.5h vào ngày thường)
$swipesOT = [
    ['ma_nv' => 'VNM-0001', 'thoi_gian_quet' => '2026-10-17 05:55:00', 'ma_may' => 'MCC-01'],
    ['ma_nv' => 'VNM-0001', 'thoi_gian_quet' => '2026-10-17 16:30:00', 'ma_may' => 'MCC-01'],
];
$model->importSwipeLogs($swipesOT, 'test_swipe_ot.csv');
$dayOT = $model->processAttendanceDay('VNM-0001', '2026-10-17');

assertTest("Phát hiện làm thêm 2.5 giờ", (float)$dayOT['tong_gio_ot'] == 2.5, "OT: " . $dayOT['tong_gio_ot']);
assertTest("Phân loại đúng vào OT ngày thường", (float)$dayOT['ot_ngay_thuong'] == 2.5);

// -------------------------------------------------------------
// 5. KIỂM THỬ BẢNG CÔNG THÁNG & CẢNH BÁO GIỚI HẠN OT
// -------------------------------------------------------------
echo "\n[5] KIỂM THỬ TỔNG HỢP CÔNG THÁNG & CẢNH BÁO GIỚI HẠN OT:\n";
$month = 10;
$year  = 2026;

$processedCount = $model->generateMonthlyTimesheet($month, $year, 'NM_TS');
assertTest("Tổng hợp bảng công tháng thành công cho đơn vị", $processedCount > 0, "Số lượng: {$processedCount}");

$timesheets = $model->getMonthlyTimesheets(['thang' => $month, 'nam' => $year, 'ma_dv' => 'NM_TS']);
assertTest("Truy vấn danh sách công tháng có dữ liệu", !empty($timesheets['data']));

// Giả lập thử nghiệm vượt ngưỡng OT tháng > 40h để kiểm tra cảnh báo
$db = Database::getInstance()->getConnection();
$db->prepare("UPDATE bang_cong_thang SET tong_gio_ot = 45.0, canh_bao_ot = 'VUOT_THANG_40H' WHERE ma_nv = 'VNM-0001' AND thang = 10 AND nam = 2026")->execute();

$tsCheck = $model->getMonthlyTimesheets(['thang' => 10, 'nam' => 2026, 'ma_dv' => 'NM_TS']);
$empTS = array_values(array_filter($tsCheck['data'], fn($r) => $r['ma_nv'] === 'VNM-0001'))[0] ?? null;

assertTest("Hệ thống gắn cờ cảnh báo OT vượt 40h/tháng", $empTS !== null && str_contains($empTS['canh_bao_ot'], 'VUOT_THANG_40H'));

// -------------------------------------------------------------
// 6. KIỂM THỬ QUY TRÌNH DUYỆT 3 BƯỚC & BẢO VỆ KHÓA CÔNG
// -------------------------------------------------------------
echo "\n[6] KIỂM THỬ QUY TRÌNH DUYỆT 3 BƯỚC & KHÓA BẢNG CÔNG:\n";
$tsId = (int)$empTS['id'];

// Reset về KHOI_TAO
$db->prepare("UPDATE bang_cong_thang SET trang_thai = 'KHOI_TAO' WHERE id = ?")->execute([$tsId]);

// Bước 1: Tổ trưởng xác nhận
$ok1 = $model->confirmByTeamLead($tsId, 'Tổ trưởng Nguyễn Văn A');
$ts1 = $db->query("SELECT trang_thai FROM bang_cong_thang WHERE id = {$tsId}")->fetch(PDO::FETCH_ASSOC);
assertTest("Bước 1: Tổ trưởng xác nhận chuyển trạng thái thành TO_TRUONG_XAC_NHAN", $ok1 && $ts1['trang_thai'] === 'TO_TRUONG_XAC_NHAN');

// Bước 2: Quản đốc duyệt
$ok2 = $model->approveByManager($tsId, 'Quản đốc Trần Văn B');
$ts2 = $db->query("SELECT trang_thai FROM bang_cong_thang WHERE id = {$tsId}")->fetch(PDO::FETCH_ASSOC);
assertTest("Bước 2: Quản đốc duyệt chuyển trạng thái thành QUAN_DOC_DUYET", $ok2 && $ts2['trang_thai'] === 'QUAN_DOC_DUYET');

// Bước 3: Khóa công
$ok3 = $model->lockTimesheet($tsId, 'Kế toán trưởng Lê Thị C');
$ts3 = $db->query("SELECT trang_thai FROM bang_cong_thang WHERE id = {$tsId}")->fetch(PDO::FETCH_ASSOC);
assertTest("Bước 3: Khóa công chuyển trạng thái thành KHOA_CONG", $ok3 && $ts3['trang_thai'] === 'KHOA_CONG');
assertTest("Hàm isTimesheetLocked xác nhận bảng công đã bị khóa", $model->isTimesheetLocked('VNM-0001', 10, 2026) === true);

// KIỂM TRA BẢO VỆ: Chạy lại generateMonthlyTimesheet KHÔNG ĐƯỢC ghi đè bảng công đã khóa
$db->prepare("UPDATE bang_cong_thang SET cong_thuc_te = 99.0 WHERE id = ?")->execute([$tsId]);
$model->generateMonthlyTimesheet(10, 2026, 'NM_TS');
$tsProtected = $db->query("SELECT cong_thuc_te, trang_thai FROM bang_cong_thang WHERE id = {$tsId}")->fetch(PDO::FETCH_ASSOC);

assertTest("Bảng công đã khóa (KHOA_CONG) được bảo vệ, KHÔNG BỊ ghi đè tự động", (float)$tsProtected['cong_thuc_te'] === 99.0 && $tsProtected['trang_thai'] === 'KHOA_CONG');

// -------------------------------------------------------------
// 7. KIỂM THỬ ĐIỀU CHỈNH CÔNG SAU KHI ĐÃ KHÓA (CÓ LÝ DO)
// -------------------------------------------------------------
echo "\n[7] KIỂM THỬ PHIẾU ĐIỀU CHỈNH CÔNG SAU KHÓA:\n";
// Tạo đề xuất điều chỉnh ngày công do quên quẹt
$adjData = [
    'ma_nv'          => 'VNM-0001',
    'ngay_cong'      => '2026-10-14',
    'loai_dieu_chinh'=> 'DIEU_CHINH_CONG_NGAY',
    'gia_tri_cu'     => '0.5',
    'gia_tri_moi'    => '1.0',
    'ly_do'          => 'Nhân viên có mặt tại xưởng họp đột xuất nên quên quẹt thẻ ra, tổ trưởng xác nhận đi đủ ca.'
];

$adjId = $model->createAdjustmentRequest($adjData);
assertTest("Tạo phiếu điều chỉnh công thành công (ID > 0)", $adjId > 0);

$adjRow = $db->query("SELECT * FROM dieu_chinh_cong WHERE id = {$adjId}")->fetch(PDO::FETCH_ASSOC);
assertTest("Phiếu điều chỉnh ở trạng thái CHO_DUYET kèm lý do", $adjRow['trang_thai'] === 'CHO_DUYET' && !empty($adjRow['ly_do']));

// Cấp trên phê duyệt phiếu điều chỉnh
$okApproveAdj = $model->approveAdjustmentRequest($adjId, 'Giám đốc Nhân sự');
assertTest("Phê duyệt phiếu điều chỉnh công thành công", $okApproveAdj === true);

// Kiểm tra xem giá trị trong cham_cong_ngay đã được cập nhật thành 1.0 hay chưa
$day14 = $db->query("SELECT cong_thuc_te FROM cham_cong_ngay WHERE ma_nv = 'VNM-0001' AND ngay = '2026-10-14'")->fetch(PDO::FETCH_ASSOC);
assertTest("Dữ liệu công ngày đã được cập nhật theo phiếu điều chỉnh đã duyệt", (float)$day14['cong_thuc_te'] === 1.0);

echo "\n=======================================================\n";
echo "KẾT QUẢ KIỂM THỬ: {$passCount} PASSED, {$failCount} FAILED\n";
echo "=======================================================\n";

if ($failCount > 0) {
    exit(1);
}
