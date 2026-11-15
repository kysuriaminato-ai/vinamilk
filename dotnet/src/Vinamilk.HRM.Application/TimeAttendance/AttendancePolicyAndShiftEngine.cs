using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.TimeAttendance;

public sealed record ResolvedAttendancePolicyDto(
    int PolicyGroupId,
    string PolicyCode,
    string PolicyName,
    string ResolutionTier, // Employee | Position | OrganizationUnit | CompanyDefault | DefaultFallback
    AttendancePolicy Policy);

public sealed record ShiftConflictWarningDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly WorkDate,
    string ConflictType, // Overlap | InsufficientRest | WeeklyHoursExceeded
    string Message,
    string? LegalReference = null);

public sealed record CalculatedAttendanceResultDto(
    DateOnly AttributedWorkDate,
    int? WorkShiftId,
    string ShiftCode,
    string AttendanceSymbol,
    decimal WorkingHours,
    decimal NightShiftHours,
    decimal WorkdayCount,
    int RawLateMinutes,
    int LateMinutes,
    int EarlyLeaveMinutes,
    string CalculationNotes);

public static class AttendancePolicyAndShiftEngine
{
    public static async Task<AttendanceStatutoryParameter?> TryResolveStatutoryParameterAsync(
        IApplicationDbContext context,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        return await context.AttendanceStatutoryParameters
            .AsNoTracking()
            .Where(p => p.EffectiveFrom <= effectiveDate && (p.EffectiveTo == null || p.EffectiveTo >= effectiveDate))
            .OrderByDescending(p => p.EffectiveFrom)
            .ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<AttendanceStatutoryParameter> ResolveStatutoryParameterAsync(
        IApplicationDbContext context,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        var param = await TryResolveStatutoryParameterAsync(context, effectiveDate, cancellationToken);

        return param ?? throw new InvalidOperationException(
            $"Chưa cấu hình tham số pháp lý chấm công (AttendanceStatutoryParameter) hiệu lực tại ngày {effectiveDate:dd/MM/yyyy}.");
    }

    public static async Task<ResolvedAttendancePolicyDto> ResolveAttendancePolicyAsync(
        IApplicationDbContext context,
        int employeeId,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        var emp = await context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.Id, e.PositionId, e.OrganizationUnitId })
            .FirstOrDefaultAsync(cancellationToken);

