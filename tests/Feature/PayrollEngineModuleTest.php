<?php

namespace Tests\Feature;

use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Organization\Models\Position;
use App\Modules\Payroll\Models\Dependent;
use App\Modules\Payroll\Models\EmployeeSalary;
use App\Modules\Payroll\Models\LegalParameter;
use App\Modules\Payroll\Models\OtRate;
use App\Modules\Payroll\Models\PayrollPeriod;
use App\Modules\Payroll\Models\Payslip;
use App\Modules\Payroll\Models\RetroactiveSalaryAdjustment;
use App\Modules\Payroll\Models\TaxBracket;
use App\Modules\Payroll\Services\PayrollEngineService;
use App\Modules\Payroll\Services\PayrollExportService;
use Brick\Math\BigDecimal;
use Carbon\Carbon;
use DomainException;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class PayrollEngineModuleTest extends TestCase
{
    use RefreshDatabase;

    protected PayrollEngineService $engineService;
    protected PayrollExportService $exportService;

    protected OrgUnit $hq;
    protected OrgUnit $factory;
    protected Position $workerPos;
    protected Position $engineerPos;
    protected Employee $worker;
    protected Employee $engineer;

    protected function setUp(): void
    {
        parent::setUp();

        $this->engineService = app(PayrollEngineService::class);
        $this->exportService = app(PayrollExportService::class);

        // 1. Cấu hình Cây tổ chức
        $this->hq = OrgUnit::create([
            'unit_code' => 'VNM_HQ', 'unit_name' => 'Trụ sở Tập đoàn Vinamilk',
            'unit_type' => 'HEADQUARTERS', 'path' => '/1/', 'level' => 1, 'effective_from' => '2000-01-01',
            'cost_center_code' => 'CC-HQ-MANAGEMENT',
        ]);
        $this->factory = OrgUnit::create([
            'unit_code' => 'VNM_FACTORY_TN', 'unit_name' => 'Nhà máy Sữa Thống Nhất',
            'unit_type' => 'FACTORY', 'parent_id' => $this->hq->id, 'path' => '/1/5/', 'level' => 2, 'effective_from' => '2000-01-01',
            'cost_center_code' => 'CC-FACTORY-PRODUCTION',
        ]);

        // 2. Chức danh
        $this->workerPos = Position::create(['position_code' => 'PROD_WORKER', 'position_name' => 'Công nhân Vận hành Ca 3', 'level' => 2]);
        $this->engineerPos = Position::create(['position_code' => 'RND_ENGINEER', 'position_name' => 'Kỹ sư Chế biến Sữa', 'level' => 4]);

        // 3. Nhân sự
        $this->worker = Employee::create([
            'employee_code'       => 'VNM-CN001',
            'full_name'           => 'Nguyễn Văn Công',
            'employment_status'   => 'ACTIVE',
            'hire_date'           => '2023-01-01',
            'bank_account_number' => '0071001234567',
            'bank_name'           => 'Vietcombank',
            'bank_branch'         => 'Chi nhánh Bình Dương',
            'social_insurance_number' => '7912345678',
        ]);
        EmployeeAssignment::create([
            'employee_id' => $this->worker->id, 'org_unit_id' => $this->factory->id, 'position_id' => $this->workerPos->id,
            'is_primary' => true, 'is_current' => true, 'effective_from' => '2023-01-01',
        ]);
        EmployeeSalary::create([
            'employee_id'          => $this->worker->id,
            'p1_position_salary'   => 8800000,
            'p2_competency_salary' => 2200000,
            'insurance_salary'     => 11000000,
            'toxic_allowance'      => 1000000,
            'farm_field_allowance' => 0,
            'responsibility_allowance' => 0,
            'meal_allowance'       => 730000,
            'effective_from'       => '2023-01-01',
            'is_current'           => true,
        ]);

        $this->engineer = Employee::create([
            'employee_code'       => 'VNM-KS002',
            'full_name'           => 'Trần Thị Kỹ Sư',
            'employment_status'   => 'ACTIVE',
            'hire_date'           => '2022-03-01',
            'bank_account_number' => '0011009876543',
            'bank_name'           => 'Vietcombank',
            'bank_branch'         => 'Hội sở TP.HCM',
            'social_insurance_number' => '7998765432',
        ]);
        EmployeeAssignment::create([
            'employee_id' => $this->engineer->id, 'org_unit_id' => $this->hq->id, 'position_id' => $this->engineerPos->id,
            'is_primary' => true, 'is_current' => true, 'effective_from' => '2022-03-01',
        ]);
        EmployeeSalary::create([
            'employee_id'          => $this->engineer->id,
            'p1_position_salary'   => 22000000,
            'p2_competency_salary' => 13200000,
            'insurance_salary'     => 35200000,
            'toxic_allowance'      => 0,
            'farm_field_allowance' => 0,
            'responsibility_allowance' => 8800000, // Định biên P3
            'meal_allowance'       => 730000,
            'effective_from'       => '2022-03-01',
            'is_current'           => true,
        ]);

        // Đăng ký 2 người phụ thuộc cho Kỹ sư
        Dependent::create([
            'employee_id'    => $this->engineer->id,
            'dependent_name' => 'Trần Bé Con 1',
            'relationship'   => 'CHILD',
            'effective_from' => '2022-01-01',
            'is_active'      => true,
        ]);
        Dependent::create([
            'employee_id'    => $this->engineer->id,
            'dependent_name' => 'Trần Bé Con 2',
            'relationship'   => 'CHILD',
            'effective_from' => '2022-01-01',
            'is_active'      => true,
        ]);

        // 4. Seed tham số pháp lý và thuế vào CSDL
        $this->seedLegalParametersAndTaxBrackets();
    }

    private function seedLegalParametersAndTaxBrackets(): void
    {
        LegalParameter::create(['param_code' => 'BASE_SALARY_STATUTORY', 'param_name' => 'Lương cơ sở', 'param_value' => '2340000', 'unit' => 'VND', 'effective_from' => '2024-07-01']);
        LegalParameter::create(['param_code' => 'PERSONAL_TAX_RELIEF', 'param_name' => 'Giảm trừ bản thân', 'param_value' => '11000000', 'unit' => 'VND', 'effective_from' => '2020-07-01']);
        LegalParameter::create(['param_code' => 'DEPENDENT_TAX_RELIEF', 'param_name' => 'Giảm trừ người phụ thuộc', 'param_value' => '4400000', 'unit' => 'VND', 'effective_from' => '2020-07-01']);
        LegalParameter::create(['param_code' => 'TAX_FREE_MEAL_CAP', 'param_name' => 'Trần ăn ca miễn thuế', 'param_value' => '730000', 'unit' => 'VND', 'effective_from' => '2016-10-15']);
        LegalParameter::create(['param_code' => 'SI_EMP_RATE', 'param_name' => 'BHXH NLĐ', 'param_value' => '0.0800', 'unit' => 'PERCENT', 'effective_from' => '2016-01-01']);
        LegalParameter::create(['param_code' => 'HI_EMP_RATE', 'param_name' => 'BHYT NLĐ', 'param_value' => '0.0150', 'unit' => 'PERCENT', 'effective_from' => '2015-01-01']);
        LegalParameter::create(['param_code' => 'UI_EMP_RATE', 'param_name' => 'BHTN NLĐ', 'param_value' => '0.0100', 'unit' => 'PERCENT', 'effective_from' => '2015-01-01']);
        LegalParameter::create(['param_code' => 'SI_COMP_RATE', 'param_name' => 'BHXH DN', 'param_value' => '0.1750', 'unit' => 'PERCENT', 'effective_from' => '2021-07-01']);
        LegalParameter::create(['param_code' => 'HI_COMP_RATE', 'param_name' => 'BHYT DN', 'param_value' => '0.0300', 'unit' => 'PERCENT', 'effective_from' => '2015-01-01']);
        LegalParameter::create(['param_code' => 'UI_COMP_RATE', 'param_name' => 'BHTN DN', 'param_value' => '0.0100', 'unit' => 'PERCENT', 'effective_from' => '2015-01-01']);
        LegalParameter::create(['param_code' => 'UNION_FEE_EMP_RATE', 'param_name' => 'Đoàn phí NLĐ', 'param_value' => '0.0100', 'unit' => 'PERCENT', 'effective_from' => '2020-01-01']);
        LegalParameter::create(['param_code' => 'UNION_FEE_COMP_RATE', 'param_name' => 'KPCĐ DN', 'param_value' => '0.0200', 'unit' => 'PERCENT', 'effective_from' => '2014-01-01']);
        LegalParameter::create(['param_code' => 'PARTY_FEE_RATE', 'param_name' => 'Đảng phí', 'param_value' => '0.0100', 'unit' => 'PERCENT', 'effective_from' => '2018-01-01']);
        LegalParameter::create(['param_code' => 'SI_HI_CAP_MULTIPLIER', 'param_name' => 'Hệ số trần BHXH/BHYT', 'param_value' => '20.0', 'unit' => 'RATIO', 'effective_from' => '2016-01-01']);
        LegalParameter::create(['param_code' => 'UI_CAP_MULTIPLIER', 'param_name' => 'Hệ số trần BHTN', 'param_value' => '20.0', 'unit' => 'RATIO', 'effective_from' => '2015-01-01']);
        LegalParameter::create(['param_code' => 'REGIONAL_MIN_WAGE_DEFAULT', 'param_name' => 'Lương tối thiểu Vùng I', 'param_value' => '4960000', 'unit' => 'VND', 'effective_from' => '2024-07-01']);

        // 7 bậc thuế TNCN
        TaxBracket::create(['bracket_level' => 1, 'min_income' => 0, 'max_income' => 5000000, 'tax_rate' => 5.00, 'quick_deduction' => 0, 'effective_from' => '2015-01-01']);
        TaxBracket::create(['bracket_level' => 2, 'min_income' => 5000000, 'max_income' => 10000000, 'tax_rate' => 10.00, 'quick_deduction' => 250000, 'effective_from' => '2015-01-01']);
        TaxBracket::create(['bracket_level' => 3, 'min_income' => 10000000, 'max_income' => 18000000, 'tax_rate' => 15.00, 'quick_deduction' => 750000, 'effective_from' => '2015-01-01']);
        TaxBracket::create(['bracket_level' => 4, 'min_income' => 18000000, 'max_income' => 32000000, 'tax_rate' => 20.00, 'quick_deduction' => 1650000, 'effective_from' => '2015-01-01']);
        TaxBracket::create(['bracket_level' => 5, 'min_income' => 32000000, 'max_income' => 52000000, 'tax_rate' => 25.00, 'quick_deduction' => 3250000, 'effective_from' => '2015-01-01']);
        TaxBracket::create(['bracket_level' => 6, 'min_income' => 52000000, 'max_income' => 80000000, 'tax_rate' => 30.00, 'quick_deduction' => 5850000, 'effective_from' => '2015-01-01']);
        TaxBracket::create(['bracket_level' => 7, 'min_income' => 80000000, 'max_income' => null, 'tax_rate' => 35.00, 'quick_deduction' => 9850000, 'effective_from' => '2015-01-01']);

        // OT Rates
        OtRate::create(['rate_code' => 'OT_WEEKDAY', 'rate_name' => 'Ngày thường', 'multiplier' => '1.50', 'night_additional_multiplier' => '0.30', 'effective_from' => '2021-01-01']);
        OtRate::create(['rate_code' => 'OT_WEEKEND', 'rate_name' => 'Nghỉ tuần', 'multiplier' => '2.00', 'night_additional_multiplier' => '0.30', 'effective_from' => '2021-01-01']);
        OtRate::create(['rate_code' => 'OT_HOLIDAY', 'rate_name' => 'Ngày lễ', 'multiplier' => '3.00', 'night_additional_multiplier' => '0.30', 'effective_from' => '2021-01-01']);
    }

    /**
     * Test 1: Vòng đời kỳ lương (Mở → Đang tính → Chờ duyệt → Đã chốt → Đã chi. Chốt rồi thì khóa)
     */
    public function test_payroll_period_lifecycle_transitions_and_locking(): void
    {
        $period = $this->engineService->openPeriod(10, 2026, 22.0);
        $this->assertEquals('OPEN', $period->status);
        $this->assertTrue($period->canRecalculate());

        // Chuyển sang ĐANG TÍNH
        $period->transitionToCalculating();
        $this->assertEquals('CALCULATING', $period->status);

        // Chuyển sang CHỜ DUYỆT
        $period->transitionToPendingApproval();
        $this->assertEquals('PENDING_APPROVAL', $period->status);

        // Chốt kỳ lương (LOCKED - Khóa)
        $this->engineService->lockPeriod($period);
        $period->refresh();
        $this->assertEquals('LOCKED', $period->status);
        $this->assertFalse($period->canRecalculate());

        // Cố tình chạy lại cho nhân viên khi đã chốt -> Bị chặn, ném DomainException
        $this->expectException(DomainException::class);
        $this->engineService->recalculateEmployee($period, $this->worker);
    }

    /**
     * Test 2: Công nhân Ca 3 làm đêm, có phụ cấp độc hại, OT ngày thường, lưu payslip_lines đầy đủ
     */
    public function test_shift_3_worker_calculation_and_payslip_lines(): void
    {
        $period = $this->engineService->openPeriod(10, 2026, 22.0);

        // Giả lập dữ liệu công: 22 ngày công, 160h ca đêm, 10h OT ngày thường
        $overrideData = [
            'actual_working_days' => 22.0,
            'paid_leave_days'     => 0.0,
            'night_work_hours'    => 160.0, // Đi đủ ca 3
            'ot_normal_hours'     => 10.0,  // Tăng ca 10 giờ
            'toxic_allowance'     => 1000000,
            'meal_allowance'      => 730000,
            'retroactive_amount'  => 0,
            'dependent_count'     => 0,
        ];

        $payslip = $this->engineService->calculateEmployeePayslip($this->worker, $period, $overrideData);

        // Kiểm tra P1 và P2 hưởng trọn 100% công chuẩn
        $this->assertEquals(8800000, $payslip->p1_position_amount);
        $this->assertEquals(2200000, $payslip->p2_competency_amount);

        // Lương giờ chuẩn = 8.800.000 / 22 / 8 = 50.000 đ/h
        // Tiền OT 10h x 50.000 x 1.5 = 750.000 đ
        $this->assertEquals(750000, $payslip->ot_pay_amount);

        // Phụ cấp ca đêm = 160h x 50.000 x 0.30 = 2.400.000 đ
        // Tổng phụ cấp = Ca đêm 2.400.000 + Độc hại 1.000.000 + Ăn ca 730.000 = 4.130.000 đ
        $this->assertEquals(4130000, $payslip->total_allowance_amount);

        // Gross = 8.8M + 2.2M + 0.75M + 4.13M = 15.880.000 đ
        $this->assertEquals(15880000, $payslip->gross_income);

        // Khấu trừ BHXH (8%), BHYT (1.5%), BHTN (1%) trên lương đóng 11.000.000 đ
        $this->assertEquals(880000, $payslip->social_insurance_emp); // 8%
        $this->assertEquals(165000, $payslip->health_insurance_emp); // 1.5%
        $this->assertEquals(110000, $payslip->unemployment_insurance_emp); // 1%
        $this->assertEquals(110000, $payslip->union_fee_emp); // Đoàn phí 1%

        // Kiểm tra payslip_lines được lưu dạng dòng
        $this->assertGreaterThan(5, $payslip->lines()->count());
        $this->assertDatabaseHas('payslip_lines', [
            'payslip_id' => $payslip->id,
            'line_code'  => 'ALW_NIGHT_SHIFT',
            'amount'     => 2400000,
        ]);
        $this->assertDatabaseHas('payslip_lines', [
            'payslip_id' => $payslip->id,
            'line_code'  => 'ALW_TOXIC',
            'amount'     => 1000000,
        ]);
    }

    /**
     * Test 3: Kỹ sư có 2 người phụ thuộc, điểm KPI Loại A (115%), áp dụng trần BHXH và biểu thuế 7 bậc
     */
    public function test_engineer_with_2_dependents_and_kpi_bonus(): void
    {
        $period = $this->engineService->openPeriod(10, 2026, 22.0);

        // Kỹ sư: P1=22M, P2=13.2M, P3 Base=8.8M, KPI 115% -> P3=10.12M
        $overrideData = [
            'actual_working_days' => 22.0,
            'paid_leave_days'     => 0.0,
            'kpi_multiplier'      => 1.15,
            'p3_target_base'      => 8800000,
            'ot_normal_hours'     => 0,
            'night_work_hours'    => 0,
            'toxic_allowance'     => 0,
            'meal_allowance'      => 730000,
            'dependent_count'     => 2,
        ];

        $payslip = $this->engineService->calculateEmployeePayslip($this->engineer, $period, $overrideData);

        // P1 + P2 + P3 = 22.000.000 + 13.200.000 + (8.800.000 * 1.15 = 10.120.000) = 45.320.000 đ
        $this->assertEquals(22000000, $payslip->p1_position_amount);
        $this->assertEquals(13200000, $payslip->p2_competency_amount);
        $this->assertEquals(10120000, $payslip->p3_performance_amount);

        // Lương đóng BH: 35.200.000 đ (nằm dưới trần 46.800.000 đ)
        $this->assertEquals(2816000, $payslip->social_insurance_emp); // 8% * 35.2M
        $this->assertEquals(528000, $payslip->health_insurance_emp);  // 1.5% * 35.2M
        $this->assertEquals(352000, $payslip->unemployment_insurance_emp); // 1% * 35.2M

        // Đoàn phí Công đoàn: 1% * 35.2M = 352.000 đ nhưng có trần 10% lương cơ sở = 234.000 đ
        $this->assertEquals(234000, $payslip->union_fee_emp);

        // Thuế TNCN tính đúng theo giảm trừ bản thân 11M + 2 phụ thuộc (8.8M) = 19.8M
        $this->assertGreaterThan(0, $payslip->personal_income_tax);
        $this->assertGreaterThan(0, $payslip->net_income);
    }

    /**
     * Test 4: Xuất các định dạng (Bank Transfer CSV, BHXH D02-LT, ERP SAP FI/CO)
     */
    public function test_payroll_exports_functionality(): void
    {
        $period = $this->engineService->openPeriod(10, 2026, 22.0);
        $this->engineService->calculateEmployeePayslip($this->worker, $period);
        $this->engineService->calculateEmployeePayslip($this->engineer, $period);

        // 1. CSV Ngân hàng
        $csv = $this->exportService->exportBankTransferCsv($period);
        $this->assertStringContainsString('VNM-CN001', $csv);
        $this->assertStringContainsString('VNM-KS002', $csv);
        $this->assertStringContainsString('0071001234567', $csv);

        // 2. Báo cáo BHXH Mẫu D02-LT
        $d02 = $this->exportService->exportSocialInsuranceD02Lt($period);
        $this->assertCount(2, $d02['data']);
        $this->assertEquals('7912345678', $d02['data'][0]['social_insurance_number']);

        // 3. File hạch toán ERP SAP FI/CO
        $erp = $this->exportService->exportErpJournalEntries($period);
        $this->assertNotEmpty($erp['journal_entries']);
        // Bút toán Nợ/Có cân đối
        $this->assertEquals($erp['total_debit'], $erp['total_credit']);
    }
}
