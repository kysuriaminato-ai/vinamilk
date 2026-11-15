using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Payroll;

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
    int WageRegion = 1);

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

        var actualBase = Math.Round(
            input.ContractBaseSalary * (input.PayableWorkdays / input.StandardWorkdays),
            0,
            MidpointRounding.AwayFromZero);

        var hourlyRate = input.ContractBaseSalary / (input.StandardWorkdays * 8m);
        var otPay = Math.Round(
            hourlyRate * (input.OtHours150 * 1.5m + input.OtHours200 * 2.0m + input.OtHours300 * 3.0m),
            0,
            MidpointRounding.AwayFromZero);

        var gross = actualBase + input.PositionAllowance + input.OtherAllowance + otPay + input.KpiBonus;

        var siCap = parameter.StatutoryBaseSalary * parameter.SocialInsuranceCapMultiplier;
        var regionalMinWage = parameter.GetRegionalMinWage(input.WageRegion);
        var uiCap = regionalMinWage * parameter.UnemploymentInsuranceCapMultiplier;

        var insContractSalary = input.ContractBaseSalary + input.PositionAllowance;
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
        var taxableIncome = Math.Max(0m, gross - totalEmpIns);
        var assessableIncome = Math.Max(0m, taxableIncome - selfDeduction - depDeduction);
        var pit = CalculateProgressivePit(assessableIncome, pitBrackets);

        var net = Math.Max(0m, gross - totalEmpIns - pit - input.DisciplineDeduction);

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
    /// Tính thuế TNCN lũy tiến từng phần theo danh sách bậc thuế có hiệu lực (5 bậc, 7 bậc hoặc tùy biến).
    /// </summary>
    public static decimal CalculateProgressivePit(decimal assessableIncome, IReadOnlyList<PitBracket> pitBrackets)
    {
        if (assessableIncome <= 0m) return 0m;
        if (pitBrackets is null || pitBrackets.Count == 0)
            throw new ArgumentException("Biểu thuế TNCN hiệu lực không được rỗng.", nameof(pitBrackets));

        decimal totalTax = 0m;
        foreach (var bracket in pitBrackets.OrderBy(b => b.BracketLevel).ThenBy(b => b.LowerBound))
        {
            if (assessableIncome <= bracket.LowerBound)
                break;

            var upper = bracket.UpperBound.HasValue
                ? Math.Min(assessableIncome, bracket.UpperBound.Value)
                : assessableIncome;

            if (upper > bracket.LowerBound)
            {
                totalTax += (upper - bracket.LowerBound) * bracket.TaxRate;
            }
        }

        return Math.Round(Math.Max(0m, totalTax), 0, MidpointRounding.AwayFromZero);
    }
}

public sealed record CalculateMonthlyPayrollCommand(int PayrollPeriodId) : IRequest<int>;

public class CalculateMonthlyPayrollCommandHandler : IRequestHandler<CalculateMonthlyPayrollCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CalculateMonthlyPayrollCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<int> Handle(CalculateMonthlyPayrollCommand request, CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        if (period.IsLocked || string.Equals(period.Status, "Locked", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Kỳ lương {period.PeriodCode} đã bị khóa, không được phép tính lại hoặc chỉnh sửa.");

        var periodEndDate = period.EndDate != default
            ? period.EndDate
            : new DateOnly(period.Year, period.Month, DateTime.DaysInMonth(period.Year, period.Month));

        var statutoryContext = await VietnamPayrollEngine.ResolveStatutoryContextAsync(
            _context, periodEndDate, cancellationToken);

        var employees = await _context.Employees
            .Include(e => e.OrganizationUnit)
            .Where(e => e.WorkStatus != "Terminated" && e.WorkStatus != "Resigned" && e.WorkStatus != "Retired")
            .ToListAsync(cancellationToken);

        var attendanceMap = await _context.MonthlyAttendanceSummaries
            .Where(a => a.Year == period.Year && a.Month == period.Month)
            .ToDictionaryAsync(a => a.EmployeeId, cancellationToken);

        var dependentCounts = await _context.EmployeeDependents
            .Where(d => d.IsTaxDeductible)
            .GroupBy(d => d.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, cancellationToken);

        var existingRecords = await _context.PayrollRecords
            .Where(r => r.PayrollPeriodId == period.Id)
            .ToListAsync(cancellationToken);
        if (existingRecords.Count > 0)
            _context.PayrollRecords.RemoveRange(existingRecords);

        foreach (var emp in employees)
        {
            attendanceMap.TryGetValue(emp.Id, out var att);
            dependentCounts.TryGetValue(emp.Id, out var deps);
            int wageRegion = emp.WageRegion ?? emp.OrganizationUnit?.WageRegion ?? 1;

            var breakdown = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
                emp.BaseSalary,
                emp.PositionAllowance,
                emp.OtherAllowance,
                period.StandardWorkdays,
                att?.TotalPayableWorkdays ?? period.StandardWorkdays,
                att?.TotalOtHours150 ?? 0m,
                att?.TotalOtHours200 ?? 0m,
                att?.TotalOtHours300 ?? 0m,
                0m,
                0m,
                deps,
                emp.IsUnionMember,
                wageRegion), statutoryContext);

            _context.PayrollRecords.Add(new PayrollRecord
            {
                PayrollPeriodId = period.Id,
                EmployeeId = emp.Id,
                OrganizationUnitId = emp.OrganizationUnitId,
                PositionId = emp.PositionId,
                MonthlyAttendanceSummaryId = att?.Id,
                StandardWorkdays = period.StandardWorkdays,
                PayableWorkdays = att?.TotalPayableWorkdays ?? period.StandardWorkdays,
                TotalOtHours = (att?.TotalOtHours150 ?? 0m) + (att?.TotalOtHours200 ?? 0m) + (att?.TotalOtHours300 ?? 0m),
                ContractBaseSalary = emp.BaseSalary,
                ActualBaseSalary = breakdown.ActualBaseSalary,
                PositionAllowance = emp.PositionAllowance,
                OtherAllowance = emp.OtherAllowance,
                OvertimePay = breakdown.OvertimePay,
                KpiBonus = 0m,
                GrossIncome = breakdown.GrossIncome,
                InsuranceSalaryBase = breakdown.SocialInsuranceBase,
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
                TaxableIncome = Math.Max(0m, breakdown.GrossIncome - breakdown.TotalEmployeeInsurance),
                AssessableIncome = breakdown.AssessableIncome,
                PersonalIncomeTax = breakdown.PersonalIncomeTax,
                DisciplineDeduction = 0m,
                NetSalary = breakdown.NetSalary,
                PaymentStatus = "Calculated"
            });
        }

        period.Status = "Calculated";
        period.CalculatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return employees.Count;
    }
}

public sealed record LockPayrollPeriodCommand(int PayrollPeriodId) : IRequest<bool>;

public class LockPayrollPeriodCommandHandler : IRequestHandler<LockPayrollPeriodCommand, bool>
{
    private readonly IApplicationDbContext _context;
    public LockPayrollPeriodCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(LockPayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        period.IsLocked = true;
        period.Status = "Locked";
        period.ApprovedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
