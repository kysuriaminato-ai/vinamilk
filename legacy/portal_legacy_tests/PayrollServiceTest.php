<?php
/**
 * =====================================================================
 * TEST SUITE: 3P PAYROLL SERVICE - VINAMILK HRM
 * =====================================================================
 * Kiểm thử toàn diện 25 ca biên (Edge Cases) theo yêu cầu:
 *   - Không người phụ thuộc (0 NPT), 1 NPT, 3 NPT
 *   - Vượt trần BHXH/BHYT (20 lần lương cơ sở: 46.8tr)
 *   - Vượt trần BHTN (20 lần lương tối thiểu vùng)
 *   - Thu nhập đúng ngưỡng các bậc thuế TNCN 2026: 10tr, 30tr, 60tr, 100tr
 *   - Có OT ngày thường, ngày nghỉ, lễ tết, ca đêm & phần miễn thuế TNCN
 *   - Phụ cấp ăn ca dưới trần & vượt trần (730k)
 *   - Khấu trừ khác (tạm ứng, bồi hoàn tài sản)
 *   - Quy trình duyệt 5 bước & Khóa sửa tuyệt đối sau khi ĐÃ CHI
 *   - In Bảng đối chiếu chi tiết để so sánh với file Excel
 */

declare(strict_types=1);

require_once __DIR__ . '/../app/core/Database.php';
require_once __DIR__ . '/../app/services/PayrollService.php';

$passCount = 0;
$failCount = 0;
$comparisonRows = [];

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

echo "======================================================================\n";
echo "BẮT ĐẦU KIỂM THỬ 3P PAYROLL SERVICE & PHÁP LÝ THUẾ 2026 (VINAMILK HRM)\n";
echo "======================================================================\n\n";

$service = new PayrollService();
$effectiveDate2026 = '2026-10-31';

$legalConfig = $service->getLegalConfig($effectiveDate2026);
$taxBrackets = $service->getTaxBrackets($effectiveDate2026);

// ---------------------------------------------------------------------
// TEST NHÓM I: KIỂM THỬ CẤU HÌNH PHÁP LÝ 2026
// ---------------------------------------------------------------------
echo "[I] KIỂM THỬ THAM SỐ PHÁP LÝ & BIỂU THUẾ TỪ 01/01/2026:\n";
assertTest("Lương cơ sở = 2.340.000 đ", $legalConfig['LUONG_CO_SO'] == 2340000.0);
assertTest("Giảm trừ bản thân từ 2026 = 15.500.000 đ (15,5tr)", $legalConfig['GIAM_TRU_BAN_THAN'] == 15500000.0);
assertTest("Giảm trừ người phụ thuộc từ 2026 = 6.200.000 đ/người (6,2tr)", $legalConfig['GIAM_TRU_PHU_THUOC'] == 6200000.0);
assertTest("Phụ cấp ăn ca miễn thuế = 730.000 đ/tháng", $legalConfig['PHU_CAP_AN_CA_MIEN_THUE'] == 730000.0);
assertTest("Biểu thuế TNCN từ 2026 gồm 5 bậc lũy tiến", count($taxBrackets) === 5);
assertTest("Bậc 1: Thuế suất 5% (Đến 10 triệu)", (float)$taxBrackets[0]['thue_suat'] === 5.0 && (float)$taxBrackets[0]['thu_nhap_den'] === 10000000.0);
assertTest("Bậc 2: Thuế suất 10% (10tr - 30tr)", (float)$taxBrackets[1]['thue_suat'] === 10.0 && (float)$taxBrackets[1]['thu_nhap_den'] === 30000000.0);
assertTest("Bậc 3: Thuế suất 20% (30tr - 60tr)", (float)$taxBrackets[2]['thue_suat'] === 20.0 && (float)$taxBrackets[2]['thu_nhap_den'] === 60000000.0);
assertTest("Bậc 4: Thuế suất 30% (60tr - 100tr)", (float)$taxBrackets[3]['thue_suat'] === 30.0 && (float)$taxBrackets[3]['thu_nhap_den'] === 100000000.0);
assertTest("Bậc 5: Thuế suất 35% (Trên 100tr)", (float)$taxBrackets[4]['thue_suat'] === 35.0 && $taxBrackets[4]['thu_nhap_den'] === null);

