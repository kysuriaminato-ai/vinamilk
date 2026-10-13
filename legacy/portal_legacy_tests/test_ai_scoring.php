<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/models/AiExpertModel.php';

$model = new AiExpertModel();
$count = $model->scoreAllEmployees('2026-10');

echo "Scored $count employees into ai_risk_score successfully!\n\n";

$top = $model->getTopRiskyEmployees('2026-10', 5);
echo "=== TOP 5 RISKY EMPLOYEES ===\n";
foreach ($top as $idx => $emp) {
    echo ($idx + 1) . ". {$emp['HoTen']} ({$emp['ma_nv']}) - Risk: " . round($emp['xac_suat_nghi_viec'] * 100, 1) . "% [{$emp['muc_do_rui_ro']}]\n";
    echo "   - Lý do 1: {$emp['ly_do_1']}\n";
    echo "   - Lý do 2: {$emp['ly_do_2']}\n";
    echo "   - Lý do 3: {$emp['ly_do_3']}\n";
    echo "   - Khuyến nghị: {$emp['khuyen_nghi_hanh_dong']}\n\n";
}

$forecast = $model->getCapacityForecast('ALL', 3000000, 20000, 2026);
echo "=== WORKFORCE CAPACITY FORECAST ===\n";
echo "   Kế hoạch công suất: " . number_format($forecast['ke_hoach_cong_suat']) . " sản phẩm/tháng\n";
echo "   Năng suất định mức: " . number_format($forecast['nang_suat_nguoi']) . " sp/người\n";
echo "   Định biên gộp: {$forecast['dinh_bien_gop']} người\n";
echo "   Headcount hiện có: {$forecast['headcount_hien_co']} người\n";
echo "   Dự báo thôi việc (từ mô hình AI): {$forecast['du_bao_thoi_viec']} người\n";
echo "   Dự báo nghỉ hưu (lộ trình BLLĐ 2019): {$forecast['du_bao_nghi_huu']} người\n";
echo "   Lao động khả dụng ròng: {$forecast['lao_dong_kha_dung']} người\n";
echo "   Nhu cầu tuyển dụng bổ sung: {$forecast['nhu_cau_tuyen_dung']} người\n";
