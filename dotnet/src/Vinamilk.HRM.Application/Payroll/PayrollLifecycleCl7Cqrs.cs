using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.Payroll;

public sealed record ResolvedPayrollPolicyDto(
    int PolicyGroupId,
    string PolicyGroupName,
    string ScopeLevel,
    PayrollPolicy Policy);

/// <summary>
/// Phân giải chính sách lương/tạm ứng nội bộ theo thứ tự ưu tiên:
/// Nhân viên &gt; Chức danh &gt; Đơn vị &gt; Mặc định công ty.
/// </summary>
public static class PayrollPolicyResolver
{
    public static async Task<ResolvedPayrollPolicyDto> ResolvePayrollPolicyAsync(
        IApplicationDbContext context,
        int employeeId,
        DateOnly effectiveDate,
        CancellationToken cancellationToken = default)
    {
        var emp = await context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.Id, e.PositionId, e.OrganizationUnitId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{employeeId}.");

        var assignments = await context.PolicyGroupAssignments
            .AsNoTracking()
            .Include(a => a.PolicyGroup)
            .Where(a => a.IsActive
                && a.PolicyGroup.IsActive
                && (a.EffectiveFrom == null || a.EffectiveFrom <= effectiveDate)
                && (a.EffectiveTo == null || a.EffectiveTo >= effectiveDate)
                && (a.EmployeeId == emp.Id
                    || a.PositionId == emp.PositionId
                    || a.OrganizationUnitId == emp.OrganizationUnitId))
            .ToListAsync(cancellationToken);

        var policies = await context.PayrollPolicies
            .AsNoTracking()
            .Include(p => p.PolicyGroup)
            .Where(p => p.PolicyGroup.IsActive)
            .ToListAsync(cancellationToken);

        var policyByGroupId = policies.ToDictionary(p => p.PolicyGroupId);

        var empMatch = assignments.FirstOrDefault(a => a.EmployeeId == emp.Id && policyByGroupId.ContainsKey(a.PolicyGroupId));
        if (empMatch != null)
        {
            var pol = policyByGroupId[empMatch.PolicyGroupId];
            return new ResolvedPayrollPolicyDto(pol.PolicyGroupId, empMatch.PolicyGroup.PolicyName, "Employee", pol);
        }

        var posMatch = assignments.FirstOrDefault(a => a.PositionId == emp.PositionId && a.EmployeeId == null && policyByGroupId.ContainsKey(a.PolicyGroupId));
        if (posMatch != null)
        {
            var pol = policyByGroupId[posMatch.PolicyGroupId];
            return new ResolvedPayrollPolicyDto(pol.PolicyGroupId, posMatch.PolicyGroup.PolicyName, "Position", pol);
        }

        var unitMatch = assignments.FirstOrDefault(a => a.OrganizationUnitId == emp.OrganizationUnitId && a.EmployeeId == null && a.PositionId == null && policyByGroupId.ContainsKey(a.PolicyGroupId));
        if (unitMatch != null)
        {
            var pol = policyByGroupId[unitMatch.PolicyGroupId];
            return new ResolvedPayrollPolicyDto(pol.PolicyGroupId, unitMatch.PolicyGroup.PolicyName, "OrganizationUnit", pol);
        }

        var companyDefault = policies.FirstOrDefault(p => p.PolicyGroup.IsCompanyDefault)
            ?? policies.FirstOrDefault();

        if (companyDefault != null)
        {
            return new ResolvedPayrollPolicyDto(
                companyDefault.PolicyGroupId,
                companyDefault.PolicyGroup.PolicyName,
                "CompanyDefault",
                companyDefault);
        }

        var fallback = new PayrollPolicy
        {
            MaxAdvanceLimitPercent = 50.00m,
            AdvanceMinTenureDays = 30,
            AdvanceCutoffDayOfMonth = 25,
            SettleLoansOnTermination = true,
            RetroTaxMode = "RecalculateSourcePeriod",
            ExtraSeveranceMonthsPerYear = 0m,
            PayMidMonthAdvanceWithoutPit = true
        };
        return new ResolvedPayrollPolicyDto(0, "Mặc định hệ thống", "CompanyDefault", fallback);
    }
}

// ============================================================================
// 1. ESS TẠM ỨNG LƯƠNG QUA WORKFLOW & KIỂM TRA HẠN MỨC (% LƯƠNG)
// ============================================================================

public sealed record EssSalaryAdvanceQuotaDto(
    decimal ReferenceBaseSalary,
    decimal MaxAdvanceLimitPercent,
    decimal MaxAllowedAmount,
    decimal UsedOrPendingInMonth,
    decimal RemainingAllowedAmount,
    int TargetYear,
    int TargetMonth,
    string PolicyScopeLevel,
    string PolicyGroupName);

public sealed record GetEssSalaryAdvanceQuotaQuery(int EmployeeId, int? TargetYear = null, int? TargetMonth = null)
    : IRequest<EssSalaryAdvanceQuotaDto>;

public sealed class GetEssSalaryAdvanceQuotaQueryHandler : IRequestHandler<GetEssSalaryAdvanceQuotaQuery, EssSalaryAdvanceQuotaDto>
{
    private readonly IApplicationDbContext _context;

    public GetEssSalaryAdvanceQuotaQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<EssSalaryAdvanceQuotaDto> Handle(GetEssSalaryAdvanceQuotaQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        int targetYear = request.TargetYear ?? today.Year;
        int targetMonth = request.TargetMonth ?? today.Month;
        var periodEnd = new DateOnly(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth));

