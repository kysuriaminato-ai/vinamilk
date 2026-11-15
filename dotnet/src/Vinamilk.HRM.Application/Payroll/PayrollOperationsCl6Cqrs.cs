using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Application.Payroll;

public sealed record CheckPayrollAttendanceLockStatusQuery(int Year, int Month)
    : IRequest<AttendanceLockStatusDto>;

public sealed class CheckPayrollAttendanceLockStatusQueryHandler
    : IRequestHandler<CheckPayrollAttendanceLockStatusQuery, AttendanceLockStatusDto>
{
    private readonly IApplicationDbContext _context;

    public CheckPayrollAttendanceLockStatusQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<AttendanceLockStatusDto> Handle(CheckPayrollAttendanceLockStatusQuery request, CancellationToken cancellationToken)
    {
        var emps = await _context.Employees
            .AsNoTracking()
            .Where(e => e.WorkStatus != "Terminated" && e.WorkStatus != "Resigned" && e.WorkStatus != "Retired")
            .Select(e => new { e.Id, e.EmployeeCode })
            .ToListAsync(cancellationToken);

        return await PayrollPeriodEvaluator.CheckAttendanceLockAsync(
            _context,
            request.Year,
            request.Month,
            emps.Select(e => e.Id).ToList(),
            emps.ToDictionary(e => e.Id, e => e.EmployeeCode),
            cancellationToken);
    }
}

public sealed record SimulatedPayrollEmployeeRowDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string DepartmentName,
    decimal PayableWorkdays,
    decimal StandardWorkdays,
    decimal ActualBaseSalary,
    decimal AllowancesTotal,
    decimal OvertimePay,
    decimal OtNightExtraPay,
    decimal OtTaxExemptAmount,
    decimal BonusesTotal,
    decimal GrossIncome,
    decimal TotalEmployeeInsurance,
    decimal PersonalIncomeTax,
    decimal OtherDeductionsTotal,
    decimal NetSalary,
    decimal? PreviousPeriodNetSalary,
    decimal NetDeltaVsPreviousPeriod,
    decimal? VariancePercentVsPreviousPeriod,
    bool HasHighVarianceWarning,
    decimal TotalCompanyCost,
    string TaxResidencyMode,
    bool IsNetToGross,
    decimal? TargetNetSalary,
    IReadOnlyList<CalculationTraceStepDto> TraceSteps);

public sealed record PayrollSimulationResultDto(
    int PayrollPeriodId,
    string PeriodCode,
    string PeriodTitle,
    string PeriodType,
    int Year,
    int Month,
    AttendanceLockStatusDto AttendanceLockStatus,
    int EmployeeCount,
    decimal TotalSimulatedGross,
    decimal TotalSimulatedPit,
    decimal TotalSimulatedNet,
    decimal TotalSimulatedCompanyCost,
    decimal PreviousPeriodTotalNet,
    decimal TotalNetDeltaVsPreviousPeriod,
    int HighVarianceCount,
    IReadOnlyList<SimulatedPayrollEmployeeRowDto> Employees);

public sealed record SimulatePayrollPeriodQuery(int PayrollPeriodId, int? EmployeeId = null)
    : IRequest<PayrollSimulationResultDto>;

public sealed class SimulatePayrollPeriodQueryHandler
    : IRequestHandler<SimulatePayrollPeriodQuery, PayrollSimulationResultDto>
{
    private readonly IApplicationDbContext _context;

    public SimulatePayrollPeriodQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PayrollSimulationResultDto> Handle(SimulatePayrollPeriodQuery request, CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        var eval = await PayrollPeriodEvaluator.EvaluatePeriodAsync(
            _context, period, request.EmployeeId, asNoTracking: true, cancellationToken);

        var rows = eval.EvaluatedEmployees.Select(item =>
        {
            var r = item.Record;
            decimal bonuses = r.KpiBonus + r.CommissionBonus + r.ThirteenthMonthBonus + r.UnusedLeavePayout + r.RetroEarningAmount;
            decimal otherDed = r.DisciplineDeduction + r.SalaryAdvanceDeduction + r.LoanInstallmentDeduction + r.RetroDeductionAmount + r.EmployeeUnionFee;
            decimal prevNet = item.PreviousPeriodNet ?? 0m;
            decimal netDelta = item.PreviousPeriodNet.HasValue ? r.NetSalary - prevNet : 0m;

            return new SimulatedPayrollEmployeeRowDto(
                item.Employee.Id,
                item.Employee.EmployeeCode,
                item.Employee.FullName,
                item.Employee.OrganizationUnit?.UnitName ?? string.Empty,
                r.PayableWorkdays,
                r.StandardWorkdays,
                r.ActualBaseSalary,
                r.PositionAllowance + r.OtherAllowance,
                r.OvertimePay,
                r.OtNightExtraPay,
                r.OtTaxExemptAmount,
                bonuses,
                r.GrossIncome,
                r.TotalEmployeeInsurance,
                r.PersonalIncomeTax,
                otherDed,
                r.NetSalary,
                item.PreviousPeriodNet,
                netDelta,
                item.VariancePercent,
                item.HasHighVarianceWarning,
                r.TotalCompanyCost,
                r.TaxResidencyMode,
                r.IsNetToGross,
                r.TargetNetSalary,
                item.TraceSteps);
        }).ToList();

        decimal prevTotalNet = rows.Where(x => x.PreviousPeriodNetSalary.HasValue).Sum(x => x.PreviousPeriodNetSalary!.Value);
        decimal totalNet = rows.Sum(x => x.NetSalary);

        return new PayrollSimulationResultDto(
            period.Id,
            period.PeriodCode,
            period.PeriodTitle,
            period.PeriodType,
            period.Year,
            period.Month,
            eval.AttendanceLockStatus,
            rows.Count,
            rows.Sum(x => x.GrossIncome),
            rows.Sum(x => x.PersonalIncomeTax),
            totalNet,
            rows.Sum(x => x.TotalCompanyCost),
            prevTotalNet,
            totalNet - prevTotalNet,
            rows.Count(x => x.HasHighVarianceWarning),
            rows);
    }
}