// ---------------------------------------------------------------------
// TEST NHÓM II: 25 CA BIÊN KIỂM THỬ TỪNG THÀNH PHẦN & TỔNG HỢP
// ---------------------------------------------------------------------
echo "\n[II] KIỂM THỬ 25 CA BIÊN CHI TIẾT:\n";

// CA 1: Lương thấp dưới ngưỡng giảm trừ (8tr), 0 NPT -> Không phát sinh thuế TNCN
$c1 = $service->calculateEmployeeSalary([
    'contract_salary' => 8000000.0,
    'actual_days'     => 26.0,
    'kpi_score'       => 100.0,
    'dependents_count'=> 0,
    'effective_date'  => $effectiveDate2026
], $legalConfig, $taxBrackets);
assertTest("Ca 1: Lương 8tr, 0 NPT -> Thuế TNCN = 0", $c1['thue_tncn'] == 0.0);
assertTest("Ca 1: BHXH 8% = 640.000 đ", $c1['bhxh_nv'] == 640000.0);
$comparisonRows[] = ['Ca 1: Lương thấp (8tr), 0 NPT', $c1];

// CA 2: Thu nhập tính thuế đúng ngưỡng BẬC 1 (Đúng 10.000.000 đ)
// Giảm trừ bản thân: 15.500.000. Bảo hiểm: 0. Cần thu nhập chịu thuế = 25.500.000 đ
$taxCase2 = $service->calculatePersonalIncomeTax(25500000.0, $taxBrackets, 15500000.0, 0, 6200000.0, 0.0);
assertTest("Ca 2: Thu nhập tính thuế đúng 10 triệu -> Thuế 5% = 500.000 đ", $taxCase2['tax_amount'] == 500000.0 && $taxCase2['assessable_income'] == 10000000.0);

// CA 3: Thu nhập tính thuế đúng ngưỡng BẬC 2 (Đúng 30.000.000 đ)
// Thuế Bậc 1 (10tr * 5% = 500k) + Bậc 2 (20tr * 10% = 2tr) = 2.500.000 đ
$taxCase3 = $service->calculatePersonalIncomeTax(45500000.0, $taxBrackets, 15500000.0, 0, 6200000.0, 0.0);
assertTest("Ca 3: Thu nhập tính thuế đúng 30 triệu -> Thuế lũy tiến = 2.500.000 đ", $taxCase3['tax_amount'] == 2500000.0 && $taxCase3['assessable_income'] == 30000000.0);

// CA 4: Thu nhập tính thuế đúng ngưỡng BẬC 3 (Đúng 60.000.000 đ)
// Thuế: 2.5tr + Bậc 3 (30tr * 20% = 6tr) = 8.500.000 đ
$taxCase4 = $service->calculatePersonalIncomeTax(75500000.0, $taxBrackets, 15500000.0, 0, 6200000.0, 0.0);
assertTest("Ca 4: Thu nhập tính thuế đúng 60 triệu -> Thuế lũy tiến = 8.500.000 đ", $taxCase4['tax_amount'] == 8500000.0 && $taxCase4['assessable_income'] == 60000000.0);

// CA 5: Thu nhập tính thuế đúng ngưỡng BẬC 4 (Đúng 100.000.000 đ)
// Thuế: 8.5tr + Bậc 4 (40tr * 30% = 12tr) = 20.500.000 đ
$taxCase5 = $service->calculatePersonalIncomeTax(115500000.0, $taxBrackets, 15500000.0, 0, 6200000.0, 0.0);
assertTest("Ca 5: Thu nhập tính thuế đúng 100 triệu -> Thuế lũy tiến = 20.500.000 đ", $taxCase5['tax_amount'] == 20500000.0 && $taxCase5['assessable_income'] == 100000000.0);

// CA 6: Thu nhập vượt vào BẬC 5 (Thu nhập tính thuế 150.000.000 đ)
// Thuế: 20.5tr + 50tr * 35% (17.5tr) = 38.000.000 đ
$taxCase6 = $service->calculatePersonalIncomeTax(165500000.0, $taxBrackets, 15500000.0, 0, 6200000.0, 0.0);
assertTest("Ca 6: Thu nhập tính thuế 150 triệu -> Thuế Bậc 5 = 38.000.000 đ", $taxCase6['tax_amount'] == 38000000.0);

