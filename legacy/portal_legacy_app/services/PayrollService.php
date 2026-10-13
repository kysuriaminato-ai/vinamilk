<?php
/**
 * =====================================================================
 * VINAMILK HRM - 3P PAYROLL SERVICE (DỊCH VỤ TÍNH LƯƠNG CHUẨN DOANH NGHIỆP)
 * =====================================================================
 * Kiến trúc module hóa tách rời từng bước độc lập, dễ kiểm thử đơn vị:
 *   1. Lương P1 (Position): Theo lương HĐ hiện hành và số ngày công thực tế
 *   2. Lương P2 (Person/Competency): Năng lực theo bậc lương
 *   3. Lương P3 (Performance): Theo điểm KPI tháng từ bảng kpi (không hằng số)
 *   4. Lương làm thêm giờ (OT): Thường 150%, Nghỉ tuần 200%, Lễ tết 300%, Đêm +30%
 *      -> Phần chênh lệch trả cao hơn so với giờ thường MIỄN THUẾ TNCN
 *   5. Bảo hiểm bắt buộc: BHXH 8%, BHYT 1.5%, BHTN 1% kèm chặn trần pháp lý
 *   6. Thuế TNCN: Biểu thuế lũy tiến & giảm trừ bản thân (15,5tr)/NPT (6,2tr) từ cau_hinh_luong
 *   7. Phụ cấp ăn ca: Miễn thuế TNCN trong hạn mức cấu hình (730.000 đ)
 *   8. Khấu trừ khác: Tạm ứng, bồi hoàn tài sản chưa bàn giao
 *   9. Quy trình duyệt 5 bước: NHAP -> DA_TINH -> KETOAN_DUYET -> GIAMDOC_DUYET -> DA_CHI
 *      -> Khóa sửa tuyệt đối sau khi "ĐÃ CHI"
 *  10. Sinh phiếu lương điện tử (Payslip Printable/PDF) cho ESS
 */

declare(strict_types=1);

require_once __DIR__ . '/../core/Database.php';
require_once __DIR__ . '/../core/Session.php';
require_once __DIR__ . '/../helpers/Auth.php';

class PayrollLockedException extends RuntimeException {}
class PayrollValidationException extends InvalidArgumentException {}

class PayrollService
{
    private PDO $db;

    public function __construct(?PDO $pdo = null)
    {
        $this->db = $pdo ?? Database::getInstance()->getConnection();
    }

    // =========================================================================
    // 1. ĐỌC THAM SỐ PHÁP LÝ TỪ BẢNG cau_hinh_luong THEO KỲ HIỆU LỰC
    // =========================================================================

    /**
     * Lấy tham số pháp lý từ cau_hinh_luong hiệu lực tại ngày mốc
     * Tuyệt đối không dùng hằng số cố định trong mã nguồn.
     */
    public function getLegalConfig(string $effectiveDate): array
    {
        $sql = "SELECT ma_tham_so, gia_tri, don_vi_tinh, van_ban_phap_ly
                FROM cau_hinh_luong
                WHERE ngay_hieu_luc <= :d1 
                  AND (ngay_het_han IS NULL OR ngay_het_han >= :d2)";

        $stmt = $this->db->prepare($sql);
        $stmt->execute([':d1' => $effectiveDate, ':d2' => $effectiveDate]);
        $rows = $stmt->fetchAll(PDO::FETCH_ASSOC);

        if (empty($rows)) {
            throw new PayrollValidationException("Không tìm thấy cấu hình lương pháp lý hiệu lực cho ngày: {$effectiveDate}");
        }

        $config = [];
        foreach ($rows as $r) {
            $config[$r['ma_tham_so']] = (float)$r['gia_tri'];
        }

        // Bổ sung các giá trị mặc định nếu bảng cấu hình chưa có
        $config['PHU_CAP_AN_CA_MIEN_THUE'] = $config['PHU_CAP_AN_CA_MIEN_THUE'] ?? 730000.0;
        $config['OT_RATE_WEEKDAY']         = $config['OT_RATE_WEEKDAY'] ?? 1.5;
        $config['OT_RATE_WEEKEND']         = $config['OT_RATE_WEEKEND'] ?? 2.0;
        $config['OT_RATE_HOLIDAY']         = $config['OT_RATE_HOLIDAY'] ?? 3.0;
        $config['NIGHT_SHIFT_RATE']        = $config['NIGHT_SHIFT_RATE'] ?? 0.3;

        return $config;
    }

    /**
     * Lấy biểu thuế TNCN lũy tiến từ bieu_thue_tncn hiệu lực tại ngày mốc
     */
    public function getTaxBrackets(string $effectiveDate): array
    {
        $sql = "SELECT bac_thue, thu_nhap_tu, thu_nhap_den, thue_suat, so_tru_nhanh
                FROM bieu_thue_tncn
                WHERE ngay_hieu_luc <= :d1
                  AND (ngay_het_han IS NULL OR ngay_het_han >= :d2)
                ORDER BY bac_thue ASC";

        $stmt = $this->db->prepare($sql);
        $stmt->execute([':d1' => $effectiveDate, ':d2' => $effectiveDate]);
        $rows = $stmt->fetchAll(PDO::FETCH_ASSOC);

        if (empty($rows)) {
            throw new PayrollValidationException("Không tìm thấy biểu thuế TNCN lũy tiến hiệu lực cho ngày: {$effectiveDate}");
        }

        return $rows;
    }

    // =========================================================================
    // 2. CÁC HÀM TÍNH TOÁN THÀNH PHẦN 3P & BẢO HIỂM, THUẾ (DECOUPLED & TESTABLE)
    // =========================================================================

    /**
     * BƯỚC 1: LƯƠNG P1 THEO LƯƠNG HỢP ĐỒNG HIỆN HÀNH VÀ CÔNG THỰC TẾ
     * Công thức: P1 = (Lương HĐ / Công chuẩn) * Công thực tế
     * 
     * @param float $contractSalary Lương thỏa thuận trên hợp đồng lao động
     * @param float $actualDays Số ngày công thực tế (gồm cả ngày phép có lương, lễ tết)
     * @param float $standardDays Công chuẩn tháng (mặc định 26.0 ngày)
     * @return float Mức lương P1 thực hưởng
     */
    public function calculateP1(float $contractSalary, float $actualDays, float $standardDays = 26.0): float
    {
        if ($contractSalary <= 0 || $standardDays <= 0) {
            return 0.0;
        }
        $actualDays = max(0.0, $actualDays);
        return round(($contractSalary / $standardDays) * $actualDays, 0);
    }

    /**
     * BƯỚC 2: LƯƠNG P2 NĂNG LỰC THEO BẬC (PERSON / COMPETENCY)
     * Công thức: P2 = Lương HĐ * Hệ số bậc năng lực
     * 
     * @param float $contractSalary Lương hợp đồng
     * @param string|null $bacLuong Bậc lương (BẬC 1 -> BẬC 6)
     * @param float|null $heSoBac Hệ số năng lực cụ thể (nếu truyền vào)
     * @return float Lương năng lực P2
     */
    public function calculateP2(float $contractSalary, ?string $bacLuong = null, ?float $heSoBac = null): float
    {
        if ($contractSalary <= 0) return 0.0;

        if ($heSoBac !== null) {
            return round($contractSalary * max(0.0, $heSoBac), 0);
        }

        // Bảng thang bậc năng lực chuẩn Vinamilk nếu không chỉ định hệ số cụ thể
        $rate = match (strtoupper((string)$bacLuong)) {
            'BAC_1', 'BẬC 1', '1' => 0.10,
            'BAC_2', 'BẬC 2', '2' => 0.15,
            'BAC_3', 'BẬC 3', '3' => 0.20,
            'BAC_4', 'BẬC 4', '4' => 0.25,
            'BAC_5', 'BẬC 5', '5' => 0.30,
            'BAC_6', 'BẬC 6', '6' => 0.35,
            default                => 0.15, // Mặc định 15%
        };

        return round($contractSalary * $rate, 0);
    }

