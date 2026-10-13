<?php

declare(strict_types=1);

namespace Tests\Unit;

use PHPUnit\Framework\TestCase;
use PayrollService;
use PayrollLockedException;
use Database;
use PDO;

class PayrollServiceTest extends TestCase
{
    private PayrollService $service;
    private PDO $db;

    protected function setUp(): void
    {
        $this->db = Database::getInstance()->getConnection();
        $this->service = new PayrollService($this->db);
    }

    /**
     * TEST 1: Lương P1 theo hợp đồng và ngày công thực tế
     */
    public function testCalculateP1Salary(): void
    {
        // Lương HĐ 26.000.000 đ, công chuẩn 26 ngày, làm thực tế 26 ngày -> 26.000.000 đ
        $p1Full = $this->service->calculateP1(26000000.0, 26.0, 26.0);
        $this->assertEquals(26000000.0, $p1Full);

        // Làm 13 ngày (nửa tháng) -> 13.000.000 đ
        $p1Half = $this->service->calculateP1(26000000.0, 13.0, 26.0);
        $this->assertEquals(13000000.0, $p1Half);

        // Làm 0 ngày -> 0 đ
        $p1Zero = $this->service->calculateP1(26000000.0, 0.0, 26.0);
        $this->assertEquals(0.0, $p1Zero);
    }

    /**
     * TEST 2: Lương P2 năng lực theo bậc lương
     */
    public function testCalculateP2CompetencySalary(): void
    {
        $contractSalary = 20000000.0;

        // Bậc 1: 10% -> 2.000.000 đ
        $p2Bac1 = $this->service->calculateP2($contractSalary, 'BAC_1');
        $this->assertEquals(2000000.0, $p2Bac1);

        // Bậc 3: 20% -> 4.000.000 đ
        $p2Bac3 = $this->service->calculateP2($contractSalary, 'BAC_3');
        $this->assertEquals(4000000.0, $p2Bac3);

        // Bậc 6: 35% -> 7.000.000 đ
        $p2Bac6 = $this->service->calculateP2($contractSalary, 'BAC_6');
        $this->assertEquals(7000000.0, $p2Bac6);

        // Chỉ định hệ số trực tiếp 0.25 -> 5.000.000 đ
        $p2Custom = $this->service->calculateP2($contractSalary, null, 0.25);
        $this->assertEquals(5000000.0, $p2Custom);
    }

    /**
     * TEST 3: Lương P3 theo điểm KPI thực tế (không dùng hằng số)
     */
    public function testCalculateP3KpiSalary(): void
    {
        $contractSalary = 20000000.0;
        $targetRate = 0.15; // Quỹ mục tiêu 15% lương HĐ = 3.000.000 đ khi đạt 100 điểm

        // Đạt 100 điểm KPI -> 3.000.000 đ
        $p3_100 = $this->service->calculateP3($contractSalary, 100.0, $targetRate);
        $this->assertEquals(3000000.0, $p3_100);

        // Đạt 120 điểm KPI (Xuất sắc) -> 3.600.000 đ
        $p3_120 = $this->service->calculateP3($contractSalary, 120.0, $targetRate);
        $this->assertEquals(3600000.0, $p3_120);

        // Đạt 80 điểm KPI -> 2.400.000 đ
        $p3_80 = $this->service->calculateP3($contractSalary, 80.0, $targetRate);
        $this->assertEquals(2400000.0, $p3_80);
    }

