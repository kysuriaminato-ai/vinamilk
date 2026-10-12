<?php

namespace App\Modules\Payroll\Services;

use App\Modules\Attendance\Models\AttendanceDay;
use App\Modules\Employee\Models\Employee;
use App\Modules\Kpi\Models\KpiEmployeeSummary;
use App\Modules\Leave\Models\LeaveRequest;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Payroll\Models\Dependent;
use App\Modules\Payroll\Models\LegalParameter;
use App\Modules\Payroll\Models\OtRate;
use App\Modules\Payroll\Models\PayrollPeriod;
use App\Modules\Payroll\Models\Payslip;
use App\Modules\Payroll\Models\PayslipLine;
use App\Modules\Payroll\Models\RetroactiveSalaryAdjustment;
use App\Modules\Payroll\Models\TaxBracket;
use Brick\Math\BigDecimal;
use Brick\Math\RoundingMode;
use Carbon\Carbon;
use DomainException;
use Illuminate\Support\Collection;
use Illuminate\Support\Facades\Cache;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Log;

class PayrollEngineService
{
    /**
     * Khởi tạo kỳ lương mới
     */
    public function openPeriod(int $month, int $year, float $standardWorkingDays = 22.0): PayrollPeriod
    {
        $periodCode = sprintf('PR-%04d-%02d', $year, $month);

        return PayrollPeriod::firstOrCreate(
            ['month' => $month, 'year' => $year],
            [
                'period_code'           => $periodCode,
                'standard_working_days' => $standardWorkingDays,
                'status'                => PayrollPeriod::STATUS_OPEN,
            ]
        );
    }

    /**
     * Tính toán bảng lương toàn bộ tập đoàn theo lô 1.000 người
     */
    public function calculatePeriod(PayrollPeriod $period, ?callable $progressCallback = null): void
    {
        if (!$period->canRecalculate()) {
            throw new DomainException("Kỳ lương {$period->period_code} đã ở trạng thái [{$period->status}] và bị khóa. Không thể chạy tính toán.");
        }

        $period->transitionToCalculating();

        $cacheKey = "payroll_progress_{$period->id}";
        $totalEmployees = Employee::active()->count();
        Cache::put($cacheKey, [
            'total'     => $totalEmployees,
            'processed' => 0,
            'percent'   => 0,
            'status'    => 'PROCESSING',
            'errors'    => [],
        ], 3600);

        $processedCount = 0;
        $errors = [];

        // Chạy theo lô 1.000 người
        Employee::active()
            ->with(['currentAssignment.orgUnit', 'currentSalary'])
            ->chunkById(1000, function (Collection $employees) use ($period, &$processedCount, $totalEmployees, $cacheKey, &$errors, $progressCallback) {
                foreach ($employees as $employee) {
                    try {
                        $this->calculateEmployeePayslip($employee, $period);
                    } catch (\Throwable $e) {
                        Log::error("Lỗi tính lương nhân viên {$employee->employee_code}: " . $e->getMessage());
                        $errors[] = [
                            'employee_code' => $employee->employee_code,
                            'message'       => $e->getMessage(),
                        ];
                    }

                    $processedCount++;
                    $percent = $totalEmployees > 0 ? (int)round(($processedCount / $totalEmployees) * 100) : 100;

                    Cache::put($cacheKey, [
                        'total'     => $totalEmployees,
                        'processed' => $processedCount,
                        'percent'   => $percent,
                        'status'    => 'PROCESSING',
                        'errors'    => $errors,
                    ], 3600);

                    if ($progressCallback) {
                        $progressCallback($processedCount, $totalEmployees, $percent);
                    }
                }
            });

        $period->transitionToPendingApproval();

        Cache::put($cacheKey, [
            'total'     => $totalEmployees,
            'processed' => $processedCount,
            'percent'   => 100,
            'status'    => 'COMPLETED',
            'errors'    => $errors,
        ], 3600);
    }

    /**
     * Tính lại lương cho một nhân viên cụ thể
     */
    public function recalculateEmployee(PayrollPeriod $period, Employee $employee): Payslip
    {
        if (!$period->canRecalculate()) {
            throw new DomainException("Kỳ lương {$period->period_code} đã bị khóa. Không thể chạy lại cho nhân viên {$employee->employee_code}.");
        }

        return $this->calculateEmployeePayslip($employee, $period);
    }

    /**
     * Tính lại lương cho toàn bộ nhân viên thuộc một đơn vị (hoặc cả cây con)
     */
    public function recalculateOrgUnit(PayrollPeriod $period, OrgUnit $orgUnit, bool $recursive = true): int
    {
        if (!$period->canRecalculate()) {
            throw new DomainException("Kỳ lương {$period->period_code} đã bị khóa. Không thể chạy lại cho đơn vị {$orgUnit->unit_name}.");
        }

        $query = Employee::active();
        if ($recursive) {
            $query->inSubtree($orgUnit->path);
        } else {
            $query->whereHas('currentAssignment', fn ($q) => $q->where('org_unit_id', $orgUnit->id));
        }

        $employees = $query->with(['currentAssignment.orgUnit', 'currentSalary'])->get();
        $count = 0;

        DB::transaction(function () use ($employees, $period, &$count) {
            foreach ($employees as $employee) {
                $this->calculateEmployeePayslip($employee, $period);
                $count++;
            }
        });

        return $count;
    }

