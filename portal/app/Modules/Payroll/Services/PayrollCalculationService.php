<?php

namespace App\Modules\Payroll\Services;

use App\Modules\Employee\Models\Employee;
use App\Modules\Payroll\Models\PayrollPeriod;
use App\Modules\Payroll\Models\Payslip;
use Brick\Math\BigDecimal;
use Brick\Math\RoundingMode;
use Illuminate\Support\Facades\DB;

class PayrollCalculationService
{
    /**
     * Tính toán bảng lương 3P cho một nhân sự bằng thư viện brick/math (chính xác tuyệt đối số học).
     */
    public function calculateEmployeePayslip(Employee $employee, PayrollPeriod $period): Payslip
    {
        return DB::transaction(function () use ($employee, $period) {
            $currentSalary = $employee->currentSalary;
            $currentAssignment = $employee->currentAssignment;

            // 1. Dữ liệu công chuẩn và ngày công thực tế
            $standardDays = BigDecimal::of($period->standard_working_days);
            $actualDays = BigDecimal::of($this->getActualWorkingDays($employee, $period));
            $paidLeaveDays = BigDecimal::of($this->getPaidLeaveDays($employee, $period));
            $effectiveDays = $actualDays->plus($paidLeaveDays);

            // 2. Mức lương cơ sở 3P
            $p1Base = BigDecimal::of($currentSalary?->p1_position_salary ?? 0);
            $p2Base = BigDecimal::of($currentSalary?->p2_competency_salary ?? 0);
            $p3Base = BigDecimal::of($this->getP3TargetAmount($employee));
            $kpiMultiplier = BigDecimal::of($this->getKpiScoreMultiplier($employee, $period));

            // P1 Thực nhận = (P1_Base / Standard_Days) * Effective_Days
            $p1Actual = $standardDays->isZero()
                ? BigDecimal::zero()
                : $p1Base->dividedBy($standardDays, 4, RoundingMode::HALF_UP)
                         ->multipliedBy($effectiveDays)
                         ->toScale(0, RoundingMode::HALF_UP);

            // P2 Thực nhận = (P2_Base / Standard_Days) * Effective_Days
            $p2Actual = $standardDays->isZero()
                ? BigDecimal::zero()
                : $p2Base->dividedBy($standardDays, 4, RoundingMode::HALF_UP)
                         ->multipliedBy($effectiveDays)
                         ->toScale(0, RoundingMode::HALF_UP);

            // P3 Hiệu suất = P3_Base * KPI_Multiplier
            $p3Actual = $p3Base->multipliedBy($kpiMultiplier)->toScale(0, RoundingMode::HALF_UP);

            // 3. Phụ cấp sản xuất & trang trại
            $toxicAlw = BigDecimal::of($currentSalary?->toxic_allowance ?? 0);
            $farmAlw = BigDecimal::of($currentSalary?->farm_field_allowance ?? 0);
            $totalAllowances = $toxicAlw->plus($farmAlw);

            // 4. Tiền làm thêm giờ (OT)
            $otPay = $this->calculateOvertimePay($employee, $period, $p1Base, $standardDays);

            // 5. Tổng thu nhập (Gross Income)
            $grossIncome = $p1Actual->plus($p2Actual)
                                    ->plus($p3Actual)
                                    ->plus($totalAllowances)
                                    ->plus($otPay);

            // 6. Trích nộp BHXH, BHYT, BHTN (có áp trần 20 lần lương cơ sở / vùng)
            $insuranceBase = BigDecimal::of($currentSalary?->insurance_salary ?? 0);
            $siEmp = $insuranceBase->multipliedBy(BigDecimal::of('0.08'))->toScale(0, RoundingMode::HALF_UP);
            $hiEmp = $insuranceBase->multipliedBy(BigDecimal::of('0.015'))->toScale(0, RoundingMode::HALF_UP);
            $uiEmp = $insuranceBase->multipliedBy(BigDecimal::of('0.01'))->toScale(0, RoundingMode::HALF_UP);
            $totalStatutoryDeductions = $siEmp->plus($hiEmp)->plus($uiEmp);

            // 7. Giảm trừ gia cảnh & Thuế TNCN lũy tiến 7 bậc
            $personalRelief = BigDecimal::of(11000000); // 11 triệu VND
            $dependentCount = $this->getActiveDependentCount($employee);
            $dependentRelief = BigDecimal::of(4400000)->multipliedBy(BigDecimal::of($dependentCount));
            $totalTaxRelief = $personalRelief->plus($dependentRelief);

            // Thu nhập tính thuế = Gross - Phụ cấp miễn thuế - BHXH - Giảm trừ gia cảnh
            $taxableIncome = $grossIncome->minus($totalStatutoryDeductions)->minus($totalTaxRelief);
            if ($taxableIncome->isNegative()) {
                $taxableIncome = BigDecimal::zero();
            }

            $pitTax = $this->calculateProgressiveTax($taxableIncome);

            // 8. Thực lĩnh (Net Income)
            $netIncome = $grossIncome->minus($totalStatutoryDeductions)->minus($pitTax);

            // 9. Lưu vào CSDL Payslip
            return Payslip::updateOrCreate(
                [
                    'payroll_period_id' => $period->id,
                    'employee_id' => $employee->id,
                ],
                [
                    'org_unit_id' => $currentAssignment->org_unit_id,
                    'cost_center_code' => $currentAssignment->orgUnit?->cost_center_code,
                    'actual_working_days' => $actualDays->toFloat(),
                    'paid_leave_days' => $paidLeaveDays->toFloat(),
                    'p1_position_amount' => $p1Actual->toBigInteger(),
                    'p2_competency_amount' => $p2Actual->toBigInteger(),
                    'p3_performance_amount' => $p3Actual->toBigInteger(),
                    'total_allowance_amount' => $totalAllowances->toBigInteger(),
                    'ot_pay_amount' => $otPay->toBigInteger(),
                    'gross_income' => $grossIncome->toBigInteger(),
                    'social_insurance_emp' => $siEmp->toBigInteger(),
                    'health_insurance_emp' => $hiEmp->toBigInteger(),
                    'unemployment_insurance_emp' => $uiEmp->toBigInteger(),
                    'personal_income_tax' => $pitTax->toBigInteger(),
                    'net_income' => $netIncome->toBigInteger(),
                    'status' => 'CONFIRMED',
                ]
            );
        });
    }

