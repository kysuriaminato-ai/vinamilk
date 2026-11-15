using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.Payroll;

public sealed record PayrollCalculationInput(
    decimal ContractBaseSalary,
    decimal PositionAllowance,
    decimal OtherAllowance,
    decimal StandardWorkdays,
    decimal PayableWorkdays,
    decimal OtHours150,
    decimal OtHours200,
    decimal OtHours300,
    decimal KpiBonus,
    decimal DisciplineDeduction,
    int DependentCount,
    bool IsUnionMember = true,
    int WageRegion = 1,
    decimal UnusedLeavePayout = 0m,
    decimal? InsuranceContractSalaryOverride = null,
    decimal? PrecomputedActualBaseSalary = null,
    decimal? PrecomputedOvertimePay = null,
    decimal ExtraTemplateEarnings = 0m,
    decimal ExtraTaxExemptAmount = 0m,
    decimal CustomDeductions = 0m,
    decimal OtHours150Night = 0m,
    decimal OtHours200Night = 0m,
    decimal OtHours300Night = 0m,
    decimal RegularNightHours = 0m,
    decimal CommissionBonus = 0m,
    decimal ThirteenthMonthBonus = 0m,
    decimal RetroEarningAmount = 0m,
    decimal RetroDeductionAmount = 0m,
    decimal SalaryAdvanceDeduction = 0m,
    decimal LoanInstallmentDeduction = 0m,
    string TaxResidencyMode = "ResidentStandard",
    bool HasPitCommitment08 = false,
    decimal? PrecomputedOtNightExtraPay = null,
    decimal? PrecomputedOtTaxExemptAmount = null,
    bool IsBonusPeriodOnly = false,
    decimal PriorMonthAssessableIncome = 0m,
    bool SkipRegularNightAllowance = false,
    bool SkipEmployeeUnionFee = false,
    bool HasPriorMonthlyDisbursement = false,
    decimal PriorMonthTaxableIncome = 0m,
    decimal PriorMonthPitWithheld = 0m,
    decimal SourcePeriodTaxDelta = 0m,
    decimal SourcePeriodAdjustedTaxableExcluded = 0m,
    decimal TargetPeriodTaxableDeductionFromRetro = 0m);

public sealed record MidMonthAggregatedPayrollResult(
    PayrollCalculationBreakdown Breakdown,
    decimal EffectiveContractBaseSalary,
    decimal EffectivePositionAllowance,
    decimal EffectiveOtherAllowance,
    decimal ExtraTemplateEarnings,
    decimal TaxExemptAmount,
    decimal CustomDeductions,
    decimal InsuranceContractSalaryUsed,
    IReadOnlyList<MidMonthPayrollSliceBreakdown> Slices,
    IReadOnlyList<EvaluatedSalaryLineDto> AggregatedLines,
    string? SplitSummaryNote,
    decimal OtTaxExemptAmount = 0m,
    decimal OtNightExtraPay = 0m);

public sealed record PayrollStatutoryContext(
    PayrollStatutoryParameter Parameter,
    IReadOnlyList<PitBracket> PitBrackets);

public sealed record PayrollCalculationBreakdown(
    decimal ActualBaseSalary,
    decimal OvertimePay,
    decimal GrossIncome,
    decimal SocialInsuranceBase,
    decimal UnemploymentInsuranceBase,
    decimal EmployeeSocialInsurance,
    decimal EmployeeHealthInsurance,
    decimal EmployeeUnemploymentInsurance,
    decimal TotalEmployeeInsurance,
    decimal CompanySocialInsurance,
    decimal CompanyHealthInsurance,
    decimal CompanyUnemploymentInsurance,
    decimal CompanyUnionFee,
    decimal SelfDeductionAmount,
    decimal DependentDeductionAmount,
    decimal AssessableIncome,
    decimal PersonalIncomeTax,
    decimal NetSalary,
    decimal OtNightExtraPay = 0m,
    decimal OtTaxExemptAmount = 0m,
    decimal RegularNightShiftPay = 0m,
    decimal CommissionBonus = 0m,
    decimal ThirteenthMonthBonus = 0m,
    decimal RetroEarningAmount = 0m,
    decimal RetroDeductionAmount = 0m,
    decimal SalaryAdvanceDeduction = 0m,
    decimal LoanInstallmentDeduction = 0m,
    decimal EmployeeUnionFee = 0m,
    decimal CompanyOccupationalAccidentInsurance = 0m,
    decimal TotalCompanyCost = 0m,
    decimal TaxableIncome = 0m,
    string TaxResidencyMode = "ResidentStandard",
    bool UsedCommitment08 = false);

public sealed record CalculationTraceStepDto(
    int StepOrder,
    string Category,
    string ItemName,
    string Formula,
    string VariablesSummary,
    decimal ResultAmount,
    string? LegalReference = null,
    decimal? SecondaryAmount = null);

public sealed record NetToGrossInput(
    decimal TargetNetSalary,
    decimal PositionAllowance = 0m,
    decimal OtherAllowance = 0m,
    decimal StandardWorkdays = 26m,
    decimal PayableWorkdays = 26m,
    decimal OtHours150 = 0m,
    decimal OtHours200 = 0m,
    decimal OtHours300 = 0m,
    decimal KpiBonus = 0m,
    decimal CommissionBonus = 0m,
    decimal ThirteenthMonthBonus = 0m,
    decimal UnusedLeavePayout = 0m,
    decimal RetroEarningAmount = 0m,
    decimal DisciplineDeduction = 0m,
    decimal RetroDeductionAmount = 0m,
    decimal SalaryAdvanceDeduction = 0m,
    decimal LoanInstallmentDeduction = 0m,
    int DependentCount = 0,
    bool IsUnionMember = true,
    int WageRegion = 1,
    decimal? FixedInsuranceContractSalary = null,
    string TaxResidencyMode = "ResidentStandard",
    bool HasPitCommitment08 = false,
    decimal OtHours150Night = 0m,
    decimal OtHours200Night = 0m,
    decimal OtHours300Night = 0m,
    decimal RegularNightHours = 0m,
    decimal ExtraTemplateEarnings = 0m,
    decimal ExtraTaxExemptAmount = 0m,
    decimal CustomDeductions = 0m,
    bool HasPriorMonthlyDisbursement = false,
    decimal PriorMonthTaxableIncome = 0m,
    decimal PriorMonthPitWithheld = 0m,
    decimal SourcePeriodTaxDelta = 0m,
    decimal SourcePeriodAdjustedTaxableExcluded = 0m,
    decimal TargetPeriodTaxableDeductionFromRetro = 0m);

public sealed record NetToGrossSolution(
    decimal SolvedContractBaseSalary,
    PayrollCalculationBreakdown Breakdown,
    decimal TargetNetSalary,
    decimal AchievedNetSalary,
    decimal ResidualDifference,
    int IterationsUsed,
    bool Converged);

/// <summary>
/// Động cơ tính lương, BHXH/BHYT/BHTN/BHTNLĐ-BNN, Đoàn phí CĐ và Thuế TNCN theo tham số hiệu lực tại ngày cuối kỳ lương.
/// Không chứa bất kỳ hằng số pháp lý ghi cứng nào; toàn bộ tham số được truyền từ bảng cấu hình hiệu lực.
/// </summary>
public static class VietnamPayrollEngine
{
    public static async Task<PayrollStatutoryContext> ResolveStatutoryContextAsync(
        IApplicationDbContext context,
        DateOnly periodEndDate,
        CancellationToken cancellationToken = default)
    {
        var result = await TryResolveStatutoryContextAsync(context, periodEndDate, cancellationToken);
        return result ?? throw new InvalidOperationException(
            $"Chưa cấu hình đầy đủ tham số lương/BH hoặc biểu thuế TNCN có hiệu lực tại ngày {periodEndDate:dd/MM/yyyy}.");
    }

    public static async Task<PayrollStatutoryContext?> TryResolveStatutoryContextAsync(
        IApplicationDbContext context,
        DateOnly periodEndDate,
        CancellationToken cancellationToken = default)
    {
        var parameter = await context.PayrollStatutoryParameters
            .AsNoTracking()
            .Where(p => p.EffectiveFrom <= periodEndDate && (p.EffectiveTo == null || p.EffectiveTo >= periodEndDate))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (parameter is null)
            return null;

        var matchingBrackets = await context.PitBrackets
            .AsNoTracking()
            .Where(b => b.EffectiveFrom <= periodEndDate && (b.EffectiveTo == null || b.EffectiveTo >= periodEndDate))
            .OrderByDescending(b => b.EffectiveFrom)
            .ThenBy(b => b.BracketLevel)
            .ToListAsync(cancellationToken);

        if (matchingBrackets.Count == 0)
            return null;

        var activeEffectiveFrom = matchingBrackets[0].EffectiveFrom;
        var activeBrackets = matchingBrackets
            .Where(b => b.EffectiveFrom == activeEffectiveFrom)
            .OrderBy(b => b.BracketLevel)
            .ThenBy(b => b.LowerBound)
            .ToList();

        return new PayrollStatutoryContext(parameter, activeBrackets);
    }

    public static PayrollCalculationBreakdown Calculate(
        PayrollCalculationInput input,
        PayrollStatutoryContext statutoryContext)
    {
        ArgumentNullException.ThrowIfNull(statutoryContext);
        return Calculate(input, statutoryContext.Parameter, statutoryContext.PitBrackets);
    }

