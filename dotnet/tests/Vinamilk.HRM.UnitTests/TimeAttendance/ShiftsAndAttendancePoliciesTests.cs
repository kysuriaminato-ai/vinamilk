using System.Runtime.CompilerServices;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Xunit;

namespace Vinamilk.HRM.UnitTests.TimeAttendance;

public class ShiftsAndAttendancePoliciesTests
{
    private sealed class MockCurrentUser(int userId = 1, string username = "hr.lead") : ICurrentUserService
    {
        public int? UserId => userId;
        public string Username => username;
        public int? EmployeeId => null;
        public string? IpAddress => "10.0.0.5";
        public string? UserAgent => "UnitTest";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permissionCode) => true;
    }

    private sealed class StubSender(Func<IBaseRequest, int> handler) : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult((TResponse)(object)handler(request));
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult<object?>(handler((IBaseRequest)request));
        public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
        public async IAsyncEnumerable<object?> CreateStream(object request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private static ApplicationDbContext CreateDbWithStatutoryDefaults()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);

        db.AttendanceStatutoryParameters.Add(new AttendanceStatutoryParameter
        {
            EffectiveFrom = new DateOnly(2021, 1, 1),
            EffectiveTo = null,
            LegalReference = "BLLD 45/2019/QH14",
            NeedsHrVerification = true,
            NightWorkStartTime = new TimeOnly(22, 0),
            NightWorkEndTime = new TimeOnly(6, 0),
            MinInterShiftRestHours = 12m,
            MaxNormalDailyHours = 8m,
            MaxNormalWeeklyHours = 48m,
            MaxMonthlyOvertimeHours = 40m,
            MaxYearlyOvertimeHours = 200m,
            WeekdayOtMultiplier = 1.5m,
            WeeklyDayOffOtMultiplier = 2.0m,
            HolidayOtMultiplier = 3.0m,
            NightShiftBonusRate = 0.30m
        });

        var defaultGroup = new PolicyGroup
        {
            PolicyCode = "DEFAULT_CO",
            PolicyName = "Mặc định công ty",
            IsCompanyDefault = true,
            IsActive = true
        };
        defaultGroup.AttendancePolicies.Add(new AttendancePolicy
        {
            GraceLateMinutes = 5,
            GraceEarlyMinutes = 5,
            RoundingIntervalMinutes = 5,
            RoundingMode = "Nearest",
            HalfDayLateThresholdMinutes = 60,
            ZeroDayLateThresholdMinutes = 240,
            MaxMissedPunchExplanationsPerMonth = 3,
            MissedPunchOverQuotaAction = "FixedFine",
            MissedPunchPenaltyAmount = 100000m,
            WeeklyWorkdaysMask = "Mon,Tue,Wed,Thu,Fri"
        });
        db.PolicyGroups.Add(defaultGroup);
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task OvernightShift_ShouldAttributeAttendanceToShiftStartDate_AndCalculate8NightHours()
    {
        // Tính tay: Ca đêm CA3 từ 22:00 ngày 10/05 đến 06:00 ngày 11/05.
        // NV check-in lúc 2026-05-10 22:00 và check-out lúc 2026-05-11 06:00.
        // Toàn bộ 8 giờ nằm trong khung đêm [22:00..06:00], ngày công phải gán vào 2026-05-10 (không tách sang 11/05).
        using var db = CreateDbWithStatutoryDefaults();
        var unit = new OrganizationUnit { UnitCode = "NM1", UnitName = "Nhà máy 1", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "CNVH", PositionName = "Công nhân vận hành" };
        db.AddRange(unit, pos);
        await db.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "VNM901", FullName = "Nguyễn Ca Đêm", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Official" };
        var shiftCa3 = new WorkShift
        {
            ShiftCode = "CA3",
            ShiftName = "Ca 3 Đêm",
            ShiftType = "Overnight",
            StartTime = new TimeOnly(22, 0),
            EndTime = new TimeOnly(6, 0),
            IsOvernight = true,
            StandardHours = 8m,
            WorkdayValue = 1m,
            NightShiftAllowanceRate = 0.30m
        };
        db.AddRange(emp, shiftCa3);
        await db.SaveChangesAsync();

        db.ShiftSchedules.Add(new ShiftSchedule
        {
            EmployeeId = emp.Id,
            WorkShiftId = shiftCa3.Id,
            WorkDate = new DateOnly(2026, 5, 10),
            IsDayOff = false
        });
        await db.SaveChangesAsync();

        var handler = new WebCheckInCommandHandler(db);
        await handler.Handle(new WebCheckInCommand(emp.Id, "CheckIn", "MobileGps", 10.728, 106.721, null, new DateTime(2026, 5, 10, 22, 0, 0)), CancellationToken.None);
        await handler.Handle(new WebCheckInCommand(emp.Id, "CheckOut", "MobileGps", 10.728, 106.721, null, new DateTime(2026, 5, 11, 6, 0, 0)), CancellationToken.None);

        var attendances = await db.DailyAttendances.Where(d => d.EmployeeId == emp.Id).ToListAsync();
        attendances.Should().ContainSingle();
        attendances[0].AttendanceDate.Should().Be(new DateOnly(2026, 5, 10));
        attendances[0].WorkingHours.Should().Be(8.0m);
        attendances[0].NightShiftHours.Should().Be(8.0m);
        attendances[0].WorkdayCount.Should().Be(1.0m);
        attendances[0].LateMinutes.Should().Be(0);
    }

    [Fact]
    public async Task PolicyGroupPrecedence_ShouldResolve_EmployeeOverPositionOverUnitHierarchyOverDefault()
    {
        using var db = CreateDbWithStatutoryDefaults();
        var parentUnit = new OrganizationUnit { UnitCode = "KHOI_SX", UnitName = "Khối Sản Xuất", UnitLevel = 1, TreePath = "/1/" };
        db.OrganizationUnits.Add(parentUnit);
        await db.SaveChangesAsync();

        var childUnit = new OrganizationUnit { UnitCode = "PX_SUA", UnitName = "Phân xưởng Sữa", UnitLevel = 2, ParentUnitId = parentUnit.Id, TreePath = $"/1/{parentUnit.Id}/" };
        var otherUnit = new OrganizationUnit { UnitCode = "KETOAN", UnitName = "Kế toán", UnitLevel = 1, TreePath = "/99/" };
        var posTarget = new Position { PositionCode = "TO_TRUONG", PositionName = "Tổ trưởng" };
        var posOther = new Position { PositionCode = "NV", PositionName = "Nhân viên" };
        db.AddRange(childUnit, otherUnit, posTarget, posOther);
        await db.SaveChangesAsync();

        var eEmp = new Employee { EmployeeCode = "E01", FullName = "NV Gán Trực Tiếp", OrganizationUnitId = childUnit.Id, PositionId = posTarget.Id, WorkStatus = "Official" };
        var ePos = new Employee { EmployeeCode = "E02", FullName = "NV Gán Chức Danh", OrganizationUnitId = childUnit.Id, PositionId = posTarget.Id, WorkStatus = "Official" };
        var eUnit = new Employee { EmployeeCode = "E03", FullName = "NV Kế Thừa Đơn Vị Cha", OrganizationUnitId = childUnit.Id, PositionId = posOther.Id, WorkStatus = "Official" };
        var eDef = new Employee { EmployeeCode = "E04", FullName = "NV Mặc Định", OrganizationUnitId = otherUnit.Id, PositionId = posOther.Id, WorkStatus = "Official" };
        db.AddRange(eEmp, ePos, eUnit, eDef);
        await db.SaveChangesAsync();

        var gUnit = new PolicyGroup { PolicyCode = "POL_UNIT", PolicyName = "Chính sách Khối SX" };
        gUnit.AttendancePolicies.Add(new AttendancePolicy { GraceLateMinutes = 10 });
        var gPos = new PolicyGroup { PolicyCode = "POL_POS", PolicyName = "Chính sách Tổ trưởng" };
        gPos.AttendancePolicies.Add(new AttendancePolicy { GraceLateMinutes = 15 });
        var gEmp = new PolicyGroup { PolicyCode = "POL_EMP", PolicyName = "Chính sách Đặc thù NV" };
        gEmp.AttendancePolicies.Add(new AttendancePolicy { GraceLateMinutes = 30 });
        db.AddRange(gUnit, gPos, gEmp);
        await db.SaveChangesAsync();

        db.PolicyGroupAssignments.AddRange(
            new PolicyGroupAssignment { PolicyGroupId = gUnit.Id, OrganizationUnitId = parentUnit.Id, IsActive = true },
            new PolicyGroupAssignment { PolicyGroupId = gPos.Id, PositionId = posTarget.Id, IsActive = true },
            new PolicyGroupAssignment { PolicyGroupId = gEmp.Id, EmployeeId = eEmp.Id, IsActive = true });
        await db.SaveChangesAsync();

        var testDate = new DateOnly(2026, 5, 10);
        var r1 = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(db, eEmp.Id, testDate, CancellationToken.None);
        var r2 = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(db, ePos.Id, testDate, CancellationToken.None);
        var r3 = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(db, eUnit.Id, testDate, CancellationToken.None);
        var r4 = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(db, eDef.Id, testDate, CancellationToken.None);

        r1.ResolutionTier.Should().Be("Employee");
        r1.PolicyCode.Should().Be("POL_EMP");
        r1.Policy.GraceLateMinutes.Should().Be(30);

        r2.ResolutionTier.Should().Be("Position");
        r2.PolicyCode.Should().Be("POL_POS");
        r2.Policy.GraceLateMinutes.Should().Be(15);

        r3.ResolutionTier.Should().Be("OrganizationUnit");
        r3.PolicyCode.Should().Be("POL_UNIT");
        r3.Policy.GraceLateMinutes.Should().Be(10);

        r4.ResolutionTier.Should().Be("CompanyDefault");
        r4.PolicyCode.Should().Be("DEFAULT_CO");
        r4.Policy.GraceLateMinutes.Should().Be(5);
    }

    [Theory]
    // Tính tay: 13 phút với bước 5 -> Ceiling=15, Floor=10, Nearest=15; bước 10 -> Ceiling=20, Floor=10, Nearest=10; bước 15 -> Ceiling=15, Floor=0, Nearest=15
    [InlineData(13, 5, "Ceiling", 15)]
    [InlineData(13, 5, "Floor", 10)]
    [InlineData(13, 5, "Nearest", 15)]
    [InlineData(13, 10, "Ceiling", 20)]
    [InlineData(13, 10, "Floor", 10)]
    [InlineData(13, 10, "Nearest", 10)]
    [InlineData(13, 15, "Ceiling", 15)]
    [InlineData(13, 15, "Floor", 0)]
    [InlineData(13, 15, "Nearest", 15)]
    [InlineData(13, 5, "None", 13)]
    public void RoundMinutes_ShouldFollowConfiguredBlockAndMode(int rawMinutes, int interval, string mode, int expected)
    {
        AttendancePolicyAndShiftEngine.RoundMinutes(rawMinutes, interval, mode).Should().Be(expected);
    }

    [Fact]
    public void LateThresholdsAndShiftTypes_ShouldCalculateExpectedHoursAndWorkdayValue()
    {
        var statutory = new AttendanceStatutoryParameter
        {
            EffectiveFrom = new DateOnly(2021, 1, 1),
            LegalReference = "BLLD 2019",
            NightWorkStartTime = new TimeOnly(22, 0),
            NightWorkEndTime = new TimeOnly(6, 0)
        };
        var policy = new AttendancePolicy
        {
            GraceLateMinutes = 5,
            GraceEarlyMinutes = 5,
            RoundingIntervalMinutes = 5,
            RoundingMode = "Nearest",
            HalfDayLateThresholdMinutes = 60,
            ZeroDayLateThresholdMinutes = 240
        };
        var workDate = new DateOnly(2026, 5, 11);

        // 1. Ca Fixed HC (08:00-17:00, nghỉ 12:00-13:00 không lương), đi trễ 75p (09:15) -> 60 < 75 <= 240 -> 0.5 công ("X/2")
        var fixedShift = new WorkShift
        {
            Id = 1, ShiftCode = "HC", ShiftType = "Fixed",
            StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0),
            BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 0), IsBreakPaid = false,
            StandardHours = 8m, WorkdayValue = 1m
        };
        var halfDay = AttendancePolicyAndShiftEngine.CalculatePunchAttendance(
            workDate, fixedShift, new DateTime(2026, 5, 11, 9, 15, 0), new DateTime(2026, 5, 11, 17, 0, 0), policy, statutory);
        halfDay.LateMinutes.Should().Be(75);
        halfDay.WorkdayCount.Should().Be(0.5m);
        halfDay.AttendanceSymbol.Should().Be("X/2");

        // 2. Đi trễ 250p (12:10) -> 250 > 240 -> 0 công ("V")
        var zeroDay = AttendancePolicyAndShiftEngine.CalculatePunchAttendance(
            workDate, fixedShift, new DateTime(2026, 5, 11, 12, 10, 0), new DateTime(2026, 5, 11, 17, 0, 0), policy, statutory);
        zeroDay.WorkdayCount.Should().Be(0m);
        zeroDay.AttendanceSymbol.Should().Be("V");

        // 3. Ca Gãy Split (08:00-12:00 & 14:00-18:00): check-in 08:00, check-out 18:00 -> bỏ 2h nghỉ trưa -> 8.0h và 1.0 công
        var splitShift = new WorkShift
        {
            Id = 2, ShiftCode = "SPLIT", ShiftType = "Split",
            StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0),
            SplitStartTime2 = new TimeOnly(14, 0), SplitEndTime2 = new TimeOnly(18, 0),
            StandardHours = 8m, WorkdayValue = 1m
        };
        var splitRes = AttendancePolicyAndShiftEngine.CalculatePunchAttendance(
            workDate, splitShift, new DateTime(2026, 5, 11, 8, 0, 0), new DateTime(2026, 5, 11, 18, 0, 0), policy, statutory);
        splitRes.WorkingHours.Should().Be(8.0m);
        splitRes.WorkdayCount.Should().Be(1.0m);

        // 4. Ca Linh hoạt Flexible (Core 09:30-15:30, chuẩn 8h): vào 09:42 (trễ core 12p -> nearest 10p), ra 18:45 (đủ 8h sau trừ trưa 12:00-13:00)
        var flexShift = new WorkShift
        {
            Id = 3, ShiftCode = "FLEX", ShiftType = "Flexible",
            StartTime = new TimeOnly(7, 30), EndTime = new TimeOnly(18, 30),
            CoreStartTime = new TimeOnly(9, 30), CoreEndTime = new TimeOnly(15, 30),
            BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 0),
            StandardHours = 8m, WorkdayValue = 1m
        };
        var flexRes = AttendancePolicyAndShiftEngine.CalculatePunchAttendance(
            workDate, flexShift, new DateTime(2026, 5, 11, 9, 42, 0), new DateTime(2026, 5, 11, 18, 45, 0), policy, statutory);
        flexRes.LateMinutes.Should().Be(10);
        flexRes.WorkingHours.Should().Be(8.05m);
        flexRes.WorkdayCount.Should().Be(1.0m);

        // 5. Ca Theo giờ Hourly (chuẩn 4h = 0.5 công): làm 2h -> 0.25 công
        var hourlyShift = new WorkShift
        {
            Id = 4, ShiftCode = "HR", ShiftType = "Hourly",
            StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(22, 0),
            StandardHours = 4m, WorkdayValue = 0.5m
        };
        var hourlyRes = AttendancePolicyAndShiftEngine.CalculatePunchAttendance(
            workDate, hourlyShift, new DateTime(2026, 5, 11, 10, 0, 0), new DateTime(2026, 5, 11, 12, 0, 0), policy, statutory);
        hourlyRes.WorkingHours.Should().Be(2.0m);
        hourlyRes.WorkdayCount.Should().Be(0.25m);
    }

    [Fact]
    public async Task MissedPunchExplanationQuota_ShouldTrackOverQuotaAndApplyFineOrHalfDay()
    {
        using var db = CreateDbWithStatutoryDefaults();
        var unit = new OrganizationUnit { UnitCode = "VP", UnitName = "Văn phòng", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "NV", PositionName = "Chuyên viên" };
        db.AddRange(unit, pos);
        await db.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "VNM701", FullName = "Lê Hay Quên", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Official" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        // Cấu hình quota = 2 lần/tháng, vượt phạt 150.000đ/lần
        var pg = await db.PolicyGroups.Include(g => g.AttendancePolicies).FirstAsync(g => g.IsCompanyDefault);
        pg.AttendancePolicies.First().MaxMissedPunchExplanationsPerMonth = 2;
        pg.AttendancePolicies.First().MissedPunchOverQuotaAction = "FixedFine";
        pg.AttendancePolicies.First().MissedPunchPenaltyAmount = 150000m;
        await db.SaveChangesAsync();

        var sender = new StubSender(_ => 555);
        var explainHandler = new SubmitAttendanceExplanationCommandHandler(db, sender);

        await explainHandler.Handle(new SubmitAttendanceExplanationCommand(emp.Id, new DateOnly(2026, 5, 4), new TimeOnly(8, 0), new TimeOnly(17, 0), "Quên 1"), CancellationToken.None);
        await explainHandler.Handle(new SubmitAttendanceExplanationCommand(emp.Id, new DateOnly(2026, 5, 5), new TimeOnly(8, 0), new TimeOnly(17, 0), "Quên 2"), CancellationToken.None);
        var summaryBeforeOver = await db.MonthlyAttendanceSummaries.FirstAsync(m => m.EmployeeId == emp.Id && m.Month == 5 && m.Year == 2026);
        summaryBeforeOver.MissedPunchOverQuotaCount.Should().Be(0);
        summaryBeforeOver.MissedPunchPenaltyAmount.Should().Be(0m);

        // Lần thứ 3 vượt quota (3 - 2 = 1 lần vượt -> phạt 150.000đ)
        await explainHandler.Handle(new SubmitAttendanceExplanationCommand(emp.Id, new DateOnly(2026, 5, 6), new TimeOnly(8, 0), new TimeOnly(17, 0), "Quên 3"), CancellationToken.None);
        var summaryAfterOver = await db.MonthlyAttendanceSummaries.FirstAsync(m => m.EmployeeId == emp.Id && m.Month == 5 && m.Year == 2026);
        summaryAfterOver.MissedPunchOverQuotaCount.Should().Be(1);
        summaryAfterOver.MissedPunchPenaltyAmount.Should().Be(150000m);
    }

    [Fact]
    public async Task ScheduleConflicts_ShouldDetectOverlap_InsufficientRest_AndWeeklyMaxHours()
    {
        using var db = CreateDbWithStatutoryDefaults();
        var unit = new OrganizationUnit { UnitCode = "SX", UnitName = "Xưởng", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "CN", PositionName = "Công nhân" };
        db.AddRange(unit, pos);
        await db.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "VNM801", FullName = "Trần Xung Đột", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Official" };
        var ca1 = new WorkShift { ShiftCode = "CA1", ShiftName = "Ca 1", ShiftType = "Fixed", StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(14, 0), StandardHours = 8m };
        var ca2 = new WorkShift { ShiftCode = "CA2", ShiftName = "Ca 2", ShiftType = "Fixed", StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(20, 0), StandardHours = 8m };
        var ca3 = new WorkShift { ShiftCode = "CA3", ShiftName = "Ca 3", ShiftType = "Overnight", StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0), IsOvernight = true, StandardHours = 8m };
        db.AddRange(emp, ca1, ca2, ca3);
        await db.SaveChangesAsync();

        var handler = new AssignShiftScheduleCommandHandler(db);

        // Xếp cả tuần Mon..Sun (7 ngày × 8h = 56h > 48h), với ngày 12/05 gán CA3 (kết thúc 06:00 ngày 13/05) rồi ngày 13/05 gán CA1 (bắt đầu 06:00 -> nghỉ 0h < 12h),
        // đồng thời ngày 14/05 gán cả CA1 và CA2 (12:00 trùng vào 06:00-14:00) với ReplaceExisting = false
        await handler.Handle(new AssignShiftScheduleDetailedCommand(emp.Id, null, new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 17), ca1.Id), CancellationToken.None);
        await handler.Handle(new AssignShiftScheduleDetailedCommand(emp.Id, null, new DateOnly(2026, 5, 12), new DateOnly(2026, 5, 12), ca3.Id), CancellationToken.None);
        var result = await handler.Handle(new AssignShiftScheduleDetailedCommand(emp.Id, null, new DateOnly(2026, 5, 14), new DateOnly(2026, 5, 14), ca2.Id, ReplaceExisting: false), CancellationToken.None);

        result.Warnings.Should().Contain(w => w.ConflictType == "InsufficientRest");
        result.Warnings.Should().Contain(w => w.ConflictType == "Overlap");
        result.Warnings.Should().Contain(w => w.ConflictType == "WeeklyHoursExceeded");
    }

    [Fact]
    public async Task RotationAndShiftSwapAndCompensatoryHoliday_ShouldWorkEndToEndWithAuditLog()
    {
        using var db = CreateDbWithStatutoryDefaults();
        var unit = new OrganizationUnit { UnitCode = "NM_SG", UnitName = "Nhà máy SG", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "CN", PositionName = "Công nhân" };
        db.AddRange(unit, pos);
        await db.SaveChangesAsync();

        var e1 = new Employee { EmployeeCode = "K01", FullName = "Kíp 1", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Official" };
        var e2 = new Employee { EmployeeCode = "K02", FullName = "Kíp 2", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Official" };
        var e3 = new Employee { EmployeeCode = "K03", FullName = "Kíp 3", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Official" };
        var e4 = new Employee { EmployeeCode = "K04", FullName = "Kíp 4", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Official" };
        var ca1 = new WorkShift { ShiftCode = "CA1", ShiftName = "Ca 1", ShiftType = "Fixed", StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(14, 0), StandardHours = 8m };
        var ca2 = new WorkShift { ShiftCode = "CA2", ShiftName = "Ca 2", ShiftType = "Fixed", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(22, 0), StandardHours = 8m };
        var ca3 = new WorkShift { ShiftCode = "CA3", ShiftName = "Ca 3", ShiftType = "Overnight", StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0), IsOvernight = true, StandardHours = 8m };
        var pattern = new ShiftRotationPattern
        {
            PatternCode = "3CA_4KIP",
            PatternName = "3 ca 4 kíp",
            CycleDays = 8,
            CrewCount = 4,
            CrewOffsetDays = 2,
            ShiftSequenceCsv = "CA1,CA1,CA2,CA2,CA3,CA3,OFF,OFF"
        };
        db.AddRange(e1, e2, e3, e4, ca1, ca2, ca3, pattern);
        await db.SaveChangesAsync();

        // 1. Sinh lịch xoay ca 3 ca 4 kíp trong 2 tuần (14 ngày × 4 NV = 56 lượt)
        var rotResult = await new GenerateRotationScheduleCommandHandler(db).Handle(
            new GenerateRotationScheduleCommand(pattern.Id, new DateOnly(2026, 6, 1), 2, unit.Id), CancellationToken.None);
        rotResult.AffectedCount.Should().Be(56);

        var day0Schedules = await db.ShiftSchedules.Include(s => s.WorkShift)
            .Where(s => s.WorkDate == new DateOnly(2026, 6, 1))
            .ToDictionaryAsync(s => s.EmployeeId);
        day0Schedules[e1.Id].WorkShift.ShiftCode.Should().Be("CA1");
        day0Schedules[e2.Id].WorkShift.ShiftCode.Should().Be("CA2");
        day0Schedules[e3.Id].WorkShift.ShiftCode.Should().Be("CA3");
        day0Schedules[e4.Id].IsDayOff.Should().BeTrue();

        // 2. Đổi ca ngày 01/06 giữa e1 (CA1) và e2 (CA2) + duyệt ghi AuditLog
        var actor = new MockCurrentUser(7, "manager.sx");
        var swapId = await new SubmitShiftSwapRequestCommandHandler(db, actor).Handle(
            new SubmitShiftSwapRequestCommand(e1.Id, e2.Id, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1), "Đổi ca bận việc gia đình"),
            CancellationToken.None);
        await new ProcessShiftSwapRequestCommandHandler(db, actor).Handle(
            new ProcessShiftSwapRequestCommand(swapId, true, "Đồng ý"), CancellationToken.None);

        var swappedDay0 = await db.ShiftSchedules.Include(s => s.WorkShift)
            .Where(s => s.WorkDate == new DateOnly(2026, 6, 1))
            .ToDictionaryAsync(s => s.EmployeeId);
        swappedDay0[e1.Id].WorkShift.ShiftCode.Should().Be("CA2");
        swappedDay0[e2.Id].WorkShift.ShiftCode.Should().Be("CA1");

        var swapAudits = await db.AuditLogs.Where(a => a.EntityName == "ShiftSwapRequest" && a.EntityId == swapId.ToString()).ToListAsync();
        swapAudits.Should().HaveCount(2);
        swapAudits.Should().AllSatisfy(a => a.Username.Should().Be("manager.sx"));

        // 3. Ngày lễ rơi vào Thứ 7 (2026-08-15) -> tự sinh ngày nghỉ bù Thứ 2 (2026-08-17)
        await new SaveHolidayCommandHandler(db).Handle(
            new SaveHolidayCommand(null, new DateOnly(2026, 8, 15), "Lễ mẫu cuối tuần", true, 3.0m, AutoGenerateCompensatoryIfWeekend: true),
            CancellationToken.None);

        var compHoliday = await db.Holidays.FirstOrDefaultAsync(h => h.IsCompensatoryDay && h.OriginalHolidayDate == new DateOnly(2026, 8, 15));
        compHoliday.Should().NotBeNull();
        compHoliday!.HolidayDate.Should().Be(new DateOnly(2026, 8, 17));
        compHoliday.NeedsHrVerification.Should().BeTrue();
    }
}
