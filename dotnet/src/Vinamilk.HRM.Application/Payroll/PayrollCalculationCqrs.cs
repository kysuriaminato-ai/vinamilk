using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

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
    decimal CustomDeductions = 0m);

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
    string? SplitSummaryNote);

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
    decimal NetSalary);

/// <summary>
/// Động cơ tính lương, BHXH/BHYT/BHTN và Thuế TNCN theo tham số hiệu lực tại ngày cuối kỳ lương.
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

        var actualBase = input.PrecomputedActualBaseSalary ?? Math.Round(
            input.ContractBaseSalary * (input.PayableWorkdays / input.StandardWorkdays),
            0,
            MidpointRounding.AwayFromZero);

        var hourlyRate = input.ContractBaseSalary / (input.StandardWorkdays * 8m);
        var otPay = input.PrecomputedOvertimePay ?? Math.Round(
            hourlyRate * (input.OtHours150 * 1.5m + input.OtHours200 * 2.0m + input.OtHours300 * 3.0m),
            0,
            MidpointRounding.AwayFromZero);

        var gross = actualBase
            + input.PositionAllowance
            + input.OtherAllowance
            + otPay
            + input.KpiBonus
            + input.UnusedLeavePayout
            + input.ExtraTemplateEarnings;

        var siCap = parameter.StatutoryBaseSalary * parameter.SocialInsuranceCapMultiplier;
        var regionalMinWage = parameter.GetRegionalMinWage(input.WageRegion);
        var uiCap = regionalMinWage * parameter.UnemploymentInsuranceCapMultiplier;

        var insContractSalary = input.InsuranceContractSalaryOverride
            ?? (input.ContractBaseSalary + input.PositionAllowance);
        var siBase = Math.Min(insContractSalary, siCap);
        var uiBase = Math.Min(insContractSalary, uiCap);

        var empSi = Math.Round(siBase * parameter.EmployeeSiRate, 0, MidpointRounding.AwayFromZero);
        var empHi = Math.Round(siBase * parameter.EmployeeHiRate, 0, MidpointRounding.AwayFromZero);
        var empUi = Math.Round(uiBase * parameter.EmployeeUiRate, 0, MidpointRounding.AwayFromZero);
        var totalEmpIns = empSi + empHi + empUi;

        var compSi = Math.Round(siBase * parameter.CompanySiRate, 0, MidpointRounding.AwayFromZero);
        var compHi = Math.Round(siBase * parameter.CompanyHiRate, 0, MidpointRounding.AwayFromZero);
        var compUi = Math.Round(uiBase * parameter.CompanyUiRate, 0, MidpointRounding.AwayFromZero);
        var unionFee = input.IsUnionMember
            ? Math.Round(siBase * parameter.CompanyUnionFeeRate, 0, MidpointRounding.AwayFromZero)
            : 0m;

        var selfDeduction = parameter.SelfDeduction;
        var depDeduction = Math.Max(0, input.DependentCount) * parameter.DependentDeduction;
        var taxableIncome = Math.Max(0m, gross - totalEmpIns - Math.Max(0m, input.ExtraTaxExemptAmount));
        var assessableIncome = Math.Max(0m, taxableIncome - selfDeduction - depDeduction);
        var pit = CalculateProgressivePit(assessableIncome, pitBrackets);

        var net = Math.Max(0m, gross - totalEmpIns - pit - input.DisciplineDeduction - Math.Max(0m, input.CustomDeductions));

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
            unionFee,
            selfDeduction,
            depDeduction,
            assessableIncome,
            pit,
            net);
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
        decimal totalOt150,
        decimal totalOt200,
        decimal totalOt300,
        int dependentCount,
        int wageRegion,
        decimal unusedLeavePayout = 0m,
        decimal kpiBonus = 0m,
        decimal disciplineDeduction = 0m,
        string roundingRule = "RoundToDong",
        IReadOnlyDictionary<string, decimal>? taxExemptParamValues = null)
    {
        if (slices == null || slices.Count == 0)
            throw new ArgumentException("Danh sách đoạn hiệu lực hồ sơ lương không được rỗng.", nameof(slices));

        var evaluatedSlices = new List<MidMonthPayrollSliceBreakdown>(slices.Count);
        var specLookup = new Dictionary<string, FormulaComponentSpec>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < slices.Count; i++)
        {
            var slice = slices[i];
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
                extraCommissionOrKpi: i == slices.Count - 1 ? kpiBonus : 0m));
        }

        decimal sumActualBase = evaluatedSlices.Sum(s => s.ActualBaseSalary);
        decimal sumPosAllowance = evaluatedSlices.Sum(s => s.PositionAllowance);
        decimal sumOtherAllowance = evaluatedSlices.Sum(s => s.OtherAllowance);
        decimal sumOtPay = evaluatedSlices.Sum(s => s.OvertimePay);
        decimal sumExtraEarnings = evaluatedSlices.Sum(s => s.ExtraTemplateEarnings);
        decimal sumCustomDeductions = evaluatedSlices.Sum(s => s.CustomDeductions);

        // Aggregate evaluated component lines across segments and enforce monthly TaxExemptLimit cap cleanly
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

        var latestSlice = evaluatedSlices[^1];
        var latestProfile = slices[^1].Profile;
        decimal insContractSalary = latestProfile.InsuranceSalary > 0m
            ? latestProfile.InsuranceSalary
            : (latestSlice.BaseSalary + latestProfile.PositionAllowance);

        var breakdown = Calculate(new PayrollCalculationInput(
            ContractBaseSalary: latestSlice.BaseSalary,
            PositionAllowance: sumPosAllowance,
            OtherAllowance: sumOtherAllowance,
            StandardWorkdays: standardWorkdays,
            PayableWorkdays: totalPayableWorkdays,
            OtHours150: totalOt150,
            OtHours200: totalOt200,
            OtHours300: totalOt300,
            KpiBonus: kpiBonus,
            DisciplineDeduction: disciplineDeduction,
            DependentCount: dependentCount,
            IsUnionMember: latestProfile.IsUnionMember,
            WageRegion: wageRegion,
            UnusedLeavePayout: unusedLeavePayout,
            InsuranceContractSalaryOverride: insContractSalary,
            PrecomputedActualBaseSalary: sumActualBase,
            PrecomputedOvertimePay: sumOtPay,
            ExtraTemplateEarnings: sumExtraEarnings,
            ExtraTaxExemptAmount: totalTaxExempt,
            CustomDeductions: sumCustomDeductions), statutoryContext);

        string? splitNote = evaluatedSlices.Count > 1
            ? "Tách lương theo hiệu lực: " + string.Join("; ", evaluatedSlices.Select(s =>
                $"{s.SegmentStart:dd/MM}-{s.SegmentEnd:dd/MM}: {s.SegmentWorkdays:N2} công, LCB={s.BaseSalary:N0}, Thực={s.ActualBaseSalary:N0}"))
            : null;

        return new MidMonthAggregatedPayrollResult(
            breakdown,
            latestSlice.BaseSalary,
            sumPosAllowance,
            sumOtherAllowance,
            sumExtraEarnings,
            totalTaxExempt,
            sumCustomDeductions,
            insContractSalary,
            evaluatedSlices,
            groupedLines,
            splitNote);
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