public sealed record PayrollRecordTraceDto(
    long PayrollRecordId,
    int PayrollPeriodId,
    string PeriodCode,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    decimal ContractBaseSalary,
    decimal ActualBaseSalary,
    decimal GrossIncome,
    decimal TotalEmployeeInsurance,
    decimal PersonalIncomeTax,
    decimal NetSalary,
    decimal TotalCompanyCost,
    string TaxResidencyMode,
    bool UsedCommitment08,
    bool IsNetToGross,
    decimal? TargetNetSalary,
    decimal OtNightExtraPay,
    decimal OtTaxExemptAmount,
    IReadOnlyList<CalculationTraceStepDto> Steps,
    IReadOnlyList<PitBracketTaxSliceDto> PitBrackets,
    bool IsRecomputedReference = false,
    string? ReferenceNotice = null,
    decimal SelfDeductionAmount = 0m,
    decimal DependentDeductionAmount = 0m,
    int DependentCount = 0,
    decimal TaxableIncome = 0m,
    decimal AssessableIncome = 0m,
    decimal EmployeeSocialInsurance = 0m,
    decimal EmployeeHealthInsurance = 0m,
    decimal EmployeeUnemploymentInsurance = 0m,
    decimal PositionAllowance = 0m,
    decimal OtherAllowance = 0m,
    decimal OvertimePay = 0m,
    decimal KpiBonus = 0m,
    decimal DisciplineDeduction = 0m,
    decimal StandardWorkdays = 0m,
    decimal PayableWorkdays = 0m);

public sealed record GetPayrollRecordTraceQuery(long PayrollRecordId) : IRequest<PayrollRecordTraceDto>;

public static class PayrollTraceHelper
{
    public static async Task<(
        IReadOnlyList<CalculationTraceStepDto> Steps,
        IReadOnlyList<PitBracketTaxSliceDto> PitSlices,
        bool IsRecomputedReference,
        string? ReferenceNotice)> ResolveOrRecomputeAsync(
        IApplicationDbContext context,
        PayrollRecord rec,
        DateOnly periodEndDate,
        int wageRegion,
        CancellationToken cancellationToken = default)
    {
        var steps = !string.IsNullOrWhiteSpace(rec.CalculationTraceJson)
            ? JsonSerializer.Deserialize<List<CalculationTraceStepDto>>(rec.CalculationTraceJson) ?? new List<CalculationTraceStepDto>()
            : new List<CalculationTraceStepDto>();

        var brackets = !string.IsNullOrWhiteSpace(rec.PitBracketsJson)
            ? JsonSerializer.Deserialize<List<PitBracketTaxSliceDto>>(rec.PitBracketsJson) ?? new List<PitBracketTaxSliceDto>()
            : new List<PitBracketTaxSliceDto>();

        bool isStaleStatutory = periodEndDate >= new DateOnly(2026, 1, 1)
            && rec.SelfDeductionAmount == 11_000_000m
            && string.Equals(rec.TaxResidencyMode, "ResidentStandard", StringComparison.OrdinalIgnoreCase);

        if (steps.Count >= 15 && !isStaleStatutory)
        {
            return (steps, brackets, false, null);
        }

        var statutoryContext = await VietnamPayrollEngine.TryResolveStatutoryContextAsync(context, periodEndDate, cancellationToken)
            ?? BuildFallbackStatutoryContext(rec, periodEndDate);

        decimal stdDays = rec.StandardWorkdays > 0m ? rec.StandardWorkdays : 22m;
        var input = new PayrollCalculationInput(
            ContractBaseSalary: rec.ContractBaseSalary,
            StandardWorkdays: stdDays,
            PayableWorkdays: rec.PayableWorkdays,
            PositionAllowance: rec.PositionAllowance,
            OtherAllowance: rec.OtherAllowance,
            OtHours150: rec.OtHours150,
            OtHours200: rec.OtHours200,
            OtHours300: rec.OtHours300,
            KpiBonus: rec.KpiBonus,
            DisciplineDeduction: rec.DisciplineDeduction,
            DependentCount: rec.DependentCount,
            IsUnionMember: rec.EmployeeUnionFee > 0m,
            WageRegion: wageRegion > 0 ? wageRegion : 1,
            UnusedLeavePayout: rec.UnusedLeavePayout,
            InsuranceContractSalaryOverride: rec.InsuranceSalaryBase > 0m ? rec.InsuranceSalaryBase : null,
            PrecomputedActualBaseSalary: rec.ActualBaseSalary > 0m ? rec.ActualBaseSalary : null,
            PrecomputedOvertimePay: rec.OvertimePay > 0m ? rec.OvertimePay : null,
            CommissionBonus: rec.CommissionBonus,
            ThirteenthMonthBonus: rec.ThirteenthMonthBonus,
            RetroEarningAmount: rec.RetroEarningAmount,
            RetroDeductionAmount: rec.RetroDeductionAmount,
            SalaryAdvanceDeduction: rec.SalaryAdvanceDeduction,
            LoanInstallmentDeduction: rec.LoanInstallmentDeduction,
            TaxResidencyMode: rec.TaxResidencyMode,
            HasPitCommitment08: rec.UsedCommitment08,
            PrecomputedOtNightExtraPay: rec.OtNightExtraPay,
            PrecomputedOtTaxExemptAmount: rec.OtTaxExemptAmount);

        var breakdown = VietnamPayrollEngine.Calculate(input, statutoryContext);
        var recomputedPit = string.Equals(breakdown.TaxResidencyMode, "ResidentStandard", StringComparison.OrdinalIgnoreCase)
            ? VietnamPayrollEngine.CalculateProgressivePitBreakdown(breakdown.AssessableIncome, statutoryContext.PitBrackets)
            : new List<PitBracketTaxSliceDto>();

        var netToGross = rec.IsNetToGross && rec.TargetNetSalary.HasValue
            ? new NetToGrossSolution(rec.ContractBaseSalary, breakdown, rec.TargetNetSalary.Value, rec.NetSalary, 0m, 0, true)
            : null;

        var recomputedSteps = VietnamPayrollEngine.BuildCalculationTrace(
            input,
            breakdown,
            statutoryContext.Parameter,
            recomputedPit,
            netToGross,
            "Tính lại để tham chiếu từ bản ghi lương");

        return (recomputedSteps, recomputedPit, true, "Tính lại để tham chiếu");
    }

