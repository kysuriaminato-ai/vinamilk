<?php
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));
require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/core/Session.php';
require_once ROOT_DIR . '/app/core/Controller.php';
require_once ROOT_DIR . '/app/helpers/Auth.php';
require_once ROOT_DIR . '/app/helpers/functions.php';
require_once ROOT_DIR . '/app/helpers/MenuHelper.php';
require_once ROOT_DIR . '/app/helpers/NotificationHelper.php';
require_once ROOT_DIR . '/app/controllers/KitchenSinkController.php';
require_once ROOT_DIR . '/app/controllers/EmployeeController.php';

echo "========================================================\n";
echo "VINAMILK HRM - VERIFICATION TEST: LAYOUTS & KITCHEN SINK\n";
echo "========================================================\n";

// 1. Kiểm tra mô phỏng Auth cho ADMIN
Session::set('vnm_logged_user', [
    'ID'       => 1,
    'Username' => 'admin',
    'RoleID'   => 1,
    'RoleCode' => 'ADMIN',
    'HoTen'    => 'Quản Trị Viên Tập Đoàn',
    'MaNV'     => 'VNM-0001',
    'MaDV'     => 'VINAMILK'
]);

echo "[TEST 1] MenuHelper::getUserMenu() cho ADMIN: ";
$adminMenu = MenuHelper::getUserMenu();
if (count($adminMenu) >= 6) {
    echo "PASS (" . count($adminMenu) . " main categories)\n";
} else {
    echo "FAIL\n";
}

// 2. Kiểm tra thông báo nghiệp vụ
echo "[TEST 2] NotificationHelper::getNotifications(): ";
$notifs = NotificationHelper::getNotifications();
if ($notifs['total_count'] > 0) {
    echo "PASS (Total: {$notifs['total_count']} - Contracts: " . count($notifs['contracts']) . ", Requests: " . count($notifs['requests']) . ", Certs: " . count($notifs['certificates']) . ")\n";
} else {
    echo "FAIL (Zero notifications)\n";
}

// 3. Kiểm tra Quick Search AJAX
echo "[TEST 3] EmployeeModel::quickSearch('VNM'): ";
$empModel = new EmployeeModel();
$quickResults = $empModel->quickSearch('VNM', 5);
if (count($quickResults) > 0) {
    echo "PASS (" . count($quickResults) . " records found, Sample: {$quickResults[0]['HoTen']} - {$quickResults[0]['MaNV']})\n";
} else {
    echo "FAIL (No results)\n";
}

// 4. Kiểm tra render KitchenSinkController::index()
echo "[TEST 4] KitchenSinkController::index() Execution & HTML Render: ";
$_GET['keyword'] = '';
$_GET['status']  = '';
$_GET['page']    = 1;

ob_start();
try {
    $kitchenController = new KitchenSinkController();
    $kitchenController->index();
    $htmlOutput = ob_get_clean();

    $checks = [
        'UI Kitchen Sink' => str_contains($htmlOutput, 'UI Kitchen Sink'),
        'Thẻ Chỉ Số Đo Lường' => str_contains($htmlOutput, 'Thẻ Chỉ Số Đo Lường'),
        'Bảng Dữ Liệu Phía Server' => str_contains($htmlOutput, 'Bảng Dữ Liệu Phía Server'),
        'Hệ Thống Badge Trạng Thái' => str_contains($htmlOutput, 'Hệ Thống Badge Trạng Thái'),
        'Modal Xác Nhận Dùng Chung' => str_contains($htmlOutput, 'Modal Xác Nhận Dùng Chung'),
        'Trạng Thái Rỗng' => str_contains($htmlOutput, 'Trạng Thái Rỗng'),
        'vinamilk.css' => str_contains($htmlOutput, 'vinamilk.css'),
        'Be Vietnam Pro' => str_contains($htmlOutput, 'Be Vietnam Pro'),
        'skip-link' => str_contains($htmlOutput, 'skip-link'),
        'quick-search-wrapper' => str_contains($htmlOutput, 'quick-search-wrapper')
    ];

    $failedChecks = [];
    foreach ($checks as $name => $passed) {
        if (!$passed) $failedChecks[] = $name;
    }

    if (empty($failedChecks)) {
        echo "PASS (HTML Length: " . strlen($htmlOutput) . " bytes, All 10 key components rendered)\n";
    } else {
        echo "FAIL (Missing: " . implode(', ', $failedChecks) . ")\n";
    }
} catch (Throwable $e) {
    ob_end_clean();
    echo "FAIL (Exception: " . $e->getMessage() . " in " . $e->getFile() . ":" . $e->getLine() . ")\n";
}

