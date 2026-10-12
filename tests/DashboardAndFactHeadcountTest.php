<?php
/**
 * =====================================================================
 * VINAMILK HRM - TEST SUITE: FACT HEADCOUNT MONTHLY & HR ANALYTICS DASHBOARD
 * =====================================================================
 * Kiểm thử:
 * 1. Bảng fact_headcount_monthly và view v_fact_headcount_summary
 * 2. Job snapshot cuối tháng & tính toán các chỉ số
 * 3. 6 Thẻ KPI & So sánh cùng kỳ MoM
 * 4. 5 Biểu đồ chuyên sâu Chart.js 4
 * 5. 3 Danh sách hành động (HĐ 60 ngày, Chứng chỉ ATTP, Thiếu định biên > 10%)
 * 6. Drill-down danh sách nhân viên cấu thành
 * 7. Phân quyền scopeByUnit() giữa các đơn vị
 */

define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/services/HeadcountSnapshotService.php';

$pdo = Database::getInstance()->getConnection();
$service = new HeadcountSnapshotService($pdo);

$passed = 0;
$failed = 0;

function assertCondition(bool $cond, string $msg) {
    global $passed, $failed;
    if ($cond) {
        echo " [PASS] $msg\n";
        $passed++;
    } else {
        echo " [FAIL] $msg\n";
        $failed++;
    }
}

echo "======================================================================\n";
echo "BẮT ĐẦU KIỂM THỬ: FACT HEADCOUNT MONTHLY & HR ANALYTICS DASHBOARD\n";
echo "======================================================================\n\n";

// ---------------------------------------------------------------------
// [I] KIỂM THỬ BẢNG FACT & VIEW TỔNG HỢP
// ---------------------------------------------------------------------
echo "[I] KIỂM THỬ BẢNG FACT & VIEW TỔNG HỢP:\n";
$factExists = $pdo->query("SHOW TABLES LIKE 'fact_headcount_monthly'")->rowCount() > 0;
assertCondition($factExists, "Bảng fact_headcount_monthly tồn tại trong CSDL");

$viewExists = $pdo->query("SHOW TABLES LIKE 'v_fact_headcount_summary'")->rowCount() > 0;
assertCondition($viewExists, "View v_fact_headcount_summary tồn tại trong CSDL");

$factCols = $pdo->query("DESCRIBE fact_headcount_monthly")->fetchAll(PDO::FETCH_COLUMN);
$requiredCols = [
    'ky_nam_thang', 'ngay_snapshot', 'ma_dv', 'headcount_dau_ky', 'tuyen_moi', 'nghi_viec',
    'headcount_cuoi_ky', 'headcount_binh_quan', 'dinh_bien', 'ty_le_lap_day', 'tong_quy_luong',
    'tong_gio_ot', 'gio_ot_binh_quan', 'so_ngay_vang_mat', 'ty_le_vang_mat', 'nghi_viec_12t',
    'headcount_avg_12t', 'ty_le_nghi_viec_12t'
];
foreach ($requiredCols as $c) {
    assertCondition(in_array($c, $factCols), "Cột $c tồn tại trong fact_headcount_monthly");
}

// ---------------------------------------------------------------------
// [II] KIỂM THỬ CHẠY SNAPSHOT CHO KỲ 2026-10
// ---------------------------------------------------------------------
echo "\n[II] KIỂM THỬ SERVICE SNAPSHOT CUỐI THÁNG:\n";
$snapRes = $service->takeSnapshot('2026-10', true);
assertCondition(!empty($snapRes['period']) && $snapRes['period'] === '2026-10', "Chốt snapshot kỳ 2026-10 thành công");
assertCondition($snapRes['total_units'] > 0, "Đã snapshot cho {$snapRes['total_units']} đơn vị");