    private static PayrollStatutoryContext BuildFallbackStatutoryContext(PayrollRecord rec, DateOnly periodEndDate)
    {
        bool is2026 = periodEndDate >= new DateOnly(2026, 1, 1);
        var param = new PayrollStatutoryParameter
        {
            EffectiveFrom = periodEndDate,
            StatutoryBaseSalary = periodEndDate >= new DateOnly(2026, 7, 1) ? 2_530_000m : 2_340_000m,
            SocialInsuranceCapMultiplier = 20m,
            UnemploymentInsuranceCapMultiplier = 20m,
            MinWageRegion1 = is2026 ? 5_310_000m : 4_960_000m,
            MinWageRegion2 = is2026 ? 4_730_000m : 4_410_000m,
            MinWageRegion3 = is2026 ? 4_140_000m : 3_860_000m,
            MinWageRegion4 = is2026 ? 3_700_000m : 3_450_000m,
            SelfDeduction = rec.SelfDeductionAmount > 0m ? rec.SelfDeductionAmount : (is2026 ? 15_500_000m : 11_000_000m),
            DependentDeduction = is2026 ? 6_200_000m : 4_400_000m,
            EmployeeSiRate = 0.08m,
            EmployeeHiRate = 0.015m,
            EmployeeUiRate = 0.01m,
            CompanySiRate = 0.17m,
            CompanyHiRate = 0.03m,
            CompanyUiRate = 0.01m,
            CompanyOccupationalAccidentRate = 0.005m,
            CompanyUnionFeeRate = 0.02m,
            EmployeeUnionFeeRate = 0.01m,
            LegalReference = is2026
                ? "NQ 110/2025/UBTVQH15 & Luật Thuế TNCN 2025 (Tham chiếu)"
                : "NQ 954/2020/UBTVQH14 & NĐ 73/2024/NĐ-CP (Tham chiếu)"
        };

        IReadOnlyList<PitBracket> brackets = is2026
            ? new List<PitBracket>
            {
                new() { BracketLevel = 1, LowerBound = 0m, UpperBound = 10_000_000m, TaxRate = 0.05m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 2, LowerBound = 10_000_000m, UpperBound = 30_000_000m, TaxRate = 0.10m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 3, LowerBound = 30_000_000m, UpperBound = 60_000_000m, TaxRate = 0.20m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 4, LowerBound = 60_000_000m, UpperBound = 100_000_000m, TaxRate = 0.30m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 5, LowerBound = 100_000_000m, UpperBound = null, TaxRate = 0.35m, EffectiveFrom = periodEndDate }
            }
            : new List<PitBracket>
            {
                new() { BracketLevel = 1, LowerBound = 0m, UpperBound = 5_000_000m, TaxRate = 0.05m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 2, LowerBound = 5_000_000m, UpperBound = 10_000_000m, TaxRate = 0.10m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 3, LowerBound = 10_000_000m, UpperBound = 18_000_000m, TaxRate = 0.15m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 4, LowerBound = 18_000_000m, UpperBound = 32_000_000m, TaxRate = 0.20m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 5, LowerBound = 32_000_000m, UpperBound = 52_000_000m, TaxRate = 0.25m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 6, LowerBound = 52_000_000m, UpperBound = 80_000_000m, TaxRate = 0.30m, EffectiveFrom = periodEndDate },
                new() { BracketLevel = 7, LowerBound = 80_000_000m, UpperBound = null, TaxRate = 0.35m, EffectiveFrom = periodEndDate }
            };

        return new PayrollStatutoryContext(param, brackets);
    }
}

public sealed class GetPayrollRecordTraceQueryHandler : IRequestHandler<GetPayrollRecordTraceQuery, PayrollRecordTraceDto>
{
    private readonly IApplicationDbContext _context;

    public GetPayrollRecordTraceQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PayrollRecordTraceDto> Handle(GetPayrollRecordTraceQuery request, CancellationToken cancellationToken)
    {
        var rec = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Employee)
            .Include(r => r.OrganizationUnit)
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy bảng lương chi tiết #{request.PayrollRecordId}.");

        var wageRegion = rec.Employee.WageRegion ?? rec.OrganizationUnit.WageRegion;
        var (steps, brackets, isRecomputed, notice) = await PayrollTraceHelper.ResolveOrRecomputeAsync(
            _context,
            rec,
            rec.PayrollPeriod.EndDate,
            wageRegion,
            cancellationToken);

        return new PayrollRecordTraceDto(
            rec.Id,
            rec.PayrollPeriodId,
            rec.PayrollPeriod.PeriodCode,
            rec.Employee.EmployeeCode,
            rec.Employee.FullName,
            rec.OrganizationUnit.UnitName,
            rec.ContractBaseSalary,
            rec.ActualBaseSalary,
            rec.GrossIncome,
            rec.TotalEmployeeInsurance,
            rec.PersonalIncomeTax,
            rec.NetSalary,
            rec.TotalCompanyCost,
            rec.TaxResidencyMode,
            rec.UsedCommitment08,
            rec.IsNetToGross,
            rec.TargetNetSalary,
            rec.OtNightExtraPay,
            rec.OtTaxExemptAmount,
            steps,
            brackets,
            isRecomputed,
            notice,
            rec.SelfDeductionAmount,
            rec.DependentDeductionAmount,
            rec.DependentCount,
            rec.TaxableIncome,
            rec.AssessableIncome,
            rec.EmployeeSocialInsurance,
            rec.EmployeeHealthInsurance,
            rec.EmployeeUnemploymentInsurance,
            rec.PositionAllowance,
            rec.OtherAllowance,
            rec.OvertimePay,
            rec.KpiBonus,
            rec.DisciplineDeduction,
            rec.StandardWorkdays,
            rec.PayableWorkdays);
    }
}

public sealed record SolveNetToGrossQuery(NetToGrossInput Input, DateOnly? EffectiveDate = null)
    : IRequest<NetToGrossSolution>;

public sealed class SolveNetToGrossQueryHandler : IRequestHandler<SolveNetToGrossQuery, NetToGrossSolution>
{
    private readonly IApplicationDbContext _context;

    public SolveNetToGrossQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<NetToGrossSolution> Handle(SolveNetToGrossQuery request, CancellationToken cancellationToken)
    {
        var effDate = request.EffectiveDate ?? DateOnly.FromDateTime(DateTime.Today);
        var ctx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(_context, effDate, cancellationToken);
        return VietnamPayrollEngine.SolveGrossFromNet(request.Input, ctx);
    }
}