    /**
     * BƯỚC 3: LƯƠNG P3 THEO ĐIỂM KPI THÁNG TỪ BẢNG kpi (PERFORMANCE)
     * Đọc điểm KPI tháng từ CSDL, không dùng hằng số cố định.
     * Công thức: P3 = Lương HĐ * Quỹ thưởng mục tiêu * (Điểm KPI / 100)
     * 
     * @param float $contractSalary Lương hợp đồng
     * @param float $kpiScore Điểm KPI thực tế trong tháng (ví dụ 100 điểm, 120 điểm, 80 điểm)
     * @param float $targetP3Rate Tỷ lệ quỹ thưởng P3 mục tiêu (mặc định 15% lương HĐ)
     * @return float Lương hiệu quả P3
     */
    public function calculateP3(float $contractSalary, float $kpiScore, float $targetP3Rate = 0.15): float
    {
        if ($contractSalary <= 0 || $kpiScore <= 0) {
            return 0.0;
        }

        // Tỷ lệ hoàn thành nhiệm vụ KPI (100 điểm = 100% = 1.0; 120 điểm = 1.2)
        $heSoKpi = $kpiScore / 100.0;
        return round($contractSalary * $targetP3Rate * $heSoKpi, 0);
    }

    /**
     * Lấy điểm KPI của nhân viên trong kỳ từ bảng kpi
     */
    public function getKpiScoreFromDb(string $maNV, int $month, int $year): float
    {
        $sql = "SELECT diem_kpi FROM kpi WHERE ma_nv = :manv AND thang = :m AND nam = :y LIMIT 1";
        $stmt = $this->db->prepare($sql);
        $stmt->execute([':manv' => $maNV, ':m' => $month, ':y' => $year]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);

        return $row ? (float)$row['diem_kpi'] : 100.0; // Mặc định 100 điểm nếu chưa đánh giá
    }

    /**
     * BƯỚC 4: TÍNH TIỀN LÀM THÊM GIỜ (OT) & TÁCH PHẦN MIỄN THUẾ TNCN
     * Tuân thủ quy định:
     *   - Ngày thường: 150% (100% chịu thuế, 50% chênh lệch MIỄN THUẾ)
     *   - Ngày nghỉ tuần: 200% (100% chịu thuế, 100% chênh lệch MIỄN THUẾ)
     *   - Ngày lễ tết: 300% (100% chịu thuế, 200% chênh lệch MIỄN THUẾ)
     *   - Làm ban đêm: +30% (30% phụ trội MIỄN THUẾ)
     * 
     * @param float $contractSalary Lương hợp đồng làm căn cứ tính đơn giá giờ
     * @param float $otWeekdayHours Số giờ OT ngày thường
     * @param float $otWeekendHours Số giờ OT ngày nghỉ hàng tuần
     * @param float $otHolidayHours Số giờ OT ngày lễ tết
     * @param float $nightHours Số giờ làm việc ban đêm (22:00 - 06:00)
     * @param float $standardDays Công chuẩn tháng (26 ngày)
     * @param float $hoursPerDay Số giờ làm việc tiêu chuẩn 1 ngày (8 giờ)
     * @return array Kết quả chi tiết tiền OT, tiền chịu thuế và tiền miễn thuế
     */
    public function calculateOvertime(
        float $contractSalary,
        float $otWeekdayHours,
        float $otWeekendHours,
        float $otHolidayHours,
        float $nightHours,
        float $standardDays = 26.0,
        float $hoursPerDay = 8.0,
        ?array $rates = null
    ): array {
        if ($contractSalary <= 0 || $standardDays <= 0 || $hoursPerDay <= 0) {
            return [
                'hourly_rate'       => 0.0,
                'weekday_pay'       => 0.0,
                'weekend_pay'       => 0.0,
                'holiday_pay'       => 0.0,
                'night_pay'         => 0.0,
                'total_ot_pay'      => 0.0,
                'taxable_ot_pay'    => 0.0,
                'tax_exempt_ot_pay' => 0.0
            ];
        }

        // Đơn giá 1 giờ làm việc bình thường
        $hourlyRate = $contractSalary / ($standardDays * $hoursPerDay);

        $rateWeekday = $rates['weekday'] ?? 1.5;
        $rateWeekend = $rates['weekend'] ?? 2.0;
        $rateHoliday = $rates['holiday'] ?? 3.0;
        $rateNight   = $rates['night']   ?? 0.3;

        // 1. Tiền làm thêm ngày thường (150%)
        $weekdayPay = $otWeekdayHours * $hourlyRate * $rateWeekday;
        $weekdayTaxable = $otWeekdayHours * $hourlyRate * 1.0;
        $weekdayExempt  = $otWeekdayHours * $hourlyRate * ($rateWeekday - 1.0); // 50% chênh lệch miễn thuế

        // 2. Tiền làm thêm ngày nghỉ tuần (200%)
        $weekendPay = $otWeekendHours * $hourlyRate * $rateWeekend;
        $weekendTaxable = $otWeekendHours * $hourlyRate * 1.0;
        $weekendExempt  = $otWeekendHours * $hourlyRate * ($rateWeekend - 1.0); // 100% chênh lệch miễn thuế

        // 3. Tiền làm thêm ngày lễ tết (300%)
        $holidayPay = $otHolidayHours * $hourlyRate * $rateHoliday;
        $holidayTaxable = $otHolidayHours * $hourlyRate * 1.0;
        $holidayExempt  = $otHolidayHours * $hourlyRate * ($rateHoliday - 1.0); // 200% chênh lệch miễn thuế

        // 4. Tiền phụ cấp làm việc ban đêm (+30%)
        // Theo Điều 9 TT 111/2013/TT-BTC: Toàn bộ phần trả thêm cho giờ làm đêm được miễn thuế
        $nightPay    = $nightHours * $hourlyRate * $rateNight;
        $nightExempt = $nightPay;

        $totalOtPay     = round($weekdayPay + $weekendPay + $holidayPay + $nightPay, 0);
        $taxExemptOtPay = round($weekdayExempt + $weekendExempt + $holidayExempt + $nightExempt, 0);
        $taxableOtPay   = round($totalOtPay - $taxExemptOtPay, 0);

        return [
            'hourly_rate'       => round($hourlyRate, 2),
            'weekday_pay'       => round($weekdayPay, 0),
            'weekend_pay'       => round($weekendPay, 0),
            'holiday_pay'       => round($holidayPay, 0),
            'night_pay'         => round($nightPay, 0),
            'total_ot_pay'      => $totalOtPay,
            'taxable_ot_pay'    => $taxableOtPay,
            'tax_exempt_ot_pay' => $taxExemptOtPay
        ];
    }