        var emp = await _context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        var activeProfile = await _context.EmployeeSalaryProfiles.AsNoTracking()
            .Where(p => p.IsActive && p.EmployeeId == emp.Id && p.EffectiveFrom <= periodEnd && (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= periodEnd))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        decimal refBase = activeProfile?.BaseSalary ?? emp.BaseSalary;
        var resolved = await PayrollPolicyResolver.ResolvePayrollPolicyAsync(_context, emp.Id, periodEnd, cancellationToken);
        decimal maxAllowed = Math.Round(refBase * (resolved.Policy.MaxAdvanceLimitPercent / 100m), 0, MidpointRounding.AwayFromZero);

        decimal used = await _context.SalaryAdvances.AsNoTracking()
            .Where(a => a.EmployeeId == emp.Id
                && a.TargetYear == targetYear
                && a.TargetMonth == targetMonth
                && (a.Status == "Pending" || a.Status == "Approved" || a.Status == "Deducted"))
            .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;

        return new EssSalaryAdvanceQuotaDto(
            refBase,
            resolved.Policy.MaxAdvanceLimitPercent,
            maxAllowed,
            used,
            Math.Max(0m, maxAllowed - used),
            targetYear,
            targetMonth,
            resolved.ScopeLevel,
            resolved.PolicyGroupName);
    }
}

public sealed record EssSalaryAdvanceSubmitResultDto(
    int SalaryAdvanceId,
    string AdvanceNumber,
    int WorkflowInstanceId,
    decimal Amount,
    decimal MaxAllowedAmount,
    string Status);

public sealed record SubmitEssSalaryAdvanceCommand(
    int EmployeeId,
    DateOnly RequestDate,
    int TargetYear,
    int TargetMonth,
    decimal Amount,
    string Reason) : IRequest<EssSalaryAdvanceSubmitResultDto>;

public sealed class SubmitEssSalaryAdvanceCommandHandler
    : IRequestHandler<SubmitEssSalaryAdvanceCommand, EssSalaryAdvanceSubmitResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public SubmitEssSalaryAdvanceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<EssSalaryAdvanceSubmitResultDto> Handle(SubmitEssSalaryAdvanceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (request.Amount <= 0m)
            throw new InvalidOperationException("Số tiền tạm ứng lương phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do đề nghị tạm ứng lương.");
        if (request.TargetMonth is < 1 or > 12 || request.TargetYear < 2000)
            throw new ArgumentOutOfRangeException(nameof(request), "Kỳ tháng/năm tạm ứng không hợp lệ.");

        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        var resolved = await PayrollPolicyResolver.ResolvePayrollPolicyAsync(_context, emp.Id, request.RequestDate, cancellationToken);
        if (emp.HireDate != default)
        {
            int tenureDays = request.RequestDate.DayNumber - emp.HireDate.DayNumber;
            if (tenureDays < resolved.Policy.AdvanceMinTenureDays)
            {
                throw new InvalidOperationException(
                    $"Nhân viên cần làm việc tối thiểu {resolved.Policy.AdvanceMinTenureDays} ngày theo chính sách ({resolved.PolicyGroupName}) mới được đề nghị tạm ứng (hiện tại: {Math.Max(0, tenureDays)} ngày).");
            }
        }

        if (request.RequestDate.Day > resolved.Policy.AdvanceCutoffDayOfMonth &&
            request.RequestDate.Year == request.TargetYear &&
            request.RequestDate.Month == request.TargetMonth)
        {
            throw new InvalidOperationException(
                $"Đã quá ngày chốt nhận đơn tạm ứng trong tháng (ngày {resolved.Policy.AdvanceCutoffDayOfMonth}) theo chính sách {resolved.PolicyGroupName}.");
        }

        var periodEnd = new DateOnly(request.TargetYear, request.TargetMonth, DateTime.DaysInMonth(request.TargetYear, request.TargetMonth));
        var activeProfile = await _context.EmployeeSalaryProfiles.AsNoTracking()
            .Where(p => p.IsActive && p.EmployeeId == emp.Id && p.EffectiveFrom <= periodEnd && (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= periodEnd))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        decimal refBaseSalary = activeProfile?.BaseSalary ?? emp.BaseSalary;
        decimal maxAllowed = Math.Round(refBaseSalary * (resolved.Policy.MaxAdvanceLimitPercent / 100m), 0, MidpointRounding.AwayFromZero);
        decimal requestedAmount = Math.Round(request.Amount, 0, MidpointRounding.AwayFromZero);

        decimal existingInMonth = await _context.SalaryAdvances.AsNoTracking()
            .Where(a => a.EmployeeId == emp.Id
                && a.TargetYear == request.TargetYear
                && a.TargetMonth == request.TargetMonth
                && (a.Status == "Pending" || a.Status == "Approved" || a.Status == "Deducted"))
            .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;

        if (existingInMonth + requestedAmount > maxAllowed)
        {
            throw new InvalidOperationException(
                $"Số tiền tạm ứng ({requestedAmount:N0} đ) cộng số đã đề nghị trong tháng ({existingInMonth:N0} đ) vượt hạn mức {resolved.Policy.MaxAdvanceLimitPercent:0.#}% lương ({maxAllowed:N0} đ).");
        }

        var monthlyLocked = await _context.PayrollPeriods.AsNoTracking()
            .AnyAsync(p => p.Year == request.TargetYear && p.Month == request.TargetMonth && p.PeriodType == "Monthly"
                && (p.IsLocked || p.Status == PayrollPeriodStatuses.Locked || p.Status == PayrollPeriodStatuses.Paid), cancellationToken);
        if (monthlyLocked)
            throw new InvalidOperationException($"Kỳ lương tháng {request.TargetMonth:D2}/{request.TargetYear} đã khóa, không thể tạo đơn tạm ứng khấu trừ vào kỳ này.");

        var code = $"ADV-ESS-{request.TargetYear}{request.TargetMonth:D2}-{DateTime.UtcNow:HHmmssfff}";
        var advance = new SalaryAdvance
        {
            AdvanceNumber = code,
            EmployeeId = emp.Id,
            RequestDate = request.RequestDate,
            TargetYear = request.TargetYear,
            TargetMonth = request.TargetMonth,
            Amount = requestedAmount,
            Reason = request.Reason.Trim(),
            Status = "Pending",
            MaxAllowedAmountSnapshot = maxAllowed,
            PolicyGroupIdUsed = resolved.PolicyGroupId > 0 ? resolved.PolicyGroupId : null
        };

        await _context.SalaryAdvances.AddAsync(advance, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var wfId = await _sender.Send(new StartWorkflowInstanceCommand(
            EntityType: "SalaryAdvance",
            EntityId: advance.Id,
            InitiatorEmployeeId: emp.Id,
            DocumentTitle: $"Đề nghị tạm ứng lương {requestedAmount:N0} đ (Kỳ {request.TargetMonth:D2}/{request.TargetYear})",
            NumericConditionValue: requestedAmount,
            TargetUrl: "/ESS?tab=requests"), cancellationToken);

        advance.WorkflowInstanceId = wfId;

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "SalaryAdvance_EssSubmit",
            EntityName = nameof(SalaryAdvance),
            EntityId = advance.Id.ToString(),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                advance.AdvanceNumber,
                emp.EmployeeCode,
                advance.TargetYear,
                advance.TargetMonth,
                advance.Amount,
                MaxAllowed = maxAllowed,
                PolicyScope = resolved.ScopeLevel,
                WorkflowInstanceId = wfId
            }),
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "ESSPortal",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new EssSalaryAdvanceSubmitResultDto(advance.Id, advance.AdvanceNumber, wfId, advance.Amount, maxAllowed, advance.Status);
    }
}