    public static PayrollCalculationBreakdown Calculate(
        PayrollCalculationInput input,
        PayrollStatutoryParameter parameter,
        IReadOnlyList<PitBracket> pitBrackets)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (pitBrackets is null || pitBrackets.Count == 0)
            throw new ArgumentException("Biểu thuế TNCN hiệu lực không được rỗng.", nameof(pitBrackets));
        if (input.StandardWorkdays <= 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Số ngày công chuẩn kỳ lương phải lớn hơn 0.");

        var residencyMode = string.IsNullOrWhiteSpace(input.TaxResidencyMode)
            ? "ResidentStandard"
            : input.TaxResidencyMode.Trim();

        decimal actualBase = input.IsBonusPeriodOnly
            ? 0m
            : input.PrecomputedActualBaseSalary ?? Math.Round(
                input.ContractBaseSalary * (input.PayableWorkdays / input.StandardWorkdays),
                0,
                MidpointRounding.AwayFromZero);

        decimal hourlyRate = input.ContractBaseSalary / (input.StandardWorkdays * 8m);

        decimal mult150 = parameter.OtMultiplier150;
        decimal mult200 = parameter.OtMultiplier200;
        decimal mult300 = parameter.OtMultiplier300;
        decimal nightBonusRate = parameter.OtNightShiftBonusRate;
        decimal nightOtExtraRate = parameter.OtNightOvertimeExtraRate;
        decimal nightMult150 = mult150 + nightBonusRate + (nightOtExtraRate * mult150);
        decimal nightMult200 = mult200 + nightBonusRate + (nightOtExtraRate * mult200);
        decimal nightMult300 = mult300 + nightBonusRate + (nightOtExtraRate * mult300);

        decimal regularNightPay = input.IsBonusPeriodOnly || input.SkipRegularNightAllowance || input.RegularNightHours <= 0m
            ? 0m
            : Math.Round(hourlyRate * input.RegularNightHours * nightBonusRate, 0, MidpointRounding.AwayFromZero);

        decimal otPay;
        decimal otNightExtraPay;
        decimal otTaxExemptAmount;

        if (input.IsBonusPeriodOnly)
        {
            otPay = 0m;
            otNightExtraPay = 0m;
            otTaxExemptAmount = 0m;
        }
        else if (input.PrecomputedOvertimePay.HasValue)
        {
            decimal nightOtPay = Math.Round(
                hourlyRate * (input.OtHours150Night * nightMult150 + input.OtHours200Night * nightMult200 + input.OtHours300Night * nightMult300),
                0,
                MidpointRounding.AwayFromZero);

            otPay = input.PrecomputedOvertimePay.Value + nightOtPay + regularNightPay;

            if (input.PrecomputedOtNightExtraPay.HasValue)
            {
                otNightExtraPay = input.PrecomputedOtNightExtraPay.Value + regularNightPay;
            }
            else
            {
                decimal nightOtAtDayRate = Math.Round(
                    hourlyRate * (input.OtHours150Night * mult150 + input.OtHours200Night * mult200 + input.OtHours300Night * mult300),
                    0,
                    MidpointRounding.AwayFromZero);
                otNightExtraPay = Math.Max(0m, nightOtPay - nightOtAtDayRate) + regularNightPay;
            }

            if (input.PrecomputedOtTaxExemptAmount.HasValue)
            {
                otTaxExemptAmount = parameter.IsOtExcessTaxExempt
                    ? input.PrecomputedOtTaxExemptAmount.Value + regularNightPay
                    : 0m;
            }
            else if (parameter.IsOtExcessTaxExempt)
            {
                decimal totalOtHoursAll = input.OtHours150 + input.OtHours200 + input.OtHours300
                    + input.OtHours150Night + input.OtHours200Night + input.OtHours300Night;
                decimal otNormalBase = Math.Round(
                    hourlyRate * totalOtHoursAll * parameter.OtTaxableBaseRate,
                    0,
                    MidpointRounding.AwayFromZero);
                otTaxExemptAmount = Math.Max(0m, (input.PrecomputedOvertimePay.Value + nightOtPay) - otNormalBase) + regularNightPay;
            }
            else
            {
                otTaxExemptAmount = 0m;
            }
        }
        else
        {
            decimal dayOtPay = Math.Round(
                hourlyRate * (input.OtHours150 * mult150 + input.OtHours200 * mult200 + input.OtHours300 * mult300),
                0,
                MidpointRounding.AwayFromZero);
            decimal nightOtPay = Math.Round(
                hourlyRate * (input.OtHours150Night * nightMult150 + input.OtHours200Night * nightMult200 + input.OtHours300Night * nightMult300),
                0,
                MidpointRounding.AwayFromZero);
            decimal nightOtAtDayRate = Math.Round(
                hourlyRate * (input.OtHours150Night * mult150 + input.OtHours200Night * mult200 + input.OtHours300Night * mult300),
                0,
                MidpointRounding.AwayFromZero);

            otPay = dayOtPay + nightOtPay + regularNightPay;
            otNightExtraPay = Math.Max(0m, nightOtPay - nightOtAtDayRate) + regularNightPay;

            if (parameter.IsOtExcessTaxExempt)
            {
                decimal totalOtHoursAll = input.OtHours150 + input.OtHours200 + input.OtHours300
                    + input.OtHours150Night + input.OtHours200Night + input.OtHours300Night;
                decimal otNormalBase = Math.Round(
                    hourlyRate * totalOtHoursAll * parameter.OtTaxableBaseRate,
                    0,
                    MidpointRounding.AwayFromZero);
                otTaxExemptAmount = Math.Max(0m, (dayOtPay + nightOtPay) - otNormalBase) + regularNightPay;
            }
            else
            {
                otTaxExemptAmount = 0m;
            }
        }

        decimal posAllow = input.IsBonusPeriodOnly ? 0m : input.PositionAllowance;
        decimal otherAllow = input.IsBonusPeriodOnly ? 0m : input.OtherAllowance;

        decimal gross = actualBase
            + posAllow
            + otherAllow
            + otPay
            + input.KpiBonus
            + input.CommissionBonus
            + input.ThirteenthMonthBonus
            + input.UnusedLeavePayout
            + input.RetroEarningAmount
            + input.ExtraTemplateEarnings;

        decimal siCap = parameter.StatutoryBaseSalary * parameter.SocialInsuranceCapMultiplier;
        decimal regionalMinWage = parameter.GetRegionalMinWage(input.WageRegion);
        decimal uiCap = regionalMinWage * parameter.UnemploymentInsuranceCapMultiplier;

        bool skipSocialInsurance = input.IsBonusPeriodOnly
            || (string.Equals(residencyMode, "NonResident", StringComparison.OrdinalIgnoreCase)
                && !input.InsuranceContractSalaryOverride.HasValue);

        decimal insContractSalary = skipSocialInsurance
            ? 0m
            : (input.InsuranceContractSalaryOverride ?? (input.ContractBaseSalary + input.PositionAllowance));
        insContractSalary = Math.Max(0m, insContractSalary);

        decimal siBase = Math.Min(insContractSalary, siCap);
        decimal uiBase = Math.Min(insContractSalary, uiCap);

        decimal empSi = Math.Round(siBase * parameter.EmployeeSiRate, 0, MidpointRounding.AwayFromZero);
        decimal empHi = Math.Round(siBase * parameter.EmployeeHiRate, 0, MidpointRounding.AwayFromZero);
        decimal empUi = Math.Round(uiBase * parameter.EmployeeUiRate, 0, MidpointRounding.AwayFromZero);
        decimal totalEmpIns = empSi + empHi + empUi;

        decimal empUnionFee = 0m;
        if (!input.IsBonusPeriodOnly && !input.SkipEmployeeUnionFee && input.IsUnionMember && parameter.EmployeeUnionFeeRate > 0m)
        {
            decimal unionRaw = Math.Round(siBase * parameter.EmployeeUnionFeeRate, 0, MidpointRounding.AwayFromZero);
            decimal unionCap = parameter.EmployeeUnionFeeCapMultiplier > 0m
                ? Math.Round(parameter.StatutoryBaseSalary * parameter.EmployeeUnionFeeCapMultiplier, 0, MidpointRounding.AwayFromZero)
                : unionRaw;
            empUnionFee = Math.Min(unionRaw, unionCap);
        }

        decimal compSi = Math.Round(siBase * parameter.CompanySiRate, 0, MidpointRounding.AwayFromZero);
        decimal compHi = Math.Round(siBase * parameter.CompanyHiRate, 0, MidpointRounding.AwayFromZero);
        decimal compUi = Math.Round(uiBase * parameter.CompanyUiRate, 0, MidpointRounding.AwayFromZero);
        decimal compOa = Math.Round(siBase * parameter.CompanyOccupationalAccidentRate, 0, MidpointRounding.AwayFromZero);
        decimal compUnionFee = !input.IsBonusPeriodOnly && input.IsUnionMember
            ? Math.Round(siBase * parameter.CompanyUnionFeeRate, 0, MidpointRounding.AwayFromZero)
            : 0m;

        decimal totalExempt = Math.Max(0m, input.ExtraTaxExemptAmount) + Math.Max(0m, otTaxExemptAmount);
        decimal taxableIncome = Math.Max(0m,
            gross
            - totalEmpIns
            - totalExempt
            - Math.Max(0m, input.SourcePeriodAdjustedTaxableExcluded)
            - Math.Max(0m, input.TargetPeriodTaxableDeductionFromRetro));

        decimal selfDeduction = 0m;
        decimal depDeduction = 0m;
        decimal assessableIncome = 0m;
        decimal pit = 0m;
        bool usedCommitment08 = false;

        if (string.Equals(residencyMode, "NonResident", StringComparison.OrdinalIgnoreCase))
        {
            selfDeduction = 0m;
            depDeduction = 0m;
            assessableIncome = taxableIncome;
            if (input.HasPriorMonthlyDisbursement)
            {
                decimal combinedTax = Math.Round((input.PriorMonthTaxableIncome + assessableIncome) * parameter.PitNonResidentRate, 0, MidpointRounding.AwayFromZero);
                pit = Math.Max(0m, combinedTax - input.PriorMonthPitWithheld);
            }
            else
            {
                pit = Math.Round(assessableIncome * parameter.PitNonResidentRate, 0, MidpointRounding.AwayFromZero);
            }
        }
        else if (string.Equals(residencyMode, "ResidentShortTerm", StringComparison.OrdinalIgnoreCase))
        {
            selfDeduction = 0m;
            depDeduction = 0m;
            assessableIncome = taxableIncome;
            decimal combinedTaxable = input.HasPriorMonthlyDisbursement
                ? input.PriorMonthTaxableIncome + taxableIncome
                : taxableIncome;
            if (combinedTaxable >= parameter.PitShortTermThreshold)
            {
                if (input.HasPitCommitment08)
                {
                    pit = 0m;
                    usedCommitment08 = true;
                }
                else
                {
                    decimal totalShortPit = Math.Round(combinedTaxable * parameter.PitShortTermRate, 0, MidpointRounding.AwayFromZero);
                    pit = input.HasPriorMonthlyDisbursement
                        ? Math.Max(0m, totalShortPit - input.PriorMonthPitWithheld)
                        : totalShortPit;
                }
            }
            else
            {
                pit = 0m;
            }
        }
        else
        {
            // ResidentStandard (HĐLĐ >= 3 tháng): biểu lũy tiến từng phần
            if (input.HasPriorMonthlyDisbursement)
            {
                // Nhiều đợt chi trong cùng tháng: tính thuế TNCN tổng trên tất cả các đợt chi rồi trừ số đã khấu trừ ở các đợt trước
                selfDeduction = 0m;
                depDeduction = 0m;
                decimal fullMonthSelf = parameter.SelfDeduction;
                decimal fullMonthDep = Math.Max(0, input.DependentCount) * parameter.DependentDeduction;
                decimal priorAssess = Math.Max(0m, input.PriorMonthTaxableIncome - fullMonthSelf - fullMonthDep);
                decimal combinedTaxable = input.PriorMonthTaxableIncome + taxableIncome;
                decimal combinedAssessable = Math.Max(0m, combinedTaxable - fullMonthSelf - fullMonthDep);
                assessableIncome = Math.Max(0m, combinedAssessable - priorAssess);
                decimal totalMonthlyPit = CalculateProgressivePit(combinedAssessable, pitBrackets);
                pit = Math.Max(0m, totalMonthlyPit - input.PriorMonthPitWithheld);
            }
            else if (input.IsBonusPeriodOnly && input.PriorMonthAssessableIncome > 0m)
            {
                // Kỳ thưởng tách riêng trong cùng tháng đã tính lương chính: không giảm trừ gia cảnh 2 lần
                selfDeduction = 0m;
                depDeduction = 0m;
                assessableIncome = taxableIncome;
                decimal taxWithBonus = CalculateProgressivePit(input.PriorMonthAssessableIncome + assessableIncome, pitBrackets);
                decimal taxMonthlyOnly = CalculateProgressivePit(input.PriorMonthAssessableIncome, pitBrackets);
                pit = Math.Max(0m, taxWithBonus - taxMonthlyOnly);
            }
            else
            {
                selfDeduction = parameter.SelfDeduction;
                depDeduction = Math.Max(0, input.DependentCount) * parameter.DependentDeduction;
                assessableIncome = Math.Max(0m, taxableIncome - selfDeduction - depDeduction);
                pit = CalculateProgressivePit(assessableIncome, pitBrackets);
            }
        }

        decimal pitRefundCredit = 0m;
        if (input.SourcePeriodTaxDelta != 0m)
        {
            decimal adjustedPit = pit + input.SourcePeriodTaxDelta;
            if (adjustedPit >= 0m)
            {
                pit = adjustedPit;
            }
            else
            {
                pitRefundCredit = Math.Abs(adjustedPit);
                pit = 0m;
            }
        }

        decimal totalOtherDeductions = input.DisciplineDeduction
            + Math.Max(0m, input.CustomDeductions)
            + Math.Max(0m, input.SalaryAdvanceDeduction)
            + Math.Max(0m, input.LoanInstallmentDeduction)
            + Math.Max(0m, input.RetroDeductionAmount)
            + empUnionFee;

        decimal net = Math.Max(0m, gross + pitRefundCredit - totalEmpIns - pit - totalOtherDeductions);
        decimal totalCompanyCost = gross + compSi + compHi + compUi + compOa + compUnionFee;

        return new PayrollCalculationBreakdown(
            actualBase,
            otPay,
            gross,
            siBase,
            uiBase,
            empSi,
            empHi,
            empUi,
            totalEmpIns,
            compSi,
            compHi,
            compUi,
            compUnionFee,
            selfDeduction,
            depDeduction,
            assessableIncome,
            pit,
            net,
            otNightExtraPay,
            otTaxExemptAmount,
            regularNightPay,
            input.CommissionBonus,
            input.ThirteenthMonthBonus,
            input.RetroEarningAmount,
            input.RetroDeductionAmount,
            input.SalaryAdvanceDeduction,
            input.LoanInstallmentDeduction,
            empUnionFee,
            compOa,
            totalCompanyCost,
            taxableIncome,
            residencyMode,
            usedCommitment08);
    }

    /// <summary>
    /// Xác định mức lương đóng BHXH áp dụng từ hồ sơ lương nhân viên:
    /// nhóm thử việc (<see cref="EmployeeSalaryProfile.IsProbation"/>), HĐ ngắn hạn (&lt; 3 tháng / ResidentShortTerm)
    /// hoặc không cư trú (NonResident) giữ nguyên giá trị 0 khi chưa tham gia BHXH bắt buộc.
    /// </summary>
    internal static decimal? ResolveInsuranceOverride(EmployeeSalaryProfile? profile)
    {
        if (profile is null)
            return null;

        if (profile.InsuranceSalary > 0m)
            return profile.InsuranceSalary;

        bool exemptIfZero = profile.IsProbation
            || string.Equals(profile.TaxResidencyMode, "NonResident", StringComparison.OrdinalIgnoreCase)
            || string.Equals(profile.TaxResidencyMode, "ResidentShortTerm", StringComparison.OrdinalIgnoreCase);

        return exemptIfZero ? 0m : null;
    }

    /// <summary>
    /// Quy đổi Net → Gross bằng tìm kiếm nhị phân theo quy tắc BHXH/BHYT/BHTN và thuế TNCN hiện hành,
    /// hội tụ sai số <= 1 đồng VND.
    /// </summary>
    public static NetToGrossSolution SolveGrossFromNet(
        NetToGrossInput input,
        PayrollStatutoryContext statutoryContext)
    {
        ArgumentNullException.ThrowIfNull(statutoryContext);
        return SolveGrossFromNet(input, statutoryContext.Parameter, statutoryContext.PitBrackets);
    }

    public static NetToGrossSolution SolveGrossFromNet(
        NetToGrossInput input,
        PayrollStatutoryParameter parameter,
        IReadOnlyList<PitBracket> pitBrackets)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (pitBrackets is null || pitBrackets.Count == 0)
            throw new ArgumentException("Biểu thuế TNCN hiệu lực không được rỗng.", nameof(pitBrackets));
        if (input.TargetNetSalary < 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Lương Net mục tiêu không được âm.");
        if (input.StandardWorkdays <= 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Số ngày công chuẩn phải lớn hơn 0.");

        PayrollCalculationBreakdown Eval(decimal candidateBase) => Calculate(
            new PayrollCalculationInput(
                ContractBaseSalary: candidateBase,
                PositionAllowance: input.PositionAllowance,
                OtherAllowance: input.OtherAllowance,
                StandardWorkdays: input.StandardWorkdays,
                PayableWorkdays: input.PayableWorkdays,
                OtHours150: input.OtHours150,
                OtHours200: input.OtHours200,
                OtHours300: input.OtHours300,
                KpiBonus: input.KpiBonus,
                DisciplineDeduction: input.DisciplineDeduction,
                DependentCount: input.DependentCount,
                IsUnionMember: input.IsUnionMember,
                WageRegion: input.WageRegion,
                UnusedLeavePayout: input.UnusedLeavePayout,
                InsuranceContractSalaryOverride: input.FixedInsuranceContractSalary,
                ExtraTemplateEarnings: input.ExtraTemplateEarnings,
                ExtraTaxExemptAmount: input.ExtraTaxExemptAmount,
                CustomDeductions: input.CustomDeductions,
                OtHours150Night: input.OtHours150Night,
                OtHours200Night: input.OtHours200Night,
                OtHours300Night: input.OtHours300Night,
                RegularNightHours: input.RegularNightHours,
                CommissionBonus: input.CommissionBonus,
                ThirteenthMonthBonus: input.ThirteenthMonthBonus,
                RetroEarningAmount: input.RetroEarningAmount,
                RetroDeductionAmount: input.RetroDeductionAmount,
                SalaryAdvanceDeduction: input.SalaryAdvanceDeduction,
                LoanInstallmentDeduction: input.LoanInstallmentDeduction,
                TaxResidencyMode: input.TaxResidencyMode,
                HasPitCommitment08: input.HasPitCommitment08,
                HasPriorMonthlyDisbursement: input.HasPriorMonthlyDisbursement,
                PriorMonthTaxableIncome: input.PriorMonthTaxableIncome,
                PriorMonthPitWithheld: input.PriorMonthPitWithheld,
                SourcePeriodTaxDelta: input.SourcePeriodTaxDelta,
                SourcePeriodAdjustedTaxableExcluded: input.SourcePeriodAdjustedTaxableExcluded,
                TargetPeriodTaxableDeductionFromRetro: input.TargetPeriodTaxableDeductionFromRetro),
            parameter,
            pitBrackets);

        if (input.TargetNetSalary == 0m)
        {
            var zeroBreakdown = Eval(0m);
            return new NetToGrossSolution(0m, zeroBreakdown, 0m, zeroBreakdown.NetSalary, zeroBreakdown.NetSalary, 0, true);
        }

        decimal low = 0m;
        decimal high = Math.Max(input.TargetNetSalary * 2.5m, input.TargetNetSalary + 50_000_000m);
        int expandCount = 0;
        while (Eval(high).NetSalary < input.TargetNetSalary && expandCount < 16)
        {
            high *= 2m;
            expandCount++;
        }

        int iterations = 0;
        for (int i = 0; i < 80; i++)
        {
            iterations = i + 1;
            decimal mid = (low + high) / 2m;
            decimal roundedMid = Math.Round(mid, 0, MidpointRounding.AwayFromZero);
            var bMid = Eval(roundedMid);
            decimal diff = bMid.NetSalary - input.TargetNetSalary;

            if (Math.Abs(diff) <= 0.5m)
            {
                low = high = roundedMid;
                break;
            }

            if (diff < 0m)
                low = mid;
            else
                high = mid;

            if (high - low <= 0.01m)
                break;
        }

        // Local integer scan around converged bracket to guarantee nearest integer VND (<= 1đ residual)
        decimal center = Math.Round((low + high) / 2m, 0, MidpointRounding.AwayFromZero);
        decimal bestBase = center;
        var bestBreakdown = Eval(center);
        decimal bestAbsDiff = Math.Abs(bestBreakdown.NetSalary - input.TargetNetSalary);

        for (decimal delta = -5m; delta <= 5m; delta += 1m)
        {
            decimal probe = Math.Max(0m, center + delta);
            var bProbe = Eval(probe);
            decimal absDiff = Math.Abs(bProbe.NetSalary - input.TargetNetSalary);
            if (absDiff < bestAbsDiff)
            {
                bestAbsDiff = absDiff;
                bestBase = probe;
                bestBreakdown = bProbe;
            }
        }

        return new NetToGrossSolution(
            SolvedContractBaseSalary: bestBase,
            Breakdown: bestBreakdown,
            TargetNetSalary: input.TargetNetSalary,
            AchievedNetSalary: bestBreakdown.NetSalary,
            ResidualDifference: bestBreakdown.NetSalary - input.TargetNetSalary,
            IterationsUsed: iterations,
            Converged: bestAbsDiff <= 1m);
    }

    /// <summary>
    /// Tổng hợp kỳ lương có thể chia nhiều đoạn hiệu lực trong tháng (đổi lương/phụ cấp giữa tháng),
    /// hỗ trợ hình thức trả Monthly/Daily/Hourly/PieceRate, thử việc, miễn thuế theo trần tháng và lương đóng BH tách riêng.
    /// </summary>
    public static MidMonthAggregatedPayrollResult CalculateFromMidMonthSlices(
        IReadOnlyList<PayrollPeriodProfileSliceInput> slices,
        PayrollStatutoryContext statutoryContext,
        decimal standardWorkdays,
        decimal totalPayableWorkdays,
        decimal totalOt150Day,
        decimal totalOt200Day,
        decimal totalOt300Day,
        int dependentCount,
        int wageRegion,
        decimal unusedLeavePayout = 0m,
        decimal kpiBonus = 0m,
        decimal disciplineDeduction = 0m,
        string roundingRule = "RoundToDong",
        IReadOnlyDictionary<string, decimal>? taxExemptParamValues = null,
        decimal regularNightHours = 0m,
        decimal totalOt150Night = 0m,
        decimal totalOt200Night = 0m,
        decimal totalOt300Night = 0m,
        decimal commissionBonus = 0m,
        decimal thirteenthMonthBonus = 0m,
        decimal retroEarningAmount = 0m,
        decimal retroDeductionAmount = 0m,
        decimal salaryAdvanceDeduction = 0m,
        decimal loanInstallmentDeduction = 0m,
        decimal kpiMultiplier = 1.0m,
        bool isBonusPeriodOnly = false,
        decimal priorMonthAssessableIncome = 0m,
        decimal? solvedBaseSalaryOverride = null,
        bool hasPriorMonthlyDisbursement = false,
        decimal priorMonthTaxableIncome = 0m,
        decimal priorMonthPitWithheld = 0m,
        decimal sourcePeriodTaxDelta = 0m,
        decimal sourcePeriodAdjustedTaxableExcluded = 0m,
        decimal targetPeriodTaxableDeductionFromRetro = 0m)
    {
        if (slices == null || slices.Count == 0)
            throw new ArgumentException("Danh sách đoạn hiệu lực hồ sơ lương không được rỗng.", nameof(slices));

        var evaluatedSlices = new List<MidMonthPayrollSliceBreakdown>(slices.Count);
        var specLookup = new Dictionary<string, FormulaComponentSpec>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < slices.Count; i++)
        {
            var slice = slices[i];
            if (solvedBaseSalaryOverride.HasValue && i == slices.Count - 1)
            {
                var cloned = new EmployeeSalaryProfile
                {
                    Id = slice.Profile.Id,
                    EmployeeId = slice.Profile.EmployeeId,
                    EffectiveFrom = slice.Profile.EffectiveFrom,
                    EffectiveTo = slice.Profile.EffectiveTo,
                    BaseSalary = solvedBaseSalaryOverride.Value,
                    InsuranceSalary = slice.Profile.InsuranceSalary,
                    SalaryScaleId = slice.Profile.SalaryScaleId,
                    SalaryStepId = slice.Profile.SalaryStepId,
                    SalaryTemplateId = slice.Profile.SalaryTemplateId,
                    PayMode = slice.Profile.PayMode,
                    UnitRate = slice.Profile.UnitRate,
                    IsProbation = slice.Profile.IsProbation,
                    ProbationRatePercent = slice.Profile.ProbationRatePercent,
                    PositionAllowance = slice.Profile.PositionAllowance,
                    OtherAllowance = slice.Profile.OtherAllowance,
                    IsUnionMember = slice.Profile.IsUnionMember,
                    TaxResidencyMode = slice.Profile.TaxResidencyMode,
                    HasPitCommitment08 = slice.Profile.HasPitCommitment08,
                    Commitment08Number = slice.Profile.Commitment08Number,
                    IsNetContract = slice.Profile.IsNetContract,
                    TargetNetSalary = slice.Profile.TargetNetSalary,
                    FixedAllowances = slice.Profile.FixedAllowances
                };
                slice = slice with { Profile = cloned };
            }

            if (slice.TemplateComponents != null)
            {
                foreach (var spec in slice.TemplateComponents)
                    specLookup[spec.ComponentCode] = spec;
            }

            evaluatedSlices.Add(MidMonthSalarySplitter.EvaluateSlice(
                slice,
                standardWorkdays,
                roundingRule,
                taxExemptParamValues,
                extraCommissionOrKpi: i == slices.Count - 1 ? commissionBonus + kpiBonus : 0m,
                kpiMultiplier: kpiMultiplier));
        }

        decimal sumActualBase = evaluatedSlices.Sum(s => s.ActualBaseSalary);
        decimal sumPosAllowance = evaluatedSlices.Sum(s => s.PositionAllowance);
        decimal sumOtherAllowance = evaluatedSlices.Sum(s => s.OtherAllowance);
        decimal sumOtPay = evaluatedSlices.Sum(s => s.OvertimePay);
        decimal sumExtraEarnings = evaluatedSlices.Sum(s => s.ExtraTemplateEarnings);
        decimal sumCustomDeductions = evaluatedSlices.Sum(s => s.CustomDeductions);

        // Compute OT tax-exempt excess per slice accurately based on slice's hourly rate
        decimal sumDayOtTaxExempt = 0m;
        if (statutoryContext.Parameter.IsOtExcessTaxExempt)
        {
            for (int i = 0; i < evaluatedSlices.Count; i++)
            {
                var sl = slices[i];
                var ev = evaluatedSlices[i];
                decimal segHourly = standardWorkdays > 0m ? ev.BaseSalary / (standardWorkdays * 8m) : 0m;
                decimal segNormalBase = Math.Round(
                    segHourly * (sl.SegmentOt150 + sl.SegmentOt200 + sl.SegmentOt300) * statutoryContext.Parameter.OtTaxableBaseRate,
                    0,
                    MidpointRounding.AwayFromZero);
                sumDayOtTaxExempt += Math.Max(0m, ev.OvertimePay - segNormalBase);
            }
        }

        var groupedLines = evaluatedSlices
            .SelectMany(s => s.Lines)
            .GroupBy(l => l.ComponentCode, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                decimal rawSum = g.Sum(x => x.RawValue);
                decimal finalSum = g.Sum(x => x.FinalAmount);
                bool isInfo = string.Equals(first.ComponentType, "Info", StringComparison.OrdinalIgnoreCase);
                bool isDeduction = string.Equals(first.ComponentType, "Deduction", StringComparison.OrdinalIgnoreCase);

                decimal exemptCap = 0m;
                if (specLookup.TryGetValue(first.ComponentCode, out var spec))
                {
                    exemptCap = spec.TaxExemptLimit;
                    if (!string.IsNullOrWhiteSpace(spec.TaxExemptLimitParameterCode) &&
                        taxExemptParamValues != null &&
                        taxExemptParamValues.TryGetValue(spec.TaxExemptLimitParameterCode, out var pVal))
                    {
                        exemptCap = pVal;
                    }
                }

                decimal taxableAmount = 0m;
                if (!isInfo && !isDeduction)
                {
                    if (!first.IsTaxable)
                    {
                        taxableAmount = 0m;
                    }
                    else if (exemptCap > 0m)
                    {
                        var exemptAmt = Math.Min(Math.Max(0m, finalSum), exemptCap);
                        taxableAmount = Math.Max(0m, finalSum - exemptAmt);
                    }
                    else
                    {
                        taxableAmount = finalSum;
                    }
                }

                var mergedNote = g.Count() > 1
                    ? $"Tách {g.Count()} đoạn hiệu lực trong kỳ"
                    : first.Note;

                return new EvaluatedSalaryLineDto(
                    first.SalaryComponentId,
                    first.ComponentCode,
                    first.ComponentName,
                    first.ComponentType,
                    rawSum,
                    finalSum,
                    taxableAmount,
                    first.IsTaxable,
                    first.IsSocialInsuranceBase,
                    first.ShowOnPayslip,
                    mergedNote);
            })
            .ToList();

