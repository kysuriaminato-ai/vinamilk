using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.TimeAttendance;

// ============================================================================
// CL3: TÍNH LẠI CÔNG CÓ TIẾN ĐỘ, CHỐT CÔNG 3 BƯỚC, ĐIỀU CHỈNH KỲ SAU & CHI TIẾT Ô
// ============================================================================

public interface IAttendanceRecalculationDispatcher
{
    Task<int> EnqueueOrRunAsync(
        int month,
        int year,
        int? organizationUnitId,
        int? employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        string requestedByUsername,
        CancellationToken cancellationToken = default);
}

public interface IAttendanceRecalculationJob
{
    Task ExecuteBatchAsync(int batchId, CancellationToken cancellationToken = default);
}

public sealed record AttendanceRecalculationBatchDto(
    int Id,
    string BatchCode,
    int Month,
    int Year,
    int? OrganizationUnitId,
    int? EmployeeId,
    DateOnly FromDate,
    DateOnly ToDate,
    string Status,
    int TotalEmployees,
    int ProcessedEmployees,
    int ProgressPercent,
    string RequestedByUsername,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Message);

public sealed record PostLockAdjustmentDto(
    long Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    int TargetMonth,
    int TargetYear,
    DateOnly AdjustmentDate,
    int AppliedPayrollMonth,
    int AppliedPayrollYear,
    string AdjustmentType,
    decimal WorkdayDelta,
    decimal OtHours150Delta,
    decimal OtHours200Delta,
    decimal OtHours300Delta,
    decimal NightHoursDelta,
    string Reason,
    string Status,
    string? ApprovedByUsername,
    DateTime? ApprovedAt);

public sealed record AttendanceSymbolDefinitionDto(
    int Id,
    string SymbolTypeCode,
    string SymbolCode,
    string SymbolName,
    string Category,
    decimal WorkdayMultiplier,
    bool IsPaidByCompany,
    bool IsPaidBySocialInsurance,
    string ColorHex,
    string CssClass);

public sealed record AttendanceCellDetailDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    DateOnly WorkDate,
    string? ShiftCode,
    string? ShiftName,
    string StatusCode,
    DateTime? ActualCheckIn,
    DateTime? ActualCheckOut,
    decimal StandardHours,
    decimal WorkingHours,
    decimal WorkdayCount,
    decimal PaidLeaveDays,
    int LateMinutes,
    int EarlyLeaveMinutes,
    decimal NightShiftHours,
    decimal OvertimeHours150Day,
    decimal OvertimeHours150Night,
    decimal OvertimeHours200Day,
    decimal OvertimeHours200Night,
    decimal OvertimeHours300Day,
    decimal OvertimeHours300Night,
    bool IsMissedPunch,
    bool IsManualEdited,
    string? EditReason,
    bool IsMonthLocked,
    IReadOnlyList<AttendanceLogItemDto> Logs,
    IReadOnlyList<string> RelatedRequests,
    IReadOnlyList<PostLockAdjustmentDto> PostLockAdjustments);

// ============================================================================
// 1. ENGINE CHẠY LÔ TÍNH CÔNG QUA AttendanceCalculationEngine & CẬP NHẬT TIẾN ĐỘ
// ============================================================================

public sealed class AttendanceRecalculationService : IAttendanceRecalculationJob
{
    private readonly IApplicationDbContext _context;