// ============================================================================
// 2. KHOẢN VAY / TRẢ GÓP NỘI BỘ: CHI TIẾT LỊCH TRẢ & TẤT TOÁN SỚM
// ============================================================================

public sealed record EmployeeLoanInstallmentRowDto(
    int Id,
    int InstallmentNumber,
    int TargetYear,
    int TargetMonth,
    decimal Amount,
    string Status,
    int? DeductedPayrollPeriodId,
    int? TerminationSettlementId);

public sealed record EmployeeLoanDetailDto(
    int Id,
    string LoanCode,
    string LoanType,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    decimal PrincipalAmount,
    decimal RemainingPrincipal,
    int TotalInstallments,
    decimal MonthlyInstallmentAmount,
    int StartYear,
    int StartMonth,
    string Reason,
    string Status,
    IReadOnlyList<EmployeeLoanInstallmentRowDto> Installments);

public sealed record GetEmployeeLoanDetailQuery(int EmployeeLoanId) : IRequest<EmployeeLoanDetailDto>;

public sealed class GetEmployeeLoanDetailQueryHandler : IRequestHandler<GetEmployeeLoanDetailQuery, EmployeeLoanDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetEmployeeLoanDetailQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<EmployeeLoanDetailDto> Handle(GetEmployeeLoanDetailQuery request, CancellationToken cancellationToken)
    {
        var loan = await _context.EmployeeLoans
            .AsNoTracking()
            .Include(l => l.Employee)
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == request.EmployeeLoanId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy khoản vay #{request.EmployeeLoanId}.");

        var items = loan.Installments
            .OrderBy(i => i.InstallmentNumber)
            .Select(i => new EmployeeLoanInstallmentRowDto(
                i.Id, i.InstallmentNumber, i.TargetYear, i.TargetMonth, i.Amount, i.Status, i.DeductedPayrollPeriodId, i.TerminationSettlementId))
            .ToList();

        decimal remaining = items.Where(i => i.Status == "Scheduled").Sum(i => i.Amount);

        return new EmployeeLoanDetailDto(
            loan.Id,
            loan.LoanCode,
            loan.LoanType,
            loan.EmployeeId,
            loan.Employee.EmployeeCode,
            loan.Employee.FullName,
            loan.PrincipalAmount,
            remaining,
            loan.TotalInstallments,
            loan.MonthlyInstallmentAmount,
            loan.StartYear,
            loan.StartMonth,
            loan.Reason,
            loan.Status,
            items);
    }
}

public sealed record SettleEmployeeLoanEarlyCommand(int EmployeeLoanId, string Reason, int? TerminationSettlementId = null) : IRequest<decimal>;

public sealed class SettleEmployeeLoanEarlyCommandHandler : IRequestHandler<SettleEmployeeLoanEarlyCommand, decimal>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SettleEmployeeLoanEarlyCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<decimal> Handle(SettleEmployeeLoanEarlyCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do tất toán sớm khoản vay.");

        var loan = await _context.EmployeeLoans
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.Id == request.EmployeeLoanId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy khoản vay #{request.EmployeeLoanId}.");

        decimal settledAmount = 0m;
        foreach (var inst in loan.Installments.Where(i => i.Status == "Scheduled"))
        {
            settledAmount += inst.Amount;
            inst.Status = request.TerminationSettlementId.HasValue ? "SettledOnTermination" : "Waived";
            inst.TerminationSettlementId = request.TerminationSettlementId;
        }
        loan.Status = "PaidOff";

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "EmployeeLoan_EarlySettle",
            EntityName = nameof(EmployeeLoan),
            EntityId = loan.LoanCode,
            NewValuesJson = JsonSerializer.Serialize(new { loan.LoanCode, SettledAmount = settledAmount, request.Reason, request.TerminationSettlementId }),
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return settledAmount;
    }
}

// ============================================================================
// 3. QUYẾT TOÁN NGHỈ VIỆC (FINAL TERMINATION SETTLEMENT + PDF)
// ============================================================================

