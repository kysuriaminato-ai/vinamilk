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
    decimal MaxDaysPerYear,
    bool AllowCarryForward,
    string AttendanceSymbol);

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
    DateOnly? CarryForwardExpiryDate,
    decimal UsedDays,
    decimal PendingDays,
    decimal RemainingDays);

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
    decimal TotalDays,
    string? Reason,
    string? HandoverEmployeeName,
    string Status,
    int? WorkflowInstanceId,
    string? ApproverComment,
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
    int TotalEmployeesWithSeniorityBonus);

// ============================================================================
// 2. BỘ TÍNH PHÉP NĂM THEO THÂM NIÊN (ĐIỀU 113 & 114 BLLĐ 2019)
// ============================================================================

public static class VietnamAnnualLeavePolicyCalculator
{
    public static (int SeniorityYears, decimal StandardEntitlement, decimal SeniorityExtraDays) CalculateEntitlement(
        DateOnly hireDate,
        int targetYear)
    {
        var refDate = new DateOnly(targetYear, 12, 31);
        if (hireDate > refDate)
            return (0, 0m, 0m);

        int seniorityYears = Math.Max(0, targetYear - hireDate.Year);
        if (hireDate.Month > 6 && seniorityYears > 0)
            seniorityYears--;

        // Điều 114 Bộ luật Lao động 2019: Cứ đủ 05 năm làm việc cho một người sử dụng lao động thì được tăng thêm 01 ngày phép
        decimal extraDays = seniorityYears / 5;

        // Nếu vào làm trong năm targetYear thì tính theo tỷ lệ số tháng làm việc (1 tháng = 1 ngày phép)
        decimal standardDays = 12m;
        if (hireDate.Year == targetYear)
        {
            standardDays = Math.Max(1m, 12 - hireDate.Month + 1);
        }

        return (seniorityYears, standardDays, extraDays);
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
            .OrderBy(l => l.Id)
            .Select(l => new LeaveTypeDto(
                l.Id,
                l.LeaveTypeCode,
                l.LeaveTypeName,
                l.IsPaidByCompany,
                l.IsPaidBySocialInsurance,
                l.MaxDaysPerYear,
                l.AllowCarryForward,
                l.AttendanceSymbol))
            .ToListAsync(cancellationToken);

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
            .Take(35)
            .ToListAsync(cancellationToken);

        var balances = balancesRaw.Select(b =>
        {
            var (senYears, _, _) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(b.Employee.HireDate, year);
            return new LeaveBalanceItemDto(
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
                b.CarryForwardExpiryDate ?? new DateOnly(year, 3, 31),
                b.UsedDays,
                b.PendingDays,
                b.RemainingDays);
        }).ToList();

        var empNameMap = await _context.Employees
            .AsNoTracking()
            .ToDictionaryAsync(e => e.Id, e => e.FullName, cancellationToken);

        var requestsRaw = await _context.LeaveRequests
            .AsNoTracking()
            .Include(r => r.Employee)
                .ThenInclude(e => e.OrganizationUnit)
            .Include(r => r.LeaveType)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
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
            r.TotalDays,
            r.Reason,
            r.HandoverToEmployeeId.HasValue && empNameMap.TryGetValue(r.HandoverToEmployeeId.Value, out var hn) ? hn : null,
            r.Status,
            r.WorkflowInstanceId,
            r.ApproverComment,
            r.CreatedAt,
            r.ApprovedAt)).ToList();

        // Xây dựng Lịch nghỉ Team (Team Leave Calendar) trong tháng
        var monthRequests = requestsRaw
            .Where(r => r.Status != "Rejected" && r.Status != "Cancelled" &&
                        r.StartDate <= monthEnd && r.EndDate >= monthStart)
            .ToList();

        var teamEmps = balancesRaw.Select(b => b.Employee).DistinctBy(e => e.Id).Take(20).ToList();
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
            balances.Count > 0 ? Math.Round(balances.Average(b => b.RemainingDays), 1) : 12m,
            balances.Count(b => b.SeniorityExtraDays > 0));
    }
}

// ============================================================================
// 4. COMMAND: GỬI ĐƠN XIN NGHỈ PHÉP / CÔNG TÁC QUA WORKFLOW ENGINE
// ============================================================================