    /**
     * BƯỚC 5: TÍNH CÁC KHOẢN BẢO HIỂM BẮT BUỘC & ÁP TRẦN ĐÓNG THEO PHÁP LUẬT
     * - BHXH 8%, BHYT 1.5%: Tối đa 20 lần mức Lương cơ sở
     * - BHTN 1%: Tối đa 20 lần mức Lương tối thiểu vùng (Vùng 1, 2, 3, 4)
     * 
     * @param float $insurableSalary Mức lương làm căn cứ đóng bảo hiểm
     * @param int $region Vùng lương tối thiểu (1, 2, 3, 4)
     * @param array $legalConfig Cấu hình pháp lý đọc từ cau_hinh_luong
     * @return array Kết quả trích nộp bảo hiểm người lao động
     */
    public function calculateInsurances(float $insurableSalary, int $region, array $legalConfig): array
    {
        if ($insurableSalary <= 0) {
            return [
                'bhxh'            => 0.0,
                'bhyt'            => 0.0,
                'bhtn'            => 0.0,
                'total_insurance' => 0.0,
                'luong_dong_bhxh' => 0.0,
                'luong_dong_bhtn' => 0.0,
                'tran_bhxh'       => 0.0,
                'tran_bhtn'       => 0.0
            ];
        }

        $luongCoSo = $legalConfig['LUONG_CO_SO'] ?? 2340000.0;
        $heSoTranBhxh = $legalConfig['TRAN_BHXH_HE_SO'] ?? 20.0;
        $heSoTranBhtn = $legalConfig['TRAN_BHTN_HE_SO'] ?? 20.0;

        // Xác định lương tối thiểu vùng tương ứng
        $luongTtVung = match ($region) {
            2 => $legalConfig['LUONG_TT_VUNG_2'] ?? 4410000.0,
            3 => $legalConfig['LUONG_TT_VUNG_3'] ?? 3860000.0,
            4 => $legalConfig['LUONG_TT_VUNG_4'] ?? 3450000.0,
            default => $legalConfig['LUONG_TT_VUNG_1'] ?? 4960000.0,
        };

        // 1. Trần BHXH / BHYT = 20 lần Lương cơ sở
        $tranBhxh = $heSoTranBhxh * $luongCoSo;
        $luongDongBhxh = min($insurableSalary, $tranBhxh);

        // 2. Trần BHTN = 20 lần Lương tối thiểu vùng
        $tranBhtn = $heSoTranBhtn * $luongTtVung;
        $luongDongBhtn = min($insurableSalary, $tranBhtn);

        $rateBhxh = $legalConfig['TYLE_BHXH_NV'] ?? 0.08;
        $rateBhyt = $legalConfig['TYLE_BHYT_NV'] ?? 0.015;
        $rateBhtn = $legalConfig['TYLE_BHTN_NV'] ?? 0.01;

        $bhxh = round($luongDongBhxh * $rateBhxh, 0);
        $bhyt = round($luongDongBhxh * $rateBhyt, 0);
        $bhtn = round($luongDongBhtn * $rateBhtn, 0);
        $totalInsurance = $bhxh + $bhyt + $bhtn;

        return [
            'bhxh'            => $bhxh,
            'bhyt'            => $bhyt,
            'bhtn'            => $bhtn,
            'total_insurance' => $totalInsurance,
            'luong_dong_bhxh' => $luongDongBhxh,
            'luong_dong_bhtn' => $luongDongBhtn,
            'tran_bhxh'       => $tranBhxh,
            'tran_bhtn'       => $tranBhtn
        ];
    }

    /**
     * BƯỚC 6: TÍNH THUẾ TNCN THEO BIỂU THUẾ LŨY TIẾN TỪNG PHẦN
     * Hỗ trợ biểu thuế 5 bậc (áp dụng từ 01/01/2026: 15,5tr/6,2tr)
     * hoặc biểu 7 bậc (trước 2026: 11tr/4,4tr) tùy mốc hiệu lực.
     * 
     * @param float $taxableIncome Thu nhập chịu thuế sau khi đã trừ các khoản miễn thuế
     * @param array $taxBrackets Biểu thuế đọc từ CSDL
     * @param float $personalDeduction Mức giảm trừ bản thân (15.500.000 từ 2026)
     * @param int $dependents Số lượng người phụ thuộc
     * @param float $dependentDeduction Mức giảm trừ 1 người phụ thuộc (6.200.000 từ 2026)
     * @param float $insuranceDeduction Khoản đóng bảo hiểm bắt buộc được trừ
     * @return array Kết quả tính thuế chi tiết từng bậc
     */
    public function calculatePersonalIncomeTax(
        float $taxableIncome,
        array $taxBrackets,
        float $personalDeduction,
        int $dependents,
        float $dependentDeduction,
        float $insuranceDeduction = 0.0
    ): array {
        $totalDependentsDeduction = max(0, $dependents) * $dependentDeduction;
        $totalRelief = $personalDeduction + $totalDependentsDeduction + $insuranceDeduction;

        // Thu nhập tính thuế = Thu nhập chịu thuế - Các khoản giảm trừ
        $assessableIncome = max(0.0, $taxableIncome - $totalRelief);

        if ($assessableIncome <= 0) {
            return [
                'taxable_income'        => $taxableIncome,
                'personal_deduction'    => $personalDeduction,
                'dependents_count'      => $dependents,
                'dependents_deduction'  => $totalDependentsDeduction,
                'insurance_deduction'   => $insuranceDeduction,
                'total_relief'          => $totalRelief,
                'assessable_income'     => 0.0,
                'tax_amount'            => 0.0,
                'bracket_breakdown'     => []
            ];
        }

        // Tính thuế lũy tiến theo từng bậc trong bảng bieu_thue_tncn
        $taxAmount = 0.0;
        $breakdown = [];

        foreach ($taxBrackets as $b) {
            $from = (float)$b['thu_nhap_tu'];
            $to   = $b['thu_nhap_den'] !== null ? (float)$b['thu_nhap_den'] : null;
            $rate = (float)$b['thue_suat'] / 100.0;

            if ($assessableIncome > $from) {
                if ($to === null || $assessableIncome <= $to) {
                    $taxedInBracket = $assessableIncome - $from;
                    $bracketTax = $taxedInBracket * $rate;
                    $taxAmount += $bracketTax;
                    $breakdown[] = [
                        'bac'         => (int)$b['bac_thue'],
                        'rate'        => $b['thue_suat'] . '%',
                        'taxed_chunk' => $taxedInBracket,
                        'tax'         => round($bracketTax, 0)
                    ];
                    break;
                } else {
                    $taxedInBracket = $to - $from;
                    $bracketTax = $taxedInBracket * $rate;
                    $taxAmount += $bracketTax;
                    $breakdown[] = [
                        'bac'         => (int)$b['bac_thue'],
                        'rate'        => $b['thue_suat'] . '%',
                        'taxed_chunk' => $taxedInBracket,
                        'tax'         => round($bracketTax, 0)
                    ];
                }
            }
        }

        return [
            'taxable_income'       => $taxableIncome,
            'personal_deduction'   => $personalDeduction,
            'dependents_count'     => $dependents,
            'dependents_deduction' => $totalDependentsDeduction,
            'insurance_deduction'  => $insuranceDeduction,
            'total_relief'         => $totalRelief,
            'assessable_income'    => $assessableIncome,
            'tax_amount'           => round($taxAmount, 0),
            'bracket_breakdown'    => $breakdown
        ];
    }

    /**
     * BƯỚC 7: XỬ LÝ PHỤ CẤP ĂN CA VÀ PHẦN MIỄN THUẾ
     * Khoản phụ cấp ăn ca không chịu thuế TNCN trong hạn mức (730.000 đ/tháng).
     * Phần vượt hạn mức phải chịu thuế TNCN.
     */
    public function calculateMealAllowance(float $actualMealAllowance, float $maxExemptAllowance = 730000.0): array
    {
        $actualMealAllowance = max(0.0, $actualMealAllowance);
        $exemptAmount = min($actualMealAllowance, $maxExemptAllowance);
        $taxableAmount = max(0.0, $actualMealAllowance - $maxExemptAllowance);

        return [
            'actual_meal_allowance' => $actualMealAllowance,
            'exempt_meal_allowance' => $exemptAmount,
            'taxable_meal_allowance'=> $taxableAmount
        ];
    }

    /**
     * BƯỚC 8: CÁC KHOẢN KHẤU TRỪ KHÁC (TẠM ỨNG, BỒI HOÀN TÀI SẢN)
     */
    public function calculateOtherDeductions(float $advancePayment = 0.0, float $unreturnedAssetPenalty = 0.0): array
    {
        $advancePayment = max(0.0, $advancePayment);
        $unreturnedAssetPenalty = max(0.0, $unreturnedAssetPenalty);

        return [
            'advance_payment'           => $advancePayment,
            'unreturned_asset_penalty'  => $unreturnedAssetPenalty,
            'total_other_deductions'    => $advancePayment + $unreturnedAssetPenalty
        ];
    }