// CA 7: Nhân sự có 3 người phụ thuộc (3 NPT)
// Giảm trừ NPT = 3 * 6.200.000 = 18.600.000 đ. Tổng giảm trừ gia cảnh = 15.5tr + 18.6tr = 34.100.000 đ
$c7 = $service->calculateEmployeeSalary([
    'contract_salary' => 45000000.0,
    'actual_days'     => 26.0,
    'kpi_score'       => 100.0,
    'dependents_count'=> 3,
    'effective_date'  => $effectiveDate2026
], $legalConfig, $taxBrackets);
assertTest("Ca 7: 3 NPT -> Giảm trừ NPT = 18.600.000 đ", $c7['giam_tru_phu_thuoc'] == 18600000.0);
assertTest("Ca 7: Tổng giảm trừ gia cảnh = 34.100.000 đ", $c7['tong_giam_tru_gia_canh'] == 34100000.0);
$comparisonRows[] = ['Ca 7: 3 Người phụ thuộc (45tr)', $c7];

// CA 8: Vượt trần đóng BHXH/BHYT (Lương HĐ 60.000.000 đ > Trần 46.800.000 đ)
$insCase8 = $service->calculateInsurances(60000000.0, 1, $legalConfig);
assertTest("Ca 8: Trần BHXH/BHYT = 46.800.000 đ (20 * 2.340.000)", $insCase8['tran_bhxh'] == 46800000.0);
assertTest("Ca 8: Lương đóng BHXH/BHYT bị chặn ở 46.800.000 đ", $insCase8['luong_dong_bhxh'] == 46800000.0);
assertTest("Ca 8: BHXH 8% = 3.744.000 đ (thay vì 4.8tr)", $insCase8['bhxh'] == 3744000.0);
assertTest("Ca 8: BHYT 1.5% = 702.000 đ (thay vì 900k)", $insCase8['bhyt'] == 702000.0);

// CA 9: Vượt trần BHTN Vùng 1 (Lương HĐ 120.000.000 đ > Trần BHTN 99.200.000 đ)
$insCase9 = $service->calculateInsurances(120000000.0, 1, $legalConfig);
assertTest("Ca 9: Trần BHTN Vùng 1 = 99.200.000 đ (20 * 4.960.000)", $insCase9['tran_bhtn'] == 99200000.0);
assertTest("Ca 9: BHTN 1% bị chặn ở 992.000 đ", $insCase9['bhtn'] == 992000.0);

// CA 10: Đóng bảo hiểm tại Vùng 2 (Hải Phòng / Bắc Ninh - Tiên Sơn)
$insCase10 = $service->calculateInsurances(95000000.0, 2, $legalConfig);
assertTest("Ca 10: Trần BHTN Vùng 2 = 88.200.000 đ (20 * 4.410.000)", $insCase10['tran_bhtn'] == 88200000.0);
assertTest("Ca 10: BHTN 1% Vùng 2 bị chặn ở 882.000 đ", $insCase10['bhtn'] == 882000.0);

// CA 11: Đóng bảo hiểm tại Vùng 3 / Vùng 4 (Trang trại Nghệ An / Lâm Đồng)
$insCase11 = $service->calculateInsurances(80000000.0, 3, $legalConfig);
assertTest("Ca 11: Trần BHTN Vùng 3 = 77.200.000 đ (20 * 3.860.000)", $insCase11['tran_bhtn'] == 77200000.0);

// CA 12: Lương P1 làm việc đủ công (26/26 ngày công chuẩn)
$p1Full = $service->calculateP1(26000000.0, 26.0, 26.0);
assertTest("Ca 12: Đi làm đủ 26 ngày -> P1 = 26.000.000 đ trọn vẹn", $p1Full == 26000000.0);

// CA 13: Lương P1 nghỉ không lương nửa tháng (13/26 ngày công)
$p1Half = $service->calculateP1(26000000.0, 13.0, 26.0);
assertTest("Ca 13: Đi làm 13/26 ngày -> P1 = 13.000.000 đ", $p1Half == 13000000.0);

// CA 14: Lương P1 chỉ đi làm 1 ngày (1/26 ngày công)
$p1One = $service->calculateP1(26000000.0, 1.0, 26.0);
assertTest("Ca 14: Đi làm 1/26 ngày -> P1 = 1.000.000 đ", $p1One == 1000000.0);