    /**
     * Duyệt bảng lương (Chuyển sang ĐÃ CHỐT - Khóa)
     */
    public function lockPeriod(PayrollPeriod $period): void
    {
        $period->lock();
    }

    /**
     * Thanh toán lương (ĐÃ CHI)
     */
    public function payPeriod(PayrollPeriod $period, string $bankReference = 'VCB-BATCH-SALARY'): void
    {
        $period->markAsPaid();

        Payslip::where('payroll_period_id', $period->id)->update([
            'status'         => 'PAID',
            'paid_at'        => now(),
            'bank_reference' => $bankReference,
        ]);
    }

    /**
     * TÍNH TOÁN CHI TIẾT PHIẾU LƯƠNG CHO 1 NHÂN VIÊN BẰNG BRICK/MATH (KHÔNG HARDCODE)
     */
    public function calculateEmployeePayslip(
        Employee $employee,
        PayrollPeriod $period,
        ?array $overrideWorkData = null
    ): Payslip {
        if (!$period->canRecalculate()) {
            throw new DomainException("Kỳ lương {$period->period_code} đã ở trạng thái [{$period->status}] và bị khóa. Không thể sửa đổi.");
        }

        return DB::transaction(function () use ($employee, $period, $overrideWorkData) {
            $endDate = $period->getEndDate();
            $startDate = $period->getStartDate();

            // 1. LẤY MỌI THAM SỐ PHÁP LÝ TỪ CSDL TẠI NGÀY CUỐI KỲ (KHÔNG HARDCODE)
            $baseSalaryStatutory   = LegalParameter::getEffectiveDecimal('BASE_SALARY_STATUTORY', $endDate, '2340000');
            $personalTaxRelief     = LegalParameter::getEffectiveDecimal('PERSONAL_TAX_RELIEF', $endDate, '11000000');
            $dependentTaxRelief    = LegalParameter::getEffectiveDecimal('DEPENDENT_TAX_RELIEF', $endDate, '4400000');
            $taxFreeMealCap        = LegalParameter::getEffectiveDecimal('TAX_FREE_MEAL_CAP', $endDate, '730000');
            $siEmpRate             = LegalParameter::getEffectiveDecimal('SI_EMP_RATE', $endDate, '0.0800');
            $hiEmpRate             = LegalParameter::getEffectiveDecimal('HI_EMP_RATE', $endDate, '0.0150');
            $uiEmpRate             = LegalParameter::getEffectiveDecimal('UI_EMP_RATE', $endDate, '0.0100');
            $siCompRate            = LegalParameter::getEffectiveDecimal('SI_COMP_RATE', $endDate, '0.1750');
            $hiCompRate            = LegalParameter::getEffectiveDecimal('HI_COMP_RATE', $endDate, '0.0300');
            $uiCompRate            = LegalParameter::getEffectiveDecimal('UI_COMP_RATE', $endDate, '0.0100');
            $unionFeeEmpRate       = LegalParameter::getEffectiveDecimal('UNION_FEE_EMP_RATE', $endDate, '0.0100');
            $unionFeeCompRate      = LegalParameter::getEffectiveDecimal('UNION_FEE_COMP_RATE', $endDate, '0.0200');
            $partyFeeRate          = LegalParameter::getEffectiveDecimal('PARTY_FEE_RATE', $endDate, '0.0100');
            $siHiCapMultiplier     = LegalParameter::getEffectiveDecimal('SI_HI_CAP_MULTIPLIER', $endDate, '20.0');
            $uiCapMultiplier       = LegalParameter::getEffectiveDecimal('UI_CAP_MULTIPLIER', $endDate, '20.0');
            $regionalMinWage       = LegalParameter::getEffectiveDecimal('REGIONAL_MIN_WAGE_DEFAULT', $endDate, '4960000');

            $taxBrackets = TaxBracket::getEffectiveBrackets($endDate);

            // Hệ số OT từ bảng ot_rates
            $otWeekdayRate = OtRate::getRate('OT_WEEKDAY', $endDate);
            $otWeekendRate = OtRate::getRate('OT_WEEKEND', $endDate);
            $otHolidayRate = OtRate::getRate('OT_HOLIDAY', $endDate);

            $otWeekdayMultiplier = $otWeekdayRate ? $otWeekdayRate->getMultiplierDecimal() : BigDecimal::of('1.50');
            $otWeekendMultiplier = $otWeekendRate ? $otWeekendRate->getMultiplierDecimal() : BigDecimal::of('2.00');
            $otHolidayMultiplier = $otHolidayRate ? $otHolidayRate->getMultiplierDecimal() : BigDecimal::of('3.00');
            $nightShiftBonusMultiplier = $otWeekdayRate ? $otWeekdayRate->getNightAdditionalMultiplierDecimal() : BigDecimal::of('0.30');

            // 2. DỮ LIỆU CÔNG CHUẨN VÀ CÔNG THỰC TẾ
            $currentSalary = $employee->currentSalary;
            $currentAssignment = $employee->currentAssignment;
            $standardDays = BigDecimal::of((string)$period->standard_working_days);

            $actualDaysVal = $overrideWorkData['actual_working_days'] ?? $this->getActualWorkingDays($employee, $startDate, $endDate);
            $paidLeaveDaysVal = $overrideWorkData['paid_leave_days'] ?? $this->getPaidLeaveDays($employee, $startDate, $endDate);

            $actualDays = BigDecimal::of((string)$actualDaysVal);
            $paidLeaveDays = BigDecimal::of((string)$paidLeaveDaysVal);
            $effectiveDays = $actualDays->plus($paidLeaveDays);

            // 3. MỨC LƯƠNG 3P CƠ SỞ
            $p1Base = BigDecimal::of((string)($currentSalary?->p1_position_salary ?? 0));
            $p2Base = BigDecimal::of((string)($currentSalary?->p2_competency_salary ?? 0));
            $p3Base = BigDecimal::of((string)($overrideWorkData['p3_target_base'] ?? $currentSalary?->responsibility_allowance ?? 0));

            // Lương ngày công chuẩn và lương giờ chuẩn (1 ngày = 8 giờ)
            $p1DailyRate = $standardDays->isGreaterThan(0)
                ? $p1Base->dividedBy($standardDays, 4, RoundingMode::HALF_UP)
                : BigDecimal::zero();

            $p2DailyRate = $standardDays->isGreaterThan(0)
                ? $p2Base->dividedBy($standardDays, 4, RoundingMode::HALF_UP)
                : BigDecimal::zero();

            $baseHourlyRate = $p1DailyRate->dividedBy(BigDecimal::of(8), 4, RoundingMode::HALF_UP);

            // P1 Thực nhận = (P1_Base / Standard_Days) * Effective_Days (làm tròn đến đồng)
            $p1Earned = $p1DailyRate->multipliedBy($effectiveDays)->toScale(0, RoundingMode::HALF_UP);

            // P2 Thực nhận = (P2_Base / Standard_Days) * Effective_Days (làm tròn đến đồng)
            $p2Earned = $p2DailyRate->multipliedBy($effectiveDays)->toScale(0, RoundingMode::HALF_UP);

            // P3 Hiệu suất = P3_Base * KPI_Score / 100
            $kpiMultiplierVal = $overrideWorkData['kpi_multiplier'] ?? $this->getApprovedKpiMultiplier($employee, $period);
            $kpiMultiplier = BigDecimal::of((string)$kpiMultiplierVal);
            $p3Earned = $p3Base->multipliedBy($kpiMultiplier)->toScale(0, RoundingMode::HALF_UP);

            // 4. TIỀN LÀM THÊM GIỜ (OT) & PHỤ CẤP CA ĐÊM THEO ĐIỀU 98 BLLĐ
            $otNormalHours  = BigDecimal::of((string)($overrideWorkData['ot_normal_hours'] ?? $this->getOtHours($employee, $startDate, $endDate, 'NORMAL')));
            $otWeekendHours = BigDecimal::of((string)($overrideWorkData['ot_weekend_hours'] ?? $this->getOtHours($employee, $startDate, $endDate, 'WEEKEND')));
            $otHolidayHours = BigDecimal::of((string)($overrideWorkData['ot_holiday_hours'] ?? $this->getOtHours($employee, $startDate, $endDate, 'HOLIDAY')));
            $nightWorkHours = BigDecimal::of((string)($overrideWorkData['night_work_hours'] ?? $this->getNightWorkHours($employee, $startDate, $endDate)));

            // Tiền OT ngày thường = giờ * lương giờ * 150%
            $otWeekdayPay = $otNormalHours->multipliedBy($baseHourlyRate)
                ->multipliedBy($otWeekdayMultiplier)
                ->toScale(0, RoundingMode::HALF_UP);

            // Tiền OT nghỉ tuần = giờ * lương giờ * 200%
            $otWeekendPay = $otWeekendHours->multipliedBy($baseHourlyRate)
                ->multipliedBy($otWeekendMultiplier)
                ->toScale(0, RoundingMode::HALF_UP);

            // Tiền OT ngày lễ = giờ * lương giờ * 300%
            $otHolidayPay = $otHolidayHours->multipliedBy($baseHourlyRate)
                ->multipliedBy($otHolidayMultiplier)
                ->toScale(0, RoundingMode::HALF_UP);

            $totalOtPay = $otWeekdayPay->plus($otWeekendPay)->plus($otHolidayPay);

            // Phụ cấp làm đêm Điều 98 Khoản 2 BLLĐ (+30% lương giờ ngày thường)
            $nightShiftPay = $nightWorkHours->multipliedBy($baseHourlyRate)
                ->multipliedBy($nightShiftBonusMultiplier)
                ->toScale(0, RoundingMode::HALF_UP);

            // 5. CÁC KHOẢN PHỤ CẤP SẢN XUẤT, ĐỘC HẠI, ĂN CA, XA NHÀ
            $toxicBase = BigDecimal::of((string)($overrideWorkData['toxic_allowance'] ?? $currentSalary?->toxic_allowance ?? 0));
            $farmFieldBase = BigDecimal::of((string)($overrideWorkData['farm_field_allowance'] ?? $currentSalary?->farm_field_allowance ?? 0));
            $mealBase = BigDecimal::of((string)($overrideWorkData['meal_allowance'] ?? $currentSalary?->meal_allowance ?? 730000));

            // Phụ cấp độc hại & xa nhà tính theo công thực tế nếu đi làm đủ công
            $toxicAlw = $toxicBase;
            $farmFieldAlw = $farmFieldBase;
            $mealAlw = $mealBase;

            $totalAllowances = $toxicAlw->plus($farmFieldAlw)->plus($mealAlw)->plus($nightShiftPay);

            // 6. TRUY LĨNH (RETROACTIVE ARREARS)
            $retroactiveAmount = $this->calculateRetroactiveArrears($employee, $period, $overrideWorkData);

            // 7. TỔNG THU NHẬP (GROSS INCOME)
            // Gross = P1 + P2 + P3 + OT + Phụ cấp (độc hại, ca đêm, xa nhà, ăn ca) + Truy lĩnh
            $grossIncome = $p1Earned->plus($p2Earned)
                ->plus($p3Earned)
                ->plus($totalOtPay)
                ->plus($totalAllowances)
                ->plus($retroactiveAmount);

            // 8. KHẤU TRỪ BẢO HIỂM BẮT BUỘC (ÁP TRẦN 20 LẦN LƯƠNG CƠ SỞ / VÙNG TỪ CSDL)
            $insuranceBase = BigDecimal::of((string)($currentSalary?->insurance_salary ?? 0));

            // Trần BHXH/BHYT = 20 * BASE_SALARY_STATUTORY
            $siHiCap = $baseSalaryStatutory->multipliedBy($siHiCapMultiplier);
            $effectiveSiHiBase = $insuranceBase->isGreaterThan($siHiCap) ? $siHiCap : $insuranceBase;

            // Trần BHTN = 20 * REGIONAL_MIN_WAGE
            $uiCap = $regionalMinWage->multipliedBy($uiCapMultiplier);
            $effectiveUiBase = $insuranceBase->isGreaterThan($uiCap) ? $uiCap : $insuranceBase;

            // NLĐ đóng
            $siEmp = $effectiveSiHiBase->multipliedBy($siEmpRate)->toScale(0, RoundingMode::HALF_UP);
            $hiEmp = $effectiveSiHiBase->multipliedBy($hiEmpRate)->toScale(0, RoundingMode::HALF_UP);
            $uiEmp = $effectiveUiBase->multipliedBy($uiEmpRate)->toScale(0, RoundingMode::HALF_UP);
            $totalStatutoryInsuranceEmp = $siEmp->plus($hiEmp)->plus($uiEmp);

            // Doanh nghiệp đóng
            $siComp = $effectiveSiHiBase->multipliedBy($siCompRate)->toScale(0, RoundingMode::HALF_UP);
            $hiComp = $effectiveSiHiBase->multipliedBy($hiCompRate)->toScale(0, RoundingMode::HALF_UP);
            $uiComp = $effectiveUiBase->multipliedBy($uiCompRate)->toScale(0, RoundingMode::HALF_UP);

            // 9. ĐOÀN PHÍ CÔNG ĐOÀN & ĐẢNG PHÍ
            // Đoàn phí NLĐ: 1% lương đóng BH, tối đa 10% lương cơ sở
            $unionMaxCap = $baseSalaryStatutory->multipliedBy(BigDecimal::of('0.10'));
            $unionRaw = $insuranceBase->multipliedBy($unionFeeEmpRate);
            $unionFeeEmp = $unionRaw->isGreaterThan($unionMaxCap)
                ? $unionMaxCap->toScale(0, RoundingMode::HALF_UP)
                : $unionRaw->toScale(0, RoundingMode::HALF_UP);

            // Kinh phí Công đoàn DN: 2%
            $unionFeeComp = $effectiveSiHiBase->multipliedBy($unionFeeCompRate)->toScale(0, RoundingMode::HALF_UP);

            // Đảng phí (nếu có)
            $isPartyMember = (bool)($overrideWorkData['is_party_member'] ?? $employee->is_party_member ?? false);
            $partyFeeEmp = $isPartyMember
                ? $insuranceBase->multipliedBy($partyFeeRate)->toScale(0, RoundingMode::HALF_UP)
                : BigDecimal::zero();

            // 10. THUẾ THU NHẬP CÁ NHÂN (PIT) LŨY TIẾN TỪNG PHẦN 7 BẬC (KHÔNG HARDCODE)
            // A. Các khoản miễn thuế theo luật thuế TNCN:
            // - Phụ cấp ăn ca: phần <= 730k miễn thuế
            $taxExemptMeal = $mealAlw->isGreaterThan($taxFreeMealCap) ? $taxFreeMealCap : $mealAlw;

            // - Phụ cấp độc hại nặng nhọc miễn thuế theo quy định
            $taxExemptToxic = $toxicAlw;

            // - Tiền OT phần chênh lệch trả cao hơn ban ngày được miễn thuế:
            // OT 150% -> miễn 50% (multiplier - 1.0)
            // OT 200% -> miễn 100% (multiplier - 1.0)
            // OT 300% -> miễn 200% (multiplier - 1.0)
            $otExemptDiff = BigDecimal::zero();
            if ($otNormalHours->isGreaterThan(0) && $otWeekdayMultiplier->isGreaterThan(1)) {
                $diff = $otWeekdayMultiplier->minus(BigDecimal::one());
                $otExemptDiff = $otExemptDiff->plus($otNormalHours->multipliedBy($baseHourlyRate)->multipliedBy($diff));
            }
            if ($otWeekendHours->isGreaterThan(0) && $otWeekendMultiplier->isGreaterThan(1)) {
                $diff = $otWeekendMultiplier->minus(BigDecimal::one());
                $otExemptDiff = $otExemptDiff->plus($otWeekendHours->multipliedBy($baseHourlyRate)->multipliedBy($diff));
            }
            if ($otHolidayHours->isGreaterThan(0) && $otHolidayMultiplier->isGreaterThan(1)) {
                $diff = $otHolidayMultiplier->minus(BigDecimal::one());
                $otExemptDiff = $otExemptDiff->plus($otHolidayHours->multipliedBy($baseHourlyRate)->multipliedBy($diff));
            }
            $otExemptDiff = $otExemptDiff->toScale(0, RoundingMode::HALF_UP);

            // B. Thu nhập chịu thuế = Gross - Các khoản miễn thuế
            $totalTaxExempt = $taxExemptMeal->plus($taxExemptToxic)->plus($otExemptDiff);
            $taxableIncome = $grossIncome->minus($totalTaxExempt);
            if ($taxableIncome->isNegative()) {
                $taxableIncome = BigDecimal::zero();
            }

            // C. Các khoản giảm trừ:
            // - Giảm trừ bản thân (11M)
            // - Giảm trừ người phụ thuộc (4.4M * số người)
            $dependentCount = $overrideWorkData['dependent_count'] ?? $this->getEffectiveDependentCount($employee, $endDate);
            $totalDependentRelief = $dependentTaxRelief->multipliedBy(BigDecimal::of($dependentCount));
            $totalFamilyRelief = $personalTaxRelief->plus($totalDependentRelief);

            // - Giảm trừ bảo hiểm bắt buộc NLĐ
            $totalDeductionsForTax = $totalFamilyRelief->plus($totalStatutoryInsuranceEmp);

            // D. Thu nhập tính thuế (Tax-assessed income)
            $taxAssessedIncome = $taxableIncome->minus($totalDeductionsForTax);
            if ($taxAssessedIncome->isNegative()) {
                $taxAssessedIncome = BigDecimal::zero();
            }

            // E. Tính thuế TNCN lũy tiến từ bảng tax_brackets
            $pitTax = $this->calculateProgressiveTaxFromBrackets($taxAssessedIncome, $taxBrackets);

            // 11. THỰC LĨNH (NET INCOME)
            // Net = Gross - BHXH - BHYT - BHTN - Đoàn phí - Đảng phí - Thuế TNCN - Khấu trừ khác
            $otherDeductions = BigDecimal::of((string)($overrideWorkData['other_deductions'] ?? 0));
            $totalEmployeeDeductions = $totalStatutoryInsuranceEmp
                ->plus($unionFeeEmp)
                ->plus($partyFeeEmp)
                ->plus($pitTax)
                ->plus($otherDeductions);

            $netIncome = $grossIncome->minus($totalEmployeeDeductions);
            if ($netIncome->isNegative()) {
                $netIncome = BigDecimal::zero();
            }

            // 12. LƯU BẢN GHI TỔNG HỢP PAYSLIP
            $orgUnitId = $currentAssignment?->org_unit_id ?? $employee->current_org_unit?->id ?? 1;
            $costCenterCode = $currentAssignment?->orgUnit?->cost_center_code ?? 'CC-DEFAULT';

            $payslip = Payslip::updateOrCreate(
                [
                    'payroll_period_id' => $period->id,
                    'employee_id'       => $employee->id,
                ],
                [
                    'org_unit_id'                 => $orgUnitId,
                    'cost_center_code'            => $costCenterCode,
                    'actual_working_days'         => $actualDays->toFloat(),
                    'paid_leave_days'             => $paidLeaveDays->toFloat(),
                    'p1_position_amount'          => $p1Earned->toBigInteger(),
                    'p2_competency_amount'        => $p2Earned->toBigInteger(),
                    'p3_performance_amount'       => $p3Earned->toBigInteger(),
                    'ot_pay_amount'               => $totalOtPay->toBigInteger(),
                    'total_allowance_amount'      => $totalAllowances->toBigInteger(),
                    'retroactive_amount'          => $retroactiveAmount->toBigInteger(),
                    'gross_income'                => $grossIncome->toBigInteger(),
                    'social_insurance_emp'        => $siEmp->toBigInteger(),
                    'health_insurance_emp'        => $hiEmp->toBigInteger(),
                    'unemployment_insurance_emp'  => $uiEmp->toBigInteger(),
                    'union_fee_emp'               => $unionFeeEmp->toBigInteger(),
                    'party_fee_emp'               => $partyFeeEmp->toBigInteger(),
                    'personal_income_tax'         => $pitTax->toBigInteger(),
                    'other_deductions'            => $otherDeductions->toBigInteger(),
                    'net_income'                  => $netIncome->toBigInteger(),
                    'social_insurance_comp'       => $siComp->toBigInteger(),
                    'health_insurance_comp'       => $hiComp->toBigInteger(),
                    'unemployment_insurance_comp' => $uiComp->toBigInteger(),
                    'union_fee_comp'              => $unionFeeComp->toBigInteger(),
                    'status'                      => 'CONFIRMED',
                ]
            );

            // 13. LƯU PAYSLIP_LINES DẠNG DÒNG CHI TIẾT (EAV) ĐỂ THÊM KHOẢN KHÔNG CẦN SỬA BẢNG
            $payslip->lines()->delete();

            $lines = [
                [
                    'line_code'           => 'P1_POSITION',
                    'line_name'           => 'Lương vị trí chức danh (P1)',
                    'line_type'           => 'EARNING',
                    'amount'              => $p1Earned->toBigInteger(),
                    'calculation_formula' => "({$p1Base} / {$standardDays}) * ({$actualDays} công + {$paidLeaveDays} phép)",
                ],
                [
                    'line_code'           => 'P2_COMPETENCY',
                    'line_name'           => 'Lương năng lực & đóng góp (P2)',
                    'line_type'           => 'EARNING',
                    'amount'              => $p2Earned->toBigInteger(),
                    'calculation_formula' => "({$p2Base} / {$standardDays}) * ({$actualDays} công + {$paidLeaveDays} phép)",
                ],
                [
                    'line_code'           => 'P3_PERFORMANCE',
                    'line_name'           => 'Lương hiệu suất KPI (P3)',
                    'line_type'           => 'EARNING',
                    'amount'              => $p3Earned->toBigInteger(),
                    'calculation_formula' => "{$p3Base} * hệ số KPI {$kpiMultiplier}",
                ],
                [
                    'line_code'           => 'OT_WEEKDAY',
                    'line_name'           => 'Làm thêm ngày thường (150%)',
                    'line_type'           => 'EARNING',
                    'amount'              => $otWeekdayPay->toBigInteger(),
                    'calculation_formula' => "{$otNormalHours}h * {$baseHourlyRate}đ * {$otWeekdayMultiplier}",
                ],
                [
                    'line_code'           => 'OT_WEEKEND',
                    'line_name'           => 'Làm thêm ngày nghỉ tuần (200%)',
                    'line_type'           => 'EARNING',
                    'amount'              => $otWeekendPay->toBigInteger(),
                    'calculation_formula' => "{$otWeekendHours}h * {$baseHourlyRate}đ * {$otWeekendMultiplier}",
                ],
                [
                    'line_code'           => 'OT_HOLIDAY',
                    'line_name'           => 'Làm thêm ngày lễ tết (300%)',
                    'line_type'           => 'EARNING',
                    'amount'              => $otHolidayPay->toBigInteger(),
                    'calculation_formula' => "{$otHolidayHours}h * {$baseHourlyRate}đ * {$otHolidayMultiplier}",
                ],
                [
                    'line_code'           => 'ALW_NIGHT_SHIFT',
                    'line_name'           => 'Phụ cấp làm việc ca đêm (+30%)',
                    'line_type'           => 'ALLOWANCE',
                    'amount'              => $nightShiftPay->toBigInteger(),
                    'calculation_formula' => "{$nightWorkHours}h * {$baseHourlyRate}đ * {$nightShiftBonusMultiplier}",
                ],
                [
                    'line_code'           => 'ALW_TOXIC',
                    'line_name'           => 'Phụ cấp độc hại & nặng nhọc',
                    'line_type'           => 'ALLOWANCE',
                    'amount'              => $toxicAlw->toBigInteger(),
                    'calculation_formula' => "Mức hưởng tháng theo hồ sơ chức danh",
                ],
                [
                    'line_code'           => 'ALW_FARM_FIELD',
                    'line_name'           => 'Phụ cấp lưu động / trang trại',
                    'line_type'           => 'ALLOWANCE',
                    'amount'              => $farmFieldAlw->toBigInteger(),
                    'calculation_formula' => "Mức hưởng theo vị trí thực địa",
                ],
                [
                    'line_code'           => 'ALW_MEAL',
                    'line_name'           => 'Phụ cấp tiền ăn ca',
                    'line_type'           => 'ALLOWANCE',
                    'amount'              => $mealAlw->toBigInteger(),
                    'calculation_formula' => "Chế độ ăn ca Vinamilk (miễn thuế tối đa {$taxFreeMealCap}đ)",
                ],
                [
                    'line_code'           => 'RETRO_ARREARS',
                    'line_name'           => 'Khoản truy lĩnh nâng lương / hồi tố',
                    'line_type'           => 'BONUS',
                    'amount'              => $retroactiveAmount->toBigInteger(),
                    'calculation_formula' => "Tổng hợp chênh lệch từ các quyết định điều chỉnh có hiệu lực hồi tố",
                ],
                [
                    'line_code'           => 'DED_SI_EMP',
                    'line_name'           => 'BHXH Người lao động (8%)',
                    'line_type'           => 'STATUTORY_DEDUCTION',
                    'amount'              => $siEmp->toBigInteger(),
                    'calculation_formula' => "min({$insuranceBase}, trần {$siHiCap}) * {$siEmpRate}",
                ],
                [
                    'line_code'           => 'DED_HI_EMP',
                    'line_name'           => 'BHYT Người lao động (1.5%)',
                    'line_type'           => 'STATUTORY_DEDUCTION',
                    'amount'              => $hiEmp->toBigInteger(),
                    'calculation_formula' => "min({$insuranceBase}, trần {$siHiCap}) * {$hiEmpRate}",
                ],
                [
                    'line_code'           => 'DED_UI_EMP',
                    'line_name'           => 'BHTN Người lao động (1%)',
                    'line_type'           => 'STATUTORY_DEDUCTION',
                    'amount'              => $uiEmp->toBigInteger(),
                    'calculation_formula' => "min({$insuranceBase}, trần {$uiCap}) * {$uiEmpRate}",
                ],
                [
                    'line_code'           => 'DED_UNION_EMP',
                    'line_name'           => 'Đoàn phí Công đoàn NLĐ (1%)',
                    'line_type'           => 'STATUTORY_DEDUCTION',
                    'amount'              => $unionFeeEmp->toBigInteger(),
                    'calculation_formula' => "min({$insuranceBase} * {$unionFeeEmpRate}, trần 10% cơ sở {$unionMaxCap})",
                ],
                [
                    'line_code'           => 'DED_PARTY_EMP',
                    'line_name'           => 'Đảng phí Đảng viên',
                    'line_type'           => 'STATUTORY_DEDUCTION',
                    'amount'              => $partyFeeEmp->toBigInteger(),
                    'calculation_formula' => $isPartyMember ? "{$insuranceBase} * {$partyFeeRate}" : 'Không áp dụng',
                ],
                [
                    'line_code'           => 'DED_PIT',
                    'line_name'           => 'Thuế thu nhập cá nhân lũy tiến',
                    'line_type'           => 'TAX_DEDUCTION',
                    'amount'              => $pitTax->toBigInteger(),
                    'calculation_formula' => "Biểu thuế 7 bậc trên thu nhập tính thuế {$taxAssessedIncome}đ (Bản thân {$personalTaxRelief}đ, {$dependentCount} phụ thuộc {$totalDependentRelief}đ)",
                ],
                [
                    'line_code'           => 'ER_SI_COMP',
                    'line_name'           => 'BHXH Doanh nghiệp đóng (17.5%)',
                    'line_type'           => 'EMPLOYER_CONTRIBUTION',
                    'amount'              => $siComp->toBigInteger(),
                    'calculation_formula' => "{$effectiveSiHiBase} * {$siCompRate}",
                ],
                [
                    'line_code'           => 'ER_HI_COMP',
                    'line_name'           => 'BHYT Doanh nghiệp đóng (3%)',
                    'line_type'           => 'EMPLOYER_CONTRIBUTION',
                    'amount'              => $hiComp->toBigInteger(),
                    'calculation_formula' => "{$effectiveSiHiBase} * {$hiCompRate}",
                ],
                [
                    'line_code'           => 'ER_UI_COMP',
                    'line_name'           => 'BHTN Doanh nghiệp đóng (1%)',
                    'line_type'           => 'EMPLOYER_CONTRIBUTION',
                    'amount'              => $uiComp->toBigInteger(),
                    'calculation_formula' => "{$effectiveUiBase} * {$uiCompRate}",
                ],
                [
                    'line_code'           => 'ER_UNION_COMP',
                    'line_name'           => 'Kinh phí Công đoàn DN (2%)',
                    'line_type'           => 'EMPLOYER_CONTRIBUTION',
                    'amount'              => $unionFeeComp->toBigInteger(),
                    'calculation_formula' => "{$effectiveSiHiBase} * {$unionFeeCompRate}",
                ],
            ];

            foreach ($lines as $line) {
                // Chỉ lưu dòng nếu số tiền > 0 hoặc là khoản bắt buộc để đối soát
                if ($line['amount'] > 0 || in_array($line['line_code'], ['P1_POSITION', 'P2_COMPETENCY', 'DED_PIT'])) {
                    PayslipLine::create([
                        'payslip_id'          => $payslip->id,
                        'line_code'           => $line['line_code'],
                        'line_name'           => $line['line_name'],
                        'line_type'           => $line['line_type'],
                        'amount'              => $line['amount'],
                        'calculation_formula' => $line['calculation_formula'],
                    ]);
                }
            }

            return $payslip;
        });
    }