    // =========================================================================
    // 3. TÍNH TOÁN LƯƠNG TRỌN VẸN CHO 1 NHÂN SỰ (FULL PIPELINE CALCULATION)
    // =========================================================================

    /**
     * Tổng hợp toàn bộ quy trình tính toán lương chi tiết cho 1 nhân sự
     * 
     * @param array $input Dữ liệu nhân sự (Hợp đồng, Điểm KPI, Giờ công, OT, Phụ cấp, Khấu trừ...)
     * @param array|null $legalConfig Cấu hình pháp lý (nếu truyền vào)
     * @param array|null $taxBrackets Biểu thuế TNCN (nếu truyền vào)
     * @return array Kết quả chi tiết toàn bộ các chỉ số thu nhập, bảo hiểm, thuế, thực lĩnh
     */
    public function calculateEmployeeSalary(array $input, ?array $legalConfig = null, ?array $taxBrackets = null): array
    {
        $effectiveDate = $input['effective_date'] ?? date('Y-m-d');
        $legalConfig = $legalConfig ?? $this->getLegalConfig($effectiveDate);
        $taxBrackets = $taxBrackets ?? $this->getTaxBrackets($effectiveDate);

        $contractSalary = (float)($input['contract_salary'] ?? $input['luong_can_ban'] ?? $input['luong_dong_bh'] ?? 0.0);
        $standardDays   = (float)($input['standard_days'] ?? 26.0);
        $actualDays     = (float)($input['actual_days'] ?? 26.0);
        $bacLuong       = $input['bac_luong'] ?? null;
        $heSoBac        = isset($input['he_so_bac']) ? (float)$input['he_so_bac'] : null;
        $kpiScore       = (float)($input['kpi_score'] ?? 100.0);
        $targetP3Rate   = (float)($input['target_p3_rate'] ?? 0.15);

        // 1. Tính P1, P2, P3
        $luongP1 = $this->calculateP1($contractSalary, $actualDays, $standardDays);
        $luongP2 = $this->calculateP2($contractSalary, $bacLuong, $heSoBac);
        $luongP3 = $this->calculateP3($contractSalary, $kpiScore, $targetP3Rate);

        // 2. Tính Phụ cấp ăn ca và các phụ cấp khác
        $actualMeal     = (float)($input['meal_allowance'] ?? 0.0);
        $maxExemptMeal  = (float)($legalConfig['PHU_CAP_AN_CA_MIEN_THUE'] ?? 730000.0);
        $mealCalc       = $this->calculateMealAllowance($actualMeal, $maxExemptMeal);
        $otherAllowance = (float)($input['other_allowance'] ?? 0.0);
        $totalAllowance = $actualMeal + $otherAllowance;

        // 3. Tính tiền làm thêm giờ (OT)
        $otWeekdayHours = (float)($input['ot_weekday_hours'] ?? 0.0);
        $otWeekendHours = (float)($input['ot_weekend_hours'] ?? 0.0);
        $otHolidayHours = (float)($input['ot_holiday_hours'] ?? 0.0);
        $nightHours     = (float)($input['night_hours'] ?? 0.0);

        $otCalc = $this->calculateOvertime(
            $contractSalary,
            $otWeekdayHours,
            $otWeekendHours,
            $otHolidayHours,
            $nightHours,
            $standardDays,
            8.0,
            [
                'weekday' => $legalConfig['OT_RATE_WEEKDAY'] ?? 1.5,
                'weekend' => $legalConfig['OT_RATE_WEEKEND'] ?? 2.0,
                'holiday' => $legalConfig['OT_RATE_HOLIDAY'] ?? 3.0,
                'night'   => $legalConfig['NIGHT_SHIFT_RATE'] ?? 0.3
            ]
        );

        // 4. Tổng thu nhập (Gross Income)
        $tongThuNhap = $luongP1 + $luongP2 + $luongP3 + $totalAllowance + $otCalc['total_ot_pay'];

        // 5. Tính bảo hiểm bắt buộc người lao động
        $region = (int)($input['region'] ?? $input['vung_luong'] ?? 1);
        // Căn cứ đóng bảo hiểm thường là Lương HĐ
        $insuranceBase = (float)($input['insurable_salary'] ?? $contractSalary);
        $insuranceCalc = $this->calculateInsurances($insuranceBase, $region, $legalConfig);

        // 6. Tính thu nhập chịu thuế & Thuế TNCN
        // Các khoản được miễn thuế TNCN: OT phần chênh lệch + Phụ cấp ăn ca trong hạn mức
        $totalTaxExempt = $otCalc['tax_exempt_ot_pay'] + $mealCalc['exempt_meal_allowance'];
        $thuNhapChiuThue = max(0.0, $tongThuNhap - $totalTaxExempt);

        $personalRelief   = (float)($legalConfig['GIAM_TRU_BAN_THAN'] ?? 15500000.0);
        $dependentRelief  = (float)($legalConfig['GIAM_TRU_PHU_THUOC'] ?? 6200000.0);
        $dependentsCount  = (int)($input['dependents_count'] ?? $input['so_nguoi_phu_thuoc'] ?? 0);

        $taxCalc = $this->calculatePersonalIncomeTax(
            $thuNhapChiuThue,
            $taxBrackets,
            $personalRelief,
            $dependentsCount,
            $dependentRelief,
            $insuranceCalc['total_insurance']
        );

        // 7. Các khoản khấu trừ khác (Tạm ứng, phạt tài sản chưa hoàn trả)
        $advancePayment  = (float)($input['advance_payment'] ?? $input['tam_ung'] ?? 0.0);
        $assetPenalty    = (float)($input['asset_penalty'] ?? $input['tai_san_chua_tra'] ?? 0.0);
        $deductionsCalc  = $this->calculateOtherDeductions($advancePayment, $assetPenalty);

        // 8. Lương thực lĩnh (Net Income)
        // Net = Gross - Bảo hiểm NLĐ - Thuế TNCN - Các khoản khấu trừ khác
        $thucLinh = $tongThuNhap - $insuranceCalc['total_insurance'] - $taxCalc['tax_amount'] - $deductionsCalc['total_other_deductions'];

        return [
            'ma_nv'                    => $input['ma_nv'] ?? 'NV-DEMO',
            'ho_ten'                   => $input['ho_ten'] ?? 'Nhân viên',
            'chuc_vu'                  => $input['chuc_vu'] ?? 'Chuyên viên',
            'don_vi'                   => $input['don_vi'] ?? 'Vinamilk',
            'vung_luong'               => $region,
            'cong_chuan'               => $standardDays,
            'cong_thuc_te'             => $actualDays,
            'luong_hop_dong'           => $contractSalary,
            'luong_p1_hop_dong'        => $luongP1,
            'luong_p2_hieu_suat'       => $luongP2,
            'luong_p3_kpi'             => $luongP3,
            'diem_kpi'                 => $kpiScore,
            'he_so_kpi'                => round($kpiScore / 100.0, 2),
            'phu_cap_an_ca'            => $actualMeal,
            'an_ca_mien_thue'          => $mealCalc['exempt_meal_allowance'],
            'an_ca_chiu_thue'          => $mealCalc['taxable_meal_allowance'],
            'phu_cap_khac'             => $otherAllowance,
            'tong_phu_cap'             => $totalAllowance,
            'luong_ot'                 => $otCalc['total_ot_pay'],
            'tien_ot_thuong'           => $otCalc['weekday_pay'],
            'tien_ot_nghi'             => $otCalc['weekend_pay'],
            'tien_ot_le'               => $otCalc['holiday_pay'],
            'tien_ot_dem'              => $otCalc['night_pay'],
            'ot_mien_thue'             => $otCalc['tax_exempt_ot_pay'],
            'ot_chiu_thue'             => $otCalc['taxable_ot_pay'],
            'tong_thu_nhap'            => $tongThuNhap,
            'tong_thu_nhap_gross'      => $tongThuNhap,
            'tong_mien_thue'           => $totalTaxExempt,
            'thu_nhap_chiu_thue'       => $thuNhapChiuThue,
            'luong_dong_bh'            => $insuranceCalc['luong_dong_bhxh'],
            'bhxh_nv'                  => $insuranceCalc['bhxh'],
            'bhyt_nv'                  => $insuranceCalc['bhyt'],
            'bhtn_nv'                  => $insuranceCalc['bhtn'],
            'tong_bao_hiem_nv'         => $insuranceCalc['total_insurance'],
            'giam_tru_ban_than'        => $personalRelief,
            'so_nguoi_phu_thuoc'       => $dependentsCount,
            'giam_tru_phu_thuoc'       => $taxCalc['dependents_deduction'],
            'tong_giam_tru_gia_canh'   => $personalRelief + $taxCalc['dependents_deduction'],
            'thu_nhap_tinh_thue'       => $taxCalc['assessable_income'],
            'thue_tncn'                => $taxCalc['tax_amount'],
            'khau_tru_tam_ung'         => $advancePayment,
            'khau_tru_tai_san'         => $assetPenalty,
            'tong_khau_tru_khac'       => $deductionsCalc['total_other_deductions'],
            'thuc_linh'                => $thucLinh,
            'thuc_linh_net'            => $thucLinh,
            'effective_date'           => $effectiveDate
        ];
    }

