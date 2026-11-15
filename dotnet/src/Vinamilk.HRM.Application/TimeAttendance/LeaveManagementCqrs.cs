using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.TimeAttendance;

// ============================================================================
// 1. DTOs CHO QUẢN LÝ NGHỈ PHÉP, QUỸ PHÉP THÂM NIÊN & LỊCH NGHỈ TEAM
// ============================================================================

public record LeaveTypeDto(
    int Id,
    string LeaveTypeCode,
    string LeaveTypeName,
    bool IsPaidByCompany,
    bool IsPaidBySocialInsurance,
    bool CountsAgainstAnnualLeave,
    bool AllowHalfDay,
    bool AllowHourly,
    bool RequiresAttachment,
    decimal? MaxDaysPerRequest,
    decimal MaxDaysPerYear,
    bool AllowCarryForward,
    string AttendanceSymbol,
    int DisplayOrder,
    bool IsActive,
    string? LegalReference,
    bool NeedsHrVerification,
    string? Description);

public record LeaveBalanceItemDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    string PositionName,
    DateOnly HireDate,
    int SeniorityYears,
    int Year,
    decimal StandardEntitlement,
    decimal SeniorityExtraDays,
    decimal CarriedForwardDays,
    decimal UsedCarriedForwardDays,
    decimal ExpiredCarryForwardDays,
    DateOnly? CarryForwardExpiryDate,
    decimal UsedDays,
    decimal PendingDays,
    decimal AdvanceUsedDays,
    decimal RemainingDays,
    decimal CompensatoryHoursBalance,
    decimal TerminationPayoutDays,
    decimal TerminationPayoutAmount,
    bool IsTerminationSettled);

public record LeaveRequestItemDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    int LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    string AttendanceSymbol,
    DateOnly StartDate,
    DateOnly EndDate,
    string StartSession,
    string EndSession,
    string LeaveDurationMode,
    decimal? RequestedHours,
    decimal TotalDays,
    string? Reason,
    string? AttachmentUrl,
    string? HandoverEmployeeName,
    string Status,
    int? WorkflowInstanceId,
    string? ApproverComment,
    string? CancelReason,
    DateTime CreatedAt,
    DateTime? ApprovedAt);

public record TeamLeaveCalendarDayCellDto(
    int Day,
    DateOnly Date,
    bool IsWeekend,
    string? LeaveSymbol,
    string? LeaveTypeName,
    string? Status);

public record TeamLeaveCalendarEmployeeRowDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    string PositionName,
    IReadOnlyList<TeamLeaveCalendarDayCellDto> Days);

public record LeaveManagementOverviewDto(
    int Year,
    int Month,
    int DaysInMonth,
    int? SelectedUnitId,
    IReadOnlyList<SimpleLookupDto> Units,
    IReadOnlyList<SimpleLookupDto> ActiveEmployees,
    IReadOnlyList<LeaveTypeDto> LeaveTypes,
    IReadOnlyList<LeaveBalanceItemDto> Balances,
    IReadOnlyList<LeaveRequestItemDto> Requests,
    IReadOnlyList<TeamLeaveCalendarEmployeeRowDto> TeamCalendarRows,
    int TotalPendingRequests,
    int TotalApprovedRequests,
    decimal AverageRemainingAnnualLeave,
    int TotalEmployeesWithSeniorityBonus,
    IReadOnlyList<OvertimeRequestItemDto>? OvertimeRequests = null,
    IReadOnlyList<EmployeeTimeRequestItemDto>? EmployeeTimeRequests = null,
    IReadOnlyList<RequestCancellationItemDto>? CancellationRequests = null,
    IReadOnlyList<TerminationLeaveSettlementDto>? TerminationSettlements = null,
    CompanyLeaveBalanceReportDto? CompanyBalanceReport = null,
    decimal StatutoryMaxMonthlyOt = 40m,
    decimal StatutoryMaxYearlyOt = 200m);

// ============================================================================
// 2. BỘ TÍNH PHÉP NĂM THEO THÂM NIÊN & CẤU HÌNH (ĐIỀU 113 & 114 BLLĐ 2019)
// ============================================================================

