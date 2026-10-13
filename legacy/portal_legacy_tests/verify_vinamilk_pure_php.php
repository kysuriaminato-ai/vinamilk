<?php
/**
 * Test & Verification Script for Vinamilk HRM Pure PHP 8.2 MVC
 */
define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

require_once ROOT_DIR . '/app/config/config.php';
require_once ROOT_DIR . '/app/helpers/functions.php';
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/core/Model.php';
require_once ROOT_DIR . '/app/models/OrgUnitModel.php';
require_once ROOT_DIR . '/app/models/EmployeeModel.php';
require_once ROOT_DIR . '/app/models/PayrollModel.php';
require_once ROOT_DIR . '/app/models/ApprovalModel.php';
require_once ROOT_DIR . '/app/models/AuditLogModel.php';
require_once ROOT_DIR . '/app/models/UserModel.php';

echo "====================================================================\n";
echo "VINAMILK ENTERPRISE HRM - PURE PHP 8.2 MVC VERIFICATION SUITE\n";
echo "====================================================================\n\n";

$passCount = 0;
$failCount = 0;

function assertTest(string $desc, bool $condition): void {
    global $passCount, $failCount;
    if ($condition) {
        echo " [PASS] " . $desc . "\n";
        $passCount++;
    } else {
        echo " [FAIL] " . $desc . "\n";
        $failCount++;
    }
}

// 1. Kiểm tra kết nối CSDL MySQL 8
try {
    $db = Database::getInstance();
    $pdo = $db->getConnection();
    assertTest("MySQL 8 Database Connection established via PDO Singleton", $pdo !== null);
} catch (Exception $e) {
    assertTest("MySQL 8 Database Connection failed: " . $e->getMessage(), false);
}

// 2. Kiểm tra Cây đơn vị 5 cấp (dm_donvi)
$orgModel = new OrgUnitModel();
$tree = $orgModel->getTree();
assertTest("dm_donvi có dữ liệu và lấy được toàn bộ cây (Tổng số: " . count($tree) . " đơn vị)", count($tree) >= 10);

$c1 = array_filter($tree, fn($u) => $u['CapDV'] == 1);
$c2 = array_filter($tree, fn($u) => $u['CapDV'] == 2);
$c3 = array_filter($tree, fn($u) => $u['CapDV'] == 3);
$c4 = array_filter($tree, fn($u) => $u['CapDV'] == 4);
$c5 = array_filter($tree, fn($u) => $u['CapDV'] == 5);
assertTest("Cây đơn vị có đủ 5 cấp: C1(" . count($c1) . "), C2(" . count($c2) . "), C3(" . count($c3) . "), C4(" . count($c4) . "), C5(" . count($c5) . ")", 
    count($c1) > 0 && count($c2) > 0 && count($c3) > 0 && count($c4) > 0 && count($c5) > 0
);

// 3. Kiểm tra Recursive CTE trong getDescendantUnits()
$descendantsNM_BD = $orgModel->getDescendantUnits('NM_BD');
assertTest("Đệ quy CTE getDescendantUnits('NM_BD') tìm ra chính nó và cấp dưới (Xưởng 1, Ca 1)", 
    in_array('NM_BD', $descendantsNM_BD) && in_array('NM_BD_XUONG1', $descendantsNM_BD) && in_array('NM_BD_CA1', $descendantsNM_BD)
);

// 4. Kiểm tra Phân quyền & scopeByUnit() cho 7 Vai trò
$userModel = new UserModel();
$empModel = new EmployeeModel();

// 4.1 ADMIN -> Thấy toàn bộ
$admin = $userModel->findByUsername('admin');
Auth::login($admin);
$adminEmps = $empModel->getEmployees([], 100, 0);
assertTest("Role ADMIN: Thấy toàn bộ nhân sự (scopeByUnit trả về " . $adminEmps['total'] . " hồ sơ)", $adminEmps['total'] >= 7);

// 4.2 BOD -> Thấy toàn bộ (chỉ xem)
$bod = $userModel->findByUsername('bod.ceo');
Auth::login($bod);
$bodEmps = $empModel->getEmployees([], 100, 0);
assertTest("Role BOD: Thấy toàn bộ nhân sự (scopeByUnit trả về " . $bodEmps['total'] . " hồ sơ)", $bodEmps['total'] === $adminEmps['total']);

// 4.3 HR_TAPDOAN -> Thấy toàn bộ
$hrTapDoan = $userModel->findByUsername('hr.tapdoan');
Auth::login($hrTapDoan);
$hrTapDoanEmps = $empModel->getEmployees([], 100, 0);
assertTest("Role HR_TAPDOAN: Thấy toàn bộ nhân sự (" . $hrTapDoanEmps['total'] . " hồ sơ)", $hrTapDoanEmps['total'] === $adminEmps['total']);