public sealed record TerminationSettlementRowDto(
    int Id,
    string SettlementCode,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    DateOnly HireDate,
    DateOnly LastWorkingDate,
    string TerminationType,
    decimal WorkedDaysInFinalMonth,
    decimal FinalMonthSalaryAmount,
    decimal UnusedLeaveDays,
    decimal UnusedLeavePayoutAmount,
    decimal EligibleSeveranceYears,
    decimal StatutorySeveranceAmount,
    decimal CompanyExtraSeveranceAmount,
    decimal OtherBonusOrRetroAmount,
    decimal TotalGrossSettlement,
    decimal EmployeeInsuranceDeduction,
    decimal PersonalIncomeTaxDeduction,
    decimal SalaryAdvanceDeduction,
    decimal LoanPayoffDeduction,
    decimal OtherDeduction,
    decimal NetSettlementAmount,
    decimal RemainingDebtToRecover,
    string Status,
    int TargetPayrollYear,
    int TargetPayrollMonth);

public sealed record TerminationSettlementOverviewDto(
    IReadOnlyList<SimpleEmployeeLookupDto> Employees,
    IReadOnlyList<TerminationSettlementRowDto> Settlements);

public sealed record GetTerminationSettlementsOverviewQuery() : IRequest<TerminationSettlementOverviewDto>;

public sealed class GetTerminationSettlementsOverviewQueryHandler
    : IRequestHandler<GetTerminationSettlementsOverviewQuery, TerminationSettlementOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetTerminationSettlementsOverviewQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<TerminationSettlementOverviewDto> Handle(GetTerminationSettlementsOverviewQuery request, CancellationToken cancellationToken)
    {
        var emps = await _context.Employees.AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleEmployeeLookupDto(e.Id, e.EmployeeCode, e.FullName, e.OrganizationUnit.UnitName, e.BaseSalary, e.IsUnionMember))
            .ToListAsync(cancellationToken);

        var raw = await _context.EmployeeTerminationSettlements.AsNoTracking()
            .Include(s => s.Employee).ThenInclude(e => e.OrganizationUnit)
            .OrderByDescending(s => s.Id)
            .Take(100)
            .ToListAsync(cancellationToken);

        var rows = raw.Select(s => new TerminationSettlementRowDto(
            s.Id,
            s.SettlementCode,
            s.EmployeeId,
            s.Employee.EmployeeCode,
            s.Employee.FullName,
            s.Employee.OrganizationUnit?.UnitName ?? string.Empty,
            s.HireDate,
            s.LastWorkingDate,
            s.TerminationType,
            s.WorkedDaysInFinalMonth,
            s.FinalMonthSalaryAmount,
            s.UnusedLeaveDays,
            s.UnusedLeavePayoutAmount,
            s.EligibleSeveranceYears,
            s.StatutorySeveranceAmount,
            s.CompanyExtraSeveranceAmount,
            s.OtherBonusOrRetroAmount,
            s.TotalGrossSettlement,
            s.EmployeeInsuranceDeduction,
            s.PersonalIncomeTaxDeduction,
            s.SalaryAdvanceDeduction,
            s.LoanPayoffDeduction,
            s.OtherDeduction,
            s.NetSettlementAmount,
            s.RemainingDebtToRecover,
            s.Status,
            s.TargetPayrollYear,
            s.TargetPayrollMonth)).ToList();

        return new TerminationSettlementOverviewDto(emps, rows);
    }
}

public sealed record CalculateTerminationSettlementCommand(
    int EmployeeId,
    DateOnly LastWorkingDate,
    string TerminationType = "Resignation", // Resignation | MutualAgreement | ContractExpiry | Dismissal | IllegalUnilateralTermination
    decimal StandardWorkdays = 26m,
    decimal? UnemploymentInsuranceCoveredMonthsOverride = null,
    bool DeductEmployeeInsuranceOnFinalMonth = true,
    string? Notes = null) : IRequest<TerminationSettlementRowDto>;