        var unitParentMap = await context.OrganizationUnits
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.ParentUnitId, cancellationToken);

        var activeGroups = await context.PolicyGroups
            .AsNoTracking()
            .Include(g => g.AttendancePolicies)
            .Include(g => g.Assignments)
            .Where(g => g.IsActive)
            .ToListAsync(cancellationToken);

        bool IsEffective(PolicyGroupAssignment a) =>
            a.IsActive
            && (!a.EffectiveFrom.HasValue || a.EffectiveFrom.Value <= effectiveDate)
            && (!a.EffectiveTo.HasValue || a.EffectiveTo.Value >= effectiveDate);

        if (emp != null)
        {
            // 1. Ưu tiên cao nhất: gán trực tiếp cho Nhân viên
            var empMatch = activeGroups
                .SelectMany(g => g.Assignments.Where(a => a.EmployeeId == emp.Id && IsEffective(a)).Select(a => new { Group = g, Assignment = a }))
                .OrderByDescending(x => x.Assignment.Id)
                .FirstOrDefault();
            if (empMatch?.Group.AttendancePolicies.FirstOrDefault() is { } empPolicy)
            {
                return new(empMatch.Group.Id, empMatch.Group.PolicyCode, empMatch.Group.PolicyName, "Employee", empPolicy);
            }

            // 2. Chức danh
            var posMatch = activeGroups
                .SelectMany(g => g.Assignments.Where(a => a.EmployeeId == null && a.PositionId == emp.PositionId && IsEffective(a)).Select(a => new { Group = g, Assignment = a }))
                .OrderByDescending(x => x.Assignment.Id)
                .FirstOrDefault();
            if (posMatch?.Group.AttendancePolicies.FirstOrDefault() is { } posPolicy)
            {
                return new(posMatch.Group.Id, posMatch.Group.PolicyCode, posMatch.Group.PolicyName, "Position", posPolicy);
            }

            // 3. Đơn vị (duyệt ngược lên đơn vị cha gần nhất trước)
            int? cursorUnitId = emp.OrganizationUnitId;
            var visited = new HashSet<int>();
            while (cursorUnitId.HasValue && visited.Add(cursorUnitId.Value))
            {
                var unitMatch = activeGroups
                    .SelectMany(g => g.Assignments.Where(a => a.EmployeeId == null && a.PositionId == null && a.OrganizationUnitId == cursorUnitId.Value && IsEffective(a)).Select(a => new { Group = g, Assignment = a }))
                    .OrderByDescending(x => x.Assignment.Id)
                    .FirstOrDefault();
                if (unitMatch?.Group.AttendancePolicies.FirstOrDefault() is { } unitPolicy)
                {
                    return new(unitMatch.Group.Id, unitMatch.Group.PolicyCode, unitMatch.Group.PolicyName, "OrganizationUnit", unitPolicy);
                }

                cursorUnitId = unitParentMap.GetValueOrDefault(cursorUnitId.Value);
            }
        }

        // 4. Mặc định công ty
        var defaultGroup = activeGroups
            .Where(g => g.IsCompanyDefault && g.AttendancePolicies.Count > 0)
            .OrderByDescending(g => g.Id)
            .FirstOrDefault();

        if (defaultGroup?.AttendancePolicies.FirstOrDefault() is { } defaultPolicy)
        {
            return new(defaultGroup.Id, defaultGroup.PolicyCode, defaultGroup.PolicyName, "CompanyDefault", defaultPolicy);
        }

        return new(0, "FALLBACK", "Chính sách mặc định hệ thống", "DefaultFallback", new AttendancePolicy());
    }

    public static int RoundMinutes(int minutes, int intervalMinutes, string roundingMode)
    {
        if (minutes <= 0 || intervalMinutes <= 1 || string.Equals(roundingMode, "None", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(0, minutes);
        }

        return roundingMode.ToLowerInvariant() switch
        {
            "ceiling" => (int)(Math.Ceiling(minutes / (double)intervalMinutes) * intervalMinutes),
            "floor" => (minutes / intervalMinutes) * intervalMinutes,
            "nearest" => (int)(Math.Round(minutes / (double)intervalMinutes, MidpointRounding.AwayFromZero) * intervalMinutes),
            _ => minutes
        };
    }

    public static (DateTime Start, DateTime End) GetPrimaryShiftWindow(DateOnly workDate, WorkShift shift)
    {
        var start = workDate.ToDateTime(shift.StartTime);
        DateOnly endDate = workDate;
        if (string.Equals(shift.ShiftType, "Split", StringComparison.OrdinalIgnoreCase) && shift.SplitEndTime2.HasValue)
        {
            var seg1End = shift.EndTime <= shift.StartTime ? workDate.AddDays(1).ToDateTime(shift.EndTime) : workDate.ToDateTime(shift.EndTime);
            var seg2Start = workDate.ToDateTime(shift.SplitStartTime2 ?? shift.EndTime);
            if (seg2Start < start)
            {
                seg2Start = seg2Start.AddDays(1);
            }
            var seg2End = workDate.ToDateTime(shift.SplitEndTime2.Value);
            if (seg2End <= seg2Start)
            {
                seg2End = seg2End.AddDays(1);
            }
            return (start, seg2End > seg1End ? seg2End : seg1End);
        }

        if (shift.IsOvernight || string.Equals(shift.ShiftType, "Overnight", StringComparison.OrdinalIgnoreCase) || shift.EndTime <= shift.StartTime)
        {
            endDate = workDate.AddDays(1);
        }

        return (start, endDate.ToDateTime(shift.EndTime));
    }

    public static async Task<(DateOnly AttributedDate, WorkShift Shift)?> ResolveShiftForPunchAsync(
        IApplicationDbContext context,
        int employeeId,
        DateTime punchTime,
        CancellationToken cancellationToken)
    {
        var punchDate = DateOnly.FromDateTime(punchTime);
        var candidateDates = new[] { punchDate.AddDays(-1), punchDate, punchDate.AddDays(1) };

        var schedules = await context.ShiftSchedules
            .AsNoTracking()
            .Include(s => s.WorkShift)
            .Where(s => s.EmployeeId == employeeId
                && !s.IsDayOff
                && candidateDates.Contains(s.WorkDate))
            .ToListAsync(cancellationToken);

        foreach (var sched in schedules.OrderBy(s => s.WorkDate))
        {
            var (winStart, winEnd) = GetPrimaryShiftWindow(sched.WorkDate, sched.WorkShift);
            var earlyBuffer = Math.Max(60, sched.WorkShift.MaxEarlyCheckInMinutes);
            var lateBuffer = Math.Max(120, sched.WorkShift.MaxLateCheckOutMinutes);
            if (punchTime >= winStart.AddMinutes(-earlyBuffer) && punchTime <= winEnd.AddMinutes(lateBuffer))
            {
                return (sched.WorkDate, sched.WorkShift);
            }
        }

        var defaultShift = await context.WorkShifts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.ShiftCode == "HC", cancellationToken)
            ?? await context.WorkShifts.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        return defaultShift != null ? (punchDate, defaultShift) : null;
    }

    public static CalculatedAttendanceResultDto CalculatePunchAttendance(
        DateOnly workDate,
        WorkShift shift,
        DateTime? checkIn,
        DateTime? checkOut,
        AttendancePolicy policy,
        AttendanceStatutoryParameter statutory)
    {
        if (!checkIn.HasValue && !checkOut.HasValue)
        {
            return new(workDate, shift.Id, shift.ShiftCode, "V", 0m, 0m, 0m, 0, 0, 0, "Không có dữ liệu chấm công");
        }

        var effectiveGraceLate = policy.GraceLateMinutes > 0 ? policy.GraceLateMinutes : shift.GraceLateMinutes;
        var effectiveGraceEarly = Math.Max(0, policy.GraceEarlyMinutes);
        var (shiftStart, shiftEnd) = GetPrimaryShiftWindow(workDate, shift);

        DateTime lateAnchorStart = string.Equals(shift.ShiftType, "Flexible", StringComparison.OrdinalIgnoreCase) && shift.CoreStartTime.HasValue
            ? workDate.ToDateTime(shift.CoreStartTime.Value)
            : shiftStart;

        DateTime earlyAnchorEnd = string.Equals(shift.ShiftType, "Flexible", StringComparison.OrdinalIgnoreCase) && shift.CoreEndTime.HasValue
            ? workDate.ToDateTime(shift.CoreEndTime.Value)
            : shiftEnd;

        int rawLate = 0;
        if (checkIn.HasValue && checkIn.Value > lateAnchorStart)
        {
            rawLate = (int)Math.Floor((checkIn.Value - lateAnchorStart).TotalMinutes);
        }

        int lateMinutes = rawLate <= effectiveGraceLate
            ? 0
            : RoundMinutes(rawLate, policy.RoundingIntervalMinutes, policy.RoundingMode);

        int rawEarly = 0;
        if (checkOut.HasValue && checkOut.Value < earlyAnchorEnd)
        {
            rawEarly = (int)Math.Ceiling((earlyAnchorEnd - checkOut.Value).TotalMinutes);
        }

        int earlyLeaveMinutes = rawEarly <= effectiveGraceEarly
            ? 0
            : RoundMinutes(rawEarly, policy.RoundingIntervalMinutes, policy.RoundingMode);

        // Nếu chỉ mới check-in, chưa có check-out: đánh dấu tạm 0.5 công hoặc giữ giờ 0 cho đến khi checkout
        if (!checkIn.HasValue || !checkOut.HasValue || checkOut.Value <= checkIn.Value)
        {
            var inProgressSymbol = lateMinutes > policy.ZeroDayLateThresholdMinutes ? "V"
                : lateMinutes > policy.HalfDayLateThresholdMinutes ? "X/2"
                : "X";
            return new(workDate, shift.Id, shift.ShiftCode, inProgressSymbol, 0m, 0m, 0m, rawLate, lateMinutes, earlyLeaveMinutes, "Chưa đủ 2 lượt vào/ra");
        }

        // Xây dựng các đoạn thời gian thực làm hợp lệ
        var workedSegments = BuildWorkedSegments(workDate, shift, checkIn.Value, checkOut.Value);
        var totalWorkedMinutes = workedSegments.Sum(seg => (decimal)(seg.End - seg.Start).TotalMinutes);
        var workingHours = Math.Round(totalWorkedMinutes / 60m, 2, MidpointRounding.AwayFromZero);

        var nightHours = CalculateNightHours(workedSegments, statutory.NightWorkStartTime, statutory.NightWorkEndTime);

        decimal workdayCount;
        string symbol;

        if (string.Equals(shift.ShiftType, "Hourly", StringComparison.OrdinalIgnoreCase))
        {
            var ratio = shift.StandardHours > 0m ? Math.Min(1m, workingHours / shift.StandardHours) : 1m;
            workdayCount = Math.Round(shift.WorkdayValue * ratio, 2, MidpointRounding.AwayFromZero);
            symbol = workdayCount >= 0.9m ? "X" : workdayCount > 0m ? "X/2" : "V";
        }
        else if (policy.ZeroDayLateThresholdMinutes > 0 && lateMinutes > policy.ZeroDayLateThresholdMinutes)
        {
            workdayCount = 0m;
            symbol = "V";
        }
        else if (policy.HalfDayLateThresholdMinutes > 0 && lateMinutes > policy.HalfDayLateThresholdMinutes)
        {
            workdayCount = Math.Round(shift.WorkdayValue * 0.5m, 2, MidpointRounding.AwayFromZero);
            symbol = "X/2";
        }
        else if (string.Equals(shift.ShiftType, "Flexible", StringComparison.OrdinalIgnoreCase) && shift.StandardHours > 0m && workingHours < shift.StandardHours)
        {
            var ratio = Math.Clamp(workingHours / shift.StandardHours, 0m, 1m);
            workdayCount = Math.Round(shift.WorkdayValue * ratio, 2, MidpointRounding.AwayFromZero);
            symbol = workdayCount >= 0.9m ? "X" : workdayCount > 0m ? "X/2" : "V";
        }
        else
        {
            workdayCount = shift.WorkdayValue;
            symbol = "X";
        }

        var note = $"Ca {shift.ShiftCode} ({shift.ShiftType}) - Chính sách: grace {effectiveGraceLate}p, round {policy.RoundingIntervalMinutes}p ({policy.RoundingMode})";
        return new(workDate, shift.Id, shift.ShiftCode, symbol, workingHours, nightHours, workdayCount, rawLate, lateMinutes, earlyLeaveMinutes, note);
    }

    private static List<(DateTime Start, DateTime End)> BuildWorkedSegments(
        DateOnly workDate,
        WorkShift shift,
        DateTime checkIn,
        DateTime checkOut)
    {
        var rawSegments = new List<(DateTime Start, DateTime End)>();

        if (string.Equals(shift.ShiftType, "Split", StringComparison.OrdinalIgnoreCase) && shift.SplitStartTime2.HasValue && shift.SplitEndTime2.HasValue)
        {
            var seg1Start = workDate.ToDateTime(shift.StartTime);
            var seg1End = shift.EndTime <= shift.StartTime ? workDate.AddDays(1).ToDateTime(shift.EndTime) : workDate.ToDateTime(shift.EndTime);

            var seg2Start = workDate.ToDateTime(shift.SplitStartTime2.Value);
            if (seg2Start < seg1Start) seg2Start = seg2Start.AddDays(1);
            var seg2End = workDate.ToDateTime(shift.SplitEndTime2.Value);
            if (seg2End <= seg2Start) seg2End = seg2End.AddDays(1);

            AddIntersection(rawSegments, checkIn, checkOut, seg1Start, seg1End);
            AddIntersection(rawSegments, checkIn, checkOut, seg2Start, seg2End);
        }
        else
        {
            rawSegments.Add((checkIn, checkOut));
        }

        if (shift.IsBreakPaid || !shift.BreakStartTime.HasValue || !shift.BreakEndTime.HasValue)
        {
            return rawSegments;
        }

        var breakStart = workDate.ToDateTime(shift.BreakStartTime.Value);
        if (breakStart < workDate.ToDateTime(shift.StartTime))
        {
            breakStart = breakStart.AddDays(1);
        }
        var breakEnd = workDate.ToDateTime(shift.BreakEndTime.Value);
        if (breakEnd <= breakStart)
        {
            breakEnd = breakEnd.AddDays(1);
        }

        return SubtractInterval(rawSegments, breakStart, breakEnd);
    }

    private static void AddIntersection(List<(DateTime Start, DateTime End)> list, DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
    {
        var s = aStart > bStart ? aStart : bStart;
        var e = aEnd < bEnd ? aEnd : bEnd;
        if (e > s)
        {
            list.Add((s, e));
        }
    }

    private static List<(DateTime Start, DateTime End)> SubtractInterval(List<(DateTime Start, DateTime End)> segments, DateTime cutStart, DateTime cutEnd)
    {
        var result = new List<(DateTime Start, DateTime End)>();
        foreach (var (s, e) in segments)
        {
            if (cutEnd <= s || cutStart >= e)
            {
                result.Add((s, e));
                continue;
            }
            if (cutStart > s)
            {
                result.Add((s, cutStart));
            }
            if (cutEnd < e)
            {
                result.Add((cutEnd, e));
            }
        }
        return result;
    }

    public static decimal CalculateNightHours(
        IReadOnlyList<(DateTime Start, DateTime End)> workedSegments,
        TimeOnly nightStart,
        TimeOnly nightEnd)
    {
        if (workedSegments.Count == 0)
        {
            return 0m;
        }

        var minDate = DateOnly.FromDateTime(workedSegments.Min(s => s.Start)).AddDays(-1);
        var maxDate = DateOnly.FromDateTime(workedSegments.Max(s => s.End)).AddDays(1);

        decimal totalNightMinutes = 0m;
        for (var d = minDate; d <= maxDate; d = d.AddDays(1))
        {
            var winStart = d.ToDateTime(nightStart);
            var winEnd = nightEnd <= nightStart ? d.AddDays(1).ToDateTime(nightEnd) : d.ToDateTime(nightEnd);

            foreach (var (s, e) in workedSegments)
            {
                var overlapStart = s > winStart ? s : winStart;
                var overlapEnd = e < winEnd ? e : winEnd;
                if (overlapEnd > overlapStart)
                {
                    totalNightMinutes += (decimal)(overlapEnd - overlapStart).TotalMinutes;
                }
            }
        }

        return Math.Round(totalNightMinutes / 60m, 2, MidpointRounding.AwayFromZero);
    }

    public static async Task<IReadOnlyList<ShiftConflictWarningDto>> EvaluateScheduleConflictsAsync(
        IApplicationDbContext context,
        IReadOnlyList<int> employeeIds,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return Array.Empty<ShiftConflictWarningDto>();
        }

        var statutory = await ResolveStatutoryParameterAsync(context, rangeStart, cancellationToken);
        var queryStart = rangeStart.AddDays(-7);
        var queryEnd = rangeEnd.AddDays(7);

        var schedules = await context.ShiftSchedules
            .AsNoTracking()
            .Include(s => s.WorkShift)
            .Include(s => s.Employee)
            .Where(s => employeeIds.Contains(s.EmployeeId)
                && !s.IsDayOff
                && s.WorkDate >= queryStart
                && s.WorkDate <= queryEnd)
            .ToListAsync(cancellationToken);

        var warnings = new List<ShiftConflictWarningDto>();

        foreach (var empGroup in schedules.GroupBy(s => s.EmployeeId))
        {
            var resolvedPolicy = await ResolveAttendancePolicyAsync(context, empGroup.Key, rangeStart, cancellationToken);
            var effectiveMaxWeekly = resolvedPolicy.Policy.MaxWeeklyWorkingHoursOverride is > 0m
                ? Math.Min(statutory.MaxNormalWeeklyHours, resolvedPolicy.Policy.MaxWeeklyWorkingHoursOverride.Value)
                : statutory.MaxNormalWeeklyHours;

            var firstEmp = empGroup.First().Employee;
            var intervals = empGroup
                .Select(s =>
                {
                    var (startDt, endDt) = GetPrimaryShiftWindow(s.WorkDate, s.WorkShift);
                    return new
                    {
                        s.WorkDate,
                        s.WorkShift,
                        Start = startDt,
                        End = endDt
                    };
                })
                .OrderBy(x => x.Start)
                .ThenBy(x => x.End)
                .ToList();

            for (int i = 1; i < intervals.Count; i++)
            {
                var prev = intervals[i - 1];
                var curr = intervals[i];

                if (curr.WorkDate < rangeStart && prev.WorkDate < rangeStart)
                    continue;
                if (curr.WorkDate > rangeEnd && prev.WorkDate > rangeEnd)
                    continue;

                if (prev.End > curr.Start)
                {
                    warnings.Add(new(
                        firstEmp.Id,
                        firstEmp.EmployeeCode,
                        firstEmp.FullName,
                        curr.WorkDate,
                        "Overlap",
                        $"Trùng ca {prev.WorkShift.ShiftCode} ({prev.Start:dd/MM HH:mm}-{prev.End:dd/MM HH:mm}) & {curr.WorkShift.ShiftCode} ({curr.Start:dd/MM HH:mm}-{curr.End:dd/MM HH:mm})",
                        statutory.LegalReference));
                }
                else
                {
                    var restHours = (decimal)(curr.Start - prev.End).TotalHours;
                    if (restHours < statutory.MinInterShiftRestHours)
                    {
                        warnings.Add(new(
                            firstEmp.Id,
                            firstEmp.EmployeeCode,
                            firstEmp.FullName,
                            curr.WorkDate,
                            "InsufficientRest",
                            $"Nghỉ giữa ca {restHours:0.##}h < {statutory.MinInterShiftRestHours:0.##}h ({prev.WorkShift.ShiftCode}→{curr.WorkShift.ShiftCode})",
                            statutory.LegalReference));
                    }
                }
            }

            // Kiểm tra tổng giờ làm theo tuần (Thứ 2 -> Chủ nhật)
            foreach (var weekGroup in intervals.GroupBy(x => GetWeekStartMonday(x.WorkDate)))
            {
                var weekStart = weekGroup.Key;
                var weekEnd = weekStart.AddDays(6);
                if (weekEnd < rangeStart || weekStart > rangeEnd)
                    continue;

                var totalWeeklyHours = weekGroup.Sum(x => x.WorkShift.StandardHours > 0m
                    ? x.WorkShift.StandardHours
                    : Math.Round((decimal)(x.End - x.Start).TotalHours, 2));

                if (totalWeeklyHours > effectiveMaxWeekly)
                {
                    warnings.Add(new(
                        firstEmp.Id,
                        firstEmp.EmployeeCode,
                        firstEmp.FullName,
                        weekStart,
                        "WeeklyHoursExceeded",
                        $"Tổng giờ tuần {totalWeeklyHours:0.##}h > {effectiveMaxWeekly:0.##}h",
                        statutory.LegalReference));
                }
            }
        }

        return warnings;
    }

    public static DateOnly GetWeekStartMonday(DateOnly date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff);
    }

    public static IReadOnlyList<DayOfWeek> ParseWorkdaysMask(string? mask)
    {
        if (string.IsNullOrWhiteSpace(mask))
        {
            return new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        }

        var tokens = mask.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<DayOfWeek>();
        foreach (var token in tokens)
        {
            switch (token.ToLowerInvariant())
            {
                case "mon": case "monday": case "t2": result.Add(DayOfWeek.Monday); break;
                case "tue": case "tuesday": case "t3": result.Add(DayOfWeek.Tuesday); break;
                case "wed": case "wednesday": case "t4": result.Add(DayOfWeek.Wednesday); break;
                case "thu": case "thursday": case "t5": result.Add(DayOfWeek.Thursday); break;
                case "fri": case "friday": case "t6": result.Add(DayOfWeek.Friday); break;
                case "sat": case "saturday": case "t7": result.Add(DayOfWeek.Saturday); break;
                case "sun": case "sunday": case "cn": result.Add(DayOfWeek.Sunday); break;
            }
        }
        return result.Count > 0 ? result : new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
    }

    public static bool IsHolidayApplicable(Holiday holiday, int? employeePolicyGroupId)
    {
        if (holiday.EffectiveFrom.HasValue && holiday.EffectiveFrom.Value > holiday.HolidayDate)
            return false;

        return holiday.PolicyGroupId == null || holiday.PolicyGroupId == employeePolicyGroupId;
    }

    public static Dictionary<DateOnly, Holiday> FilterHolidaysForPolicyGroup(
        IEnumerable<Holiday> allHolidays,
        int? employeePolicyGroupId)
    {
        var result = new Dictionary<DateOnly, Holiday>();
        // Ưu tiên: lễ chung (PolicyGroupId == null) nạp trước, lễ riêng của nhóm ghi đè cùng ngày nếu có
        foreach (var h in allHolidays
                     .Where(x => IsHolidayApplicable(x, employeePolicyGroupId))
                     .OrderBy(x => x.PolicyGroupId.HasValue ? 1 : 0))
        {
            result[h.HolidayDate] = h;
        }
        return result;
    }

    public static async Task<int> EnsureCompensatoryHolidaysForYearAsync(
        IApplicationDbContext context,
        int year,
        CancellationToken cancellationToken)
    {
        var baseHolidays = await context.Holidays
            .Where(h => h.Year == year && !h.IsCompensatoryDay)
            .OrderBy(h => h.HolidayDate)
            .ToListAsync(cancellationToken);

        int createdCount = 0;
        foreach (var h in baseHolidays)
        {
            var created = await EnsureCompensatoryHolidayAsync(context, h, cancellationToken);
            if (created != null)
                createdCount++;
        }

        return createdCount;
    }

    public static async Task<Holiday?> EnsureCompensatoryHolidayAsync(
        IApplicationDbContext context,
        Holiday holiday,
        CancellationToken cancellationToken)
    {
        if (holiday.IsCompensatoryDay)
        {
            return null;
        }

        if (holiday.EffectiveFrom.HasValue && holiday.EffectiveFrom.Value > holiday.HolidayDate)
        {
            return null;
        }

        var activeGroups = await context.PolicyGroups
            .AsNoTracking()
            .Include(g => g.AttendancePolicies)
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.IsCompanyDefault)
            .ThenBy(g => g.Id)
            .ToListAsync(cancellationToken);

        var targets = new List<(int? PolicyGroupId, IReadOnlySet<DayOfWeek> Workdays)>();
        foreach (var pg in activeGroups)
        {
            var ap = pg.AttendancePolicies.FirstOrDefault();
            if (ap == null) continue;
            targets.Add((pg.Id, ParseWorkdaysMask(ap.WeeklyWorkdaysMask).ToHashSet()));
        }

        if (targets.Count == 0)
        {
            targets.Add((null, ParseWorkdaysMask(null).ToHashSet()));
        }

        var allRelevantHolidays = await context.Holidays
            .AsNoTracking()
            .Where(h => h.Year == holiday.Year || h.Year == holiday.Year + 1)
            .ToListAsync(cancellationToken);

        Holiday? firstCreated = null;
        var newlyAdded = new List<Holiday>();

        foreach (var (groupId, workdays) in targets)
        {
            // Chỉ phát sinh nghỉ bù nếu ngày lễ trùng vào NGÀY NGHỈ HẰNG TUẦN của nhóm này
            if (workdays.Contains(holiday.HolidayDate.DayOfWeek))
            {
                continue;
            }

            var existingComp = allRelevantHolidays.FirstOrDefault(h =>
                h.IsCompensatoryDay
                && h.OriginalHolidayDate == holiday.HolidayDate
                && h.PolicyGroupId == groupId);
            if (existingComp != null)
            {
                firstCreated ??= existingComp;
                continue;
            }

            var occupied = allRelevantHolidays
                .Where(h => h.PolicyGroupId == null || h.PolicyGroupId == groupId)
                .Select(h => h.HolidayDate)
                .Concat(newlyAdded.Where(h => h.PolicyGroupId == groupId).Select(h => h.HolidayDate))
                .ToHashSet();

            var candidate = holiday.HolidayDate.AddDays(1);
            for (int i = 0; i < 14; i++)
            {
                if (workdays.Contains(candidate.DayOfWeek) && !occupied.Contains(candidate))
                {
                    string compName = $"Nghỉ bù {holiday.HolidayName}";
                    if (compName.Length > 150)
                        compName = compName[..150];

                    var comp = new Holiday
                    {
                        HolidayDate = candidate,
                        HolidayName = compName,
                        Year = candidate.Year,
                        IsPaidHoliday = holiday.IsPaidHoliday,
                        OvertimeMultiplier = holiday.OvertimeMultiplier,
                        IsCompensatoryDay = true,
                        OriginalHolidayDate = holiday.HolidayDate,
                        EffectiveFrom = holiday.EffectiveFrom,
                        PolicyGroupId = groupId,
                        LegalReference = "Điều 111 khoản 3 BLLĐ 2019 (Nghỉ bù ngày lễ trùng ngày nghỉ hàng tuần)",
                        NeedsHrVerification = true
                    };
                    await context.Holidays.AddAsync(comp, cancellationToken);
                    newlyAdded.Add(comp);
                    firstCreated ??= comp;
                    break;
                }
                candidate = candidate.AddDays(1);
            }
        }

        if (newlyAdded.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return firstCreated;
    }
}