    public AttendanceRecalculationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.AttendanceRecalculationBatches
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch == null) return;

        batch.Status = "Running";
        batch.StartedAt = DateTime.UtcNow;
        batch.ProgressPercent = 0;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var empQuery = _context.Employees
                .AsNoTracking()
                .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation");

            if (batch.EmployeeId.HasValue)
            {
                empQuery = empQuery.Where(e => e.Id == batch.EmployeeId.Value);
            }
            else if (batch.OrganizationUnitId.HasValue)
            {
                empQuery = empQuery.Where(e => e.OrganizationUnitId == batch.OrganizationUnitId.Value);
            }

            var targetEmployees = await empQuery.OrderBy(e => e.Id).ToListAsync(cancellationToken);
            batch.TotalEmployees = targetEmployees.Count;
            batch.ProcessedEmployees = 0;
            await _context.SaveChangesAsync(cancellationToken);

            var symbolEntities = await _context.AttendanceSymbolDefinitions
                .AsNoTracking()
                .Where(s => s.IsActive)
                .ToListAsync(cancellationToken);
            var symbolCatalog = symbolEntities.Count > 0
                ? new AttendanceSymbolCatalog(symbolEntities)
                : AttendanceSymbolCatalog.CreateDefault();

            var fromDate = batch.FromDate;
            var toDate = batch.ToDate;
            var statutory = await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(_context, toDate, cancellationToken);

            var allHolidaysInRange = await _context.Holidays
                .AsNoTracking()
                .Where(h => (h.HolidayDate >= fromDate && h.HolidayDate <= toDate)
                    || (h.IsCompensatoryDay && h.OriginalHolidayDate >= fromDate && h.OriginalHolidayDate <= toDate))
                .ToListAsync(cancellationToken);

            var empIds = targetEmployees.Select(e => e.Id).ToList();
            var lockedSummaries = (await _context.MonthlyAttendanceSummaries
                .AsNoTracking()
                .Where(m => m.Month == batch.Month && m.Year == batch.Year && m.IsLocked && empIds.Contains(m.EmployeeId))
                .Select(m => m.EmployeeId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var schedules = await _context.ShiftSchedules
                .AsNoTracking()
                .Include(s => s.WorkShift)
                .Where(s => empIds.Contains(s.EmployeeId) && s.WorkDate >= fromDate && s.WorkDate <= toDate)
                .ToListAsync(cancellationToken);

            var defaultShift = await _context.WorkShifts
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.ShiftCode == "HC", cancellationToken)
                ?? await _context.WorkShifts.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

            var logStart = fromDate.AddDays(-1).ToDateTime(TimeOnly.MinValue);
            var logEnd = toDate.AddDays(1).ToDateTime(TimeOnly.MaxValue);
            var rawLogs = await _context.AttendanceLogs
                .AsNoTracking()
                .Where(l => empIds.Contains(l.EmployeeId) && l.PunchTime >= logStart && l.PunchTime <= logEnd)
                .ToListAsync(cancellationToken);

            var leaves = await _context.LeaveRequests
                .AsNoTracking()
                .Include(l => l.LeaveType)
                .Where(l => empIds.Contains(l.EmployeeId)
                    && l.Status == "Approved"
                    && l.StartDate <= toDate
                    && l.EndDate >= fromDate)
                .ToListAsync(cancellationToken);

            var overtimes = await _context.OvertimeRequests
                .AsNoTracking()
                .Where(o => empIds.Contains(o.EmployeeId)
                    && o.Status == "Approved"
                    && o.OvertimeDate >= fromDate
                    && o.OvertimeDate <= toDate)
                .ToListAsync(cancellationToken);

            var existingDailies = await _context.DailyAttendances
                .Where(d => empIds.Contains(d.EmployeeId) && d.AttendanceDate >= fromDate && d.AttendanceDate <= toDate)
                .ToListAsync(cancellationToken);
            var dailyLookup = existingDailies.ToDictionary(d => (d.EmployeeId, d.AttendanceDate));

            // Lấy các giải trình công đã Approved qua WorkflowInstance
            var candidateDailyIds = existingDailies.Select(d => (int)d.Id).ToList();
            var approvedExplainDailyIds = candidateDailyIds.Count > 0
                ? (await _context.WorkflowInstances
                    .AsNoTracking()
                    .Where(w => w.EntityType == "AttendanceExplanation" && w.Status == "Approved" && candidateDailyIds.Contains(w.EntityId))
                    .Select(w => (long)w.EntityId)
                    .ToListAsync(cancellationToken))
                    .ToHashSet()
                : new HashSet<long>();

            int processed = 0;
            foreach (var emp in targetEmployees)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!lockedSummaries.Contains(emp.Id))
                {
                    var resolvedPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(
                        _context, emp.Id, fromDate, cancellationToken);
                    var empHolidays = AttendancePolicyAndShiftEngine.FilterHolidaysForPolicyGroup(
                        allHolidaysInRange, resolvedPolicy.Policy.PolicyGroupId);
                    var workdaysMask = (resolvedPolicy.Policy.WeeklyWorkdaysMask ?? "Mon,Tue,Wed,Thu,Fri")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var empLogs = rawLogs.Where(l => l.EmployeeId == emp.Id).ToList();

                    for (var date = fromDate; date <= toDate; date = date.AddDays(1))
                    {
                        if (dailyLookup.TryGetValue((emp.Id, date), out var existingDaily) && existingDaily.IsManualEdited)
                        {
                            // Tôn trọng ô đã được HR sửa tay / có ghi nhận thủ công
                            continue;
                        }

                        var sched = schedules.FirstOrDefault(s => s.EmployeeId == emp.Id && s.WorkDate == date);
                        var shift = sched?.WorkShift ?? defaultShift;
                        bool isWeeklyOff = sched?.IsDayOff == true
                            || (sched == null && !workdaysMask.Contains(date.DayOfWeek.ToString()[..3]));

                        // Gom punch cho ngày làm việc (bao gồm đầu sáng hôm sau nếu ca qua đêm)
                        var (winStart, winEnd) = shift != null
                            ? AttendancePolicyAndShiftEngine.GetPrimaryShiftWindow(date, shift)
                            : (date.ToDateTime(new TimeOnly(8, 0)), date.ToDateTime(new TimeOnly(17, 0)));
                        var earlyBuf = Math.Max(120, shift?.MaxEarlyCheckInMinutes ?? 120);
                        var lateBuf = Math.Max(360, shift?.MaxLateCheckOutMinutes ?? 180);

                        var dayPunches = empLogs
                            .Where(l => l.PunchTime >= winStart.AddMinutes(-earlyBuf) && l.PunchTime <= winEnd.AddMinutes(lateBuf))
                            .Select(l => new AttendancePunchInput(l.PunchTime, l.PunchType, l.SourceType, l.GeofenceStatus))
                            .ToList();

                        var dayLeaves = leaves
                            .Where(l => l.EmployeeId == emp.Id && l.StartDate <= date && l.EndDate >= date)
                            .Select(l =>
                            {
                                decimal daysForDate = 1.0m;
                                string session = "FullDay";
                                if (l.StartDate == l.EndDate)
                                {
                                    daysForDate = l.TotalDays < 1.0m ? l.TotalDays : 1.0m;
                                    session = l.StartSession;
                                }
                                else if (date == l.StartDate && l.StartSession is "Afternoon" or "Morning")
                                {
                                    daysForDate = 0.5m;
                                    session = l.StartSession;
                                }
                                else if (date == l.EndDate && l.EndSession is "Morning" or "Afternoon")
                                {
                                    daysForDate = 0.5m;
                                    session = l.EndSession;
                                }

                                bool isTrip = string.Equals(l.LeaveType.LeaveTypeCode, "BUSINESS_TRIP", StringComparison.OrdinalIgnoreCase);
                                return new AttendanceApprovedLeaveInput(
                                    l.LeaveType.LeaveTypeCode,
                                    l.LeaveType.AttendanceSymbol,
                                    session,
                                    daysForDate,
                                    l.LeaveType.IsPaidByCompany,
                                    l.LeaveType.IsPaidBySocialInsurance,
                                    isTrip,
                                    l.Reason);
                            })
                            .ToList();

                        var dayOts = overtimes
                            .Where(o => o.EmployeeId == emp.Id && o.OvertimeDate == date)
                            .Select(o => new AttendanceApprovedOvertimeInput(o.StartTime, o.EndTime, o.ApprovedHours, o.OvertimeRateType, o.CompensationMode))
                            .ToList();

                        AttendanceApprovedExplanationInput? approvedExplain = null;
                        if (existingDaily != null
                            && approvedExplainDailyIds.Contains(existingDaily.Id)
                            && TryParseProposedTimes(existingDaily.EditReason, out var pIn, out var pOut))
                        {
                            approvedExplain = new AttendanceApprovedExplanationInput(pIn, pOut, existingDaily.EditReason);
                        }

                        bool outsideEmployment = date < emp.HireDate
                            || (emp.TerminationDate.HasValue && date >= emp.TerminationDate.Value);

                        var dayHoliday = outsideEmployment ? null : empHolidays.GetValueOrDefault(date);
                        // Nếu ngày lễ trùng ngày nghỉ hằng tuần của nhóm VÀ nhóm đã có ngày nghỉ bù tương ứng ->
                        // không cộng đúp thêm 1 công Lễ ở ngày nghỉ tuần (chỉ tính OT 300% nếu thực tế có quẹt thẻ trực lễ; ngày nghỉ bù được tính 1 công L).
                        if (dayHoliday != null
                            && !dayHoliday.IsCompensatoryDay
                            && (sched?.IsDayOff == true || isWeeklyOff)
                            && dayPunches.Count == 0
                            && empHolidays.Values.Any(c => c.IsCompensatoryDay && c.OriginalHolidayDate == date))
                        {
                            dayHoliday = null;
                        }

                        var calc = AttendanceCalculationEngine.CalculateDaily(new AttendanceDailyCalculationInput(
                            date,
                            shift,
                            sched?.IsDayOff == true || outsideEmployment,
                            isWeeklyOff || outsideEmployment,
                            dayHoliday,
                            outsideEmployment ? Array.Empty<AttendancePunchInput>() : dayPunches,
                            outsideEmployment ? Array.Empty<AttendanceApprovedLeaveInput>() : dayLeaves,
                            outsideEmployment ? Array.Empty<AttendanceApprovedOvertimeInput>() : dayOts,
                            outsideEmployment ? null : approvedExplain,
                            resolvedPolicy.Policy,
                            statutory,
                            symbolCatalog));

                        if (existingDaily == null)
                        {
                            existingDaily = new DailyAttendance
                            {
                                EmployeeId = emp.Id,
                                AttendanceDate = date
                            };
                            await _context.DailyAttendances.AddAsync(existingDaily, cancellationToken);
                            dailyLookup[(emp.Id, date)] = existingDaily;
                        }

                        existingDaily.WorkShiftId = calc.WorkShiftId;
                        existingDaily.CheckInTime = calc.ActualCheckIn;
                        existingDaily.CheckOutTime = calc.ActualCheckOut;
                        existingDaily.AttendanceSymbol = calc.StatusCode;
                        existingDaily.StandardHours = calc.StandardHours;
                        existingDaily.WorkingHours = calc.WorkingHours;
                        existingDaily.WorkdayCount = calc.WorkdayCount;
                        existingDaily.PaidLeaveDays = calc.PaidLeaveDays;
                        existingDaily.UnpaidLeaveDays = calc.UnpaidLeaveDays;
                        existingDaily.LateMinutes = calc.LateMinutes;
                        existingDaily.EarlyLeaveMinutes = calc.EarlyLeaveMinutes;
                        existingDaily.OvertimeHours150 = calc.OvertimeHours150;
                        existingDaily.OvertimeHours200 = calc.OvertimeHours200;
                        existingDaily.OvertimeHours300 = calc.OvertimeHours300;
                        existingDaily.OvertimeHours150Day = calc.OvertimeHours150Day;
                        existingDaily.OvertimeHours150Night = calc.OvertimeHours150Night;
                        existingDaily.OvertimeHours200Day = calc.OvertimeHours200Day;
                        existingDaily.OvertimeHours200Night = calc.OvertimeHours200Night;
                        existingDaily.OvertimeHours300Day = calc.OvertimeHours300Day;
                        existingDaily.OvertimeHours300Night = calc.OvertimeHours300Night;
                        existingDaily.NightShiftHours = calc.NightShiftHours;
                        existingDaily.IsMissedPunch = calc.IsMissedPunch;
                        existingDaily.EditReason = calc.CalculationNotes;
                    }
                }

                processed++;
                batch.ProcessedEmployees = processed;
                batch.ProgressPercent = batch.TotalEmployees > 0
                    ? (int)Math.Round(processed * 100.0 / batch.TotalEmployees)
                    : 100;

                if (processed % 5 == 0 || processed == batch.TotalEmployees)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(_context, batch.Month, batch.Year, cancellationToken);

            batch.Status = "Completed";
            batch.ProgressPercent = 100;
            batch.CompletedAt = DateTime.UtcNow;
            batch.Message = $"Đã tính lại công thành công cho {processed}/{batch.TotalEmployees} nhân sự từ {fromDate:dd/MM} đến {toDate:dd/MM/yyyy}.";

            var sysUserId = await _context.Users
                .AsNoTracking()
                .Where(u => u.Username == SystemJobAccount.Username)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            await _context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = sysUserId,
                Username = SystemJobAccount.Username,
                ActionType = "AttendanceRecalculateBatch",
                ModuleName = "TimeAttendance",
                EntityName = "AttendanceRecalculationBatch",
                EntityId = batch.BatchCode,
                NewValuesJson = $"{{\"month\":{batch.Month},\"year\":{batch.Year},\"processed\":{processed}}}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            batch.Status = "Failed";
            batch.CompletedAt = DateTime.UtcNow;
            batch.Message = $"Lỗi tính công: {ex.Message}";
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static bool TryParseProposedTimes(string? editReason, out TimeOnly checkIn, out TimeOnly checkOut)
    {
        checkIn = default;
        checkOut = default;
        if (string.IsNullOrWhiteSpace(editReason)) return false;

        var marker = "Đề xuất giờ:";
        var idx = editReason.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;

        var rest = editReason[(idx + marker.Length)..].Trim();
        var pipeIdx = rest.IndexOf('|');
        if (pipeIdx > 0) rest = rest[..pipeIdx].Trim();
        var parts = rest.Split('-', StringSplitOptions.TrimEntries);
        return parts.Length == 2
            && TimeOnly.TryParse(parts[0], out checkIn)
            && TimeOnly.TryParse(parts[1], out checkOut);
    }
}