    /**
     * TEST 4: Tiền làm thêm giờ (OT) và tách phần chênh lệch miễn thuế TNCN
     */
    public function testCalculateOvertimeAndTaxExemption(): void
    {
        $contractSalary = 20800000.0;
        $standardDays = 26.0;
        $hoursPerDay = 8.0;
        // Đơn giá 1 giờ = 20.800.000 / (26 * 8) = 100.000 đ/giờ

        // 10 giờ ngày thường (150% = 1.500.000 đ; chịu thuế 1.000.000 đ; miễn thuế 500.000 đ)
        // 5 giờ ngày nghỉ tuần (200% = 1.000.000 đ; chịu thuế 500.000 đ; miễn thuế 500.000 đ)
        // 2 giờ ngày lễ tết (300% = 600.000 đ; chịu thuế 200.000 đ; miễn thuế 400.000 đ)
        // 4 giờ làm đêm (+30% = 120.000 đ; toàn bộ miễn thuế)
        $otCalc = $this->service->calculateOvertime(
            $contractSalary,
            10.0, // Thường
            5.0,  // Nghỉ tuần
            2.0,  // Lễ tết
            4.0,  // Đêm
            $standardDays,
            $hoursPerDay
        );

        $this->assertEquals(100000.0, $otCalc['hourly_rate']);
        $this->assertEquals(1500000.0, $otCalc['weekday_pay']);
        $this->assertEquals(1000000.0, $otCalc['weekend_pay']);
        $this->assertEquals(600000.0, $otCalc['holiday_pay']);
        $this->assertEquals(120000.0, $otCalc['night_pay']);

        $expectedTotal = 1500000.0 + 1000000.0 + 600000.0 + 120000.0; // 3.220.000 đ
        $this->assertEquals($expectedTotal, $otCalc['total_ot_pay']);

        $expectedExempt = 500000.0 + 500000.0 + 400000.0 + 120000.0; // 1.520.000 đ
        $this->assertEquals($expectedExempt, $otCalc['tax_exempt_ot_pay']);

        $expectedTaxable = 1000000.0 + 500000.0 + 200000.0; // 1.700.000 đ
        $this->assertEquals($expectedTaxable, $otCalc['taxable_ot_pay']);
    }

    /**
     * TEST 5: Bảo hiểm bắt buộc NLĐ (BHXH 8%, BHYT 1.5%, BHTN 1%) kèm chặn trần pháp lý
     */
    public function testMandatoryInsurancesCalculationAndCeilings(): void
    {
        $legalConfig = $this->service->getLegalConfig('2026-03-01');

        // Mức lương cơ bản 10.000.000 đ (nằm dưới trần)
        $ins10m = $this->service->calculateInsurances(10000000.0, 1, $legalConfig);
        $this->assertEquals(800000.0, $ins10m['bhxh']);  // 8%
        $this->assertEquals(150000.0, $ins10m['bhyt']);  // 1.5%
        $this->assertEquals(100000.0, $ins10m['bhtn']);  // 1%
        $this->assertEquals(1050000.0, $ins10m['total_insurance']);

        // Mức lương cực cao 100.000.000 đ -> Phải bị CHẶN TRẦN BHXH/BHYT (20 lần lương cơ sở) và BHTN (20 lần LTT vùng)
        $insCap = $this->service->calculateInsurances(100000000.0, 1, $legalConfig);
        $this->assertLessThan(8000000.0, $insCap['bhxh'], "BHXH phải bị chặn trần không vượt 20 lần mức cơ sở");
        $this->assertLessThan(1000000.0, $insCap['bhtn'], "BHTN phải bị chặn trần không vượt 20 lần lương tối thiểu vùng");
    }

