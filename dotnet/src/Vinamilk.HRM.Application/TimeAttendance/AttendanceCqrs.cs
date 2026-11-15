using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.TimeAttendance;

// ============================================================================
// 1. DTOs & GEOFENCING CALCULATOR CHO CHẤM CÔNG, OT & GIẢI TRÌNH CÔNG
// ============================================================================

public record GeofenceLocationSpec(
    string Code,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    double AllowedRadiusMeters,
    string OutsideGeofenceAction = "PendingApproval",
    int QrRotationSeconds = 30);

public static class VinamilkGeofenceCalculator
{
    public static readonly IReadOnlyList<GeofenceLocationSpec> StandardLocations = new List<GeofenceLocationSpec>
    {
        new("HQ_SG", "Trụ sở chính Vinamilk Tower (Q.7, TP.HCM)", "Số 10 Tân Trào, Phường Tân Phú, Quận 7, TP.HCM", 10.7296, 106.7218, 300, "PendingApproval", 30),
        new("NM_BD", "Nhà máy Sữa Việt Nam - Mega Factory (Bình Dương)", "KCN Mỹ Phước 2, Bến Cát, Bình Dương", 11.1154, 106.6122, 500, "Reject", 30),
        new("NM_TS", "Nhà máy Sữa Tiên Sơn (Bắc Ninh)", "KCN Tiên Sơn, Tiên Du, Bắc Ninh", 21.1189, 105.9864, 500, "Reject", 30),
        new("CN_HN", "Chi nhánh Vinamilk Hà Nội (Cầu Giấy)", "Tháp Thành Công, Dịch Vọng Hậu, Cầu Giấy, Hà Nội", 21.0285, 105.7892, 300, "PendingApproval", 30),
        new("TT_TN", "Trang trại Bò sữa Tây Ninh (Resort Bò sữa)", "Xã Long Khánh, Huyện Bến Cầu, Tây Ninh", 11.1245, 106.1520, 800, "PendingApproval", 30)
    };

    public static double CalculateHaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMeters = 6371000.0;
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(EarthRadiusMeters * c, 1);
    }

    public static (GeofenceLocationSpec Nearest, double DistanceMeters, bool IsWithinGeofence) EvaluateGps(
        double lat,
        double lon,
        IReadOnlyList<GeofenceLocationSpec>? locations = null)
    {
        var source = locations is { Count: > 0 } ? locations : StandardLocations;
        var best = source
            .Select(loc => (Loc: loc, Dist: CalculateHaversineMeters(lat, lon, loc.Latitude, loc.Longitude)))
            .OrderBy(x => x.Dist)
            .First();

        return (best.Loc, best.Dist, best.Dist <= best.Loc.AllowedRadiusMeters);
    }

    public static string GenerateDynamicQrToken(DateTime utcNow, int rotationSeconds = 30)
    {
        // Mã QR động thay đổi mỗi 30–60 giây (mặc định 30 giây)
        int effectiveSeconds = Math.Clamp(rotationSeconds, 30, 60);
        var windowSlot = utcNow.Ticks / TimeSpan.FromSeconds(effectiveSeconds).Ticks;
        var raw = $"VNM_QR_CHECKIN::{effectiveSeconds}::{windowSlot}::VinamilkEnterprise2026";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return "VNM-QR-" + Convert.ToHexString(hash)[..10];
    }

    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);
}

public record DailyCellSummaryDto(
    long? DailyId,
    int Day,
    DateOnly Date,
    string Symbol,
    string? CheckInText,
    string? CheckOutText,
    int LateMinutes,
    int EarlyLeaveMinutes,
    decimal OvertimeHours,
    bool IsManualEdited,
    string? EditReason,
    string CssClass);

public record MonthlyAttendanceEmployeeRowDto(
    int SummaryId,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    string PositionName,
    decimal StandardWorkdays,
    decimal ActualWorkdays,
    decimal PaidLeaveDays,
    decimal HolidayDays,
    decimal UnpaidLeaveDays,
    int TotalLateCount,
    int TotalLateMinutes,
    decimal TotalOtHours150,
    decimal TotalOtHours200,
    decimal TotalOtHours300,
    decimal TotalNightHours,
    decimal TotalPayableWorkdays,
    bool IsLocked,
    IReadOnlyList<DailyCellSummaryDto> Days,
    int TotalEarlyLeaveCount = 0,
    int TotalEarlyLeaveMinutes = 0,
    int TotalMissedPunchCount = 0,
    decimal TotalPaidPersonalLeaveDays = 0m,
    decimal TotalUnpaidPersonalLeaveDays = 0m,
    decimal TotalUnexcusedAbsentDays = 0m,
    decimal TotalSocialInsuranceLeaveDays = 0m,
    decimal TotalOtHours150Day = 0m,
    decimal TotalOtHours150Night = 0m,
    decimal TotalOtHours200Day = 0m,
    decimal TotalOtHours200Night = 0m,
    decimal TotalOtHours300Day = 0m,
    decimal TotalOtHours300Night = 0m,
    string ClosingStatus = "Open");

public record OvertimeRequestItemDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    DateOnly OvertimeDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal RequestedHours,
    decimal ApprovedHours,
    string OvertimeRateType,
    string? Reason,
    string Status,
    int? WorkflowInstanceId,
    decimal EmployeeMonthlyOtTotal,
    decimal EmployeeYearlyOtTotal);

public record AttendanceLogItemDto(
    long Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    DateTime PunchTime,
    string PunchType,
    string SourceType,
    string? DeviceId,
    decimal? Latitude,
    decimal? Longitude,
    string? LocationAddress,
    string? IpAddress = null,
    string? PhotoUrl = null,
    string? EnteredByUsername = null,
    string? ManualReason = null,
    string? GeofenceStatus = null);

public record AttendanceDeviceDto(
    int Id,
    string DeviceCode,
    string DeviceName,
    string ConnectorType,
    string? LocationName,
    int SyncIntervalMinutes,
    bool IsActive,
    DateTime? LastSyncAt,
    string? LastSyncSummary);

public record EmployeeDeviceMappingDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string? DeviceCode,
    string DeviceUserCode,
    string? Notes);

public record UnmappedAttendanceLogDto(
    long Id,
    string DeviceCode,
    string DeviceUserCode,
    DateTime PunchTime,
    string PunchType,
    string? RawPayload,
    string Status);

public record AttendanceExplanationItemDto(
    long DailyAttendanceId,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    DateOnly AttendanceDate,
    string CurrentSymbol,
    int LateMinutes,
    int EarlyLeaveMinutes,
    string? EditReason,
    bool IsApproved,
    int? WorkflowInstanceId,
    string WorkflowStatus);

public record MonthlyAttendanceBoardDto(
    int Month,
    int Year,
    int DaysInMonth,
    decimal StandardWorkdaysInMonth,
    bool IsMonthLocked,
    string? LockedBy,
    DateTime? LockedAt,
    int? SelectedUnitId,
    string? SearchKeyword,
    string CurrentDynamicQrToken,
    IReadOnlyList<GeofenceLocationSpec> GeofenceLocations,
    IReadOnlyList<SimpleLookupDto> Units,
    IReadOnlyList<SimpleLookupDto> ActiveEmployees,
    IReadOnlyList<MonthlyAttendanceEmployeeRowDto> MatrixRows,
    IReadOnlyList<OvertimeRequestItemDto> OvertimeRequests,
    IReadOnlyList<AttendanceExplanationItemDto> Explanations,
    IReadOnlyList<AttendanceLogItemDto> RecentLogs,
    int TotalPresentToday,
    int TotalLateCasesInMonth,
    decimal TotalOtHoursInMonth,
    int TotalBusinessTripDays,
    IReadOnlyList<AttendanceDeviceDto>? Devices = null,
    IReadOnlyList<EmployeeDeviceMappingDto>? DeviceMappings = null,
    IReadOnlyList<UnmappedAttendanceLogDto>? UnmappedLogs = null,
    string ClosingStatus = "Open",
    string? UnitSubmittedBy = null,
    DateTime? UnitSubmittedAt = null,
    string? ManagerConfirmedBy = null,
    DateTime? ManagerConfirmedAt = null,
    DateTime? ReviewDeadlineAt = null,
    IReadOnlyList<AttendanceSymbolDefinitionDto>? Symbols = null,
    IReadOnlyList<PostLockAdjustmentDto>? PostLockAdjustments = null,
    IReadOnlyList<AttendanceRecalculationBatchDto>? RecalculationBatches = null);