public sealed record AdhocPayrollCalculatorInput(
    decimal ContractBaseSalary = 20_000_000m,
    decimal PositionAllowance = 0m,
    decimal OtherAllowance = 0m,
    decimal StandardWorkdays = 22m,
    decimal PayableWorkdays = 22m,
    decimal OtHours150 = 0m,
    decimal OtHours200 = 0m,
    decimal OtHours300 = 0m,
    decimal OtHours150Night = 0m,
    decimal OtHours200Night = 0m,
    decimal OtHours300Night = 0m,
    decimal RegularNightHours = 0m,
    decimal KpiBonus = 0m,
    decimal CommissionBonus = 0m,
    decimal DisciplineDeduction = 0m,
    decimal ExtraTaxExemptAmount = 0m,
    int DependentCount = 0,
    bool IsUnionMember = true,
    int WageRegion = 1,
    decimal? InsuranceSalaryOverride = null,
    DateOnly? EffectiveDate = null,
    string TaxResidencyMode = "ResidentStandard",
    bool HasPitCommitment08 = false,
    bool IsNetToGross = false,
    decimal TargetNetSalary = 0m);

public sealed record AdhocPayrollCalculatorResultDto(
    DateOnly EffectiveDate,
    PayrollStatutoryParameter StatutoryParameter,
    PayrollCalculationBreakdown Breakdown,
    PayrollRecordTraceDto Trace,
    NetToGrossSolution? NetToGross = null);

public sealed record CalculateAdhocPayrollQuery(AdhocPayrollCalculatorInput Input)
    : IRequest<AdhocPayrollCalculatorResultDto>;

public sealed class CalculateAdhocPayrollQueryHandler : IRequestHandler<CalculateAdhocPayrollQuery, AdhocPayrollCalculatorResultDto>
{
    private readonly IApplicationDbContext _context;

    public CalculateAdhocPayrollQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<AdhocPayrollCalculatorResultDto> Handle(CalculateAdhocPayrollQuery request, CancellationToken cancellationToken)
    {
        var req = request.Input;
        var effDate = req.EffectiveDate ?? DateOnly.FromDateTime(DateTime.Today);
        var stdDays = req.StandardWorkdays > 0m ? req.StandardWorkdays : 22m;

        var ctx = await VietnamPayrollEngine.TryResolveStatutoryContextAsync(_context, effDate, cancellationToken)
            ?? throw new InvalidOperationException($"Chưa cấu hình tham số luật lao động/thuế TNCN có hiệu lực tại ngày {effDate:dd/MM/yyyy}.");

        NetToGrossSolution? n2gSol = null;
        PayrollCalculationInput calcInput;

        if (req.IsNetToGross && req.TargetNetSalary > 0m)
        {
            var n2gInput = new NetToGrossInput(
                TargetNetSalary: req.TargetNetSalary,
                PositionAllowance: req.PositionAllowance,
                OtherAllowance: req.OtherAllowance,
                StandardWorkdays: stdDays,
                PayableWorkdays: req.PayableWorkdays,
                OtHours150: req.OtHours150,
                OtHours200: req.OtHours200,
                OtHours300: req.OtHours300,
                KpiBonus: req.KpiBonus,
                CommissionBonus: req.CommissionBonus,
                DisciplineDeduction: req.DisciplineDeduction,
                DependentCount: req.DependentCount,
                IsUnionMember: req.IsUnionMember,
                WageRegion: req.WageRegion,
                FixedInsuranceContractSalary: req.InsuranceSalaryOverride,
                TaxResidencyMode: req.TaxResidencyMode,
                HasPitCommitment08: req.HasPitCommitment08,
                OtHours150Night: req.OtHours150Night,
                OtHours200Night: req.OtHours200Night,
                OtHours300Night: req.OtHours300Night,
                RegularNightHours: req.RegularNightHours,
                ExtraTaxExemptAmount: req.ExtraTaxExemptAmount);

            n2gSol = VietnamPayrollEngine.SolveGrossFromNet(n2gInput, ctx);
            calcInput = new PayrollCalculationInput(
                ContractBaseSalary: n2gSol.SolvedContractBaseSalary,
                StandardWorkdays: stdDays,
                PayableWorkdays: req.PayableWorkdays,
                PositionAllowance: req.PositionAllowance,
                OtherAllowance: req.OtherAllowance,
                OtHours150: req.OtHours150,
                OtHours200: req.OtHours200,
                OtHours300: req.OtHours300,
                KpiBonus: req.KpiBonus,
                DisciplineDeduction: req.DisciplineDeduction,
                DependentCount: req.DependentCount,
                IsUnionMember: req.IsUnionMember,
                WageRegion: req.WageRegion,
                InsuranceContractSalaryOverride: req.InsuranceSalaryOverride,
                ExtraTaxExemptAmount: req.ExtraTaxExemptAmount,
                OtHours150Night: req.OtHours150Night,
                OtHours200Night: req.OtHours200Night,
                OtHours300Night: req.OtHours300Night,
                RegularNightHours: req.RegularNightHours,
                CommissionBonus: req.CommissionBonus,
                TaxResidencyMode: req.TaxResidencyMode,
                HasPitCommitment08: req.HasPitCommitment08);
        }
        else
        {
            calcInput = new PayrollCalculationInput(
                ContractBaseSalary: req.ContractBaseSalary,
                StandardWorkdays: stdDays,
                PayableWorkdays: req.PayableWorkdays,
                PositionAllowance: req.PositionAllowance,
                OtherAllowance: req.OtherAllowance,
                OtHours150: req.OtHours150,
                OtHours200: req.OtHours200,
                OtHours300: req.OtHours300,
                KpiBonus: req.KpiBonus,
                DisciplineDeduction: req.DisciplineDeduction,
                DependentCount: req.DependentCount,
                IsUnionMember: req.IsUnionMember,
                WageRegion: req.WageRegion,
                InsuranceContractSalaryOverride: req.InsuranceSalaryOverride,
                ExtraTaxExemptAmount: req.ExtraTaxExemptAmount,
                OtHours150Night: req.OtHours150Night,
                OtHours200Night: req.OtHours200Night,
                OtHours300Night: req.OtHours300Night,
                RegularNightHours: req.RegularNightHours,
                CommissionBonus: req.CommissionBonus,
                TaxResidencyMode: req.TaxResidencyMode,
                HasPitCommitment08: req.HasPitCommitment08);
        }

        var breakdown = VietnamPayrollEngine.Calculate(calcInput, ctx);
        var pitSlices = string.Equals(breakdown.TaxResidencyMode, "ResidentStandard", StringComparison.OrdinalIgnoreCase)
            ? VietnamPayrollEngine.CalculateProgressivePitBreakdown(breakdown.AssessableIncome, ctx.PitBrackets)
            : new List<PitBracketTaxSliceDto>();

        var steps = VietnamPayrollEngine.BuildCalculationTrace(
            calcInput,
            breakdown,
            ctx.Parameter,
            pitSlices,
            n2gSol,
            $"Máy tính lương ad-hoc (hiệu lực {effDate:dd/MM/yyyy})");

        var trace = new PayrollRecordTraceDto(
            PayrollRecordId: 0,
            PayrollPeriodId: 0,
            PeriodCode: $"CALC-{effDate:yyyy-MM}",
            EmployeeCode: "ADHOC",
            EmployeeName: req.IsNetToGross ? "Tính thử Net → Gross" : "Tính thử Gross → Net",
            DepartmentName: $"Vùng BHTN {req.WageRegion}",
            ContractBaseSalary: calcInput.ContractBaseSalary,
            ActualBaseSalary: breakdown.ActualBaseSalary,
            GrossIncome: breakdown.GrossIncome,
            TotalEmployeeInsurance: breakdown.TotalEmployeeInsurance,
            PersonalIncomeTax: breakdown.PersonalIncomeTax,
            NetSalary: breakdown.NetSalary,
            TotalCompanyCost: breakdown.TotalCompanyCost,
            TaxResidencyMode: breakdown.TaxResidencyMode,
            UsedCommitment08: breakdown.UsedCommitment08,
            IsNetToGross: req.IsNetToGross,
            TargetNetSalary: req.IsNetToGross ? req.TargetNetSalary : null,
            OtNightExtraPay: breakdown.OtNightExtraPay,
            OtTaxExemptAmount: breakdown.OtTaxExemptAmount,
            Steps: steps,
            PitBrackets: pitSlices,
            IsRecomputedReference: false,
            ReferenceNotice: null,
            SelfDeductionAmount: breakdown.SelfDeductionAmount,
            DependentDeductionAmount: breakdown.DependentDeductionAmount,
            DependentCount: req.DependentCount,
            TaxableIncome: breakdown.TaxableIncome,
            AssessableIncome: breakdown.AssessableIncome,
            EmployeeSocialInsurance: breakdown.EmployeeSocialInsurance,
            EmployeeHealthInsurance: breakdown.EmployeeHealthInsurance,
            EmployeeUnemploymentInsurance: breakdown.EmployeeUnemploymentInsurance,
            PositionAllowance: req.PositionAllowance,
            OtherAllowance: req.OtherAllowance,
            OvertimePay: breakdown.OvertimePay,
            KpiBonus: req.KpiBonus + req.CommissionBonus,
            DisciplineDeduction: req.DisciplineDeduction,
            StandardWorkdays: stdDays,
            PayableWorkdays: req.PayableWorkdays);

        return new AdhocPayrollCalculatorResultDto(effDate, ctx.Parameter, breakdown, trace, n2gSol);
    }
}