public sealed class CalculateTerminationSettlementCommandHandler
    : IRequestHandler<CalculateTerminationSettlementCommand, TerminationSettlementRowDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CalculateTerminationSettlementCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TerminationSettlementRowDto> Handle(CalculateTerminationSettlementCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (request.StandardWorkdays <= 0m)
            throw new InvalidOperationException("Số ngày công chuẩn phải lớn hơn 0.");

        var emp = await _context.Employees
            .Include(e => e.OrganizationUnit)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        var lastDay = request.LastWorkingDate;
        var monthStart = new DateOnly(lastDay.Year, lastDay.Month, 1);
        var monthEnd = new DateOnly(lastDay.Year, lastDay.Month, DateTime.DaysInMonth(lastDay.Year, lastDay.Month));
        var activeStart = emp.HireDate != default && emp.HireDate > monthStart ? emp.HireDate : monthStart;

        var statutoryCtx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(_context, lastDay, cancellationToken);
        var resolvedPolicy = await PayrollPolicyResolver.ResolvePayrollPolicyAsync(_context, emp.Id, lastDay, cancellationToken);
        var policy = resolvedPolicy.Policy;
        var param = statutoryCtx.Parameter;

        // 1. Hồ sơ lương hiệu lực ở tháng cuối
        var activeProfile = await _context.EmployeeSalaryProfiles.AsNoTracking()
            .Where(p => p.IsActive && p.EmployeeId == emp.Id && p.EffectiveFrom <= lastDay && (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= activeStart))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        decimal baseSalary = activeProfile?.BaseSalary ?? emp.BaseSalary;
        decimal posAllow = activeProfile?.PositionAllowance ?? emp.PositionAllowance;
        decimal otherAllow = activeProfile?.OtherAllowance ?? emp.OtherAllowance;
        decimal insSalary = activeProfile?.InsuranceSalary > 0m ? activeProfile.InsuranceSalary : (baseSalary + posAllow);
        bool isUnionMember = activeProfile?.IsUnionMember ?? emp.IsUnionMember;
        string residencyMode = activeProfile?.TaxResidencyMode ?? "ResidentStandard";
        bool hasCommitment08 = activeProfile?.HasPitCommitment08 ?? false;

        // 2. Công & Lương ngày làm cuối
        var monthlyAtt = await _context.MonthlyAttendanceSummaries.AsNoTracking()
            .FirstOrDefaultAsync(a => a.EmployeeId == emp.Id && a.Year == lastDay.Year && a.Month == lastDay.Month, cancellationToken);

        decimal workedDays;
        if (monthlyAtt != null && monthlyAtt.TotalPayableWorkdays > 0m)
        {
            workedDays = monthlyAtt.TotalPayableWorkdays;
        }
        else
        {
            int monthWorkingDays = Math.Max(1, MidMonthSalarySplitter.CountWorkingDaysInRange(monthStart, monthEnd));
            int activeWorkingDays = activeStart <= lastDay
                ? MidMonthSalarySplitter.CountWorkingDaysInRange(activeStart, lastDay)
                : 0;
            workedDays = Math.Round(request.StandardWorkdays * activeWorkingDays / monthWorkingDays, 2, MidpointRounding.AwayFromZero);
        }

        decimal probationFactor = activeProfile is { IsProbation: true }
            ? (activeProfile.ProbationRatePercent / 100m)
            : 1.0m;

        decimal effectiveDailyBase = Math.Round(baseSalary * probationFactor, 4, MidpointRounding.AwayFromZero);
        decimal finalMonthSalary = Math.Round(effectiveDailyBase * (workedDays / request.StandardWorkdays), 0, MidpointRounding.AwayFromZero);

        decimal otHours150 = monthlyAtt?.TotalOtHours150 ?? 0m;
        decimal otHours200 = monthlyAtt?.TotalOtHours200 ?? 0m;
        decimal otHours300 = monthlyAtt?.TotalOtHours300 ?? 0m;

        // 3. Phép năm chưa nghỉ
        decimal unusedLeaveDays = 0m;
        decimal unusedLeavePayout = 0m;
        var existingLeaveSet = await _context.TerminationLeaveSettlements.AsNoTracking()
            .FirstOrDefaultAsync(s => s.EmployeeId == emp.Id && s.SettlementYear == lastDay.Year, cancellationToken);

        if (existingLeaveSet != null)
        {
            unusedLeaveDays = existingLeaveSet.UnusedPayableDays;
            unusedLeavePayout = existingLeaveSet.PayoutAmount;
        }
        else
        {
            var attStat = await AttendancePolicyAndShiftEngine.TryResolveStatutoryParameterAsync(_context, lastDay, cancellationToken);
            if (attStat != null)
            {
                var attPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, emp.Id, lastDay, cancellationToken);
                var (_, proRatedEntitlement, seniorityExtra) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(
                    emp.HireDate, lastDay.Year, attStat, attPolicy.Policy, lastDay);

                var annualType = await _context.LeaveTypes.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LeaveTypeCode == "ANNUAL", cancellationToken);
                var balance = annualType != null
                    ? await _context.LeaveBalances.AsNoTracking()
                        .FirstOrDefaultAsync(b => b.EmployeeId == emp.Id && b.LeaveTypeId == annualType.Id && b.Year == lastDay.Year, cancellationToken)
                    : null;

                decimal usedDays = balance?.UsedDays ?? 0m;
                decimal validCarry = 0m;
                if (balance != null)
                {
                    var (vc, _, _, _) = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(balance, attPolicy.Policy, lastDay);
                    validCarry = vc;
                }

                unusedLeaveDays = Math.Max(0m, proRatedEntitlement + seniorityExtra + validCarry - usedDays);
                decimal dailySalary = baseSalary / request.StandardWorkdays;
                unusedLeavePayout = Math.Round(dailySalary * unusedLeaveDays, 0, MidpointRounding.AwayFromZero);
            }
        }

        // 4. Trợ cấp thôi việc (Điều 46 BLLĐ 2019 & Điều 8 NĐ 145/2020/NĐ-CP)
        var hireDate = emp.HireDate != default ? emp.HireDate : lastDay;
        decimal totalTenureMonths = CalculateFullAndPartialMonths(hireDate, lastDay);
        DateOnly uiLawStart = new(2009, 1, 1);
        decimal defaultUiMonths = lastDay >= uiLawStart
            ? CalculateFullAndPartialMonths(hireDate > uiLawStart ? hireDate : uiLawStart, lastDay)
            : 0m;
        decimal uiCoveredMonths = request.UnemploymentInsuranceCoveredMonthsOverride.HasValue
            ? Math.Max(0m, Math.Min(totalTenureMonths, request.UnemploymentInsuranceCoveredMonthsOverride.Value))
            : Math.Min(totalTenureMonths, defaultUiMonths);

        bool eligibleType = !string.Equals(request.TerminationType, "Dismissal", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.TerminationType, "IllegalUnilateralTermination", StringComparison.OrdinalIgnoreCase);

        bool isEligibleForSeverance = eligibleType && totalTenureMonths >= param.SeveranceMinTenureMonths;

        decimal eligibleSeveranceYears = 0m;
        if (isEligibleForSeverance)
        {
            decimal rawEligibleMonths = Math.Max(0m, totalTenureMonths - uiCoveredMonths);
            eligibleSeveranceYears = RoundSeveranceYears(rawEligibleMonths, param.SeveranceRoundingHalfYearMonths);
        }

        // Bình quân tiền lương 6 tháng liền kề trước khi nghỉ việc
        var recentSalaryRecords = await _context.PayrollRecords.AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Where(r => r.EmployeeId == emp.Id && r.PayrollPeriod.PeriodType == "Monthly"
                && (r.PayrollPeriod.Year < lastDay.Year || (r.PayrollPeriod.Year == lastDay.Year && r.PayrollPeriod.Month < lastDay.Month)))
            .OrderByDescending(r => r.PayrollPeriod.Year)
            .ThenByDescending(r => r.PayrollPeriod.Month)
            .Take(6)
            .Select(r => r.ContractBaseSalary + r.PositionAllowance)
            .ToListAsync(cancellationToken);

        decimal avgSalary6Months = recentSalaryRecords.Count > 0
            ? Math.Round(recentSalaryRecords.Average(), 0, MidpointRounding.AwayFromZero)
            : Math.Round(baseSalary + posAllow, 0, MidpointRounding.AwayFromZero);

        decimal statutorySeveranceAmount = Math.Round(
            avgSalary6Months * eligibleSeveranceYears * param.SeveranceAllowanceMonthsPerYear,
            0,
            MidpointRounding.AwayFromZero);
        decimal companyExtraSeveranceAmount = isEligibleForSeverance && policy.ExtraSeveranceMonthsPerYear > 0m
            ? Math.Round(avgSalary6Months * eligibleSeveranceYears * policy.ExtraSeveranceMonthsPerYear, 0, MidpointRounding.AwayFromZero)
            : 0m;

        // 5. Tạm ứng chưa trừ, Vay cần tất toán & Điều chỉnh / Kỷ luật còn tồn
        var pendingAdvs = await _context.SalaryAdvances.AsNoTracking()
            .Where(a => a.EmployeeId == emp.Id && a.Status == "Approved" && a.TerminationSettlementId == null)
            .ToListAsync(cancellationToken);
        decimal advanceDed = pendingAdvs.Sum(a => a.Amount);

        decimal loanPayoffDed = 0m;
        var scheduledInsts = new List<EmployeeLoanInstallment>();
        if (policy.SettleLoansOnTermination)
        {
            scheduledInsts = await _context.EmployeeLoanInstallments.AsNoTracking()
                .Include(i => i.EmployeeLoan)
                .Where(i => i.EmployeeId == emp.Id && i.EmployeeLoan.Status == "Active" && i.Status == "Scheduled")
                .ToListAsync(cancellationToken);
            loanPayoffDed = scheduledInsts.Sum(i => i.Amount);
        }

        var pendingAdjs = await _context.PayrollAdjustments.AsNoTracking()
            .Where(a => a.EmployeeId == emp.Id && a.Status == "Approved" && a.TerminationSettlementId == null)
            .ToListAsync(cancellationToken);

        decimal adjEarning = 0m;
        decimal adjDeduction = 0m;
        foreach (var a in pendingAdjs)
        {
            if (a.AdjustmentType == "RetroDeduction")
                adjDeduction += a.Amount;
            else
                adjEarning += a.Amount;
        }

        var pendingRds = await _context.RewardDisciplines.AsNoTracking()
            .Where(r => r.EmployeeId == emp.Id && r.Status == "Approved" && r.PayrollPeriodId == null
                && r.EffectiveDate >= monthStart && r.EffectiveDate <= lastDay)
            .ToListAsync(cancellationToken);
        decimal rewardAmt = pendingRds.Where(r => !string.Equals(r.Type, "Discipline", StringComparison.OrdinalIgnoreCase)).Sum(r => r.Amount);
        decimal fineAmt = pendingRds.Where(r => string.Equals(r.Type, "Discipline", StringComparison.OrdinalIgnoreCase)).Sum(r => r.Amount);

        decimal otherBonusOrRetro = adjEarning + rewardAmt;
        decimal otherDed = adjDeduction + fineAmt;

        int dependentCount = await _context.EmployeeDependents.AsNoTracking()
            .CountAsync(d => d.EmployeeId == emp.Id && d.IsTaxDeductible
                && (d.DeductionFromDate == null || d.DeductionFromDate <= lastDay)
                && (d.DeductionToDate == null || d.DeductionToDate >= monthStart), cancellationToken);

        int wageRegion = emp.WageRegion ?? emp.OrganizationUnit?.WageRegion ?? 1;
        decimal exemptSeverance = param.IsSeveranceStatutoryTaxExempt ? statutorySeveranceAmount : 0m;

        var calcInput = new PayrollCalculationInput(
            ContractBaseSalary: baseSalary,
            PositionAllowance: workedDays > 0m ? posAllow : 0m,
            OtherAllowance: workedDays > 0m ? otherAllow : 0m,
            StandardWorkdays: request.StandardWorkdays,
            PayableWorkdays: workedDays,
            OtHours150: otHours150,
            OtHours200: otHours200,
            OtHours300: otHours300,
            KpiBonus: otherBonusOrRetro + companyExtraSeveranceAmount + statutorySeveranceAmount,
            DisciplineDeduction: otherDed,
            DependentCount: dependentCount,
            IsUnionMember: isUnionMember,
            WageRegion: wageRegion,
            UnusedLeavePayout: unusedLeavePayout,
            InsuranceContractSalaryOverride: request.DeductEmployeeInsuranceOnFinalMonth ? insSalary : 0m,
            PrecomputedActualBaseSalary: finalMonthSalary,
            ExtraTaxExemptAmount: exemptSeverance,
            SalaryAdvanceDeduction: advanceDed,
            LoanInstallmentDeduction: loanPayoffDed,
            TaxResidencyMode: residencyMode,
            HasPitCommitment08: hasCommitment08);

        var bd = VietnamPayrollEngine.Calculate(calcInput, statutoryCtx);

        decimal rawBalanceAfterTax = bd.GrossIncome - bd.TotalEmployeeInsurance - bd.PersonalIncomeTax - bd.EmployeeUnionFee;
        decimal totalDebtAndDed = advanceDed + loanPayoffDed + otherDed;
        decimal netPayable = rawBalanceAfterTax >= totalDebtAndDed ? rawBalanceAfterTax - totalDebtAndDed : 0m;
        decimal debtToRecover = rawBalanceAfterTax < totalDebtAndDed ? totalDebtAndDed - rawBalanceAfterTax : 0m;

        var tracePayload = new
        {
            BaseSalary = baseSalary,
            WorkedDays = workedDays,
            FinalMonthSalary = finalMonthSalary,
            Allowances = (workedDays > 0m ? posAllow + otherAllow : 0m),
            OvertimePay = bd.OvertimePay,
            UnusedLeaveDays = unusedLeaveDays,
            UnusedLeavePayout = unusedLeavePayout,
            AvgSalary6Months = avgSalary6Months,
            TotalTenureMonths = totalTenureMonths,
            UiCoveredMonths = uiCoveredMonths,
            EligibleSeveranceYears = eligibleSeveranceYears,
            StatutorySeveranceRate = param.SeveranceAllowanceMonthsPerYear,
            StatutorySeveranceAmount = statutorySeveranceAmount,
            CompanyExtraSeveranceAmount = companyExtraSeveranceAmount,
            OtherBonusOrRetro = otherBonusOrRetro,
            Gross = bd.GrossIncome,
            EmployeeInsurance = bd.TotalEmployeeInsurance,
            EmployeeUnionFee = bd.EmployeeUnionFee,
            TaxableIncome = bd.TaxableIncome,
            PersonalIncomeTax = bd.PersonalIncomeTax,
            SalaryAdvanceDeduction = advanceDed,
            AdvanceNumbers = pendingAdvs.Select(a => new { a.Id, a.AdvanceNumber, a.Amount }),
            LoanPayoffDeduction = loanPayoffDed,
            SettledInstallmentIds = scheduledInsts.Select(i => i.Id),
            ConsumedAdjustmentIds = pendingAdjs.Select(a => a.Id),
            OtherDeduction = otherDed,
            NetSettlement = netPayable,
            RemainingDebtToRecover = debtToRecover
        };

        var settlement = await _context.EmployeeTerminationSettlements
            .FirstOrDefaultAsync(s => s.EmployeeId == emp.Id && s.Status == "Draft", cancellationToken);

        if (settlement == null)
        {
            settlement = new EmployeeTerminationSettlement
            {
                SettlementCode = $"FSET-{lastDay:yyyyMM}-{emp.EmployeeCode}-{DateTime.UtcNow:HHmmss}",
                EmployeeId = emp.Id
            };
            await _context.EmployeeTerminationSettlements.AddAsync(settlement, cancellationToken);
        }

        settlement.HireDate = hireDate;
        settlement.LastWorkingDate = lastDay;
        settlement.TerminationType = request.TerminationType;
        settlement.IsEligibleForSeverance = isEligibleForSeverance;
        settlement.StandardWorkdaysUsed = request.StandardWorkdays;
        settlement.WorkedDaysInFinalMonth = workedDays;
        settlement.BaseSalaryUsed = baseSalary;
        settlement.PositionAllowanceUsed = posAllow;
        settlement.FinalMonthSalaryAmount = finalMonthSalary + (workedDays > 0m ? posAllow + otherAllow : 0m) + bd.OvertimePay;
        settlement.AverageSalaryLast6Months = avgSalary6Months;
        settlement.TotalTenureMonths = totalTenureMonths;
        settlement.UnemploymentInsuranceCoveredMonths = uiCoveredMonths;
        settlement.EligibleSeveranceYears = eligibleSeveranceYears;
        settlement.StatutorySeveranceRatePerYear = param.SeveranceAllowanceMonthsPerYear;
        settlement.CompanyExtraSeveranceRatePerYear = policy.ExtraSeveranceMonthsPerYear;
        settlement.StatutorySeveranceAmount = statutorySeveranceAmount;
        settlement.CompanyExtraSeveranceAmount = companyExtraSeveranceAmount;
        settlement.UnusedLeaveDays = unusedLeaveDays;
        settlement.UnusedLeavePayoutAmount = unusedLeavePayout;
        settlement.OtherBonusOrRetroAmount = otherBonusOrRetro;
        settlement.TotalGrossSettlement = bd.GrossIncome;
        settlement.EmployeeInsuranceDeduction = bd.TotalEmployeeInsurance;
        settlement.PersonalIncomeTaxDeduction = bd.PersonalIncomeTax;
        settlement.SalaryAdvanceDeduction = advanceDed;
        settlement.LoanPayoffDeduction = loanPayoffDed;
        settlement.OtherDeduction = otherDed + bd.EmployeeUnionFee;
        settlement.NetSettlementAmount = netPayable;
        settlement.RemainingDebtToRecover = debtToRecover;
        settlement.CalculationBreakdownJson = JsonSerializer.Serialize(tracePayload);
        settlement.Status = "Draft";
        settlement.TargetPayrollYear = lastDay.Year;
        settlement.TargetPayrollMonth = lastDay.Month;
        settlement.Notes = string.IsNullOrWhiteSpace(request.Notes)
            ? $"Quyết toán nghỉ việc ({request.TerminationType}) tại ngày làm việc cuối {lastDay:dd/MM/yyyy}"
            : request.Notes.Trim();

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "TerminationSettlement_Calculate",
            EntityName = nameof(EmployeeTerminationSettlement),
            EntityId = settlement.SettlementCode,
            NewValuesJson = settlement.CalculationBreakdownJson,
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new TerminationSettlementRowDto(
            settlement.Id,
            settlement.SettlementCode,
            emp.Id,
            emp.EmployeeCode,
            emp.FullName,
            emp.OrganizationUnit?.UnitName ?? string.Empty,
            settlement.HireDate,
            settlement.LastWorkingDate,
            settlement.TerminationType,
            settlement.WorkedDaysInFinalMonth,
            settlement.FinalMonthSalaryAmount,
            settlement.UnusedLeaveDays,
            settlement.UnusedLeavePayoutAmount,
            settlement.EligibleSeveranceYears,
            settlement.StatutorySeveranceAmount,
            settlement.CompanyExtraSeveranceAmount,
            settlement.OtherBonusOrRetroAmount,
            settlement.TotalGrossSettlement,
            settlement.EmployeeInsuranceDeduction,
            settlement.PersonalIncomeTaxDeduction,
            settlement.SalaryAdvanceDeduction,
            settlement.LoanPayoffDeduction,
            settlement.OtherDeduction,
            settlement.NetSettlementAmount,
            settlement.RemainingDebtToRecover,
            settlement.Status,
            settlement.TargetPayrollYear,
            settlement.TargetPayrollMonth);
    }

    public static decimal CalculateFullAndPartialMonths(DateOnly start, DateOnly end)
    {
        if (end < start)
            return 0m;
        int months = ((end.Year - start.Year) * 12) + (end.Month - start.Month);
        int dayDiff = end.Day - start.Day + 1;
        if (dayDiff >= 15)
            months += 1;
        else if (months == 0 && dayDiff > 0)
            return Math.Round(dayDiff / 30m, 2, MidpointRounding.AwayFromZero);
        return Math.Max(0, months);
    }

    public static decimal RoundSeveranceYears(decimal rawMonths, int fullYearThresholdMonths)
    {
        if (rawMonths <= 0m)
            return 0m;
        int wholeYears = (int)Math.Floor(rawMonths / 12m);
        decimal oddMonths = rawMonths - (wholeYears * 12m);
        if (oddMonths <= 0m)
            return wholeYears;
        return oddMonths >= fullYearThresholdMonths ? wholeYears + 1.0m : wholeYears + 0.5m;
    }
}