// CA 15: OT ngày thường (10 giờ OT thường: 150%)
// Lương HĐ: 20.800.000 đ -> Đơn giá 1 giờ: 20.8tr / (26 * 8) = 100.000 đ/giờ
// 10 giờ OT thường: Tổng tiền = 10 * 100k * 1.5 = 1.500.000 đ
// Chịu thuế = 1.000.000 đ (100%), Miễn thuế = 500.000 đ (50% chênh lệch)
$otCase15 = $service->calculateOvertime(20800000.0, 10.0, 0.0, 0.0, 0.0);
assertTest("Ca 15: 10h OT thường -> Tổng tiền OT = 1.500.000 đ", $otCase15['total_ot_pay'] == 1500000.0);
assertTest("Ca 15: 10h OT thường -> Miễn thuế TNCN = 500.000 đ", $otCase15['tax_exempt_ot_pay'] == 500000.0);
assertTest("Ca 15: 10h OT thường -> Chịu thuế TNCN = 1.000.000 đ", $otCase15['taxable_ot_pay'] == 1000000.0);

// CA 16: OT ngày nghỉ hàng tuần Chủ nhật (8 giờ OT nghỉ: 200%)
// Đơn giá 100k -> Tổng tiền = 8 * 100k * 2.0 = 1.600.000 đ
// Chịu thuế = 800k (100%), Miễn thuế = 800k (100% chênh lệch)
$otCase16 = $service->calculateOvertime(20800000.0, 0.0, 8.0, 0.0, 0.0);
assertTest("Ca 16: 8h OT ngày nghỉ -> Tổng tiền OT = 1.600.000 đ", $otCase16['total_ot_pay'] == 1600000.0);
assertTest("Ca 16: 8h OT ngày nghỉ -> Miễn thuế TNCN = 800.000 đ", $otCase16['tax_exempt_ot_pay'] == 800000.0);

// CA 17: OT ngày lễ tết (8 giờ OT lễ: 300%)
// Đơn giá 100k -> Tổng tiền = 8 * 100k * 3.0 = 2.400.000 đ
// Chịu thuế = 800k (100%), Miễn thuế = 1.600.000 đ (200% chênh lệch)
$otCase17 = $service->calculateOvertime(20800000.0, 0.0, 0.0, 8.0, 0.0);
assertTest("Ca 17: 8h OT ngày lễ -> Tổng tiền OT = 2.400.000 đ", $otCase17['total_ot_pay'] == 2400000.0);
assertTest("Ca 17: 8h OT ngày lễ -> Miễn thuế TNCN = 1.600.000 đ", $otCase17['tax_exempt_ot_pay'] == 1600000.0);

// CA 18: Phụ cấp làm đêm (+30%)
// 10 giờ đêm -> Tiền thêm: 10 * 100k * 0.3 = 300.000 đ -> Miễn thuế trọn vẹn 300k
$otCase18 = $service->calculateOvertime(20800000.0, 0.0, 0.0, 0.0, 10.0);
assertTest("Ca 18: 10h ca đêm -> Phụ cấp đêm = 300.000 đ", $otCase18['total_ot_pay'] == 300000.0);
assertTest("Ca 18: 10h ca đêm -> Miễn thuế TNCN trọn vẹn 300.000 đ", $otCase18['tax_exempt_ot_pay'] == 300000.0);

// CA 19: Tổng hợp nhiều loại OT trong tháng
// 10h thường + 8h nghỉ + 8h lễ + 8h đêm
// Tổng tiền = 1.5tr + 1.6tr + 2.4tr + 240k = 5.740.000 đ
// Miễn thuế = 500k + 800k + 1.6tr + 240k = 3.140.000 đ
$otCase19 = $service->calculateOvertime(20800000.0, 10.0, 8.0, 8.0, 8.0);
assertTest("Ca 19: Tổng hợp OT hỗn hợp -> Tổng tiền OT = 5.740.000 đ", $otCase19['total_ot_pay'] == 5740000.0);
assertTest("Ca 19: Tổng hợp OT hỗn hợp -> Miễn thuế = 3.140.000 đ", $otCase19['tax_exempt_ot_pay'] == 3140000.0);

// CA 20: Phụ cấp ăn ca dưới trần miễn thuế (600.000 đ < 730.000 đ)
$mealCase20 = $service->calculateMealAllowance(600000.0, 730000.0);
assertTest("Ca 20: Ăn ca 600k -> Miễn thuế toàn bộ 600.000 đ", $mealCase20['exempt_meal_allowance'] == 600000.0 && $mealCase20['taxable_meal_allowance'] == 0.0);