public sealed record SalaryAdvanceRowDto(
    int Id,
    string AdvanceCode,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly AdvanceDate,
    int TargetYear,
    int TargetMonth,
    decimal Amount,
    string Reason,
    string Status,
    int? DeductedInPayrollPeriodId);

public sealed record EmployeeLoanRowDto(
    int Id,
    string LoanCode,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    decimal PrincipalAmount,
    decimal RemainingPrincipal,
    int TermMonths,
    decimal MonthlyInstallmentAmount,
    int StartYear,
    int StartMonth,
    string Reason,
    string Status,
    int PaidInstallments,
    int PendingInstallments);

public sealed record PayrollAdjustmentRowDto(
    int Id,
    string AdjustmentCode,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string AdjustmentType,
    int TargetYear,
    int TargetMonth,
    decimal Amount,
    int? SourceLockedPeriodId,
    string? SourceLockedPeriodCode,
    string Reason,
    string Status,
    int? AppliedInPayrollPeriodId,
    bool RecalculateTaxInSourcePeriod = false,
    decimal SourcePeriodTaxDelta = 0m,
    string? TaxCalculationNote = null);

public sealed record PayrollOperationsOverviewDto(
    IReadOnlyList<SimpleEmployeeLookupDto> Employees,
    IReadOnlyList<PayrollPeriodRowDto> Periods,
    IReadOnlyList<PayrollPeriodRowDto> LockedPeriods,
    IReadOnlyList<SalaryAdvanceRowDto> Advances,
    IReadOnlyList<EmployeeLoanRowDto> Loans,
    IReadOnlyList<PayrollAdjustmentRowDto> Adjustments);

public sealed record GetPayrollOperationsOverviewQuery() : IRequest<PayrollOperationsOverviewDto>;