public record SubmitLeaveRequestCommand(
    int EmployeeId,
    int LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string StartSession, // FullDay, Morning, Afternoon
    string EndSession,
    int? HandoverToEmployeeId,
    string Reason) : IRequest<int>;

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

    public SubmitLeaveRequestCommandHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<int> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.LeaveTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy loại nghỉ phép.");

        var holidays = await _context.Holidays
            .AsNoTracking()
            .Where(h => h.HolidayDate >= request.StartDate && h.HolidayDate <= request.EndDate)
            .Select(h => h.HolidayDate)
            .ToListAsync(cancellationToken);

        var totalDays = VietnamAnnualLeavePolicyCalculator.CalculateWorkingLeaveDays(
            request.StartDate,
            request.EndDate,
            request.StartSession,
            request.EndSession,
            holidays.ToHashSet());

        if (totalDays <= 0m)
            throw new InvalidOperationException("Khoảng thời gian chọn trùng vào ngày Chủ Nhật hoặc Ngày Lễ, số ngày nghỉ thực tế = 0.");

        // Kiểm tra hạn mức quỹ phép năm nếu đăng ký Nghỉ phép năm (ANNUAL)
        if (leaveType.LeaveTypeCode == "ANNUAL")
        {
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b =>
                    b.EmployeeId == request.EmployeeId &&
                    b.LeaveTypeId == request.LeaveTypeId &&
                    b.Year == request.StartDate.Year,
                    cancellationToken);

            if (balance != null)
            {
                if (balance.RemainingDays < totalDays)
                {
                    throw new InvalidOperationException(
                        $"Số ngày phép năm còn lại ({balance.RemainingDays:0.#} ngày) không đủ để đăng ký {totalDays:0.#} ngày.");
                }

                balance.PendingDays += totalDays;
                balance.RemainingDays = Math.Max(0m,
                    balance.StandardEntitlement + balance.SeniorityExtraDays + balance.CarriedForwardDays - balance.UsedDays - balance.PendingDays);
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
            TotalDays = totalDays,
            Reason = request.Reason.Trim(),
            HandoverToEmployeeId = request.HandoverToEmployeeId,
            Status = "Pending"
        };

        await _context.LeaveRequests.AddAsync(leaveEntity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Khởi tạo luồng phê duyệt Workflow Engine (Realtime SignalR + Email)
        var wfInstanceId = await _sender.Send(new StartWorkflowInstanceCommand(
            "LeaveRequest",
            leaveEntity.Id,
            request.EmployeeId,
            $"{leaveType.LeaveTypeName} ({totalDays:0.#} ngày: {request.StartDate:dd/MM} - {request.EndDate:dd/MM/yyyy})",
            totalDays,
            "/Leaves"), cancellationToken);

        leaveEntity.WorkflowInstanceId = wfInstanceId;
        await _context.SaveChangesAsync(cancellationToken);

        return leaveEntity.Id;
    }
}

// ============================================================================
// 5. COMMAND: TÍNH LẠI QUỸ PHÉP NĂM THEO THÂM NIÊN & KẾT CHUYỂN PHÉP TỒN
// ============================================================================

public record RecalculateAnnualLeaveEntitlementCommand(
    int Year,
    decimal DefaultCarryForwardCap = 5.0m) : IRequest<int>;

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

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation")
            .ToListAsync(cancellationToken);

        var existingBalances = await _context.LeaveBalances
            .Where(b => b.Year == request.Year && b.LeaveTypeId == annualType.Id)
            .ToDictionaryAsync(b => b.EmployeeId, cancellationToken);

        int updatedCount = 0;
        var expiryDate = new DateOnly(request.Year, 3, 31);

        foreach (var emp in employees)
        {
            var (_, standardDays, seniorityExtra) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(emp.HireDate, request.Year);

            if (existingBalances.TryGetValue(emp.Id, out var bal))
            {
                bal.StandardEntitlement = standardDays;
                bal.SeniorityExtraDays = seniorityExtra;
                bal.CarriedForwardDays = Math.Min(bal.CarriedForwardDays, request.DefaultCarryForwardCap);
                bal.CarryForwardExpiryDate = expiryDate;
                bal.RemainingDays = Math.Max(0m,
                    bal.StandardEntitlement + bal.SeniorityExtraDays + bal.CarriedForwardDays - bal.UsedDays - bal.PendingDays);
            }
            else
            {
                var carried = emp.WorkStatus == "Official" ? 2.0m : 0m;
                await _context.LeaveBalances.AddAsync(new LeaveBalance
                {
                    EmployeeId = emp.Id,
                    LeaveTypeId = annualType.Id,
                    Year = request.Year,
                    StandardEntitlement = standardDays,
                    SeniorityExtraDays = seniorityExtra,
                    CarriedForwardDays = carried,
                    CarryForwardExpiryDate = expiryDate,
                    UsedDays = 0m,
                    PendingDays = 0m,
                    RemainingDays = standardDays + seniorityExtra + carried
                }, cancellationToken);
            }
            updatedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return updatedCount;
    }
}