// CA 21: Phụ cấp ăn ca vượt trần miễn thuế (1.500.000 đ > 730.000 đ)
// Miễn thuế: 730.000 đ; Chịu thuế: 770.000 đ
$mealCase21 = $service->calculateMealAllowance(1500000.0, 730000.0);
assertTest("Ca 21: Ăn ca 1.5tr -> Miễn thuế 730.000 đ", $mealCase21['exempt_meal_allowance'] == 730000.0);
assertTest("Ca 21: Ăn ca 1.5tr -> Phần vượt 770.000 đ chịu thuế TNCN", $mealCase21['taxable_meal_allowance'] == 770000.0);

// CA 22: Khấu trừ khác: Tạm ứng lương 5.000.000 đ & Phạt mất tài sản 10.000.000 đ
$dedCase22 = $service->calculateOtherDeductions(5000000.0, 10000000.0);
assertTest("Ca 22: Tạm ứng 5tr + Mất tài sản 10tr -> Tổng khấu trừ = 15.000.000 đ", $dedCase22['total_other_deductions'] == 15000000.0);

// CA 23: P3 KPI xuất sắc (Điểm KPI = 125 điểm -> Hệ số 1.25)
$p3High = $service->calculateP3(20000000.0, 125.0, 0.15);
// 20tr * 15% * 1.25 = 3.750.000 đ
assertTest("Ca 23: KPI 125 điểm -> P3 = 3.750.000 đ", $p3High == 3750000.0);

// CA 24: P3 KPI dưới mức chuẩn (Điểm KPI = 70 điểm -> Hệ số 0.70)
$p3Low = $service->calculateP3(20000000.0, 70.0, 0.15);
// 20tr * 15% * 0.7 = 2.100.000 đ
assertTest("Ca 24: KPI 70 điểm -> P3 = 2.100.000 đ", $p3Low == 2100000.0);

// CA 25: Quy trình duyệt 5 bước và Khóa sửa tuyệt đối sau khi ĐÃ CHI
echo "\n[III] KIỂM THỬ QUY TRÌNH DUYỆT 5 BƯỚC & KHÓA SỔ BẤT BIẾN:\n";
$db = Database::getInstance()->getConnection();
$db->exec("DELETE FROM bang_luong_chi_tiet WHERE bang_luong_id IN (SELECT id FROM bang_luong WHERE ky_luong = '2026-10' AND ma_dv = 'NM_TS_TEST')");
$db->exec("DELETE FROM bang_luong WHERE ky_luong = '2026-10' AND ma_dv = 'NM_TS_TEST'");

$draftId = $service->createDraft('2026-10', 'NM_TS_TEST', 'tester');
assertTest("Bước 1: Tạo bảng lương Nháp thành công (ID: {$draftId})", $draftId > 0);

// Chạy tính toán (chuyển sang DA_TINH)
$batchRes = $service->calculateBatch($draftId);
assertTest("Bước 2: Tính lương hàng loạt chuyển sang DA_TINH", $batchRes['status'] === 'DA_TINH');

// Kế toán duyệt (KETOAN_DUYET)
$okKetoan = $service->accountantApprove($draftId, 'Kế toán trưởng Nguyễn Văn K');
assertTest("Bước 3: Kế toán duyệt chuyển sang KETOAN_DUYET", $okKetoan === true);

// Giám đốc duyệt (GIAMDOC_DUYET)
$okGiamdoc = $service->directorApprove($draftId, 'Tổng Giám đốc Mai Kiều L');
assertTest("Bước 4: Giám đốc duyệt chuyển sang GIAMDOC_DUYET", $okGiamdoc === true);

// Đã chi (DA_CHI) -> Khóa sổ
$okDachi = $service->disburse($draftId, 'Thủ quỹ Chi nhánh');
assertTest("Bước 5: Chi tiền thành công chuyển sang DA_CHI", $okDachi === true);
assertTest("Hàm isLocked xác nhận bảng lương đã bị KHÓA CỨNG", $service->isLocked($draftId) === true);

// Thử tính toán lại hoặc sửa đổi khi đã chi -> Bắt buộc ném PayrollLockedException!
$blockedExceptionCaught = false;
try {
    $service->calculateBatch($draftId);
} catch (PayrollLockedException $e) {
    $blockedExceptionCaught = true;
}
assertTest("BẢO VỆ KHÓA LƯƠNG: Cố tình tính lại bảng lương ĐÃ CHI bị chặn tuyệt đối (PayrollLockedException)", $blockedExceptionCaught === true);

