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
    IReadOnlyList<PitBracketTaxSliceDto> PitBrackets);

public sealed record GetPayrollRecordTraceQuery(long PayrollRecordId) : IRequest<PayrollRecordTraceDto>;

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

        var steps = !string.IsNullOrWhiteSpace(rec.CalculationTraceJson)
            ? JsonSerializer.Deserialize<List<CalculationTraceStepDto>>(rec.CalculationTraceJson) ?? new List<CalculationTraceStepDto>()
            : new List<CalculationTraceStepDto>();

        var brackets = !string.IsNullOrWhiteSpace(rec.PitBracketsJson)
            ? JsonSerializer.Deserialize<List<PitBracketTaxSliceDto>>(rec.PitBracketsJson) ?? new List<PitBracketTaxSliceDto>()
            : new List<PitBracketTaxSliceDto>();

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
            brackets);
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
    int? AppliedInPayrollPeriodId);

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
            $"ADJ-{a.Id}",
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
            a.AppliedPayrollPeriodId)).ToList();

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
    int? SourceLockedPeriodId = null) : IRequest<int>;

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
        if (request.TargetMonth is < 1 or > 12 || request.TargetYear < 2000)
            throw new ArgumentOutOfRangeException(nameof(request), "Tháng/năm áp dụng không hợp lệ.");

        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        if (request.SourceLockedPeriodId.HasValue)
        {
            var srcPeriod = await _context.PayrollPeriods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.SourceLockedPeriodId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương nguồn #{request.SourceLockedPeriodId.Value}.");
            if (!srcPeriod.IsLocked)
                throw new InvalidOperationException($"Kỳ nguồn {srcPeriod.PeriodCode} chưa khóa; chỉ dùng truy lĩnh/truy thu đối chiếu kỳ đã khóa.");
        }

        var code = $"ADJ-{request.TargetYear}{request.TargetMonth:D2}-{DateTime.UtcNow:HHmmssfff}";
        var entity = new PayrollAdjustment
        {
            EmployeeId = emp.Id,
            AdjustmentType = request.AdjustmentType.Trim(),
            TargetYear = request.TargetYear,
            TargetMonth = request.TargetMonth,
            Amount = Math.Round(request.Amount, 0, MidpointRounding.AwayFromZero),
            SourceLockedPeriodId = request.SourceLockedPeriodId,
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
            NewValuesJson = JsonSerializer.Serialize(new { emp.EmployeeCode, entity.AdjustmentType, entity.TargetYear, entity.TargetMonth, entity.Amount, entity.SourceLockedPeriodId, entity.Reason }),
            IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser?.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