// Kiểm tra bản ghi của Nhà máy Bình Dương (NM_BD)
$nmBd = $pdo->query("SELECT * FROM fact_headcount_monthly WHERE ky_nam_thang = '2026-10' AND ma_dv = 'NM_BD'")->fetch(PDO::FETCH_ASSOC);
assertCondition(!empty($nmBd), "Có bản ghi snapshot của Nhà máy Bình Dương (NM_BD)");
assertCondition((int)$nmBd['headcount_cuoi_ky'] > 0, "Headcount cuối kỳ NM_BD: {$nmBd['headcount_cuoi_ky']} người");
assertCondition((float)$nmBd['ty_le_lap_day'] > 0, "Tỷ lệ lấp đầy định biên NM_BD: {$nmBd['ty_le_lap_day']}%");

// ---------------------------------------------------------------------
// [III] KIỂM THỬ 6 THẺ KPI TỔNG HỢP & SO SÁNH CÙNG KỲ (MOM)
// ---------------------------------------------------------------------
echo "\n[III] KIỂM THỬ 6 THẺ KPI & SO SÁNH CÙNG KỲ (MoM):\n";
$kpiData = $service->getKpiCardsData('2026-10', 'ALL', []);

assertCondition(isset($kpiData['headcount']['value']) && $kpiData['headcount']['value'] > 0, 
    "Thẻ 1: Headcount hiện diện = " . $kpiData['headcount']['value'] . " người");
assertCondition(isset($kpiData['headcount']['diff']), 
    "Thẻ 1: Có chênh lệch MoM = " . $kpiData['headcount']['diff'] . " người (" . $kpiData['headcount']['diff_pct'] . "%)");

assertCondition(isset($kpiData['turnover_12m']['value']), 
    "Thẻ 2: Tỷ lệ nghỉ việc 12M trượt = " . $kpiData['turnover_12m']['value'] . "%");
assertCondition(isset($kpiData['turnover_12m']['diff']), 
    "Thẻ 2: Có so sánh chênh lệch MoM = " . $kpiData['turnover_12m']['diff'] . "% điểm");

assertCondition(isset($kpiData['absenteeism']['value']), 
    "Thẻ 3: Tỷ lệ vắng mặt = " . $kpiData['absenteeism']['value'] . "%");
assertCondition(isset($kpiData['absenteeism']['diff']), 
    "Thẻ 3: Có so sánh chênh lệch MoM = " . $kpiData['absenteeism']['diff'] . "% điểm");

assertCondition(isset($kpiData['overtime_avg']['value']), 
    "Thẻ 4: Giờ OT bình quân = " . $kpiData['overtime_avg']['value'] . " giờ/người");
assertCondition(isset($kpiData['overtime_avg']['diff']), 
    "Thẻ 4: Có so sánh chênh lệch MoM = " . $kpiData['overtime_avg']['diff'] . "h");

assertCondition(isset($kpiData['payroll_fund']['value']) && $kpiData['payroll_fund']['value'] > 0, 
    "Thẻ 5: Quỹ lương tháng = " . number_format($kpiData['payroll_fund']['value']) . " đ");
assertCondition(isset($kpiData['payroll_fund']['diff_pct']), 
    "Thẻ 5: Có tỷ lệ biến động quỹ lương MoM = " . $kpiData['payroll_fund']['diff_pct'] . "%");

assertCondition(isset($kpiData['staffing_fill']['value']) && $kpiData['staffing_fill']['value'] > 0, 
    "Thẻ 6: Tỷ lệ lấp đầy định biên = " . $kpiData['staffing_fill']['value'] . "%");
assertCondition(isset($kpiData['staffing_fill']['diff']), 
    "Thẻ 6: Có so sánh biến động lấp đầy MoM = " . $kpiData['staffing_fill']['diff'] . "%");

// ---------------------------------------------------------------------
// [IV] KIỂM THỬ 5 BIỂU ĐỒ CHUYÊN SÂU CHART.JS 4
// ---------------------------------------------------------------------
echo "\n[IV] KIỂM THỬ 5 BIỂU ĐỒ CHUYÊN SÂU (CHART.JS 4):\n";

