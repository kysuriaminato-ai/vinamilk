<?php

namespace Tests\Unit;

use Brick\Math\BigDecimal;
use Brick\Math\RoundingMode;
use Carbon\Carbon;
use PHPUnit\Framework\TestCase;

/**
 * ====================================================================================================
 * BỘ KIỂM THỬ ĐƠN VỊ (UNIT TEST SUITE) - CÁC HÀM TÍNH TOÁN CỐT LÕI HRIS VINAMILK
 * ====================================================================================================
 * Kiểm tra tính đúng đắn của các thuật toán:
 * 1. Cơ chế tính lương 3P (P1 Vị trí + P2 Năng lực theo ngày công + P3 Hiệu suất theo KPI)
 * 2. Biểu thuế thu nhập cá nhân (TNCN) 7 bậc lũy tiến từng phần theo Luật Thuế TNCN
 * 3. Đơn giá làm thêm giờ (OT 150%, 200%, 300%) và phụ cấp làm đêm (+30%) theo Điều 98 BLLĐ
 * 4. Thuật toán tính tuổi & tháng nghỉ hưu theo lộ trình Nghị định 135/2020/NĐ-CP
 * ====================================================================================================
 */
class HrCalculationUnitTest extends TestCase
{
    // ================================================================================================
    // 1. UNIT TEST: TÍNH LƯƠNG 3P (P1 + P2 THEO CÔNG + P3 THEO KPI)
    // ================================================================================================

    /**
     * Thuật toán tính lương 3P chuẩn Vinamilk:
     * - P1 thực hưởng = P1_chức_danh * (công_thực_tế + nghỉ_có_lương) / công_chuẩn
     * - P2 thực hưởng = P2_năng_lực * (công_thực_tế + nghỉ_có_lương) / công_chuẩn
     * - P3 hiệu suất  = P3_cơ_sở * hệ_số_kpi
     * - Tổng thu nhập = P1_thực + P2_thực + P3_thực + phụ_cấp
     */
    protected function calculate3PSalary(
        string $p1Base,
        string $p2Base,
        string $p3Base,
        float $actualDays,
        float $standardDays,
        float $kpiMultiplier,
        string $allowances = '0'
    ): array {
        $p1 = BigDecimal::of($p1Base);
        $p2 = BigDecimal::of($p2Base);
        $p3 = BigDecimal::of($p3Base);
        $allowance = BigDecimal::of($allowances);

        $workRatio = BigDecimal::of((string)$actualDays)->dividedBy(BigDecimal::of((string)$standardDays), 8, RoundingMode::HALF_UP);

        $earnedP1 = $p1->multipliedBy($workRatio)->toScale(0, RoundingMode::HALF_UP);
        $earnedP2 = $p2->multipliedBy($workRatio)->toScale(0, RoundingMode::HALF_UP);
        $earnedP3 = $p3->multipliedBy(BigDecimal::of((string)$kpiMultiplier))->toScale(0, RoundingMode::HALF_UP);

        $gross = $earnedP1->plus($earnedP2)->plus($earnedP3)->plus($allowance);

        return [
            'p1'    => (int)$earnedP1->toInt(),
            'p2'    => (int)$earnedP2->toInt(),
            'p3'    => (int)$earnedP3->toInt(),
            'gross' => (int)$gross->toInt(),
        ];
    }

    public function test_3p_salary_calculation_with_full_working_days_and_kpi_grade_a(): void
    {
        // Công nhân làm đủ 22/22 công, KPI loại A (hệ số 1.15), phụ cấp độc hại 800.000đ
        $result = $this->calculate3PSalary(
            p1Base: '10000000',
            p2Base: '4000000',
            p3Base: '3000000',
            actualDays: 22.0,
            standardDays: 22.0,
            kpiMultiplier: 1.15,
            allowances: '800000'
        );

        $this->assertEquals(10000000, $result['p1'], 'P1 phải đủ 100% khi đi làm đủ 22/22 ngày');
        $this->assertEquals(4000000, $result['p2'], 'P2 phải đủ 100% khi đi làm đủ 22/22 ngày');
        $this->assertEquals(3450000, $result['p3'], 'P3 = 3.000.000 * 1.15 = 3.450.000 VND');
        $this->assertEquals(18250000, $result['gross'], 'Tổng Gross = 10tr + 4tr + 3.45tr + 0.8tr = 18.250.000 VND');
    }