        decimal totalTaxExempt = groupedLines
            .Where(l => !string.Equals(l.ComponentType, "Info", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(l.ComponentType, "Deduction", StringComparison.OrdinalIgnoreCase))
            .Sum(l => Math.Max(0m, l.FinalAmount - l.TaxableAmount));

        bool hasNightAllowanceLine = groupedLines.Any(l =>
            string.Equals(l.ComponentCode, "NIGHT_ALLOWANCE", StringComparison.OrdinalIgnoreCase) && l.FinalAmount > 0m);

        var latestSlice = evaluatedSlices[^1];
        var latestProfile = slices[^1].Profile;
        if (solvedBaseSalaryOverride.HasValue)
            latestProfile = slices[^1].Profile;

        decimal insContractSalary = ResolveInsuranceOverride(latestProfile)
            ?? (latestSlice.BaseSalary + latestProfile.PositionAllowance);

        decimal? insOverride = insContractSalary;

        decimal nightOtHourly = standardWorkdays > 0m ? latestSlice.BaseSalary / (standardWorkdays * 8m) : 0m;
        decimal mult150 = statutoryContext.Parameter.OtMultiplier150;
        decimal mult200 = statutoryContext.Parameter.OtMultiplier200;
        decimal mult300 = statutoryContext.Parameter.OtMultiplier300;
        decimal nightMult150 = mult150 + statutoryContext.Parameter.OtNightShiftBonusRate + (statutoryContext.Parameter.OtNightOvertimeExtraRate * mult150);
        decimal nightMult200 = mult200 + statutoryContext.Parameter.OtNightShiftBonusRate + (statutoryContext.Parameter.OtNightOvertimeExtraRate * mult200);
        decimal nightMult300 = mult300 + statutoryContext.Parameter.OtNightShiftBonusRate + (statutoryContext.Parameter.OtNightOvertimeExtraRate * mult300);

        decimal nightOtPay = Math.Round(
            nightOtHourly * (totalOt150Night * nightMult150 + totalOt200Night * nightMult200 + totalOt300Night * nightMult300),
            0,
            MidpointRounding.AwayFromZero);
        decimal nightOtAtDayRate = Math.Round(
            nightOtHourly * (totalOt150Night * mult150 + totalOt200Night * mult200 + totalOt300Night * mult300),
            0,
            MidpointRounding.AwayFromZero);
        decimal nightOtNormalBase = Math.Round(
            nightOtHourly * (totalOt150Night + totalOt200Night + totalOt300Night) * statutoryContext.Parameter.OtTaxableBaseRate,
            0,
            MidpointRounding.AwayFromZero);
        decimal nightOtTaxExempt = statutoryContext.Parameter.IsOtExcessTaxExempt
            ? Math.Max(0m, nightOtPay - nightOtNormalBase)
            : 0m;

        decimal effectiveBaseForCalc = solvedBaseSalaryOverride ?? latestSlice.BaseSalary;

        var breakdown = Calculate(new PayrollCalculationInput(
            ContractBaseSalary: effectiveBaseForCalc,
            PositionAllowance: sumPosAllowance,
            OtherAllowance: sumOtherAllowance,
            StandardWorkdays: standardWorkdays,
            PayableWorkdays: totalPayableWorkdays,
            OtHours150: totalOt150Day,
            OtHours200: totalOt200Day,
            OtHours300: totalOt300Day,
            KpiBonus: kpiBonus,
            DisciplineDeduction: disciplineDeduction,
            DependentCount: dependentCount,
            IsUnionMember: latestProfile.IsUnionMember,
            WageRegion: wageRegion,
            UnusedLeavePayout: unusedLeavePayout,
            InsuranceContractSalaryOverride: insOverride,
            PrecomputedActualBaseSalary: sumActualBase,
            PrecomputedOvertimePay: sumOtPay,
            ExtraTemplateEarnings: sumExtraEarnings,
            ExtraTaxExemptAmount: totalTaxExempt,
            CustomDeductions: sumCustomDeductions,
            OtHours150Night: totalOt150Night,
            OtHours200Night: totalOt200Night,
            OtHours300Night: totalOt300Night,
            RegularNightHours: regularNightHours,
            CommissionBonus: commissionBonus,
            ThirteenthMonthBonus: thirteenthMonthBonus,
            RetroEarningAmount: retroEarningAmount,
            RetroDeductionAmount: retroDeductionAmount,
            SalaryAdvanceDeduction: salaryAdvanceDeduction,
            LoanInstallmentDeduction: loanInstallmentDeduction,
            TaxResidencyMode: latestProfile.TaxResidencyMode,
            HasPitCommitment08: latestProfile.HasPitCommitment08,
            PrecomputedOtNightExtraPay: Math.Max(0m, nightOtPay - nightOtAtDayRate),
            PrecomputedOtTaxExemptAmount: sumDayOtTaxExempt + nightOtTaxExempt,
            IsBonusPeriodOnly: isBonusPeriodOnly,
            PriorMonthAssessableIncome: priorMonthAssessableIncome,
            SkipRegularNightAllowance: hasNightAllowanceLine,
            HasPriorMonthlyDisbursement: hasPriorMonthlyDisbursement,
            PriorMonthTaxableIncome: priorMonthTaxableIncome,
            PriorMonthPitWithheld: priorMonthPitWithheld,
            SourcePeriodTaxDelta: sourcePeriodTaxDelta,
            SourcePeriodAdjustedTaxableExcluded: sourcePeriodAdjustedTaxableExcluded,
            TargetPeriodTaxableDeductionFromRetro: targetPeriodTaxableDeductionFromRetro), statutoryContext);

        string? splitNote = evaluatedSlices.Count > 1
            ? "Tách lương theo hiệu lực: " + string.Join("; ", evaluatedSlices.Select(s =>
                $"{s.SegmentStart:dd/MM}-{s.SegmentEnd:dd/MM}: {s.SegmentWorkdays:N2} công, LCB={s.BaseSalary:N0}, Thực={s.ActualBaseSalary:N0}"))
            : null;

        return new MidMonthAggregatedPayrollResult(
            breakdown,
            effectiveBaseForCalc,
            sumPosAllowance,
            sumOtherAllowance,
            sumExtraEarnings,
            totalTaxExempt,
            sumCustomDeductions,
            insContractSalary,
            evaluatedSlices,
            groupedLines,
            splitNote,
            breakdown.OtTaxExemptAmount,
            breakdown.OtNightExtraPay);
    }

