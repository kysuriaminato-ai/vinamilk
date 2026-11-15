using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.TimeAttendance;

// ============================================================================
// 1. DTOs CHO CA LÀM VIỆC, XẾP CA KÉO THẢ & NGÀY LỄ TẾT
// ============================================================================

public record WorkShiftItemDto(
    int Id,
    string ShiftCode,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    TimeOnly? BreakStartTime,
    TimeOnly? BreakEndTime,
    bool IsOvernight,
    decimal StandardHours,
    decimal WorkdayValue,
    decimal NightShiftAllowanceRate,
    int GraceLateMinutes,
    int AssignedSchedulesCount,
    string BadgeColorClass,
    string ShiftType = "Fixed",
    TimeOnly? CoreStartTime = null,
    TimeOnly? CoreEndTime = null,
    TimeOnly? SplitStartTime2 = null,
    TimeOnly? SplitEndTime2 = null,
    bool IsBreakPaid = false,
    int MaxEarlyCheckInMinutes = 120,
    int MaxLateCheckOutMinutes = 180);

public record HolidayItemDto(
    int Id,
    DateOnly HolidayDate,
    string DayOfWeekVi,
    string HolidayName,
    int Year,
    bool IsPaidHoliday,
    decimal OvertimeMultiplier,
    bool IsCompensatoryDay = false,
    DateOnly? OriginalHolidayDate = null,
    string? LegalReference = null,
    bool NeedsHrVerification = true);

public record ShiftCellDto(
    long? ScheduleId,
    DateOnly WorkDate,
    int? WorkShiftId,
    string ShiftCode,
    string ShiftName,
    string TimeRangeText,
    bool IsDayOff,
    bool IsHoliday,
    string? HolidayName,
    string BadgeColorClass,
    string? ConflictWarning = null);

public record EmployeeShiftRowDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    string PositionName,
    IReadOnlyList<ShiftCellDto> DayCells);

public record PolicyGroupSummaryDto(
    int Id,
    string PolicyCode,
    string PolicyName,
    bool IsCompanyDefault,
    int GraceLateMinutes,
    int GraceEarlyMinutes,
    int RoundingIntervalMinutes,
    string RoundingMode,
    int HalfDayLateThresholdMinutes,
    int ZeroDayLateThresholdMinutes,
    int MaxMissedPunchExplanationsPerMonth,
    string MissedPunchOverQuotaAction,
    decimal MissedPunchPenaltyAmount,
    bool RequireApprovedOvertimeRequest,
    string WeeklyWorkdaysMask,
    decimal? MaxWeeklyWorkingHoursOverride,
    int AssignmentCount);

public record ShiftRotationPatternDto(
    int Id,
    string PatternCode,
    string PatternName,
    int CycleDays,
    int CrewCount,
    string ShiftSequenceCsv,
    int CrewOffsetDays);

public record ShiftSwapRequestItemDto(
    int Id,
    int RequesterEmployeeId,
    string RequesterName,
    DateOnly RequesterWorkDate,
    int TargetEmployeeId,
    string TargetName,
    DateOnly TargetWorkDate,
    string Reason,
    string Status,
    string? ApproverComment,
    DateTime CreatedAt);

public record AssignScheduleResultDto(
    int AffectedCount,
    IReadOnlyList<ShiftConflictWarningDto> Warnings);

public record ShiftAndScheduleOverviewDto(
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    int SelectedYear,
    int? SelectedUnitId,
    IReadOnlyList<DateOnly> WeekDates,
    IReadOnlyList<WorkShiftItemDto> WorkShifts,
    IReadOnlyList<HolidayItemDto> Holidays,
    IReadOnlyList<EmployeeShiftRowDto> RosterRows,
    IReadOnlyList<Workflows.SimpleLookupDto> Units,
    int TotalFactoryShiftCount,
    int TotalNightShiftCount,
    int TotalHolidayDaysInYear,
    IReadOnlyList<ShiftConflictWarningDto>? ConflictWarnings = null,
    IReadOnlyList<PolicyGroupSummaryDto>? PolicyGroups = null,
    IReadOnlyList<ShiftRotationPatternDto>? RotationPatterns = null,
    IReadOnlyList<ShiftSwapRequestItemDto>? ShiftSwapRequests = null,
    IReadOnlyList<Workflows.SimpleLookupDto>? EmployeesLookup = null,
    IReadOnlyList<Workflows.SimpleLookupDto>? PositionsLookup = null,
    AttendanceStatutoryParameter? StatutoryParameter = null);

// ============================================================================
// 2. AUTO-SEED NGÀY LỄ VIỆT NAM & LỊCH PHÂN CA MẪU
// ============================================================================