    public function test_3p_salary_calculation_with_partial_working_days(): void
    {
        // Nhân viên chỉ đi làm 11/22 ngày (50% công), KPI loại B (hệ số 1.00)
        $result = $this->calculate3PSalary(
            p1Base: '20000000',
            p2Base: '6000000',
            p3Base: '5000000',
            actualDays: 11.0,
            standardDays: 22.0,
            kpiMultiplier: 1.00,
            allowances: '0'
        );

        $this->assertEquals(10000000, $result['p1'], 'P1 giảm 50% = 10.000.000 VND');
        $this->assertEquals(3000000, $result['p2'], 'P2 giảm 50% = 3.000.000 VND');
        $this->assertEquals(5000000, $result['p3'], 'P3 giữ nguyên theo KPI đạt chuẩn B = 5.000.000 VND');
        $this->assertEquals(18000000, $result['gross']);
    }

    // ================================================================================================
    // 2. UNIT TEST: BIỂU THUẾ THU NHẬP CÁ NHÂN (TNCN) 7 BẬC LŨY TIẾN
    // ================================================================================================

    /**
     * Thuật toán tính thuế TNCN 7 bậc lũy tiến từng phần theo Nghị quyết 954/2020:
     * - Giảm trừ bản thân: 11.000.000 đ
     * - Giảm trừ mỗi người phụ thuộc: 4.400.000 đ
     * Biểu thuế:
     *   Bậc 1: Đến 5tr            -> 5%  (trừ nhanh 0)
     *   Bậc 2: Trên 5tr đến 10tr  -> 10% (trừ nhanh 250.000)
     *   Bậc 3: Trên 10tr đến 18tr -> 15% (trừ nhanh 750.000)
     *   Bậc 4: Trên 18tr đến 32tr -> 20% (trừ nhanh 1.650.000)
     *   Bậc 5: Trên 32tr đến 52tr -> 25% (trừ nhanh 3.250.000)
     *   Bậc 6: Trên 52tr đến 80tr -> 30% (trừ nhanh 5.850.000)
     *   Bậc 7: Trên 80tr          -> 35% (trừ nhanh 9.850.000)
     */
    protected function calculatePitTax(int $taxableIncome): int
    {
        if ($taxableIncome <= 0) {
            return 0;
        }

        $income = BigDecimal::of($taxableIncome);

        if ($taxableIncome <= 5000000) {
            $tax = $income->multipliedBy(BigDecimal::of('0.05'));
        } elseif ($taxableIncome <= 10000000) {
            $tax = $income->multipliedBy(BigDecimal::of('0.10'))->minus(BigDecimal::of(250000));
        } elseif ($taxableIncome <= 18000000) {
            $tax = $income->multipliedBy(BigDecimal::of('0.15'))->minus(BigDecimal::of(750000));
        } elseif ($taxableIncome <= 32000000) {
            $tax = $income->multipliedBy(BigDecimal::of('0.20'))->minus(BigDecimal::of(1650000));
        } elseif ($taxableIncome <= 52000000) {
            $tax = $income->multipliedBy(BigDecimal::of('0.25'))->minus(BigDecimal::of(3250000));
        } elseif ($taxableIncome <= 80000000) {
            $tax = $income->multipliedBy(BigDecimal::of('0.30'))->minus(BigDecimal::of(5850000));
        } else {
            $tax = $income->multipliedBy(BigDecimal::of('0.35'))->minus(BigDecimal::of(9850000));
        }

        return (int)$tax->toScale(0, RoundingMode::HALF_UP)->toInt();
    }

    public function test_pit_tax_bracket_1(): void
    {
        // Thu nhập tính thuế 4.000.000 đ -> 5% = 200.000 đ
        $this->assertEquals(200000, $this->calculatePitTax(4000000));
    }

    public function test_pit_tax_bracket_2(): void
    {
        // Thu nhập tính thuế 8.000.000 đ -> 8tr * 10% - 250k = 550.000 đ
        $this->assertEquals(550000, $this->calculatePitTax(8000000));
    }

    public function test_pit_tax_bracket_3(): void
    {
        // Thu nhập tính thuế 15.000.000 đ -> 15tr * 15% - 750k = 1.500.000 đ
        $this->assertEquals(1500000, $this->calculatePitTax(15000000));
    }

    public function test_pit_tax_bracket_4(): void
    {
        // Thu nhập tính thuế 25.000.000 đ -> 25tr * 20% - 1.65tr = 3.350.000 đ
        $this->assertEquals(3350000, $this->calculatePitTax(25000000));
    }

    public function test_pit_tax_bracket_7_high_earner(): void
    {
        // Thu nhập tính thuế 100.000.000 đ -> 100tr * 35% - 9.85tr = 25.150.000 đ
        $this->assertEquals(25150000, $this->calculatePitTax(100000000));
    }

