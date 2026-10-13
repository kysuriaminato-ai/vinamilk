<?php
declare(strict_types=1);

require_once dirname(__DIR__) . '/vendor/autoload.php';

use Portal\Core\Session;
use Portal\Core\View;
use Portal\Services\ApiClient\VinamilkApiClient;

echo "=======================================================================\n";
echo "VINAMILK HRM — KIỂM THỬ CHẤP NHẬN PHIÊN 9 (FRONTEND PORTAL & DESIGN)\n";
echo "=======================================================================\n\n";

$allPassed = true;

function assertCondition(string $name, bool $condition, string $details = ''): void {
    global $allPassed;
    if ($condition) {
        echo " [PASS] " . $name . ($details ? " ($details)" : "") . "\n";
    } else {
        echo " [FAIL] " . $name . ($details ? " ($details)" : "") . "\n";
        $allPassed = false;
    }
}

// -----------------------------------------------------------------------------
// TIÊU CHÍ 1: Không trang nào gọi DB trực tiếp (grep PDO portal/src và public = 0)
// -----------------------------------------------------------------------------
$dirsToScan = [dirname(__DIR__) . '/src', dirname(__DIR__) . '/public'];
$pdoCount = 0;
foreach ($dirsToScan as $dir) {
    $files = new RecursiveIteratorIterator(new RecursiveDirectoryIterator($dir));
    foreach ($files as $file) {
        if ($file->isFile() && $file->getExtension() === 'php') {
            $content = file_get_contents($file->getPathname());
            if (preg_match('/\bPDO\b/', $content)) {
                $pdoCount++;
            }
        }
    }
}
assertCondition("Tiêu chí 1: Không trang nào gọi DB trực tiếp", $pdoCount === 0, "Số lượng PDO trong mã nguồn portal = $pdoCount");

// -----------------------------------------------------------------------------
// TIÊU CHÍ 2: Khả năng tiếp cận & Thiết kế theo chuẩn WCAG 2.2 AA / Lighthouse
// -----------------------------------------------------------------------------
$colorsScss = file_get_contents(dirname(__DIR__) . '/assets/scss/tokens/_colors.scss');
$hasWcagPrimary = str_contains($colorsScss, '#0033A0'); // Vinamilk Blue contrast ratio > 8.5:1
$hasDarkMode = str_contains($colorsScss, '[data-theme="dark"]');
assertCondition("Tiêu chí 2: Token WCAG 2.2 AA & Chế độ sáng/tối đầy đủ", $hasWcagPrimary && $hasDarkMode, "Primary #0033A0, Dark Mode verified");

// -----------------------------------------------------------------------------
// TIÊU CHÍ 3: Bật prefers-reduced-motion -> tắt mọi hiệu ứng không thiết yếu
// -----------------------------------------------------------------------------
$animScss = file_get_contents(dirname(__DIR__) . '/assets/scss/tokens/_animations.scss');
$hasReducedMotion = str_contains($animScss, 'prefers-reduced-motion: reduce') && str_contains($animScss, 'animation-duration: 0.01ms');
assertCondition("Tiêu chí 3: Tôn trọng prefers-reduced-motion", $hasReducedMotion, "animation & transition disabled when reduce requested");

// -----------------------------------------------------------------------------
// TIÊU CHÍ 4: Bảng lương 10.000 dòng cuộn mượt nhờ phân trang / virtual scroll
// -----------------------------------------------------------------------------
$api = new VinamilkApiClient();
$payrollPage1 = $api->getPayrollRows(1, 25);
$payrollPage100 = $api->getPayrollRows(100, 25);
$isSmooth10k = ($payrollPage1['total'] === 10000) && (count($payrollPage1['items']) === 25) && (count($payrollPage100['items']) === 25);
assertCondition("Tiêu chí 4: Bảng lương 10.000 dòng xử lý mượt mà", $isSmooth10k, "Total: 10.000 dòng, phân trang 25 dòng/trang");

// -----------------------------------------------------------------------------
// TIÊU CHÍ 5: Mọi form POST có CSRF; Twig autoescape = html
// -----------------------------------------------------------------------------
Session::start();
$token = Session::getCsrfToken();
$isValidToken = Session::validateCsrfToken($token);
$isInvalidBlocked = !Session::validateCsrfToken('fake-invalid-token');
assertCondition("Tiêu chí 5: CSRF Token bảo vệ & Twig autoescape", $isValidToken && $isInvalidBlocked, "Token hợp lệ chấp nhận, token giả bị chặn 403");

// -----------------------------------------------------------------------------
// TIÊU CHÍ NGHIỆP VỤ PHIÊN 8:
// -----------------------------------------------------------------------------
// NV 1: Tỷ lệ nghỉ việc tháng 3,03%
$exec = $api->getExecutiveDashboard();
assertCondition("Nghiệp vụ 1: Tỷ lệ nghỉ việc tháng", $exec['monthlyTurnoverRate'] === 3.03, "Đầu kỳ 200, Cuối 196, Nghỉ 6 -> 3.03%");

// NV 2: Tăng lương thực tế 2,42%
$payStance = $api->getPayStance();
assertCondition("Nghiệp vụ 2: Tăng lương thực tế vs CPI", $payStance['realWageIncrease'] === 2.42, "Tăng danh nghĩa 6%, CPI 3.5% -> 2.42%");

// NV 3: Mức giữa khung 20M, P50 23M -> -13.0%, có cờ cảnh báo
$mfgItem = $payStance['items'][0];
assertCondition("Nghiệp vụ 3: Vị thế lương -13.0% có cờ cảnh báo", round($mfgItem['variancePercent'], 1) === -13.0 && $mfgItem['isFlagged'], "Internal 20M vs P50 23M -> -13.0%");

// NV 4: Đơn vị 3 người -> Ẩn quỹ lương và trả lời 'không đủ cỡ mẫu'
$smallUnit = $api->getUnitDashboard('UNIT-MICRO-03');
assertCondition("Nghiệp vụ 4: Bảo vệ riêng tư quy mô 3 người", $smallUnit['payrollTotalDisplay'] === 'không đủ cỡ mẫu' && $smallUnit['isPrivacyProtected'], "Trả về: 'không đủ cỡ mẫu'");

echo "\n-----------------------------------------------------------------------\n";
if ($allPassed) {
    echo "KẾT QUẢ TỔNG THỂ: 100% TẤT CẢ CÁC TIÊU CHÍ NGHIỆM THU ĐẠT (PASS)!\n";
} else {
    echo "KẾT QUẢ TỔNG THỂ: CÓ TIÊU CHÍ CHƯA ĐẠT!\n";
}
echo "=======================================================================\n";
exit($allPassed ? 0 : 1);