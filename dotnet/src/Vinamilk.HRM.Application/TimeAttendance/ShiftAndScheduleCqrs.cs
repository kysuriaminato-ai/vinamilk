using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
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
    string BadgeColorClass);

public record HolidayItemDto(
    int Id,
    DateOnly HolidayDate,
    string DayOfWeekVi,
    string HolidayName,
    int Year,
    bool IsPaidHoliday,
    decimal OvertimeMultiplier);

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
    string BadgeColorClass);

public record EmployeeShiftRowDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    string PositionName,
    IReadOnlyList<ShiftCellDto> DayCells);

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
    int TotalHolidayDaysInYear);

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
                new() { HolidayDate = new DateOnly(year, 1, 1), HolidayName = "Tết Dương lịch", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 2, 16), HolidayName = "Nghỉ Tết Nguyên đán (29 Tết)", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 2, 17), HolidayName = "Mùng 1 Tết Nguyên đán", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 2, 18), HolidayName = "Mùng 2 Tết Nguyên đán", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 2, 19), HolidayName = "Mùng 3 Tết Nguyên đán", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 2, 20), HolidayName = "Mùng 4 Tết Nguyên đán", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 4, 26), HolidayName = "Giỗ Tổ Hùng Vương (10/3 Âm lịch)", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 4, 30), HolidayName = "Ngày Giải phóng miền Nam 30/4", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 5, 1), HolidayName = "Ngày Quốc tế Lao động 1/5", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 9, 2), HolidayName = "Quốc khánh nước CHXHCN Việt Nam 2/9", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m },
                new() { HolidayDate = new DateOnly(year, 9, 3), HolidayName = "Nghỉ liền kề Quốc khánh 2/9", Year = year, IsPaidHoliday = true, OvertimeMultiplier = 3.0m }
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
            ShiftAndHolidaySeeder.GetShiftBadgeColor(s.ShiftCode, false))).ToList();

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
            h.OvertimeMultiplier)).ToList();

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
                        ShiftAndHolidaySeeder.GetShiftBadgeColor(code, sched.IsDayOff)));
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
            holidays.Count);
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
    int GraceLateMinutes) : IRequest<int>;

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

        shift.ShiftCode = request.ShiftCode.Trim().ToUpperInvariant();
        shift.ShiftName = request.ShiftName.Trim();
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.BreakStartTime = request.BreakStartTime;
        shift.BreakEndTime = request.BreakEndTime;
        shift.IsOvernight = request.IsOvernight;
        shift.StandardHours = request.StandardHours;
        shift.WorkdayValue = request.WorkdayValue;
        shift.NightShiftAllowanceRate = request.NightShiftAllowanceRate;
        shift.GraceLateMinutes = request.GraceLateMinutes;

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
    string? Notes = null) : IRequest<int>;

public class AssignShiftScheduleCommandHandler : IRequestHandler<AssignShiftScheduleCommand, int>
{
    private readonly IApplicationDbContext _context;

    public AssignShiftScheduleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AssignShiftScheduleCommand request, CancellationToken cancellationToken)
    {
        var defaultShift = await _context.WorkShifts.AsNoTracking().FirstAsync(cancellationToken);
        int targetShiftId = request.WorkShiftId ?? defaultShift.Id;

        var empIds = new List<int>();
        if (request.EmployeeId.HasValue && request.EmployeeId.Value > 0)
        {
            empIds.Add(request.EmployeeId.Value);
        }
        else if (request.OrganizationUnitId.HasValue && request.OrganizationUnitId.Value > 0)
        {
            empIds = await _context.Employees
                .AsNoTracking()
                .Where(e => e.OrganizationUnitId == request.OrganizationUnitId.Value &&
                            (e.WorkStatus == "Official" || e.WorkStatus == "Probation"))
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);
        }

        if (empIds.Count == 0)
            return 0;

        var existingSchedules = await _context.ShiftSchedules
            .Where(s => empIds.Contains(s.EmployeeId) && s.WorkDate >= request.FromDate && s.WorkDate <= request.ToDate)
            .ToListAsync(cancellationToken);

        int affected = 0;
        foreach (var empId in empIds)
        {
            for (var dt = request.FromDate; dt <= request.ToDate; dt = dt.AddDays(1))
            {
                var existing = existingSchedules.FirstOrDefault(s => s.EmployeeId == empId && s.WorkDate == dt);
                if (existing != null)
                {
                    existing.WorkShiftId = targetShiftId;
                    existing.IsDayOff = request.IsDayOff;
                    existing.Notes = request.Notes ?? "Cập nhật từ lịch phân ca kéo-thả";
                }
                else
                {
                    await _context.ShiftSchedules.AddAsync(new ShiftSchedule
                    {
                        EmployeeId = empId,
                        WorkShiftId = targetShiftId,
                        WorkDate = dt,
                        IsDayOff = request.IsDayOff,
                        Notes = request.Notes ?? "Phân ca từ lịch kéo-thả"
                    }, cancellationToken);
                }
                affected++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return affected;
    }
}

// ============================================================================
// 6. COMMAND: QUẢN LÝ NGÀY LỄ TẾT (SAVE / DELETE HOLIDAY)
// ============================================================================

public record SaveHolidayCommand(
    int? Id,
    DateOnly HolidayDate,
    string HolidayName,
    bool IsPaidHoliday = true,
    decimal OvertimeMultiplier = 3.0m) : IRequest<int>;

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

        await _context.SaveChangesAsync(cancellationToken);
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