    /**
     * TÍNH THUẾ TNCN LŨY TIẾN TỪNG PHẦN 7 BẬC BẰNG BIỂU THUẾ TRONG CSDL (KHÔNG HARDCODE)
     */
    public function calculateProgressiveTaxFromBrackets(BigDecimal $taxAssessed, Collection $brackets): BigDecimal
    {
        if ($taxAssessed->isZero() || $taxAssessed->isNegative() || $brackets->isEmpty()) {
            return BigDecimal::zero();
        }

        // Tìm bậc phù hợp bằng số trừ nhanh (hoặc lũy tiến từng phần)
        foreach ($brackets as $b) {
            $min = BigDecimal::of((string)$b->min_income);
            $max = $b->max_income !== null ? BigDecimal::of((string)$b->max_income) : null;
            $rate = BigDecimal::of((string)$b->tax_rate)->dividedBy(BigDecimal::of(100), 4, RoundingMode::HALF_UP);
            $sub = BigDecimal::of((string)$b->quick_deduction);

            if ($max === null || $taxAssessed->isLessThanOrEqualTo($max)) {
                $tax = $taxAssessed->multipliedBy($rate)->minus($sub);
                return $tax->isNegative() ? BigDecimal::zero() : $tax->toScale(0, RoundingMode::HALF_UP);
            }
        }

        return BigDecimal::zero();
    }