// ============================================================================
// 2. AUTO-SEED DỮ LIỆU CHẤM CÔNG THÁNG & TỔNG HỢP BẢNG CÔNG
// ============================================================================

public static class AttendanceBoardSeeder
{
    public static decimal CalculateStandardWorkdays(int year, int month)
    {
        int days = DateTime.DaysInMonth(year, month);
        decimal count = 0m;
        for (int d = 1; d <= days; d++)
        {
            var dow = new DateOnly(year, month, d).DayOfWeek;
            if (dow != DayOfWeek.Sunday)
                count += 1m;
        }
        return count;
    }

    public static async Task EnsureMonthAttendanceSeededAsync(
        IApplicationDbContext context,
        int month,
        int year,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        if (!await context.DailyAttendances.AnyAsync(d => d.AttendanceDate >= monthStart && d.AttendanceDate <= monthEnd, cancellationToken))
        {
            var employees = await context.Employees
                .AsNoTracking()
                .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation")
                .OrderBy(e => e.EmployeeCode)
                .Take(25)
                .ToListAsync(cancellationToken);

            var hcShift = await context.WorkShifts.AsNoTracking().FirstOrDefaultAsync(s => s.ShiftCode == "HC", cancellationToken);
            var holidays = await context.Holidays
                .AsNoTracking()
                .Where(h => h.HolidayDate >= monthStart && h.HolidayDate <= monthEnd)
                .ToDictionaryAsync(h => h.HolidayDate, cancellationToken);

            var dailyList = new List<DailyAttendance>();
            var logsList = new List<AttendanceLog>();
            int empIdx = 0;

            foreach (var emp in employees)
            {
                for (var dt = monthStart; dt <= monthEnd; dt = dt.AddDays(1))
                {
                    if (dt.DayOfWeek == DayOfWeek.Sunday)
                    {
                        dailyList.Add(new DailyAttendance
                        {
                            EmployeeId = emp.Id,
                            AttendanceDate = dt,
                            WorkShiftId = hcShift?.Id,
                            AttendanceSymbol = "OFF",
                            WorkingHours = 0m,
                            WorkdayCount = 0m
                        });
                        continue;
                    }

                    if (holidays.TryGetValue(dt, out var hol))
                    {
                        dailyList.Add(new DailyAttendance
                        {
                            EmployeeId = emp.Id,
                            AttendanceDate = dt,
                            WorkShiftId = hcShift?.Id,
                            AttendanceSymbol = "L",
                            WorkingHours = 8m,
                            WorkdayCount = 1m,
                            EditReason = hol.HolidayName
                        });
                        continue;
                    }

                    int dayNum = dt.Day;
                    string symbol = "X";
                    int lateMins = 0;
                    int earlyMins = 0;
                    decimal ot150 = 0m;
                    decimal workdays = 1.0m;
                    string? reason = null;

                    if ((empIdx + dayNum) % 29 == 0)
                    {
                        symbol = "CT";
                        reason = "Đi công tác làm việc với nhà phân phối khu vực";
                    }
                    else if ((empIdx + dayNum) % 23 == 0)
                    {
                        symbol = "P";
                        reason = "Nghỉ phép năm có kế hoạch";
                    }
                    else if ((empIdx + dayNum) % 19 == 0)
                    {
                        lateMins = 18;
                        reason = "Kẹt xe đường Nguyễn Văn Linh (Chờ giải trình)";
                    }
                    else if ((empIdx + dayNum) % 17 == 0)
                    {
                        ot150 = 2.0m;
                    }

                    var checkIn = dt.ToDateTime(new TimeOnly(8, 0).AddMinutes(lateMins));
                    var checkOut = dt.ToDateTime(new TimeOnly(17, 0).AddHours((double)ot150));

                    dailyList.Add(new DailyAttendance
                    {
                        EmployeeId = emp.Id,
                        AttendanceDate = dt,
                        WorkShiftId = hcShift?.Id,
                        CheckInTime = symbol == "X" ? checkIn : null,
                        CheckOutTime = symbol == "X" ? checkOut : null,
                        AttendanceSymbol = symbol,
                        WorkingHours = symbol == "X" ? 8.0m : 0m,
                        WorkdayCount = workdays,
                        LateMinutes = lateMins,
                        EarlyLeaveMinutes = earlyMins,
                        OvertimeHours150 = ot150,
                        EditReason = reason
                    });

                    if (dt == monthStart.AddDays(Math.Min(14, DateTime.DaysInMonth(year, month) - 1)) && empIdx < 8)
                    {
                        var loc = VinamilkGeofenceCalculator.StandardLocations[empIdx % VinamilkGeofenceCalculator.StandardLocations.Count];
                        logsList.Add(new AttendanceLog
                        {
                            EmployeeId = emp.Id,
                            PunchTime = checkIn,
                            PunchType = "CheckIn",
                            SourceType = empIdx % 2 == 0 ? "MobileGps" : "BiometricDevice",
                            DeviceId = empIdx % 2 == 0 ? "WEB-GPS-PORTAL" : "ZKTECO-F22-GATE1",
                            Latitude = (decimal)loc.Latitude,
                            Longitude = (decimal)loc.Longitude,
                            LocationAddress = loc.Name
                        });
                    }
                }
                empIdx++;
            }

            await context.DailyAttendances.AddRangeAsync(dailyList, cancellationToken);
            if (logsList.Count > 0)
                await context.AttendanceLogs.AddRangeAsync(logsList, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            // Tổng hợp lên MonthlyAttendanceSummaries
            await RecalculateSummariesInternalAsync(context, month, year, cancellationToken);
        }

        // Seed một số đơn OT mẫu nếu chưa có
        if (!await context.OvertimeRequests.AnyAsync(cancellationToken))
        {
            var sampleEmps = await context.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).Take(4).ToListAsync(cancellationToken);
            if (sampleEmps.Count >= 2)
            {
                await context.OvertimeRequests.AddRangeAsync(new List<OvertimeRequest>
                {
                    new()
                    {
                        EmployeeId = sampleEmps[0].Id,
                        OvertimeDate = new DateOnly(year, month, Math.Min(10, DateTime.DaysInMonth(year, month))),
                        StartTime = new TimeOnly(17, 30),
                        EndTime = new TimeOnly(20, 30),
                        RequestedHours = 3.0m,
                        ApprovedHours = 3.0m,
                        OvertimeRateType = "OT150",
                        Reason = "Vận hành dây chuyền tiệt trùng UHT ca cao điểm",
                        Status = "Approved"
                    },
                    new()
                    {
                        EmployeeId = sampleEmps[1].Id,
                        OvertimeDate = new DateOnly(year, month, Math.Min(15, DateTime.DaysInMonth(year, month))),
                        StartTime = new TimeOnly(8, 0),
                        EndTime = new TimeOnly(16, 0),
                        RequestedHours = 8.0m,
                        ApprovedHours = 0m,
                        OvertimeRateType = "OT200",
                        Reason = "Kiểm kê kho nguyên liệu sữa bột định kỳ cuối tuần",
                        Status = "Pending"
                    }
                }, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public static async Task RecalculateSummariesInternalAsync(
        IApplicationDbContext context,
        int month,
        int year,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var standardDays = CalculateStandardWorkdays(year, month);

        var dailies = await context.DailyAttendances
            .AsNoTracking()
            .Where(d => d.AttendanceDate >= monthStart && d.AttendanceDate <= monthEnd)
            .ToListAsync(cancellationToken);

        var existingSummaries = await context.MonthlyAttendanceSummaries
            .Where(m => m.Month == month && m.Year == year)
            .ToDictionaryAsync(m => m.EmployeeId, cancellationToken);

        foreach (var group in dailies.GroupBy(d => d.EmployeeId))
        {
            var empId = group.Key;
            if (existingSummaries.TryGetValue(empId, out var existing) && existing.IsLocked)
                continue;

            decimal actualDays = 0m;
            decimal paidLeave = 0m;
            decimal paidPersonal = 0m;
            decimal holidayDays = 0m;
            decimal siLeave = 0m;
            decimal unpaidPersonal = 0m;
            decimal unexcusedAbsent = 0m;

            foreach (var d in group)
            {
                var sym = d.AttendanceSymbol.Trim();
                if (sym.Equals("L", StringComparison.OrdinalIgnoreCase))
                {
                    holidayDays += d.WorkdayCount > 0m ? d.WorkdayCount : 1.0m;
                }
                else if (sym.Equals("P", StringComparison.OrdinalIgnoreCase) || sym.Equals("P/2", StringComparison.OrdinalIgnoreCase))
                {
                    paidLeave += d.PaidLeaveDays > 0m ? d.PaidLeaveDays : d.WorkdayCount;
                }
                else if (sym.Equals("R", StringComparison.OrdinalIgnoreCase))
                {
                    paidPersonal += d.PaidLeaveDays > 0m ? d.PaidLeaveDays : (d.WorkdayCount > 0m ? d.WorkdayCount : 1.0m);
                }
                else if (sym.Equals("Ô", StringComparison.OrdinalIgnoreCase) || sym.Equals("Om", StringComparison.OrdinalIgnoreCase) || sym.Equals("TS", StringComparison.OrdinalIgnoreCase))
                {
                    siLeave += 1.0m;
                }
                else if (sym.Equals("Ro", StringComparison.OrdinalIgnoreCase))
                {
                    unpaidPersonal += 1.0m;
                }
                else if (sym.Equals("KL", StringComparison.OrdinalIgnoreCase) || sym.Equals("V", StringComparison.OrdinalIgnoreCase))
                {
                    unexcusedAbsent += 1.0m;
                }
                else
                {
                    actualDays += d.WorkdayCount;
                    paidLeave += d.PaidLeaveDays;
                    if (sym.Contains("+Ro", StringComparison.OrdinalIgnoreCase)) unpaidPersonal += 0.5m;
                    else if (sym.Contains("+R", StringComparison.OrdinalIgnoreCase)) paidPersonal += 0.5m;
                    else if (sym.Contains("+KL", StringComparison.OrdinalIgnoreCase)) unexcusedAbsent += 0.5m;
                    else if (d.UnpaidLeaveDays > 0m) unpaidPersonal += d.UnpaidLeaveDays;
                }
            }

            decimal unpaidLeave = unpaidPersonal + unexcusedAbsent;
            int lateCount = group.Count(x => x.LateMinutes > 0);
            int lateMinutes = group.Sum(x => x.LateMinutes);
            int earlyCount = group.Count(x => x.EarlyLeaveMinutes > 0);
            int earlyMinutes = group.Sum(x => x.EarlyLeaveMinutes);
            int totalMissedCount = group.Count(x => x.IsMissedPunch);
            decimal ot150D = group.Sum(x => x.OvertimeHours150Day);
            decimal ot150N = group.Sum(x => x.OvertimeHours150Night);
            decimal ot200D = group.Sum(x => x.OvertimeHours200Day);
            decimal ot200N = group.Sum(x => x.OvertimeHours200Night);
            decimal ot300D = group.Sum(x => x.OvertimeHours300Day);
            decimal ot300N = group.Sum(x => x.OvertimeHours300Night);
            decimal ot150 = group.Sum(x => x.OvertimeHours150);
            decimal ot200 = group.Sum(x => x.OvertimeHours200);
            decimal ot300 = group.Sum(x => x.OvertimeHours300);
            decimal nightHours = group.Sum(x => x.NightShiftHours);
            decimal payable = actualDays + paidLeave + paidPersonal + holidayDays;

            var resolvedPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(context, empId, monthStart, cancellationToken);
            int missedPunchCount = group.Count(x => !string.IsNullOrEmpty(x.EditReason) && x.EditReason.Contains("Đề xuất giờ:", StringComparison.OrdinalIgnoreCase));
            int overQuotaCount = Math.Max(0, missedPunchCount - resolvedPolicy.Policy.MaxMissedPunchExplanationsPerMonth);
            decimal missedPunchPenalty = string.Equals(resolvedPolicy.Policy.MissedPunchOverQuotaAction, "FixedFine", StringComparison.OrdinalIgnoreCase)
                ? decimal.Round(overQuotaCount * resolvedPolicy.Policy.MissedPunchPenaltyAmount, 0, MidpointRounding.AwayFromZero)
                : 0m;

            if (existing != null)
            {
                existing.StandardWorkdays = standardDays;
                existing.ActualWorkdays = actualDays;
                existing.PaidLeaveDays = paidLeave;
                existing.HolidayDays = holidayDays;
                existing.SocialInsuranceLeaveDays = siLeave;
                existing.UnpaidLeaveDays = unpaidLeave;
                existing.TotalPaidPersonalLeaveDays = paidPersonal;
                existing.TotalUnpaidPersonalLeaveDays = unpaidPersonal;
                existing.TotalUnexcusedAbsentDays = unexcusedAbsent;
                existing.TotalLateCount = lateCount;
                existing.TotalLateMinutes = lateMinutes;
                existing.TotalEarlyLeaveCount = earlyCount;
                existing.TotalEarlyLeaveMinutes = earlyMinutes;
                existing.TotalMissedPunchCount = totalMissedCount;
                existing.TotalOtHours150 = ot150;
                existing.TotalOtHours200 = ot200;
                existing.TotalOtHours300 = ot300;
                existing.TotalOtHours150Day = ot150D;
                existing.TotalOtHours150Night = ot150N;
                existing.TotalOtHours200Day = ot200D;
                existing.TotalOtHours200Night = ot200N;
                existing.TotalOtHours300Day = ot300D;
                existing.TotalOtHours300Night = ot300N;
                existing.TotalNightHours = nightHours;
                existing.TotalPayableWorkdays = payable;
                existing.MissedPunchOverQuotaCount = overQuotaCount;
                existing.MissedPunchPenaltyAmount = missedPunchPenalty;
            }
            else
            {
                await context.MonthlyAttendanceSummaries.AddAsync(new MonthlyAttendanceSummary
                {
                    EmployeeId = empId,
                    Month = month,
                    Year = year,
                    StandardWorkdays = standardDays,
                    ActualWorkdays = actualDays,
                    PaidLeaveDays = paidLeave,
                    HolidayDays = holidayDays,
                    SocialInsuranceLeaveDays = siLeave,
                    UnpaidLeaveDays = unpaidLeave,
                    TotalPaidPersonalLeaveDays = paidPersonal,
                    TotalUnpaidPersonalLeaveDays = unpaidPersonal,
                    TotalUnexcusedAbsentDays = unexcusedAbsent,
                    TotalLateCount = lateCount,
                    TotalLateMinutes = lateMinutes,
                    TotalEarlyLeaveCount = earlyCount,
                    TotalEarlyLeaveMinutes = earlyMinutes,
                    TotalMissedPunchCount = totalMissedCount,
                    TotalOtHours150 = ot150,
                    TotalOtHours200 = ot200,
                    TotalOtHours300 = ot300,
                    TotalOtHours150Day = ot150D,
                    TotalOtHours150Night = ot150N,
                    TotalOtHours200Day = ot200D,
                    TotalOtHours200Night = ot200N,
                    TotalOtHours300Day = ot300D,
                    TotalOtHours300Night = ot300N,
                    TotalNightHours = nightHours,
                    TotalPayableWorkdays = payable,
                    MissedPunchOverQuotaCount = overQuotaCount,
                    MissedPunchPenaltyAmount = missedPunchPenalty,
                    IsLocked = false,
                    ClosingStatus = "Open"
                }, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public static string GetSymbolCssClass(string symbol, int lateMinutes, bool isManualEdited = false)
    {
        if (isManualEdited)
            return "bg-warning-subtle text-dark fw-bold border border-2 border-warning vnm-manual-edited-cell";
        if (lateMinutes > 0) return "bg-danger-subtle text-danger fw-bold border-danger-subtle";
        if (symbol.Contains('+'))
            return "bg-info-subtle text-primary fw-bold";

        return symbol switch
        {
            "X" => "bg-success-subtle text-success fw-semibold",
            "½" or "X/2" => "bg-warning-subtle text-warning-emphasis fw-semibold",
            "CT" => "bg-info-subtle text-info-emphasis fw-bold",
            "R" => "bg-info-subtle text-dark fw-semibold",
            "CGD" => "bg-warning text-dark fw-bold",
            "TC" => "bg-danger-subtle text-danger fw-bold border border-danger",
            "P" or "P/2" => "bg-primary-subtle text-primary fw-bold",
            "L" => "bg-danger text-white fw-bold",
            "Ô" or "Om" or "TS" => "bg-warning text-dark fw-semibold",
            "Ro" => "bg-secondary-subtle text-secondary fw-semibold",
            "KL" or "V" => "bg-dark-subtle text-dark fw-bold",
            "OFF" => "bg-light text-muted",
            _ => "bg-light text-secondary"
        };
    }
}

// ============================================================================
// 3. QUERY: LẤY BẢNG CÔNG THÁNG, CHẤM CÔNG GPS/QR, OT & GIẢI TRÌNH CÔNG
// ============================================================================

public record GetMonthlyAttendanceBoardQuery(
    int? Month = null,
    int? Year = null,
    int? UnitId = null,
    string? SearchKeyword = null) : IRequest<MonthlyAttendanceBoardDto>;

public class GetMonthlyAttendanceBoardQueryHandler : IRequestHandler<GetMonthlyAttendanceBoardQuery, MonthlyAttendanceBoardDto>
{
    private readonly IApplicationDbContext _context;

    public GetMonthlyAttendanceBoardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MonthlyAttendanceBoardDto> Handle(GetMonthlyAttendanceBoardQuery request, CancellationToken cancellationToken)
    {
        int month = request.Month ?? DateTime.Today.Month;
        int year = request.Year ?? DateTime.Today.Year;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, daysInMonth);

        await AttendanceBoardSeeder.EnsureMonthAttendanceSeededAsync(_context, month, year, cancellationToken);

        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .OrderBy(u => u.SortOrder)
            .Select(u => new SimpleLookupDto(u.Id, u.UnitCode, u.UnitName))
            .ToListAsync(cancellationToken);

        var activeEmps = await _context.Employees
            .AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation")
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleLookupDto(e.Id, e.EmployeeCode, $"{e.EmployeeCode} - {e.FullName}"))
            .ToListAsync(cancellationToken);

        var summariesQuery = _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .Include(m => m.Employee)
                .ThenInclude(e => e.OrganizationUnit)
            .Include(m => m.Employee)
                .ThenInclude(e => e.Position)
            .Where(m => m.Month == month && m.Year == year);

        if (request.UnitId.HasValue && request.UnitId.Value > 0)
        {
            var unit = await _context.OrganizationUnits.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UnitId.Value, cancellationToken);
            if (unit != null && !string.IsNullOrEmpty(unit.TreePath))
                summariesQuery = summariesQuery.Where(m => m.Employee.OrganizationUnit.TreePath!.StartsWith(unit.TreePath));
            else
                summariesQuery = summariesQuery.Where(m => m.Employee.OrganizationUnitId == request.UnitId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
        {
            var kw = request.SearchKeyword.Trim().ToLower();
            summariesQuery = summariesQuery.Where(m =>
                m.Employee.EmployeeCode.ToLower().Contains(kw) ||
                m.Employee.FullName.ToLower().Contains(kw));
        }

        var summaries = await summariesQuery
            .OrderBy(m => m.Employee.EmployeeCode)
            .Take(30)
            .ToListAsync(cancellationToken);

        var empIds = summaries.Select(s => s.EmployeeId).ToList();
        var dailies = await _context.DailyAttendances
            .AsNoTracking()
            .Where(d => empIds.Contains(d.EmployeeId) && d.AttendanceDate >= monthStart && d.AttendanceDate <= monthEnd)
            .ToListAsync(cancellationToken);

        var dailyMap = dailies
            .GroupBy(d => (d.EmployeeId, d.AttendanceDate.Day))
            .ToDictionary(g => g.Key, g => g.First());

        var matrixRows = new List<MonthlyAttendanceEmployeeRowDto>();
        int totalBusinessTripDays = 0;

        foreach (var sum in summaries)
        {
            var dayCells = new List<DailyCellSummaryDto>();
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateOnly(year, month, day);
                if (dailyMap.TryGetValue((sum.EmployeeId, day), out var d))
                {
                    if (d.AttendanceSymbol == "CT")
                        totalBusinessTripDays++;

                    var totalOt = d.OvertimeHours150 + d.OvertimeHours200 + d.OvertimeHours300;
                    dayCells.Add(new DailyCellSummaryDto(
                        d.Id,
                        day,
                        date,
                        d.AttendanceSymbol,
                        d.CheckInTime?.ToString("HH:mm"),
                        d.CheckOutTime?.ToString("HH:mm"),
                        d.LateMinutes,
                        d.EarlyLeaveMinutes,
                        totalOt,
                        d.IsManualEdited,
                        d.EditReason,
                        AttendanceBoardSeeder.GetSymbolCssClass(d.AttendanceSymbol, d.LateMinutes, d.IsManualEdited)));
                }
                else
                {
                    var isSun = date.DayOfWeek == DayOfWeek.Sunday;
                    var sym = isSun ? "OFF" : "-";
                    dayCells.Add(new DailyCellSummaryDto(
                        null,
                        day,
                        date,
                        sym,
                        null,
                        null,
                        0,
                        0,
                        0m,
                        false,
                        null,
                        AttendanceBoardSeeder.GetSymbolCssClass(sym, 0)));
                }
            }

            matrixRows.Add(new MonthlyAttendanceEmployeeRowDto(
                sum.Id,
                sum.EmployeeId,
                sum.Employee.EmployeeCode,
                sum.Employee.FullName,
                sum.Employee.OrganizationUnit.UnitName,
                sum.Employee.Position.PositionName,
                sum.StandardWorkdays,
                sum.ActualWorkdays,
                sum.PaidLeaveDays,
                sum.HolidayDays,
                sum.UnpaidLeaveDays,
                sum.TotalLateCount,
                sum.TotalLateMinutes,
                sum.TotalOtHours150,
                sum.TotalOtHours200,
                sum.TotalOtHours300,
                sum.TotalNightHours,
                sum.TotalPayableWorkdays,
                sum.IsLocked,
                dayCells,
                sum.TotalEarlyLeaveCount,
                sum.TotalEarlyLeaveMinutes,
                sum.TotalMissedPunchCount,
                sum.TotalPaidPersonalLeaveDays,
                sum.TotalUnpaidPersonalLeaveDays,
                sum.TotalUnexcusedAbsentDays,
                sum.SocialInsuranceLeaveDays,
                sum.TotalOtHours150Day,
                sum.TotalOtHours150Night,
                sum.TotalOtHours200Day,
                sum.TotalOtHours200Night,
                sum.TotalOtHours300Day,
                sum.TotalOtHours300Night,
                sum.ClosingStatus));
        }

        // Lấy danh sách đơn OT kèm tính tổng trần OT tháng/năm
        var otListRaw = await _context.OvertimeRequests
            .AsNoTracking()
            .Include(o => o.Employee)
                .ThenInclude(e => e.OrganizationUnit)
            .OrderByDescending(o => o.OvertimeDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        var otDtos = otListRaw.Select(o =>
        {
            var monthlyTotal = otListRaw
                .Where(x => x.EmployeeId == o.EmployeeId && x.OvertimeDate.Month == o.OvertimeDate.Month && x.OvertimeDate.Year == o.OvertimeDate.Year && x.Status != "Rejected")
                .Sum(x => x.RequestedHours);
            var yearlyTotal = otListRaw
                .Where(x => x.EmployeeId == o.EmployeeId && x.OvertimeDate.Year == o.OvertimeDate.Year && x.Status != "Rejected")
                .Sum(x => x.RequestedHours);

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

        // Lấy danh sách Giải trình chấm công
        var explainWfMap = await _context.WorkflowInstances
            .AsNoTracking()
            .Where(w => w.EntityType == "AttendanceExplanation")
            .ToDictionaryAsync(w => w.EntityId, cancellationToken);

        var explanationsRaw = await _context.DailyAttendances
            .AsNoTracking()
            .Include(d => d.Employee)
                .ThenInclude(e => e.OrganizationUnit)
            .Where(d => d.AttendanceDate >= monthStart && d.AttendanceDate <= monthEnd &&
                        (d.LateMinutes > 0 || d.IsManualEdited || !string.IsNullOrEmpty(d.EditReason)))
            .OrderByDescending(d => d.AttendanceDate)
            .Take(40)
            .ToListAsync(cancellationToken);

        var explanations = explanationsRaw.Select(d =>
        {
            explainWfMap.TryGetValue((int)d.Id, out var wf);
            return new AttendanceExplanationItemDto(
                d.Id,
                d.EmployeeId,
                d.Employee.EmployeeCode,
                d.Employee.FullName,
                d.Employee.OrganizationUnit.UnitName,
                d.AttendanceDate,
                d.AttendanceSymbol,
                d.LateMinutes,
                d.EarlyLeaveMinutes,
                d.EditReason,
                d.IsManualEdited,
                wf?.Id,
                wf?.Status ?? (d.IsManualEdited ? "Approved" : "NotSubmitted"));
        }).ToList();

        // Lấy nhật ký chấm công đa nguồn gần nhất
        var recentLogs = await _context.AttendanceLogs
            .AsNoTracking()
            .Include(l => l.Employee)
            .OrderByDescending(l => l.PunchTime)
            .Take(25)
            .Select(l => new AttendanceLogItemDto(
                l.Id,
                l.EmployeeId,
                l.Employee.EmployeeCode,
                l.Employee.FullName,
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

        var dbLocations = await _context.GeofenceLocations
            .AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.LocationCode)
            .Select(g => new GeofenceLocationSpec(
                g.LocationCode, g.LocationName, g.Address ?? string.Empty,
                (double)g.Latitude, (double)g.Longitude, g.AllowedRadiusMeters,
                g.OutsideGeofenceAction, g.QrRotationSeconds))
            .ToListAsync(cancellationToken);

        var effectiveLocations = dbLocations.Count > 0 ? dbLocations : VinamilkGeofenceCalculator.StandardLocations;
        int qrSeconds = effectiveLocations.FirstOrDefault()?.QrRotationSeconds ?? 30;

        var devices = await _context.AttendanceDevices
            .AsNoTracking()
            .OrderBy(d => d.DeviceCode)
            .Select(d => new AttendanceDeviceDto(d.Id, d.DeviceCode, d.DeviceName, d.ConnectorType, d.LocationName, d.SyncIntervalMinutes, d.IsActive, d.LastSyncAt, d.LastSyncSummary))
            .ToListAsync(cancellationToken);

        var deviceMappings = await _context.EmployeeDeviceMappings
            .AsNoTracking()
            .Include(m => m.Employee)
            .Where(m => m.IsActive)
            .OrderBy(m => m.DeviceUserCode)
            .Select(m => new EmployeeDeviceMappingDto(m.Id, m.EmployeeId, m.Employee.EmployeeCode, m.Employee.FullName, m.DeviceCode, m.DeviceUserCode, m.Notes))
            .ToListAsync(cancellationToken);

        var unmappedLogs = await _context.UnmappedAttendanceLogs
            .AsNoTracking()
            .Where(u => u.Status == "PendingMapping")
            .OrderByDescending(u => u.PunchTime)
            .Take(30)
            .Select(u => new UnmappedAttendanceLogDto(u.Id, u.DeviceCode, u.DeviceUserCode, u.PunchTime, u.PunchType, u.RawPayload, u.Status))
            .ToListAsync(cancellationToken);

        var firstLocked = summaries.FirstOrDefault(s => s.IsLocked);
        var closingStatus = summaries.Any(s => s.IsLocked) ? "Locked"
            : summaries.Any(s => s.ClosingStatus == "ManagerConfirmed") ? "ManagerConfirmed"
            : summaries.Any(s => s.ClosingStatus == "UnitSubmitted") ? "UnitSubmitted"
            : "Open";
        var submittedSample = summaries.FirstOrDefault(s => !string.IsNullOrEmpty(s.UnitSubmittedBy));
        var confirmedSample = summaries.FirstOrDefault(s => !string.IsNullOrEmpty(s.ManagerConfirmedBy));
        var reviewDeadline = summaries.Select(s => s.ReviewDeadlineAt).Where(d => d.HasValue).Max();

        var symbolDefs = await _context.AttendanceSymbolDefinitions
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new AttendanceSymbolDefinitionDto(
                s.Id, s.SymbolTypeCode, s.SymbolCode, s.SymbolName, s.Category,
                s.WorkdayMultiplier, s.IsPaidByCompany, s.IsPaidBySocialInsurance, s.ColorHex, s.CssClass))
            .ToListAsync(cancellationToken);

        var postLockAdjs = await _context.PostLockAttendanceAdjustments
            .AsNoTracking()
            .Include(a => a.Employee)
            .Where(a => a.TargetMonth == month && a.TargetYear == year)
            .OrderByDescending(a => a.CreatedAt)
            .Take(30)
            .Select(a => new PostLockAdjustmentDto(
                a.Id, a.EmployeeId, a.Employee.EmployeeCode, a.Employee.FullName,
                a.TargetMonth, a.TargetYear, a.AdjustmentDate,
                a.AppliedPayrollMonth, a.AppliedPayrollYear, a.AdjustmentType,
                a.WorkdayDelta, a.OtHours150Delta, a.OtHours200Delta, a.OtHours300Delta, a.NightHoursDelta,
                a.Reason, a.Status, a.ApprovedByUsername, a.ApprovedAt))
            .ToListAsync(cancellationToken);

        var recalcBatches = await _context.AttendanceRecalculationBatches
            .AsNoTracking()
            .Where(b => b.Month == month && b.Year == year)
            .OrderByDescending(b => b.Id)
            .Take(5)
            .Select(b => new AttendanceRecalculationBatchDto(
                b.Id, b.BatchCode, b.Month, b.Year, b.OrganizationUnitId, b.EmployeeId,
                b.FromDate, b.ToDate, b.Status, b.TotalEmployees, b.ProcessedEmployees, b.ProgressPercent,
                b.RequestedByUsername, b.StartedAt, b.CompletedAt, b.Message))
            .ToListAsync(cancellationToken);

        return new MonthlyAttendanceBoardDto(
            month,
            year,
            daysInMonth,
            AttendanceBoardSeeder.CalculateStandardWorkdays(year, month),
            firstLocked != null,
            firstLocked?.LockedBy,
            firstLocked?.LockedAt,
            request.UnitId,
            request.SearchKeyword,
            VinamilkGeofenceCalculator.GenerateDynamicQrToken(DateTime.UtcNow, qrSeconds),
            effectiveLocations,
            units,
            activeEmps,
            matrixRows,
            otDtos,
            explanations,
            recentLogs,
            summaries.Count(s => s.ActualWorkdays > 0),
            summaries.Sum(s => s.TotalLateCount),
            summaries.Sum(s => s.TotalOtHours150 + s.TotalOtHours200 + s.TotalOtHours300),
            totalBusinessTripDays,
            devices,
            deviceMappings,
            unmappedLogs,
            closingStatus,
            submittedSample?.UnitSubmittedBy,
            submittedSample?.UnitSubmittedAt,
            confirmedSample?.ManagerConfirmedBy,
            confirmedSample?.ManagerConfirmedAt,
            reviewDeadline,
            symbolDefs,
            postLockAdjs,
            recalcBatches);
    }
}

// ============================================================================
// 4. COMMAND: CHẤM CÔNG WEB CÓ ĐỊNH VỊ GPS GEOFENCING & QUÉT MÃ QR ĐỘNG
// ============================================================================

public record WebCheckInCommand(
    int EmployeeId,
    string PunchType, // CheckIn, CheckOut
    string SourceType, // Web, MobileGps, WebQr
    double Latitude,
    double Longitude,
    string? QrToken = null,
    DateTime? OverridePunchTime = null,
    string? ClientDeviceId = null,
    string? IpAddress = null,
    string? PhotoUrl = null,
    DateTime? OverrideUtcNow = null) : IRequest<string>;

public class WebCheckInCommandHandler : IRequestHandler<WebCheckInCommand, string>
{
    private readonly IApplicationDbContext _context;

    public WebCheckInCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(WebCheckInCommand request, CancellationToken cancellationToken)
    {
        var emp = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy nhân viên chấm công.");

        var nowLocal = request.OverridePunchTime ?? DateTime.Now;
        var utcNow = request.OverrideUtcNow ?? DateTime.UtcNow;

        // Xác định ca làm việc và NGÀY BẮT ĐẦU CA (ca qua đêm gán về ngày bắt đầu ca)
        var resolvedShift = await AttendancePolicyAndShiftEngine.ResolveShiftForPunchAsync(_context, emp.Id, nowLocal, cancellationToken);
        var shift = resolvedShift?.Shift;
        var workDate = resolvedShift?.AttributedDate ?? DateOnly.FromDateTime(nowLocal);

        // Kiểm tra bảng công tháng đã khóa chưa theo ngày công được quy đổi
        var isLocked = await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .AnyAsync(m => m.Month == workDate.Month && m.Year == workDate.Year && m.IsLocked, cancellationToken);

        if (isLocked)
            throw new InvalidOperationException($"Bảng chấm công tháng {workDate.Month:D2}/{workDate.Year} đã khóa, không thể chấm công bổ sung.");

        // Chặn chấm hộ theo 1 thiết bị / 1 tài khoản (nếu client gửi định danh thiết bị)
        if (!string.IsNullOrWhiteSpace(request.ClientDeviceId))
        {
            var fp = request.ClientDeviceId.Trim();
            var otherOwner = await _context.EmployeeDeviceBindings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.IsActive && b.DeviceFingerprint == fp && b.EmployeeId != emp.Id, cancellationToken);
            if (otherOwner != null)
                throw new InvalidOperationException("Thiết bị này đã được gắn cho tài khoản nhân viên khác. Hệ thống chặn chấm công hộ!");

            var empExistingDevice = await _context.EmployeeDeviceBindings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.IsActive && b.EmployeeId == emp.Id && b.DeviceFingerprint != fp, cancellationToken);
            if (empExistingDevice != null)
                throw new InvalidOperationException("Tài khoản đã gắn cố định với thiết bị khác. Liên hệ HR để đổi thiết bị (chặn chấm hộ).");

            var ownBinding = await _context.EmployeeDeviceBindings
                .FirstOrDefaultAsync(b => b.IsActive && b.EmployeeId == emp.Id && b.DeviceFingerprint == fp, cancellationToken);
            if (ownBinding == null)
            {
                await _context.EmployeeDeviceBindings.AddAsync(new EmployeeDeviceBinding
                {
                    EmployeeId = emp.Id,
                    DeviceFingerprint = fp,
                    DeviceLabel = request.SourceType,
                    LastIpAddress = request.IpAddress,
                    LastSeenAt = utcNow,
                    IsActive = true
                }, cancellationToken);
            }
            else
            {
                ownBinding.LastIpAddress = request.IpAddress ?? ownBinding.LastIpAddress;
                ownBinding.LastSeenAt = utcNow;
            }
        }

        // Đánh giá tọa độ GPS Haversine theo bảng GeofenceLocations trong DB (hoặc mặc định chuẩn)
        var dbLocations = await _context.GeofenceLocations
            .AsNoTracking()
            .Where(g => g.IsActive)
            .Select(g => new GeofenceLocationSpec(
                g.LocationCode, g.LocationName, g.Address ?? string.Empty,
                (double)g.Latitude, (double)g.Longitude, g.AllowedRadiusMeters,
                g.OutsideGeofenceAction, g.QrRotationSeconds))
            .ToListAsync(cancellationToken);

        var (nearestLoc, distMeters, isWithinGeofence) = VinamilkGeofenceCalculator.EvaluateGps(
            request.Latitude, request.Longitude, dbLocations.Count > 0 ? dbLocations : null);

        // Kiểm tra mã QR động (30–60 giây) nếu chọn phương thức WebQr
        if (string.Equals(request.SourceType, "WebQr", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.QrToken))
                throw new InvalidOperationException("Vui lòng quét mã QR động tại điểm chấm công.");

            var expectedQr = VinamilkGeofenceCalculator.GenerateDynamicQrToken(utcNow, nearestLoc.QrRotationSeconds);
            if (!request.QrToken.Trim().Equals(expectedQr, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Mã QR động '{request.QrToken}' không hợp lệ hoặc đã hết hạn ({nearestLoc.QrRotationSeconds} giây).");
        }

        // Xử lý ngoài Geofence theo cấu hình của địa điểm: Reject hoặc PendingApproval
        string geofenceStatus;
        if (isWithinGeofence)
        {
            geofenceStatus = "Inside";
        }
        else if (string.Equals(nearestLoc.OutsideGeofenceAction, "Reject", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Chấm công bị từ chối: tọa độ cách {nearestLoc.Name} là {distMeters:0.#}m, vượt bán kính cho phép {nearestLoc.AllowedRadiusMeters:0.#}m.");
        }
        else
        {
            geofenceStatus = "OutsidePendingApproval";
        }

        var addressNote = $"{nearestLoc.Name} (Khoảng cách GPS: {distMeters:0.#}m - {(isWithinGeofence ? "Trong bán kính" : "Ngoài vùng - Chờ duyệt")})";

        await _context.AttendanceLogs.AddAsync(new AttendanceLog
        {
            EmployeeId = emp.Id,
            PunchTime = nowLocal,
            PunchType = request.PunchType,
            SourceType = request.SourceType,
            DeviceId = !string.IsNullOrWhiteSpace(request.ClientDeviceId)
                ? request.ClientDeviceId.Trim()
                : (request.SourceType == "WebQr" ? "VINAMILK-QR-KIOSK" : "VINAMILK-WEB-GPS"),
            Latitude = (decimal)request.Latitude,
            Longitude = (decimal)request.Longitude,
            LocationAddress = addressNote,
            IpAddress = request.IpAddress,
            PhotoUrl = request.PhotoUrl,
            GeofenceLocationCode = nearestLoc.Code,
            GeofenceStatus = geofenceStatus,
            DistanceMeters = distMeters,
            ClientDeviceFingerprint = request.ClientDeviceId
        }, cancellationToken);

        var daily = await _context.DailyAttendances
            .FirstOrDefaultAsync(d => d.EmployeeId == emp.Id && d.AttendanceDate == workDate, cancellationToken);

        if (daily == null)
        {
            daily = new DailyAttendance
            {
                EmployeeId = emp.Id,
                AttendanceDate = workDate,
                WorkShiftId = shift?.Id,
                CheckInTime = request.PunchType == "CheckIn" ? nowLocal : null,
                CheckOutTime = request.PunchType == "CheckOut" ? nowLocal : null
            };
            await _context.DailyAttendances.AddAsync(daily, cancellationToken);
        }
        else
        {
            if (shift != null)
            {
                daily.WorkShiftId = shift.Id;
            }
            if (request.PunchType == "CheckIn")
            {
                daily.CheckInTime = nowLocal;
            }
            else
            {
                daily.CheckOutTime = nowLocal;
            }
        }

        if (geofenceStatus == "OutsidePendingApproval")
        {
            daily.AttendanceSymbol = "CGD";
            daily.WorkdayCount = 0m;
            daily.EditReason = $"[NGOÀI GEOFENCE CHỜ DUYỆT] {addressNote}";
        }
        else if (shift != null)
        {
            var statutory = await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(_context, workDate, cancellationToken);
            var resolvedPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, emp.Id, workDate, cancellationToken);
            var calc = AttendancePolicyAndShiftEngine.CalculatePunchAttendance(
                workDate, shift, daily.CheckInTime, daily.CheckOutTime, resolvedPolicy.Policy, statutory);

            daily.AttendanceSymbol = calc.AttendanceSymbol;
            daily.WorkingHours = calc.WorkingHours;
            daily.NightShiftHours = calc.NightShiftHours;
            daily.WorkdayCount = calc.WorkdayCount;
            daily.LateMinutes = calc.LateMinutes;
            daily.EarlyLeaveMinutes = calc.EarlyLeaveMinutes;
            daily.EditReason = $"{addressNote} | {calc.CalculationNotes}";
        }
        else
        {
            daily.AttendanceSymbol = "X";
            daily.WorkingHours = 8.0m;
            daily.WorkdayCount = 1.0m;
            daily.EditReason = addressNote;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(_context, workDate.Month, workDate.Year, cancellationToken);

        return $"Chấm công {request.PunchType} ({geofenceStatus}) thành công cho {emp.FullName} lúc {nowLocal:HH:mm:ss} (Ngày công: {workDate:dd/MM/yyyy}) tại {addressNote}.";
    }
}

// ============================================================================
// 5. COMMAND: ĐĂNG KÝ LÀM THÊM GIỜ (OVERTIME) QUA WORKFLOW & KIỂM TRA TRẦN OT
// ============================================================================

public record SubmitOvertimeRequestCommand(
    int EmployeeId,
    DateOnly OvertimeDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string OvertimeRateType, // OT150, OT200, OT300
    string Reason) : IRequest<int>;

public class SubmitOvertimeRequestCommandHandler : IRequestHandler<SubmitOvertimeRequestCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public SubmitOvertimeRequestCommandHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<int> Handle(SubmitOvertimeRequestCommand request, CancellationToken cancellationToken)
    {
        double hours = (request.EndTime - request.StartTime).TotalHours;
        if (hours <= 0) hours += 24.0; // Ca qua đêm
        decimal requestedHours = Math.Round((decimal)hours, 1);

        if (requestedHours <= 0 || requestedHours > 12)
            throw new InvalidOperationException("Số giờ đăng ký OT trong ngày phải từ 0.5 đến 12 giờ.");

        var statutory = await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(_context, request.OvertimeDate, cancellationToken);

        // Kiểm tra trần OT tháng và năm từ bảng AttendanceStatutoryParameter
        var monthOt = await _context.OvertimeRequests
            .AsNoTracking()
            .Where(o => o.EmployeeId == request.EmployeeId &&
                        o.OvertimeDate.Month == request.OvertimeDate.Month &&
                        o.OvertimeDate.Year == request.OvertimeDate.Year &&
                        o.Status != "Rejected" && o.Status != "Cancelled")
            .SumAsync(o => o.RequestedHours, cancellationToken);

        if (monthOt + requestedHours > statutory.MaxMonthlyOvertimeHours)
            throw new InvalidOperationException($"Vượt trần làm thêm giờ tháng! Tổng OT tháng hiện tại ({monthOt:0.#}h) + đơn mới ({requestedHours:0.#}h) vượt giới hạn {statutory.MaxMonthlyOvertimeHours:0.#} giờ/tháng theo {statutory.LegalReference}.");

        var yearOt = await _context.OvertimeRequests
            .AsNoTracking()
            .Where(o => o.EmployeeId == request.EmployeeId &&
                        o.OvertimeDate.Year == request.OvertimeDate.Year &&
                        o.Status != "Rejected" && o.Status != "Cancelled")
            .SumAsync(o => o.RequestedHours, cancellationToken);

        if (yearOt + requestedHours > statutory.MaxYearlyOvertimeHours)
            throw new InvalidOperationException($"Vượt trần làm thêm giờ năm ({yearOt + requestedHours:0.#}/{statutory.MaxYearlyOvertimeHours:0.#} giờ) theo {statutory.LegalReference}.");

        var otEntity = new OvertimeRequest
        {
            EmployeeId = request.EmployeeId,
            OvertimeDate = request.OvertimeDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RequestedHours = requestedHours,
            ApprovedHours = 0m,
            OvertimeRateType = request.OvertimeRateType,
            Reason = request.Reason.Trim(),
            Status = "Pending"
        };

        await _context.OvertimeRequests.AddAsync(otEntity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Khởi tạo luồng phê duyệt Workflow Engine dùng chung
        var wfInstanceId = await _sender.Send(new StartWorkflowInstanceCommand(
            "OvertimeRequest",
            otEntity.Id,
            request.EmployeeId,
            $"Đăng ký OT {request.OvertimeRateType} ({requestedHours:0.#}h ngày {request.OvertimeDate:dd/MM/yyyy})",
            requestedHours,
            "/Attendance?tab=overtime"), cancellationToken);

        otEntity.WorkflowInstanceId = wfInstanceId;
        await _context.SaveChangesAsync(cancellationToken);

        return otEntity.Id;
    }
}

// ============================================================================
// 6. COMMAND: GỬI GIẢI TRÌNH CHẤM CÔNG / BỔ SUNG CÔNG QUA WORKFLOW
// ============================================================================

public record SubmitAttendanceExplanationCommand(
    int EmployeeId,
    DateOnly AttendanceDate,
    TimeOnly ProposedCheckIn,
    TimeOnly ProposedCheckOut,
    string Reason) : IRequest<int>;

public class SubmitAttendanceExplanationCommandHandler : IRequestHandler<SubmitAttendanceExplanationCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public SubmitAttendanceExplanationCommandHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<int> Handle(SubmitAttendanceExplanationCommand request, CancellationToken cancellationToken)
    {
        var monthSummary = await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.EmployeeId == request.EmployeeId
                && m.Month == request.AttendanceDate.Month
                && m.Year == request.AttendanceDate.Year, cancellationToken);

        var isLocked = monthSummary?.IsLocked == true || await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .AnyAsync(m => m.Month == request.AttendanceDate.Month && m.Year == request.AttendanceDate.Year && m.IsLocked, cancellationToken);

        if (isLocked)
            throw new InvalidOperationException($"Bảng công tháng {request.AttendanceDate.Month:D2}/{request.AttendanceDate.Year} đã khóa, không thể gửi giải trình (chỉ điều chỉnh kỳ sau).");

        if (monthSummary?.ReviewDeadlineAt.HasValue == true && DateTime.UtcNow > monthSummary.ReviewDeadlineAt.Value)
            throw new InvalidOperationException($"Đã quá thời hạn {monthSummary.ReviewWindowDays} ngày nhân viên xem & giải trình công (hạn {monthSummary.ReviewDeadlineAt.Value:dd/MM/yyyy HH:mm}).");

        var resolvedPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(
            _context, request.EmployeeId, request.AttendanceDate, cancellationToken);
        var monthStart = new DateOnly(request.AttendanceDate.Year, request.AttendanceDate.Month, 1);
        var monthEnd = new DateOnly(request.AttendanceDate.Year, request.AttendanceDate.Month, DateTime.DaysInMonth(request.AttendanceDate.Year, request.AttendanceDate.Month));

        var existingExplainCount = await _context.DailyAttendances
            .AsNoTracking()
            .CountAsync(d => d.EmployeeId == request.EmployeeId
                && d.AttendanceDate >= monthStart
                && d.AttendanceDate <= monthEnd
                && d.AttendanceDate != request.AttendanceDate
                && d.EditReason != null
                && d.EditReason.Contains("Đề xuất giờ:"), cancellationToken);

        int projectedMonthExplainCount = existingExplainCount + 1;
        bool isOverQuota = projectedMonthExplainCount > resolvedPolicy.Policy.MaxMissedPunchExplanationsPerMonth;

        var daily = await _context.DailyAttendances
            .FirstOrDefaultAsync(d => d.EmployeeId == request.EmployeeId && d.AttendanceDate == request.AttendanceDate, cancellationToken);

        var explainDetail = $"Đề xuất giờ: {request.ProposedCheckIn:HH\\:mm}-{request.ProposedCheckOut:HH\\:mm} | Lý do: {request.Reason.Trim()}"
            + (isOverQuota ? $" | [VƯỢT QUOTA {projectedMonthExplainCount}/{resolvedPolicy.Policy.MaxMissedPunchExplanationsPerMonth}: {resolvedPolicy.Policy.MissedPunchOverQuotaAction}]" : string.Empty);

        decimal penaltyWorkday = 1m;
        string symbol = "M";
        if (isOverQuota)
        {
            if (string.Equals(resolvedPolicy.Policy.MissedPunchOverQuotaAction, "DeductHalfDay", StringComparison.OrdinalIgnoreCase))
            {
                penaltyWorkday = 0.5m;
                symbol = "X/2";
            }
            else if (string.Equals(resolvedPolicy.Policy.MissedPunchOverQuotaAction, "DeductFullDay", StringComparison.OrdinalIgnoreCase))
            {
                penaltyWorkday = 0m;
                symbol = "V";
            }
        }

        if (daily == null)
        {
            daily = new DailyAttendance
            {
                EmployeeId = request.EmployeeId,
                AttendanceDate = request.AttendanceDate,
                CheckInTime = request.AttendanceDate.ToDateTime(request.ProposedCheckIn),
                CheckOutTime = request.AttendanceDate.ToDateTime(request.ProposedCheckOut),
                AttendanceSymbol = symbol,
                WorkingHours = penaltyWorkday * 8m,
                WorkdayCount = penaltyWorkday,
                IsManualEdited = false,
                EditReason = explainDetail
            };
            await _context.DailyAttendances.AddAsync(daily, cancellationToken);
        }
        else
        {
            daily.EditReason = explainDetail;
            if (isOverQuota && resolvedPolicy.Policy.MissedPunchOverQuotaAction is "DeductHalfDay" or "DeductFullDay")
            {
                daily.WorkdayCount = penaltyWorkday;
                daily.AttendanceSymbol = symbol;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(_context, request.AttendanceDate.Month, request.AttendanceDate.Year, cancellationToken);

        var wfId = await _sender.Send(new StartWorkflowInstanceCommand(
            "AttendanceExplanation",
            (int)daily.Id,
            request.EmployeeId,
            $"Giải trình công ngày {request.AttendanceDate:dd/MM/yyyy} ({request.ProposedCheckIn:HH\\:mm}-{request.ProposedCheckOut:HH\\:mm})",
            1m,
            "/Attendance?tab=explanations"), cancellationToken);

        return wfId;
    }
}

// ============================================================================
// 7. COMMAND: TÍNH LẠI & KHÓA / MỞ KHÓA BẢNG CÔNG THÁNG
// ============================================================================

public record RecalculateMonthlyAttendanceCommand(int Month, int Year) : IRequest<int>;

public class RecalculateMonthlyAttendanceCommandHandler : IRequestHandler<RecalculateMonthlyAttendanceCommand, int>
{
    private readonly IApplicationDbContext _context;

    public RecalculateMonthlyAttendanceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(RecalculateMonthlyAttendanceCommand request, CancellationToken cancellationToken)
    {
        await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(_context, request.Month, request.Year, cancellationToken);
        return await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .CountAsync(m => m.Month == request.Month && m.Year == request.Year, cancellationToken);
    }
}

public record ToggleLockMonthlyAttendanceCommand(
    int Month,
    int Year,
    bool LockState) : IRequest<string>;

public class ToggleLockMonthlyAttendanceCommandHandler : IRequestHandler<ToggleLockMonthlyAttendanceCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ToggleLockMonthlyAttendanceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(ToggleLockMonthlyAttendanceCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.RequireUserId();
        var actor = _currentUser.RequireUsername();

        var summaries = await _context.MonthlyAttendanceSummaries
            .Where(m => m.Month == request.Month && m.Year == request.Year)
            .ToListAsync(cancellationToken);

        foreach (var s in summaries)
        {
            s.IsLocked = request.LockState;
            s.ClosingStatus = request.LockState ? "Locked" : "Open";
            s.LockedAt = request.LockState ? DateTime.UtcNow : null;
            s.LockedBy = request.LockState ? actor : null;
        }

        await _context.AuditLogs.AddAsync(new Domain.Entities.SystemEntities.AuditLog
        {
            UserId = actorUserId,
            Username = actor,
            ActionType = request.LockState ? "Lock" : "Unlock",
            ModuleName = "TimeAttendance",
            EntityName = "MonthlyAttendanceSummary",
            EntityId = $"{request.Year}-{request.Month:D2}",
            NewValuesJson = $"{{\"summary\":\"{(request.LockState ? "Khóa" : "Mở khóa")} bảng chấm công tháng {request.Month:D2}/{request.Year} ({summaries.Count} NV)\"}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return request.LockState
            ? $"Đã KHÓA bảng chấm công tháng {request.Month:D2}/{request.Year} ({summaries.Count} nhân viên) để chuyển sang tính lương."
            : $"Đã MỞ KHÓA bảng chấm công tháng {request.Month:D2}/{request.Year} để điều chỉnh.";
    }
}

