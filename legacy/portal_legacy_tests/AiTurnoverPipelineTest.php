<?php
/**
 * =====================================================================
 * VINAMILK HRM - TEST SUITE: AI TURNOVER PREDICTION PIPELINE & CAPACITY FORECAST
 * =====================================================================
 */

define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/models/AiExpertModel.php';

$pdo = Database::getInstance()->getConnection();
$aiModel = new AiExpertModel();

$passed = 0;
$failed = 0;

function assertCheck(bool $condition, string $message) {
    global $passed, $failed;
    if ($condition) {
        echo " [PASS] $message\n";
        $passed++;
    } else {
        echo " [FAIL] $message\n";
        $failed++;
    }
}

echo "======================================================================\n";
echo "BẮT ĐẦU KIỂM THỬ: AI TURNOVER PIPELINE & WORKFORCE CAPACITY PLANNING\n";
echo "======================================================================\n\n";

// ---------------------------------------------------------------------
// [I] KIỂM THỬ XÓA BỎ HOÀN TOÀN rand() TRONG CÁC MODEL AI
// ---------------------------------------------------------------------
echo "[I] KIỂM THỬ LOẠI BỎ rand() TRONG AI MODELS:\n";
$appModelContent = file_get_contents(ROOT_DIR . '/app/models/AiExpertModel.php');
$legacyModelContent = file_get_contents(ROOT_DIR . '/legacy/app/models/AiExpertModel.php');

assertCheck(!str_contains($appModelContent, 'rand(') && !str_contains($appModelContent, 'mt_rand('), 
    "app/models/AiExpertModel.php TUYỆT ĐỐI KHÔNG chứa rand() hay mt_rand()");
assertCheck(!str_contains($legacyModelContent, 'rand('), 
    "legacy/app/models/AiExpertModel.php đã xóa bỏ hoàn toàn rand()");

// ---------------------------------------------------------------------
// [II] KIỂM THỬ FILE SCRIPT SYNTHETIC GENERATOR & TRAINER
// ---------------------------------------------------------------------
echo "\n[II] KIỂM THỬ SCRIPTS PYTHON & DATA PIPELINE:\n";
$genPy = ROOT_DIR . '/scripts/gen_synthetic.py';
$trainPy = ROOT_DIR . '/scripts/train_turnover.py';
$csvData = ROOT_DIR . '/data/synthetic_turnover_24m.csv';
$jsonModel = ROOT_DIR . '/storage/models/turnover_model.json';

assertCheck(file_exists($genPy), "scripts/gen_synthetic.py tồn tại");
assertCheck(file_exists($trainPy), "scripts/train_turnover.py tồn tại");
assertCheck(file_exists($csvData), "File dữ liệu giả lập data/synthetic_turnover_24m.csv tồn tại");
assertCheck(file_exists($jsonModel), "File mô hình JSON storage/models/turnover_model.json tồn tại");

// Đọc kiểm tra dữ liệu CSV
$csvLines = file($csvData);
$header = trim($csvLines[0]);
$expectedCols = ['employee_id', 'month_idx', 'period', 'tham_nien', 'tuoi', 'khoi', 'ot_3_thang', 'so_ngay_nghi', 'thang_tu_tang_luong', 'diem_kpi', 'so_lan_thuyen_chuyen', 'turnover'];
$hasAllCols = true;
foreach ($expectedCols as $col) {
    if (!str_contains($header, $col)) $hasAllCols = false;
}
assertCheck($hasAllCols, "File CSV có đầy đủ 12 cột đặc trưng quy định");
assertCheck(count($csvLines) > 10000, "File CSV có " . number_format(count($csvLines) - 1) . " bản ghi employee-month");

// ---------------------------------------------------------------------
// [III] KIỂM THỬ CẤU TRÚC MODEL JSON ĐÃ XUẤT
// ---------------------------------------------------------------------
echo "\n[III] KIỂM THỬ MODEL JSON STORAGE & EVALUATION METRICS:\n";
$modelData = json_decode(file_get_contents($jsonModel), true);

assertCheck(!empty($modelData['model_name']), "Mô hình có định danh: " . ($modelData['model_name'] ?? 'N/A'));
assertCheck(!empty($modelData['disclaimer']) && str_contains($modelData['disclaimer'], 'giả lập'), 
    "Mô hình có tuyên bố bắt buộc: '" . ($modelData['disclaimer'] ?? '') . "'");

$eval = $modelData['evaluation_metrics'];
assertCheck(isset($eval['logistic_regression']['auc']) && $eval['logistic_regression']['auc'] > 0.60, 
    "Logistic Regression AUC = " . ($eval['logistic_regression']['auc'] ?? 0));
assertCheck(isset($eval['logistic_regression']['precision_at_top10']), 
    "Logistic Regression Precision@Top10% = " . ($eval['logistic_regression']['precision_at_top10'] ?? 0));

assertCheck(isset($eval['random_forest']['auc']) && $eval['random_forest']['auc'] > 0.60, 
    "Random Forest AUC = " . ($eval['random_forest']['auc'] ?? 0));
assertCheck(!empty($eval['random_forest']['feature_importances']), 
    "Random Forest có Feature Importances cho 8 đặc trưng");