    // =========================================================================
    // TRỢ GIÚP DỮ LIỆU ĐẦU VÀO TÍNH LƯƠNG
    // =========================================================================

    private function getActualWorkingDays(Employee $employee, Carbon $startDate, Carbon $endDate): float
    {
        // Đếm công thực tế từ bảng chấm công
        $days = AttendanceDay::where('employee_id', $employee->id)
            ->whereBetween('work_date', [$startDate->toDateString(), $endDate->toDateString()])
            ->whereIn('status', ['PRESENT', 'BUSINESS_TRIP'])
            ->count();

        return $days > 0 ? (float)$days : 22.0; // Fallback công chuẩn nếu chưa có log máy chấm công
    }

    private function getPaidLeaveDays(Employee $employee, Carbon $startDate, Carbon $endDate): float
    {
        $paidLeaves = LeaveRequest::where('employee_id', $employee->id)
            ->where('status', 'APPROVED')
            ->whereBetween('start_date', [$startDate->toDateString(), $endDate->toDateString()])
            ->whereHas('leaveType', fn ($q) => $q->where('is_paid', true))
            ->sum('total_days');

        return (float)$paidLeaves;
    }

    private function getApprovedKpiMultiplier(Employee $employee, PayrollPeriod $period): float
    {
        $summary = KpiEmployeeSummary::where('employee_id', $employee->id)
            ->where('status', 'APPROVED')
            ->latest('id')
            ->first();

        if ($summary && $summary->p3_multiplier > 0) {
            return (float)$summary->p3_multiplier;
        }

        if ($summary && $summary->kpi_score > 0) {
            return (float)($summary->kpi_score / 100);
        }

        return 1.0; // Mặc định 100% nếu chưa có kỳ đánh giá
    }