public static class ShiftAndHolidaySeeder
{
    public static async Task EnsureHolidaysAndSampleSchedulesAsync(
        IApplicationDbContext context,
        int year,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        if (!await context.Holidays.AnyAsync(h => h.Year == year, cancellationToken))
        {
            var holidays = new List<Holiday>
            {
                new() { HolidayDate = new DateOnly(year, 1, 1), HolidayName = "Tết Dương lịch", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.a BLLĐ 2019", NeedsHrVerification = false },
                new() { HolidayDate = new DateOnly(year, 2, 16), HolidayName = "Nghỉ Tết Nguyên đán (29 Tết)", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 2019 (lịch Âm)", NeedsHrVerification = true },
                new() { HolidayDate = new DateOnly(year, 2, 17), HolidayName = "Mùng 1 Tết Nguyên đán", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 2019 (lịch Âm)", NeedsHrVerification = true },
                new() { HolidayDate = new DateOnly(year, 2, 18), HolidayName = "Mùng 2 Tết Nguyên đán", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 2019 (lịch Âm)", NeedsHrVerification = true },
                new() { HolidayDate = new DateOnly(year, 2, 19), HolidayName = "Mùng 3 Tết Nguyên đán", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 2019 (lịch Âm)", NeedsHrVerification = true },
                new() { HolidayDate = new DateOnly(year, 2, 20), HolidayName = "Mùng 4 Tết Nguyên đán", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 2019 (lịch Âm)", NeedsHrVerification = true },
                new() { HolidayDate = new DateOnly(year, 4, 26), HolidayName = "Giỗ Tổ Hùng Vương (10/3 Âm lịch)", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.e BLLĐ 2019 (lịch Âm)", NeedsHrVerification = true },
                new() { HolidayDate = new DateOnly(year, 4, 30), HolidayName = "Ngày Giải phóng miền Nam 30/4", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.c BLLĐ 2019", NeedsHrVerification = false },
                new() { HolidayDate = new DateOnly(year, 5, 1), HolidayName = "Ngày Quốc tế Lao động 1/5", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.d BLLĐ 2019", NeedsHrVerification = false },
                new() { HolidayDate = new DateOnly(year, 9, 2), HolidayName = "Quốc khánh nước CHXHCN Việt Nam 2/9", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.đ BLLĐ 2019", NeedsHrVerification = false },
                new() { HolidayDate = new DateOnly(year, 9, 3), HolidayName = "Nghỉ liền kề Quốc khánh 2/9 (Dự kiến sau - HR xác nhận 01/09 hoặc 03/09)", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.đ BLLĐ 2019 (chọn 01/09 trước hoặc 03/09 sau theo QĐ Thủ tướng)", NeedsHrVerification = true }
            };
            await context.Holidays.AddRangeAsync(holidays, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var weekEnd = weekStart.AddDays(6);
        if (!await context.ShiftSchedules.AnyAsync(s => s.WorkDate >= weekStart && s.WorkDate <= weekEnd, cancellationToken))
        {
            var shifts = await context.WorkShifts.AsNoTracking().ToListAsync(cancellationToken);
            if (shifts.Count == 0)
                return;

            var hcShift = shifts.FirstOrDefault(s => s.ShiftCode == "HC") ?? shifts[0];
            var ca1Shift = shifts.FirstOrDefault(s => s.ShiftCode == "CA1") ?? hcShift;
            var ca2Shift = shifts.FirstOrDefault(s => s.ShiftCode == "CA2") ?? hcShift;
            var ca3Shift = shifts.FirstOrDefault(s => s.ShiftCode == "CA3") ?? hcShift;

            var employees = await context.Employees
                .AsNoTracking()
                .Include(e => e.OrganizationUnit)
                .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation")
                .OrderBy(e => e.EmployeeCode)
                .Take(25)
                .ToListAsync(cancellationToken);

            var newSchedules = new List<ShiftSchedule>();
            int empIdx = 0;

            foreach (var emp in employees)
            {
                bool isFactoryOrFarm = emp.OrganizationUnit.UnitType is "Factory" or "Farm";

                for (int d = 0; d < 7; d++)
                {
                    var date = weekStart.AddDays(d);
                    bool isSunday = date.DayOfWeek == DayOfWeek.Sunday;

                    int shiftId = hcShift.Id;
                    if (isFactoryOrFarm)
                    {
                        var rotation = (empIdx + d) % 3;
                        shiftId = rotation switch
                        {
                            0 => ca1Shift.Id,
                            1 => ca2Shift.Id,
                            _ => ca3Shift.Id
                        };
                    }

                    newSchedules.Add(new ShiftSchedule
                    {
                        EmployeeId = emp.Id,
                        WorkShiftId = shiftId,
                        WorkDate = date,
                        IsDayOff = isSunday,
                        Notes = isSunday ? "Nghỉ cuối tuần" : "Lịch phân ca tiêu chuẩn"
                    });
                }
                empIdx++;
            }

            await context.ShiftSchedules.AddRangeAsync(newSchedules, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public static string GetShiftBadgeColor(string shiftCode, bool isDayOff)
    {
        if (isDayOff) return "bg-secondary-subtle text-secondary border-secondary-subtle";
        return shiftCode.ToUpperInvariant() switch
        {
            "HC" => "bg-primary-subtle text-primary border-primary-subtle",
            "CA1" => "bg-success-subtle text-success border-success-subtle",
            "CA2" => "bg-warning-subtle text-warning-emphasis border-warning-subtle",
            "CA3" => "bg-dark text-white border-dark",
            _ => "bg-info-subtle text-info-emphasis border-info-subtle"
        };
    }

    public static string GetDayOfWeekVi(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday => "Thứ Hai",
        DayOfWeek.Tuesday => "Thứ Ba",
        DayOfWeek.Wednesday => "Thứ Tư",
        DayOfWeek.Thursday => "Thứ Năm",
        DayOfWeek.Friday => "Thứ Sáu",
        DayOfWeek.Saturday => "Thứ Bảy",
        DayOfWeek.Sunday => "Chủ Nhật",
        _ => ""
    };
}

// ============================================================================
// 3. QUERY: LẤY DỮ LIỆU CA LÀM VIỆC, LỊCH XẾP CA KÉO THẢ & NGÀY LỄ
// ============================================================================

public record GetShiftAndScheduleOverviewQuery(
    DateOnly? WeekStart = null,
    int? UnitId = null,
    int? Year = null) : IRequest<ShiftAndScheduleOverviewDto>;

public class GetShiftAndScheduleOverviewQueryHandler : IRequestHandler<GetShiftAndScheduleOverviewQuery, ShiftAndScheduleOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetShiftAndScheduleOverviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShiftAndScheduleOverviewDto> Handle(GetShiftAndScheduleOverviewQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        int diffToMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var weekStart = request.WeekStart ?? today.AddDays(-diffToMonday);
        var weekEnd = weekStart.AddDays(6);
        var selectedYear = request.Year ?? weekStart.Year;

        await ShiftAndHolidaySeeder.EnsureHolidaysAndSampleSchedulesAsync(
            _context, selectedYear, weekStart, cancellationToken);

        var weekDates = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList();

        var shiftsRaw = await _context.WorkShifts
            .AsNoTracking()
            .OrderBy(s => s.ShiftCode)
            .ToListAsync(cancellationToken);

        var scheduleCountsByShift = await _context.ShiftSchedules
            .AsNoTracking()
            .Where(s => s.WorkDate >= weekStart && s.WorkDate <= weekEnd && !s.IsDayOff)
            .GroupBy(s => s.WorkShiftId)
            .Select(g => new { ShiftId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShiftId, x => x.Count, cancellationToken);

        var shifts = shiftsRaw.Select(s => new WorkShiftItemDto(
            s.Id,
            s.ShiftCode,
            s.ShiftName,
            s.StartTime,
            s.EndTime,
            s.BreakStartTime,
            s.BreakEndTime,
            s.IsOvernight,
            s.StandardHours,
            s.WorkdayValue,
            s.NightShiftAllowanceRate,
            s.GraceLateMinutes,
            scheduleCountsByShift.TryGetValue(s.Id, out var c) ? c : 0,
            ShiftAndHolidaySeeder.GetShiftBadgeColor(s.ShiftCode, false),
            s.ShiftType,
            s.CoreStartTime,
            s.CoreEndTime,
            s.SplitStartTime2,
            s.SplitEndTime2,
            s.IsBreakPaid,
            s.MaxEarlyCheckInMinutes,
            s.MaxLateCheckOutMinutes)).ToList();

        var holidaysRaw = await _context.Holidays
            .AsNoTracking()
            .Where(h => h.Year == selectedYear)
            .OrderBy(h => h.HolidayDate)
            .ToListAsync(cancellationToken);

        var holidays = holidaysRaw.Select(h => new HolidayItemDto(
            h.Id,
            h.HolidayDate,
            ShiftAndHolidaySeeder.GetDayOfWeekVi(h.HolidayDate.DayOfWeek),
            h.HolidayName,
            h.Year,
            h.IsPaidHoliday,
            h.OvertimeMultiplier,
            h.IsCompensatoryDay,
            h.OriginalHolidayDate,
            h.LegalReference,
            h.NeedsHrVerification)).ToList();

        var holidayByDate = holidays.ToDictionary(h => h.HolidayDate, h => h.HolidayName);

        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .OrderBy(u => u.SortOrder)
            .Select(u => new Workflows.SimpleLookupDto(u.Id, u.UnitCode, u.UnitName))
            .ToListAsync(cancellationToken);

        var empQuery = _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation");

        if (request.UnitId.HasValue && request.UnitId.Value > 0)
        {
            var selectedUnit = await _context.OrganizationUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UnitId.Value, cancellationToken);

            if (selectedUnit != null && !string.IsNullOrEmpty(selectedUnit.TreePath))
            {
                empQuery = empQuery.Where(e => e.OrganizationUnit.TreePath!.StartsWith(selectedUnit.TreePath));
            }
            else
            {
                empQuery = empQuery.Where(e => e.OrganizationUnitId == request.UnitId.Value);
            }
        }

        var employees = await empQuery
            .OrderBy(e => e.EmployeeCode)
            .Take(25)
            .ToListAsync(cancellationToken);

        var empIds = employees.Select(e => e.Id).ToList();
        var schedulesInWeek = await _context.ShiftSchedules
            .AsNoTracking()
            .Include(s => s.WorkShift)
            .Where(s => empIds.Contains(s.EmployeeId) && s.WorkDate >= weekStart && s.WorkDate <= weekEnd)
            .ToListAsync(cancellationToken);

        var scheduleLookup = schedulesInWeek
            .GroupBy(s => (s.EmployeeId, s.WorkDate))
            .ToDictionary(g => g.Key, g => g.First());

        var defaultShift = shiftsRaw.FirstOrDefault(s => s.ShiftCode == "HC") ?? shiftsRaw.FirstOrDefault();

        var rosterRows = new List<EmployeeShiftRowDto>();
        int factoryShiftCount = 0;
        int nightShiftCount = 0;

        foreach (var emp in employees)
        {
            var cells = new List<ShiftCellDto>();
            foreach (var dt in weekDates)
            {
                var isHoliday = holidayByDate.TryGetValue(dt, out var holidayName);
                if (scheduleLookup.TryGetValue((emp.Id, dt), out var sched))
                {
                    var code = sched.IsDayOff ? "OFF" : sched.WorkShift.ShiftCode;
                    var name = sched.IsDayOff ? "Nghỉ tuần (OFF)" : sched.WorkShift.ShiftName;
                    var timeText = sched.IsDayOff ? "Nghỉ" : $"{sched.WorkShift.StartTime:HH\\:mm}-{sched.WorkShift.EndTime:HH\\:mm}";

                    if (!sched.IsDayOff && code is "CA1" or "CA2" or "CA3")
                        factoryShiftCount++;
                    if (!sched.IsDayOff && sched.WorkShift.IsOvernight)
                        nightShiftCount++;

                    cells.Add(new ShiftCellDto(
                        sched.Id,
                        dt,
                        sched.WorkShiftId,
                        code,
                        name,
                        timeText,
                        sched.IsDayOff,
                        isHoliday,
                        holidayName,
                        ShiftAndHolidaySeeder.GetShiftBadgeColor(code, sched.IsDayOff),
                        sched.ConflictWarning));
                }
                else
                {
                    bool isSun = dt.DayOfWeek == DayOfWeek.Sunday;
                    var code = isSun ? "OFF" : (defaultShift?.ShiftCode ?? "HC");
                    var name = isSun ? "Nghỉ tuần (OFF)" : (defaultShift?.ShiftName ?? "Ca Hành chính");
                    var timeText = isSun ? "Nghỉ" : $"{defaultShift?.StartTime:HH\\:mm}-{defaultShift?.EndTime:HH\\:mm}";

                    cells.Add(new ShiftCellDto(
                        null,
                        dt,
                        defaultShift?.Id,
                        code,
                        name,
                        timeText,
                        isSun,
                        isHoliday,
                        holidayName,
                        ShiftAndHolidaySeeder.GetShiftBadgeColor(code, isSun)));
                }
            }

            rosterRows.Add(new EmployeeShiftRowDto(
                emp.Id,
                emp.EmployeeCode,
                emp.FullName,
                emp.OrganizationUnit.UnitName,
                emp.Position.PositionName,
                cells));
        }

        var conflictWarnings = await _context.AttendanceStatutoryParameters.AnyAsync(cancellationToken)
            ? await AttendancePolicyAndShiftEngine.EvaluateScheduleConflictsAsync(_context, empIds, weekStart, weekEnd, cancellationToken)
            : Array.Empty<ShiftConflictWarningDto>();

        var policyGroupsRaw = await _context.PolicyGroups
            .AsNoTracking()
            .Include(g => g.AttendancePolicies)
            .Include(g => g.Assignments)
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.IsCompanyDefault)
            .ThenBy(g => g.PolicyCode)
            .ToListAsync(cancellationToken);

        var policyGroups = policyGroupsRaw.Select(g =>
        {
            var r = g.AttendancePolicies.FirstOrDefault() ?? new AttendancePolicy();
            return new PolicyGroupSummaryDto(
                g.Id, g.PolicyCode, g.PolicyName, g.IsCompanyDefault,
                r.GraceLateMinutes, r.GraceEarlyMinutes,
                r.RoundingIntervalMinutes, r.RoundingMode,
                r.HalfDayLateThresholdMinutes, r.ZeroDayLateThresholdMinutes,
                r.MaxMissedPunchExplanationsPerMonth, r.MissedPunchOverQuotaAction, r.MissedPunchPenaltyAmount,
                r.RequireApprovedOvertimeRequest, r.WeeklyWorkdaysMask, r.MaxWeeklyWorkingHoursOverride,
                g.Assignments.Count(a => a.IsActive));
        }).ToList();

        var rotationPatterns = await _context.ShiftRotationPatterns
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PatternCode)
            .Select(p => new ShiftRotationPatternDto(p.Id, p.PatternCode, p.PatternName, p.CycleDays, p.CrewCount, p.ShiftSequenceCsv, p.CrewOffsetDays))
            .ToListAsync(cancellationToken);

        var swapRequests = await _context.ShiftSwapRequests
            .AsNoTracking()
            .Include(r => r.RequesterEmployee)
            .Include(r => r.TargetEmployee)
            .OrderByDescending(r => r.CreatedAt)
            .Take(25)
            .Select(r => new ShiftSwapRequestItemDto(
                r.Id, r.RequesterEmployeeId, r.RequesterEmployee.FullName, r.RequesterWorkDate,
                r.TargetEmployeeId, r.TargetEmployee.FullName, r.TargetWorkDate,
                r.Reason, r.Status, r.ApproverComment, r.CreatedAt))
            .ToListAsync(cancellationToken);

        var employeesLookup = await _context.Employees
            .AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation")
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new Workflows.SimpleLookupDto(e.Id, e.EmployeeCode, e.FullName))
            .ToListAsync(cancellationToken);

        var positionsLookup = await _context.Positions
            .AsNoTracking()
            .OrderBy(p => p.PositionCode)
            .Select(p => new Workflows.SimpleLookupDto(p.Id, p.PositionCode, p.PositionName))
            .ToListAsync(cancellationToken);

        var statutory = await _context.AttendanceStatutoryParameters
            .AsNoTracking()
            .Where(p => p.EffectiveFrom <= weekStart && (p.EffectiveTo == null || p.EffectiveTo >= weekStart))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return new ShiftAndScheduleOverviewDto(
            weekStart,
            weekEnd,
            selectedYear,
            request.UnitId,
            weekDates,
            shifts,
            holidays,
            rosterRows,
            units,
            factoryShiftCount,
            nightShiftCount,
            holidays.Count,
            conflictWarnings,
            policyGroups,
            rotationPatterns,
            swapRequests,
            employeesLookup,
            positionsLookup,
            statutory);
    }
}

// ============================================================================
// 4. COMMAND: LƯU CA LÀM VIỆC (WORK SHIFT)
// ============================================================================

public record SaveWorkShiftCommand(
    int? Id,
    string ShiftCode,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    TimeOnly? BreakStartTime,
    TimeOnly? BreakEndTime,
    bool IsOvernight,
    decimal StandardHours,
    decimal WorkdayValue,
    decimal NightShiftAllowanceRate,
    int GraceLateMinutes,
    string ShiftType = "Fixed",
    TimeOnly? CoreStartTime = null,
    TimeOnly? CoreEndTime = null,
    TimeOnly? SplitStartTime2 = null,
    TimeOnly? SplitEndTime2 = null,
    bool IsBreakPaid = false,
    int MaxEarlyCheckInMinutes = 120,
    int MaxLateCheckOutMinutes = 180) : IRequest<int>;

public class SaveWorkShiftCommandHandler : IRequestHandler<SaveWorkShiftCommand, int>
{
    private readonly IApplicationDbContext _context;

    public SaveWorkShiftCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(SaveWorkShiftCommand request, CancellationToken cancellationToken)
    {
        WorkShift shift;
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            shift = await _context.WorkShifts.FirstOrDefaultAsync(s => s.Id == request.Id.Value, cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy ca làm việc.");
        }
        else
        {
            shift = new WorkShift { ShiftCode = request.ShiftCode.Trim().ToUpperInvariant() };
            await _context.WorkShifts.AddAsync(shift, cancellationToken);
        }

        var normalizedType = string.IsNullOrWhiteSpace(request.ShiftType) ? (request.IsOvernight ? "Overnight" : "Fixed") : request.ShiftType.Trim();
        shift.ShiftCode = request.ShiftCode.Trim().ToUpperInvariant();
        shift.ShiftName = request.ShiftName.Trim();
        shift.ShiftType = normalizedType;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.CoreStartTime = request.CoreStartTime;
        shift.CoreEndTime = request.CoreEndTime;
        shift.SplitStartTime2 = request.SplitStartTime2;
        shift.SplitEndTime2 = request.SplitEndTime2;
        shift.BreakStartTime = request.BreakStartTime;
        shift.BreakEndTime = request.BreakEndTime;
        shift.IsBreakPaid = request.IsBreakPaid;
        shift.IsOvernight = request.IsOvernight || string.Equals(normalizedType, "Overnight", StringComparison.OrdinalIgnoreCase);
        shift.StandardHours = request.StandardHours;
        shift.WorkdayValue = request.WorkdayValue;
        shift.NightShiftAllowanceRate = request.NightShiftAllowanceRate;
        shift.GraceLateMinutes = request.GraceLateMinutes;
        shift.MaxEarlyCheckInMinutes = request.MaxEarlyCheckInMinutes;
        shift.MaxLateCheckOutMinutes = request.MaxLateCheckOutMinutes;

        await _context.SaveChangesAsync(cancellationToken);
        return shift.Id;
    }
}

// ============================================================================
// 5. COMMAND: XẾP CA KÉO THẢ HOẶC PHÂN CA HÀNG LOẠT (ASSIGN SHIFT SCHEDULE)
// ============================================================================

public record AssignShiftScheduleCommand(
    int? EmployeeId,
    int? OrganizationUnitId,
    DateOnly FromDate,
    DateOnly ToDate,
    int? WorkShiftId,
    bool IsDayOff = false,
    string? Notes = null,
    bool ReplaceExisting = true) : IRequest<int>;

public record AssignShiftScheduleDetailedCommand(
    int? EmployeeId,
    int? OrganizationUnitId,
    DateOnly FromDate,
    DateOnly ToDate,
    int? WorkShiftId,
    bool IsDayOff = false,
    string? Notes = null,
    bool ReplaceExisting = true) : IRequest<AssignScheduleResultDto>;

public class AssignShiftScheduleCommandHandler :
    IRequestHandler<AssignShiftScheduleCommand, int>,
    IRequestHandler<AssignShiftScheduleDetailedCommand, AssignScheduleResultDto>
{
    private readonly IApplicationDbContext _context;

    public AssignShiftScheduleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AssignShiftScheduleCommand request, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(
            request.EmployeeId, request.OrganizationUnitId, request.FromDate, request.ToDate,
            request.WorkShiftId, request.IsDayOff, request.Notes, request.ReplaceExisting, cancellationToken);
        return result.AffectedCount;
    }

    public Task<AssignScheduleResultDto> Handle(AssignShiftScheduleDetailedCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(
            request.EmployeeId, request.OrganizationUnitId, request.FromDate, request.ToDate,
            request.WorkShiftId, request.IsDayOff, request.Notes, request.ReplaceExisting, cancellationToken);

    private async Task<AssignScheduleResultDto> ExecuteAsync(
        int? employeeId,
        int? organizationUnitId,
        DateOnly fromDate,
        DateOnly toDate,
        int? workShiftId,
        bool isDayOff,
        string? notes,
        bool replaceExisting,
        CancellationToken cancellationToken)
    {
        var defaultShift = await _context.WorkShifts.AsNoTracking().FirstAsync(cancellationToken);
        int targetShiftId = workShiftId ?? defaultShift.Id;

        var empIds = new List<int>();
        if (employeeId.HasValue && employeeId.Value > 0)
        {
            empIds.Add(employeeId.Value);
        }
        else if (organizationUnitId.HasValue && organizationUnitId.Value > 0)
        {
            empIds = await _context.Employees
                .AsNoTracking()
                .Where(e => e.OrganizationUnitId == organizationUnitId.Value &&
                            (e.WorkStatus == "Official" || e.WorkStatus == "Probation"))
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);
        }

        if (empIds.Count == 0)
            return new AssignScheduleResultDto(0, Array.Empty<ShiftConflictWarningDto>());

        var existingSchedules = await _context.ShiftSchedules
            .Where(s => empIds.Contains(s.EmployeeId) && s.WorkDate >= fromDate && s.WorkDate <= toDate)
            .ToListAsync(cancellationToken);

        int affected = 0;
        var touched = new List<ShiftSchedule>();

        foreach (var empId in empIds)
        {
            for (var dt = fromDate; dt <= toDate; dt = dt.AddDays(1))
            {
                if (replaceExisting)
                {
                    var daySchedules = existingSchedules.Where(s => s.EmployeeId == empId && s.WorkDate == dt).ToList();
                    var exact = daySchedules.FirstOrDefault(s => s.WorkShiftId == targetShiftId) ?? daySchedules.FirstOrDefault();
                    if (exact != null)
                    {
                        exact.WorkShiftId = targetShiftId;
                        exact.IsDayOff = isDayOff;
                        exact.Notes = notes ?? "Cập nhật từ lịch phân ca kéo-thả";
                        exact.ConflictWarning = null;
                        touched.Add(exact);

                        foreach (var extra in daySchedules.Where(d => d.Id != exact.Id))
                        {
                            extra.IsDeleted = true;
                        }
                    }
                    else
                    {
                        var created = new ShiftSchedule
                        {
                            EmployeeId = empId,
                            WorkShiftId = targetShiftId,
                            WorkDate = dt,
                            IsDayOff = isDayOff,
                            Notes = notes ?? "Phân ca từ lịch kéo-thả"
                        };
                        await _context.ShiftSchedules.AddAsync(created, cancellationToken);
                        touched.Add(created);
                    }
                }
                else
                {
                    var exact = existingSchedules.FirstOrDefault(s => s.EmployeeId == empId && s.WorkDate == dt && s.WorkShiftId == targetShiftId);
                    if (exact != null)
                    {
                        exact.IsDayOff = isDayOff;
                        exact.Notes = notes ?? "Cập nhật ca bổ sung";
                        touched.Add(exact);
                    }
                    else
                    {
                        var created = new ShiftSchedule
                        {
                            EmployeeId = empId,
                            WorkShiftId = targetShiftId,
                            WorkDate = dt,
                            IsDayOff = isDayOff,
                            Notes = notes ?? "Phân ca bổ sung"
                        };
                        await _context.ShiftSchedules.AddAsync(created, cancellationToken);
                        touched.Add(created);
                    }
                }
                affected++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var warnings = await _context.AttendanceStatutoryParameters.AnyAsync(cancellationToken)
            ? await AttendancePolicyAndShiftEngine.EvaluateScheduleConflictsAsync(_context, empIds, fromDate, toDate, cancellationToken)
            : Array.Empty<ShiftConflictWarningDto>();

        if (warnings.Count > 0)
        {
            foreach (var sched in touched)
            {
                var msgs = warnings
                    .Where(w => w.EmployeeId == sched.EmployeeId && (w.WorkDate == sched.WorkDate || w.ConflictType == "WeeklyHoursExceeded"))
                    .Select(w => w.Message)
                    .Distinct()
                    .ToList();
                sched.ConflictWarning = msgs.Count > 0 ? string.Join(" | ", msgs) : null;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new AssignScheduleResultDto(affected, warnings);
    }
}

// ============================================================================
// 6. COMMAND: QUẢN LÝ NGÀY LỄ TẾT (SAVE / DELETE HOLIDAY) + NGHỈ BÙ
// ============================================================================

public record SaveHolidayCommand(
    int? Id,
    DateOnly HolidayDate,
    string HolidayName,
    bool IsPaidHoliday = true,
    decimal OvertimeMultiplier = 3.0m,
    bool IsCompensatoryDay = false,
    DateOnly? OriginalHolidayDate = null,
    bool AutoGenerateCompensatoryIfWeekend = true,
    string? LegalReference = null,
    bool NeedsHrVerification = true) : IRequest<int>;

public class SaveHolidayCommandHandler : IRequestHandler<SaveHolidayCommand, int>
{
    private readonly IApplicationDbContext _context;

    public SaveHolidayCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(SaveHolidayCommand request, CancellationToken cancellationToken)
    {
        Holiday holiday;
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            holiday = await _context.Holidays.FirstOrDefaultAsync(h => h.Id == request.Id.Value, cancellationToken)
                      ?? throw new KeyNotFoundException("Không tìm thấy ngày lễ.");
        }
        else
        {
            holiday = new Holiday();
            await _context.Holidays.AddAsync(holiday, cancellationToken);
        }

        holiday.HolidayDate = request.HolidayDate;
        holiday.HolidayName = request.HolidayName.Trim();
        holiday.Year = request.HolidayDate.Year;
        holiday.IsPaidHoliday = request.IsPaidHoliday;
        holiday.OvertimeMultiplier = request.OvertimeMultiplier;
        holiday.IsCompensatoryDay = request.IsCompensatoryDay;
        holiday.OriginalHolidayDate = request.OriginalHolidayDate;
        holiday.LegalReference = request.LegalReference ?? "Điều 112 Bộ luật Lao động 45/2019/QH14";
        holiday.NeedsHrVerification = request.NeedsHrVerification;

        await _context.SaveChangesAsync(cancellationToken);

        if (request.AutoGenerateCompensatoryIfWeekend && !holiday.IsCompensatoryDay)
        {
            await AttendancePolicyAndShiftEngine.EnsureCompensatoryHolidayAsync(_context, holiday, cancellationToken);
        }

        return holiday.Id;
    }
}

public record DeleteHolidayCommand(int Id) : IRequest<bool>;

public class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteHolidayCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteHolidayCommand request, CancellationToken cancellationToken)
    {
        var holiday = await _context.Holidays.FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);
        if (holiday == null) return false;

        holiday.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>
/// Cho phép HR chọn ngày nghỉ liền kề Quốc khánh 2/9 ("Before" = 01/09 hoặc "After" = 03/09) theo Quyết định của Thủ tướng Chính phủ.
/// </summary>
public record SelectNationalDayAdjacentHolidayCommand(int Year, string Choice) : IRequest<DateOnly>;

public class SelectNationalDayAdjacentHolidayCommandHandler : IRequestHandler<SelectNationalDayAdjacentHolidayCommand, DateOnly>
{
    private readonly IApplicationDbContext _context;

    public SelectNationalDayAdjacentHolidayCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DateOnly> Handle(SelectNationalDayAdjacentHolidayCommand request, CancellationToken cancellationToken)
    {
        bool chooseBefore = string.Equals(request.Choice?.Trim(), "Before", StringComparison.OrdinalIgnoreCase);
        var selectedDate = new DateOnly(request.Year, 9, chooseBefore ? 1 : 3);
        var unselectedDate = new DateOnly(request.Year, 9, chooseBefore ? 3 : 1);

        var other = await _context.Holidays.FirstOrDefaultAsync(h => h.HolidayDate == unselectedDate, cancellationToken);
        if (other is not null)
            other.IsDeleted = true;

        var existingSelected = await _context.Holidays
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.HolidayDate == selectedDate, cancellationToken);

        string name = chooseBefore
            ? "Nghỉ liền kề trước Quốc khánh (01/09)"
            : "Nghỉ liền kề sau Quốc khánh (03/09)";
        string legalRef = $"Điều 112.1.đ BLLĐ 2019 (HR đã xác nhận ngày liền kề {(chooseBefore ? "01/09 trước Lễ" : "03/09 sau Lễ")})";

        if (existingSelected is not null)
        {
            existingSelected.IsDeleted = false;
            existingSelected.Year = request.Year;
            existingSelected.HolidayName = name;
            existingSelected.IsPaidHoliday = true;
            existingSelected.OvertimeMultiplier = 3.0m;
            existingSelected.LegalReference = legalRef;
            existingSelected.NeedsHrVerification = false;
        }
        else
        {
            await _context.Holidays.AddAsync(new Holiday
            {
                HolidayDate = selectedDate,
                HolidayName = name,
                Year = request.Year,
                IsPaidHoliday = true,
                OvertimeMultiplier = 3.0m,
                LegalReference = legalRef,
                NeedsHrVerification = false
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return selectedDate;
    }
}

// ============================================================================
// 7. COMMAND: XOAY CA TỰ ĐỘNG (VD: 3 CA 4 KÍP TRONG N TUẦN)
// ============================================================================

public record GenerateRotationScheduleCommand(
    int PatternId,
    DateOnly StartDate,
    int WeeksCount,
    int? OrganizationUnitId = null,
    IReadOnlyList<IReadOnlyList<int>>? CrewEmployeeGroups = null) : IRequest<AssignScheduleResultDto>;

public class GenerateRotationScheduleCommandHandler : IRequestHandler<GenerateRotationScheduleCommand, AssignScheduleResultDto>
{
    private readonly IApplicationDbContext _context;

    public GenerateRotationScheduleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssignScheduleResultDto> Handle(GenerateRotationScheduleCommand request, CancellationToken cancellationToken)
    {
        if (request.WeeksCount <= 0 || request.WeeksCount > 52)
            throw new InvalidOperationException("Số tuần xoay ca phải từ 1 đến 52 tuần.");

        var pattern = await _context.ShiftRotationPatterns
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PatternId && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy mẫu xoay ca.");

        var sequence = pattern.ShiftSequenceCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToUpperInvariant())
            .ToArray();

        if (sequence.Length == 0)
            throw new InvalidOperationException("Mẫu xoay ca chưa có chuỗi ca hợp lệ.");

        var shifts = await _context.WorkShifts.AsNoTracking().ToListAsync(cancellationToken);
        if (shifts.Count == 0)
            throw new InvalidOperationException("Hệ thống chưa có ca làm việc để phân bổ.");

        var shiftMap = shifts.ToDictionary(s => s.ShiftCode.ToUpperInvariant(), s => s);
        var fallbackShift = shiftMap.Values.First();

        var crews = new List<List<int>>();
        if (request.CrewEmployeeGroups is { Count: > 0 })
        {
            crews.AddRange(request.CrewEmployeeGroups.Select(g => g.ToList()));
        }
        else
        {
            var empQuery = _context.Employees
                .AsNoTracking()
                .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation");

            if (request.OrganizationUnitId.HasValue && request.OrganizationUnitId.Value > 0)
            {
                empQuery = empQuery.Where(e => e.OrganizationUnitId == request.OrganizationUnitId.Value);
            }

            var unitEmpIds = await empQuery.OrderBy(e => e.EmployeeCode).Select(e => e.Id).ToListAsync(cancellationToken);
            int crewCount = Math.Max(1, pattern.CrewCount);
            for (int c = 0; c < crewCount; c++) crews.Add(new List<int>());
            for (int i = 0; i < unitEmpIds.Count; i++) crews[i % crewCount].Add(unitEmpIds[i]);
        }

        var allEmpIds = crews.SelectMany(c => c).Distinct().ToList();
        if (allEmpIds.Count == 0)
            return new AssignScheduleResultDto(0, Array.Empty<ShiftConflictWarningDto>());

        int totalDays = request.WeeksCount * 7;
        var endDate = request.StartDate.AddDays(totalDays - 1);

        var existing = await _context.ShiftSchedules
            .Where(s => allEmpIds.Contains(s.EmployeeId) && s.WorkDate >= request.StartDate && s.WorkDate <= endDate)
            .ToListAsync(cancellationToken);

        int affected = 0;
        var touched = new List<ShiftSchedule>();

        for (int crewIdx = 0; crewIdx < crews.Count; crewIdx++)
        {
            int crewStartOffset = crewIdx * Math.Max(0, pattern.CrewOffsetDays);
            foreach (var empId in crews[crewIdx])
            {
                for (int dayOffset = 0; dayOffset < totalDays; dayOffset++)
                {
                    var dt = request.StartDate.AddDays(dayOffset);
                    var token = sequence[(dayOffset + crewStartOffset) % sequence.Length];
                    bool isOff = token == "OFF";
                    int shiftId = isOff
                        ? fallbackShift.Id
                        : (shiftMap.TryGetValue(token, out var matchedShift)
                            ? matchedShift.Id
                            : throw new InvalidOperationException($"Không tìm thấy ca '{token}' trong mẫu xoay ca {pattern.PatternCode}."));

                    var existingOnDay = existing.Where(s => s.EmployeeId == empId && s.WorkDate == dt).ToList();
                    var primary = existingOnDay.FirstOrDefault();
                    if (primary != null)
                    {
                        primary.WorkShiftId = shiftId;
                        primary.IsDayOff = isOff;
                        primary.Notes = $"Xoay ca {pattern.PatternCode} - Kíp {crewIdx + 1}";
                        primary.ConflictWarning = null;
                        touched.Add(primary);
                        foreach (var extra in existingOnDay.Where(e => e.Id != primary.Id))
                            extra.IsDeleted = true;
                    }
                    else
                    {
                        var created = new ShiftSchedule
                        {
                            EmployeeId = empId,
                            WorkShiftId = shiftId,
                            WorkDate = dt,
                            IsDayOff = isOff,
                            Notes = $"Xoay ca {pattern.PatternCode} - Kíp {crewIdx + 1}"
                        };
                        await _context.ShiftSchedules.AddAsync(created, cancellationToken);
                        touched.Add(created);
                    }
                    affected++;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var warnings = await _context.AttendanceStatutoryParameters.AnyAsync(cancellationToken)
            ? await AttendancePolicyAndShiftEngine.EvaluateScheduleConflictsAsync(_context, allEmpIds, request.StartDate, endDate, cancellationToken)
            : Array.Empty<ShiftConflictWarningDto>();

        if (warnings.Count > 0)
        {
            foreach (var sched in touched)
            {
                var msgs = warnings
                    .Where(w => w.EmployeeId == sched.EmployeeId && (w.WorkDate == sched.WorkDate || w.ConflictType == "WeeklyHoursExceeded"))
                    .Select(w => w.Message)
                    .Distinct()
                    .ToList();
                sched.ConflictWarning = msgs.Count > 0 ? string.Join(" | ", msgs) : null;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new AssignScheduleResultDto(affected, warnings);
    }
}

// ============================================================================
// 8. COMMAND: ĐỔI CA GIỮA 2 NHÂN VIÊN (SUBMIT / APPROVE-REJECT + AUDITLOG)
// ============================================================================

public record SubmitShiftSwapRequestCommand(
    int RequesterEmployeeId,
    int TargetEmployeeId,
    DateOnly RequesterWorkDate,
    DateOnly TargetWorkDate,
    string Reason) : IRequest<int>;

public class SubmitShiftSwapRequestCommandHandler : IRequestHandler<SubmitShiftSwapRequestCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender? _sender;

    public SubmitShiftSwapRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ISender? sender = null)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<int> Handle(SubmitShiftSwapRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (request.RequesterEmployeeId == request.TargetEmployeeId)
            throw new InvalidOperationException("Không thể gửi yêu cầu đổi ca cho chính mình.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do đổi ca.");

        var reqMonthLocked = await _context.MonthlyAttendanceSummaries.AnyAsync(
            m => m.IsLocked && ((m.Year == request.RequesterWorkDate.Year && m.Month == request.RequesterWorkDate.Month && m.EmployeeId == request.RequesterEmployeeId)
                             || (m.Year == request.TargetWorkDate.Year && m.Month == request.TargetWorkDate.Month && m.EmployeeId == request.TargetEmployeeId)),
            cancellationToken);
        if (reqMonthLocked)
            throw new InvalidOperationException("Bảng công tháng liên quan đã khóa, không thể tạo đơn đổi ca.");

        var reqSched = await _context.ShiftSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.EmployeeId == request.RequesterEmployeeId && s.WorkDate == request.RequesterWorkDate, cancellationToken);
        var tgtSched = await _context.ShiftSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.EmployeeId == request.TargetEmployeeId && s.WorkDate == request.TargetWorkDate, cancellationToken);

        var swap = new ShiftSwapRequest
        {
            RequesterEmployeeId = request.RequesterEmployeeId,
            TargetEmployeeId = request.TargetEmployeeId,
            RequesterWorkDate = request.RequesterWorkDate,
            TargetWorkDate = request.TargetWorkDate,
            RequesterOriginalShiftId = reqSched?.WorkShiftId,
            TargetOriginalShiftId = tgtSched?.WorkShiftId,
            Reason = request.Reason.Trim(),
            Status = "Pending"
        };

        await _context.ShiftSwapRequests.AddAsync(swap, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.AuditLogs.AddAsync(new Domain.Entities.SystemEntities.AuditLog
        {
            UserId = userId,
            Username = username,
            ActionType = "Create",
            ModuleName = "TimeAttendance",
            EntityName = "ShiftSwapRequest",
            EntityId = swap.Id.ToString(),
            NewValuesJson = $"{{\"summary\":\"Đề nghị đổi ca giữa NV #{request.RequesterEmployeeId} ({request.RequesterWorkDate:dd/MM}) và NV #{request.TargetEmployeeId} ({request.TargetWorkDate:dd/MM})\"}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        if (_sender != null)
        {
            var wfId = await _sender.Send(new StartWorkflowInstanceCommand(
                "ShiftSwapRequest",
                swap.Id,
                request.RequesterEmployeeId,
                $"Đơn Đổi ca #{swap.Id} ({request.RequesterWorkDate:dd/MM} ↔ {request.TargetWorkDate:dd/MM})",
                1m,
                "/Shifts?tab=swaps"), cancellationToken);
            swap.WorkflowInstanceId = wfId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return swap.Id;
    }
}

public record ProcessShiftSwapRequestCommand(int RequestId, bool Approve, string? ApproverComment) : IRequest<AssignScheduleResultDto>;

public class ProcessShiftSwapRequestCommandHandler : IRequestHandler<ProcessShiftSwapRequestCommand, AssignScheduleResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProcessShiftSwapRequestCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<AssignScheduleResultDto> Handle(ProcessShiftSwapRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var swap = await _context.ShiftSwapRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn đổi ca.");

        if (!string.Equals(swap.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đơn đổi ca đã được xử lý trước đó.");

        swap.ApprovedByUserId = userId;
        swap.ApprovedAt = DateTime.UtcNow;
        swap.ApproverComment = request.ApproverComment?.Trim();

        IReadOnlyList<ShiftConflictWarningDto> warnings = Array.Empty<ShiftConflictWarningDto>();
        int affected = 0;

        if (!request.Approve)
        {
            swap.Status = "Rejected";
        }
        else
        {
            var isLocked = await _context.MonthlyAttendanceSummaries.AnyAsync(
                m => m.IsLocked && ((m.Year == swap.RequesterWorkDate.Year && m.Month == swap.RequesterWorkDate.Month && m.EmployeeId == swap.RequesterEmployeeId)
                                 || (m.Year == swap.TargetWorkDate.Year && m.Month == swap.TargetWorkDate.Month && m.EmployeeId == swap.TargetEmployeeId)),
                cancellationToken);
            if (isLocked)
                throw new InvalidOperationException("Bảng công tháng đã khóa, không thể duyệt đổi ca.");

            swap.Status = "Approved";

            var reqSched = await _context.ShiftSchedules
                .FirstOrDefaultAsync(s => s.EmployeeId == swap.RequesterEmployeeId && s.WorkDate == swap.RequesterWorkDate, cancellationToken);
            var tgtSched = await _context.ShiftSchedules
                .FirstOrDefaultAsync(s => s.EmployeeId == swap.TargetEmployeeId && s.WorkDate == swap.TargetWorkDate, cancellationToken);

            var defaultShiftId = await _context.WorkShifts.AsNoTracking().Select(w => w.Id).FirstAsync(cancellationToken);

            var reqOldShiftId = reqSched?.WorkShiftId ?? swap.RequesterOriginalShiftId ?? defaultShiftId;
            var reqOldOff = reqSched?.IsDayOff ?? false;
            var tgtOldShiftId = tgtSched?.WorkShiftId ?? swap.TargetOriginalShiftId ?? defaultShiftId;
            var tgtOldOff = tgtSched?.IsDayOff ?? false;

            if (reqSched != null)
            {
                reqSched.WorkShiftId = tgtOldShiftId;
                reqSched.IsDayOff = tgtOldOff;
                reqSched.Notes = $"Đổi ca #{swap.Id} với NV #{swap.TargetEmployeeId}";
            }
            else
            {
                await _context.ShiftSchedules.AddAsync(new ShiftSchedule
                {
                    EmployeeId = swap.RequesterEmployeeId,
                    WorkShiftId = tgtOldShiftId,
                    WorkDate = swap.RequesterWorkDate,
                    IsDayOff = tgtOldOff,
                    Notes = $"Đổi ca #{swap.Id}"
                }, cancellationToken);
            }

            if (tgtSched != null)
            {
                tgtSched.WorkShiftId = reqOldShiftId;
                tgtSched.IsDayOff = reqOldOff;
                tgtSched.Notes = $"Đổi ca #{swap.Id} với NV #{swap.RequesterEmployeeId}";
            }
            else
            {
                await _context.ShiftSchedules.AddAsync(new ShiftSchedule
                {
                    EmployeeId = swap.TargetEmployeeId,
                    WorkShiftId = reqOldShiftId,
                    WorkDate = swap.TargetWorkDate,
                    IsDayOff = reqOldOff,
                    Notes = $"Đổi ca #{swap.Id}"
                }, cancellationToken);
            }

            affected = 2;
            await _context.SaveChangesAsync(cancellationToken);

            var minDate = swap.RequesterWorkDate < swap.TargetWorkDate ? swap.RequesterWorkDate : swap.TargetWorkDate;
            var maxDate = swap.RequesterWorkDate > swap.TargetWorkDate ? swap.RequesterWorkDate : swap.TargetWorkDate;
            if (await _context.AttendanceStatutoryParameters.AnyAsync(cancellationToken))
            {
                warnings = await AttendancePolicyAndShiftEngine.EvaluateScheduleConflictsAsync(
                    _context, new[] { swap.RequesterEmployeeId, swap.TargetEmployeeId }, minDate, maxDate, cancellationToken);
            }
        }

        await _context.AuditLogs.AddAsync(new Domain.Entities.SystemEntities.AuditLog
        {
            UserId = userId,
            Username = username,
            ActionType = request.Approve ? "Approve" : "Reject",
            ModuleName = "TimeAttendance",
            EntityName = "ShiftSwapRequest",
            EntityId = swap.Id.ToString(),
            NewValuesJson = $"{{\"summary\":\"{(request.Approve ? "Duyệt" : "Từ chối")} đơn đổi ca #{swap.Id}\",\"status\":\"{swap.Status}\"}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AssignScheduleResultDto(affected, warnings);
    }
}

// ============================================================================
// 9. COMMAND: NHÓM CHÍNH SÁCH (POLICY GROUP) & GÁN PHẠM VI
// ============================================================================

public record SavePolicyGroupCommand(
    int? Id,
    string PolicyCode,
    string PolicyName,
    bool IsCompanyDefault,
    int GraceLateMinutes,
    int GraceEarlyMinutes,
    int RoundingIntervalMinutes,
    string RoundingMode,
    int HalfDayLateThresholdMinutes,
    int ZeroDayLateThresholdMinutes,
    int MaxMissedPunchExplanationsPerMonth,
    string MissedPunchOverQuotaAction,
    decimal MissedPunchPenaltyAmount,
    bool RequireApprovedOvertimeRequest,
    string WeeklyWorkdaysMask,
    decimal? MaxWeeklyWorkingHoursOverride,
    string? Description = null) : IRequest<int>;

public class SavePolicyGroupCommandHandler : IRequestHandler<SavePolicyGroupCommand, int>
{
    private readonly IApplicationDbContext _context;

    public SavePolicyGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(SavePolicyGroupCommand request, CancellationToken cancellationToken)
    {
        PolicyGroup group;
        AttendancePolicy policy;

        if (request.Id.HasValue && request.Id.Value > 0)
        {
            group = await _context.PolicyGroups
                .Include(g => g.AttendancePolicies)
                .FirstOrDefaultAsync(g => g.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy nhóm chính sách.");
            policy = group.AttendancePolicies.FirstOrDefault() ?? new AttendancePolicy { PolicyGroupId = group.Id };
            if (policy.Id == 0)
            {
                group.AttendancePolicies.Add(policy);
            }
        }
        else
        {
            group = new PolicyGroup();
            policy = new AttendancePolicy();
            group.AttendancePolicies.Add(policy);
            await _context.PolicyGroups.AddAsync(group, cancellationToken);
        }

        if (request.IsCompanyDefault)
        {
            var others = await _context.PolicyGroups.Where(g => g.IsCompanyDefault && g.Id != group.Id).ToListAsync(cancellationToken);
            foreach (var o in others) o.IsCompanyDefault = false;
        }

        group.PolicyCode = request.PolicyCode.Trim().ToUpperInvariant();
        group.PolicyName = request.PolicyName.Trim();
        group.IsCompanyDefault = request.IsCompanyDefault;
        group.IsActive = true;
        group.Description = request.Description?.Trim();

        policy.GraceLateMinutes = Math.Max(0, request.GraceLateMinutes);
        policy.GraceEarlyMinutes = Math.Max(0, request.GraceEarlyMinutes);
        policy.RoundingIntervalMinutes = request.RoundingIntervalMinutes;
        policy.RoundingMode = string.IsNullOrWhiteSpace(request.RoundingMode) ? "Nearest" : request.RoundingMode.Trim();
        policy.HalfDayLateThresholdMinutes = Math.Max(0, request.HalfDayLateThresholdMinutes);
        policy.ZeroDayLateThresholdMinutes = Math.Max(0, request.ZeroDayLateThresholdMinutes);
        policy.MaxMissedPunchExplanationsPerMonth = Math.Max(0, request.MaxMissedPunchExplanationsPerMonth);
        policy.MissedPunchOverQuotaAction = string.IsNullOrWhiteSpace(request.MissedPunchOverQuotaAction) ? "FixedFine" : request.MissedPunchOverQuotaAction.Trim();
        policy.MissedPunchPenaltyAmount = decimal.Round(Math.Max(0m, request.MissedPunchPenaltyAmount), 0, MidpointRounding.AwayFromZero);
        policy.RequireApprovedOvertimeRequest = request.RequireApprovedOvertimeRequest;
        policy.WeeklyWorkdaysMask = string.IsNullOrWhiteSpace(request.WeeklyWorkdaysMask) ? "Mon,Tue,Wed,Thu,Fri" : request.WeeklyWorkdaysMask.Trim();
        policy.MaxWeeklyWorkingHoursOverride = request.MaxWeeklyWorkingHoursOverride;

        await _context.SaveChangesAsync(cancellationToken);
        return group.Id;
    }
}

public record AssignPolicyGroupCommand(
    int PolicyGroupId,
    int? EmployeeId = null,
    int? PositionId = null,
    int? OrganizationUnitId = null,
    DateOnly? EffectiveFrom = null,
    DateOnly? EffectiveTo = null) : IRequest<int>;

public class AssignPolicyGroupCommandHandler : IRequestHandler<AssignPolicyGroupCommand, int>
{
    private readonly IApplicationDbContext _context;

    public AssignPolicyGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AssignPolicyGroupCommand request, CancellationToken cancellationToken)
    {
        if (!request.EmployeeId.HasValue && !request.PositionId.HasValue && !request.OrganizationUnitId.HasValue)
            throw new InvalidOperationException("Phải chọn ít nhất một đối tượng gán: Nhân viên, Chức danh hoặc Đơn vị.");

        var assignment = new PolicyGroupAssignment
        {
            PolicyGroupId = request.PolicyGroupId,
            EmployeeId = request.EmployeeId > 0 ? request.EmployeeId : null,
            PositionId = request.PositionId > 0 ? request.PositionId : null,
            OrganizationUnitId = request.OrganizationUnitId > 0 ? request.OrganizationUnitId : null,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = true
        };

        await _context.PolicyGroupAssignments.AddAsync(assignment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return assignment.Id;
    }
}