public sealed class InProcessAttendanceRecalculationDispatcher : IAttendanceRecalculationDispatcher
{
    private readonly IApplicationDbContext _context;
    private readonly IAttendanceRecalculationJob _job;

    public InProcessAttendanceRecalculationDispatcher(IApplicationDbContext context, IAttendanceRecalculationJob job)
    {
        _context = context;
        _job = job;
    }

    public async Task<int> EnqueueOrRunAsync(
        int month,
        int year,
        int? organizationUnitId,
        int? employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        string requestedByUsername,
        CancellationToken cancellationToken = default)
    {
        var batch = new AttendanceRecalculationBatch
        {
            BatchCode = $"RECALC-{year}{month:D2}-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            Month = month,
            Year = year,
            OrganizationUnitId = organizationUnitId,
            EmployeeId = employeeId,
            FromDate = fromDate,
            ToDate = toDate,
            Status = "Queued",
            RequestedByUsername = requestedByUsername
        };
        await _context.AttendanceRecalculationBatches.AddAsync(batch, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _job.ExecuteBatchAsync(batch.Id, cancellationToken);
        return batch.Id;
    }
}

// ============================================================================
// 2. COMMANDS & QUERIES: RECALC, CHỐT CÔNG 3 BƯỚC, ĐIỀU CHỈNH KỲ SAU & CHI TIẾT Ô
// ============================================================================

public record StartAttendanceRecalculationCommand(
    int Month,
    int Year,
    int? OrganizationUnitId = null,
    int? EmployeeId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<int>;

public class StartAttendanceRecalculationCommandHandler : IRequestHandler<StartAttendanceRecalculationCommand, int>
{
    private readonly IAttendanceRecalculationDispatcher _dispatcher;
    private readonly ICurrentUserService _currentUser;

    public StartAttendanceRecalculationCommandHandler(
        IAttendanceRecalculationDispatcher dispatcher,
        ICurrentUserService currentUser)
    {
        _dispatcher = dispatcher;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(StartAttendanceRecalculationCommand request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.RequireUsername();
        var start = request.FromDate ?? new DateOnly(request.Year, request.Month, 1);
        var end = request.ToDate ?? new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));
        if (end < start)
            throw new InvalidOperationException("Ngày kết thúc tính công phải lớn hơn hoặc bằng ngày bắt đầu.");

        return await _dispatcher.EnqueueOrRunAsync(
            request.Month,
            request.Year,
            request.OrganizationUnitId,
            request.EmployeeId,
            start,
            end,
            actor,
            cancellationToken);
    }
}

public record GetRecalculationBatchStatusQuery(int BatchId) : IRequest<AttendanceRecalculationBatchDto?>;

public class GetRecalculationBatchStatusQueryHandler : IRequestHandler<GetRecalculationBatchStatusQuery, AttendanceRecalculationBatchDto?>
{
    private readonly IApplicationDbContext _context;