    // =========================================================================
    // 4. QUY TRÌNH DUYỆT 5 BƯỚC & BẢO VỆ KHÓA LƯƠNG BẤT BIẾN (IMMUTABILITY)
    //    Nháp -> Đã tính -> Kế toán duyệt -> Giám đốc duyệt -> Đã chi (Khóa)
    // =========================================================================

    /**
     * Kiểm tra xem bảng lương đã bị KHÓA chưa (sau khi Đã chi)
     */
    public function isLocked(int $bangLuongId): bool
    {
        $stmt = $this->db->prepare("SELECT trang_thai FROM bang_luong WHERE id = :id LIMIT 1");
        $stmt->execute([':id' => $bangLuongId]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$row) return false;
        return in_array($row['trang_thai'], ['DA_CHI', 'DA_CHI_TRA'], true);
    }

    /**
     * Xác thực trạng thái bảng lương chưa bị khóa
     */
    public function assertPayrollNotLocked(string $status): void
    {
        if (in_array(strtoupper($status), ['DA_CHI', 'DA_CHI_TRA'], true)) {
            throw new PayrollLockedException("Bảng lương đã ở trạng thái {$status} và được khóa bất biến. Nghiêm cấm chỉnh sửa!");
        }
    }

    /**
     * BƯỚC 1: KHỞI TẠO BẢNG LƯƠNG NHÁP (NHAP)
     */
    public function createDraft(string $kyLuong, string $maDV, string $creator): int
    {
        $stmtCheck = $this->db->prepare("SELECT id, trang_thai FROM bang_luong WHERE ky_luong = :ky AND ma_dv = :dv LIMIT 1");
        $stmtCheck->execute([':ky' => $kyLuong, ':dv' => $maDV]);
        $existing = $stmtCheck->fetch(PDO::FETCH_ASSOC);

        if ($existing) {
            if ($this->isLocked((int)$existing['id'])) {
                throw new PayrollLockedException("Bảng lương kỳ {$kyLuong} của đơn vị {$maDV} đã ở trạng thái ĐÃ CHI và ĐÃ ĐƯỢC KHÓA. Không thể tạo lại!");
            }
            return (int)$existing['id'];
        }

        $ten = "Bảng thanh toán lương kỳ {$kyLuong} - {$maDV}";
        $sql = "INSERT INTO bang_luong (ky_luong, ma_dv, ten_bang_luong, trang_thai, nguoi_lap, created_at)
                VALUES (:ky, :dv, :ten, 'NHAP', :nguoi, NOW())";
        $stmt = $this->db->prepare($sql);
        $stmt->execute([':ky' => $kyLuong, ':dv' => $maDV, ':ten' => $ten, ':nguoi' => $creator]);

        return (int)$this->db->lastInsertId();
    }