public static class VietnamAnnualLeavePolicyCalculator
{
    public static (int SeniorityYears, decimal StandardEntitlement, decimal SeniorityExtraDays) CalculateEntitlement(
        DateOnly hireDate,
        int targetYear,
        AttendanceStatutoryParameter statutory,
        AttendancePolicy? policy = null,
        DateOnly? terminationDate = null)
    {
        ArgumentNullException.ThrowIfNull(statutory);
        if (statutory.StandardAnnualLeaveDays <= 0m)
            throw new InvalidOperationException("Chưa cấu hình StandardAnnualLeaveDays hợp lệ trong AttendanceStatutoryParameter.");

        var yearStart = new DateOnly(targetYear, 1, 1);
        var yearEnd = new DateOnly(targetYear, 12, 31);
        var effectiveEnd = terminationDate.HasValue && terminationDate.Value < yearEnd
            ? terminationDate.Value
            : yearEnd;

        if (hireDate > effectiveEnd)
            return (0, 0m, 0m);

        int cutoffDay = policy?.MidMonthHireCutoffDay > 0 ? policy.MidMonthHireCutoffDay : 15;

        int seniorityYears = Math.Max(0, effectiveEnd.Year - hireDate.Year);
        if (hireDate.Month > effectiveEnd.Month || (hireDate.Month == effectiveEnd.Month && hireDate.Day > effectiveEnd.Day))
        {
            if (seniorityYears > 0) seniorityYears--;
        }

        decimal extraDays = statutory.SeniorityBonusYearsInterval > 0
            ? (seniorityYears / statutory.SeniorityBonusYearsInterval) * statutory.SeniorityBonusDaysPerInterval
            : 0m;

        int startMonth = hireDate < yearStart
            ? 1
            : (hireDate.Year == targetYear ? (hireDate.Day <= cutoffDay ? hireDate.Month : hireDate.Month + 1) : 13);

        int endMonth = effectiveEnd.Year > targetYear
            ? 12
            : (effectiveEnd.Day >= cutoffDay ? effectiveEnd.Month : effectiveEnd.Month - 1);

        int workedMonths = Math.Clamp(endMonth - startMonth + 1, 0, 12);

        decimal standardDays = workedMonths == 12
            ? statutory.StandardAnnualLeaveDays
            : Math.Round(statutory.StandardAnnualLeaveDays * workedMonths / 12m, 1, MidpointRounding.AwayFromZero);

        return (seniorityYears, standardDays, extraDays);
    }

    public static (decimal ValidCarryForwardDays, decimal ExpiredCarryForwardDays, decimal TotalAvailableDays, decimal MaxAdvanceAllowed) ComputeAvailableBalance(
        LeaveBalance balance,
        AttendancePolicy? policy,
        DateOnly asOfDate)
    {
        int expMonth = policy != null ? Math.Clamp(policy.CarryForwardExpiryMonth, 1, 12) : 3;
        int expDay = policy != null
            ? Math.Clamp(policy.CarryForwardExpiryDay, 1, DateTime.DaysInMonth(balance.Year, expMonth))
            : 31;
        var expiryDate = balance.CarryForwardExpiryDate ?? new DateOnly(balance.Year, expMonth, expDay);

        decimal unusedCarry = Math.Max(0m, balance.CarriedForwardDays - balance.UsedCarriedForwardDays);
        bool carryExpired = asOfDate > expiryDate;
        decimal validCarry = carryExpired ? balance.UsedCarriedForwardDays : balance.CarriedForwardDays;
        decimal expiredCarry = carryExpired ? unusedCarry : 0m;

        decimal grossAvailable = balance.StandardEntitlement
            + balance.SeniorityExtraDays
            + validCarry
            - balance.UsedDays
            - balance.PendingDays;

        decimal maxAdvance = policy?.AllowAdvanceAnnualLeave == true
            ? Math.Max(0m, policy.MaxAdvanceAnnualLeaveDays)
            : 0m;

        return (validCarry, expiredCarry, grossAvailable, maxAdvance);
    }

    public static decimal CalculateWorkingLeaveDays(
        DateOnly startDate,
        DateOnly endDate,
        string startSession,
        string endSession,
        IReadOnlySet<DateOnly> holidays)
    {
        if (endDate < startDate)
            return 0m;

        if (startDate == endDate)
        {
            if (startDate.DayOfWeek == DayOfWeek.Sunday || holidays.Contains(startDate))
                return 0m;
            return (startSession == "FullDay" && endSession == "FullDay") ? 1.0m : 0.5m;
        }

        decimal total = 0m;
        for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
        {
            if (dt.DayOfWeek == DayOfWeek.Sunday || holidays.Contains(dt))
                continue;

            if (dt == startDate && startSession != "FullDay")
                total += 0.5m;
            else if (dt == endDate && endSession != "FullDay")
                total += 0.5m;
            else
                total += 1.0m;
        }

        return total;
    }
}

// ============================================================================
// 3. QUERY: LẤY TỔNG QUAN QUỸ PHÉP, ĐƠN NGHỈ PHÉP & LỊCH NGHỈ TEAM
// ============================================================================

public record GetLeaveManagementOverviewQuery(
    int? Year = null,
    int? Month = null,
    int? UnitId = null) : IRequest<LeaveManagementOverviewDto>;