$logModel = $modelData['logistic_model'];
assertCheck(isset($logModel['intercept']), "Logistic Model có hệ số chặn (intercept) = " . $logModel['intercept']);
$reqFeatures = ['tham_nien', 'tuoi', 'khoi_idx', 'ot_3_thang', 'so_ngay_nghi', 'thang_tu_tang_luong', 'diem_kpi', 'so_lan_thuyen_chuyen'];
$hasAllWeights = true;
foreach ($reqFeatures as $f) {
    if (!isset($logModel['coefficients'][$f])) $hasAllWeights = false;
}
assertCheck($hasAllWeights, "Logistic Model có đủ trọng số beta cho 8 đặc trưng cốt lõi");

// ---------------------------------------------------------------------
// [IV] KIỂM THỬ CHẤM ĐIỂM AI VÀ BẢNG ai_risk_score
// ---------------------------------------------------------------------
echo "\n[IV] KIỂM THỬ CHẤM ĐIỂM VÀO BẢNG ai_risk_score:\n";
$scoreCount = $aiModel->scoreAllEmployees('2026-10');
assertCheck($scoreCount > 0, "Đã chấm điểm thành công cho $scoreCount nhân sự vào bảng ai_risk_score");

$dbCount = (int)$pdo->query("SELECT COUNT(*) FROM ai_risk_score WHERE ky_danh_gia = '2026-10'")->fetchColumn();
assertCheck($dbCount === $scoreCount, "Bảng ai_risk_score lưu trữ khớp chính xác $dbCount bản ghi");

// ---------------------------------------------------------------------
// [V] KIỂM THỬ TOP NHÂN SỰ RỦI RO, 3 LÝ DO & KHUYẾN NGHỊ HÀNH ĐỘNG
// ---------------------------------------------------------------------
echo "\n[V] KIỂM THỬ TOP NHÂN SỰ RỦI RO, 3 LÝ DO VÀ KHUYẾN NGHỊ:\n";
$topRisky = $aiModel->getTopRiskyEmployees('2026-10', 10);
assertCheck(!empty($topRisky), "Lấy danh sách Top nhân sự rủi ro thành công (" . count($topRisky) . " nhân sự)");

$first = $topRisky[0];
assertCheck((float)$first['xac_suat_nghi_viec'] > 0.0, "Xác suất rủi ro nhân sự #1 = " . round($first['xac_suat_nghi_viec'] * 100, 1) . "%");
assertCheck(!empty($first['muc_do_rui_ro']), "Mức độ rủi ro hợp lệ: " . $first['muc_do_rui_ro']);
assertCheck(!empty($first['ly_do_1']) && !empty($first['ly_do_2']) && !empty($first['ly_do_3']), 
    "Có đầy đủ 3 lý do chính đóng góp rủi ro:\n      * Lý do 1: {$first['ly_do_1']}\n      * Lý do 2: {$first['ly_do_2']}\n      * Lý do 3: {$first['ly_do_3']}");
assertCheck(!empty($first['khuyen_nghi_hanh_dong']), 
    "Có khuyến nghị hành động giữ chân cụ thể: {$first['khuyen_nghi_hanh_dong']}");

// ---------------------------------------------------------------------
// [VI] KIỂM THỬ DỰ BÁO ĐỊNH BIÊN & LỘ TRÌNH NGHỈ HƯU BLLĐ 2019
// ---------------------------------------------------------------------
echo "\n[VI] KIỂM THỬ DỰ BÁO ĐỊNH BIÊN & LỘ TRÌNH TUỔI NGHỈ HƯU BLLĐ 2019:\n";
$forecast = $aiModel->getCapacityForecast('ALL', 3000000, 20000, 2026);

// Kiểm tra công thức: Định biên gộp = Công suất / Năng suất
$expectedGross = (int)ceil(3000000 / 20000);
assertCheck($forecast['dinh_bien_gop'] === $expectedGross, "Định biên gộp đúng công thức (3tr / 20k = 150 người)");

assertCheck(isset($forecast['du_bao_thoi_viec']), "Có số lượng dự báo thôi việc từ AI = " . $forecast['du_bao_thoi_viec']);
assertCheck(isset($forecast['du_bao_nghi_huu']), "Có số lượng dự báo nghỉ hưu BLLĐ 2019 = " . $forecast['du_bao_nghi_huu']);
assertCheck(isset($forecast['nhu_cau_tuyen_dung']), "Có nhu cầu tuyển dụng bổ sung = " . $forecast['nhu_cau_tuyen_dung']);

// Kiểm tra lộ trình tuổi nghỉ hưu BLLĐ 2019
$retirees = $aiModel->getRetirementEligibleEmployees('ALL', 2026);
assertCheck(is_array($retirees), "Truy vấn danh sách nhân sự chạm tuổi nghỉ hưu thành công");
assertCheck($forecast['chuan_tuoi_nam'] === '61 tuổi 6 tháng', "Chuẩn tuổi nghỉ hưu Nam năm 2026 đúng quy định: 61 tuổi 6 tháng");
assertCheck($forecast['chuan_tuoi_nu'] === '57 tuổi 0 tháng', "Chuẩn tuổi nghỉ hưu Nữ năm 2026 đúng quy định: 57 tuổi 0 tháng");

echo "\n======================================================================\n";
echo "TỔNG KẾT KIỂM THỬ AI PIPELINE: $passed PASSED, $failed FAILED\n";
echo "======================================================================\n";

if ($failed === 0) exit(0);
else exit(1);