    private function getOtHours(Employee $employee, Carbon $startDate, Carbon $endDate, string $type): float
    {
        $query = AttendanceDay::where('employee_id', $employee->id)
            ->whereBetween('work_date', [$startDate->toDateString(), $endDate->toDateString()]);

        return match ($type) {
            'NORMAL'  => (float)$query->sum('ot_normal_hours'),
            'WEEKEND' => (float)$query->sum('ot_weekend_hours'),
            'HOLIDAY' => (float)$query->sum('ot_holiday_hours'),
            default   => 0.0,
        };
    }

    private function getNightWorkHours(Employee $employee, Carbon $startDate, Carbon $endDate): float
    {
        return (float)AttendanceDay::where('employee_id', $employee->id)
            ->whereBetween('work_date', [$startDate->toDateString(), $endDate->toDateString()])
            ->sum('night_work_hours');
    }

    private function getEffectiveDependentCount(Employee $employee, Carbon $asOfDate): int
    {
        $dateStr = $asOfDate->toDateString();

        return Dependent::where('employee_id', $employee->id)
            ->where('is_active', true)
            ->where('effective_from', '<=', $dateStr)
            ->where(function ($q) use ($dateStr) {
                $q->whereNull('effective_to')
                  ->orWhere('effective_to', '>=', $dateStr);
            })
            ->count();
    }

    private function calculateRetroactiveArrears(Employee $employee, PayrollPeriod $period, ?array $overrideWorkData): BigDecimal
    {
        if (isset($overrideWorkData['retroactive_amount'])) {
            return BigDecimal::of((string)$overrideWorkData['retroactive_amount']);
        }

        $pendingAdjustments = RetroactiveSalaryAdjustment::where('payroll_period_id', $period->id)
            ->where('employee_id', $employee->id)
            ->where('status', 'PENDING')
            ->get();

        $total = BigDecimal::zero();
        foreach ($pendingAdjustments as $adj) {
            $total = $total->plus(BigDecimal::of((string)$adj->retroactive_difference));
            $adj->update(['status' => 'PROCESSED']);
        }

        return $total;
    }
}