    public GetRecalculationBatchStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceRecalculationBatchDto?> Handle(GetRecalculationBatchStatusQuery request, CancellationToken cancellationToken)
    {
        return await _context.AttendanceRecalculationBatches
            .AsNoTracking()
            .Where(b => b.Id == request.BatchId)
            .Select(b => new AttendanceRecalculationBatchDto(
                b.Id,
                b.BatchCode,
                b.Month,
                b.Year,
                b.OrganizationUnitId,
                b.EmployeeId,
                b.FromDate,
                b.ToDate,
                b.Status,
                b.TotalEmployees,
                b.ProcessedEmployees,
                b.ProgressPercent,
                b.RequestedByUsername,
                b.StartedAt,
                b.CompletedAt,
                b.Message))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public record AdvanceAttendanceClosingCommand(
    int Month,
    int Year,
    string ActionStep, // UnitSubmit | ManagerConfirm | FinalLock
    int? OrganizationUnitId = null,
    bool ForceLockBeforeReviewDeadline = false,
    DateTime? OverrideUtcNow = null) : IRequest<string>;

public class AdvanceAttendanceClosingCommandHandler : IRequestHandler<AdvanceAttendanceClosingCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AdvanceAttendanceClosingCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(AdvanceAttendanceClosingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();
        var nowUtc = request.OverrideUtcNow ?? DateTime.UtcNow;

        var query = _context.MonthlyAttendanceSummaries
            .Include(m => m.Employee)
            .Where(m => m.Month == request.Month && m.Year == request.Year);

        if (request.OrganizationUnitId.HasValue)
            query = query.Where(m => m.Employee.OrganizationUnitId == request.OrganizationUnitId.Value);

        var summaries = await query.ToListAsync(cancellationToken);
        if (summaries.Count == 0)
            throw new InvalidOperationException($"Chưa có dữ liệu tổng hợp bảng công tháng {request.Month:D2}/{request.Year}.");

        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var defaultPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(
            _context, summaries[0].EmployeeId, monthStart, cancellationToken);
        int reviewWindowDays = Math.Max(0, defaultPolicy.Policy.EmployeeReviewWindowDays);

        string message;
        var step = request.ActionStep.Trim();

        switch (step)
        {
            case "UnitSubmit":
                if (summaries.Any(s => s.IsLocked))
                    throw new InvalidOperationException("Bảng công tháng đã khóa cứng.");
                var deadline = nowUtc.AddDays(reviewWindowDays);
                foreach (var s in summaries)
                {
                    s.ClosingStatus = "UnitSubmitted";
                    s.UnitSubmittedBy = username;
                    s.UnitSubmittedAt = nowUtc;
                    s.ReviewWindowDays = reviewWindowDays;
                    s.ReviewDeadlineAt = deadline;
                }
                message = $"HR đơn vị ({username}) đã chốt sơ bộ {summaries.Count} nhân sự tháng {request.Month:D2}/{request.Year}. Nhân viên có {reviewWindowDays} ngày xem & giải trình đến {deadline:dd/MM/yyyy HH:mm}.";
                break;

            case "ManagerConfirm":
                if (summaries.Any(s => s.IsLocked))
                    throw new InvalidOperationException("Bảng công tháng đã khóa cứng.");
                if (summaries.Any(s => s.ClosingStatus == "Open"))
                    throw new InvalidOperationException("Cần HR đơn vị chốt bước 1 (UnitSubmit) trước khi Quản lý xác nhận.");
                foreach (var s in summaries)
                {
                    s.ClosingStatus = "ManagerConfirmed";
                    s.ManagerConfirmedBy = username;
                    s.ManagerConfirmedAt = nowUtc;
                }
                message = $"Quản lý ({username}) đã xác nhận bảng công tháng {request.Month:D2}/{request.Year} ({summaries.Count} nhân sự).";
                break;

            case "FinalLock":
                if (summaries.Any(s => s.ClosingStatus is "Open" or "UnitSubmitted"))
                    throw new InvalidOperationException("Phải hoàn thành 2 bước HR đơn vị chốt → Quản lý xác nhận trước khi HR tổng khóa bảng công.");

                var earliestDeadline = summaries.Select(s => s.ReviewDeadlineAt).Where(d => d.HasValue).Max();
                if (earliestDeadline.HasValue && nowUtc < earliestDeadline.Value && !request.ForceLockBeforeReviewDeadline)
                {
                    throw new InvalidOperationException($"Đang trong thời hạn {reviewWindowDays} ngày nhân viên xem & giải trình (đến {earliestDeadline.Value:dd/MM/yyyy HH:mm}). Không thể khóa sớm trừ khi bật cờ ép khóa.");
                }

                foreach (var s in summaries)
                {
                    s.ClosingStatus = "Locked";
                    s.IsLocked = true;
                    s.LockedBy = username;
                    s.LockedAt = nowUtc;
                }
                message = $"HR tổng ({username}) đã KHÓA CỨNG bảng công tháng {request.Month:D2}/{request.Year} ({summaries.Count} NV). Mọi thay đổi sau này đi qua điều chỉnh kỳ sau.";
                break;

            default:
                throw new InvalidOperationException($"Bước chốt công '{request.ActionStep}' không hợp lệ.");
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ActionType = $"AttendanceClosing_{step}",
            ModuleName = "TimeAttendance",
            EntityName = "MonthlyAttendanceSummary",
            EntityId = $"{request.Year}-{request.Month:D2}",
            NewValuesJson = $"{{\"step\":\"{step}\",\"count\":{summaries.Count}}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        bool hasHolidaysInYear = await _context.Holidays
            .AsNoTracking()
            .AnyAsync(h => h.Year == request.Year, cancellationToken);
        if (!hasHolidaysInYear)
        {
            message += $" [CẢNH BÁO: Năm {request.Year} chưa có danh mục ngày nghỉ lễ trong hệ thống — hãy sinh lịch lễ năm {request.Year} và tính lại công nếu có ngày lễ trong kỳ.]";
        }

        return message;
    }
}

public record CreatePostLockAdjustmentCommand(
    int EmployeeId,
    int TargetMonth,
    int TargetYear,
    DateOnly AdjustmentDate,
    int AppliedPayrollMonth,
    int AppliedPayrollYear,
    string AdjustmentType,
    decimal WorkdayDelta,
    decimal OtHours150Delta,
    decimal OtHours200Delta,
    decimal OtHours300Delta,
    decimal NightHoursDelta,
    string Reason) : IRequest<long>;

public class CreatePostLockAdjustmentCommandHandler : IRequestHandler<CreatePostLockAdjustmentCommand, long>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreatePostLockAdjustmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<long> Handle(CreatePostLockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Điều chỉnh công kỳ sau bắt buộc phải nhập lý do.");

        var isTargetLocked = await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .AnyAsync(m => m.EmployeeId == request.EmployeeId
                && m.Month == request.TargetMonth
                && m.Year == request.TargetYear
                && m.IsLocked, cancellationToken);

        if (!isTargetLocked)
            throw new InvalidOperationException($"Bảng công tháng {request.TargetMonth:D2}/{request.TargetYear} chưa khóa, có thể sửa/tính lại trực tiếp mà không cần phiếu điều chỉnh kỳ sau.");

        if (request.AppliedPayrollYear * 100 + request.AppliedPayrollMonth <= request.TargetYear * 100 + request.TargetMonth)
            throw new InvalidOperationException("Kỳ lương áp dụng truy lĩnh/truy thu phải là kỳ sau kỳ công đã khóa.");

        var adj = new PostLockAttendanceAdjustment
        {
            EmployeeId = request.EmployeeId,
            TargetMonth = request.TargetMonth,
            TargetYear = request.TargetYear,
            AdjustmentDate = request.AdjustmentDate,
            AppliedPayrollMonth = request.AppliedPayrollMonth,
            AppliedPayrollYear = request.AppliedPayrollYear,
            AdjustmentType = request.AdjustmentType,
            WorkdayDelta = request.WorkdayDelta,
            OtHours150Delta = request.OtHours150Delta,
            OtHours200Delta = request.OtHours200Delta,
            OtHours300Delta = request.OtHours300Delta,
            NightHoursDelta = request.NightHoursDelta,
            Reason = request.Reason.Trim(),
            Status = "Pending"
        };

        await _context.PostLockAttendanceAdjustments.AddAsync(adj, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ActionType = "CreatePostLockAdjustment",
            ModuleName = "TimeAttendance",
            EntityName = "PostLockAttendanceAdjustment",
            EntityId = adj.Id.ToString(),
            NewValuesJson = $"{{\"empId\":{request.EmployeeId},\"target\":\"{request.TargetMonth:D2}/{request.TargetYear}\",\"apply\":\"{request.AppliedPayrollMonth:D2}/{request.AppliedPayrollYear}\",\"workdayDelta\":{request.WorkdayDelta}}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return adj.Id;
    }
}

public record DecidePostLockAdjustmentCommand(
    long AdjustmentId,
    bool Approve,
    string? Comment = null) : IRequest<string>;

public class DecidePostLockAdjustmentCommandHandler : IRequestHandler<DecidePostLockAdjustmentCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DecidePostLockAdjustmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(DecidePostLockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var adj = await _context.PostLockAttendanceAdjustments
            .FirstOrDefaultAsync(a => a.Id == request.AdjustmentId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu điều chỉnh công kỳ sau.");

        adj.Status = request.Approve ? "Approved" : "Rejected";
        adj.ApprovedByUsername = username;
        adj.ApprovedAt = DateTime.UtcNow;
        adj.ApproverComment = request.Comment?.Trim();

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ActionType = request.Approve ? "ApprovePostLockAdjustment" : "RejectPostLockAdjustment",
            ModuleName = "TimeAttendance",
            EntityName = "PostLockAttendanceAdjustment",
            EntityId = adj.Id.ToString(),
            NewValuesJson = $"{{\"status\":\"{adj.Status}\",\"comment\":\"{adj.ApproverComment}\"}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return request.Approve
            ? $"Đã DUYỆT phiếu điều chỉnh kỳ sau #{adj.Id} (cộng vào kỳ {adj.AppliedPayrollMonth:D2}/{adj.AppliedPayrollYear})."
            : $"Đã TỪ CHỐI phiếu điều chỉnh kỳ sau #{adj.Id}.";
    }
}

public record UpsertAttendanceSymbolCommand(
    string SymbolTypeCode,
    string SymbolCode,
    string SymbolName,
    string Category,
    decimal WorkdayMultiplier,
    bool IsPaidByCompany,
    bool IsPaidBySocialInsurance) : IRequest<int>;

public class UpsertAttendanceSymbolCommandHandler : IRequestHandler<UpsertAttendanceSymbolCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpsertAttendanceSymbolCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(UpsertAttendanceSymbolCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var existing = await _context.AttendanceSymbolDefinitions
            .FirstOrDefaultAsync(s => s.SymbolTypeCode == request.SymbolTypeCode.Trim(), cancellationToken);

        if (existing == null)
        {
            existing = new AttendanceSymbolDefinition
            {
                SymbolTypeCode = request.SymbolTypeCode.Trim(),
                SymbolCode = request.SymbolCode.Trim(),
                SymbolName = request.SymbolName.Trim(),
                Category = request.Category.Trim(),
                WorkdayMultiplier = request.WorkdayMultiplier,
                IsPaidByCompany = request.IsPaidByCompany,
                IsPaidBySocialInsurance = request.IsPaidBySocialInsurance,
                IsActive = true
            };
            await _context.AttendanceSymbolDefinitions.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.SymbolCode = request.SymbolCode.Trim();
            existing.SymbolName = request.SymbolName.Trim();
            existing.Category = request.Category.Trim();
            existing.WorkdayMultiplier = request.WorkdayMultiplier;
            existing.IsPaidByCompany = request.IsPaidByCompany;
            existing.IsPaidBySocialInsurance = request.IsPaidBySocialInsurance;
            existing.IsActive = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ActionType = "UpsertAttendanceSymbol",
            ModuleName = "TimeAttendance",
            EntityName = "AttendanceSymbolDefinition",
            EntityId = existing.SymbolTypeCode,
            NewValuesJson = $"{{\"code\":\"{existing.SymbolCode}\",\"multiplier\":{existing.WorkdayMultiplier}}}",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return existing.Id;
    }
}

public record GetAttendanceCellDetailsQuery(int EmployeeId, DateOnly WorkDate) : IRequest<AttendanceCellDetailDto?>;

public class GetAttendanceCellDetailsQueryHandler : IRequestHandler<GetAttendanceCellDetailsQuery, AttendanceCellDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAttendanceCellDetailsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceCellDetailDto?> Handle(GetAttendanceCellDetailsQuery request, CancellationToken cancellationToken)
    {
        var emp = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);
        if (emp == null) return null;

        var daily = await _context.DailyAttendances
            .AsNoTracking()
            .Include(d => d.WorkShift)
            .FirstOrDefaultAsync(d => d.EmployeeId == request.EmployeeId && d.AttendanceDate == request.WorkDate, cancellationToken);

        var sched = await _context.ShiftSchedules
            .AsNoTracking()
            .Include(s => s.WorkShift)
            .FirstOrDefaultAsync(s => s.EmployeeId == request.EmployeeId && s.WorkDate == request.WorkDate, cancellationToken);

        var isLocked = await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .AnyAsync(m => m.EmployeeId == request.EmployeeId
                && m.Month == request.WorkDate.Month
                && m.Year == request.WorkDate.Year
                && m.IsLocked, cancellationToken);

        var logStart = request.WorkDate.AddDays(-1).ToDateTime(TimeOnly.MinValue);
        var logEnd = request.WorkDate.AddDays(1).ToDateTime(TimeOnly.MaxValue);
        var logs = await _context.AttendanceLogs
            .AsNoTracking()
            .Where(l => l.EmployeeId == request.EmployeeId && l.PunchTime >= logStart && l.PunchTime <= logEnd)
            .OrderBy(l => l.PunchTime)
            .Select(l => new AttendanceLogItemDto(
                l.Id,
                l.EmployeeId,
                emp.EmployeeCode,
                emp.FullName,
                l.PunchTime,
                l.PunchType,
                l.SourceType,
                l.DeviceId,
                l.Latitude,
                l.Longitude,
                l.LocationAddress,
                l.IpAddress,
                l.PhotoUrl,
                l.EnteredByUsername,
                l.ManualReason,
                l.GeofenceStatus))
            .ToListAsync(cancellationToken);

        var leaves = await _context.LeaveRequests
            .AsNoTracking()
            .Include(l => l.LeaveType)
            .Where(l => l.EmployeeId == request.EmployeeId
                && l.StartDate <= request.WorkDate
                && l.EndDate >= request.WorkDate)
            .ToListAsync(cancellationToken);

        var ots = await _context.OvertimeRequests
            .AsNoTracking()
            .Where(o => o.EmployeeId == request.EmployeeId && o.OvertimeDate == request.WorkDate)
            .ToListAsync(cancellationToken);

        var relatedRequests = new List<string>();
        foreach (var l in leaves)
            relatedRequests.Add($"[Nghỉ {l.LeaveType.LeaveTypeName} - {l.Status}] {l.StartSession}/{l.EndSession} ({l.Reason})");
        foreach (var o in ots)
            relatedRequests.Add($"[Đơn OT {o.OvertimeRateType} - {o.Status}] {o.StartTime:HH\\:mm}-{o.EndTime:HH\\:mm} ({o.ApprovedHours:0.#}h)");

        var postLockAdjs = await _context.PostLockAttendanceAdjustments
            .AsNoTracking()
            .Where(a => a.EmployeeId == request.EmployeeId && a.AdjustmentDate == request.WorkDate)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PostLockAdjustmentDto(
                a.Id, a.EmployeeId, emp.EmployeeCode, emp.FullName,
                a.TargetMonth, a.TargetYear, a.AdjustmentDate,
                a.AppliedPayrollMonth, a.AppliedPayrollYear,
                a.AdjustmentType, a.WorkdayDelta,
                a.OtHours150Delta, a.OtHours200Delta, a.OtHours300Delta, a.NightHoursDelta,
                a.Reason, a.Status, a.ApprovedByUsername, a.ApprovedAt))
            .ToListAsync(cancellationToken);