// Thêm một số case tổng hợp vào bảng đối chiếu
$comparisonRows[] = ['Ca 15: OT ngày thường 10h', $service->calculateEmployeeSalary(['contract_salary' => 20800000.0, 'ot_weekday_hours' => 10.0, 'effective_date' => $effectiveDate2026], $legalConfig, $taxBrackets)];
$comparisonRows[] = ['Ca 16: OT ngày nghỉ tuần 8h', $service->calculateEmployeeSalary(['contract_salary' => 20800000.0, 'ot_weekend_hours' => 8.0, 'effective_date' => $effectiveDate2026], $legalConfig, $taxBrackets)];
$comparisonRows[] = ['Ca 17: OT ngày lễ tết 8h', $service->calculateEmployeeSalary(['contract_salary' => 20800000.0, 'ot_holiday_hours' => 8.0, 'effective_date' => $effectiveDate2026], $legalConfig, $taxBrackets)];
$comparisonRows[] = ['Ca 19: OT hỗn hợp (Thường+Nghỉ+Lễ+Đêm)', $service->calculateEmployeeSalary(['contract_salary' => 20800000.0, 'ot_weekday_hours' => 10.0, 'ot_weekend_hours' => 8.0, 'ot_holiday_hours' => 8.0, 'night_hours' => 8.0, 'effective_date' => $effectiveDate2026], $legalConfig, $taxBrackets)];
$comparisonRows[] = ['Ca 8: Vượt trần BHXH (60tr, 0 NPT)', $service->calculateEmployeeSalary(['contract_salary' => 60000000.0, 'effective_date' => $effectiveDate2026], $legalConfig, $taxBrackets)];
$comparisonRows[] = ['Ca 9: Vượt trần BHTN (120tr, 0 NPT)', $service->calculateEmployeeSalary(['contract_salary' => 120000000.0, 'effective_date' => $effectiveDate2026], $legalConfig, $taxBrackets)];
$comparisonRows[] = ['Ca 22: Tạm ứng lương 5tr', $service->calculateEmployeeSalary(['contract_salary' => 30000000.0, 'advance_payment' => 5000000.0, 'effective_date' => $effectiveDate2026], $legalConfig, $taxBrackets)];

// ---------------------------------------------------------------------
// TEST NHÓM IV: IN BẢNG ĐỐI CHIẾU SO SÁNH VỚI EXCEL
// ---------------------------------------------------------------------
echo "\n===================================================================================================================================================\n";
echo "BẢNG ĐỐI CHIẾU CÁC CHỈ SỐ LƯƠNG 3P, BẢO HIỂM, THUẾ TNCN & THỰC LĨNH SO VỚI EXCEL:\n";
echo "===================================================================================================================================================\n";
printf(
    "%-35s | %-12s | %-12s | %-12s | %-12s | %-12s | %-12s | %-12s | %-12s | %-12s\n",
    "Tên Ca Kiểm Thử", "Lương HĐ", "Gross", "Bảo Hiểm", "OT Miễn Thuế", "Giảm Trừ GT", "TN Chịu Thuế", "TN Tính Thuế", "Thuế TNCN", "Thực Lĩnh (Net)"
);
echo str_repeat("-", 147) . "\n";

foreach ($comparisonRows as $r) {
    $title = $r[0];
    $d = $r[1];
    printf(
        "%-35s | %12s | %12s | %12s | %12s | %12s | %12s | %12s | %12s | %12s\n",
        mb_strimwidth($title, 0, 35, '..'),
        number_format($d['luong_hop_dong'], 0, ',', '.'),
        number_format($d['tong_thu_nhap_gross'], 0, ',', '.'),
        number_format($d['tong_bao_hiem_nv'], 0, ',', '.'),
        number_format($d['ot_mien_thue'], 0, ',', '.'),
        number_format($d['tong_giam_tru_gia_canh'], 0, ',', '.'),
        number_format($d['thu_nhap_chiu_thue'], 0, ',', '.'),
        number_format($d['thu_nhap_tinh_thue'], 0, ',', '.'),
        number_format($d['thue_tncn'], 0, ',', '.'),
        number_format($d['thuc_linh_net'], 0, ',', '.')
    );
}

echo "===================================================================================================================================================\n\n";

echo "KẾT QUẢ KIỂM THỬ: {$passCount} PASSED, {$failCount} FAILED\n";

if ($failCount > 0) {
    exit(1);
}