public sealed class GetPayrollOperationsOverviewQueryHandler
    : IRequestHandler<GetPayrollOperationsOverviewQuery, PayrollOperationsOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetPayrollOperationsOverviewQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PayrollOperationsOverviewDto> Handle(GetPayrollOperationsOverviewQuery request, CancellationToken cancellationToken)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Where(e => e.WorkStatus != "Terminated" && e.WorkStatus != "Resigned" && e.WorkStatus != "Retired")
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleEmployeeLookupDto(
                e.Id, e.EmployeeCode, e.FullName, e.OrganizationUnit.UnitName, e.BaseSalary, e.IsUnionMember))
            .ToListAsync(cancellationToken);

        var periodsRaw = await _context.PayrollPeriods
            .AsNoTracking()
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        var recStats = await _context.PayrollRecords
            .AsNoTracking()
            .GroupBy(r => r.PayrollPeriodId)
            .Select(g => new
            {
                PeriodId = g.Key,
                Count = g.Count(),
                Gross = g.Sum(r => r.GrossIncome),
                Net = g.Sum(r => r.NetSalary),
                Pit = g.Sum(r => r.PersonalIncomeTax),
                CompCost = g.Sum(r => r.TotalCompanyCost)
            })
            .ToDictionaryAsync(x => x.PeriodId, cancellationToken);

        var periodDtos = periodsRaw.Select(p =>
        {
            recStats.TryGetValue(p.Id, out var st);
            var end = p.EndDate != default
                ? p.EndDate
                : new DateOnly(p.Year, p.Month, DateTime.DaysInMonth(p.Year, p.Month));
            return new PayrollPeriodRowDto(
                p.Id,
                p.PeriodCode,
                p.Month,
                p.Year,
                p.PeriodTitle,
                p.StartDate != default ? p.StartDate : new DateOnly(p.Year, p.Month, 1),
                end,
                p.StandardWorkdays,
                p.Status,
                p.IsLocked || string.Equals(p.Status, PayrollPeriodStatuses.Locked, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(p.Status, PayrollPeriodStatuses.Paid, StringComparison.OrdinalIgnoreCase),
                p.CalculationVersion,
                p.CalculatedByUserId,
                p.WorkflowInstanceId,
                st?.Count ?? 0,
                st?.Gross ?? 0m,
                st?.Net ?? 0m,
                st?.Pit ?? 0m,
                p.PeriodType,
                p.HasAttendanceLockWarning,
                p.AttendanceWarningMessage,
                st?.CompCost ?? 0m);
        }).ToList();

        var lockedPeriods = periodDtos.Where(p => p.IsLocked).ToList();

        var advances = await _context.SalaryAdvances
            .AsNoTracking()
            .Include(a => a.Employee)
            .OrderByDescending(a => a.Id)
            .Take(100)
            .Select(a => new SalaryAdvanceRowDto(
                a.Id, a.AdvanceNumber, a.EmployeeId, a.Employee.EmployeeCode, a.Employee.FullName,
                a.RequestDate, a.TargetYear, a.TargetMonth, a.Amount, a.Reason, a.Status, a.DeductedPayrollPeriodId))
            .ToListAsync(cancellationToken);

        var loansRaw = await _context.EmployeeLoans
            .AsNoTracking()
            .Include(l => l.Employee)
            .Include(l => l.Installments)
            .OrderByDescending(l => l.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

        var loans = loansRaw.Select(l => new EmployeeLoanRowDto(
            l.Id, l.LoanCode, l.EmployeeId, l.Employee.EmployeeCode, l.Employee.FullName,
            l.PrincipalAmount,
            l.Installments.Where(i => i.Status == "Scheduled").Sum(i => i.Amount),
            l.TotalInstallments, l.MonthlyInstallmentAmount, l.StartYear, l.StartMonth, l.Reason, l.Status,
            l.Installments.Count(i => i.Status == "Deducted"),
            l.Installments.Count(i => i.Status == "Scheduled"))).ToList();

        var periodCodeMap = periodsRaw.ToDictionary(p => p.Id, p => p.PeriodCode);
        var adjustmentsRaw = await _context.PayrollAdjustments
            .AsNoTracking()
            .Include(a => a.Employee)
            .OrderByDescending(a => a.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

        var adjustments = adjustmentsRaw.Select(a => new PayrollAdjustmentRowDto(
            a.Id,
            string.IsNullOrWhiteSpace(a.AdjustmentCode) ? $"ADJ-{a.Id}" : a.AdjustmentCode,
            a.EmployeeId,
            a.Employee.EmployeeCode,
            a.Employee.FullName,
            a.AdjustmentType,
            a.TargetYear,
            a.TargetMonth,
            a.Amount,
            a.SourceLockedPeriodId,
            a.SourceLockedPeriodId.HasValue && periodCodeMap.TryGetValue(a.SourceLockedPeriodId.Value, out var srcCode) ? srcCode : null,
            a.Reason,
            a.Status,
            a.AppliedPayrollPeriodId,
            a.RecalculateTaxInSourcePeriod,
            a.SourcePeriodTaxDelta,
            a.TaxCalculationNote)).ToList();

        return new PayrollOperationsOverviewDto(employees, periodDtos, lockedPeriods, advances, loans, adjustments);
    }
}

public sealed record CreateSalaryAdvanceCommand(
    int EmployeeId,
    DateOnly AdvanceDate,
    int TargetYear,
    int TargetMonth,
    decimal Amount,
    string Reason) : IRequest<int>;

public sealed class CreateSalaryAdvanceCommandHandler : IRequestHandler<CreateSalaryAdvanceCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public CreateSalaryAdvanceCommandHandler(IApplicationDbContext context, ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CreateSalaryAdvanceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (request.Amount <= 0m)
            throw new InvalidOperationException("Số tiền tạm ứng lương phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do tạm ứng lương.");
        if (request.TargetMonth is < 1 or > 12 || request.TargetYear < 2000)
            throw new ArgumentOutOfRangeException(nameof(request), "Tháng/năm trừ tạm ứng không hợp lệ.");

        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        var code = $"ADV-{request.TargetYear}{request.TargetMonth:D2}-{DateTime.UtcNow:HHmmssfff}";
        var entity = new SalaryAdvance
        {
            AdvanceNumber = code,
            EmployeeId = emp.Id,
            RequestDate = request.AdvanceDate,
            TargetYear = request.TargetYear,
            TargetMonth = request.TargetMonth,
            Amount = Math.Round(request.Amount, 0, MidpointRounding.AwayFromZero),
            Reason = request.Reason.Trim(),
            Status = "Approved"
        };
        await _context.SalaryAdvances.AddAsync(entity, cancellationToken);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "SalaryAdvance_Create",
            EntityName = nameof(SalaryAdvance),
            EntityId = code,
            NewValuesJson = JsonSerializer.Serialize(new { emp.EmployeeCode, entity.TargetYear, entity.TargetMonth, entity.Amount, entity.Reason }),
            IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser?.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public sealed record CreateEmployeeLoanCommand(
    int EmployeeId,
    DateOnly DisbursementDate,
    decimal PrincipalAmount,
    int TermMonths,
    int StartYear,
    int StartMonth,
    string Reason) : IRequest<int>;

public sealed class CreateEmployeeLoanCommandHandler : IRequestHandler<CreateEmployeeLoanCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public CreateEmployeeLoanCommandHandler(IApplicationDbContext context, ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CreateEmployeeLoanCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (request.PrincipalAmount <= 0m)
            throw new InvalidOperationException("Số tiền vay nhân viên phải lớn hơn 0.");
        if (request.TermMonths < 1 || request.TermMonths > 120)
            throw new InvalidOperationException("Số kỳ trả góp phải từ 1 đến 120 tháng.");
        if (request.StartMonth is < 1 or > 12 || request.StartYear < 2000)
            throw new ArgumentOutOfRangeException(nameof(request), "Tháng/năm bắt đầu trả góp không hợp lệ.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do khoản vay nhân viên.");

        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        decimal totalPrincipal = Math.Round(request.PrincipalAmount, 0, MidpointRounding.AwayFromZero);
        decimal baseMonthly = Math.Floor(totalPrincipal / request.TermMonths);
        decimal remainder = totalPrincipal - (baseMonthly * request.TermMonths);

        var code = $"LOAN-{request.StartYear}{request.StartMonth:D2}-{DateTime.UtcNow:HHmmssfff}";
        var loan = new EmployeeLoan
        {
            LoanCode = code,
            EmployeeId = emp.Id,
            LoanType = "Loan",
            PrincipalAmount = totalPrincipal,
            TotalInstallments = request.TermMonths,
            MonthlyInstallmentAmount = baseMonthly,
            StartYear = request.StartYear,
            StartMonth = request.StartMonth,
            Reason = request.Reason.Trim(),
            Status = "Active"
        };

        int curYear = request.StartYear;
        int curMonth = request.StartMonth;
        for (int idx = 1; idx <= request.TermMonths; idx++)
        {
            decimal instAmt = idx == request.TermMonths ? baseMonthly + remainder : baseMonthly;
            loan.Installments.Add(new EmployeeLoanInstallment
            {
                EmployeeId = emp.Id,
                InstallmentNumber = idx,
                TargetYear = curYear,
                TargetMonth = curMonth,
                Amount = instAmt,
                Status = "Scheduled"
            });

            curMonth++;
            if (curMonth > 12)
            {
                curMonth = 1;
                curYear++;
            }
        }

        await _context.EmployeeLoans.AddAsync(loan, cancellationToken);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "EmployeeLoan_Create",
            EntityName = nameof(EmployeeLoan),
            EntityId = code,
            NewValuesJson = JsonSerializer.Serialize(new { emp.EmployeeCode, totalPrincipal, request.TermMonths, request.StartYear, request.StartMonth }),
            IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser?.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return loan.Id;
    }
}

public sealed record CreatePayrollAdjustmentCommand(
    int EmployeeId,
    string AdjustmentType, // RetroEarning | RetroDeduction | Commission | Bonus13 | HolidayBonus
    int TargetYear,
    int TargetMonth,
    decimal Amount,
    string Reason,
    int? SourceLockedPeriodId = null,
    bool IsTaxable = true,
    bool? RecalculateTaxInSourcePeriod = null,
    bool AutoAssignNextOpenPeriod = false) : IRequest<int>;

public sealed class CreatePayrollAdjustmentCommandHandler : IRequestHandler<CreatePayrollAdjustmentCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public CreatePayrollAdjustmentCommandHandler(IApplicationDbContext context, ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CreatePayrollAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (request.Amount <= 0m)
            throw new InvalidOperationException("Số tiền điều chỉnh/thưởng phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Bắt buộc nhập lý do cho phiếu điều chỉnh truy lĩnh/truy thu/thưởng.");

        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        PayrollPeriod? srcPeriod = null;
        if (request.SourceLockedPeriodId.HasValue)
        {
            srcPeriod = await _context.PayrollPeriods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.SourceLockedPeriodId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương nguồn #{request.SourceLockedPeriodId.Value}.");
            if (!srcPeriod.IsLocked)
                throw new InvalidOperationException($"Kỳ nguồn {srcPeriod.PeriodCode} chưa khóa; chỉ dùng truy lĩnh/truy thu đối chiếu kỳ đã khóa.");
        }

        int targetYear = request.TargetYear;
        int targetMonth = request.TargetMonth;
        if ((request.AutoAssignNextOpenPeriod || targetYear <= 0 || targetMonth <= 0) && srcPeriod != null)
        {
            var nextOpen = await _context.PayrollPeriods
                .AsNoTracking()
                .Where(p => !p.IsLocked
                    && p.PeriodType == "Monthly"
                    && (p.Year > srcPeriod.Year || (p.Year == srcPeriod.Year && p.Month > srcPeriod.Month)))
                .OrderBy(p => p.Year)
                .ThenBy(p => p.Month)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextOpen != null)
            {
                targetYear = nextOpen.Year;
                targetMonth = nextOpen.Month;
            }
            else
            {
                targetYear = srcPeriod.Month == 12 ? srcPeriod.Year + 1 : srcPeriod.Year;
                targetMonth = srcPeriod.Month == 12 ? 1 : srcPeriod.Month + 1;
            }
        }

        if (targetMonth is < 1 or > 12 || targetYear < 2000)
            throw new ArgumentOutOfRangeException(nameof(request), "Tháng/năm áp dụng không hợp lệ.");

        bool targetAlreadyLocked = await _context.PayrollPeriods
            .AsNoTracking()
            .AnyAsync(p => p.Year == targetYear
                && p.Month == targetMonth
                && p.PeriodType == "Monthly"
                && p.IsLocked, cancellationToken);
        if (targetAlreadyLocked)
        {
            throw new InvalidOperationException(
                $"Kỳ lương {targetMonth:D2}/{targetYear} đã khóa, không thể gán phiếu điều chỉnh mới trực tiếp vào kỳ này. Hãy chọn kỳ mở kế tiếp.");
        }

        decimal roundedAmount = Math.Round(request.Amount, 0, MidpointRounding.AwayFromZero);
        string adjType = request.AdjustmentType.Trim();
        bool recalcInSource = false;
        decimal sourceTaxDelta = 0m;
        string? taxNote = null;

        if (srcPeriod != null && request.IsTaxable && (adjType is "RetroEarning" or "RetroDeduction" or "Commission"))
        {
            var policyResolved = await PayrollPolicyResolver.ResolvePayrollPolicyAsync(_context, emp.Id, srcPeriod.EndDate, cancellationToken);
            bool policyRecalc = string.Equals(policyResolved.Policy.RetroTaxMode, "RecalculateSourcePeriod", StringComparison.OrdinalIgnoreCase);
            bool wantRecalc = request.RecalculateTaxInSourcePeriod ?? policyRecalc;

            if (wantRecalc)
            {
                var srcRecord = await _context.PayrollRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.PayrollPeriodId == srcPeriod.Id && r.EmployeeId == emp.Id, cancellationToken);

                if (srcRecord != null)
                {
                    var statutoryContext = await VietnamPayrollEngine.TryResolveStatutoryContextAsync(_context, srcPeriod.EndDate, cancellationToken);
                    if (statutoryContext != null)
                    {
                        var priorAdjs = await _context.PayrollAdjustments
                            .AsNoTracking()
                            .Where(a => a.EmployeeId == emp.Id
                                && a.SourceLockedPeriodId == srcPeriod.Id
                                && a.RecalculateTaxInSourcePeriod
                                && a.IsTaxable
                                && a.Status != "Cancelled")
                            .Select(a => new { a.AdjustmentType, a.Amount })
                            .ToListAsync(cancellationToken);

                        decimal priorDelta = priorAdjs.Sum(a =>
                            a.AdjustmentType == "RetroDeduction" ? -a.Amount :
                            (a.AdjustmentType is "RetroEarning" or "Commission" ? a.Amount : 0m));

                        decimal signedDelta = adjType == "RetroDeduction" ? -roundedAmount : roundedAmount;
                        decimal baselineTaxable = Math.Max(0m, srcRecord.TaxableIncome + priorDelta);
                        decimal updatedTaxable = Math.Max(0m, baselineTaxable + signedDelta);

                        decimal oldTax;
                        decimal newTax;
                        if (string.Equals(srcRecord.TaxResidencyMode, "NonResidentFlat20", StringComparison.OrdinalIgnoreCase))
                        {
                            oldTax = Math.Round(baselineTaxable * statutoryContext.Parameter.PitNonResidentRate, 0, MidpointRounding.AwayFromZero);
                            newTax = Math.Round(updatedTaxable * statutoryContext.Parameter.PitNonResidentRate, 0, MidpointRounding.AwayFromZero);
                        }
                        else if (string.Equals(srcRecord.TaxResidencyMode, "CasualWithholding10", StringComparison.OrdinalIgnoreCase))
                        {
                            oldTax = !srcRecord.UsedCommitment08 && baselineTaxable >= statutoryContext.Parameter.PitShortTermThreshold
                                ? Math.Round(baselineTaxable * statutoryContext.Parameter.PitShortTermRate, 0, MidpointRounding.AwayFromZero)
                                : 0m;
                            newTax = !srcRecord.UsedCommitment08 && updatedTaxable >= statutoryContext.Parameter.PitShortTermThreshold
                                ? Math.Round(updatedTaxable * statutoryContext.Parameter.PitShortTermRate, 0, MidpointRounding.AwayFromZero)
                                : 0m;
                        }
                        else
                        {
                            decimal deductions = srcRecord.SelfDeductionAmount + srcRecord.DependentDeductionAmount;
                            decimal oldAssess = Math.Max(0m, baselineTaxable - deductions);
                            decimal newAssess = Math.Max(0m, updatedTaxable - deductions);
                            oldTax = VietnamPayrollEngine.CalculateProgressivePit(oldAssess, statutoryContext.PitBrackets);
                            newTax = VietnamPayrollEngine.CalculateProgressivePit(newAssess, statutoryContext.PitBrackets);
                        }

                        recalcInSource = true;
                        sourceTaxDelta = newTax - oldTax;
                        taxNote = $"Tính lại thuế kỳ nguồn {srcPeriod.PeriodCode}: Thu nhập chịu thuế {baselineTaxable:N0} -> {updatedTaxable:N0} đ; Thuế TNCN {oldTax:N0} -> {newTax:N0} đ (Chênh lệch {sourceTaxDelta:+N0;-N0;0} đ).";
                    }
                    else
                    {
                        taxNote = $"Không tìm thấy tham số thuế kỳ nguồn {srcPeriod.PeriodCode}, thuế sẽ tính vào kỳ chi trả {targetMonth:D2}/{targetYear}.";
                    }
                }
                else
                {
                    taxNote = $"Nhân viên chưa có phiếu lương tại kỳ nguồn {srcPeriod.PeriodCode}, thuế sẽ tính vào kỳ chi trả {targetMonth:D2}/{targetYear}.";
                }
            }
            else
            {
                taxNote = $"Thuế TNCN phần điều chỉnh gộp tính tại kỳ mở {targetMonth:D2}/{targetYear} theo chính sách {policyResolved.PolicyGroupName}.";
            }
        }

        var code = $"ADJ-{targetYear}{targetMonth:D2}-{DateTime.UtcNow:HHmmssfff}";
        var entity = new PayrollAdjustment
        {
            AdjustmentCode = code,
            EmployeeId = emp.Id,
            AdjustmentType = adjType,
            TargetYear = targetYear,
            TargetMonth = targetMonth,
            Amount = roundedAmount,
            SourceLockedPeriodId = request.SourceLockedPeriodId,
            IsTaxable = request.IsTaxable,
            RecalculateTaxInSourcePeriod = recalcInSource,
            SourcePeriodTaxDelta = sourceTaxDelta,
            TaxCalculationNote = taxNote,
            Reason = request.Reason.Trim(),
            Status = "Approved"
        };
        await _context.PayrollAdjustments.AddAsync(entity, cancellationToken);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "PayrollAdjustment_Create",
            EntityName = nameof(PayrollAdjustment),
            EntityId = code,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                emp.EmployeeCode,
                entity.AdjustmentCode,
                entity.AdjustmentType,
                entity.TargetYear,
                entity.TargetMonth,
                entity.Amount,
                entity.SourceLockedPeriodId,
                entity.RecalculateTaxInSourcePeriod,
                entity.SourcePeriodTaxDelta,
                entity.Reason
            }),
            IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser?.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