    public function test_pit_tax_zero_or_negative_income(): void
    {
        $this->assertEquals(0, $this->calculatePitTax(0));
        $this->assertEquals(0, $this->calculatePitTax(-5000000));
    }

    // ================================================================================================
    // 3. UNIT TEST: HỆ SỐ LÀM THÊM GIỜ (OT) & PHỤ CẤP CA ĐÊM THEO ĐIỀU 98 BLLĐ
    // ================================================================================================

    /**
     * Tính tiền làm thêm giờ (OT) và phụ cấp làm việc ban đêm theo Điều 98 BLLĐ 2019:
     * - Lương giờ chuẩn = Lương_tháng / (22 ngày * 8 giờ)
     * - Giờ thường OT ban ngày: 150%
     * - Giờ nghỉ tuần ban ngày: 200%
     * - Giờ ngày lễ ban ngày:   300%
     * - Làm việc ban đêm (22:00 - 06:00): cộng thêm ít nhất 30% lương giờ chuẩn.
     * - Làm thêm giờ vào ban đêm: lương OT theo loại ngày + 30% lương giờ chuẩn + 20% lương làm thêm của ngày đó.
     */
    protected function calculateOvertimePay(
        int $monthlySalary,
        float $otNormalDayHours = 0.0,
        float $otWeekendHours = 0.0,
        float $otHolidayHours = 0.0,
        float $nightWorkHours = 0.0
    ): int {
        // 22 công chuẩn * 8h = 176h
        $hourlyRate = BigDecimal::of($monthlySalary)->dividedBy(BigDecimal::of(176), 6, RoundingMode::HALF_UP);

        // 1. OT ngày thường 150%
        $otNormalPay = $hourlyRate->multipliedBy(BigDecimal::of((string)$otNormalDayHours))->multipliedBy(BigDecimal::of('1.50'));

        // 2. OT ngày nghỉ tuần 200%
        $otWeekendPay = $hourlyRate->multipliedBy(BigDecimal::of((string)$otWeekendHours))->multipliedBy(BigDecimal::of('2.00'));

        // 3. OT ngày lễ 300%
        $otHolidayPay = $hourlyRate->multipliedBy(BigDecimal::of((string)$otHolidayHours))->multipliedBy(BigDecimal::of('3.00'));

        // 4. Phụ cấp ca đêm +30%
        $nightAllowance = $hourlyRate->multipliedBy(BigDecimal::of((string)$nightWorkHours))->multipliedBy(BigDecimal::of('0.30'));

        $total = $otNormalPay->plus($otWeekendPay)->plus($otHolidayPay)->plus($nightAllowance);

        return (int)$total->toScale(0, RoundingMode::HALF_UP)->toInt();
    }

    public function test_overtime_pay_weekday(): void
    {
        // Lương 17.600.000 đ -> Lương giờ = 100.000 đ/h
        // Tăng ca ngày thường 2 giờ -> 2h * 100.000 * 150% = 300.000 đ
        $pay = $this->calculateOvertimePay(monthlySalary: 17600000, otNormalDayHours: 2.0);
        $this->assertEquals(300000, $pay);
    }

    public function test_overtime_pay_weekend_and_night_allowance(): void
    {
        // Lương 17.600.000 đ (100.000 đ/h)
        // Làm thêm chủ nhật 8 giờ (200%): 8 * 100k * 2 = 1.600.000 đ
        // Làm ca 3 đêm 8 giờ (+30%): 8 * 100k * 0.3 = 240.000 đ
        // Tổng cộng = 1.840.000 đ
        $pay = $this->calculateOvertimePay(
            monthlySalary: 17600000,
            otWeekendHours: 8.0,
            nightWorkHours: 8.0
        );
        $this->assertEquals(1840000, $pay);
    }

    public function test_overtime_pay_holiday(): void
    {
        // Lương 17.600.000 đ (100.000 đ/h)
        // Làm thêm ngày Quốc khánh 2/9 (300%): 8h * 100k * 3 = 2.400.000 đ
        $pay = $this->calculateOvertimePay(monthlySalary: 17600000, otHolidayHours: 8.0);
        $this->assertEquals(2400000, $pay);
    }

    // ================================================================================================
    // 4. UNIT TEST: TUỔI NGHỈ HƯU THEO NGHỊ ĐỊNH 135/2020/NĐ-CP
    // ================================================================================================