// 4.4 HR_DONVI (NM_BD) -> Chỉ thấy cây thuộc NM_BD
$hrNhaMay = $userModel->findByUsername('hr.nhamay');
Auth::login($hrNhaMay);
$hrNhaMayEmps = $empModel->getEmployees([], 100, 0);
$allInBranch = true;
foreach ($hrNhaMayEmps['data'] as $e) {
    if (!in_array($e['MaDV'], $descendantsNM_BD)) {
        $allInBranch = false;
        break;
    }
}
assertTest("Role HR_DONVI (NM_BD): Chỉ thấy nhân viên thuộc cây NM_BD (Số lượng: " . $hrNhaMayEmps['total'] . " <= Tổng)", 
    $allInBranch && $hrNhaMayEmps['total'] <= $adminEmps['total']
);

// 4.5 NHANVIEN (ESS: VNM-0016) -> Chỉ thấy chính mình
$nvEss = $userModel->findByUsername('nhanvien.ess');
Auth::login($nvEss);
$essEmps = $empModel->getEmployees([], 100, 0);
assertTest("Role NHANVIEN (ESS): scopeByUnit chỉ trả về duy nhất bản thân (" . $essEmps['total'] . " hồ sơ)", 
    $essEmps['total'] === 1 && $essEmps['data'][0]['MaNV'] === 'VNM-0016'
);

// 5. Kiểm tra Tham số pháp lý từ cau_hinh_luong (Không dùng hằng số)
$payrollModel = new PayrollModel();
$legalConfig = $payrollModel->getLegalConfig('2026-10-12');
assertTest("Đọc tham số pháp lý từ cau_hinh_luong thành công", !empty($legalConfig));
assertTest("Lương cơ sở 2026 = 2.340.000 (NĐ 73/2024)", (float)($legalConfig['LUONG_CO_SO'] ?? 0) === 2340000.0);
assertTest("Lương tối thiểu Vùng 1 = 4.960.000 (NĐ 74/2024)", (float)($legalConfig['LUONG_TT_VUNG_1'] ?? 0) === 4960000.0);
assertTest("Tỷ lệ BHXH NLĐ = 8% (0.08)", (float)($legalConfig['TYLE_BHXH_NV'] ?? 0) === 0.08);
assertTest("Tỷ lệ BHYT NLĐ = 1.5% (0.015)", (float)($legalConfig['TYLE_BHYT_NV'] ?? 0) === 0.015);
assertTest("Tỷ lệ BHTN NLĐ = 1% (0.01)", (float)($legalConfig['TYLE_BHTN_NV'] ?? 0) === 0.01);
assertTest("Giảm trừ gia cảnh bản thân = 11.000.000 (NQ 954/2020)", (float)($legalConfig['GIAM_TRU_BAN_THAN'] ?? 0) === 11000000.0);
assertTest("Giảm trừ gia cảnh người phụ thuộc = 4.400.000", (float)($legalConfig['GIAM_TRU_PHU_THUOC'] ?? 0) === 4400000.0);

// 6. Kiểm tra Thuế TNCN lũy tiến 7 bậc (bieu_thue_tncn)
$taxBrackets = $payrollModel->getTaxBrackets('2026-10-12');
assertTest("Biểu thuế TNCN gồm 7 bậc lũy tiến theo Luật Thuế TNCN", count($taxBrackets) === 7);

// Test tính thuế thử: Thu nhập tính thuế 20.000.000
// Bậc 1: 5tr * 5% = 250.000
// Bậc 2: 5tr * 10% = 500.000
// Bậc 3: 8tr * 15% = 1.200.000
// Bậc 4: 2tr * 20% = 400.000
// Tổng thuế: 2.350.000
$calcTax = $payrollModel->calculateProgressiveTax(20000000, '2026-10-12');
assertTest("Tính thuế lũy tiến TNCN cho 20.000.000 VNĐ: Thuế chuẩn = 2.350.000 (Thực tế: " . number_format($calcTax) . ")", $calcTax === 2350000.0);

// 7. Kiểm tra Tính lương hàng loạt (Batch Payroll Calculation)
Auth::login($admin);
$calcBatchId = $payrollModel->calculateAndSaveBatch('2026-10', 'ALL');
assertTest("Tính và lưu bảng lương tháng 2026-10 thành công (Mã bảng lương: {$calcBatchId})", $calcBatchId > 0);

// 8. Kiểm tra Audit Log
$auditModel = new AuditLogModel();
$logs = $auditModel->getLogs(50);
assertTest("Hệ thống tự động ghi audit_log khi thực thi (Tổng log: " . count($logs) . ")", count($logs) > 0);

// 9. Kiểm tra Bảo mật XSS e() & CSRF Token
$dirtyString = '<script>alert("XSS")</script>&"\'';
$cleanString = e($dirtyString);
assertTest("Hàm e() chống XSS hoạt động chính xác", $cleanString === '&lt;script&gt;alert(&quot;XSS&quot;)&lt;/script&gt;&amp;&quot;&#039;');

Session::start();
$token = Session::getCsrfToken();
assertTest("CSRF Token sinh tự động và hợp lệ", !empty($token) && strlen($token) >= 32);

echo "\n====================================================================\n";
echo "KẾT QUẢ KIỂM THỬ: {$passCount} PASSED, {$failCount} FAILED\n";
echo "====================================================================\n";