        var shift = daily?.WorkShift ?? sched?.WorkShift;

        return new AttendanceCellDetailDto(
            emp.Id,
            emp.EmployeeCode,
            emp.FullName,
            request.WorkDate,
            shift?.ShiftCode,
            shift?.ShiftName,
            daily?.AttendanceSymbol ?? "KL",
            daily?.CheckInTime,
            daily?.CheckOutTime,
            daily?.StandardHours ?? shift?.StandardHours ?? 8m,
            daily?.WorkingHours ?? 0m,
            daily?.WorkdayCount ?? 0m,
            daily?.PaidLeaveDays ?? 0m,
            daily?.LateMinutes ?? 0,
            daily?.EarlyLeaveMinutes ?? 0,
            daily?.NightShiftHours ?? 0m,
            daily?.OvertimeHours150Day ?? 0m,
            daily?.OvertimeHours150Night ?? 0m,
            daily?.OvertimeHours200Day ?? 0m,
            daily?.OvertimeHours200Night ?? 0m,
            daily?.OvertimeHours300Day ?? 0m,
            daily?.OvertimeHours300Night ?? 0m,
            daily?.IsMissedPunch ?? false,
            daily?.IsManualEdited ?? false,
            daily?.EditReason,
            isLocked,
            logs,
            relatedRequests,
            postLockAdjs);
    }
}
