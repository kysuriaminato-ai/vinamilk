using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Shifts;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public class AttendanceCalculationService : IAttendanceCalculationService
{
    private readonly IApplicationDbContext _context;

    // Dung sai đi muộn / về sớm cho phép (mặc định 5 phút)
    private const int LateToleranceMinutes = 5;
    private const int EarlyLeaveToleranceMinutes = 5;

    public AttendanceCalculationService(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DailyAttendance> CalculateDailyAttendanceAsync(
        int employeeId,
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
        // 1. Lấy thông tin lịch phân ca (RosterEntry)
        var rosterEntry = await _context.RosterEntries
            .Include(r => r.ShiftDefinition)
                .ThenInclude(s => s!.Segments)
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.WorkDate == workDate, cancellationToken);

        // 2. Lấy hoặc khởi tạo bản ghi DailyAttendance
        var dailyAtt = await _context.DailyAttendances
            .Include(d => d.Segments)
            .FirstOrDefaultAsync(d => d.EmployeeId == employeeId && d.AttendanceDate == workDate, cancellationToken);

        if (dailyAtt == null)
        {
            dailyAtt = new DailyAttendance(
                rosterEntry?.Id,
                employeeId,
                workDate,
                rosterEntry?.ShiftDefinitionId);
            _context.DailyAttendances.Add(dailyAtt);
        }

        dailyAtt.ClearSegments();

        // 3. Nếu là ngày nghỉ OFF
        if (rosterEntry == null || rosterEntry.IsOff || rosterEntry.ShiftDefinition == null)
        {
            dailyAtt.SetSummary(
                totalWorkHours: 0m,
                lateMinutes: 0,
                earlyLeaveMinutes: 0,
                nightHours: 0m,
                regularOtHours: 0m,
                restDayOtHours: 0m,
                holidayOtHours: 0m,
                nightOtHours: 0m,
                unapprovedOtHours: 0m,
                attendanceCode: "OFF",
                workDayCredit: 0m,
                isComplete: true,
                status: AttendanceStatus.Present,
                hasWarning: false,
                remarks: "Ngày nghỉ theo lịch (OFF)");

            await _context.SaveChangesAsync(cancellationToken);
            return dailyAtt;
        }

        var shift = rosterEntry.ShiftDefinition;
        var segments = shift.Segments.OrderBy(s => s.SegmentIndex).ToList();
        if (segments.Count == 0)
        {
            // Nếu ca chưa cấu hình segment, tạo mặc định 1 segment 08:00 - 17:00
            segments.Add(new ShiftSegment(shift.Id, 1, new TimeOnly(8, 0), new TimeOnly(17, 0), 60, false));
        }

        // 4. Xác định cửa sổ thời gian quét TimeEvents
        // Bắt đầu: từ 2 giờ trước giờ vào phân đoạn đầu tiên
        // Kết thúc: đến 4 giờ sau giờ ra phân đoạn cuối cùng
        var firstStart = segments.First().StartTime;
        var lastEnd = segments.Last().EndTime;

        DateTime windowStartLocal = workDate.ToDateTime(firstStart).AddHours(-2);
        DateTime windowEndLocal = workDate.ToDateTime(lastEnd).AddHours(4);
        if (shift.CrossesMidnight || lastEnd < firstStart)
        {
            windowEndLocal = workDate.AddDays(1).ToDateTime(lastEnd).AddHours(4);
        }

        var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // UTC+7
        var windowStartUtc = TimeZoneInfo.ConvertTimeToUtc(windowStartLocal, tz);
        var windowEndUtc = TimeZoneInfo.ConvertTimeToUtc(windowEndLocal, tz);

        // Lấy tất cả các lần quẹt trong cửa sổ
        var punches = await _context.TimeEvents
            .Where(t => t.EmployeeId == employeeId &&
                        t.Timestamp >= windowStartUtc &&
                        t.Timestamp <= windowEndUtc)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(cancellationToken);

        // Chuyển punches sang giờ địa phương Việt Nam để xử lý so khớp
        var localPunches = punches.Select(p => TimeZoneInfo.ConvertTime(p.Timestamp, tz)).ToList();

        // 5. Kiểm tra phiếu duyệt làm thêm giờ (OvertimeRequest)
        var approvedOt = await _context.OvertimeRequests
            .FirstOrDefaultAsync(o => o.EmployeeId == employeeId &&
                                      o.WorkDate == workDate &&
                                      o.Status == OvertimeStatus.Approved, cancellationToken);

        // 6. Kiểm tra ngày lễ (PublicHoliday)
        bool isHoliday = await _context.PublicHolidays.AnyAsync(h => h.Date == workDate, cancellationToken);

        // 7. Xử lý tính toán cho từng phân đoạn ca
        decimal totalWorkHours = 0m;
        int totalLateMinutes = 0;
        int totalEarlyLeaveMinutes = 0;
        decimal totalNightHours = 0m;
        decimal unapprovedOtHours = 0m;
        decimal approvedOtHours = 0m;
        bool hasWarning = false;
        string? warningReason = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            DateTime segPlannedStart = workDate.ToDateTime(seg.StartTime);
            DateTime segPlannedEnd = (seg.EndTime >= seg.StartTime)
                ? workDate.ToDateTime(seg.EndTime)
                : workDate.AddDays(1).ToDateTime(seg.EndTime);

            // Cửa sổ ghép quẹt cho đoạn này
            DateTime segWindowStart = segPlannedStart.AddHours(-2);
            DateTime segWindowEnd = (i < segments.Count - 1)
                ? workDate.ToDateTime(segments[i + 1].StartTime)
                : segPlannedEnd.AddHours(4);

            var segPunches = localPunches
                .Where(p => p.DateTime >= segWindowStart && p.DateTime <= segWindowEnd)
                .OrderBy(p => p)
                .ToList();

            DateTimeOffset? checkIn = null;
            DateTimeOffset? checkOut = null;
            int segLate = 0;
            int segEarly = 0;
            decimal segWorkHours = 0m;
            decimal segNightHours = 0m;

            if (segPunches.Count >= 2)
            {
                checkIn = segPunches.First();
                checkOut = segPunches.Last();
            }
            else if (segPunches.Count == 1)
            {
                // Chỉ có 1 lần quẹt
                if (Math.Abs((segPunches[0].DateTime - segPlannedStart).TotalHours) <= 1.5)
                {
                    checkIn = segPunches[0];
                }
                else
                {
                    checkOut = segPunches[0];
                }
            }

            // Tính đi muộn
            if (checkIn.HasValue)
            {
                if (checkIn.Value.DateTime > segPlannedStart.AddMinutes(LateToleranceMinutes))
                {
                    segLate = (int)(checkIn.Value.DateTime - segPlannedStart).TotalMinutes;
                }
            }

            // Tính về sớm
            if (checkOut.HasValue)
            {
                if (checkOut.Value.DateTime < segPlannedEnd.AddMinutes(-EarlyLeaveToleranceMinutes))
                {
                    segEarly = (int)(segPlannedEnd - checkOut.Value.DateTime).TotalMinutes;
                }
            }

            // Tính giờ làm việc & giờ đêm
            if (checkIn.HasValue && checkOut.HasValue)
            {
                var effectiveStart = checkIn.Value.DateTime > segPlannedStart ? checkIn.Value.DateTime : segPlannedStart;
                var effectiveEnd = checkOut.Value.DateTime < segPlannedEnd ? checkOut.Value.DateTime : segPlannedEnd;

                if (effectiveEnd > effectiveStart)
                {
                    double netMinutes = (effectiveEnd - effectiveStart).TotalMinutes;
                    if (!seg.IsPaidBreak && seg.BreakMinutes > 0)
                    {
                        netMinutes = Math.Max(0, netMinutes - seg.BreakMinutes);
                    }
                    segWorkHours = Math.Round((decimal)(netMinutes / 60.0), 2, MidpointRounding.AwayFromZero);
                }

                // Tính số giờ trong khung đêm 22:00 - 06:00
                segNightHours = CalculateNightHours(effectiveStart, effectiveEnd);

                // Kiểm tra xem nhân viên có làm việc sau giờ kết thúc ca kế hoạch không
                if (checkOut.Value.DateTime > segPlannedEnd)
                {
                    double extraMinutes = (checkOut.Value.DateTime - segPlannedEnd).TotalMinutes;
                    if (extraMinutes >= 30) // Tối thiểu 30 phút dôi dư
                    {
                        decimal extraHours = Math.Round((decimal)(extraMinutes / 60.0), 2, MidpointRounding.AwayFromZero);
                        if (approvedOt != null)
                        {
                            // Được duyệt làm thêm giờ
                            approvedOtHours += Math.Min(extraHours, approvedOt.PlannedHours);
                        }
                        else
                        {
                            // Không có phiếu duyệt -> Đánh dấu cảnh báo, KHÔNG trả tiền tăng ca
                            unapprovedOtHours += extraHours;
                            hasWarning = true;
                            warningReason = $"Ở lại {extraHours:F1} giờ sau ca làm việc không có phiếu duyệt làm thêm giờ (OvertimeRequest).";
                        }
                    }
                }
            }

            totalWorkHours += segWorkHours;
            totalLateMinutes += segLate;
            totalEarlyLeaveMinutes += segEarly;
            totalNightHours += segNightHours;

            dailyAtt.AddSegment(
                seg.SegmentIndex,
                checkIn,
                checkOut,
                segLate,
                segEarly,
                segWorkHours,
                segNightHours);
        }

        // 8. Xác định trạng thái và mã công
        string attendanceCode;
        decimal workDayCredit;
        AttendanceStatus status;

        decimal plannedHours = shift.TotalPlannedWorkHours;
        if (totalWorkHours >= plannedHours * 0.95m || totalWorkHours >= 8.0m)
        {
            attendanceCode = "X";
            workDayCredit = 1.0m;
            status = AttendanceStatus.Present;
        }
        else if (totalWorkHours >= plannedHours * 0.45m || totalWorkHours >= 4.0m)
        {
            attendanceCode = "X/2";
            workDayCredit = 0.5m;
            status = (totalLateMinutes > 0) ? AttendanceStatus.Late : AttendanceStatus.EarlyDeparture;
        }
        else
        {
            // Kiểm tra có đơn xin nghỉ phép hợp lệ không
            var leave = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.EmployeeId == employeeId &&
                                          l.StartDate <= workDate.ToDateTime(TimeOnly.MinValue) &&
                                          l.EndDate >= workDate.ToDateTime(TimeOnly.MaxValue) &&
                                          l.Status == LeaveStatus.Approved, cancellationToken);

            if (leave != null)
            {
                switch (leave.LeaveType)
                {
                    case LeaveType.AnnualLeave:
                        attendanceCode = "P";
                        workDayCredit = 1.0m;
                        status = AttendanceStatus.OnLeave;
                        break;
                    case LeaveType.SickLeave:
                        attendanceCode = "Ô";
                        workDayCredit = 1.0m;
                        status = AttendanceStatus.OnLeave;
                        break;
                    case LeaveType.MaternityLeave:
                        attendanceCode = "TS";
                        workDayCredit = 1.0m;
                        status = AttendanceStatus.OnLeave;
                        break;
                    case LeaveType.CompassionateLeave:
                        attendanceCode = "NB";
                        workDayCredit = 1.0m;
                        status = AttendanceStatus.OnLeave;
                        break;
                    case LeaveType.UnpaidLeave:
                        attendanceCode = "KL";
                        workDayCredit = 0m;
                        status = AttendanceStatus.OnLeave;
                        break;
                    default:
                        attendanceCode = "P";
                        workDayCredit = 1.0m;
                        status = AttendanceStatus.OnLeave;
                        break;
                }
            }
            else
            {
                attendanceCode = "KP"; // Vắng không phép
                workDayCredit = 0m;
                status = AttendanceStatus.AbsentUnexcused;
            }
        }

        // Phân loại giờ tăng ca theo loại ngày
        decimal regularOtHours = 0m;
        decimal restDayOtHours = 0m;
        decimal holidayOtHours = 0m;
        decimal nightOtHours = 0m;

        if (approvedOtHours > 0)
        {
            if (isHoliday)
            {
                holidayOtHours = approvedOtHours;
            }
            else if (rosterEntry.IsOff || workDate.DayOfWeek == DayOfWeek.Sunday)
            {
                restDayOtHours = approvedOtHours;
            }
            else
            {
                regularOtHours = approvedOtHours;
            }
        }

        dailyAtt.SetSummary(
            totalWorkHours,
            totalLateMinutes,
            totalEarlyLeaveMinutes,
            totalNightHours,
            regularOtHours,
            restDayOtHours,
            holidayOtHours,
            nightOtHours,
            unapprovedOtHours,
            attendanceCode,
            workDayCredit,
            isComplete: (totalWorkHours > 0 || attendanceCode != "KP"),
            status,
            hasWarning,
            warningReason,
            remarks: null);

        await _context.SaveChangesAsync(cancellationToken);
        return dailyAtt;
    }

    public async Task<int> CalculateDailyAttendanceForUnitAsync(
        int organizationalUnitId,
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
        var rosterEntries = await _context.RosterEntries
            .Include(r => r.Employee)
            .Where(r => r.Employee!.OrganizationalUnitId == organizationalUnitId && r.WorkDate == workDate)
            .ToListAsync(cancellationToken);

        int processed = 0;
        foreach (var entry in rosterEntries)
        {
            await CalculateDailyAttendanceAsync(entry.EmployeeId, workDate, cancellationToken);
            processed++;
        }

        return processed;
    }

    public async Task<MonthlyTimesheetSummaryDto> CalculateMonthlySummaryForEmployeeAsync(
        int employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        string period = $"{year:D4}-{month:D2}";
        var emp = await _context.Employees
            .Include(e => e.OrganizationalUnit)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={employeeId}.", "ERR_EMP_NOT_FOUND");

        int daysInMonth = DateTime.DaysInMonth(year, month);
        var startDate = new DateOnly(year, month, 1);
        var endDate = new DateOnly(year, month, daysInMonth);

        // 1. Tính công chuẩn tháng theo WorkPattern của đơn vị
        var workPattern = emp.OrganizationalUnit?.WorkPattern ?? WorkPattern.OfficeWeek;
        decimal standardWorkDays = 26m;

        if (workPattern == WorkPattern.OfficeWeek)
        {
            // Calendar: Ngày từ T2-T6 trừ ngày lễ
            int workDays = 0;
            var holidays = await _context.PublicHolidays
                .Where(h => h.Date >= startDate && h.Date <= endDate)
                .Select(h => h.Date)
                .ToListAsync(cancellationToken);

            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(year, month, d);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday && !holidays.Contains(date))
                {
                    workDays++;
                }
            }
            standardWorkDays = workDays;
        }
        else if (workPattern == WorkPattern.ThreeShiftRotation)
        {
            // Roster: Số ca được xếp không phải OFF
            int scheduledShifts = await _context.RosterEntries
                .CountAsync(r => r.EmployeeId == employeeId && r.WorkDate >= startDate && r.WorkDate <= endDate && !r.IsOff, cancellationToken);
            standardWorkDays = scheduledShifts > 0 ? scheduledShifts : 24m;
        }
        else
        {
            // Fixed26: Cố định 26 công
            standardWorkDays = 26m;
        }

        // 2. Lấy toàn bộ DailyAttendance của nhân viên trong tháng
        var dailyRecords = await _context.DailyAttendances
            .Where(d => d.EmployeeId == employeeId && d.AttendanceDate >= startDate && d.AttendanceDate <= endDate)
            .OrderBy(d => d.AttendanceDate)
            .ToListAsync(cancellationToken);

        // Lấy danh sách RosterEntries trong tháng để đếm vắng không phép liên tục theo ca được xếp
        var rosterEntries = await _context.RosterEntries
            .Where(r => r.EmployeeId == employeeId && r.WorkDate >= startDate && r.WorkDate <= endDate)
            .OrderBy(r => r.WorkDate)
            .ToListAsync(cancellationToken);

        var dailyMap = dailyRecords.ToDictionary(d => d.AttendanceDate);

        decimal actualWorkDays = 0m;
        decimal paidLeaveDays = 0m;
        decimal sickLeaveDays = 0m;
        decimal maternityLeaveDays = 0m;
        decimal holidayDays = 0m;
        decimal compensatoryLeaveDays = 0m;
        decimal businessTripDays = 0m;
        decimal trainingDays = 0m;
        decimal unpaidLeaveDays = 0m;
        decimal unexcusedAbsenceDays = 0m;

        decimal totalWorkHours = 0m;
        decimal nightHours = 0m;
        decimal regularOt = 0m;
        decimal restDayOt = 0m;
        decimal holidayOt = 0m;
        decimal nightOt = 0m;
        int lateTimes = 0;
        int earlyTimes = 0;

        int currentConsecutiveUnexcused = 0;
        int maxConsecutiveUnexcused = 0;

        foreach (var roster in rosterEntries)
        {
            if (roster.IsOff)
            {
                // Ngày nghỉ OFF không tính ngắt chuỗi và không tính vắng
                continue;
            }

            if (dailyMap.TryGetValue(roster.WorkDate, out var rec))
            {
                totalWorkHours += rec.TotalWorkHours;
                nightHours += rec.NightHours;
                regularOt += rec.RegularOtHours;
                restDayOt += rec.RestDayOtHours;
                holidayOt += rec.HolidayOtHours;
                nightOt += rec.NightOtHours;
                if (rec.LateMinutes > 0) lateTimes++;
                if (rec.EarlyLeaveMinutes > 0) earlyTimes++;

                switch (rec.AttendanceCode)
                {
                    case "X":
                        actualWorkDays += 1.0m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "X/2":
                        actualWorkDays += 0.5m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "P":
                        paidLeaveDays += 1.0m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "Ô":
                        sickLeaveDays += 1.0m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "TS":
                        maternityLeaveDays += 1.0m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "L":
                        holidayDays += 1.0m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "NB":
                        compensatoryLeaveDays += 1.0m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "CT":
                        businessTripDays += 1.0m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "ĐT":
                        trainingDays += 1.0m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "KL":
                        unpaidLeaveDays += 1.0m;
                        currentConsecutiveUnexcused = 0;
                        break;
                    case "KP":
                    default:
                        unexcusedAbsenceDays += 1.0m;
                        currentConsecutiveUnexcused++;
                        if (currentConsecutiveUnexcused > maxConsecutiveUnexcused)
                        {
                            maxConsecutiveUnexcused = currentConsecutiveUnexcused;
                        }
                        break;
                }
            }
            else
            {
                // Có lịch làm mà không có dữ liệu quẹt thẻ -> xem là vắng không phép
                unexcusedAbsenceDays += 1.0m;
                currentConsecutiveUnexcused++;
                if (currentConsecutiveUnexcused > maxConsecutiveUnexcused)
                {
                    maxConsecutiveUnexcused = currentConsecutiveUnexcused;
                }
            }
        }

        // Lưu hoặc cập nhật MonthlyTimesheetSummary
        var summary = await _context.MonthlyTimesheetSummaries
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Period == period, cancellationToken);

        if (summary == null)
        {
            summary = new MonthlyTimesheetSummary(
                period,
                employeeId,
                emp.OrganizationalUnitId,
                standardWorkDays);
            _context.MonthlyTimesheetSummaries.Add(summary);
        }

        summary.UpdateMetrics(
            actualWorkDays,
            paidLeaveDays,
            sickLeaveDays,
            maternityLeaveDays,
            holidayDays,
            compensatoryLeaveDays,
            businessTripDays,
            trainingDays,
            unpaidLeaveDays,
            unexcusedAbsenceDays,
            maxConsecutiveUnexcused,
            totalWorkHours,
            nightHours,
            regularOt,
            restDayOt,
            holidayOt,
            nightOt,
            lateTimes,
            earlyTimes);

        await _context.SaveChangesAsync(cancellationToken);

        return new MonthlyTimesheetSummaryDto(
            summary.Id,
            summary.Period,
            summary.EmployeeId,
            emp.EmployeeCode,
            emp.FullName,
            emp.OrganizationalUnitId,
            emp.OrganizationalUnit?.UnitName ?? string.Empty,
            summary.StandardWorkDays,
            summary.ActualWorkDays,
            summary.PaidLeaveDays,
            summary.SickLeaveDays,
            summary.MaternityLeaveDays,
            summary.HolidayDays,
            summary.CompensatoryLeaveDays,
            summary.BusinessTripDays,
            summary.TrainingDays,
            summary.UnpaidLeaveDays,
            summary.UnexcusedAbsenceDays,
            summary.ConsecutiveUnexcusedDays,
            summary.TotalWorkHours,
            summary.NightHours,
            summary.TotalOtHours,
            summary.RegularOtHours,
            summary.RestDayOtHours,
            summary.HolidayOtHours,
            summary.NightOtHours,
            summary.LateTimes,
            summary.EarlyLeaveTimes);
    }

    public async Task<int> CalculateMonthlySummaryForUnitAsync(
        int organizationalUnitId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .Where(e => e.OrganizationalUnitId == organizationalUnitId && e.IsActive)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var emp in employees)
        {
            await CalculateMonthlySummaryForEmployeeAsync(emp.Id, year, month, cancellationToken);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Tính số giờ làm việc nằm trong khung giờ đêm từ 22:00 đến 06:00 sáng hôm sau.
    /// </summary>
    private static decimal CalculateNightHours(DateTime start, DateTime end)
    {
        if (end <= start) return 0m;

        double totalNightMinutes = 0;
        DateTime current = start;

        while (current < end)
        {
            DateTime nextHour = current.Date.AddHours(current.Hour + 1);
            if (nextHour > end) nextHour = end;

            // Khung giờ đêm: 22h, 23h, 00h, 01h, 02h, 03h, 04h, 05h
            if (current.Hour >= 22 || current.Hour < 6)
            {
                totalNightMinutes += (nextHour - current).TotalMinutes;
            }

            current = nextHour;
        }

        return Math.Round((decimal)(totalNightMinutes / 60.0), 2, MidpointRounding.AwayFromZero);
    }
}
