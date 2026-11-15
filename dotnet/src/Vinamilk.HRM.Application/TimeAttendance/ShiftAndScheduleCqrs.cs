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
    bool NeedsHrVerification = true,
    int? PolicyGroupId = null,
    string? PolicyGroupName = null,
    DateOnly? EffectiveFrom = null);

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
    string? ConflictWarning = null,
    string? Notes = null);

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

public record GenerateHolidaysResultDto(
    int Year,
    int CreatedCount,
    int SkippedCount,
    int CompensatoryCount,
    IReadOnlyList<string> PendingManualItems,
    string SummaryMessage);

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

public static class VietnamHolidayRuleBuilder
{
    public static List<Holiday> BuildDefaultHolidays2026() =>
    [
        new() { HolidayDate = new DateOnly(2026, 1, 1), HolidayName = "Tết Dương lịch 2026", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.a BLLĐ 45/2019/QH14", NeedsHrVerification = false },
        new() { HolidayDate = new DateOnly(2026, 2, 16), HolidayName = "Nghỉ Tết Nguyên đán (29 Tết Âm lịch)", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 45/2019/QH14; Thông báo 9441/TB-BNV", NeedsHrVerification = true },
        new() { HolidayDate = new DateOnly(2026, 2, 17), HolidayName = "Mùng 1 Tết Nguyên đán Bính Ngọ", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 45/2019/QH14; Thông báo 9441/TB-BNV", NeedsHrVerification = true },
        new() { HolidayDate = new DateOnly(2026, 2, 18), HolidayName = "Mùng 2 Tết Nguyên đán Bính Ngọ", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 45/2019/QH14; Thông báo 9441/TB-BNV", NeedsHrVerification = true },
        new() { HolidayDate = new DateOnly(2026, 2, 19), HolidayName = "Mùng 3 Tết Nguyên đán Bính Ngọ", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 45/2019/QH14; Thông báo 9441/TB-BNV", NeedsHrVerification = true },
        new() { HolidayDate = new DateOnly(2026, 2, 20), HolidayName = "Mùng 4 Tết Nguyên đán Bính Ngọ", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.b BLLĐ 45/2019/QH14; Thông báo 9441/TB-BNV", NeedsHrVerification = true },
        new() { HolidayDate = new DateOnly(2026, 4, 26), HolidayName = "Giỗ Tổ Hùng Vương (10/3 Âm lịch)", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.e BLLĐ 45/2019/QH14 (lịch Âm; trùng Chủ nhật - kiểm tra nghỉ bù)", NeedsHrVerification = true },
        new() { HolidayDate = new DateOnly(2026, 4, 30), HolidayName = "Ngày Giải phóng miền Nam 30/4", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.c BLLĐ 45/2019/QH14", NeedsHrVerification = false },
        new() { HolidayDate = new DateOnly(2026, 5, 1), HolidayName = "Ngày Quốc tế Lao động 1/5", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.d BLLĐ 45/2019/QH14", NeedsHrVerification = false },
        new() { HolidayDate = new DateOnly(2026, 9, 1), HolidayName = "Nghỉ liền kề Quốc khánh 2/9 (01/09 theo TB 9441/TB-BNV)", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.đ BLLĐ 45/2019/QH14; Thông báo 9441/TB-BNV (mặc định 01/09, HR chọn 01/09 hoặc 03/09)", NeedsHrVerification = true },
        new() { HolidayDate = new DateOnly(2026, 9, 2), HolidayName = "Quốc khánh nước CHXHCN Việt Nam (02/09)", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.đ BLLĐ 45/2019/QH14; Thông báo 9441/TB-BNV", NeedsHrVerification = false },
        new() { HolidayDate = new DateOnly(2026, 11, 24), HolidayName = "Ngày Văn hóa Việt Nam (24/11)", Year = 2026, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, EffectiveFrom = new DateOnly(2026, 7, 1), LegalReference = "Nghị quyết 28/2026/QH16 về phát triển văn hóa Việt Nam, Điều 2 khoản 1, hiệu lực 01/7/2026 (chủ trương: NQ 80-NQ/TW)", NeedsHrVerification = true }
    ];

    public static List<Holiday> BuildHolidaysForYearFromRules(
        int year,
        DateOnly? tetStartDate,
        DateOnly? hungKingsDate,
        string nationalDayAdjacentChoice = "Before",
        bool includeVietnamCultureDay = true)
    {
        bool chooseBefore = !string.Equals(nationalDayAdjacentChoice?.Trim(), "After", StringComparison.OrdinalIgnoreCase);
        int adjacentDay = chooseBefore ? 1 : 3;

        var list = new List<Holiday>
        {
            new() { HolidayDate = new DateOnly(year, 1, 1), HolidayName = $"Tết Dương lịch {year}", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.a BLLĐ 45/2019/QH14", NeedsHrVerification = false },
            new() { HolidayDate = new DateOnly(year, 4, 30), HolidayName = "Ngày Giải phóng miền Nam 30/4", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.c BLLĐ 45/2019/QH14", NeedsHrVerification = false },
            new() { HolidayDate = new DateOnly(year, 5, 1), HolidayName = "Ngày Quốc tế Lao động 1/5", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.d BLLĐ 45/2019/QH14", NeedsHrVerification = false },
            new() { HolidayDate = new DateOnly(year, 9, 2), HolidayName = "Quốc khánh nước CHXHCN Việt Nam (02/09)", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = "Điều 112.1.đ BLLĐ 45/2019/QH14", NeedsHrVerification = false },
            new() { HolidayDate = new DateOnly(year, 9, adjacentDay), HolidayName = chooseBefore ? "Nghỉ liền kề trước Quốc khánh (01/09)" : "Nghỉ liền kề sau Quốc khánh (03/09)", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m, LegalReference = $"Điều 112.1.đ BLLĐ 45/2019/QH14 (sinh tự động theo quy tắc {(chooseBefore ? "01/09" : "03/09")} - HR xác nhận)", NeedsHrVerification = true }
        };

        if (includeVietnamCultureDay && year >= 2026)
        {
            list.Add(new Holiday
            {
                HolidayDate = new DateOnly(year, 11, 24),
                HolidayName = "Ngày Văn hóa Việt Nam (24/11)",
                Year = year,
                IsPaidHoliday = true,
                OvertimeMultiplier = 3.0m,
                EffectiveFrom = new DateOnly(2026, 7, 1),
                LegalReference = "Nghị quyết 28/2026/QH16 về phát triển văn hóa Việt Nam, Điều 2 khoản 1, hiệu lực 01/7/2026 (chủ trương: NQ 80-NQ/TW)",
                NeedsHrVerification = true
            });
        }

        if (tetStartDate.HasValue)
        {
            for (int i = 0; i < 5; i++)
            {
                var d = tetStartDate.Value.AddDays(i);
                list.Add(new Holiday
                {
                    HolidayDate = d,
                    HolidayName = $"Nghỉ Tết Nguyên đán {year} (Ngày {i + 1}/5)",
                    Year = year,
                    IsPaidHoliday = true,
                    OvertimeMultiplier = 3.0m,
                    LegalReference = "Điều 112.1.b BLLĐ 45/2019/QH14 (lịch Âm do HR nhập theo Thông báo Bộ Nội vụ)",
                    NeedsHrVerification = true
                });
            }
        }

        if (hungKingsDate.HasValue)
        {
            list.Add(new Holiday
            {
                HolidayDate = hungKingsDate.Value,
                HolidayName = $"Giỗ Tổ Hùng Vương {year} (10/3 Âm lịch)",
                Year = year,
                IsPaidHoliday = true,
                OvertimeMultiplier = 3.0m,
                LegalReference = "Điều 112.1.e BLLĐ 45/2019/QH14 (10/3 Âm lịch do HR nhập)",
                NeedsHrVerification = true
            });
        }

        return list.OrderBy(h => h.HolidayDate).ToList();
    }
}

public static class ShiftAndHolidaySeeder
{
    public static async Task EnsureHolidaysAndSampleSchedulesAsync(
        IApplicationDbContext context,
        int year,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        if (year == 2026 && !await context.Holidays.AnyAsync(h => h.Year == 2026, cancellationToken))
        {
            await context.Holidays.AddRangeAsync(VietnamHolidayRuleBuilder.BuildDefaultHolidays2026(), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await AttendancePolicyAndShiftEngine.EnsureCompensatoryHolidaysForYearAsync(context, 2026, cancellationToken);
        }

        var weekEnd = weekStart.AddDays(6);
        var existingWeekSchedules = await context.ShiftSchedules
            .Include(s => s.WorkShift)
            .Include(s => s.Employee).ThenInclude(e => e.OrganizationUnit)
            .Where(s => s.WorkDate >= weekStart && s.WorkDate <= weekEnd)
            .ToListAsync(cancellationToken);

        bool hasUntouchedBrokenSeed = existingWeekSchedules.Count > 0 && existingWeekSchedules
            .All(s => s.Notes is "Lịch phân ca tiêu chuẩn" or "Nghỉ cuối tuần") &&
            (existingWeekSchedules.Any(s =>
                s.Employee.OrganizationUnit.UnitType is not ("Factory" or "Farm")
                && s.WorkDate.DayOfWeek == DayOfWeek.Saturday
                && !s.IsDayOff)
             || existingWeekSchedules
                .Where(s => !s.IsDayOff && s.WorkShift.ShiftCode == "CA3")
                .Any(ca3 => existingWeekSchedules.Any(next =>
                    next.EmployeeId == ca3.EmployeeId
                    && !next.IsDayOff
                    && next.WorkDate == ca3.WorkDate.AddDays(1)
                    && next.WorkShift.ShiftCode == "CA1")));

        if (existingWeekSchedules.Count == 0 || hasUntouchedBrokenSeed)
        {
            if (hasUntouchedBrokenSeed)
            {
                context.ShiftSchedules.RemoveRange(existingWeekSchedules);
                await context.SaveChangesAsync(cancellationToken);
            }

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

            // Chu kỳ xoay 3 ca 4 kíp an toàn >= 12h nghỉ giữa ca: CA1->CA1->CA2->CA2->CA3->CA3->OFF->OFF
            var safeFactoryCycle = new[] { "CA1", "CA1", "CA2", "CA2", "CA3", "CA3", "OFF", "OFF" };

            var newSchedules = new List<ShiftSchedule>();
            int factoryIdx = 0;

            foreach (var emp in employees)
            {
                bool isFactoryOrFarm = emp.OrganizationUnit.UnitType is "Factory" or "Farm";
                var resolved = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(context, emp.Id, weekStart, cancellationToken);
                var workdays = AttendancePolicyAndShiftEngine.ParseWorkdaysMask(resolved.Policy.WeeklyWorkdaysMask);
                int crewOffset = (factoryIdx % 4) * 2;

                for (int d = 0; d < 7; d++)
                {
                    var date = weekStart.AddDays(d);

                    if (isFactoryOrFarm)
                    {
                        var token = safeFactoryCycle[(d + crewOffset) % safeFactoryCycle.Length];
                        bool isOff = token == "OFF";
                        int shiftId = token switch
                        {
                            "CA1" => ca1Shift.Id,
                            "CA2" => ca2Shift.Id,
                            "CA3" => ca3Shift.Id,
                            _ => ca1Shift.Id
                        };
                        newSchedules.Add(new ShiftSchedule
                        {
                            EmployeeId = emp.Id,
                            WorkShiftId = shiftId,
                            WorkDate = date,
                            IsDayOff = isOff,
                            Notes = isOff ? "Nghỉ cuối tuần" : "Lịch phân ca tiêu chuẩn"
                        });
                    }
                    else
                    {
                        bool isOff = !workdays.Contains(date.DayOfWeek);
                        newSchedules.Add(new ShiftSchedule
                        {
                            EmployeeId = emp.Id,
                            WorkShiftId = hcShift.Id,
                            WorkDate = date,
                            IsDayOff = isOff,
                            Notes = isOff ? "Nghỉ cuối tuần" : "Lịch phân ca tiêu chuẩn"
                        });
                    }
                }

                if (isFactoryOrFarm)
                {
                    factoryIdx++;
                }
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
    private readonly ICurrentUserService? _currentUser;

    public GetShiftAndScheduleOverviewQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ShiftAndScheduleOverviewDto> Handle(GetShiftAndScheduleOverviewQuery request, CancellationToken cancellationToken)
    {
        var scope = await Common.Security.DataScopeResolver.ResolveAsync(_context, _currentUser, cancellationToken);
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
            .Include(h => h.PolicyGroup)
            .Where(h => h.Year == selectedYear)
            .OrderBy(h => h.HolidayDate)
            .ThenBy(h => h.PolicyGroupId)
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
            h.NeedsHrVerification,
            h.PolicyGroupId,
            h.PolicyGroup?.PolicyName,
            h.EffectiveFrom)).ToList();

        var holidayByDate = holidays
            .Where(h => !h.IsCompensatoryDay)
            .GroupBy(h => h.HolidayDate)
            .ToDictionary(g => g.Key, g => g.First().HolidayName);

        var unitsQuery = _context.OrganizationUnits.AsNoTracking().AsQueryable();
        var empQuery = _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation");

        if (!scope.IsUnrestricted)
        {
            var allowedUnitIds = scope.AllowedUnitIdList;
            var selfEmpId = scope.CurrentEmployeeId;
            unitsQuery = unitsQuery.Where(u => allowedUnitIds.Contains(u.Id));
            empQuery = empQuery.Where(e =>
                allowedUnitIds.Contains(e.OrganizationUnitId) ||
                (selfEmpId.HasValue && e.Id == selfEmpId.Value));
        }

        var units = await unitsQuery
            .OrderBy(u => u.SortOrder)
            .Select(u => new Workflows.SimpleLookupDto(u.Id, u.UnitCode, u.UnitName))
            .ToListAsync(cancellationToken);

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
            var resolvedEmpPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, emp.Id, weekStart, cancellationToken);
            var empWorkdays = AttendancePolicyAndShiftEngine.ParseWorkdaysMask(resolvedEmpPolicy.Policy.WeeklyWorkdaysMask);
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
                        sched.ConflictWarning,
                        sched.Notes));
                }
                else
                {
                    bool isOff = !empWorkdays.Contains(dt.DayOfWeek);
                    var code = isOff ? "OFF" : (defaultShift?.ShiftCode ?? "HC");
                    var name = isOff ? "Nghỉ tuần (OFF)" : (defaultShift?.ShiftName ?? "Ca Hành chính");
                    var timeText = isOff ? "Nghỉ" : $"{defaultShift?.StartTime:HH\\:mm}-{defaultShift?.EndTime:HH\\:mm}";

                    cells.Add(new ShiftCellDto(
                        null,
                        dt,
                        defaultShift?.Id,
                        code,
                        name,
                        timeText,
                        isOff,
                        isHoliday,
                        holidayName,
                        ShiftAndHolidaySeeder.GetShiftBadgeColor(code, isOff)));
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

internal static class ShiftScheduleValidationHelper
{
    public static async Task EnsureAttendanceMonthsUnlockedAsync(
        IApplicationDbContext context,
        IReadOnlyList<int> employeeIds,
        IReadOnlyList<(int Year, int Month)> months,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0 || months.Count == 0)
            return;

        var monthPairs = months.Distinct().ToList();
        var years = monthPairs.Select(m => m.Year).Distinct().ToList();

        var lockedRows = await context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .Where(m => m.IsLocked && employeeIds.Contains(m.EmployeeId) && years.Contains(m.Year))
            .Select(m => new { m.EmployeeId, m.Year, m.Month })
            .ToListAsync(cancellationToken);

        var conflictMonth = lockedRows.FirstOrDefault(r => monthPairs.Contains((r.Year, r.Month)));
        if (conflictMonth != null)
        {
            throw new InvalidOperationException(
                $"Bảng công tháng {conflictMonth.Month:00}/{conflictMonth.Year} đã khóa, không thể thêm/sửa/xóa lịch phân ca.");
        }
    }

    public static async Task<IReadOnlyList<ShiftConflictWarningDto>> EvaluateAndStampConflictsAsync(
        IApplicationDbContext context,
        IReadOnlyList<int> empIds,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        if (empIds.Count == 0 || !await context.AttendanceStatutoryParameters.AnyAsync(cancellationToken))
            return Array.Empty<ShiftConflictWarningDto>();

        var warnings = await AttendancePolicyAndShiftEngine.EvaluateScheduleConflictsAsync(
            context, empIds, fromDate, toDate, cancellationToken);

        var windowStart = fromDate.AddDays(-1);
        var windowEnd = toDate.AddDays(1);
        var schedules = await context.ShiftSchedules
            .Where(s => empIds.Contains(s.EmployeeId) && s.WorkDate >= windowStart && s.WorkDate <= windowEnd)
            .ToListAsync(cancellationToken);

        foreach (var sched in schedules)
        {
            if (sched.IsDayOff)
            {
                sched.ConflictWarning = null;
                continue;
            }

            var msgs = warnings
                .Where(w => w.EmployeeId == sched.EmployeeId && (w.WorkDate == sched.WorkDate || w.ConflictType == "WeeklyHoursExceeded"))
                .Select(w => w.Message)
                .Distinct()
                .ToList();
            sched.ConflictWarning = msgs.Count > 0 ? string.Join(" | ", msgs) : null;
        }

        await context.SaveChangesAsync(cancellationToken);
        return warnings;
    }

    public static async Task WriteScheduleAuditAsync(
        IApplicationDbContext context,
        ICurrentUserService? currentUser,
        string actionType,
        string entityId,
        string summaryJson,
        CancellationToken cancellationToken)
    {
        if (currentUser?.UserId is not int uid)
            return;

        await context.AuditLogs.AddAsync(new Domain.Entities.SystemEntities.AuditLog
        {
            UserId = uid,
            Username = currentUser.Username ?? "hr.admin",
            ActionType = actionType,
            ModuleName = "TimeAttendance",
            EntityName = "ShiftSchedule",
            EntityId = entityId,
            NewValuesJson = summaryJson,
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public class AssignShiftScheduleCommandHandler :
    IRequestHandler<AssignShiftScheduleCommand, int>,
    IRequestHandler<AssignShiftScheduleDetailedCommand, AssignScheduleResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public AssignShiftScheduleCommandHandler(IApplicationDbContext context, ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
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

        var coveredMonths = new List<(int Year, int Month)>();
        for (var dt = fromDate; dt <= toDate; dt = dt.AddDays(1))
        {
            var pair = (dt.Year, dt.Month);
            if (!coveredMonths.Contains(pair))
                coveredMonths.Add(pair);
        }
        await ShiftScheduleValidationHelper.EnsureAttendanceMonthsUnlockedAsync(_context, empIds, coveredMonths, cancellationToken);

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

        var warnings = await ShiftScheduleValidationHelper.EvaluateAndStampConflictsAsync(
            _context, empIds, fromDate, toDate, cancellationToken);

        await ShiftScheduleValidationHelper.WriteScheduleAuditAsync(
            _context,
            _currentUser,
            "AssignShiftSchedule",
            employeeId?.ToString() ?? $"unit:{organizationUnitId}",
            System.Text.Json.JsonSerializer.Serialize(new
            {
                employees = empIds.Count,
                from = fromDate.ToString("yyyy-MM-dd"),
                to = toDate.ToString("yyyy-MM-dd"),
                workShiftId = targetShiftId,
                isDayOff,
                notes
            }),
            cancellationToken);

        return new AssignScheduleResultDto(affected, warnings);
    }
}

public record ShiftCellTargetDto(int EmployeeId, DateOnly WorkDate);

public record DeleteShiftScheduleCommand(
    int EmployeeId,
    DateOnly WorkDate,
    string? Reason = null) : IRequest<AssignScheduleResultDto>;

public class DeleteShiftScheduleCommandHandler : IRequestHandler<DeleteShiftScheduleCommand, AssignScheduleResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public DeleteShiftScheduleCommandHandler(IApplicationDbContext context, ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<AssignScheduleResultDto> Handle(DeleteShiftScheduleCommand request, CancellationToken cancellationToken)
    {
        var empIds = new[] { request.EmployeeId };
        await ShiftScheduleValidationHelper.EnsureAttendanceMonthsUnlockedAsync(
            _context, empIds, new[] { (request.WorkDate.Year, request.WorkDate.Month) }, cancellationToken);

        var existing = await _context.ShiftSchedules
            .Where(s => s.EmployeeId == request.EmployeeId && s.WorkDate == request.WorkDate)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            var emptyWarnings = await ShiftScheduleValidationHelper.EvaluateAndStampConflictsAsync(
                _context, empIds, request.WorkDate, request.WorkDate, cancellationToken);
            return new AssignScheduleResultDto(0, emptyWarnings);
        }

        foreach (var item in existing)
            item.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);

        var warnings = await ShiftScheduleValidationHelper.EvaluateAndStampConflictsAsync(
            _context, empIds, request.WorkDate, request.WorkDate, cancellationToken);

        await ShiftScheduleValidationHelper.WriteScheduleAuditAsync(
            _context,
            _currentUser,
            "DeleteShiftSchedule",
            $"{request.EmployeeId}:{request.WorkDate:yyyy-MM-dd}",
            System.Text.Json.JsonSerializer.Serialize(new
            {
                employeeId = request.EmployeeId,
                workDate = request.WorkDate.ToString("yyyy-MM-dd"),
                removedCount = existing.Count,
                reason = request.Reason
            }),
            cancellationToken);

        return new AssignScheduleResultDto(existing.Count, warnings);
    }
}

public record BulkAssignShiftCellsCommand(
    IReadOnlyList<ShiftCellTargetDto> Cells,
    int? WorkShiftId,
    bool IsDayOff = false,
    bool DeleteSchedule = false,
    string? Notes = null) : IRequest<AssignScheduleResultDto>;

public class BulkAssignShiftCellsCommandHandler : IRequestHandler<BulkAssignShiftCellsCommand, AssignScheduleResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public BulkAssignShiftCellsCommandHandler(IApplicationDbContext context, ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<AssignScheduleResultDto> Handle(BulkAssignShiftCellsCommand request, CancellationToken cancellationToken)
    {
        var cells = (request.Cells ?? Array.Empty<ShiftCellTargetDto>()).Distinct().ToList();
        if (cells.Count == 0)
            return new AssignScheduleResultDto(0, Array.Empty<ShiftConflictWarningDto>());

        var empIds = cells.Select(c => c.EmployeeId).Distinct().ToList();
        var months = cells.Select(c => (c.WorkDate.Year, c.WorkDate.Month)).Distinct().ToList();
        await ShiftScheduleValidationHelper.EnsureAttendanceMonthsUnlockedAsync(_context, empIds, months, cancellationToken);

        var minDate = cells.Min(c => c.WorkDate);
        var maxDate = cells.Max(c => c.WorkDate);
        var defaultShift = await _context.WorkShifts.AsNoTracking().FirstAsync(cancellationToken);
        int targetShiftId = request.WorkShiftId ?? defaultShift.Id;

        var existing = await _context.ShiftSchedules
            .Where(s => empIds.Contains(s.EmployeeId) && s.WorkDate >= minDate && s.WorkDate <= maxDate)
            .ToListAsync(cancellationToken);

        int affected = 0;
        foreach (var cell in cells)
        {
            var onDay = existing.Where(s => s.EmployeeId == cell.EmployeeId && s.WorkDate == cell.WorkDate).ToList();
            if (request.DeleteSchedule)
            {
                foreach (var item in onDay)
                {
                    item.IsDeleted = true;
                    affected++;
                }
                continue;
            }

            var primary = onDay.FirstOrDefault();
            if (primary != null)
            {
                primary.WorkShiftId = targetShiftId;
                primary.IsDayOff = request.IsDayOff;
                primary.Notes = request.Notes ?? "Cập nhật hàng loạt trên Lịch phân ca";
                primary.ConflictWarning = null;
                foreach (var extra in onDay.Where(x => x.Id != primary.Id))
                    extra.IsDeleted = true;
            }
            else
            {
                await _context.ShiftSchedules.AddAsync(new ShiftSchedule
                {
                    EmployeeId = cell.EmployeeId,
                    WorkShiftId = targetShiftId,
                    WorkDate = cell.WorkDate,
                    IsDayOff = request.IsDayOff,
                    Notes = request.Notes ?? "Phân ca hàng loạt trên Lịch phân ca"
                }, cancellationToken);
            }
            affected++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        var warnings = await ShiftScheduleValidationHelper.EvaluateAndStampConflictsAsync(
            _context, empIds, minDate, maxDate, cancellationToken);

        await ShiftScheduleValidationHelper.WriteScheduleAuditAsync(
            _context,
            _currentUser,
            request.DeleteSchedule ? "BulkDeleteShiftCells" : "BulkAssignShiftCells",
            $"{cells.Count}cells",
            System.Text.Json.JsonSerializer.Serialize(new
            {
                cellCount = cells.Count,
                from = minDate.ToString("yyyy-MM-dd"),
                to = maxDate.ToString("yyyy-MM-dd"),
                request.DeleteSchedule,
                request.IsDayOff,
                workShiftId = targetShiftId
            }),
            cancellationToken);

        return new AssignScheduleResultDto(affected, warnings);
    }
}

public record CopyPreviousWeekScheduleCommand(
    DateOnly TargetWeekStart,
    int? OrganizationUnitId = null) : IRequest<AssignScheduleResultDto>;

public class CopyPreviousWeekScheduleCommandHandler : IRequestHandler<CopyPreviousWeekScheduleCommand, AssignScheduleResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public CopyPreviousWeekScheduleCommandHandler(IApplicationDbContext context, ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<AssignScheduleResultDto> Handle(CopyPreviousWeekScheduleCommand request, CancellationToken cancellationToken)
    {
        var targetEnd = request.TargetWeekStart.AddDays(6);
        var prevStart = request.TargetWeekStart.AddDays(-7);
        var prevEnd = request.TargetWeekStart.AddDays(-1);

        var empQuery = _context.Employees.AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation");
        if (request.OrganizationUnitId.HasValue && request.OrganizationUnitId.Value > 0)
        {
            var unit = await _context.OrganizationUnits.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.OrganizationUnitId.Value, cancellationToken);
            if (unit != null && !string.IsNullOrEmpty(unit.TreePath))
                empQuery = empQuery.Where(e => e.OrganizationUnit.TreePath!.StartsWith(unit.TreePath));
            else
                empQuery = empQuery.Where(e => e.OrganizationUnitId == request.OrganizationUnitId.Value);
        }

        var empIds = await empQuery.Select(e => e.Id).ToListAsync(cancellationToken);
        if (empIds.Count == 0)
            return new AssignScheduleResultDto(0, Array.Empty<ShiftConflictWarningDto>());

        var coveredMonths = Enumerable.Range(0, 7)
            .Select(i => request.TargetWeekStart.AddDays(i))
            .Select(d => (d.Year, d.Month))
            .Distinct()
            .ToList();
        await ShiftScheduleValidationHelper.EnsureAttendanceMonthsUnlockedAsync(_context, empIds, coveredMonths, cancellationToken);

        var prevSchedules = await _context.ShiftSchedules.AsNoTracking()
            .Where(s => empIds.Contains(s.EmployeeId) && s.WorkDate >= prevStart && s.WorkDate <= prevEnd)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        if (prevSchedules.Count == 0)
            throw new InvalidOperationException($"Tuần trước ({prevStart:dd/MM/yyyy} - {prevEnd:dd/MM/yyyy}) chưa có dữ liệu phân ca để sao chép.");

        var existingTarget = await _context.ShiftSchedules
            .Where(s => empIds.Contains(s.EmployeeId) && s.WorkDate >= request.TargetWeekStart && s.WorkDate <= targetEnd)
            .ToListAsync(cancellationToken);

        int affected = 0;
        foreach (var prev in prevSchedules)
        {
            var targetDate = prev.WorkDate.AddDays(7);
            var onDay = existingTarget.Where(s => s.EmployeeId == prev.EmployeeId && s.WorkDate == targetDate).ToList();
            var primary = onDay.FirstOrDefault();
            if (primary != null)
            {
                primary.WorkShiftId = prev.WorkShiftId;
                primary.IsDayOff = prev.IsDayOff;
                primary.Notes = $"Sao chép từ {prev.WorkDate:dd/MM/yyyy}";
                primary.ConflictWarning = null;
                foreach (var extra in onDay.Where(e => e.Id != primary.Id))
                    extra.IsDeleted = true;
            }
            else
            {
                await _context.ShiftSchedules.AddAsync(new ShiftSchedule
                {
                    EmployeeId = prev.EmployeeId,
                    WorkShiftId = prev.WorkShiftId,
                    WorkDate = targetDate,
                    IsDayOff = prev.IsDayOff,
                    Notes = $"Sao chép từ {prev.WorkDate:dd/MM/yyyy}"
                }, cancellationToken);
            }
            affected++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var warnings = await ShiftScheduleValidationHelper.EvaluateAndStampConflictsAsync(
            _context, empIds, request.TargetWeekStart, targetEnd, cancellationToken);

        await ShiftScheduleValidationHelper.WriteScheduleAuditAsync(
            _context,
            _currentUser,
            "CopyPreviousWeekSchedule",
            request.TargetWeekStart.ToString("yyyy-MM-dd"),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                targetWeekStart = request.TargetWeekStart.ToString("yyyy-MM-dd"),
                unitId = request.OrganizationUnitId,
                affected
            }),
            cancellationToken);

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

/// <summary>
/// Sinh lịch ngày nghỉ lễ năm N từ quy tắc ngày Dương cố định kết hợp ngày Âm lịch (Tết Nguyên đán 5 ngày, Giỗ Tổ 10/3 ÂL) do HR nhập.
/// </summary>
public record GenerateHolidaysForYearCommand(
    int Year,
    DateOnly? TetStartDate = null,
    DateOnly? HungKingsDate = null,
    string NationalDayAdjacentChoice = "Before",
    bool IncludeVietnamCultureDay = true,
    bool AutoGenerateCompensatoryIfWeekend = true,
    bool OverwriteExistingAutoGenerated = false) : IRequest<GenerateHolidaysResultDto>;

public class GenerateHolidaysForYearCommandHandler : IRequestHandler<GenerateHolidaysForYearCommand, GenerateHolidaysResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public GenerateHolidaysForYearCommandHandler(IApplicationDbContext context, ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<GenerateHolidaysResultDto> Handle(GenerateHolidaysForYearCommand request, CancellationToken cancellationToken)
    {
        if (request.Year is < 2020 or > 2100)
            throw new InvalidOperationException("Năm sinh lịch lễ phải nằm trong khoảng 2020–2100.");

        if (request.TetStartDate.HasValue && Math.Abs(request.TetStartDate.Value.Year - request.Year) > 1)
            throw new InvalidOperationException($"Ngày bắt đầu Tết Nguyên đán ({request.TetStartDate:dd/MM/yyyy}) không khớp năm {request.Year}.");

        if (request.HungKingsDate.HasValue && request.HungKingsDate.Value.Year != request.Year)
            throw new InvalidOperationException($"Ngày Giỗ Tổ Hùng Vương ({request.HungKingsDate:dd/MM/yyyy}) phải thuộc năm {request.Year}.");

        var generated = VietnamHolidayRuleBuilder.BuildHolidaysForYearFromRules(
            request.Year,
            request.TetStartDate,
            request.HungKingsDate,
            request.NationalDayAdjacentChoice,
            request.IncludeVietnamCultureDay);

        var targetDates = generated.Select(g => g.HolidayDate).ToList();
        var existingByDate = await _context.Holidays
            .IgnoreQueryFilters()
            .Where(h => h.PolicyGroupId == null && targetDates.Contains(h.HolidayDate))
            .ToDictionaryAsync(h => h.HolidayDate, cancellationToken);

        int createdCount = 0;
        int skippedCount = 0;
        var addedHolidays = new List<Holiday>();

        foreach (var item in generated)
        {
            if (existingByDate.TryGetValue(item.HolidayDate, out var existing))
            {
                if (!existing.IsDeleted && !request.OverwriteExistingAutoGenerated)
                {
                    skippedCount++;
                    continue;
                }

                existing.IsDeleted = false;
                existing.HolidayName = item.HolidayName;
                existing.Year = item.Year;
                existing.IsPaidHoliday = item.IsPaidHoliday;
                existing.OvertimeMultiplier = item.OvertimeMultiplier;
                existing.LegalReference = item.LegalReference;
                existing.NeedsHrVerification = item.NeedsHrVerification;
                createdCount++;
                addedHolidays.Add(existing);
            }
            else
            {
                await _context.Holidays.AddAsync(item, cancellationToken);
                createdCount++;
                addedHolidays.Add(item);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        int compCount = 0;
        if (request.AutoGenerateCompensatoryIfWeekend)
        {
            compCount = await AttendancePolicyAndShiftEngine.EnsureCompensatoryHolidaysForYearAsync(
                _context, request.Year, cancellationToken);
        }

        var pending = new List<string>();
        if (!request.TetStartDate.HasValue && !await _context.Holidays.AnyAsync(h => h.Year == request.Year && (h.HolidayDate.Month == 1 || h.HolidayDate.Month == 2) && h.HolidayName.Contains("Tết Nguyên đán"), cancellationToken))
            pending.Add("Tết Nguyên đán (5 ngày Âm lịch - HR cần nhập ngày bắt đầu nghỉ)");
        if (!request.HungKingsDate.HasValue && !await _context.Holidays.AnyAsync(h => h.Year == request.Year && h.HolidayName.Contains("Giỗ Tổ"), cancellationToken))
            pending.Add("Giỗ Tổ Hùng Vương 10/3 Âm lịch (HR cần nhập ngày quy đổi Dương lịch)");

        if (_currentUser?.UserId is int uid)
        {
            await _context.AuditLogs.AddAsync(new Domain.Entities.SystemEntities.AuditLog
            {
                UserId = uid,
                Username = _currentUser.Username ?? "hr.admin",
                ActionType = "GenerateHolidaysForYear",
                ModuleName = "TimeAttendance",
                EntityName = "Holiday",
                EntityId = request.Year.ToString(),
                NewValuesJson = $"{{\"year\":{request.Year},\"created\":{createdCount},\"skipped\":{skippedCount},\"compensatory\":{compCount}}}",
                IpAddress = _currentUser.IpAddress,
                UserAgent = _currentUser.UserAgent,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        string msg = $"Đã sinh lịch lễ năm {request.Year}: thêm/cập nhật {createdCount} ngày lễ (bỏ qua {skippedCount} ngày đã tồn tại"
            + (compCount > 0 ? $", tự sinh {compCount} ngày nghỉ bù cuối tuần" : string.Empty)
            + ").";

        return new GenerateHolidaysResultDto(request.Year, createdCount, skippedCount, compCount, pending, msg);
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
    private readonly ICurrentUserService? _currentUser;

    public GenerateRotationScheduleCommandHandler(IApplicationDbContext context, ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
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
                var unit = await _context.OrganizationUnits.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == request.OrganizationUnitId.Value, cancellationToken);
                if (unit != null && !string.IsNullOrEmpty(unit.TreePath))
                    empQuery = empQuery.Where(e => e.OrganizationUnit.TreePath!.StartsWith(unit.TreePath));
                else
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

        var coveredMonths = new List<(int Year, int Month)>();
        for (var dt = request.StartDate; dt <= endDate; dt = dt.AddDays(1))
        {
            var pair = (dt.Year, dt.Month);
            if (!coveredMonths.Contains(pair))
                coveredMonths.Add(pair);
        }
        await ShiftScheduleValidationHelper.EnsureAttendanceMonthsUnlockedAsync(_context, allEmpIds, coveredMonths, cancellationToken);

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

        var warnings = await ShiftScheduleValidationHelper.EvaluateAndStampConflictsAsync(
            _context, allEmpIds, request.StartDate, endDate, cancellationToken);

        await ShiftScheduleValidationHelper.WriteScheduleAuditAsync(
            _context,
            _currentUser,
            "GenerateRotationSchedule",
            pattern.PatternCode,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                pattern = pattern.PatternCode,
                startDate = request.StartDate.ToString("yyyy-MM-dd"),
                weeksCount = request.WeeksCount,
                unitId = request.OrganizationUnitId,
                affected
            }),
            cancellationToken);

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