public class GetLeaveManagementOverviewQueryHandler : IRequestHandler<GetLeaveManagementOverviewQuery, LeaveManagementOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetLeaveManagementOverviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveManagementOverviewDto> Handle(GetLeaveManagementOverviewQuery request, CancellationToken cancellationToken)
    {
        int year = request.Year ?? DateTime.Today.Year;
        int month = request.Month ?? DateTime.Today.Month;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, daysInMonth);

        // Đảm bảo có loại hình Đi công tác (BUSINESS_TRIP - CT) bên cạnh các loại nghỉ chế độ
        if (!await _context.LeaveTypes.AnyAsync(l => l.LeaveTypeCode == "BUSINESS_TRIP", cancellationToken))
        {
            await _context.LeaveTypes.AddAsync(new LeaveType
            {
                LeaveTypeCode = "BUSINESS_TRIP",
                LeaveTypeName = "Đi công tác trong/ngoài nước (Hưởng nguyên lương)",
                IsPaidByCompany = true,
                IsPaidBySocialInsurance = false,
                MaxDaysPerYear = 90m,
                AllowCarryForward = false,
                AttendanceSymbol = "CT"
            }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Seed một số đơn nghỉ phép / công tác mẫu nếu chưa có
        if (!await _context.LeaveRequests.AnyAsync(cancellationToken))
        {
            var annualType = await _context.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "ANNUAL", cancellationToken);
            var marriageType = await _context.LeaveTypes.FirstOrDefaultAsync(l => l.LeaveTypeCode == "MARRIAGE", cancellationToken) ?? annualType;
            var tripType = await _context.LeaveTypes.FirstOrDefaultAsync(l => l.LeaveTypeCode == "BUSINESS_TRIP", cancellationToken) ?? annualType;
            var emps = await _context.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).Take(5).ToListAsync(cancellationToken);

            if (emps.Count >= 3)
            {
                await _context.LeaveRequests.AddRangeAsync(new List<LeaveRequest>
                {
                    new()
                    {
                        EmployeeId = emps[0].Id,
                        LeaveTypeId = annualType.Id,
                        StartDate = new DateOnly(year, month, Math.Min(5, daysInMonth)),
                        EndDate = new DateOnly(year, month, Math.Min(6, daysInMonth)),
                        StartSession = "FullDay",
                        EndSession = "FullDay",
                        TotalDays = 2.0m,
                        Reason = "Nghỉ phép năm đưa gia đình đi khám sức khỏe định kỳ",
                        Status = "Approved",
                        ApprovedAt = DateTime.UtcNow.AddDays(-2),
                        ApproverComment = "Đồng ý theo kế hoạch phép năm"
                    },
                    new()
                    {
                        EmployeeId = emps[1].Id,
                        LeaveTypeId = tripType.Id,
                        StartDate = new DateOnly(year, month, Math.Min(12, daysInMonth)),
                        EndDate = new DateOnly(year, month, Math.Min(14, daysInMonth)),
                        StartSession = "FullDay",
                        EndSession = "FullDay",
                        TotalDays = 3.0m,
                        Reason = "Công tác kiểm tra chất lượng trang trại bò sữa Organic Lâm Đồng",
                        Status = "Approved",
                        ApprovedAt = DateTime.UtcNow.AddDays(-1),
                        ApproverComment = "Đã duyệt lệnh điều động công tác"
                    },
                    new()
                    {
                        EmployeeId = emps[2].Id,
                        LeaveTypeId = marriageType.Id,
                        StartDate = new DateOnly(year, month, Math.Min(18, daysInMonth)),
                        EndDate = new DateOnly(year, month, Math.Min(20, daysInMonth)),
                        StartSession = "FullDay",
                        EndSession = "FullDay",
                        TotalDays = 3.0m,
                        Reason = "Nghỉ chế độ kết hôn theo Điều 115 Bộ luật Lao động 2019",
                        Status = "Pending"
                    }
                }, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .OrderBy(u => u.SortOrder)
            .Select(u => new SimpleLookupDto(u.Id, u.UnitCode, u.UnitName))
            .ToListAsync(cancellationToken);

        var activeEmployees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation")
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleLookupDto(e.Id, e.EmployeeCode, $"{e.EmployeeCode} - {e.FullName}"))
            .ToListAsync(cancellationToken);

        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Id)
            .Select(l => new LeaveTypeDto(
                l.Id,
                l.LeaveTypeCode,
                l.LeaveTypeName,
                l.IsPaidByCompany,
                l.IsPaidBySocialInsurance,
                l.CountsAgainstAnnualLeave,
                l.AllowHalfDay,
                l.AllowHourly,
                l.RequiresAttachment,
                l.MaxDaysPerRequest,
                l.MaxDaysPerYear,
                l.AllowCarryForward,
                l.AttendanceSymbol,
                l.DisplayOrder,
                l.IsActive,
                l.LegalReference,
                l.NeedsHrVerification,
                l.Description))
            .ToListAsync(cancellationToken);

        var statutory = await AttendancePolicyAndShiftEngine.TryResolveStatutoryParameterAsync(_context, new DateOnly(year, 12, 31), cancellationToken)
            ?? await AttendancePolicyAndShiftEngine.TryResolveStatutoryParameterAsync(_context, new DateOnly(year, 1, 1), cancellationToken);

        var balancesQuery = _context.LeaveBalances
            .AsNoTracking()
            .Include(b => b.Employee)
                .ThenInclude(e => e.OrganizationUnit)
            .Include(b => b.Employee)
                .ThenInclude(e => e.Position)
            .Where(b => b.Year == year);

        if (request.UnitId.HasValue && request.UnitId.Value > 0)
        {
            var unit = await _context.OrganizationUnits.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UnitId.Value, cancellationToken);
            if (unit != null && !string.IsNullOrEmpty(unit.TreePath))
                balancesQuery = balancesQuery.Where(b => b.Employee.OrganizationUnit.TreePath!.StartsWith(unit.TreePath));
            else
                balancesQuery = balancesQuery.Where(b => b.Employee.OrganizationUnitId == request.UnitId.Value);
        }

        var balancesRaw = await balancesQuery
            .OrderBy(b => b.Employee.EmployeeCode)
            .Take(60)
            .ToListAsync(cancellationToken);

        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var balances = new List<LeaveBalanceItemDto>();
        foreach (var b in balancesRaw)
        {
            var resolved = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, b.EmployeeId, asOfDate, cancellationToken);
            int senYears = statutory != null
                ? VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(b.Employee.HireDate, year, statutory, resolved.Policy, b.Employee.TerminationDate).SeniorityYears
                : Math.Max(0, year - b.Employee.HireDate.Year);
            var (_, expiredCarry, grossAvail, _) = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(b, resolved.Policy, asOfDate);

            balances.Add(new LeaveBalanceItemDto(
                b.Id,
                b.EmployeeId,
                b.Employee.EmployeeCode,
                b.Employee.FullName,
                b.Employee.OrganizationUnit.UnitName,
                b.Employee.Position.PositionName,
                b.Employee.HireDate,
                senYears,
                b.Year,
                b.StandardEntitlement,
                b.SeniorityExtraDays,
                b.CarriedForwardDays,
                b.UsedCarriedForwardDays,
                Math.Max(b.ExpiredCarryForwardDays, expiredCarry),
                b.CarryForwardExpiryDate,
                b.UsedDays,
                b.PendingDays,
                b.AdvanceUsedDays,
                Math.Max(0m, grossAvail),
                b.CompensatoryHoursBalance,
                b.TerminationPayoutDays,
                b.TerminationPayoutAmount,
                b.IsTerminationSettled));
        }

        var empNameMap = await _context.Employees
            .AsNoTracking()
            .ToDictionaryAsync(e => e.Id, e => e.FullName, cancellationToken);

        var requestsRaw = await _context.LeaveRequests
            .AsNoTracking()
            .Include(r => r.Employee)
                .ThenInclude(e => e.OrganizationUnit)
            .Include(r => r.LeaveType)
            .OrderByDescending(r => r.CreatedAt)
            .Take(80)
            .ToListAsync(cancellationToken);

        var requests = requestsRaw.Select(r => new LeaveRequestItemDto(
            r.Id,
            r.EmployeeId,
            r.Employee.EmployeeCode,
            r.Employee.FullName,
            r.Employee.OrganizationUnit.UnitName,
            r.LeaveTypeId,
            r.LeaveType.LeaveTypeCode,
            r.LeaveType.LeaveTypeName,
            r.LeaveType.AttendanceSymbol,
            r.StartDate,
            r.EndDate,
            r.StartSession,
            r.EndSession,
            r.LeaveDurationMode,
            r.RequestedHours,
            r.TotalDays,
            r.Reason,
            r.AttachmentUrl,
            r.HandoverToEmployeeId.HasValue && empNameMap.TryGetValue(r.HandoverToEmployeeId.Value, out var hn) ? hn : null,
            r.Status,
            r.WorkflowInstanceId,
            r.ApproverComment,
            r.CancelReason,
            r.CreatedAt,
            r.ApprovedAt)).ToList();

        // Xây dựng Lịch nghỉ Team (Team Leave Calendar) trong tháng
        var monthRequests = requestsRaw
            .Where(r => r.Status != "Rejected" && r.Status != "Cancelled" &&
                        r.StartDate <= monthEnd && r.EndDate >= monthStart)
            .ToList();

        var teamEmps = balancesRaw.Select(b => b.Employee).DistinctBy(e => e.Id).Take(30).ToList();
        var teamCalendarRows = new List<TeamLeaveCalendarEmployeeRowDto>();

        foreach (var emp in teamEmps)
        {
            var empReqs = monthRequests.Where(r => r.EmployeeId == emp.Id).ToList();
            var dayCells = new List<TeamLeaveCalendarDayCellDto>();

            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(year, month, d);
                var matchedReq = empReqs.FirstOrDefault(r => date >= r.StartDate && date <= r.EndDate);
                dayCells.Add(new TeamLeaveCalendarDayCellDto(
                    d,
                    date,
                    date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                    matchedReq?.LeaveType.AttendanceSymbol,
                    matchedReq?.LeaveType.LeaveTypeName,
                    matchedReq?.Status));
            }

            teamCalendarRows.Add(new TeamLeaveCalendarEmployeeRowDto(
                emp.Id,
                emp.EmployeeCode,
                emp.FullName,
                emp.OrganizationUnit.UnitName,
                emp.Position.PositionName,
                dayCells));
        }

        var otRequestsRaw = await _context.OvertimeRequests
            .AsNoTracking()
            .Include(o => o.Employee).ThenInclude(e => e.OrganizationUnit)
            .Where(o => o.OvertimeDate.Year == year)
            .OrderByDescending(o => o.OvertimeDate)
            .ThenByDescending(o => o.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        var otRequests = otRequestsRaw.Select(o =>
        {
            var monthlyTotal = otRequestsRaw
                .Where(x => x.EmployeeId == o.EmployeeId && x.OvertimeDate.Month == o.OvertimeDate.Month && x.Status == "Approved")
                .Sum(x => x.ApprovedHours);
            var yearlyTotal = otRequestsRaw
                .Where(x => x.EmployeeId == o.EmployeeId && x.Status == "Approved")
                .Sum(x => x.ApprovedHours);
            return new OvertimeRequestItemDto(
                o.Id,
                o.EmployeeId,
                o.Employee.EmployeeCode,
                o.Employee.FullName,
                o.Employee.OrganizationUnit.UnitName,
                o.OvertimeDate,
                o.StartTime,
                o.EndTime,
                o.RequestedHours,
                o.ApprovedHours,
                o.OvertimeRateType,
                o.Reason,
                o.Status,
                o.WorkflowInstanceId,
                monthlyTotal,
                yearlyTotal);
        }).ToList();

        var timeRequests = await _context.EmployeeTimeRequests
            .AsNoTracking()
            .Include(t => t.Employee).ThenInclude(e => e.OrganizationUnit)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new EmployeeTimeRequestItemDto(
                t.Id,
                t.EmployeeId,
                t.Employee.EmployeeCode,
                t.Employee.FullName,
                t.Employee.OrganizationUnit.UnitName,
                t.RequestType,
                t.SubType,
                t.StartDate,
                t.EndDate,
                t.PlannedTime,
                t.ProposedEndTime,
                t.RequestedMinutes,
                t.LocationOrDestination,
                t.AttachmentUrl,
                t.Reason,
                t.Status,
                t.WorkflowInstanceId,
                t.ApproverComment,
                t.CreatedAt,
                t.ApprovedAt))
            .ToListAsync(cancellationToken);

        var cancellations = await _context.RequestCancellationRequests
            .AsNoTracking()
            .Include(c => c.RequesterEmployee)
            .OrderByDescending(c => c.CreatedAt)
            .Take(40)
            .Select(c => new RequestCancellationItemDto(
                c.Id,
                c.OriginalEntityType,
                c.OriginalEntityId,
                c.RequesterEmployeeId,
                c.RequesterEmployee.FullName,
                c.Reason,
                c.Status,
                c.WorkflowInstanceId,
                c.RestoredBalanceSummary,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        var settlements = await _context.TerminationLeaveSettlements
            .AsNoTracking()
            .Include(s => s.Employee)
            .Where(s => s.SettlementYear == year)
            .OrderByDescending(s => s.TerminationDate)
            .Select(s => new TerminationLeaveSettlementDto(
                s.Id,
                s.EmployeeId,
                s.Employee.EmployeeCode,
                s.Employee.FullName,
                s.TerminationDate,
                s.ProRatedEntitlementDays,
                s.SeniorityExtraDays,
                s.ValidCarryForwardDays,
                s.UsedDays,
                s.UnusedPayableDays,
                s.BaseSalaryUsed,
                s.StandardWorkdaysUsed,
                s.PayoutAmount,
                s.TargetPayrollMonth,
                s.TargetPayrollYear,
                s.Status,
                s.Notes))
            .ToListAsync(cancellationToken);

        var companyReport = statutory != null
            ? await new GetCompanyLeaveBalanceReportQueryHandler(_context).Handle(
                new GetCompanyLeaveBalanceReportQuery(year, asOfDate, request.UnitId), cancellationToken)
            : null;

        return new LeaveManagementOverviewDto(
            year,
            month,
            daysInMonth,
            request.UnitId,
            units,
            activeEmployees,
            leaveTypes,
            balances,
            requests,
            teamCalendarRows,
            requests.Count(r => r.Status == "Pending"),
            requests.Count(r => r.Status == "Approved"),
            balances.Count > 0 ? Math.Round(balances.Average(b => b.RemainingDays), 1) : 0m,
            balances.Count(b => b.SeniorityExtraDays > 0),
            otRequests,
            timeRequests,
            cancellations,
            settlements,
            companyReport,
            statutory?.MaxMonthlyOvertimeHours ?? 0m,
            statutory?.MaxYearlyOvertimeHours ?? 0m);
    }
}