// Biểu đồ 1: Headcount & Nghỉ việc 12 tháng trượt
$trend12m = $service->get12MonthsTrend('2026-10', 'ALL', []);
assertCondition(count($trend12m['labels']) === 12, "Biểu đồ 1: Có đúng 12 mốc tháng (12 tháng trượt)");
assertCondition(count($trend12m['headcount']) === 12, "Biểu đồ 1: Có đầy đủ 12 điểm dữ liệu Headcount");
assertCondition(count($trend12m['turnover']) === 12, "Biểu đồ 1: Có đầy đủ 12 điểm dữ liệu Nghỉ việc");

// Biểu đồ 2: Cơ cấu theo Khối
$blocks = $service->getHeadcountByBlock('ALL', []);
assertCondition(!empty($blocks['labels']) && count($blocks['labels']) >= 3, 
    "Biểu đồ 2: Cơ cấu theo Khối có " . count($blocks['labels']) . " khối chức năng (" . implode(', ', $blocks['labels']) . ")");

// Biểu đồ 3: Xếp hạng giờ OT theo Nhà máy (Cột ngang)
$otRanking = $service->getOvertimeRankingByFactory('2026-10', []);
assertCondition(!empty($otRanking['labels']), "Biểu đồ 3: Có danh sách nhà máy xếp hạng OT (" . count($otRanking['labels']) . " đơn vị)");
assertCondition($otRanking['data'][0] >= end($otRanking['data']), "Biểu đồ 3: Dữ liệu được sắp xếp giảm dần chính xác theo giờ OT");

// Biểu đồ 4: Tháp tuổi - Giới tính (Age-Gender Pyramid)
$pyramid = $service->getAgeGenderPyramid('ALL', []);
assertCondition(count($pyramid['labels']) === 5, "Biểu đồ 4: Tháp tuổi có 5 nhóm tuổi (<25, 25-34, 35-44, 45-54, >=55)");
assertCondition(count($pyramid['male']) === 5 && count($pyramid['female']) === 5, "Biểu đồ 4: Có đủ 2 trục Nam và Nữ");
$hasNegativeMale = false;
foreach ($pyramid['male'] as $m) {
    if ($m <= 0) $hasNegativeMale = true;
}
assertCondition($hasNegativeMale, "Biểu đồ 4: Cột Nam được chuẩn hóa giá trị âm đối xứng chuẩn Pyramid");

// Biểu đồ 5: Phân bổ thâm niên công tác
$tenure = $service->getTenureDistribution('ALL', []);
assertCondition(count($tenure['labels']) === 5, "Biểu đồ 5: Có 5 nhóm thâm niên (<1 năm, 1-3 năm, 3-5 năm, 5-10 năm, >10 năm)");
assertCondition(array_sum($tenure['data']) > 0, "Biểu đồ 5: Tổng số nhân sự phân bổ = " . array_sum($tenure['data']));

// ---------------------------------------------------------------------
// [V] KIỂM THỬ 3 DANH SÁCH HÀNH ĐỘNG CẦN XỬ LÝ (ACTION CARDS)
// ---------------------------------------------------------------------
echo "\n[V] KIỂM THỬ 3 DANH SÁCH HÀNH ĐỘNG (ACTION CARDS):\n";

// 1. HĐ hết hạn trong 60 ngày
$expContracts = $service->getExpiringContracts(60, 'ALL', []);
assertCondition(is_array($expContracts), "Danh sách 1: Truy vấn HĐ hết hạn trong 60 ngày thành công (Tìm thấy " . count($expContracts) . " HĐ)");
if (!empty($expContracts)) {
    assertCondition(isset($expContracts[0]['SoNgayConLai']) && $expContracts[0]['SoNgayConLai'] <= 60, 
        "Danh sách 1: Số ngày còn lại hợp lệ (" . $expContracts[0]['SoNgayConLai'] . " ngày)");
}

// 2. Chứng chỉ ATTP sắp hết hạn (60 ngày)
$expCerts = $service->getExpiringCertificates(60, 'ALL', []);
assertCondition(is_array($expCerts), "Danh sách 2: Truy vấn chứng chỉ ATTP sắp hết hạn thành công (Tìm thấy " . count($expCerts) . " chứng chỉ)");
if (!empty($expCerts)) {
    assertCondition(!empty($expCerts[0]['ten_chung_chi']), "Danh sách 2: Có tên chứng chỉ (" . $expCerts[0]['ten_chung_chi'] . ")");
}