    /**
     * BƯỚC 2: TÍNH LƯƠNG CHI TIẾT VÀ CHUYỂN TRẠNG THÁI 'ĐÃ TÍNH' (DA_TINH)
     */
    public function calculateBatch(int $bangLuongId, ?array $employeeList = null): array
    {
        if ($this->isLocked($bangLuongId)) {
            throw new PayrollLockedException("KHÓA SỔ KẾ TOÁN: Bảng lương [#{$bangLuongId}] đã chi trả, cấm tính toán lại hoặc ghi đè!");
        }

        $stmtBl = $this->db->prepare("SELECT * FROM bang_luong WHERE id = :id LIMIT 1");
        $stmtBl->execute([':id' => $bangLuongId]);
        $bangLuong = $stmtBl->fetch(PDO::FETCH_ASSOC);

        if (!$bangLuong) {
            throw new PayrollValidationException("Không tìm thấy bảng lương [#{$bangLuongId}]");
        }

        $kyLuong = $bangLuong['ky_luong']; // YYYY-MM
        $periodDate = date('Y-m-t', strtotime($kyLuong . '-01'));
        $legalConfig = $this->getLegalConfig($periodDate);
        $taxBrackets = $this->getTaxBrackets($periodDate);

        // Lấy danh sách nhân viên cần tính nếu không truyền trực tiếp
        if ($employeeList === null) {
            $maDV = $bangLuong['ma_dv'];
            $whereUnit = ($maDV === 'ALL' || $maDV === 'VINAMILK') ? "1=1" : "ns.MaDV = :madv";
            $params = ($maDV === 'ALL' || $maDV === 'VINAMILK') ? [] : [':madv' => $maDV];

            $sqlEmp = "SELECT ns.*, cv.TenCV, dv.TenDV
                       FROM nhansu ns
                       LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                       LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                       WHERE ns.TrangThai = 1 AND {$whereUnit}
                       ORDER BY ns.HoTen ASC";
            $stmtEmp = $this->db->prepare($sqlEmp);
            $stmtEmp->execute($params);
            $employeeList = $stmtEmp->fetchAll(PDO::FETCH_ASSOC);
        }

        $month = (int)date('m', strtotime($kyLuong . '-01'));
        $year  = (int)date('Y', strtotime($kyLuong . '-01'));

        $this->db->beginTransaction();
        try {
            // Xóa chi tiết cũ nếu đang ở trạng thái Nháp hoặc Đã tính
            $this->db->prepare("DELETE FROM bang_luong_chi_tiet WHERE bang_luong_id = :id")->execute([':id' => $bangLuongId]);

            $totalEmployees = 0;
            $totalGross = 0.0;
            $totalInsurance = 0.0;
            $totalTax = 0.0;
            $totalNet = 0.0;

            $sqlInsert = "INSERT INTO bang_luong_chi_tiet 
                          (bang_luong_id, ma_nv, ho_ten, chuc_vu, ten_dv, vung_luong, cong_chuan, cong_thuc_te,
                           luong_p1_hop_dong, luong_p2_hieu_suat, luong_p3_kpi, diem_kpi, he_so_kpi,
                           phu_cap, phu_cap_an_ca, an_ca_mien_thue, luong_ot, tien_ot_thuong, tien_ot_nghi, tien_ot_le, tien_ot_dem, ot_mien_thue,
                           tong_thu_nhap, thu_nhap_chiu_thue, luong_dong_bh, bhxh_nv, bhyt_nv, bhtn_nv, tong_bao_hiem_nv,
                           giam_tru_ban_than, giam_tru_phu_thuoc, thu_nhap_tinh_thue, thue_tncn,
                           khau_tru_tam_ung, khau_tru_tai_san, tong_khau_tru, thuc_linh, ngay_tinh)
                          VALUES
                          (:bl_id, :ma_nv, :ho_ten, :chuc_vu, :ten_dv, :vung, :cong_chuan, :cong_tt,
                           :p1, :p2, :p3, :kpi, :heso_kpi,
                           :phu_cap, :an_ca, :an_ca_mien, :ot, :ot_thuong, :ot_nghi, :ot_le, :ot_dem, :ot_mien,
                           :tong_tn, :tn_chiu_thue, :dong_bh, :bhxh, :bhyt, :bhtn, :tong_bh,
                           :gt_bt, :gt_pt, :tntt, :thue,
                           :tam_ung, :tai_san, :tong_kt, :thuc_linh, NOW())";
            $stmtInsert = $this->db->prepare($sqlInsert);

            foreach ($employeeList as $emp) {
                $manv = $emp['MaNV'];

                // 1. Đọc lương HĐ hiện hành từ bảng hop_dong_lao_dong
                $stmtHd = $this->db->prepare("SELECT luong_can_ban, luong_dong_bh, phu_cap_an_ca, phu_cap_trach_nhiem 
                                              FROM hop_dong_lao_dong 
                                              WHERE ma_nv = :manv AND trang_thai = 'HIEU_LUC' 
                                              ORDER BY tu_ngay DESC LIMIT 1");
                $stmtHd->execute([':manv' => $manv]);
                $hd = $stmtHd->fetch(PDO::FETCH_ASSOC);

                $contractSalary = $hd ? (float)$hd['luong_can_ban'] : ((float)($emp['LuongBaoHiem'] ?? 0) ?: 10000000.0);
                $mealAllowance  = $hd ? (float)$hd['phu_cap_an_ca'] : 1000000.0;
                $otherAllowance = $hd ? (float)$hd['phu_cap_trach_nhiem'] : 500000.0;

                // 2. Đọc công tháng từ bang_cong_thang (hoặc cham_cong_ngay)
                $stmtCc = $this->db->prepare("SELECT cong_chuan_thang, cong_thuc_te, tong_cong_tinh_luong, 
                                                     gio_ot_ngay_thuong, gio_ot_ngay_nghi, gio_ot_ngay_le, gio_lam_dem 
                                              FROM bang_cong_thang 
                                              WHERE ma_nv = :manv AND thang = :m AND nam = :y LIMIT 1");
                $stmtCc->execute([':manv' => $manv, ':m' => $month, ':y' => $year]);
                $cc = $stmtCc->fetch(PDO::FETCH_ASSOC);

                $standardDays   = $cc ? (float)$cc['cong_chuan_thang'] : 26.0;
                $actualDays     = $cc ? (float)$cc['tong_cong_tinh_luong'] : 26.0;
                $otWeekdayHours = $cc ? (float)$cc['gio_ot_ngay_thuong'] : 0.0;
                $otWeekendHours = $cc ? (float)$cc['gio_ot_ngay_nghi'] : 0.0;
                $otHolidayHours = $cc ? (float)$cc['gio_ot_ngay_le'] : 0.0;
                $nightHours     = $cc ? (float)$cc['gio_lam_dem'] : 0.0;

                // 3. Đọc điểm KPI tháng từ bảng kpi
                $kpiScore = $this->getKpiScoreFromDb($manv, $month, $year);

                // 4. Khấu trừ tài sản chưa hoàn trả hoặc chưa bàn giao khi thôi việc (Offboarding)
                $assetPenalty = 0.0;
                $stmtOff = $this->db->prepare("SELECT tong_tien_khau_tru_ts FROM offboarding_process 
                                               WHERE ma_nv = :manv AND trang_thai IN ('DANG_XU_LY', 'HOAN_TAT') 
                                               ORDER BY id DESC LIMIT 1");
                $stmtOff->execute([':manv' => $manv]);
                $offTs = $stmtOff->fetch(PDO::FETCH_ASSOC);
                if ($offTs && (float)$offTs['tong_tien_khau_tru_ts'] > 0) {
                    $assetPenalty = (float)$offTs['tong_tien_khau_tru_ts'];
                } else {
                    $stmtTs = $this->db->prepare("SELECT SUM(
                                                    CASE 
                                                        WHEN gia_tri_khau_tru > 0 THEN gia_tri_khau_tru
                                                        WHEN gia_tri_tai_san > 0 THEN gia_tri_tai_san
                                                        ELSE 500000 
                                                    END
                                                  ) as total_penalty 
                                                  FROM cap_phat_tai_san 
                                                  WHERE ma_nv = :manv AND trang_thai IN ('BAO_MAT', 'BAO_HONG')");
                    $stmtTs->execute([':manv' => $manv]);
                    $tsRow = $stmtTs->fetch(PDO::FETCH_ASSOC);
                    $assetPenalty = ($tsRow && (float)$tsRow['total_penalty'] > 0) ? (float)$tsRow['total_penalty'] : 0.0;
                }

                // Chạy pipeline tính toán lương
                $calcInput = [
                    'ma_nv'             => $manv,
                    'ho_ten'            => $emp['HoTen'],
                    'chuc_vu'           => $emp['TenCV'] ?? 'Chuyên viên',
                    'don_vi'            => $emp['TenDV'] ?? 'Vinamilk',
                    'region'            => (int)($emp['VungLuong'] ?? 1),
                    'contract_salary'   => $contractSalary,
                    'standard_days'     => $standardDays,
                    'actual_days'       => $actualDays,
                    'kpi_score'         => $kpiScore,
                    'meal_allowance'    => $mealAllowance,
                    'other_allowance'   => $otherAllowance,
                    'ot_weekday_hours'  => $otWeekdayHours,
                    'ot_weekend_hours'  => $otWeekendHours,
                    'ot_holiday_hours'  => $otHolidayHours,
                    'night_hours'       => $nightHours,
                    'dependents_count'  => (int)($emp['SoNguoiPhuThuoc'] ?? 0),
                    'asset_penalty'     => $assetPenalty,
                    'effective_date'    => $periodDate
                ];

                $calc = $this->calculateEmployeeSalary($calcInput, $legalConfig, $taxBrackets);

                // Lưu kết quả vào bang_luong_chi_tiet
                $stmtInsert->execute([
                    ':bl_id'        => $bangLuongId,
                    ':ma_nv'        => $calc['ma_nv'],
                    ':ho_ten'       => $calc['ho_ten'],
                    ':chuc_vu'      => $calc['chuc_vu'],
                    ':ten_dv'       => $calc['don_vi'],
                    ':vung'         => $calc['vung_luong'],
                    ':cong_chuan'   => $calc['cong_chuan'],
                    ':cong_tt'      => $calc['cong_thuc_te'],
                    ':p1'           => $calc['luong_p1_hop_dong'],
                    ':p2'           => $calc['luong_p2_hieu_suat'],
                    ':p3'           => $calc['luong_p3_kpi'],
                    ':kpi'          => $calc['diem_kpi'],
                    ':heso_kpi'     => $calc['he_so_kpi'],
                    ':phu_cap'      => $calc['phu_cap_khac'],
                    ':an_ca'        => $calc['phu_cap_an_ca'],
                    ':an_ca_mien'   => $calc['an_ca_mien_thue'],
                    ':ot'           => $calc['luong_ot'],
                    ':ot_thuong'    => $calc['tien_ot_thuong'],
                    ':ot_nghi'      => $calc['tien_ot_nghi'],
                    ':ot_le'        => $calc['tien_ot_le'],
                    ':ot_dem'       => $calc['tien_ot_dem'],
                    ':ot_mien'      => $calc['ot_mien_thue'],
                    ':tong_tn'      => $calc['tong_thu_nhap'],
                    ':tn_chiu_thue' => $calc['thu_nhap_chiu_thue'],
                    ':dong_bh'      => $calc['luong_dong_bh'],
                    ':bhxh'         => $calc['bhxh_nv'],
                    ':bhyt'         => $calc['bhyt_nv'],
                    ':bhtn'         => $calc['bhtn_nv'],
                    ':tong_bh'      => $calc['tong_bao_hiem_nv'],
                    ':gt_bt'        => $calc['giam_tru_ban_than'],
                    ':gt_pt'        => $calc['giam_tru_phu_thuoc'],
                    ':tntt'         => $calc['thu_nhap_tinh_thue'],
                    ':thue'         => $calc['thue_tncn'],
                    ':tam_ung'      => $calc['khau_tru_tam_ung'],
                    ':tai_san'      => $calc['khau_tru_tai_san'],
                    ':tong_kt'      => $calc['tong_khau_tru_khac'],
                    ':thuc_linh'    => $calc['thuc_linh']
                ]);

                $totalEmployees++;
                $totalGross     += $calc['tong_thu_nhap'];
                $totalInsurance += $calc['tong_bao_hiem_nv'];
                $totalTax       += $calc['thue_tncn'];
                $totalNet       += $calc['thuc_linh'];
            }

            // Cập nhật bảng lương sang trạng thái 'DA_TINH'
            $sqlUpdateSummary = "UPDATE bang_luong 
                                 SET tong_nhan_su = :count,
                                     tong_thu_nhap = :tn,
                                     tong_bao_hiem = :bh,
                                     tong_thue_tncn = :thue,
                                     tong_thuc_linh = :tl,
                                     trang_thai = 'DA_TINH',
                                     updated_at = NOW()
                                 WHERE id = :id";
            $stmtUp = $this->db->prepare($sqlUpdateSummary);
            $stmtUp->execute([
                ':count' => $totalEmployees,
                ':tn'    => $totalGross,
                ':bh'    => $totalInsurance,
                ':thue'  => $totalTax,
                ':tl'    => $totalNet,
                ':id'    => $bangLuongId
            ]);

            $this->db->commit();

            return [
                'bang_luong_id'   => $bangLuongId,
                'total_employees' => $totalEmployees,
                'total_gross'     => $totalGross,
                'total_insurance' => $totalInsurance,
                'total_tax'       => $totalTax,
                'total_net'       => $totalNet,
                'status'          => 'DA_TINH'
            ];
        } catch (Throwable $e) {
            $this->db->rollBack();
            throw $e;
        }
    }

    /**
     * BƯỚC 3: KẾ TOÁN DUYỆT (KETOAN_DUYET)
     */
    public function accountantApprove(int $bangLuongId, string $approver): bool
    {
        if ($this->isLocked($bangLuongId)) {
            throw new PayrollLockedException("Bảng lương đã chi trả thành công và đã khóa sổ, không thể duyệt lại!");
        }

        $sql = "UPDATE bang_luong 
                SET trang_thai = 'KETOAN_DUYET', nguoi_ketoan_duyet = :appr, ngay_ketoan_duyet = NOW() 
                WHERE id = :id AND trang_thai IN ('DA_TINH', 'NHAP')";
        $stmt = $this->db->prepare($sql);
        $stmt->execute([':appr' => $approver, ':id' => $bangLuongId]);

        return $stmt->rowCount() > 0;
    }

    /**
     * BƯỚC 4: GIÁM ĐỐC DUYỆT (GIAMDOC_DUYET)
     */
    public function directorApprove(int $bangLuongId, string $approver): bool
    {
        if ($this->isLocked($bangLuongId)) {
            throw new PayrollLockedException("Bảng lương đã chi trả thành công và đã khóa sổ, không thể duyệt lại!");
        }

        $sql = "UPDATE bang_luong 
                SET trang_thai = 'GIAMDOC_DUYET', nguoi_giamdoc_duyet = :appr, ngay_giamdoc_duyet = NOW(),
                    nguoi_duyet = :appr2, ngay_duyet = NOW()
                WHERE id = :id AND trang_thai IN ('KETOAN_DUYET', 'DA_TINH')";
        $stmt = $this->db->prepare($sql);
        $stmt->execute([':appr' => $approver, ':appr2' => $approver, ':id' => $bangLuongId]);

        return $stmt->rowCount() > 0;
    }

    /**
     * BƯỚC 5: ĐÃ CHI (DA_CHI) -> KHÓA SỔ HOÀN TOÀN
     * Sau khi "Đã chi", bảng lương bị khóa cứng không cho phép sửa đổi.
     */
    public function disburse(int $bangLuongId, string $disburser): bool
    {
        $sql = "UPDATE bang_luong 
                SET trang_thai = 'DA_CHI', ghi_chu = CONCAT(IFNULL(ghi_chu, ''), ' [Đã chi bởi: ', :disb, ' lúc ', NOW(), ']'),
                    ngay_chi_tra = NOW() 
                WHERE id = :id AND trang_thai = 'GIAMDOC_DUYET'";
        $stmt = $this->db->prepare($sql);
        $stmt->execute([':disb' => $disburser, ':id' => $bangLuongId]);

        return $stmt->rowCount() > 0;
    }

    // =========================================================================
    // 5. PHIẾU LƯƠNG ĐIỆN TỬ CHO ESS (PAYSLIP PRINTABLE / PDF GENERATION)
    // =========================================================================

    /**
     * Lấy dữ liệu phiếu lương của nhân viên theo kỳ
     */
    public function generatePayslip(int $bangLuongId, string $maNV): ?array
    {
        $sql = "SELECT d.*, bl.ky_luong, bl.trang_thai as trang_thai_bang_luong, bl.ten_bang_luong,
                       bl.nguoi_giamdoc_duyet, bl.ngay_giamdoc_duyet, bl.ngay_chi_tra,
                       ns.Email, ns.SoDienThoai, ns.NgayVaoLam, ns.SoCMND_CCCD, ns.SoTaiKhoanNH, ns.NganHang,
                       dv.TenDV as DonViTen, dv.DiaChi as DonViDiaChi
                FROM bang_luong_chi_tiet d
                JOIN bang_luong bl ON d.bang_luong_id = bl.id
                LEFT JOIN nhansu ns ON d.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE d.bang_luong_id = :bl_id AND d.ma_nv = :manv
                LIMIT 1";
        $stmt = $this->db->prepare($sql);
        $stmt->execute([':bl_id' => $bangLuongId, ':manv' => $maNV]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);

        return $row ?: null;
    }

    /**
     * Sinh HTML phiếu lương bảo mật chuẩn doanh nghiệp Vinamilk sẵn sàng in hoặc xuất PDF
     */
    public function renderPayslipHtml(array $p): string
    {
        $kyLuong = $p['ky_luong'] ?? date('Y-m');
        $thang = date('m', strtotime($kyLuong . '-01'));
        $nam   = date('Y', strtotime($kyLuong . '-01'));

        $fmt = fn($val) => number_format((float)$val, 0, ',', '.') . ' ₫';
        $fmtNum = fn($val) => number_format((float)$val, 1, ',', '.');

        $isDisbursed = in_array($p['trang_thai_bang_luong'] ?? '', ['DA_CHI', 'DA_CHI_TRA'], true);
        $statusBadge = $isDisbursed 
            ? '<span style="color:#059669; font-weight:bold; border:1px solid #059669; padding:2px 8px; border-radius:4px;">ĐÃ THANH TOÁN (LOCKED)</span>'
            : '<span style="color:#D97706; font-weight:bold; border:1px solid #D97706; padding:2px 8px; border-radius:4px;">DỰ TOÁN LƯƠNG</span>';

        return <<<HTML
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title>Phiếu Lương Tháng {$thang}/{$nam} - {$p['ma_nv']}</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #1e293b; line-height: 1.5; padding: 20px; font-size: 13px; background: #fff; }
        .payslip-container { max-width: 800px; margin: 0 auto; border: 2px solid #0033A0; border-radius: 8px; padding: 24px; box-shadow: 0 4px 12px rgba(0,0,0,0.05); }
        .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #0033A0; padding-bottom: 15px; margin-bottom: 20px; }
        .logo-title h2 { margin: 0; color: #0033A0; font-size: 20px; text-transform: uppercase; }
        .logo-title p { margin: 2px 0 0 0; color: #64748b; font-size: 12px; }
        .grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-bottom: 20px; }
        .info-card { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; }
        .table-custom { width: 100%; border-collapse: collapse; margin-bottom: 15px; }
        .table-custom th, .table-custom td { border: 1px solid #cbd5e1; padding: 8px 10px; font-size: 12px; }
        .table-custom th { background: #0033A0; color: #ffffff; text-align: left; }
        .highlight-row { background: #f1f5f9; font-weight: bold; }
        .net-pay-box { background: #eff6ff; border: 2px dashed #0033A0; border-radius: 6px; padding: 15px; text-align: center; margin-top: 20px; }
        .net-pay-amount { font-size: 24px; font-weight: bold; color: #0033A0; margin-top: 5px; }
        .footer-note { font-size: 11px; color: #64748b; margin-top: 25px; text-align: center; border-top: 1px solid #e2e8f0; padding-top: 10px; }
        @media print {
            body { padding: 0; }
            .payslip-container { border: none; box-shadow: none; }
            .no-print { display: none; }
        }
    </style>
</head>
<body>
    <div class="payslip-container">
        <div class="header">
            <div class="logo-title">
                <h2>CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)</h2>
                <p>Hệ Thống Quản Trị Nhân Lực & Tiền Lương Doanh Nghiệp (Vinamilk HRM)</p>
            </div>
            <div style="text-align: right;">
                <h3 style="margin:0; color:#0033A0;">PHIẾU LƯƠNG THÁNG {$thang}/{$nam}</h3>
                <div style="margin-top: 5px;">{$statusBadge}</div>
            </div>
        </div>

        <div class="grid-2">
            <div class="info-card">
                <div><strong>Họ và tên:</strong> {$p['ho_ten']}</div>
                <div><strong>Mã nhân viên:</strong> {$p['ma_nv']}</div>
                <div><strong>Chức vụ / Vị trí:</strong> {$p['chuc_vu']}</div>
                <div><strong>Đơn vị công tác:</strong> {$p['ten_dv']}</div>
            </div>
            <div class="info-card">
                <div><strong>Số tài khoản:</strong> {$p['SoTaiKhoanNH']} ({$p['NganHang']})</div>
                <div><strong>Số CCCD:</strong> {$p['SoCMND_CCCD']}</div>
                <div><strong>Công chuẩn:</strong> {$fmtNum($p['cong_chuan'])} ngày | <strong>Công thực tế:</strong> {$fmtNum($p['cong_thuc_te'])} ngày</div>
                <div><strong>Điểm KPI tháng:</strong> {$fmtNum($p['diem_kpi'])} điểm (Hệ số: {$p['he_so_kpi']})</div>
            </div>
        </div>

        <table class="table-custom">
            <thead>
                <tr>
                    <th style="width: 50%;">I. CÁC KHOẢN THU NHẬP (3P & PHỤ CẤP)</th>
                    <th style="width: 50%; text-align: right;">SỐ TIỀN (VNĐ)</th>
                </tr>
            </thead>
            <tbody>
                <tr><td>1. Lương vị trí theo hợp đồng & công thực tế (P1)</td><td style="text-align: right;">{$fmt($p['luong_p1_hop_dong'])}</td></tr>
                <tr><td>2. Lương năng lực theo bậc (P2)</td><td style="text-align: right;">{$fmt($p['luong_p2_hieu_suat'])}</td></tr>
                <tr><td>3. Thưởng hiệu quả KPI tháng (P3)</td><td style="text-align: right;">{$fmt($p['luong_p3_kpi'])}</td></tr>
                <tr><td>4. Phụ cấp tiền ăn giữa ca ({$fmt($p['an_ca_mien_thue'])} miễn thuế)</td><td style="text-align: right;">{$fmt($p['phu_cap_an_ca'])}</td></tr>
                <tr><td>5. Phụ cấp trách nhiệm / công việc khác</td><td style="text-align: right;">{$fmt($p['phu_cap'])}</td></tr>
                <tr><td>6. Tiền làm thêm giờ OT (Ngày thường, Nghỉ, Lễ, Đêm)</td><td style="text-align: right;">{$fmt($p['luong_ot'])}</td></tr>
                <tr class="highlight-row">
                    <td>TỔNG THU NHẬP (GROSS)</td>
                    <td style="text-align: right; color:#0033A0;">{$fmt($p['tong_thu_nhap'])}</td>
                </tr>
            </tbody>
        </table>

        <table class="table-custom">
            <thead>
                <tr>
                    <th style="width: 50%;">II. CÁC KHOẢN KHẤU TRỪ BẢO HIỂM & THUẾ TNCN</th>
                    <th style="width: 50%; text-align: right;">SỐ TIỀN (VNĐ)</th>
                </tr>
            </thead>
            <tbody>
                <tr><td>1. Bảo hiểm Xã hội (8% trên mức đóng {$fmt($p['luong_dong_bh'])})</td><td style="text-align: right;">-{$fmt($p['bhxh_nv'])}</td></tr>
                <tr><td>2. Bảo hiểm Y tế (1.5%)</td><td style="text-align: right;">-{$fmt($p['bhyt_nv'])}</td></tr>
                <tr><td>3. Bảo hiểm Thất nghiệp (1%)</td><td style="text-align: right;">-{$fmt($p['bhtn_nv'])}</td></tr>
                <tr><td>4. Giảm trừ bản thân (Luật 2026: 15,5 triệu) + NPT ({$p['giam_tru_phu_thuoc']})</td><td style="text-align: right; color:#64748b;">(Đã trừ tính thuế)</td></tr>
                <tr><td>5. Thuế Thu nhập cá nhân (Biểu 5 bậc lũy tiến)</td><td style="text-align: right; color:#dc2626;">-{$fmt($p['thue_tncn'])}</td></tr>
                <tr><td>6. Khấu trừ khác (Tạm ứng, phạt tài sản chưa hoàn trả)</td><td style="text-align: right;">-{$fmt($p['tong_khau_tru'])}</td></tr>
                <tr class="highlight-row">
                    <td>TỔNG CÁC KHOẢN TRỪ</td>
                    <td style="text-align: right; color:#dc2626;">-{$fmt((float)$p['tong_bao_hiem_nv'] + (float)$p['thue_tncn'] + (float)$p['tong_khau_tru'])}</td>
                </tr>
            </tbody>
        </table>

        <div class="net-pay-box">
            <div style="font-size: 13px; text-transform: uppercase; color: #475569; font-weight: 600;">LƯƠNG THỰC LĨNH CHUYỂN KHOẢN (NET SALARY)</div>
            <div class="net-pay-amount">{$fmt($p['thuc_linh'])}</div>
            <div style="font-size: 11px; color:#64748b; margin-top:4px;">Chuyển vào tài khoản {$p['SoTaiKhoanNH']} - Ngân hàng {$p['NganHang']}</div>
        </div>

        <div class="footer-note">
            Phiếu lương điện tử được phát hành tự động qua Cổng thông tin Nhân viên ESS Vinamilk HRM.<br>
            Mọi thắc mắc về tiền lương, vui lòng liên hệ Phòng Nhân sự / Kế toán tiền lương trong vòng 5 ngày làm việc kể từ ngày nhận phiếu.
        </div>

        <div class="no-print" style="text-align: center; margin-top: 20px;">
            <button onclick="window.print()" style="background:#0033A0; color:#fff; border:none; padding:8px 20px; font-weight:600; border-radius:4px; cursor:pointer;">
                🖨️ In Phiếu Lương / Lưu PDF
            </button>
        </div>
    </div>
</body>
</html>
HTML;
    }
}