    public static IReadOnlyList<CalculationTraceStepDto> BuildCalculationTrace(
        PayrollCalculationInput input,
        PayrollCalculationBreakdown breakdown,
        PayrollStatutoryParameter parameter,
        IReadOnlyList<PitBracketTaxSliceDto> pitSlices,
        NetToGrossSolution? netToGross = null,
        string? segmentNote = null)
    {
        var steps = new List<CalculationTraceStepDto>(15);

        decimal allowanceTotal = input.IsBonusPeriodOnly
            ? 0m
            : input.PositionAllowance + input.OtherAllowance + input.ExtraTemplateEarnings;
        decimal missingWorkdays = !input.IsBonusPeriodOnly && input.StandardWorkdays > input.PayableWorkdays
            ? input.StandardWorkdays - input.PayableWorkdays
            : 0m;
        decimal missingWorkDeduction = missingWorkdays > 0m && input.StandardWorkdays > 0m
            ? Math.Round(input.ContractBaseSalary * (missingWorkdays / input.StandardWorkdays), 0, MidpointRounding.AwayFromZero)
            : 0m;
        decimal bonusTotal = input.KpiBonus + input.CommissionBonus + input.ThirteenthMonthBonus
            + input.UnusedLeavePayout + input.RetroEarningAmount;
        decimal siCap = parameter.StatutoryBaseSalary * parameter.SocialInsuranceCapMultiplier;
        decimal regionalMinWage = parameter.GetRegionalMinWage(input.WageRegion);
        decimal uiCap = regionalMinWage * parameter.UnemploymentInsuranceCapMultiplier;
        decimal exemptTotal = breakdown.OtTaxExemptAmount + input.ExtraTaxExemptAmount;
        decimal familyDeduction = breakdown.SelfDeductionAmount + breakdown.DependentDeductionAmount;
        decimal otherDed = input.DisciplineDeduction + input.SalaryAdvanceDeduction + input.LoanInstallmentDeduction
            + input.RetroDeductionAmount + input.CustomDeductions + breakdown.EmployeeUnionFee;

        // 1. Lương cơ bản hợp đồng (kèm quy đổi Net -> Gross nếu có)
        steps.Add(new CalculationTraceStepDto(
            1,
            "Contract",
            "Lương cơ bản hợp đồng",
            netToGross != null
                ? $"Quy đổi Net → Gross (Binary Search): Tìm LCB sao cho Net(LCB) = TargetNet ({netToGross.TargetNetSalary:N0}đ)"
                : "Lương cơ bản thỏa thuận theo hợp đồng lao động",
            (netToGross != null
                ? $"TargetNet={netToGross.TargetNetSalary:N0}đ → SolvedLCB={netToGross.SolvedContractBaseSalary:N0}đ (Net đạt {netToGross.AchievedNetSalary:N0}đ sau {netToGross.IterationsUsed} vòng) | "
                : string.Empty)
            + $"LCB={input.ContractBaseSalary:N0}đ, Công chuẩn={input.StandardWorkdays:0.##}, Công hưởng lương={input.PayableWorkdays:0.##}"
            + (string.IsNullOrWhiteSpace(segmentNote) ? string.Empty : $" [{segmentNote}]"),
            input.ContractBaseSalary,
            "Điều 90 BLLĐ 2019",
            netToGross?.TargetNetSalary));

        // 2. Lương theo công chuẩn
        steps.Add(new CalculationTraceStepDto(
            2,
            "Income",
            "Lương theo ngày công chuẩn",
            "LCB × (Công hưởng lương / Công chuẩn)",
            input.IsBonusPeriodOnly
                ? $"Kỳ thưởng/chi trả bổ sung (không tính lương công) = {breakdown.ActualBaseSalary:N0}đ"
                : $"{input.ContractBaseSalary:N0} × ({input.PayableWorkdays:0.##} / {input.StandardWorkdays:0.##}) = {breakdown.ActualBaseSalary:N0}đ"
                  + (string.IsNullOrWhiteSpace(segmentNote) ? string.Empty : $" [{segmentNote}]"),
            breakdown.ActualBaseSalary,
            "Điều 90 BLLĐ 2019"));

        // 3. Phụ cấp chức vụ & phụ cấp khác
        steps.Add(new CalculationTraceStepDto(
            3,
            "Allowance",
            "Phụ cấp chức vụ & phụ cấp khác",
            "Phụ cấp chức vụ + Phụ cấp khác + Thành phần công thức khác",
            input.IsBonusPeriodOnly
                ? "Kỳ thưởng/bổ sung: Phụ cấp = 0đ"
                : $"{input.PositionAllowance:N0} + {input.OtherAllowance:N0} + {input.ExtraTemplateEarnings:N0} = {allowanceTotal:N0}đ",
            allowanceTotal,
            null));

        // 4. Khấu trừ công thiếu / nghỉ không lương
        steps.Add(new CalculationTraceStepDto(
            4,
            "DeductionInfo",
            "Khấu trừ công thiếu / nghỉ không lương",
            "max(0, Công chuẩn - Công hưởng lương) × (LCB / Công chuẩn) [Đã khấu trừ vào Bước 2]",
            missingWorkdays > 0m
                ? $"Thiếu {missingWorkdays:0.##} công × ({input.ContractBaseSalary:N0} / {input.StandardWorkdays:0.##}) = {missingWorkDeduction:N0}đ (đã trừ ở Bước 2)"
                : "Đủ hoặc vượt công chuẩn (0 ngày thiếu) = 0đ",
            missingWorkDeduction,
            "Điều 115 BLLĐ 2019"));

        // 5. OT (150%/200%/300%) & Phụ cấp ca đêm
        decimal ot150All = input.OtHours150 + input.OtHours150Night;
        decimal ot200All = input.OtHours200 + input.OtHours200Night;
        decimal ot300All = input.OtHours300 + input.OtHours300Night;
        steps.Add(new CalculationTraceStepDto(
            5,
            "Overtime",
            "Làm thêm giờ (150% / 200% / 300%) & Phụ cấp ca đêm",
            "Đơn giá giờ × (OT Ngày×1.5/2.0/3.0 + OT Đêm×2.0/2.7/3.9 + Giờ đêm×0.3)",
            input.IsBonusPeriodOnly
                ? "Kỳ thưởng/bổ sung: OT = 0đ"
                : $"OT150={ot150All:0.##}h (đêm {input.OtHours150Night:0.##}h), OT200={ot200All:0.##}h (đêm {input.OtHours200Night:0.##}h), OT300={ot300All:0.##}h (đêm {input.OtHours300Night:0.##}h), CaĐêm={input.RegularNightHours:0.##}h → {breakdown.OvertimePay:N0}đ (thưởng đêm {breakdown.OtNightExtraPay:N0}đ)",
            breakdown.OvertimePay,
            "Điều 98 BLLĐ 2019",
            breakdown.OtNightExtraPay));

        // 6. Thưởng KPI / Hoa hồng / Tháng 13 / Phép tồn / Truy lĩnh
        steps.Add(new CalculationTraceStepDto(
            6,
            "Bonus",
            "Thưởng KPI, Hoa hồng, Tháng 13, Phép tồn & Truy lĩnh",
            "KPI + Hoa hồng + Tháng 13 + Phép tồn + Truy lĩnh",
            $"{input.KpiBonus:N0} + {input.CommissionBonus:N0} + {input.ThirteenthMonthBonus:N0} + {input.UnusedLeavePayout:N0} + {input.RetroEarningAmount:N0} = {bonusTotal:N0}đ",
            bonusTotal,
            "Điều 104 BLLĐ 2019"));

        // 7. Tổng thu nhập (Gross)
        steps.Add(new CalculationTraceStepDto(
            7,
            "Income",
            "Tổng thu nhập (Gross)",
            "Lương công + Phụ cấp + OT/Đêm + Thưởng/Phép tồn/Truy lĩnh",
            $"{breakdown.ActualBaseSalary:N0} + {allowanceTotal:N0} + {breakdown.OvertimePay:N0} + {bonusTotal:N0} = {breakdown.GrossIncome:N0}đ",
            breakdown.GrossIncome,
            null));

        // 8. Nền đóng bảo hiểm (trần 20xLCS & 20xLTTV vùng)
        decimal insContractSalary = input.IsBonusPeriodOnly
            ? 0m
            : Math.Max(0m, input.InsuranceContractSalaryOverride ?? (input.ContractBaseSalary + input.PositionAllowance));
        steps.Add(new CalculationTraceStepDto(
            8,
            "InsuranceBase",
            "Nền đóng bảo hiểm (Trần 20×LCS & 20×LTTV vùng)",
            "Nền BHXH/BHYT = min(LươngBH, 20×LCS); Nền BHTN = min(LươngBH, 20×LTTV vùng)",
            input.IsBonusPeriodOnly
                ? "Kỳ thưởng/bổ sung: Nền đóng BH = 0đ"
                : $"LươngBH={insContractSalary:N0}đ | Trần BHXH (20×{parameter.StatutoryBaseSalary:N0})={siCap:N0}đ → BHXH/BHYT={breakdown.SocialInsuranceBase:N0}đ | Trần BHTN Vùng {input.WageRegion} (20×{regionalMinWage:N0})={uiCap:N0}đ → BHTN={breakdown.UnemploymentInsuranceBase:N0}đ",
            breakdown.SocialInsuranceBase,
            parameter.LegalReference,
            breakdown.UnemploymentInsuranceBase));

        // 9. Bảo hiểm NLĐ (BHXH 8% + BHYT 1.5% + BHTN 1%)
        steps.Add(new CalculationTraceStepDto(
            9,
            "Insurance",
            "Bảo hiểm bắt buộc NLĐ (BHXH 8% + BHYT 1.5% + BHTN 1%)",
            $"Nền BHXH × ({parameter.EmployeeSiRate:P1} + {parameter.EmployeeHiRate:P1}) + Nền BHTN × {parameter.EmployeeUiRate:P1}",
            $"BHXH({parameter.EmployeeSiRate:P1})={breakdown.EmployeeSocialInsurance:N0} + BHYT({parameter.EmployeeHiRate:P1})={breakdown.EmployeeHealthInsurance:N0} + BHTN({parameter.EmployeeUiRate:P1})={breakdown.EmployeeUnemploymentInsurance:N0} = {breakdown.TotalEmployeeInsurance:N0}đ",
            breakdown.TotalEmployeeInsurance,
            parameter.LegalReference));

        // 10. Khoản thu nhập miễn thuế (OT >100% & phụ cấp miễn thuế)
        steps.Add(new CalculationTraceStepDto(
            10,
            "TaxExempt",
            "Khoản thu nhập miễn thuế (OT >100% & Phụ cấp miễn thuế)",
            "Phần trả thêm OT/Làm đêm vượt 100% + Phụ cấp miễn thuế (ăn ca, độc hại...)",
            $"Miễn OT/Đêm={breakdown.OtTaxExemptAmount:N0} + Phụ cấp miễn thuế={input.ExtraTaxExemptAmount:N0} = {exemptTotal:N0}đ",
            exemptTotal,
            "Điều 3 Thông tư 111/2013/TT-BTC"));

        // 11. Thu nhập chịu thuế
        steps.Add(new CalculationTraceStepDto(
            11,
            "TaxableIncome",
            "Thu nhập chịu thuế TNCN",
            "max(0, Tổng thu nhập Gross - Khoản miễn thuế)",
            $"max(0, {breakdown.GrossIncome:N0} - {exemptTotal:N0}) = {breakdown.TaxableIncome:N0}đ",
            breakdown.TaxableIncome,
            "Thông tư 111/2013/TT-BTC"));

        // 12. Giảm trừ gia cảnh (bản thân + N người phụ thuộc)
        steps.Add(new CalculationTraceStepDto(
            12,
            "Deduction",
            "Giảm trừ gia cảnh (Bản thân + Người phụ thuộc)",
            "Giảm trừ bản thân + (Số người phụ thuộc × Mức giảm trừ mỗi NPT)",
            string.Equals(breakdown.TaxResidencyMode, "ResidentStandard", StringComparison.OrdinalIgnoreCase)
                ? $"Bản thân={breakdown.SelfDeductionAmount:N0} + ({input.DependentCount} × {parameter.DependentDeduction:N0} = {breakdown.DependentDeductionAmount:N0}) = {familyDeduction:N0}đ"
                : $"Không áp dụng giảm trừ gia cảnh ({breakdown.TaxResidencyMode}) = 0đ",
            familyDeduction,
            parameter.LegalReference));

        // 13. Thu nhập tính thuế & Thuế TNCN từng bậc
        var taxFormula = breakdown.TaxResidencyMode switch
        {
            "NonResident" => $"Lao động không cư trú: Thuế suất phẳng {parameter.PitNonResidentRate:P0} × Thu nhập chịu thuế",
            "ResidentShortTerm" => breakdown.UsedCommitment08
                ? "HĐ <3 tháng có cam kết mẫu 08/CK-TNCN: tạm khấu trừ 0%"
                : $"HĐ <3 tháng không cam kết 08: {parameter.PitShortTermRate:P0} khi TN chịu thuế >= {parameter.PitShortTermThreshold:N0}",
            _ => "Thu nhập tính thuế = max(0, TN chịu thuế - BH NLĐ - Giảm trừ); Thuế TNCN lũy tiến từng bậc"
        };

        var activePitText = string.Equals(breakdown.TaxResidencyMode, "ResidentStandard", StringComparison.OrdinalIgnoreCase)
            ? string.Join("; ", pitSlices.Where(s => s.TaxAmount > 0m).Select(s => $"Bậc {s.BracketLevel}({s.TaxRate:P0}): {s.TaxAmount:N0}đ"))
            : taxFormula;

        steps.Add(new CalculationTraceStepDto(
            13,
            "Tax",
            "Thu nhập tính thuế & Thuế TNCN từng bậc",
            taxFormula,
            $"TNTT = max(0, {breakdown.TaxableIncome:N0} - {breakdown.TotalEmployeeInsurance:N0} - {familyDeduction:N0}) = {breakdown.AssessableIncome:N0}đ"
            + (input.HasPriorMonthlyDisbursement ? $" | Gộp đợt trước: TNCT={input.PriorMonthTaxableIncome:N0}, Thuế={input.PriorMonthPitWithheld:N0}" : string.Empty)
            + (input.SourcePeriodTaxDelta != 0m ? $" | Điều chỉnh thuế kỳ gốc={input.SourcePeriodTaxDelta:+N0;-N0;0}" : string.Empty)
            + (string.IsNullOrWhiteSpace(activePitText) ? " | Không phát sinh thuế" : $" | {activePitText}")
            + $" → Tổng thuế={breakdown.PersonalIncomeTax:N0}đ",
            breakdown.PersonalIncomeTax,
            parameter.LegalReference,
            breakdown.AssessableIncome));

        // 14. Khấu trừ khác (tạm ứng, vay, kỷ luật, truy thu, ĐP CĐ)
        steps.Add(new CalculationTraceStepDto(
            14,
            "Deduction",
            "Khấu trừ khác (Tạm ứng, Vay, Kỷ luật, Truy thu, ĐP CĐ)",
            "Tạm ứng + Trả góp vay + Kỷ luật + Truy thu + Khấu trừ khác + Đoàn phí NLĐ",
            $"Tạm ứng={input.SalaryAdvanceDeduction:N0} + Vay={input.LoanInstallmentDeduction:N0} + Kỷ luật={input.DisciplineDeduction:N0} + Truy thu={input.RetroDeductionAmount:N0} + Khác={input.CustomDeductions:N0} + ĐP CĐ={breakdown.EmployeeUnionFee:N0} = {otherDed:N0}đ",
            otherDed,
            null));

        // 15. Thực lĩnh (Net) & Tổng chi phí DN
        decimal companyInsurance = breakdown.CompanySocialInsurance + breakdown.CompanyHealthInsurance + breakdown.CompanyUnemploymentInsurance;
        steps.Add(new CalculationTraceStepDto(
            15,
            "CompanyCost",
            "Thực lĩnh (Net) & Tổng chi phí doanh nghiệp",
            $"Net = Gross - BH NLĐ - Thuế TNCN - Khấu trừ khác | Chi phí DN = Gross + BHXH/BHYT/BHTN DN + BHTNLĐ ({parameter.CompanyOccupationalAccidentRate:P1}) + KPCĐ ({parameter.CompanyUnionFeeRate:P1})",
            $"Net = {breakdown.GrossIncome:N0} - {breakdown.TotalEmployeeInsurance:N0} - {breakdown.PersonalIncomeTax:N0} - {otherDed:N0} = {breakdown.NetSalary:N0}đ | Chi phí DN = {breakdown.GrossIncome:N0} + {companyInsurance:N0} + {breakdown.CompanyOccupationalAccidentInsurance:N0} + {breakdown.CompanyUnionFee:N0} = {breakdown.TotalCompanyCost:N0}đ",
            breakdown.TotalCompanyCost,
            parameter.LegalReference,
            breakdown.NetSalary));

        return steps;
    }

    public static List<PayrollRecordDetail> BuildStandardRecordDetails(
        PayrollCalculationBreakdown breakdown,
        decimal positionAllowance,
        decimal otherAllowance,
        decimal kpiAndBonusTotal,
        IReadOnlyDictionary<string, int> componentIdsByCode)
    {
        var details = new List<PayrollRecordDetail>();

        void AddIfExists(string code, string fallbackName, decimal amount, string? note = null)
        {
            if (amount == 0m) return;
            if (!componentIdsByCode.TryGetValue(code, out int compId)) return;
            details.Add(new PayrollRecordDetail
            {
                SalaryComponentId = compId,
                ComponentCode = code,
                ComponentName = fallbackName,
                Amount = amount,
                CalculationNote = note
            });
        }

        AddIfExists("BASIC_PAY", "Lương cơ bản theo ngày công (P1)", breakdown.ActualBaseSalary, "Tính theo ngày công hưởng lương");
        AddIfExists("POS_ALLOWANCE", "Phụ cấp chức vụ", positionAllowance);
        AddIfExists("OTHER_ALLOWANCE", "Phụ cấp ăn ca, xăng xe, điện thoại (P2)", otherAllowance);
        AddIfExists("KPI_BONUS", "Thưởng hiệu quả KPI (P3) & thưởng/OT", kpiAndBonusTotal + breakdown.OvertimePay, breakdown.OvertimePay > 0m ? $"Gồm OT {breakdown.OvertimePay:N0}đ" : null);
        AddIfExists("SI_DEDUCTION", "Khấu trừ BHXH, BHYT, BHTN (10.5%)", -breakdown.TotalEmployeeInsurance, $"BHXH {breakdown.EmployeeSocialInsurance:N0} + BHYT {breakdown.EmployeeHealthInsurance:N0} + BHTN {breakdown.EmployeeUnemploymentInsurance:N0}");
        AddIfExists("PIT_TAX", "Thuế Thu nhập cá nhân (TNCN)", -breakdown.PersonalIncomeTax, $"TN tính thuế {breakdown.AssessableIncome:N0}đ");

        return details;
    }