// 3. Đơn vị thiếu định biên > 10%
$understaffed = $service->getUnderstaffedUnits(0.10, 'ALL', []);
assertCondition(is_array($understaffed), "Danh sách 3: Truy vấn đơn vị thiếu định biên > 10% thành công (Tìm thấy " . count($understaffed) . " đơn vị)");
if (!empty($understaffed)) {
    assertCondition($understaffed[0]['TyLeThieu'] >= 10.0, 
        "Danh sách 3: Đơn vị " . $understaffed[0]['TenDV'] . " thiếu " . $understaffed[0]['TyLeThieu'] . "% định biên");
}

// ---------------------------------------------------------------------
// [VI] KIỂM THỬ DRILL-DOWN: XEM CHI TIẾT NHÂN VIÊN CẤU THÀNH
// ---------------------------------------------------------------------
echo "\n[VI] KIỂM THỬ DRILL-DOWN DANH SÁCH NHÂN SỰ CẤU THÀNH:\n";

$drillHc = $service->getDrilldownEmployees('headcount', '2026-10', 'ALL', []);
assertCondition(count($drillHc) > 0, "Drill-down Headcount: Trả về " . count($drillHc) . " nhân sự đang làm việc");
assertCondition(isset($drillHc[0]['MaNV']) && isset($drillHc[0]['HoTen']), "Drill-down có đầy đủ thông tin MaNV, HoTen");

$drillOt = $service->getDrilldownEmployees('overtime_avg', '2026-10', 'ALL', []);
assertCondition(is_array($drillOt), "Drill-down Overtime: Truy vấn thành công nhân sự có giờ OT");

$drillAbs = $service->getDrilldownEmployees('absenteeism', '2026-10', 'ALL', []);
assertCondition(is_array($drillAbs), "Drill-down Absenteeism: Truy vấn thành công nhân sự có ngày vắng mặt");

// ---------------------------------------------------------------------
// [VII] KIỂM THỬ PHÂN QUYỀN SCOPE BY UNIT
// ---------------------------------------------------------------------
echo "\n[VII] KIỂM THỬ SCOPE BY UNIT TRONG DASHBOARD:\n";

// Giả lập HR Nhà máy Tiên Sơn (NM_TS): chỉ được phép xem các đơn vị con của NM_TS
$tsUnits = $service->getSubUnitCodes('NM_TS');
assertCondition(in_array('NM_TS', $tsUnits), "Cây đơn vị Tiên Sơn có mã NM_TS");
assertCondition(!in_array('NM_BD', $tsUnits), "Cây đơn vị Tiên Sơn TUYỆT ĐỐI KHÔNG chứa Nhà máy Bình Dương (NM_BD)");

$kpiTienson = $service->getKpiCardsData('2026-10', 'NM_TS', $tsUnits);
assertCondition($kpiTienson['headcount']['value'] <= $kpiData['headcount']['value'], 
    "Headcount Tiên Sơn ({$kpiTienson['headcount']['value']}) nhỏ hơn hoặc bằng Headcount Toàn tập đoàn ({$kpiData['headcount']['value']})");

$drillTienson = $service->getDrilldownEmployees('headcount', '2026-10', 'NM_TS', $tsUnits);
$hasBdEmployee = false;
foreach ($drillTienson as $emp) {
    if (strpos($emp['TenDV'] ?? '', 'Bình Dương') !== false) {
        $hasBdEmployee = true;
    }
}
assertCondition(!$hasBdEmployee, "Drill-down Tiên Sơn TUYỆT ĐỐI KHÔNG chứa nhân sự của Nhà máy Bình Dương");

echo "\n======================================================================\n";
echo "TỔNG KẾT KIỂM THỬ DASHBOARD & FACT HEADCOUNT: $passed PASSED, $failed FAILED\n";
echo "======================================================================\n";

if ($failed === 0) {
    exit(0);
} else {
    exit(1);
}