// ============================================================================
// 4. COMMAND: GỬI ĐƠN XIN NGHỈ PHÉP / CHẾ ĐỘ / NGHỈ BÙ QUA WORKFLOW ENGINE
// ============================================================================

public record SubmitLeaveRequestCommand(
    int EmployeeId,
    int LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string StartSession, // FullDay, Morning, Afternoon
    string EndSession,
    int? HandoverToEmployeeId,
    string Reason,
    string LeaveDurationMode = "Days", // Days | HalfDay | Hours
    decimal? RequestedHours = null,
    string? AttachmentUrl = null) : IRequest<int>;

public class SubmitLeaveRequestCommandValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    public SubmitLeaveRequestCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("Vui lòng chọn nhân sự.");
        RuleFor(x => x.LeaveTypeId).GreaterThan(0).WithMessage("Vui lòng chọn loại nghỉ phép / chế độ.");
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Vui lòng nhập lý do xin nghỉ hoặc nội dung công tác.");
    }
}

public class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ICurrentUserService? _currentUser;

    public SubmitLeaveRequestCommandHandler(
        IApplicationDbContext context,
        ISender sender,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LeaveTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy loại nghỉ phép.");

        if (!leaveType.IsActive)
            throw new InvalidOperationException($"Loại nghỉ '{leaveType.LeaveTypeName}' hiện đang tạm ngưng áp dụng.");

        if (leaveType.RequiresAttachment && string.IsNullOrWhiteSpace(request.AttachmentUrl))
            throw new InvalidOperationException($"Loại nghỉ '{leaveType.LeaveTypeName}' bắt buộc phải đính kèm chứng từ/giấy tờ chứng minh.");

        var isHalfDayRequest = string.Equals(request.LeaveDurationMode, "HalfDay", StringComparison.OrdinalIgnoreCase)
            || request.StartSession != "FullDay"
            || request.EndSession != "FullDay";

        if (isHalfDayRequest && !leaveType.AllowHalfDay)
            throw new InvalidOperationException($"Loại nghỉ '{leaveType.LeaveTypeName}' không hỗ trợ đăng ký nghỉ nửa ngày.");

        var isHourlyRequest = string.Equals(request.LeaveDurationMode, "Hours", StringComparison.OrdinalIgnoreCase);
        if (isHourlyRequest && !leaveType.AllowHourly)
            throw new InvalidOperationException($"Loại nghỉ '{leaveType.LeaveTypeName}' không hỗ trợ đăng ký nghỉ theo giờ.");

        var resolvedPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(
            _context, request.EmployeeId, request.StartDate, cancellationToken);
        var holidaysRaw = await _context.Holidays
            .AsNoTracking()
            .Where(h => h.HolidayDate >= request.StartDate && h.HolidayDate <= request.EndDate)
            .ToListAsync(cancellationToken);
        var holidays = AttendancePolicyAndShiftEngine
            .FilterHolidaysForPolicyGroup(holidaysRaw, resolvedPolicy.Policy.PolicyGroupId)
            .Keys
            .ToList();
        var statutory = await AttendancePolicyAndShiftEngine.TryResolveStatutoryParameterAsync(
            _context, request.StartDate, cancellationToken);
        decimal dailyStdHours = statutory?.MaxNormalDailyHours > 0m ? statutory.MaxNormalDailyHours : 8m;

        decimal totalDays;
        if (isHourlyRequest)
        {
            if (!request.RequestedHours.HasValue || request.RequestedHours.Value <= 0m)
                throw new InvalidOperationException("Số giờ xin nghỉ phải lớn hơn 0.");
            if (request.StartDate != request.EndDate)
                throw new InvalidOperationException("Nghỉ theo giờ chỉ áp dụng trong cùng một ngày.");
            totalDays = Math.Round(request.RequestedHours.Value / dailyStdHours, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            totalDays = VietnamAnnualLeavePolicyCalculator.CalculateWorkingLeaveDays(
                request.StartDate,
                request.EndDate,
                request.StartSession,
                request.EndSession,
                holidays.ToHashSet());
        }

        if (totalDays <= 0m)
            throw new InvalidOperationException("Khoảng thời gian chọn trùng vào ngày Chủ Nhật hoặc Ngày Lễ, số ngày nghỉ thực tế = 0.");

        if (leaveType.MaxDaysPerRequest.HasValue && totalDays > leaveType.MaxDaysPerRequest.Value)
            throw new InvalidOperationException(
                $"Loại nghỉ '{leaveType.LeaveTypeName}' tối đa {leaveType.MaxDaysPerRequest.Value:0.#} ngày/lần nghỉ (bạn đang đăng ký {totalDays:0.#} ngày).");

        var isAnnualLeave = leaveType.CountsAgainstAnnualLeave || string.Equals(leaveType.LeaveTypeCode, "ANNUAL", StringComparison.OrdinalIgnoreCase);
        var isCompLeave = string.Equals(leaveType.LeaveTypeCode, "COMP_LEAVE", StringComparison.OrdinalIgnoreCase);

        if (!isAnnualLeave && !isCompLeave && leaveType.MaxDaysPerYear > 0m)
        {
            var usedSameTypeInYear = await _context.LeaveRequests
                .AsNoTracking()
                .Where(r => r.EmployeeId == request.EmployeeId
                    && r.LeaveTypeId == request.LeaveTypeId
                    && r.StartDate.Year == request.StartDate.Year
                    && r.Status != "Rejected"
                    && r.Status != "Cancelled")
                .SumAsync(r => r.TotalDays, cancellationToken);

            if (usedSameTypeInYear + totalDays > leaveType.MaxDaysPerYear)
                throw new InvalidOperationException(
                    $"Đã vượt số ngày tối đa trong năm của '{leaveType.LeaveTypeName}' ({usedSameTypeInYear + totalDays:0.#}/{leaveType.MaxDaysPerYear:0.#} ngày).");
        }

        if (isCompLeave)
        {
            var annualTypeId = await _context.LeaveTypes
                .AsNoTracking()
                .Where(l => l.LeaveTypeCode == "ANNUAL")
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync(cancellationToken) ?? request.LeaveTypeId;

            var compBal = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId && b.LeaveTypeId == annualTypeId && b.Year == request.StartDate.Year, cancellationToken);

            decimal requiredHours = isHourlyRequest && request.RequestedHours.HasValue
                ? request.RequestedHours.Value
                : Math.Round(totalDays * dailyStdHours, 2, MidpointRounding.AwayFromZero);

            decimal currentCompHours = compBal?.CompensatoryHoursBalance ?? 0m;
            if (currentCompHours < requiredHours)
                throw new InvalidOperationException($"Quỹ giờ nghỉ bù không đủ (còn {currentCompHours:0.##}h, cần {requiredHours:0.##}h).");

            if (compBal != null)
                compBal.CompensatoryHoursBalance -= requiredHours;
        }
        else if (isAnnualLeave)
        {
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b =>
                    b.EmployeeId == request.EmployeeId &&
                    b.LeaveTypeId == request.LeaveTypeId &&
                    b.Year == request.StartDate.Year,
                    cancellationToken);

            if (balance != null)
            {
                var (_, expiredCarry, grossAvailable, maxAdvanceAllowed) = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(
                    balance, resolvedPolicy.Policy, request.StartDate);

                if (expiredCarry > balance.ExpiredCarryForwardDays)
                    balance.ExpiredCarryForwardDays = expiredCarry;

                if (grossAvailable + maxAdvanceAllowed < totalDays)
                {
                    throw new InvalidOperationException(
                        $"Số ngày phép năm khả dụng tại {request.StartDate:dd/MM/yyyy} ({Math.Max(0m, grossAvailable):0.#} ngày"
                        + (maxAdvanceAllowed > 0m ? $", tối đa ứng thêm {maxAdvanceAllowed:0.#} ngày" : string.Empty)
                        + $") không đủ để đăng ký {totalDays:0.#} ngày.");
                }

                if (grossAvailable < totalDays)
                {
                    balance.AdvanceUsedDays += (totalDays - Math.Max(0m, grossAvailable));
                }

                balance.PendingDays += totalDays;
                var (_, _, updatedAvail, _) = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(
                    balance, resolvedPolicy.Policy, request.StartDate);
                balance.RemainingDays = Math.Max(0m, updatedAvail);
            }
        }

        var leaveEntity = new LeaveRequest
        {
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            StartSession = request.StartSession,
            EndSession = request.EndSession,
            LeaveDurationMode = isHourlyRequest ? "Hours" : (isHalfDayRequest ? "HalfDay" : "Days"),
            RequestedHours = request.RequestedHours,
            TotalDays = totalDays,
            Reason = request.Reason.Trim(),
            HandoverToEmployeeId = request.HandoverToEmployeeId,
            AttachmentUrl = request.AttachmentUrl?.Trim(),
            Status = "Pending"
        };

        await _context.LeaveRequests.AddAsync(leaveEntity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var wfInstanceId = await _sender.Send(new StartWorkflowInstanceCommand(
            "LeaveRequest",
            leaveEntity.Id,
            request.EmployeeId,
            $"{leaveType.LeaveTypeName} ({totalDays:0.#} ngày: {request.StartDate:dd/MM} - {request.EndDate:dd/MM/yyyy})",
            totalDays,
            "/Leaves"), cancellationToken);

        leaveEntity.WorkflowInstanceId = wfInstanceId;
        if (_currentUser != null && _currentUser.IsAuthenticated)
        {
            var actorId = _currentUser.RequireUserId();
            var actorName = _currentUser.RequireUsername();
            await _context.AuditLogs.AddAsync(new Domain.Entities.SystemEntities.AuditLog
            {
                UserId = actorId,
                Username = actorName,
                ModuleName = "TimeAttendance",
                ActionType = "LeaveRequest_Submit",
                EntityName = nameof(LeaveRequest),
                EntityId = leaveEntity.Id.ToString(),
                NewValuesJson = $"{{\"LeaveTypeCode\":\"{leaveType.LeaveTypeCode}\",\"TotalDays\":{totalDays}}}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorName
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return leaveEntity.Id;
    }
}

// ============================================================================
// 5. COMMAND: TÍNH LẠI QUỸ PHÉP NĂM THEO THÂM NIÊN & KẾT CHUYỂN PHÉP TỒN
// ============================================================================

public record RecalculateAnnualLeaveEntitlementCommand(
    int Year,
    decimal DefaultCarryForwardCap = 5.0m,
    DateOnly? AsOfDate = null) : IRequest<int>;

public class RecalculateAnnualLeaveEntitlementCommandHandler : IRequestHandler<RecalculateAnnualLeaveEntitlementCommand, int>
{
    private readonly IApplicationDbContext _context;

    public RecalculateAnnualLeaveEntitlementCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(RecalculateAnnualLeaveEntitlementCommand request, CancellationToken cancellationToken)
    {
        var annualType = await _context.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LeaveTypeCode == "ANNUAL", cancellationToken)
            ?? throw new InvalidOperationException("Chưa cấu hình loại phép ANNUAL.");

        var yearEnd = new DateOnly(request.Year, 12, 31);
        var yearStart = new DateOnly(request.Year, 1, 1);
        var statutory = await AttendancePolicyAndShiftEngine.TryResolveStatutoryParameterAsync(_context, yearEnd, cancellationToken)
            ?? await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(_context, yearStart, cancellationToken);

        var asOfDate = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation" || e.WorkStatus == "Active")
            .ToListAsync(cancellationToken);

        var existingBalances = await _context.LeaveBalances
            .Where(b => b.Year == request.Year && b.LeaveTypeId == annualType.Id)
            .ToDictionaryAsync(b => b.EmployeeId, cancellationToken);

        int updatedCount = 0;

        foreach (var emp in employees)
        {
            var resolved = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, emp.Id, yearEnd, cancellationToken);
            var (_, standardDays, seniorityExtra) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(
                emp.HireDate, request.Year, statutory, resolved.Policy, emp.TerminationDate);

            decimal cap = resolved.Policy.AnnualLeaveCarryForwardCapDays > 0m
                ? Math.Min(resolved.Policy.AnnualLeaveCarryForwardCapDays, request.DefaultCarryForwardCap)
                : request.DefaultCarryForwardCap;

            int expMonth = Math.Clamp(resolved.Policy.CarryForwardExpiryMonth, 1, 12);
            int expDay = Math.Clamp(resolved.Policy.CarryForwardExpiryDay, 1, DateTime.DaysInMonth(request.Year, expMonth));
            var expiryDate = new DateOnly(request.Year, expMonth, expDay);

            if (existingBalances.TryGetValue(emp.Id, out var bal))
            {
                bal.StandardEntitlement = standardDays;
                bal.SeniorityExtraDays = seniorityExtra;
                bal.CarriedForwardDays = Math.Min(bal.CarriedForwardDays, cap);
                bal.CarryForwardExpiryDate = expiryDate;

                var (_, expiredCarry, avail, _) = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(bal, resolved.Policy, asOfDate);
                bal.ExpiredCarryForwardDays = expiredCarry;
                bal.RemainingDays = Math.Max(0m, avail);
            }
            else
            {
                var prevYearBal = await _context.LeaveBalances
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.EmployeeId == emp.Id && b.LeaveTypeId == annualType.Id && b.Year == request.Year - 1, cancellationToken);

                decimal carried = prevYearBal != null && emp.WorkStatus != "Probation"
                    ? Math.Min(Math.Max(0m, prevYearBal.RemainingDays), cap)
                    : 0m;

                var newBal = new LeaveBalance
                {
                    EmployeeId = emp.Id,
                    LeaveTypeId = annualType.Id,
                    Year = request.Year,
                    StandardEntitlement = standardDays,
                    SeniorityExtraDays = seniorityExtra,
                    CarriedForwardDays = carried,
                    CarryForwardExpiryDate = expiryDate,
                    UsedDays = 0m,
                    PendingDays = 0m
                };
                var (_, expiredCarry, avail, _) = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(newBal, resolved.Policy, asOfDate);
                newBal.ExpiredCarryForwardDays = expiredCarry;
                newBal.RemainingDays = Math.Max(0m, avail);

                await _context.LeaveBalances.AddAsync(newBal, cancellationToken);
            }
            updatedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return updatedCount;
    }
}