public sealed record ApproveAndSettleTerminationCommand(int TerminationSettlementId, bool MarkAsPaid = true) : IRequest<string>;

public sealed class ApproveAndSettleTerminationCommandHandler
    : IRequestHandler<ApproveAndSettleTerminationCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ApproveAndSettleTerminationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(ApproveAndSettleTerminationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var settlement = await _context.EmployeeTerminationSettlements
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == request.TerminationSettlementId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy biên bản quyết toán #{request.TerminationSettlementId}.");

        if (settlement.Status == "Paid")
            return $"Biên bản {settlement.SettlementCode} đã tất toán trước đó.";

        // Tất toán các khoản tạm ứng lương còn mở
        var pendingAdvs = await _context.SalaryAdvances
            .Where(a => a.EmployeeId == settlement.EmployeeId && a.Status == "Approved")
            .ToListAsync(cancellationToken);
        foreach (var adv in pendingAdvs)
        {
            adv.Status = "Deducted";
            adv.TerminationSettlementId = settlement.Id;
        }

        // Tất toán toàn bộ các kỳ trả góp còn lại của khoản vay
        var remainingInstallments = await _context.EmployeeLoanInstallments
            .Include(i => i.EmployeeLoan)
            .Where(i => i.EmployeeId == settlement.EmployeeId && i.EmployeeLoan.Status == "Active" && i.Status == "Scheduled")
            .ToListAsync(cancellationToken);
        var settledLoans = new HashSet<EmployeeLoan>();
        foreach (var inst in remainingInstallments)
        {
            inst.Status = "SettledOnTermination";
            inst.TerminationSettlementId = settlement.Id;
            settledLoans.Add(inst.EmployeeLoan);
        }
        foreach (var loan in settledLoans)
            loan.Status = "PaidOff";

        // Đánh dấu các điều chỉnh chờ áp dụng đã đưa vào quyết toán
        var pendingAdjs = await _context.PayrollAdjustments
            .Where(a => a.EmployeeId == settlement.EmployeeId && a.Status == "Approved")
            .ToListAsync(cancellationToken);
        foreach (var adj in pendingAdjs)
        {
            adj.Status = "Applied";
            adj.TerminationSettlementId = settlement.Id;
        }

        settlement.Employee.TerminationDate = settlement.LastWorkingDate;
        if (settlement.Employee.WorkStatus is "Official" or "Probation" or "Active")
            settlement.Employee.WorkStatus = "Resigned";

        settlement.ApprovedByUserId = userId;
        settlement.ApprovedAt = DateTime.UtcNow;
        if (request.MarkAsPaid)
        {
            settlement.Status = "Paid";
            settlement.PaidAt = DateTime.UtcNow;
        }
        else
        {
            settlement.Status = "Approved";
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "TerminationSettlement_ApproveSettle",
            EntityName = nameof(EmployeeTerminationSettlement),
            EntityId = settlement.SettlementCode,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                settlement.SettlementCode,
                settlement.Status,
                SettledAdvanceCount = pendingAdvs.Count,
                SettledInstallmentCount = remainingInstallments.Count,
                SettledLoanCount = settledLoans.Count,
                settlement.NetSettlementAmount,
                settlement.RemainingDebtToRecover
            }),
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return $"Đã duyệt & tất toán biên bản quyết toán {settlement.SettlementCode} (Thực nhận: {settlement.NetSettlementAmount:N0} đ).";
    }
}

public sealed record GetTerminationSettlementPdfQuery(int TerminationSettlementId) : IRequest<PayslipPdfResult>;

public sealed class GetTerminationSettlementPdfQueryHandler
    : IRequestHandler<GetTerminationSettlementPdfQuery, PayslipPdfResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPayrollDocumentService _documentService;

    public GetTerminationSettlementPdfQueryHandler(
        IApplicationDbContext context,
        IPayrollDocumentService documentService)
    {
        _context = context;
        _documentService = documentService;
    }

    public async Task<PayslipPdfResult> Handle(GetTerminationSettlementPdfQuery request, CancellationToken cancellationToken)
    {
        var settlement = await _context.EmployeeTerminationSettlements
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.TerminationSettlementId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy biên bản quyết toán #{request.TerminationSettlementId}.");

        var emp = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == settlement.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{settlement.EmployeeId}.");

        return _documentService.GenerateTerminationSettlementPdf(settlement, emp);
    }
}