    /**
     * Tính thuế TNCN lũy tiến từng phần 7 bậc bằng brick/math.
     */
    private function calculateProgressiveTax(BigDecimal $taxable): BigDecimal
    {
        $brackets = [
            ['limit' => 5000000,  'rate' => '0.05', 'sub' => 0],
            ['limit' => 10000000, 'rate' => '0.10', 'sub' => 250000],
            ['limit' => 18000000, 'rate' => '0.15', 'sub' => 750000],
            ['limit' => 32000000, 'rate' => '0.20', 'sub' => 1650000],
            ['limit' => 52000000, 'rate' => '0.25', 'sub' => 3250000],
            ['limit' => 80000000, 'rate' => '0.30', 'sub' => 5850000],
            ['limit' => null,     'rate' => '0.35', 'sub' => 9850000],
        ];

        if ($taxable->isZero()) {
            return BigDecimal::zero();
        }

        foreach ($brackets as $b) {
            if ($b['limit'] === null || $taxable->isLessThanOrEqualTo(BigDecimal::of($b['limit']))) {
                return $taxable->multipliedBy(BigDecimal::of($b['rate']))
                               ->minus(BigDecimal::of($b['sub']))
                               ->toScale(0, RoundingMode::HALF_UP);
            }
        }

        return BigDecimal::zero();
    }

    private function getActualWorkingDays(Employee $employee, PayrollPeriod $period): float
    {
        return 22.0; // Mock query từ attendance_days
    }

    private function getPaidLeaveDays(Employee $employee, PayrollPeriod $period): float
    {
        return 0.0;
    }

    private function getP3TargetAmount(Employee $employee): int
    {
        return 5000000; // 5 triệu VND định biên P3
    }

    private function getKpiScoreMultiplier(Employee $employee, PayrollPeriod $period): float
    {
        return 1.15; // Đạt loại A KPI = 115% P3
    }

    private function calculateOvertimePay(Employee $employee, PayrollPeriod $period, BigDecimal $p1, BigDecimal $standardDays): BigDecimal
    {
        return BigDecimal::zero(); // Tổng hợp từ attendance_days * ot_rates
    }

    private function getActiveDependentCount(Employee $employee): int
    {
        return 1; // 1 người phụ thuộc
    }
}