    /**
     * TEST 6: Thuế TNCN lũy tiến áp dụng mức giảm trừ 2026 (15,5tr bản thân, 6,2tr NPT)
     */
    public function testPersonalIncomeTaxWithNewReliefDeductions(): void
    {
        $legalConfig = $this->service->getLegalConfig('2026-03-01');
        $taxBrackets = $this->service->getTaxBrackets('2026-03-01');

        // Thu nhập chịu thuế 15.000.000 đ, giảm trừ bản thân 15.500.000 đ -> Thuế = 0 đ
        $tax0 = $this->service->calculatePersonalIncomeTax(15000000.0, $taxBrackets, 15500000.0, 0, 6200000.0);
        $this->assertEquals(0.0, $tax0['tax_amount']);

        // Thu nhập chịu thuế 25.500.000 đ, 0 NPT -> Thu nhập tính thuế = 10.000.000 đ
        // Biểu 5 bậc mới hiệu lực từ 2026: Bậc 1 (<= 10tr) thuế suất 5% -> 10tr * 5% = 500.000 đ
        $tax10m = $this->service->calculatePersonalIncomeTax(25500000.0, $taxBrackets, 15500000.0, 0, 6200000.0);
        $this->assertEquals(10000000.0, $tax10m['assessable_income']);
        $this->assertEquals(500000.0, $tax10m['tax_amount']);

        // Có 1 người phụ thuộc (6.2tr) -> Thu nhập tính thuế còn 3.800.000 đ (Bậc 1 * 5% = 190.000 đ)
        $taxWithNpt = $this->service->calculatePersonalIncomeTax(25500000.0, $taxBrackets, 15500000.0, 1, 6200000.0);
        $this->assertEquals(3800000.0, $taxWithNpt['assessable_income']);
        $this->assertEquals(190000.0, $taxWithNpt['tax_amount']);
    }

    /**
     * TEST 7: Phụ cấp ăn ca và phần miễn thuế (hạn mức 730.000 đ)
     */
    public function testMealAllowanceExemption(): void
    {
        // Nhận 1.000.000 đ ăn ca -> Miễn thuế 730.000 đ, chịu thuế 270.000 đ
        $res = $this->service->calculateMealAllowance(1000000.0, 730000.0);
        $this->assertEquals(1000000.0, $res['actual_meal_allowance']);
        $this->assertEquals(730000.0, $res['exempt_meal_allowance']);
        $this->assertEquals(270000.0, $res['taxable_meal_allowance']);
    }

    /**
     * TEST 8: Khấu trừ khác (Tạm ứng & Bồi hoàn tài sản chưa hoàn trả)
     */
    public function testOtherDeductionsWithUnreturnedAssets(): void
    {
        $res = $this->service->calculateOtherDeductions(1000000.0, 3500000.0);
        $this->assertEquals(1000000.0, $res['advance_payment']);
        $this->assertEquals(3500000.0, $res['unreturned_asset_penalty']);
        $this->assertEquals(4500000.0, $res['total_other_deductions']);
    }

    /**
     * TEST 9: Pipeline tính toán lương đầy đủ một nhân sự
     */
    public function testFullEmployeeSalaryPipeline(): void
    {
        $input = [
            'ma_nv'             => 'VNM-0001',
            'ho_ten'            => 'Nguyễn Văn Hùng',
            'contract_salary'   => 30000000.0,
            'standard_days'     => 26.0,
            'actual_days'       => 26.0,
            'bac_luong'         => 'BAC_4', // 25% = 7.500.000 đ
            'kpi_score'         => 110.0,   // 30tr * 15% * 1.1 = 4.950.000 đ
            'meal_allowance'    => 1000000.0,
            'other_allowance'   => 500000.0,
            'ot_weekday_hours'  => 8.0,
            'dependents_count'  => 1,
            'asset_penalty'     => 1500000.0, // Bồi thường tài sản nợ
            'effective_date'    => '2026-03-01'
        ];

        $calc = $this->service->calculateEmployeeSalary($input);

        $this->assertEquals(30000000.0, $calc['luong_p1_hop_dong']);
        $this->assertEquals(7500000.0, $calc['luong_p2_hieu_suat']);
        $this->assertEquals(4950000.0, $calc['luong_p3_kpi']);
        $this->assertEquals(1500000.0, $calc['khau_tru_tai_san']);
        $this->assertGreaterThan(0.0, $calc['thuc_linh']);
    }

    /**
     * TEST 10: Khóa bảng lương bất biến sau khi ĐÃ CHI
     */
    public function testApprovalStateTransitionsAndImmutability(): void
    {
        // Bảng lương đã ở trạng thái DA_CHI thì không được phép tính lại hoặc chỉnh sửa
        $this->expectException(PayrollLockedException::class);
        $this->service->assertPayrollNotLocked('DA_CHI');
    }
}