public sealed record CalculateMonthlyPayrollCommand(
    int PayrollPeriodId,
    int? EmployeeId = null,
    string? Notes = null) : IRequest<int>;

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

        var periodStartDate = period.StartDate != default
            ? period.StartDate
            : new DateOnly(period.Year, period.Month, 1);
        var periodEndDate = period.EndDate != default
            ? period.EndDate
            : new DateOnly(period.Year, period.Month, DateTime.DaysInMonth(period.Year, period.Month));

        var statutoryContext = await VietnamPayrollEngine.ResolveStatutoryContextAsync(
            _context, periodEndDate, cancellationToken);

        // Nạp tham số miễn thuế & cấu hình phụ thuộc ngày hiệu lực kỳ lương
        var activeParams = await _context.PayrollParameters
            .AsNoTracking()
            .Where(p => p.EffectiveFrom <= periodEndDate && (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= periodStartDate))
            .OrderBy(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var taxExemptParamValues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var prm in activeParams)
            taxExemptParamValues[prm.ParamCode] = prm.ParamValue;

        var periodSettlements = await _context.TerminationLeaveSettlements
            .Where(s => s.TargetPayrollYear == period.Year
                && s.TargetPayrollMonth == period.Month
                && (s.Status == "PushedToPayroll" || s.Status == "Paid"))
            .ToListAsync(cancellationToken);

        var settlementByEmpId = periodSettlements
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PayoutAmount));

        var settlementEmpIds = settlementByEmpId.Keys.ToHashSet();

        var empQuery = _context.Employees
            .Include(e => e.OrganizationUnit)
            .Where(e => (e.WorkStatus != "Terminated" && e.WorkStatus != "Resigned" && e.WorkStatus != "Retired")
                || settlementEmpIds.Contains(e.Id));

        if (request.EmployeeId.HasValue)
            empQuery = empQuery.Where(e => e.Id == request.EmployeeId.Value);

        var employees = await empQuery.ToListAsync(cancellationToken);
        if (request.EmployeeId.HasValue && employees.Count == 0)
            throw new KeyNotFoundException($"Không tìm thấy nhân viên hợp lệ #{request.EmployeeId.Value} để tính lại lương.");

        var employeeIds = employees.Select(e => e.Id).ToHashSet();

        var attendanceMap = await _context.MonthlyAttendanceSummaries
            .Where(a => a.Year == period.Year && a.Month == period.Month && employeeIds.Contains(a.EmployeeId))
            .ToDictionaryAsync(a => a.EmployeeId, cancellationToken);

        // Nạp hồ sơ lương hiệu lực trong kỳ + mẫu lương & công thức
        var activeProfiles = await _context.EmployeeSalaryProfiles
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
            var templates = await _context.SalaryTemplates
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

        Dictionary<int, List<Domain.Entities.Time.DailyAttendance>> dailyAttendanceByEmpId = new();
        if (activeProfiles.Count > 0)
        {
            var empIdsWithProfiles = profilesByEmpId.Keys.ToHashSet();
            var dailyRows = await _context.DailyAttendances
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

        var dependentCounts = await _context.EmployeeDependents
            .Where(d => d.IsTaxDeductible && employeeIds.Contains(d.EmployeeId))
            .GroupBy(d => d.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, cancellationToken);

        // Lấy bản ghi hiện có trong kỳ (để tính chênh lệch so với lần tính trước)
        var allCurrentRecords = await _context.PayrollRecords
            .Where(r => r.PayrollPeriodId == period.Id)
            .ToListAsync(cancellationToken);

        var previousRevisionNetMap = allCurrentRecords.ToDictionary(r => r.EmployeeId, r => r.NetSalary);
        var previousTotalPeriodNet = allCurrentRecords.Sum(r => r.NetSalary);

        // Lấy kỳ lương liền kề trước đó để tính chênh lệch so với kỳ trước & cảnh báo > 20%
        var prevPeriodId = await _context.PayrollPeriods
            .AsNoTracking()
            .Where(p => p.Year < period.Year || (p.Year == period.Year && p.Month < period.Month))
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var prevPeriodNetMap = prevPeriodId.HasValue
            ? await _context.PayrollRecords
                .AsNoTracking()
                .Where(r => r.PayrollPeriodId == prevPeriodId.Value && employeeIds.Contains(r.EmployeeId))
                .ToDictionaryAsync(r => r.EmployeeId, r => r.NetSalary, cancellationToken)
            : new Dictionary<int, decimal>();

        var recordsToRemove = request.EmployeeId.HasValue
            ? allCurrentRecords.Where(r => r.EmployeeId == request.EmployeeId.Value).ToList()
            : allCurrentRecords;
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

        var snapshotItems = new List<object>();
        int highVarianceCount = 0;
        var newlyAdded = new List<PayrollRecord>();

        foreach (var emp in employees)
        {
            attendanceMap.TryGetValue(emp.Id, out var att);
            dependentCounts.TryGetValue(emp.Id, out var deps);
            int wageRegion = emp.WageRegion ?? emp.OrganizationUnit?.WageRegion ?? 1;

            decimal payableWorkdays = att?.TotalPayableWorkdays ?? period.StandardWorkdays;
            decimal ot150 = att?.TotalOtHours150 ?? 0m;
            decimal ot200 = att?.TotalOtHours200 ?? 0m;
            decimal ot300 = att?.TotalOtHours300 ?? 0m;
            decimal nightHours = att?.TotalNightHours ?? 0m;

            decimal unusedLeavePayout = settlementByEmpId.TryGetValue(emp.Id, out var payoutAmt) ? payoutAmt : 0m;

            PayrollCalculationBreakdown breakdown;
            decimal contractBaseSalary;
            decimal posAllowance;
            decimal otherAllowance;
            decimal taxExemptAmount = 0m;
            decimal disciplineDeduction = 0m;
            string? splitNote = null;
            IReadOnlyList<EvaluatedSalaryLineDto>? evaluatedLines = null;

            if (profilesByEmpId.TryGetValue(emp.Id, out var empProfiles) && empProfiles.Count > 0)
            {
                dailyAttendanceByEmpId.TryGetValue(emp.Id, out var dailyAtts);
                var slices = MidMonthSalarySplitter.BuildPeriodSlices(
                    periodStartDate,
                    periodEndDate,
                    payableWorkdays,
                    ot150,
                    ot200,
                    ot300,
                    nightHours,
                    empProfiles,
                    templateComponentMap,
                    dailyAtts);

                var lastTplId = slices[^1].Profile.SalaryTemplateId ?? 0;
                var roundingRule = templateRoundingMap.TryGetValue(lastTplId, out var rRule) ? rRule : "RoundToDong";

                var agg = VietnamPayrollEngine.CalculateFromMidMonthSlices(
                    slices,
                    statutoryContext,
                    period.StandardWorkdays,
                    payableWorkdays,
                    ot150,
                    ot200,
                    ot300,
                    deps,
                    wageRegion,
                    unusedLeavePayout: unusedLeavePayout,
                    kpiBonus: 0m,
                    disciplineDeduction: 0m,
                    roundingRule: roundingRule,
                    taxExemptParamValues: taxExemptParamValues);

                breakdown = agg.Breakdown;
                contractBaseSalary = agg.EffectiveContractBaseSalary;
                posAllowance = agg.EffectivePositionAllowance;
                otherAllowance = agg.EffectiveOtherAllowance + agg.ExtraTemplateEarnings;
                taxExemptAmount = agg.TaxExemptAmount;
                disciplineDeduction = agg.CustomDeductions;
                splitNote = agg.SplitSummaryNote;
                evaluatedLines = agg.AggregatedLines;
            }
            else
            {
                contractBaseSalary = emp.BaseSalary;
                posAllowance = emp.PositionAllowance;
                otherAllowance = emp.OtherAllowance;

                breakdown = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
                    emp.BaseSalary,
                    emp.PositionAllowance,
                    emp.OtherAllowance,
                    period.StandardWorkdays,
                    payableWorkdays,
                    ot150,
                    ot200,
                    ot300,
                    0m,
                    0m,
                    deps,
                    emp.IsUnionMember,
                    wageRegion,
                    unusedLeavePayout), statutoryContext);
            }

            var pitSlices = VietnamPayrollEngine.CalculateProgressivePitBreakdown(
                breakdown.AssessableIncome, statutoryContext.PitBrackets);

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
                highVariance = Math.Abs(variancePct.Value) > 20m;
                if (highVariance)
                    highVarianceCount++;
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
                OtHours150 = ot150,
                OtHours200 = ot200,
                OtHours300 = ot300,
                TotalOtHours = ot150 + ot200 + ot300,
                ContractBaseSalary = contractBaseSalary,
                ActualBaseSalary = breakdown.ActualBaseSalary,
                PositionAllowance = posAllowance,
                OtherAllowance = otherAllowance,
                OvertimePay = breakdown.OvertimePay,
                KpiBonus = 0m,
                UnusedLeavePayout = unusedLeavePayout,
                GrossIncome = breakdown.GrossIncome,
                InsuranceSalaryBase = breakdown.SocialInsuranceBase,
                UnemploymentInsuranceBase = breakdown.UnemploymentInsuranceBase,
                EmployeeSocialInsurance = breakdown.EmployeeSocialInsurance,
                EmployeeHealthInsurance = breakdown.EmployeeHealthInsurance,
                EmployeeUnemploymentInsurance = breakdown.EmployeeUnemploymentInsurance,
                TotalEmployeeInsurance = breakdown.TotalEmployeeInsurance,
                CompanySocialInsurance = breakdown.CompanySocialInsurance,
                CompanyHealthInsurance = breakdown.CompanyHealthInsurance,
                CompanyUnemploymentInsurance = breakdown.CompanyUnemploymentInsurance,
                CompanyUnionFee = breakdown.CompanyUnionFee,
                DependentCount = deps,
                SelfDeductionAmount = breakdown.SelfDeductionAmount,
                DependentDeductionAmount = breakdown.DependentDeductionAmount,
                TaxableIncome = Math.Max(0m, breakdown.GrossIncome - breakdown.TotalEmployeeInsurance - taxExemptAmount),
                AssessableIncome = breakdown.AssessableIncome,
                PersonalIncomeTax = breakdown.PersonalIncomeTax,
                PitBracketsJson = JsonSerializer.Serialize(pitSlices),
                DisciplineDeduction = disciplineDeduction,
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

            newlyAdded.Add(rec);
            _context.PayrollRecords.Add(rec);

            snapshotItems.Add(new
            {
                emp.EmployeeCode,
                emp.FullName,
                NewNet = breakdown.NetSalary,
                PreviousRevisionNet = prevRevNet,
                DeltaFromPreviousRevision = prevRevNet.HasValue ? breakdown.NetSalary - prevRevNet.Value : 0m,
                PreviousPeriodNet = prevPeriodNet,
                VariancePercent = variancePct,
                HighVarianceWarning = highVariance
            });
        }

        // Tổng hợp quỹ kỳ lương sau khi cập nhật
        var unchangedRecords = request.EmployeeId.HasValue
            ? allCurrentRecords.Where(r => r.EmployeeId != request.EmployeeId.Value).ToList()
            : new List<PayrollRecord>();

        var combinedGross = unchangedRecords.Sum(r => r.GrossIncome) + newlyAdded.Sum(r => r.GrossIncome);
        var combinedPit = unchangedRecords.Sum(r => r.PersonalIncomeTax) + newlyAdded.Sum(r => r.PersonalIncomeTax);
        var combinedNet = unchangedRecords.Sum(r => r.NetSalary) + newlyAdded.Sum(r => r.NetSalary);
        var totalRevDelta = previousRevisionNetMap.Count > 0 ? combinedNet - previousTotalPeriodNet : 0m;

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
            OldValuesJson = JsonSerializer.Serialize(new { Revision = period.CalculationVersion - 1, PreviousTotalNet = previousTotalPeriodNet }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                period.PeriodCode,
                Revision = period.CalculationVersion,
                Scope = revision.Scope,
                AffectedEmployees = newlyAdded.Count,
                TotalGross = combinedGross,
                TotalNet = combinedNet,
                NetDeltaFromPreviousRevision = totalRevDelta,
                HighVarianceWarningCount = highVarianceCount
            }),
            AffectedColumns = "Status,CalculationVersion,CalculatedByUserId,CalculatedAt",
            IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser?.UserAgent ?? "PayrollEngine",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return employees.Count;
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