    /**
     * Thuật toán tính ngày nghỉ hưu theo lộ trình Điều 4 & 5 Nghị định 135/2020/NĐ-CP:
     * - Từ năm 2021:
     *   + Nam đủ 60 tuổi 03 tháng; mỗi năm tăng thêm 03 tháng cho đến khi đủ 62 tuổi vào năm 2028.
     *   + Nữ đủ 55 tuổi 04 tháng; mỗi năm tăng thêm 04 tháng cho đến khi đủ 60 tuổi vào năm 2035.
     * Trả về mảng [retirement_age_years, retirement_age_months, retirement_date]
     */
    protected function calculateRetirementDate(string $dob, string $gender): array
    {
        $birthDate = Carbon::parse($dob);
        $birthYear = $birthDate->year;
        $birthMonth = $birthDate->month;

        if ($gender === 'MALE') {
            // Lộ trình Nam: 60 tuổi vào năm nào?
            $base60Year = $birthYear + 60;
            if ($base60Year < 2021) {
                $years = 60;
                $months = 0;
            } else {
                $diffYears = $base60Year - 2021;
                $extraMonths = 3 + ($diffYears * 3);
                if ($extraMonths >= 24) { // Tối đa 62 tuổi (24 tháng)
                    $years = 62;
                    $months = 0;
                } else {
                    $years = 60 + intdiv($extraMonths, 12);
                    $months = $extraMonths % 12;
                }
            }
        } else {
            // Lộ trình Nữ: 55 tuổi vào năm nào?
            $base55Year = $birthYear + 55;
            if ($base55Year < 2021) {
                $years = 55;
                $months = 0;
            } else {
                $diffYears = $base55Year - 2021;
                $extraMonths = 4 + ($diffYears * 4);
                if ($extraMonths >= 60) { // Tối đa 60 tuổi (60 tháng)
                    $years = 60;
                    $months = 0;
                } else {
                    $years = 55 + intdiv($extraMonths, 12);
                    $months = $extraMonths % 12;
                }
            }
        }

        // Ngày nghỉ hưu tính từ ngày 1 của tháng liền kề sau tháng đủ tuổi nghỉ hưu
        $retirementDate = $birthDate->copy()
            ->addYears($years)
            ->addMonths($months)
            ->endOfMonth()
            ->addDay();

        return [
            'age_years'       => $years,
            'age_months'      => $months,
            'retirement_date' => $retirementDate->toDateString(),
        ];
    }

    public function test_retirement_age_male_born_in_1965(): void
    {
        // Nam sinh tháng 05/1965 -> Năm 2025 đủ 60 tuổi -> 2025 - 2021 = 4 năm -> tăng 3 + 4*3 = 15 tháng
        // Tuổi nghỉ hưu = 60 tuổi + 15 tháng = 61 tuổi 3 tháng
        $info = $this->calculateRetirementDate('1965-05-15', 'MALE');
        $this->assertEquals(61, $info['age_years']);
        $this->assertEquals(3, $info['age_months']);
        $this->assertEquals('2026-09-01', $info['retirement_date'], 'Đủ 61t 3th vào 08/2026 -> Bắt đầu nghỉ hưu 01/09/2026');
    }

    public function test_retirement_age_female_born_in_1970(): void
    {
        // Nữ sinh tháng 02/1970 -> Năm 2025 đủ 55 tuổi -> 2025 - 2021 = 4 năm -> tăng 4 + 4*4 = 20 tháng
        // Tuổi nghỉ hưu = 55 tuổi + 20 tháng = 56 tuổi 8 tháng
        $info = $this->calculateRetirementDate('1970-02-10', 'FEMALE');
        $this->assertEquals(56, $info['age_years']);
        $this->assertEquals(8, $info['age_months']);
        $this->assertEquals('2026-11-01', $info['retirement_date'], 'Đủ 56t 8th vào 10/2026 -> Bắt đầu nghỉ hưu 01/11/2026');
    }

    public function test_retirement_age_male_capped_at_62(): void
    {
        // Nam sinh năm 1980 -> sau năm 2028 chắc chắn trần 62 tuổi 0 tháng
        $info = $this->calculateRetirementDate('1980-01-01', 'MALE');
        $this->assertEquals(62, $info['age_years']);
        $this->assertEquals(0, $info['age_months']);
        $this->assertEquals('2042-02-01', $info['retirement_date']);
    }

    public function test_retirement_age_female_capped_at_60(): void
    {
        // Nữ sinh năm 1985 -> sau năm 2035 chắc chắn trần 60 tuổi 0 tháng
        $info = $this->calculateRetirementDate('1985-06-15', 'FEMALE');
        $this->assertEquals(60, $info['age_years']);
        $this->assertEquals(0, $info['age_months']);
        $this->assertEquals('2045-07-01', $info['retirement_date']);
    }
}