    /// <summary>
    /// Tính thuế TNCN lũy tiến từng phần theo danh sách bậc thuế có hiệu lực (5 bậc, 7 bậc hoặc tùy biến).
    /// </summary>
    public static decimal CalculateProgressivePit(decimal assessableIncome, IReadOnlyList<PitBracket> pitBrackets)
    {
        var slices = CalculateProgressivePitBreakdown(assessableIncome, pitBrackets);
        return Math.Round(slices.Sum(s => s.TaxAmount), 0, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<PitBracketTaxSliceDto> CalculateProgressivePitBreakdown(
        decimal assessableIncome,
        IReadOnlyList<PitBracket> pitBrackets)
    {
        if (pitBrackets is null || pitBrackets.Count == 0)
            throw new ArgumentException("Biểu thuế TNCN hiệu lực không được rỗng.", nameof(pitBrackets));

        var ordered = pitBrackets.OrderBy(b => b.BracketLevel).ThenBy(b => b.LowerBound).ToList();
        var result = new List<PitBracketTaxSliceDto>(ordered.Count);

        foreach (var bracket in ordered)
        {
            decimal incomeInBracket = 0m;
            if (assessableIncome > bracket.LowerBound)
            {
                var upper = bracket.UpperBound.HasValue
                    ? Math.Min(assessableIncome, bracket.UpperBound.Value)
                    : assessableIncome;
                if (upper > bracket.LowerBound)
                    incomeInBracket = upper - bracket.LowerBound;
            }

            var taxAmount = incomeInBracket * bracket.TaxRate;
            result.Add(new PitBracketTaxSliceDto(
                bracket.BracketLevel,
                bracket.LowerBound,
                bracket.UpperBound,
                bracket.TaxRate,
                Math.Round(incomeInBracket, 0, MidpointRounding.AwayFromZero),
                Math.Round(taxAmount, 0, MidpointRounding.AwayFromZero)));
        }

        return result;
    }
}

public sealed record AttendanceLockStatusDto(
    int Year,
    int Month,
    int TotalTargetEmployees,
    int SummariesFound,
    int LockedCount,
    int UnlockedOrMissingCount,
    bool IsAllLocked,
    string WarningMessage,
    IReadOnlyList<string> UnlockedEmployeeCodes);

public sealed record EvaluatedEmployeePayroll(
    Employee Employee,
    MonthlyAttendanceSummary? Attendance,
    PayrollRecord Record,
    IReadOnlyList<CalculationTraceStepDto> TraceSteps,
    IReadOnlyList<PitBracketTaxSliceDto> PitSlices,
    IReadOnlyList<int> ConsumedAdvanceIds,
    IReadOnlyList<int> ConsumedLoanInstallmentIds,
    IReadOnlyList<int> ConsumedAdjustmentIds,
    IReadOnlyList<int> ConsumedRewardDisciplineIds,
    decimal? PreviousRevisionNet,
    decimal? PreviousPeriodNet,
    decimal? VariancePercent,
    bool HasHighVarianceWarning,
    IReadOnlyList<int>? ConsumedDisbursedAdvanceIds = null);

public sealed record EvaluatedPeriodBundle(
    PayrollPeriod Period,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    PayrollStatutoryContext StatutoryContext,
    AttendanceLockStatusDto AttendanceLockStatus,
    decimal PreviousTotalPeriodNet,
    bool HasPreviousRevision,
    IReadOnlyList<PayrollRecord> ExistingCurrentRecords,
    IReadOnlyList<EvaluatedEmployeePayroll> EvaluatedEmployees);

/// <summary>
/// Bộ máy đánh giá dùng chung cho cả Chạy tính lương chính thức (Calculate) và Giả lập chạy lương (Simulate)
/// nhằm đảm bảo 100% logic thống nhất, không trùng lặp code.
/// </summary>
public static class PayrollPeriodEvaluator
{
    public static async Task<AttendanceLockStatusDto> CheckAttendanceLockAsync(
        IApplicationDbContext context,
        int year,
        int month,
        IReadOnlyList<int> targetEmployeeIds,
        IReadOnlyDictionary<int, string> empCodeMap,
        CancellationToken cancellationToken)
    {
        var summaries = await context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .Where(a => a.Year == year && a.Month == month && targetEmployeeIds.Contains(a.EmployeeId))
            .ToListAsync(cancellationToken);

        var summaryByEmp = summaries.ToDictionary(s => s.EmployeeId);
        var unlockedCodes = new List<string>();
        int lockedCount = 0;

        foreach (var empId in targetEmployeeIds)
        {
            if (summaryByEmp.TryGetValue(empId, out var att) && att.IsLocked)
            {
                lockedCount++;
            }
            else
            {
                unlockedCodes.Add(empCodeMap.TryGetValue(empId, out var code) ? code : $"#{empId}");
            }
        }

        int missingOrUnlocked = targetEmployeeIds.Count - lockedCount;
        bool isAllLocked = targetEmployeeIds.Count > 0 && missingOrUnlocked == 0;
        string warning = isAllLocked
            ? $"Bảng công tháng {month:D2}/{year} đã chốt khóa đầy đủ ({lockedCount}/{targetEmployeeIds.Count} NV)."
            : $"Cảnh báo: còn {missingOrUnlocked}/{targetEmployeeIds.Count} nhân viên chưa có hoặc chưa khóa bảng công tháng {month:D2}/{year} ({string.Join(", ", unlockedCodes.Take(5))}{(unlockedCodes.Count > 5 ? "..." : "")}).";

        return new AttendanceLockStatusDto(
            year,
            month,
            targetEmployeeIds.Count,
            summaries.Count,
            lockedCount,
            missingOrUnlocked,
            isAllLocked,
            warning,
            unlockedCodes);
    }

    public static async Task<EvaluatedPeriodBundle> EvaluatePeriodAsync(
        IApplicationDbContext context,
        PayrollPeriod period,
        int? singleEmployeeId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var periodStartDate = period.StartDate != default
            ? period.StartDate
            : new DateOnly(period.Year, period.Month, 1);
        var periodEndDate = period.EndDate != default
            ? period.EndDate
            : new DateOnly(period.Year, period.Month, DateTime.DaysInMonth(period.Year, period.Month));

        bool isAdvanceDisbursementPeriod = string.Equals(period.PeriodType, "MidMonthAdvance", StringComparison.OrdinalIgnoreCase);
        bool isBonusPeriodOnly = isAdvanceDisbursementPeriod || !string.Equals(period.PeriodType, "Monthly", StringComparison.OrdinalIgnoreCase);

        var statutoryContext = await VietnamPayrollEngine.ResolveStatutoryContextAsync(
            context, periodEndDate, cancellationToken);

        var activeParams = await context.PayrollParameters
            .AsNoTracking()
            .Where(p => p.EffectiveFrom <= periodEndDate && (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= periodStartDate))
            .OrderBy(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var taxExemptParamValues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var prm in activeParams)
            taxExemptParamValues[prm.ParamCode] = prm.ParamValue;

        var periodSettlements = await context.TerminationLeaveSettlements
            .AsNoTracking()
            .Where(s => s.TargetPayrollYear == period.Year
                && s.TargetPayrollMonth == period.Month
                && (s.Status == "PushedToPayroll" || s.Status == "Paid"))
            .ToListAsync(cancellationToken);

        var settlementByEmpId = periodSettlements
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PayoutAmount));
        var settlementEmpIds = settlementByEmpId.Keys.ToHashSet();

        var empQuery = context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Where(e => (e.HireDate == default || e.HireDate <= periodEndDate)
                && ((e.WorkStatus != "Terminated" && e.WorkStatus != "Resigned" && e.WorkStatus != "Retired")
                    || settlementEmpIds.Contains(e.Id)
                    || (e.TerminationDate.HasValue && e.TerminationDate.Value >= periodStartDate)));

        if (singleEmployeeId.HasValue)
            empQuery = empQuery.Where(e => e.Id == singleEmployeeId.Value);

        var employees = await empQuery.OrderBy(e => e.EmployeeCode).ToListAsync(cancellationToken);
        if (singleEmployeeId.HasValue && employees.Count == 0)
            throw new KeyNotFoundException($"Không tìm thấy nhân viên hợp lệ #{singleEmployeeId.Value} để tính lương.");

        var employeeIds = employees.Select(e => e.Id).ToHashSet();
        var empCodeMap = employees.ToDictionary(e => e.Id, e => e.EmployeeCode);

        var attendanceLock = await CheckAttendanceLockAsync(
            context, period.Year, period.Month, employees.Select(e => e.Id).ToList(), empCodeMap, cancellationToken);

        var attendanceMap = await context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .Where(a => a.Year == period.Year && a.Month == period.Month && employeeIds.Contains(a.EmployeeId))
            .ToDictionaryAsync(a => a.EmployeeId, cancellationToken);

        var activeProfiles = await context.EmployeeSalaryProfiles
            .Include(p => p.FixedAllowances)
            .Include(p => p.SalaryTemplate)
            .AsNoTracking()
            .Where(p => p.IsActive
                && employeeIds.Contains(p.EmployeeId)
                && p.EffectiveFrom <= periodEndDate
                && (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= periodStartDate))
            .OrderBy(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var profilesByEmpId = activeProfiles
            .GroupBy(p => p.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EmployeeSalaryProfile>)g.ToList());

        var templateIds = activeProfiles
            .Where(p => p.SalaryTemplateId.HasValue)
            .Select(p => p.SalaryTemplateId!.Value)
            .Distinct()
            .ToList();

        var templateComponentMap = new Dictionary<int, IReadOnlyList<FormulaComponentSpec>>();
        var templateRoundingMap = new Dictionary<int, string>();
        if (templateIds.Count > 0)
        {
            var templates = await context.SalaryTemplates
                .Include(t => t.Components)
                    .ThenInclude(tc => tc.SalaryComponent)
                .AsNoTracking()
                .Where(t => templateIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            foreach (var tpl in templates)
            {
                templateRoundingMap[tpl.Id] = string.IsNullOrWhiteSpace(tpl.RoundingRule) ? "RoundToDong" : tpl.RoundingRule;
                templateComponentMap[tpl.Id] = tpl.Components
                    .Where(tc => tc.SalaryComponent.IsActive)
                    .OrderBy(tc => tc.SortOrder)
                    .Select(tc => new FormulaComponentSpec(
                        tc.SalaryComponentId,
                        tc.SalaryComponent.ComponentCode,
                        tc.SalaryComponent.ComponentName,
                        tc.SalaryComponent.ComponentType,
                        tc.SalaryComponent.CalculationType,
                        tc.DefaultFixedAmount ?? tc.SalaryComponent.DefaultAmount,
                        tc.OverrideFormulaExpression ?? tc.SalaryComponent.FormulaExpression,
                        tc.SortOrder,
                        tc.SalaryComponent.IsTaxable,
                        tc.SalaryComponent.TaxExemptLimit,
                        tc.SalaryComponent.TaxExemptLimitParameterCode,
                        tc.SalaryComponent.IsSocialInsuranceBase,
                        tc.SalaryComponent.ShowOnPayslip))
                    .ToList();
            }
        }

        Dictionary<int, List<DailyAttendance>> dailyAttendanceByEmpId = new();
        if (activeProfiles.Count > 0 && !isBonusPeriodOnly)
        {
            var empIdsWithProfiles = profilesByEmpId.Keys.ToHashSet();
            var dailyRows = await context.DailyAttendances
                .AsNoTracking()
                .Where(d => empIdsWithProfiles.Contains(d.EmployeeId)
                    && d.AttendanceDate >= periodStartDate
                    && d.AttendanceDate <= periodEndDate)
                .OrderBy(d => d.AttendanceDate)
                .ToListAsync(cancellationToken);

            dailyAttendanceByEmpId = dailyRows
                .GroupBy(d => d.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // Người phụ thuộc hiệu lực trong kỳ (DeductionFromDate / DeductionToDate)
        var dependentCounts = await context.EmployeeDependents
            .AsNoTracking()
            .Where(d => d.IsTaxDeductible
                && employeeIds.Contains(d.EmployeeId)
                && (d.DeductionFromDate == null || d.DeductionFromDate <= periodEndDate)
                && (d.DeductionToDate == null || d.DeductionToDate >= periodStartDate))
            .GroupBy(d => d.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, cancellationToken);

        // Đánh giá KPI đã duyệt có chu kỳ kết thúc trong kỳ
        var kpiEvals = await context.EmployeeKpiEvaluations
            .AsNoTracking()
            .Include(k => k.EvaluationCycle)
            .Where(k => employeeIds.Contains(k.EmployeeId)
                && (k.Status == "Approved" || k.Status == "Closed")
                && k.EvaluationCycle.EndDate >= periodStartDate
                && k.EvaluationCycle.EndDate <= periodEndDate)
            .OrderByDescending(k => k.ApprovedAt)
            .ToListAsync(cancellationToken);

        var kpiEvalByEmpId = kpiEvals
            .GroupBy(k => k.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());

        // Khen thưởng / Kỷ luật hiệu lực trong kỳ (chỉ gán cho kỳ lương chính hoặc chưa khóa kỳ khác)
        var rewardDisciplines = await context.RewardDisciplines
            .AsNoTracking()
            .Where(r => employeeIds.Contains(r.EmployeeId)
                && r.Status == "Approved"
                && r.EffectiveDate >= periodStartDate
                && r.EffectiveDate <= periodEndDate
                && (r.PayrollPeriodId == null || r.PayrollPeriodId == period.Id))
            .ToListAsync(cancellationToken);
        var rdByEmpId = rewardDisciplines
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Tạm ứng lương (Approved hoặc đã khấu trừ vào chính kỳ này khi tính lại; với kỳ MidMonthAdvance lấy các đơn chưa chi hoặc đã chi qua chính kỳ này)
        var advances = await context.SalaryAdvances
            .AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                && a.TargetYear == period.Year
                && a.TargetMonth == period.Month
                && (a.Status == "Approved"
                    || (a.Status == "Deducted" && a.DeductedPayrollPeriodId == period.Id)
                    || (isAdvanceDisbursementPeriod && a.DisbursementPayrollPeriodId == period.Id)))
            .ToListAsync(cancellationToken);
        var advanceByEmpId = advances
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Trả góp khoản vay đến hạn trong tháng
        var installments = await context.EmployeeLoanInstallments
            .AsNoTracking()
            .Include(i => i.EmployeeLoan)
            .Where(i => employeeIds.Contains(i.EmployeeId)
                && i.TargetYear == period.Year
                && i.TargetMonth == period.Month
                && (i.EmployeeLoan.Status == "Active" || (i.Status == "Deducted" && i.DeductedPayrollPeriodId == period.Id))
                && (i.Status == "Scheduled" || (i.Status == "Deducted" && i.DeductedPayrollPeriodId == period.Id)))
            .ToListAsync(cancellationToken);
        var installmentByEmpId = installments
            .GroupBy(i => i.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Truy lĩnh / Truy thu / Hoa hồng / Thưởng từ PayrollAdjustments
        var adjustments = await context.PayrollAdjustments
            .AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                && a.TargetYear == period.Year
                && a.TargetMonth == period.Month
                && (a.Status == "Approved" || (a.Status == "Applied" && a.AppliedPayrollPeriodId == period.Id)))
            .ToListAsync(cancellationToken);
        var adjByEmpId = adjustments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Nhiều đợt chi trong cùng tháng: cộng dồn Thu nhập chịu thuế & Thuế đã khấu trừ của tất cả các đợt chi khác đã tính trong cùng (Year, Month)
        var priorMonthTaxableMap = new Dictionary<int, decimal>();
        var priorMonthPitMap = new Dictionary<int, decimal>();
        var priorMonthAssessableMap = new Dictionary<int, decimal>();
        var dedicatedBonusPeriodTypesInMonth = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!isAdvanceDisbursementPeriod)
        {
            var otherNonAdvancePeriods = await context.PayrollPeriods
                .AsNoTracking()
                .Where(p => p.Year == period.Year
                    && p.Month == period.Month
                    && p.Id != period.Id
                    && p.PeriodType != "MidMonthAdvance")
                .Select(p => new { p.Id, p.PeriodType })
                .ToListAsync(cancellationToken);

            foreach (var op in otherNonAdvancePeriods)
            {
                if (string.Equals(op.PeriodType, "Bonus13", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(op.PeriodType, "HolidayBonus", StringComparison.OrdinalIgnoreCase))
                {
                    dedicatedBonusPeriodTypesInMonth.Add(op.PeriodType);
                }
            }

            var otherNonAdvanceIds = otherNonAdvancePeriods.Select(p => p.Id).ToList();
            if (otherNonAdvanceIds.Count > 0)
            {
                var priorRecs = await context.PayrollRecords
                    .AsNoTracking()
                    .Where(r => otherNonAdvanceIds.Contains(r.PayrollPeriodId) && employeeIds.Contains(r.EmployeeId))
                    .Select(r => new { r.EmployeeId, r.TaxableIncome, r.PersonalIncomeTax, r.AssessableIncome })
                    .ToListAsync(cancellationToken);

                foreach (var g in priorRecs.GroupBy(r => r.EmployeeId))
                {
                    priorMonthTaxableMap[g.Key] = g.Sum(x => x.TaxableIncome);
                    priorMonthPitMap[g.Key] = g.Sum(x => x.PersonalIncomeTax);
                    priorMonthAssessableMap[g.Key] = g.Sum(x => x.AssessableIncome);
                }
            }
        }

        var currentRecordsQuery = context.PayrollRecords.Where(r => r.PayrollPeriodId == period.Id);
        if (asNoTracking)
            currentRecordsQuery = currentRecordsQuery.AsNoTracking();
        var allCurrentRecords = await currentRecordsQuery.ToListAsync(cancellationToken);

        var previousRevisionNetMap = allCurrentRecords.ToDictionary(r => r.EmployeeId, r => r.NetSalary);
        var previousTotalPeriodNet = allCurrentRecords.Sum(r => r.NetSalary);

        var prevPeriodId = await context.PayrollPeriods
            .AsNoTracking()
            .Where(p => p.PeriodType == period.PeriodType
                && (p.Year < period.Year || (p.Year == period.Year && p.Month < period.Month)))
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var prevPeriodNetMap = prevPeriodId.HasValue
            ? await context.PayrollRecords
                .AsNoTracking()
                .Where(r => r.PayrollPeriodId == prevPeriodId.Value && employeeIds.Contains(r.EmployeeId))
                .ToDictionaryAsync(r => r.EmployeeId, r => r.NetSalary, cancellationToken)
            : new Dictionary<int, decimal>();

        decimal varianceThreshold = statutoryContext.Parameter.PayrollVarianceWarningThresholdPercent > 0m
            ? statutoryContext.Parameter.PayrollVarianceWarningThresholdPercent
            : 20m;

        int totalPeriodWorkingDays = Math.Max(1, MidMonthSalarySplitter.CountWorkingDaysInRange(periodStartDate, periodEndDate));
        var evaluatedEmployees = new List<EvaluatedEmployeePayroll>(employees.Count);

        foreach (var emp in employees)
        {
            attendanceMap.TryGetValue(emp.Id, out var att);
            dependentCounts.TryGetValue(emp.Id, out var deps);
            int wageRegion = emp.WageRegion ?? emp.OrganizationUnit?.WageRegion ?? 1;

            DateOnly empActiveStart = emp.HireDate != default && emp.HireDate > periodStartDate ? emp.HireDate : periodStartDate;
            DateOnly empActiveEnd = emp.TerminationDate.HasValue && emp.TerminationDate.Value < periodEndDate ? emp.TerminationDate.Value : periodEndDate;

            decimal payableWorkdays;
            if (isBonusPeriodOnly)
            {
                payableWorkdays = 0m;
            }
            else if (att != null)
            {
                payableWorkdays = att.TotalPayableWorkdays;
            }
            else if (empActiveStart > periodStartDate || empActiveEnd < periodEndDate)
            {
                int empActiveDays = MidMonthSalarySplitter.CountWorkingDaysInRange(empActiveStart, empActiveEnd);
                payableWorkdays = Math.Round(period.StandardWorkdays * empActiveDays / totalPeriodWorkingDays, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                payableWorkdays = period.StandardWorkdays;
            }

            decimal ot150Night = isBonusPeriodOnly ? 0m : (att?.TotalOtHours150Night ?? 0m);
            decimal ot200Night = isBonusPeriodOnly ? 0m : (att?.TotalOtHours200Night ?? 0m);
            decimal ot300Night = isBonusPeriodOnly ? 0m : (att?.TotalOtHours300Night ?? 0m);

            decimal ot150Day = isBonusPeriodOnly ? 0m : (att != null && (att.TotalOtHours150Day > 0m || att.TotalOtHours150Night > 0m) ? att.TotalOtHours150Day : (att?.TotalOtHours150 ?? 0m));
            decimal ot200Day = isBonusPeriodOnly ? 0m : (att != null && (att.TotalOtHours200Day > 0m || att.TotalOtHours200Night > 0m) ? att.TotalOtHours200Day : (att?.TotalOtHours200 ?? 0m));
            decimal ot300Day = isBonusPeriodOnly ? 0m : (att != null && (att.TotalOtHours300Day > 0m || att.TotalOtHours300Night > 0m) ? att.TotalOtHours300Day : (att?.TotalOtHours300 ?? 0m));
            decimal nightHours = isBonusPeriodOnly ? 0m : (att?.TotalNightHours ?? 0m);

            decimal unusedLeavePayout = isBonusPeriodOnly
                ? 0m
                : (settlementByEmpId.TryGetValue(emp.Id, out var payoutAmt) ? payoutAmt : 0m);

            // Đợt chi Tạm ứng lương giữa tháng (MidMonthAdvance): chỉ trả tiền tạm ứng cho NV có đơn đã duyệt (chưa khấu trừ, quyết toán thuế vào lương chính cuối tháng)
            if (isAdvanceDisbursementPeriod)
            {
                if (!advanceByEmpId.TryGetValue(emp.Id, out var empMidAdvs) || empMidAdvs.Count == 0)
                    continue;

                decimal advTotal = empMidAdvs.Sum(a => a.Amount);
                var disbursedAdvIds = empMidAdvs.Select(a => a.Id).ToList();
                var advInput = new PayrollCalculationInput(
                    ContractBaseSalary: advTotal,
                    PositionAllowance: 0m,
                    OtherAllowance: 0m,
                    StandardWorkdays: period.StandardWorkdays,
                    PayableWorkdays: 0m,
                    OtHours150: 0m,
                    OtHours200: 0m,
                    OtHours300: 0m,
                    KpiBonus: 0m,
                    DisciplineDeduction: 0m,
                    DependentCount: deps,
                    IsUnionMember: false,
                    WageRegion: wageRegion,
                    InsuranceContractSalaryOverride: 0m,
                    IsBonusPeriodOnly: true);

                var advTrace = new List<CalculationTraceStepDto>
                {
                    new(1, "Advance", "Tổng tiền tạm ứng chi trả giữa tháng", "Tổng các đơn tạm ứng đã duyệt trong tháng",
                        string.Join("; ", empMidAdvs.Select(a => $"{a.AdvanceNumber}: {a.Amount:N0}")), advTotal, null),
                    new(2, "Tax", "Thuế TNCN đợt tạm ứng", "Chuyển sang quyết toán gộp cuối tháng theo kỳ lương chính",
                        "Thuế tạm ứng = 0 đ", 0m, null),
                    new(3, "Net", "Thực lĩnh đợt tạm ứng", "Tiền tạm ứng thực nhận",
                        $"Net = {advTotal:N0} đ", advTotal, null)
                };

                var advRec = new PayrollRecord
                {
                    PayrollPeriodId = period.Id,
                    EmployeeId = emp.Id,
                    OrganizationUnitId = emp.OrganizationUnitId,
                    PositionId = emp.PositionId,
                    StandardWorkdays = period.StandardWorkdays,
                    PayableWorkdays = 0m,
                    ContractBaseSalary = advTotal,
                    ActualBaseSalary = advTotal,
                    GrossIncome = advTotal,
                    TaxableIncome = 0m,
                    AssessableIncome = 0m,
                    PersonalIncomeTax = 0m,
                    NetSalary = advTotal,
                    TotalCompanyCost = advTotal,
                    DependentCount = deps,
                    TaxResidencyMode = "ResidentStandard",
                    PitBracketsJson = "[]",
                    CalculationTraceJson = JsonSerializer.Serialize(advTrace),
                    PaymentStatus = "Calculated",
                    Notes = $"Chi tạm ứng giữa tháng ({string.Join(", ", empMidAdvs.Select(a => a.AdvanceNumber))})"
                };

                evaluatedEmployees.Add(new EvaluatedEmployeePayroll(
                    emp,
                    att,
                    advRec,
                    advTrace,
                    Array.Empty<PitBracketTaxSliceDto>(),
                    Array.Empty<int>(),
                    Array.Empty<int>(),
                    Array.Empty<int>(),
                    Array.Empty<int>(),
                    previousRevisionNetMap.TryGetValue(emp.Id, out var prevAdvRevNet) ? prevAdvRevNet : null,
                    prevPeriodNetMap.TryGetValue(emp.Id, out var prevAdvPerNet) ? prevAdvPerNet : null,
                    null,
                    false,
                    disbursedAdvIds));
                continue;
            }

            // Tạm ứng, vay trả góp, truy lĩnh/truy thu, thưởng/kỷ luật
            var consumedAdvIds = new List<int>();
            decimal salaryAdvanceDed = 0m;
            if (!isBonusPeriodOnly && advanceByEmpId.TryGetValue(emp.Id, out var empAdvs))
            {
                foreach (var a in empAdvs)
                {
                    salaryAdvanceDed += a.Amount;
                    consumedAdvIds.Add(a.Id);
                }
            }

            var consumedInstIds = new List<int>();
            decimal loanInstDed = 0m;
            if (!isBonusPeriodOnly && installmentByEmpId.TryGetValue(emp.Id, out var empInsts))
            {
                foreach (var inst in empInsts)
                {
                    loanInstDed += inst.Amount;
                    consumedInstIds.Add(inst.Id);
                }
            }

            var consumedRdIds = new List<int>();
            decimal rewardExtra = 0m;
            decimal disciplineFine = 0m;
            if (!isBonusPeriodOnly && rdByEmpId.TryGetValue(emp.Id, out var empRds))
            {
                foreach (var rd in empRds)
                {
                    if (string.Equals(rd.Type, "Discipline", StringComparison.OrdinalIgnoreCase))
                        disciplineFine += rd.Amount;
                    else
                        rewardExtra += rd.Amount;
                    consumedRdIds.Add(rd.Id);
                }
            }

            var consumedAdjIds = new List<int>();
            decimal retroEarning = 0m;
            decimal retroDeduction = 0m;
            decimal commissionBonus = 0m;
            decimal thirteenthBonus = 0m;
            decimal kpiBonusManual = rewardExtra;
            decimal sourcePeriodTaxDelta = 0m;
            decimal sourcePeriodAdjustedTaxableExcluded = 0m;
            decimal targetPeriodTaxableDeductionFromRetro = 0m;
            decimal nonTaxableAdjExempt = 0m;

            if (adjByEmpId.TryGetValue(emp.Id, out var empAdjs))
            {
                foreach (var adj in empAdjs)
                {
                    if (isBonusPeriodOnly)
                    {
                        if (string.Equals(adj.AdjustmentType, period.PeriodType, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(adj.AdjustmentType, "Bonus13", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(adj.AdjustmentType, "HolidayBonus", StringComparison.OrdinalIgnoreCase))
                        {
                            thirteenthBonus += adj.Amount;
                            if (!adj.IsTaxable)
                                nonTaxableAdjExempt += adj.Amount;
                            consumedAdjIds.Add(adj.Id);
                        }
                    }
                    else
                    {
                        switch (adj.AdjustmentType)
                        {
                            case "RetroEarning":
                                retroEarning += adj.Amount;
                                if (!adj.IsTaxable)
                                    nonTaxableAdjExempt += adj.Amount;
                                else if (adj.RecalculateTaxInSourcePeriod)
                                {
                                    sourcePeriodAdjustedTaxableExcluded += adj.Amount;
                                    sourcePeriodTaxDelta += adj.SourcePeriodTaxDelta;
                                }
                                consumedAdjIds.Add(adj.Id);
                                break;
                            case "RetroDeduction":
                                retroDeduction += adj.Amount;
                                if (adj.IsTaxable)
                                {
                                    if (adj.RecalculateTaxInSourcePeriod)
                                        sourcePeriodTaxDelta += adj.SourcePeriodTaxDelta;
                                    else if (adj.TaxCalculationNote != null
                                        && adj.TaxCalculationNote.Contains("gộp tính tại kỳ mở", StringComparison.OrdinalIgnoreCase))
                                        targetPeriodTaxableDeductionFromRetro += adj.Amount;
                                }
                                consumedAdjIds.Add(adj.Id);
                                break;
                            case "Commission":
                                commissionBonus += adj.Amount;
                                if (!adj.IsTaxable)
                                    nonTaxableAdjExempt += adj.Amount;
                                else if (adj.RecalculateTaxInSourcePeriod)
                                {
                                    sourcePeriodAdjustedTaxableExcluded += adj.Amount;
                                    sourcePeriodTaxDelta += adj.SourcePeriodTaxDelta;
                                }
                                consumedAdjIds.Add(adj.Id);
                                break;
                            case "Bonus13":
                            case "HolidayBonus":
                                // Nếu tháng này đã mở đợt chi thưởng tách riêng cho loại thưởng tương ứng -> để dành cho đợt chi đó
                                if (dedicatedBonusPeriodTypesInMonth.Contains(adj.AdjustmentType))
                                    break;
                                thirteenthBonus += adj.Amount;
                                if (!adj.IsTaxable)
                                    nonTaxableAdjExempt += adj.Amount;
                                consumedAdjIds.Add(adj.Id);
                                break;
                        }
                    }
                }
            }

            profilesByEmpId.TryGetValue(emp.Id, out var empProfiles);
            var latestProfile = empProfiles != null && empProfiles.Count > 0 ? empProfiles[^1] : null;
            decimal referenceBaseSalary = latestProfile?.BaseSalary ?? emp.BaseSalary;

            // KPI Evaluation Bonus (nếu có đánh giá KPI được duyệt kết thúc trong kỳ)
            decimal kpiMultiplier = 1.0m;
            bool templateComputesKpi = latestProfile?.SalaryTemplateId != null
                && templateComponentMap.TryGetValue(latestProfile.SalaryTemplateId.Value, out var tplSpecs)
                && tplSpecs.Any(s => string.Equals(s.ComponentCode, "KPI_BONUS", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(s.CalculationType, "Formula", StringComparison.OrdinalIgnoreCase));

            if (!isBonusPeriodOnly && kpiEvalByEmpId.TryGetValue(emp.Id, out var kpiEval))
            {
                kpiMultiplier = kpiEval.BonusMultiplier;
                if (!templateComputesKpi)
                {
                    if (taxExemptParamValues.TryGetValue("KPI_BONUS_BASE_AMOUNT", out var flatKpi) && flatKpi > 0m)
                        kpiBonusManual += Math.Round(flatKpi * kpiEval.BonusMultiplier, 0, MidpointRounding.AwayFromZero);
                    else if (taxExemptParamValues.TryGetValue("KPI_BONUS_RATE_OF_BASE", out var rateKpi) && rateKpi > 0m)
                        kpiBonusManual += Math.Round(referenceBaseSalary * rateKpi * kpiEval.BonusMultiplier, 0, MidpointRounding.AwayFromZero);
                }
            }

            // Nếu là kỳ thưởng Bonus13 riêng và chưa có Adjustment tường minh -> tự động phân bổ theo số tháng làm việc trong năm
            if (isBonusPeriodOnly && thirteenthBonus == 0m && string.Equals(period.PeriodType, "Bonus13", StringComparison.OrdinalIgnoreCase))
            {
                int startMonth = emp.HireDate != default && emp.HireDate.Year == period.Year ? emp.HireDate.Month : 1;
                int monthsWorked = Math.Max(0, period.Month - startMonth + 1);
                thirteenthBonus = Math.Round(referenceBaseSalary * monthsWorked / 12m, 0, MidpointRounding.AwayFromZero);
            }

            bool hasPriorDisbursement = priorMonthTaxableMap.ContainsKey(emp.Id);
            decimal priorMonthTaxable = priorMonthTaxableMap.TryGetValue(emp.Id, out var pTax) ? pTax : 0m;
            decimal priorMonthPit = priorMonthPitMap.TryGetValue(emp.Id, out var pPit) ? pPit : 0m;
            decimal priorMonthAssessable = priorMonthAssessableMap.TryGetValue(emp.Id, out var priorAss)
                ? priorAss
                : 0m;

            PayrollCalculationBreakdown breakdown;
            decimal contractBaseSalary;
            decimal posAllowance;
            decimal otherAllowance;
            decimal taxExemptAmount = 0m;
            decimal disciplineDeduction = disciplineFine;
            string? splitNote = null;
            IReadOnlyList<EvaluatedSalaryLineDto>? evaluatedLines = null;
            NetToGrossSolution? netToGrossSol = null;
            PayrollCalculationInput? traceInput = null;

            if (!isBonusPeriodOnly && empProfiles != null && empProfiles.Count > 0)
            {
                dailyAttendanceByEmpId.TryGetValue(emp.Id, out var dailyAtts);
                var slices = MidMonthSalarySplitter.BuildPeriodSlices(
                    periodStartDate,
                    periodEndDate,
                    payableWorkdays,
                    ot150Day,
                    ot200Day,
                    ot300Day,
                    nightHours,
                    empProfiles,
                    templateComponentMap,
                    dailyAtts,
                    activeStart: empActiveStart,
                    activeEnd: empActiveEnd);

                var lastProf = slices[^1].Profile;
                var lastTplId = lastProf.SalaryTemplateId ?? 0;
                var roundingRule = templateRoundingMap.TryGetValue(lastTplId, out var rRule) ? rRule : "RoundToDong";

                decimal? solvedOverride = null;
                if (lastProf.IsNetContract && lastProf.TargetNetSalary.HasValue && lastProf.TargetNetSalary.Value > 0m)
                {
                    var preAgg = VietnamPayrollEngine.CalculateFromMidMonthSlices(
                        slices,
                        statutoryContext,
                        period.StandardWorkdays,
                        payableWorkdays,
                        ot150Day,
                        ot200Day,
                        ot300Day,
                        deps,
                        wageRegion,
                        unusedLeavePayout: unusedLeavePayout,
                        kpiBonus: kpiBonusManual,
                        disciplineDeduction: disciplineFine,
                        roundingRule: roundingRule,
                        taxExemptParamValues: taxExemptParamValues,
                        regularNightHours: nightHours,
                        totalOt150Night: ot150Night,
                        totalOt200Night: ot200Night,
                        totalOt300Night: ot300Night,
                        commissionBonus: commissionBonus,
                        thirteenthMonthBonus: thirteenthBonus,
                        retroEarningAmount: retroEarning,
                        retroDeductionAmount: retroDeduction,
                        salaryAdvanceDeduction: salaryAdvanceDed,
                        loanInstallmentDeduction: loanInstDed,
                        kpiMultiplier: kpiMultiplier,
                        hasPriorMonthlyDisbursement: hasPriorDisbursement,
                        priorMonthTaxableIncome: priorMonthTaxable,
                        priorMonthPitWithheld: priorMonthPit,
                        sourcePeriodTaxDelta: sourcePeriodTaxDelta,
                        sourcePeriodAdjustedTaxableExcluded: sourcePeriodAdjustedTaxableExcluded,
                        targetPeriodTaxableDeductionFromRetro: targetPeriodTaxableDeductionFromRetro);

                    decimal? fixedIns = VietnamPayrollEngine.ResolveInsuranceOverride(lastProf);
                    netToGrossSol = VietnamPayrollEngine.SolveGrossFromNet(
                        new NetToGrossInput(
                            TargetNetSalary: lastProf.TargetNetSalary.Value,
                            PositionAllowance: preAgg.EffectivePositionAllowance,
                            OtherAllowance: preAgg.EffectiveOtherAllowance,
                            StandardWorkdays: period.StandardWorkdays,
                            PayableWorkdays: payableWorkdays,
                            OtHours150: ot150Day,
                            OtHours200: ot200Day,
                            OtHours300: ot300Day,
                            KpiBonus: kpiBonusManual,
                            CommissionBonus: commissionBonus,
                            ThirteenthMonthBonus: thirteenthBonus,
                            UnusedLeavePayout: unusedLeavePayout,
                            RetroEarningAmount: retroEarning,
                            DisciplineDeduction: disciplineFine,
                            RetroDeductionAmount: retroDeduction,
                            SalaryAdvanceDeduction: salaryAdvanceDed,
                            LoanInstallmentDeduction: loanInstDed,
                            DependentCount: deps,
                            IsUnionMember: lastProf.IsUnionMember,
                            WageRegion: wageRegion,
                            FixedInsuranceContractSalary: fixedIns,
                            TaxResidencyMode: lastProf.TaxResidencyMode,
                            HasPitCommitment08: lastProf.HasPitCommitment08,
                            OtHours150Night: ot150Night,
                            OtHours200Night: ot200Night,
                            OtHours300Night: ot300Night,
                            RegularNightHours: nightHours,
                            ExtraTemplateEarnings: preAgg.ExtraTemplateEarnings,
                            ExtraTaxExemptAmount: preAgg.TaxExemptAmount + nonTaxableAdjExempt,
                            CustomDeductions: preAgg.CustomDeductions,
                            HasPriorMonthlyDisbursement: hasPriorDisbursement,
                            PriorMonthTaxableIncome: priorMonthTaxable,
                            PriorMonthPitWithheld: priorMonthPit,
                            SourcePeriodTaxDelta: sourcePeriodTaxDelta,
                            SourcePeriodAdjustedTaxableExcluded: sourcePeriodAdjustedTaxableExcluded,
                            TargetPeriodTaxableDeductionFromRetro: targetPeriodTaxableDeductionFromRetro),
                        statutoryContext);
                    solvedOverride = netToGrossSol.SolvedContractBaseSalary;
                }

                var agg = VietnamPayrollEngine.CalculateFromMidMonthSlices(
                    slices,
                    statutoryContext,
                    period.StandardWorkdays,
                    payableWorkdays,
                    ot150Day,
                    ot200Day,
                    ot300Day,
                    deps,
                    wageRegion,
                    unusedLeavePayout: unusedLeavePayout,
                    kpiBonus: kpiBonusManual,
                    disciplineDeduction: disciplineFine,
                    roundingRule: roundingRule,
                    taxExemptParamValues: taxExemptParamValues,
                    regularNightHours: nightHours,
                    totalOt150Night: ot150Night,
                    totalOt200Night: ot200Night,
                    totalOt300Night: ot300Night,
                    commissionBonus: commissionBonus,
                    thirteenthMonthBonus: thirteenthBonus,
                    retroEarningAmount: retroEarning,
                    retroDeductionAmount: retroDeduction,
                    salaryAdvanceDeduction: salaryAdvanceDed,
                    loanInstallmentDeduction: loanInstDed,
                    kpiMultiplier: kpiMultiplier,
                    solvedBaseSalaryOverride: solvedOverride,
                    hasPriorMonthlyDisbursement: hasPriorDisbursement,
                    priorMonthTaxableIncome: priorMonthTaxable,
                    priorMonthPitWithheld: priorMonthPit,
                    sourcePeriodTaxDelta: sourcePeriodTaxDelta,
                    sourcePeriodAdjustedTaxableExcluded: sourcePeriodAdjustedTaxableExcluded,
                    targetPeriodTaxableDeductionFromRetro: targetPeriodTaxableDeductionFromRetro);

                breakdown = agg.Breakdown;
                contractBaseSalary = agg.EffectiveContractBaseSalary;
                posAllowance = agg.EffectivePositionAllowance;
                otherAllowance = agg.EffectiveOtherAllowance + agg.ExtraTemplateEarnings;
                taxExemptAmount = agg.TaxExemptAmount + breakdown.OtTaxExemptAmount + nonTaxableAdjExempt;
                disciplineDeduction = disciplineFine + agg.CustomDeductions;
                splitNote = agg.SplitSummaryNote;
                evaluatedLines = agg.AggregatedLines;

                traceInput = new PayrollCalculationInput(
                    contractBaseSalary,
                    posAllowance,
                    otherAllowance,
                    period.StandardWorkdays,
                    payableWorkdays,
                    ot150Day,
                    ot200Day,
                    ot300Day,
                    kpiBonusManual,
                    disciplineFine,
                    deps,
                    lastProf.IsUnionMember,
                    wageRegion,
                    unusedLeavePayout,
                    agg.InsuranceContractSalaryUsed,
                    breakdown.ActualBaseSalary,
                    breakdown.OvertimePay,
                    agg.ExtraTemplateEarnings,
                    agg.TaxExemptAmount + nonTaxableAdjExempt,
                    agg.CustomDeductions,
                    ot150Night,
                    ot200Night,
                    ot300Night,
                    nightHours,
                    commissionBonus,
                    thirteenthBonus,
                    retroEarning,
                    retroDeduction,
                    salaryAdvanceDed,
                    loanInstDed,
                    lastProf.TaxResidencyMode,
                    lastProf.HasPitCommitment08,
                    HasPriorMonthlyDisbursement: hasPriorDisbursement,
                    PriorMonthTaxableIncome: priorMonthTaxable,
                    PriorMonthPitWithheld: priorMonthPit,
                    SourcePeriodTaxDelta: sourcePeriodTaxDelta,
                    SourcePeriodAdjustedTaxableExcluded: sourcePeriodAdjustedTaxableExcluded,
                    TargetPeriodTaxableDeductionFromRetro: targetPeriodTaxableDeductionFromRetro);
            }
            else
            {
                string residencyMode = latestProfile?.TaxResidencyMode ?? "ResidentStandard";
                bool commitment08 = latestProfile?.HasPitCommitment08 ?? false;
                bool isUnion = latestProfile?.IsUnionMember ?? emp.IsUnionMember;

                if (!isBonusPeriodOnly && latestProfile is { IsNetContract: true, TargetNetSalary: > 0m })
                {
                    netToGrossSol = VietnamPayrollEngine.SolveGrossFromNet(
                        new NetToGrossInput(
                            TargetNetSalary: latestProfile.TargetNetSalary.Value,
                            PositionAllowance: latestProfile.PositionAllowance,
                            OtherAllowance: latestProfile.OtherAllowance,
                            StandardWorkdays: period.StandardWorkdays,
                            PayableWorkdays: payableWorkdays,
                            OtHours150: ot150Day,
                            OtHours200: ot200Day,
                            OtHours300: ot300Day,
                            KpiBonus: kpiBonusManual,
                            CommissionBonus: commissionBonus,
                            ThirteenthMonthBonus: thirteenthBonus,
                            UnusedLeavePayout: unusedLeavePayout,
                            RetroEarningAmount: retroEarning,
                            DisciplineDeduction: disciplineFine,
                            RetroDeductionAmount: retroDeduction,
                            SalaryAdvanceDeduction: salaryAdvanceDed,
                            LoanInstallmentDeduction: loanInstDed,
                            DependentCount: deps,
                            IsUnionMember: isUnion,
                            WageRegion: wageRegion,
                            FixedInsuranceContractSalary: VietnamPayrollEngine.ResolveInsuranceOverride(latestProfile),
                            TaxResidencyMode: residencyMode,
                            HasPitCommitment08: commitment08,
                            OtHours150Night: ot150Night,
                            OtHours200Night: ot200Night,
                            OtHours300Night: ot300Night,
                            RegularNightHours: nightHours,
                            ExtraTaxExemptAmount: nonTaxableAdjExempt,
                            HasPriorMonthlyDisbursement: hasPriorDisbursement,
                            PriorMonthTaxableIncome: priorMonthTaxable,
                            PriorMonthPitWithheld: priorMonthPit,
                            SourcePeriodTaxDelta: sourcePeriodTaxDelta,
                            SourcePeriodAdjustedTaxableExcluded: sourcePeriodAdjustedTaxableExcluded,
                            TargetPeriodTaxableDeductionFromRetro: targetPeriodTaxableDeductionFromRetro),
                        statutoryContext);
                    contractBaseSalary = netToGrossSol.SolvedContractBaseSalary;
                    posAllowance = latestProfile.PositionAllowance;
                    otherAllowance = latestProfile.OtherAllowance;
                }
                else
                {
                    contractBaseSalary = latestProfile?.BaseSalary ?? emp.BaseSalary;
                    posAllowance = isBonusPeriodOnly ? 0m : (latestProfile?.PositionAllowance ?? emp.PositionAllowance);
                    otherAllowance = isBonusPeriodOnly ? 0m : (latestProfile?.OtherAllowance ?? emp.OtherAllowance);
                }

                traceInput = new PayrollCalculationInput(
                    contractBaseSalary,
                    posAllowance,
                    otherAllowance,
                    period.StandardWorkdays,
                    payableWorkdays,
                    ot150Day,
                    ot200Day,
                    ot300Day,
                    kpiBonusManual,
                    disciplineFine,
                    deps,
                    isUnion,
                    wageRegion,
                    unusedLeavePayout,
                    InsuranceContractSalaryOverride: isBonusPeriodOnly
                        ? 0m
                        : VietnamPayrollEngine.ResolveInsuranceOverride(latestProfile),
                    OtHours150Night: ot150Night,
                    OtHours200Night: ot200Night,
                    OtHours300Night: ot300Night,
                    RegularNightHours: nightHours,
                    CommissionBonus: commissionBonus,
                    ThirteenthMonthBonus: thirteenthBonus,
                    RetroEarningAmount: retroEarning,
                    RetroDeductionAmount: retroDeduction,
                    SalaryAdvanceDeduction: salaryAdvanceDed,
                    LoanInstallmentDeduction: loanInstDed,
                    TaxResidencyMode: residencyMode,
                    HasPitCommitment08: commitment08,
                    IsBonusPeriodOnly: isBonusPeriodOnly,
                    PriorMonthAssessableIncome: priorMonthAssessable,
                    ExtraTaxExemptAmount: nonTaxableAdjExempt,
                    HasPriorMonthlyDisbursement: hasPriorDisbursement,
                    PriorMonthTaxableIncome: priorMonthTaxable,
                    PriorMonthPitWithheld: priorMonthPit,
                    SourcePeriodTaxDelta: sourcePeriodTaxDelta,
                    SourcePeriodAdjustedTaxableExcluded: sourcePeriodAdjustedTaxableExcluded,
                    TargetPeriodTaxableDeductionFromRetro: targetPeriodTaxableDeductionFromRetro);

                breakdown = VietnamPayrollEngine.Calculate(traceInput, statutoryContext);
                taxExemptAmount = breakdown.OtTaxExemptAmount + nonTaxableAdjExempt;
            }

            var pitSlices = string.Equals(breakdown.TaxResidencyMode, "ResidentStandard", StringComparison.OrdinalIgnoreCase)
                ? VietnamPayrollEngine.CalculateProgressivePitBreakdown(breakdown.AssessableIncome, statutoryContext.PitBrackets)
                : Array.Empty<PitBracketTaxSliceDto>();

            var traceSteps = VietnamPayrollEngine.BuildCalculationTrace(
                traceInput,
                breakdown,
                statutoryContext.Parameter,
                pitSlices,
                netToGrossSol,
                splitNote);

            decimal? prevRevNet = previousRevisionNetMap.TryGetValue(emp.Id, out var prNet) ? prNet : null;
            decimal? prevPeriodNet = prevPeriodNetMap.TryGetValue(emp.Id, out var ppNet) ? ppNet : null;

            decimal? variancePct = null;
            bool highVariance = false;
            if (prevPeriodNet.HasValue && prevPeriodNet.Value > 0m)
            {
                variancePct = Math.Round(
                    ((breakdown.NetSalary - prevPeriodNet.Value) / prevPeriodNet.Value) * 100m,
                    2,
                    MidpointRounding.AwayFromZero);
                highVariance = Math.Abs(variancePct.Value) > varianceThreshold;
            }

            var rec = new PayrollRecord
            {
                PayrollPeriodId = period.Id,
                EmployeeId = emp.Id,
                OrganizationUnitId = emp.OrganizationUnitId,
                PositionId = emp.PositionId,
                MonthlyAttendanceSummaryId = att?.Id,
                StandardWorkdays = period.StandardWorkdays,
                PayableWorkdays = payableWorkdays,
                OtHours150 = ot150Day + ot150Night,
                OtHours200 = ot200Day + ot200Night,
                OtHours300 = ot300Day + ot300Night,
                TotalOtHours = ot150Day + ot150Night + ot200Day + ot200Night + ot300Day + ot300Night,
                ContractBaseSalary = contractBaseSalary,
                ActualBaseSalary = breakdown.ActualBaseSalary,
                PositionAllowance = posAllowance,
                OtherAllowance = otherAllowance,
                OvertimePay = breakdown.OvertimePay,
                OtNightExtraPay = breakdown.OtNightExtraPay,
                OtTaxExemptAmount = taxExemptAmount,
                KpiBonus = kpiBonusManual,
                CommissionBonus = commissionBonus,
                ThirteenthMonthBonus = thirteenthBonus,
                UnusedLeavePayout = unusedLeavePayout,
                RetroEarningAmount = retroEarning,
                GrossIncome = breakdown.GrossIncome,
                InsuranceSalaryBase = breakdown.SocialInsuranceBase,
                UnemploymentInsuranceBase = breakdown.UnemploymentInsuranceBase,
                EmployeeSocialInsurance = breakdown.EmployeeSocialInsurance,
                EmployeeHealthInsurance = breakdown.EmployeeHealthInsurance,
                EmployeeUnemploymentInsurance = breakdown.EmployeeUnemploymentInsurance,
                TotalEmployeeInsurance = breakdown.TotalEmployeeInsurance,
                EmployeeUnionFee = breakdown.EmployeeUnionFee,
                CompanySocialInsurance = breakdown.CompanySocialInsurance,
                CompanyHealthInsurance = breakdown.CompanyHealthInsurance,
                CompanyUnemploymentInsurance = breakdown.CompanyUnemploymentInsurance,
                CompanyOccupationalAccidentInsurance = breakdown.CompanyOccupationalAccidentInsurance,
                CompanyUnionFee = breakdown.CompanyUnionFee,
                TotalCompanyCost = breakdown.TotalCompanyCost,
                DependentCount = deps,
                SelfDeductionAmount = breakdown.SelfDeductionAmount,
                DependentDeductionAmount = breakdown.DependentDeductionAmount,
                TaxableIncome = breakdown.TaxableIncome,
                AssessableIncome = breakdown.AssessableIncome,
                PersonalIncomeTax = breakdown.PersonalIncomeTax,
                TaxResidencyMode = breakdown.TaxResidencyMode,
                UsedCommitment08 = breakdown.UsedCommitment08,
                IsNetToGross = netToGrossSol != null,
                TargetNetSalary = netToGrossSol?.TargetNetSalary,
                PitBracketsJson = JsonSerializer.Serialize(pitSlices),
                CalculationTraceJson = JsonSerializer.Serialize(traceSteps),
                DisciplineDeduction = disciplineDeduction,
                SalaryAdvanceDeduction = salaryAdvanceDed,
                LoanInstallmentDeduction = loanInstDed,
                RetroDeductionAmount = retroDeduction,
                NetSalary = breakdown.NetSalary,
                PreviousRevisionNetSalary = prevRevNet,
                PreviousPeriodNetSalary = prevPeriodNet,
                VariancePercentVsPreviousPeriod = variancePct,
                HasHighVarianceWarning = highVariance,
                PaymentStatus = "Calculated",
                Notes = splitNote
            };

            if (evaluatedLines != null)
            {
                foreach (var line in evaluatedLines.Where(l =>
                    l.ShowOnPayslip
                    && l.SalaryComponentId > 0
                    && !string.Equals(l.ComponentType, "Info", StringComparison.OrdinalIgnoreCase)))
                {
                    rec.Details.Add(new PayrollRecordDetail
                    {
                        SalaryComponentId = line.SalaryComponentId,
                        ComponentCode = line.ComponentCode,
                        ComponentName = line.ComponentName,
                        Amount = line.FinalAmount,
                        CalculationNote = line.Note
                    });
                }
            }

            evaluatedEmployees.Add(new EvaluatedEmployeePayroll(
                emp,
                att,
                rec,
                traceSteps,
                pitSlices,
                consumedAdvIds,
                consumedInstIds,
                consumedAdjIds,
                consumedRdIds,
                prevRevNet,
                prevPeriodNet,
                variancePct,
                highVariance));
        }

        return new EvaluatedPeriodBundle(
            period,
            periodStartDate,
            periodEndDate,
            statutoryContext,
            attendanceLock,
            previousTotalPeriodNet,
            previousRevisionNetMap.Count > 0,
            allCurrentRecords,
            evaluatedEmployees);
    }
}

public sealed record CalculateMonthlyPayrollCommand(
    int PayrollPeriodId,
    int? EmployeeId = null,
    string? Notes = null,
    bool RequireLockedAttendance = false) : IRequest<int>;

public class CalculateMonthlyPayrollCommandHandler : IRequestHandler<CalculateMonthlyPayrollCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public CalculateMonthlyPayrollCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CalculateMonthlyPayrollCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.RequireUserId();
        var actorUsername = _currentUser.RequireUsername();

        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        if (period.IsLocked ||
            period.Status is PayrollPeriodStatuses.Locked or PayrollPeriodStatuses.Paid
                or PayrollPeriodStatuses.Submitted or PayrollPeriodStatuses.Approved)
        {
            throw new InvalidOperationException(
                $"Kỳ lương {period.PeriodCode} đang ở trạng thái '{period.Status}', không được phép tính lại hoặc chỉnh sửa.");
        }

        var eval = await PayrollPeriodEvaluator.EvaluatePeriodAsync(
            _context, period, request.EmployeeId, asNoTracking: false, cancellationToken);

        bool isMonthly = string.Equals(period.PeriodType, "Monthly", StringComparison.OrdinalIgnoreCase);
        if (isMonthly && request.RequireLockedAttendance && !eval.AttendanceLockStatus.IsAllLocked)
        {
            throw new InvalidOperationException(
                $"Không thể chạy tính lương chính thức khi bảng công tháng {period.Month:D2}/{period.Year} chưa chốt khóa. {eval.AttendanceLockStatus.WarningMessage}");
        }

        period.HasAttendanceLockWarning = isMonthly && !eval.AttendanceLockStatus.IsAllLocked;
        period.AttendanceWarningMessage = isMonthly && !eval.AttendanceLockStatus.IsAllLocked
            ? eval.AttendanceLockStatus.WarningMessage
            : null;

        var recordsToRemove = request.EmployeeId.HasValue
            ? eval.ExistingCurrentRecords.Where(r => r.EmployeeId == request.EmployeeId.Value).ToList()
            : eval.ExistingCurrentRecords.ToList();

        if (recordsToRemove.Count > 0)
        {
            var removeRecordIds = recordsToRemove.Select(r => r.Id).ToHashSet();
            var oldDetails = await _context.PayrollRecordDetails
                .Where(d => removeRecordIds.Contains(d.PayrollRecordId))
                .ToListAsync(cancellationToken);
            if (oldDetails.Count > 0)
                _context.PayrollRecordDetails.RemoveRange(oldDetails);

            _context.PayrollRecords.RemoveRange(recordsToRemove);
        }

        var affectedEmpIds = eval.EvaluatedEmployees.Select(e => e.Employee.Id).ToHashSet();

        // Giải phóng liên kết cũ (tính lại là hoàn toàn idempotent) rồi gán lại theo lần tính mới
        var prevDisbursedAdvs = await _context.SalaryAdvances
            .Where(a => a.DisbursementPayrollPeriodId == period.Id && affectedEmpIds.Contains(a.EmployeeId))
            .ToListAsync(cancellationToken);
        foreach (var adv in prevDisbursedAdvs)
            adv.DisbursementPayrollPeriodId = null;

        var prevLinkedAdvs = await _context.SalaryAdvances
            .Where(a => a.DeductedPayrollPeriodId == period.Id && affectedEmpIds.Contains(a.EmployeeId))
            .ToListAsync(cancellationToken);
        foreach (var adv in prevLinkedAdvs)
        {
            adv.Status = "Approved";
            adv.DeductedPayrollPeriodId = null;
        }

        var affectedLoanIds = new HashSet<int>();
        var prevLinkedInsts = await _context.EmployeeLoanInstallments
            .Where(i => i.DeductedPayrollPeriodId == period.Id && affectedEmpIds.Contains(i.EmployeeId))
            .ToListAsync(cancellationToken);
        foreach (var inst in prevLinkedInsts)
        {
            inst.Status = "Scheduled";
            inst.DeductedPayrollPeriodId = null;
            affectedLoanIds.Add(inst.EmployeeLoanId);
        }

        var prevLinkedAdjs = await _context.PayrollAdjustments
            .Where(a => a.AppliedPayrollPeriodId == period.Id && affectedEmpIds.Contains(a.EmployeeId))
            .ToListAsync(cancellationToken);
        foreach (var adj in prevLinkedAdjs)
        {
            adj.Status = "Approved";
            adj.AppliedPayrollPeriodId = null;
        }

        var allConsumedDisbursedAdvIds = eval.EvaluatedEmployees
            .Where(e => e.ConsumedDisbursedAdvanceIds != null)
            .SelectMany(e => e.ConsumedDisbursedAdvanceIds!)
            .ToHashSet();
        if (allConsumedDisbursedAdvIds.Count > 0)
        {
            var toDisburseAdvs = await _context.SalaryAdvances
                .Where(a => allConsumedDisbursedAdvIds.Contains(a.Id))
                .ToListAsync(cancellationToken);
            foreach (var adv in toDisburseAdvs)
                adv.DisbursementPayrollPeriodId = period.Id;
        }

        var allConsumedAdvIds = eval.EvaluatedEmployees.SelectMany(e => e.ConsumedAdvanceIds).ToHashSet();
        if (allConsumedAdvIds.Count > 0)
        {
            var toDeductAdvs = await _context.SalaryAdvances
                .Where(a => allConsumedAdvIds.Contains(a.Id))
                .ToListAsync(cancellationToken);
            foreach (var adv in toDeductAdvs)
            {
                adv.Status = "Deducted";
                adv.DeductedPayrollPeriodId = period.Id;
            }
        }

        var allConsumedInstIds = eval.EvaluatedEmployees.SelectMany(e => e.ConsumedLoanInstallmentIds).ToHashSet();
        if (allConsumedInstIds.Count > 0)
        {
            var toDeductInsts = await _context.EmployeeLoanInstallments
                .Where(i => allConsumedInstIds.Contains(i.Id))
                .ToListAsync(cancellationToken);
            foreach (var inst in toDeductInsts)
            {
                inst.Status = "Deducted";
                inst.DeductedPayrollPeriodId = period.Id;
                affectedLoanIds.Add(inst.EmployeeLoanId);
            }
        }

        if (affectedLoanIds.Count > 0)
        {
            var loansToCheck = await _context.EmployeeLoans
                .Include(l => l.Installments)
                .Where(l => affectedLoanIds.Contains(l.Id))
                .ToListAsync(cancellationToken);
            foreach (var loan in loansToCheck)
            {
                if (loan.Status is "Active" or "PaidOff")
                {
                    bool hasRemainingScheduled = loan.Installments.Any(i => i.Status == "Scheduled");
                    loan.Status = hasRemainingScheduled ? "Active" : "PaidOff";
                }
            }
        }

        var allConsumedAdjIds = eval.EvaluatedEmployees.SelectMany(e => e.ConsumedAdjustmentIds).ToHashSet();
        if (allConsumedAdjIds.Count > 0)
        {
            var toApplyAdjs = await _context.PayrollAdjustments
                .Where(a => allConsumedAdjIds.Contains(a.Id))
                .ToListAsync(cancellationToken);
            foreach (var adj in toApplyAdjs)
            {
                adj.Status = "Applied";
                adj.AppliedPayrollPeriodId = period.Id;
            }
        }

        var allConsumedRdIds = eval.EvaluatedEmployees.SelectMany(e => e.ConsumedRewardDisciplineIds).ToHashSet();
        if (allConsumedRdIds.Count > 0)
        {
            var toLinkRds = await _context.RewardDisciplines
                .Where(r => allConsumedRdIds.Contains(r.Id))
                .ToListAsync(cancellationToken);
            foreach (var rd in toLinkRds)
                rd.PayrollPeriodId = period.Id;
        }

        var snapshotItems = new List<object>();
        int highVarianceCount = 0;
        var newlyAdded = new List<PayrollRecord>(eval.EvaluatedEmployees.Count);

        foreach (var item in eval.EvaluatedEmployees)
        {
            if (item.HasHighVarianceWarning)
                highVarianceCount++;

            newlyAdded.Add(item.Record);
            _context.PayrollRecords.Add(item.Record);

            snapshotItems.Add(new
            {
                item.Employee.EmployeeCode,
                item.Employee.FullName,
                NewNet = item.Record.NetSalary,
                PreviousRevisionNet = item.PreviousRevisionNet,
                DeltaFromPreviousRevision = item.PreviousRevisionNet.HasValue ? item.Record.NetSalary - item.PreviousRevisionNet.Value : 0m,
                PreviousPeriodNet = item.PreviousPeriodNet,
                VariancePercent = item.VariancePercent,
                HighVarianceWarning = item.HasHighVarianceWarning
            });
        }

        var unchangedRecords = request.EmployeeId.HasValue
            ? eval.ExistingCurrentRecords.Where(r => r.EmployeeId != request.EmployeeId.Value).ToList()
            : new List<PayrollRecord>();

        var combinedGross = unchangedRecords.Sum(r => r.GrossIncome) + newlyAdded.Sum(r => r.GrossIncome);
        var combinedPit = unchangedRecords.Sum(r => r.PersonalIncomeTax) + newlyAdded.Sum(r => r.PersonalIncomeTax);
        var combinedNet = unchangedRecords.Sum(r => r.NetSalary) + newlyAdded.Sum(r => r.NetSalary);
        var totalRevDelta = eval.HasPreviousRevision ? combinedNet - eval.PreviousTotalPeriodNet : 0m;

        period.CalculationVersion += 1;
        period.Status = PayrollPeriodStatuses.Calculated;
        period.CalculatedByUserId = actorUserId;
        period.CalculatedAt = DateTime.UtcNow;

        var revision = new PayrollCalculationRevision
        {
            PayrollPeriodId = period.Id,
            EmployeeId = request.EmployeeId,
            RevisionNumber = period.CalculationVersion,
            Scope = request.EmployeeId.HasValue ? "SingleEmployee" : "FullPeriod",
            AffectedEmployeeCount = newlyAdded.Count,
            TotalGrossIncome = combinedGross,
            TotalPersonalIncomeTax = combinedPit,
            TotalNetSalary = combinedNet,
            NetDeltaFromPreviousRevision = totalRevDelta,
            HighVarianceWarningCount = highVarianceCount,
            SnapshotJson = JsonSerializer.Serialize(snapshotItems),
            CalculatedByUsername = actorUsername,
            CalculatedAt = DateTime.UtcNow,
            Notes = request.Notes?.Trim()
        };
        await _context.PayrollCalculationRevisions.AddAsync(revision, cancellationToken);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "Payroll",
            ActionType = request.EmployeeId.HasValue ? "PayrollPeriod_RecalculateEmployee" : "PayrollPeriod_Calculate",
            EntityName = nameof(PayrollPeriod),
            EntityId = period.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { Revision = period.CalculationVersion - 1, PreviousTotalNet = eval.PreviousTotalPeriodNet }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                period.PeriodCode,
                period.PeriodType,
                Revision = period.CalculationVersion,
                Scope = revision.Scope,
                AffectedEmployees = newlyAdded.Count,
                TotalGross = combinedGross,
                TotalNet = combinedNet,
                NetDeltaFromPreviousRevision = totalRevDelta,
                HighVarianceWarningCount = highVarianceCount,
                AttendanceLockWarning = period.HasAttendanceLockWarning
            }),
            AffectedColumns = "Status,CalculationVersion,CalculatedByUserId,CalculatedAt,HasAttendanceLockWarning",
            IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser?.UserAgent ?? "PayrollEngine",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return employeesCountSafe(newlyAdded.Count);

        static int employeesCountSafe(int n) => n;
    }
}

public sealed record LockPayrollPeriodCommand(int PayrollPeriodId) : IRequest<bool>;

public class LockPayrollPeriodCommandHandler : IRequestHandler<LockPayrollPeriodCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public LockPayrollPeriodCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(LockPayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.RequireUserId();
        var actorUsername = _currentUser.RequireUsername();

        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        if (!string.Equals(period.Status, PayrollPeriodStatuses.Approved, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Chỉ được phép khóa kỳ lương khi kỳ đang ở trạng thái 'Approved' (trạng thái hiện tại: '{period.Status}').");
        }

        var oldStatus = period.Status;
        period.IsLocked = true;
        period.Status = PayrollPeriodStatuses.Locked;
        period.LockedByUserId = actorUserId;
        period.LockedAt = DateTime.UtcNow;

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "Payroll",
            ActionType = "PayrollPeriod_Lock",
            EntityName = nameof(PayrollPeriod),
            EntityId = period.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { Status = oldStatus, IsLocked = false }),
            NewValuesJson = JsonSerializer.Serialize(new { Status = period.Status, IsLocked = true, period.LockedAt }),
            AffectedColumns = "Status,IsLocked,LockedByUserId,LockedAt",
            IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser?.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
