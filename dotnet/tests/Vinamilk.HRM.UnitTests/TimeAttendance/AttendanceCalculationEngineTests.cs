using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Services;
using Xunit;

namespace Vinamilk.HRM.UnitTests.TimeAttendance;

public class AttendanceCalculationEngineTests
{
    private sealed class MockCurrentUser(int userId = 30, string username = "hr.lead") : ICurrentUserService
    {
        public int? UserId => userId;
        public string Username => username;
        public int? EmployeeId => null;
        public string? IpAddress => "10.20.30.40";
        public string? UserAgent => "UnitTest/CL3";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permissionCode) => true;
    }

    private static WorkShift CreateDayShift() => new()
    {
        Id = 1,
        ShiftCode = "HC",
        ShiftName = "Hành chính",
        ShiftType = "Fixed",
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(17, 0),
        BreakStartTime = new TimeOnly(12, 0),
        BreakEndTime = new TimeOnly(13, 0),
        IsBreakPaid = false,
        StandardHours = 8.0m,
        WorkdayValue = 1.0m,
        GraceLateMinutes = 5
    };

    private static WorkShift CreateNightShift() => new()
    {
        Id = 4,
        ShiftCode = "CA3",
        ShiftName = "Ca 3 Đêm",
        ShiftType = "Overnight",
        StartTime = new TimeOnly(22, 0),
        EndTime = new TimeOnly(6, 0),
        IsOvernight = true,
        StandardHours = 8.0m,
        WorkdayValue = 1.0m,
        NightShiftAllowanceRate = 0.30m,
        GraceLateMinutes = 5
    };

    private static AttendancePolicy CreateDefaultPolicy(string pairingMode = "FirstLast") => new()
    {
        GraceLateMinutes = 5,
        GraceEarlyMinutes = 5,
        RoundingIntervalMinutes = 5,
        RoundingMode = "Nearest",
        HalfDayLateThresholdMinutes = 60,
        ZeroDayLateThresholdMinutes = 240,
        RequireApprovedOvertimeRequest = true,
        PunchPairingMode = pairingMode,
        MissedPunchCreditWorkdays = 0m,
        EmployeeReviewWindowDays = 3,
        WeeklyWorkdaysMask = "Mon,Tue,Wed,Thu,Fri"
    };

    private static AttendanceStatutoryParameter CreateStatutory() => new()
    {
        EffectiveFrom = new DateOnly(2021, 1, 1),
        LegalReference = "BLLD 45/2019/QH14",
        NightWorkStartTime = new TimeOnly(22, 0),
        NightWorkEndTime = new TimeOnly(6, 0),
        MaxNormalDailyHours = 8.0m,
        WeekdayOtMultiplier = 1.5m,
        WeeklyDayOffOtMultiplier = 2.0m,
        HolidayOtMultiplier = 3.0m
    };

    private static AttendanceDailyCalculationInput BuildInput(
        DateOnly date,
        WorkShift? shift = null,
        bool isWeeklyOff = false,
        Holiday? holiday = null,
        IReadOnlyList<AttendancePunchInput>? punches = null,
        IReadOnlyList<AttendanceApprovedLeaveInput>? leaves = null,
        IReadOnlyList<AttendanceApprovedOvertimeInput>? ots = null,
        AttendanceApprovedExplanationInput? explanation = null,
        AttendancePolicy? policy = null,
        AttendanceSymbolCatalog? symbols = null) =>
        new(
            date,
            shift ?? CreateDayShift(),
            false,
            isWeeklyOff,
            holiday,
            punches ?? Array.Empty<AttendancePunchInput>(),
            leaves ?? Array.Empty<AttendanceApprovedLeaveInput>(),
            ots ?? Array.Empty<AttendanceApprovedOvertimeInput>(),
            explanation,
            policy ?? CreateDefaultPolicy(),
            CreateStatutory(),
            symbols ?? AttendanceSymbolCatalog.CreateDefault());

    [Fact]
    public void S01_NormalFullDay_ShouldAward1WorkdayAnd8Hours()
    {
        // Tính tay: 08:00->17:00 = 9h trừ nghỉ trưa 12:00->13:00 (1h) = 8.0h công chuẩn, 1.0 công, mã X.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 1),
            punches:
            [
                new(new DateTime(2026, 7, 1, 8, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 1, 17, 0, 0), "CheckOut", "BiometricDevice")
            ]));

        res.StatusCode.Should().Be("X");
        res.WorkingHours.Should().Be(8.00m);
        res.WorkdayCount.Should().Be(1.00m);
        res.LateMinutes.Should().Be(0);
        res.EarlyLeaveMinutes.Should().Be(0);
    }

    [Fact]
    public void S02_LateWithinGraceWindow_ShouldBeZeroLateMinutes()
    {
        // Tính tay: Ca 08:00, ân hạn 5 phút. Vào lúc 08:05 (trễ 5p <= 5p) -> LateMinutes = 0, vẫn đạt 1.0 công X.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 1),
            punches:
            [
                new(new DateTime(2026, 7, 1, 8, 5, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 1, 17, 5, 0), "CheckOut", "BiometricDevice")
            ]));

        res.LateMinutes.Should().Be(0);
        res.WorkdayCount.Should().Be(1.00m);
        res.StatusCode.Should().Be("X");
    }

    [Fact]
    public void S03_LateOutsideGraceWindow_ShouldRoundNearest5Minutes()
    {
        // Tính tay: Vào 08:08 (trễ thực 8p > 5p ân hạn). Làm tròn Nearest 5p -> 10 phút trễ. Chưa quá 60p nên vẫn 1.0 công.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 1),
            punches:
            [
                new(new DateTime(2026, 7, 1, 8, 8, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 1, 17, 0, 0), "CheckOut", "BiometricDevice")
            ]));

        res.RawLateMinutes.Should().Be(8);
        res.LateMinutes.Should().Be(10);
        res.WorkdayCount.Should().Be(1.00m);
    }

    [Fact]
    public void S04_LateBeyondHalfDayThreshold_ShouldAwardHalfWorkday()
    {
        // Tính tay: Vào 09:16 -> trễ 76p -> làm tròn 75p > 60p (ngưỡng nửa công) -> chỉ tính 0.5 công, mã ½.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 1),
            punches:
            [
                new(new DateTime(2026, 7, 1, 9, 16, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 1, 17, 0, 0), "CheckOut", "BiometricDevice")
            ]));

        res.LateMinutes.Should().Be(75);
        res.WorkdayCount.Should().Be(0.50m);
        res.StatusCode.Should().Be("½");
    }

    [Fact]
    public void S05_LateBeyondZeroDayThreshold_ShouldAwardZeroWorkdayKL()
    {
        // Tính tay: Vào 12:30 -> trễ 270p > 240p (ngưỡng mất công) -> 0 công, mã KL.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 1),
            punches:
            [
                new(new DateTime(2026, 7, 1, 12, 30, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 1, 17, 0, 0), "CheckOut", "BiometricDevice")
            ]));

        res.LateMinutes.Should().Be(270);
        res.WorkdayCount.Should().Be(0m);
        res.StatusCode.Should().Be("KL");
        res.UnexcusedAbsentDays.Should().Be(1.00m);
    }

    [Fact]
    public void S06_EarlyLeaveOutsideGrace_ShouldRecordRoundedEarlyMinutes()
    {
        // Tính tay: Tan ca 17:00, ra lúc 16:48 -> sớm 12p > 5p ân hạn -> làm tròn 10 phút về sớm.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 1),
            punches:
            [
                new(new DateTime(2026, 7, 1, 8, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 1, 16, 48, 0), "CheckOut", "BiometricDevice")
            ]));

        res.RawEarlyLeaveMinutes.Should().Be(12);
        res.EarlyLeaveMinutes.Should().Be(10);
        res.StatusCode.Should().Be("X");
    }

    [Fact]
    public void S07_OvernightShiftCA3_ShouldCalculate8NightHours()
    {
        // Tính tay: Ca đêm 22:00 (01/07) -> 06:00 (02/07), không nghỉ giữa ca.
        // Toàn bộ 8h nằm trong khung đêm [22:00..06:00] -> WorkingHours=8.0, NightShiftHours=8.0, WorkdayCount=1.0, X.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 1),
            shift: CreateNightShift(),
            punches:
            [
                new(new DateTime(2026, 7, 1, 22, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 2, 6, 0, 0), "CheckOut", "BiometricDevice")
            ]));

        res.StatusCode.Should().Be("X");
        res.WorkingHours.Should().Be(8.00m);
        res.NightShiftHours.Should().Be(8.00m);
        res.WorkdayCount.Should().Be(1.00m);
    }

    [Fact]
    public void S08_AfternoonShiftCA2WithNightOvertime_ShouldSplitRegularAndNightOt()
    {
        // Tính tay: Ca 2 (14:00–22:00), NV chấm vào 14:00, duyệt OT 22:00–24:00 (2h), ra 00:05 ngày hôm sau.
        // Giờ ca chính 14:00–22:00 = 8.0h (0h đêm vì kết thúc đúng 22:00). OT150 đêm 22:00–00:00 = 2.0h.
        var ca2 = new WorkShift
        {
            Id = 3,
            ShiftCode = "CA2",
            ShiftType = "Fixed",
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(22, 0),
            StandardHours = 8.0m,
            WorkdayValue = 1.0m
        };
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 1),
            shift: ca2,
            punches:
            [
                new(new DateTime(2026, 7, 1, 14, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 2, 0, 5, 0), "CheckOut", "BiometricDevice")
            ],
            ots: [new AttendanceApprovedOvertimeInput(new TimeOnly(22, 0), new TimeOnly(0, 0), 2.0m, "OT150")]));

        res.WorkingHours.Should().Be(8.00m);
        res.NightShiftHours.Should().Be(0m);
        res.OvertimeHours150Day.Should().Be(0m);
        res.OvertimeHours150Night.Should().Be(2.00m);
        res.OvertimeHours150.Should().Be(2.00m);
    }

    [Fact]
    public void S09_WeekdayOvertimeSpanningDayAndNight_ShouldSplitOt150Day4hAndNight1h()
    {
        // Tính tay: Ca HC (08:00–17:00). Duyệt OT 18:00–23:00 (5h), ra 23:05.
        // Từ 18:00–22:00 = 4.0h ban ngày; 22:00–23:00 = 1.0h ban đêm -> OT150Day = 4.0, OT150Night = 1.0.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 2),
            punches:
            [
                new(new DateTime(2026, 7, 2, 8, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 2, 23, 5, 0), "CheckOut", "BiometricDevice")
            ],
            ots: [new AttendanceApprovedOvertimeInput(new TimeOnly(18, 0), new TimeOnly(23, 0), 5.0m, "OT150")]));

        res.OvertimeHours150Day.Should().Be(4.00m);
        res.OvertimeHours150Night.Should().Be(1.00m);
        res.OvertimeHours150.Should().Be(5.00m);
    }

    [Fact]
    public void S10_WeeklyOffDayOvertime_ShouldGoIntoOt200Day()
    {
        // Tính tay: Chủ nhật (nghỉ tuần OFF), làm và duyệt OT 09:00–14:00 (5h ban ngày) -> OT200Day = 5.0h, OFF.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 5),
            isWeeklyOff: true,
            punches:
            [
                new(new DateTime(2026, 7, 5, 9, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 5, 14, 0, 0), "CheckOut", "BiometricDevice")
            ],
            ots: [new AttendanceApprovedOvertimeInput(new TimeOnly(9, 0), new TimeOnly(14, 0), 5.0m, "OT200")]));

        res.StatusCode.Should().Be("OFF");
        res.WorkdayCount.Should().Be(0m);
        res.OvertimeHours200Day.Should().Be(5.00m);
        res.OvertimeHours200Night.Should().Be(0m);
    }

    [Fact]
    public void S11_WeeklyOffNightOvertime_ShouldSplitOt200DayAndNight()
    {
        // Tính tay: Ngày nghỉ tuần, OT duyệt 20:00–02:00 (6h): 20:00–22:00 = 2h ngày, 22:00–02:00 = 4h đêm.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 5),
            isWeeklyOff: true,
            punches:
            [
                new(new DateTime(2026, 7, 5, 19, 55, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 6, 2, 10, 0), "CheckOut", "BiometricDevice")
            ],
            ots: [new AttendanceApprovedOvertimeInput(new TimeOnly(20, 0), new TimeOnly(2, 0), 6.0m, "OT200")]));

        res.OvertimeHours200Day.Should().Be(2.00m);
        res.OvertimeHours200Night.Should().Be(4.00m);
        res.OvertimeHours200.Should().Be(6.00m);
    }

    [Fact]
    public void S12_PaidHolidayNoPunch_ShouldAwardHolidaySymbolAnd1Workday()
    {
        // Tính tay: Ngày Lễ 02/09 được trả nguyên lương, 0 log quét thẻ -> mã L, WorkdayCount = 1.0, Late = 0.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 9, 2),
            holiday: new Holiday { HolidayDate = new DateOnly(2026, 9, 2), HolidayName = "Quốc khánh 2/9", IsPaidHoliday = true }));

        res.StatusCode.Should().Be("L");
        res.WorkdayCount.Should().Be(1.00m);
        res.LateMinutes.Should().Be(0);
    }

    [Fact]
    public void S13_HolidayOvertimeDayAndNight_ShouldSplitOt300Day2hAndNight3h()
    {
        // Tính tay: Ngày Lễ 02/09, duyệt OT 20:00–01:00 (5h):
        // Công lễ chuẩn vẫn hưởng 1.0 (L); OT300 ngày (20:00–22:00) = 2.0h; OT300 đêm (22:00–01:00) = 3.0h.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 9, 2),
            holiday: new Holiday { HolidayDate = new DateOnly(2026, 9, 2), HolidayName = "Quốc khánh 2/9", IsPaidHoliday = true },
            punches:
            [
                new(new DateTime(2026, 9, 2, 19, 55, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 9, 3, 1, 5, 0), "CheckOut", "BiometricDevice")
            ],
            ots: [new AttendanceApprovedOvertimeInput(new TimeOnly(20, 0), new TimeOnly(1, 0), 5.0m, "OT300")]));

        res.StatusCode.Should().Be("L");
        res.WorkdayCount.Should().Be(1.00m);
        res.OvertimeHours300Day.Should().Be(2.00m);
        res.OvertimeHours300Night.Should().Be(3.00m);
        res.OvertimeHours300.Should().Be(5.00m);
    }

    [Fact]
    public void S14_MorningAnnualLeaveAndAfternoonWork_ShouldNotPenalizeMorningAndAwardHalfPlusHalf()
    {
        // Tính tay: Sáng xin phép năm (0.5 ngày P), chiều làm 13:00–17:00 (4h).
        // Không tính đi muộn 08:00->13:00 -> LateMinutes = 0, WorkingHours = 4.0h, WorkdayCount = 0.5, PaidLeaveDays = 0.5, Status = "½+P".
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 3),
            punches:
            [
                new(new DateTime(2026, 7, 3, 13, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 3, 17, 0, 0), "CheckOut", "BiometricDevice")
            ],
            leaves: [new AttendanceApprovedLeaveInput("ANNUAL", "P", "Morning", 0.5m, true, false)]));

        res.StatusCode.Should().Be("½+P");
        res.LateMinutes.Should().Be(0);
        res.WorkingHours.Should().Be(4.00m);
        res.WorkdayCount.Should().Be(0.50m);
        res.PaidLeaveDays.Should().Be(0.50m);
    }

    [Fact]
    public void S15_FullDayBusinessTripWithoutPunches_ShouldAwardFullWorkdayCT()
    {
        // Tính tay: Công tác cả ngày không quét thẻ máy công ty -> 0 punch vẫn đủ 8.0h, 1.0 công, mã CT.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 6),
            leaves: [new AttendanceApprovedLeaveInput("BUSINESS_TRIP", "CT", "FullDay", 1.0m, true, false, IsBusinessTrip: true, Description: "Công tác Hà Nội")]));

        res.StatusCode.Should().Be("CT");
        res.WorkdayCount.Should().Be(1.00m);
        res.WorkingHours.Should().Be(8.00m);
        res.LateMinutes.Should().Be(0);
    }

    [Fact]
    public void S16_MultiplePunches_FirstLastMode_ShouldUseEarliestAndLatest()
    {
        // Tính tay: FirstLast chấm 4 lần (07:55, 11:30, 12:45, 17:05) -> lấy đầu 07:55 & cuối 17:05, cắt theo ca 08:00-17:00 trừ trưa 12:00-13:00 = 8.0h.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 7),
            punches:
            [
                new(new DateTime(2026, 7, 7, 7, 55, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 7, 11, 30, 0), "CheckOut", "BiometricDevice"),
                new(new DateTime(2026, 7, 7, 12, 45, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 7, 17, 5, 0), "CheckOut", "BiometricDevice")
            ],
            policy: CreateDefaultPolicy("FirstLast")));

        res.ActualCheckIn.Should().Be(new DateTime(2026, 7, 7, 7, 55, 0));
        res.ActualCheckOut.Should().Be(new DateTime(2026, 7, 7, 17, 5, 0));
        res.WorkingHours.Should().Be(8.00m);
    }

    [Fact]
    public void S17_MultiplePunches_PairedSegmentsMode_ShouldSumOnlyPairedIntervals()
    {
        // Tính tay: PairedSegments chấm [08:00->11:00 = 3h] và [14:00->17:00 = 3h] -> tổng thực làm = 6.0h (nghỉ trưa dài không được cộng).
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 7),
            punches:
            [
                new(new DateTime(2026, 7, 7, 8, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 7, 11, 0, 0), "CheckOut", "BiometricDevice"),
                new(new DateTime(2026, 7, 7, 14, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 7, 17, 0, 0), "CheckOut", "BiometricDevice")
            ],
            policy: CreateDefaultPolicy("PairedSegments")));

        res.WorkingHours.Should().Be(6.00m);
        res.StatusCode.Should().Be("X");
    }

    [Fact]
    public void S18_SingleMissedPunchWithoutExplanation_ShouldFlagTCAndZeroWorkday()
    {
        // Tính tay: Quên chấm ra (chỉ có 08:00), chưa giải trình -> IsMissedPunch = true, WorkdayCount = 0, mã TC.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 8),
            punches: [new(new DateTime(2026, 7, 8, 8, 0, 0), "CheckIn", "BiometricDevice")]));

        res.IsMissedPunch.Should().BeTrue();
        res.StatusCode.Should().Be("TC");
        res.WorkdayCount.Should().Be(0m);
    }

    [Fact]
    public void S19_SinglePunchWithApprovedExplanation_ShouldCreditWorkdayX()
    {
        // Tính tay: Quên chấm ra nhưng có giải trình 08:00–17:00 ĐÃ DUYỆT -> HasApprovedExplanation = true, WorkingHours = 8.0h, 1.0 công X.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 8),
            punches: [new(new DateTime(2026, 7, 8, 8, 0, 0), "CheckIn", "BiometricDevice")],
            explanation: new AttendanceApprovedExplanationInput(new TimeOnly(8, 0), new TimeOnly(17, 0), "Quên quẹt thẻ chiều")));

        res.IsMissedPunch.Should().BeFalse();
        res.HasApprovedExplanation.Should().BeTrue();
        res.StatusCode.Should().Be("X");
        res.WorkingHours.Should().Be(8.00m);
        res.WorkdayCount.Should().Be(1.00m);
    }

    [Fact]
    public void S20_NoPunchNoLeave_ShouldBeUnexcusedAbsentKL()
    {
        // Tính tay: Ngày làm việc 09/07 không punch, không đơn -> vắng không phép KL, UnexcusedAbsentDays = 1.0, WorkdayCount = 0.
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(new DateOnly(2026, 7, 9)));

        res.StatusCode.Should().Be("KL");
        res.UnexcusedAbsentDays.Should().Be(1.00m);
        res.WorkdayCount.Should().Be(0m);
    }

    [Theory]
    [InlineData("SICK", "Ô", false, true, "Ô", 0, 1, 0)]
    [InlineData("MATERNITY", "TS", false, true, "TS", 0, 1, 0)]
    [InlineData("MARRIAGE", "R", true, false, "R", 1, 0, 0)]
    [InlineData("UNPAID", "Ro", false, false, "Ro", 0, 0, 1)]
    public void S21_LeaveTypes_ShouldMapConfiguredSymbolsAndBucketsAccurately(
        string leaveTypeCode, string rawSymbol, bool paidCo, bool paidSi,
        string expectedSymbol, decimal expPaid, decimal expSi, decimal expUnpaid)
    {
        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 10),
            leaves: [new AttendanceApprovedLeaveInput(leaveTypeCode, rawSymbol, "FullDay", 1.0m, paidCo, paidSi)]));

        res.StatusCode.Should().Be(expectedSymbol);
        res.PaidLeaveDays.Should().Be(expPaid);
        res.SocialInsuranceLeaveDays.Should().Be(expSi);
        res.UnpaidLeaveDays.Should().Be(expUnpaid);
        res.WorkdayCount.Should().Be(0m);
    }

    [Fact]
    public void S22_CustomConfiguredSymbolCatalog_ShouldOverrideDefaultCodes()
    {
        // Cấu hình bảng ký hiệu tùy biến: đổi Work ("X" -> "ĐL"), MissedPunch ("TC" -> "QCC").
        var customCatalog = new AttendanceSymbolCatalog(new AttendanceSymbolDefinition[]
        {
            new() { SymbolTypeCode = "Work", SymbolCode = "ĐL", SymbolName = "Đi làm", Category = "Work", WorkdayMultiplier = 1.0m, IsActive = true },
            new() { SymbolTypeCode = "MissedPunch", SymbolCode = "QCC", SymbolName = "Quên chấm", Category = "Missing", WorkdayMultiplier = 0m, IsActive = true }
        });

        var workRes = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 13),
            punches:
            [
                new(new DateTime(2026, 7, 13, 8, 0, 0), "CheckIn", "Web"),
                new(new DateTime(2026, 7, 13, 17, 0, 0), "CheckOut", "Web")
            ],
            symbols: customCatalog));

        workRes.StatusCode.Should().Be("ĐL");

        var missedRes = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 14),
            punches: [new(new DateTime(2026, 7, 14, 8, 0, 0), "CheckIn", "Web")],
            symbols: customCatalog));

        missedRes.StatusCode.Should().Be("QCC");
    }

    [Fact]
    public void S23_SplitShift_ShouldExcludeUnpaidGapAndCount8Hours()
    {
        // Tính tay: Ca gãy 08:00–12:00 và 14:00–18:00. Chấm 08:00–18:00 -> chỉ tính 4h sáng + 4h chiều = 8.0h.
        var splitShift = new WorkShift
        {
            Id = 6,
            ShiftCode = "CA_GAY",
            ShiftType = "Split",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0),
            SplitStartTime2 = new TimeOnly(14, 0),
            SplitEndTime2 = new TimeOnly(18, 0),
            StandardHours = 8.0m,
            WorkdayValue = 1.0m
        };

        var res = AttendanceCalculationEngine.CalculateDaily(BuildInput(
            new DateOnly(2026, 7, 15),
            shift: splitShift,
            punches:
            [
                new(new DateTime(2026, 7, 15, 8, 0, 0), "CheckIn", "BiometricDevice"),
                new(new DateTime(2026, 7, 15, 18, 0, 0), "CheckOut", "BiometricDevice")
            ]));

        res.WorkingHours.Should().Be(8.00m);
        res.WorkdayCount.Should().Be(1.00m);
        res.StatusCode.Should().Be("X");
    }

    // ========================================================================
    // KIỂM THỬ TÍCH HỢP CL3: CHỐT CÔNG 3 BƯỚC, ĐIỀU CHỈNH KỲ SAU, RECALC JOB & EXCEL
    // ========================================================================

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);

        db.AttendanceStatutoryParameters.Add(CreateStatutory());
        var policyGroup = new PolicyGroup { PolicyCode = "DEFAULT_CO", PolicyName = "Mặc định", IsCompanyDefault = true, IsActive = true };
        policyGroup.AttendancePolicies.Add(CreateDefaultPolicy());
        db.PolicyGroups.Add(policyGroup);
        db.AttendanceSymbolDefinitions.AddRange(ApplicationDbContextInitializer.BuildDefaultAttendanceSymbols());
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task ThreeStepClosingWorkflow_ShouldEnforceOrderAndReviewWindow_AndPostLockShouldRequireAdjustmentTicket()
    {
        using var db = CreateDb();
        var unit = new OrganizationUnit { UnitCode = "NM2", UnitName = "Nhà máy 2", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "NV", PositionName = "Nhân viên" };
        db.AddRange(unit, pos);
        await db.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "VNM301", FullName = "Trần Chốt Công", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Official" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = emp.Id,
            Month = 7,
            Year = 2026,
            ActualWorkdays = 22m,
            TotalPayableWorkdays = 22m,
            ClosingStatus = "Open",
            IsLocked = false
        });
        await db.SaveChangesAsync();

        var unitHr = new AdvanceAttendanceClosingCommandHandler(db, new MockCurrentUser(1, "hr.unit"));
        var manager = new AdvanceAttendanceClosingCommandHandler(db, new MockCurrentUser(2, "manager.nm2"));
        var groupHr = new AdvanceAttendanceClosingCommandHandler(db, new MockCurrentUser(3, "hr.director"));

        // Chưa chốt bước 1 thì bước 2 phải lỗi
        Func<Task> confirmBeforeSubmit = () => manager.Handle(new AdvanceAttendanceClosingCommand(7, 2026, "ManagerConfirm"), CancellationToken.None);
        await confirmBeforeSubmit.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*HR đơn vị chốt bước 1*");

        // Bước 1: HR đơn vị chốt -> mở hạn 3 ngày xem & giải trình
        var t0 = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        await unitHr.Handle(new AdvanceAttendanceClosingCommand(7, 2026, "UnitSubmit", OverrideUtcNow: t0), CancellationToken.None);

        // Bước 2: Quản lý xác nhận
        await manager.Handle(new AdvanceAttendanceClosingCommand(7, 2026, "ManagerConfirm", OverrideUtcNow: t0.AddHours(4)), CancellationToken.None);

        // Bước 3: Thử khóa sớm TRƯỚC 3 ngày xem/giải trình (t0 + 1 ngày) mà không có cờ force -> bị chặn
        Func<Task> earlyLock = () => groupHr.Handle(new AdvanceAttendanceClosingCommand(7, 2026, "FinalLock", ForceLockBeforeReviewDeadline: false, OverrideUtcNow: t0.AddDays(1)), CancellationToken.None);
        await earlyLock.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*thời hạn 3 ngày*");

        // Sau hạn 3 ngày (t0 + 4 ngày) -> HR tổng khóa cứng
        await groupHr.Handle(new AdvanceAttendanceClosingCommand(7, 2026, "FinalLock", OverrideUtcNow: t0.AddDays(4)), CancellationToken.None);

        var summary = await db.MonthlyAttendanceSummaries.SingleAsync(m => m.EmployeeId == emp.Id);
        summary.IsLocked.Should().BeTrue();
        summary.ClosingStatus.Should().Be("Locked");

        // Đã khóa -> tạo điều chỉnh kỳ sau áp dụng cho tháng 08/2026
        var postLockHandler = new CreatePostLockAdjustmentCommandHandler(db, new MockCurrentUser(3, "hr.director"));
        var adjId = await postLockHandler.Handle(new CreatePostLockAdjustmentCommand(
            emp.Id, 7, 2026, new DateOnly(2026, 7, 20), 8, 2026,
            "WorkdayDelta", 1.0m, 0m, 0m, 0m, 0m, "Bổ sung 1 công đi công tác sót chốt tháng 7"), CancellationToken.None);
        adjId.Should().BeGreaterThan(0);

        var decideHandler = new DecidePostLockAdjustmentCommandHandler(db, new MockCurrentUser(3, "hr.director"));
        await decideHandler.Handle(new DecidePostLockAdjustmentCommand(adjId, true, "Đã kiểm tra vé công tác"), CancellationToken.None);

        var savedAdj = await db.PostLockAttendanceAdjustments.SingleAsync(a => a.Id == adjId);
        savedAdj.Status.Should().Be("Approved");
        savedAdj.AppliedPayrollMonth.Should().Be(8);
    }

    [Fact]
    public async Task BackgroundRecalculationAndExcelExport_ShouldTrackProgressAndIncludeSummaryAndSignatures()
    {
        using var db = CreateDb();
        var unit = new OrganizationUnit { UnitCode = "VP", UnitName = "Văn phòng HCM", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "CV", PositionName = "Chuyên viên" };
        db.AddRange(unit, pos);
        await db.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "VNM302", FullName = "Lê Tính Công", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Official" };
        var hc = CreateDayShift();
        hc.Id = 0;
        db.AddRange(emp, hc);
        await db.SaveChangesAsync();

        // Ngày 01/07 đi làm đủ công X; ngày 02/07 sáng phép năm P chiều đi làm X/2 -> Tổng Payable = 1.0 + 1.0 = 2.0
        db.AttendanceLogs.AddRange(
            new AttendanceLog { EmployeeId = emp.Id, PunchTime = new DateTime(2026, 7, 1, 8, 0, 0), PunchType = "CheckIn", SourceType = "BiometricDevice" },
            new AttendanceLog { EmployeeId = emp.Id, PunchTime = new DateTime(2026, 7, 1, 17, 0, 0), PunchType = "CheckOut", SourceType = "BiometricDevice" },
            new AttendanceLog { EmployeeId = emp.Id, PunchTime = new DateTime(2026, 7, 2, 13, 0, 0), PunchType = "CheckIn", SourceType = "BiometricDevice" },
            new AttendanceLog { EmployeeId = emp.Id, PunchTime = new DateTime(2026, 7, 2, 17, 0, 0), PunchType = "CheckOut", SourceType = "BiometricDevice" });

        var annualLeave = new LeaveType { LeaveTypeCode = "ANNUAL", LeaveTypeName = "Phép năm", IsPaidByCompany = true, AttendanceSymbol = "P" };
        db.LeaveTypes.Add(annualLeave);
        await db.SaveChangesAsync();

        db.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = emp.Id,
            LeaveTypeId = annualLeave.Id,
            StartDate = new DateOnly(2026, 7, 2),
            EndDate = new DateOnly(2026, 7, 2),
            StartSession = "Morning",
            EndSession = "Morning",
            TotalDays = 0.5m,
            Status = "Approved",
            Reason = "Việc gia đình buổi sáng"
        });
        await db.SaveChangesAsync();

        var recalcService = new AttendanceRecalculationService(db);
        var dispatcher = new InProcessAttendanceRecalculationDispatcher(db, recalcService);
        var startHandler = new StartAttendanceRecalculationCommandHandler(dispatcher, new MockCurrentUser(5, "hr.payroll"));

        var batchId = await startHandler.Handle(new StartAttendanceRecalculationCommand(
            7, 2026, unit.Id, emp.Id, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2)), CancellationToken.None);

        var statusHandler = new GetRecalculationBatchStatusQueryHandler(db);
        var status = await statusHandler.Handle(new GetRecalculationBatchStatusQuery(batchId), CancellationToken.None);
        status.Should().NotBeNull();
        status!.Status.Should().Be("Completed");
        status.ProgressPercent.Should().Be(100);
        status.ProcessedEmployees.Should().Be(1);

        var summary = await db.MonthlyAttendanceSummaries.SingleAsync(m => m.EmployeeId == emp.Id && m.Month == 7 && m.Year == 2026);
        summary.ActualWorkdays.Should().Be(1.5m);
        summary.PaidLeaveDays.Should().Be(0.5m);
        summary.TotalPayableWorkdays.Should().Be(2.0m);

        // Kiểm tra Click ô chi tiết (CellDetails)
        var cellHandler = new GetAttendanceCellDetailsQueryHandler(db);
        var detailDay2 = await cellHandler.Handle(new GetAttendanceCellDetailsQuery(emp.Id, new DateOnly(2026, 7, 2)), CancellationToken.None);
        detailDay2.Should().NotBeNull();
        detailDay2!.StatusCode.Should().Be("½+P");
        detailDay2.Logs.Should().HaveCount(4);
        detailDay2.RelatedRequests.Should().ContainSingle(r => r.Contains("Phép năm"));

        // Kiểm tra xuất Excel có ký hiệu, tổng hợp & 3 chữ ký
        var boardHandler = new GetMonthlyAttendanceBoardQueryHandler(db);
        var board = await boardHandler.Handle(new GetMonthlyAttendanceBoardQuery(7, 2026, unit.Id, null), CancellationToken.None);
        var docService = new TimeAttendanceDocumentService(db);
        var excelBytes = docService.ExportMonthlyTimesheetToExcel(board);

        using var ms = new MemoryStream(excelBytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheet(1);
        var allText = string.Join(" ", ws.CellsUsed().Select(c => c.GetString()));
        allText.Should().Contain("NGƯỜI LẬP BIỂU / HR ĐƠN VỊ");
        allText.Should().Contain("TRƯỞNG BỘ PHẬN XÁC NHẬN");
        allText.Should().Contain("HR TỔNG HỢP / KHÓA CÔNG");
        allText.Should().Contain("Quên chấm");
        allText.Should().Contain("OT 300% Đêm");
    }
}