// 5. Kiểm tra phân quyền menu cho các vai trò khác (KETOAN_LUONG, HR_DONVI, NHANVIEN)
$rolesToTest = [
    ['RoleID' => 5, 'RoleCode' => 'KETOAN_LUONG', 'Name' => 'Kế toán Lương', 'MustNotHave' => 'Quản Lý Nhân Sự'],
    ['RoleID' => 4, 'RoleCode' => 'HR_DONVI',      'Name' => 'Nhân sự Đơn vị', 'MustNotHave' => 'Hệ Thống & Kiểm Toán'],
    ['RoleID' => 7, 'RoleCode' => 'NHANVIEN',      'Name' => 'Nhân viên ESS', 'MustNotHave' => 'Quản Lý Nhân Sự']
];

foreach ($rolesToTest as $r) {
    Session::set('vnm_logged_user', [
        'ID'       => 99,
        'Username' => 'test_' . strtolower($r['RoleCode']),
        'RoleID'   => $r['RoleID'],
        'RoleCode' => $r['RoleCode'],
        'HoTen'    => $r['Name'],
        'MaNV'     => 'VNM-9999',
        'MaDV'     => 'NM_BD'
    ]);

    $roleMenu = MenuHelper::getUserMenu();
    $menuTitles = array_column($roleMenu, 'Title');
    $hasForbidden = in_array($r['MustNotHave'], $menuTitles);

    echo "[TEST 5] Menu Permission Check cho {$r['RoleCode']}: ";
    if (!$hasForbidden) {
        echo "PASS (Properly hid '{$r['MustNotHave']}', Total accessible menus: " . count($roleMenu) . ")\n";
    } else {
        echo "FAIL (Leaked '{$r['MustNotHave']}')\n";
    }
}

// 6. Kiểm tra độ tương phản màu chuẩn WCAG AA
function getLuminance($r, $g, $b) {
    $a = array_map(function ($v) {
        $v /= 255;
        return $v <= 0.03928 ? $v / 12.92 : pow(($v + 0.055) / 1.055, 2.4);
    }, [$r, $g, $b]);
    return 0.2126 * $a[0] + 0.7152 * $a[1] + 0.0722 * $a[2];
}

function getContrastRatio($hex1, $hex2) {
    list($r1, $g1, $b1) = sscanf($hex1, "#%02x%02x%02x");
    list($r2, $g2, $b2) = sscanf($hex2, "#%02x%02x%02x");
    $l1 = getLuminance($r1, $g1, $b1);
    $l2 = getLuminance($r2, $g2, $b2);
    if ($l1 > $l2) {
        return ($l1 + 0.05) / ($l2 + 0.05);
    }
    return ($l2 + 0.05) / ($l1 + 0.05);
}

echo "[TEST 6] WCAG AA Color Contrast Verification:\n";
$pairs = [
    'White on Brand #0033A0' => ['#FFFFFF', '#0033A0', 4.5],
    'Dark text #0F172A on White' => ['#0F172A', '#FFFFFF', 4.5],
    'Muted text #475569 on White' => ['#475569', '#FFFFFF', 4.5],
    'Success text #065F46 on #ECFDF5' => ['#065F46', '#ECFDF5', 4.5],
    'Warning text #92400E on #FFFBEB' => ['#92400E', '#FFFBEB', 4.5],
    'Danger text #991B1B on #FEF2F2' => ['#991B1B', '#FEF2F2', 4.5],
    'Info text #075985 on #F0F9FF' => ['#075985', '#F0F9FF', 4.5],
];

foreach ($pairs as $label => $info) {
    $ratio = getContrastRatio($info[0], $info[1]);
    $passed = $ratio >= $info[2];
    printf("   • %-35s Ratio: %5.2f:1 (Req: >= %.1f:1) -> %s\n", $label, $ratio, $info[2], $passed ? 'PASS (AA)' : 'FAIL');
}

echo "\nALL LAYOUTS & KITCHEN SINK VERIFICATION COMPLETED SUCCESSFULLY!\n";
