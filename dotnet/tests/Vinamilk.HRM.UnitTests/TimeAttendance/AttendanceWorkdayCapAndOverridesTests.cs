using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Interceptors;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Xunit;

namespace Vinamilk.HRM.UnitTests.TimeAttendance;

public class AttendanceWorkdayCapAndOverridesTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var interceptor = new AuditableEntityInterceptor(new FakeCurrentUserService());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AttCapTest_{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Nov2026_T2ToT6_SaturdayPunchWithoutOt_ShouldNotCountNormalWorkday_AndWithApprovedOtShouldBe200()
    {
        await using var db = CreateDbContext();
        var defaultGroup = ApplicationDbContextInitializer.BuildDefaultCompanyPolicyGroup();
        var statutory = new AttendanceStatutoryParameter
        {
            EffectiveFrom = new DateOnly(2024, 1, 1),
            LegalReference = "BLLĐ 2019"
        };
        await db.PolicyGroups.AddAsync(defaultGroup);
        await db.AttendanceStatutoryParameters.AddAsync(statutory);
        await db.AttendanceSymbolDefinitions.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultAttendanceSymbols());
        await db.SaveChangesAsync();

        var policy = defaultGroup.AttendancePolicies.First();
        var catalog = new AttendanceSymbolCatalog(await db.AttendanceSymbolDefinitions.ToListAsync());

        // Saturday 07/11/2026 for T2-T6 employee (Mon..Fri mask -> Saturday is WeeklyOff)
        var satDate = new DateOnly(2026, 11, 7);
        bool isSatWeeklyOff = !AttendancePolicyAndShiftEngine.ParseWorkdaysMask(policy.WeeklyWorkdaysMask).Contains(satDate.DayOfWeek);
        isSatWeeklyOff.Should().BeTrue("chính sách T2-T6 coi Thứ 7 là ngày nghỉ hằng tuần");

        var hcShift = new WorkShift
        {
            Id = 1,
            ShiftCode = "HC",
            ShiftName = "Hành chính",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            BreakStartTime = new TimeOnly(12, 0),
            BreakEndTime = new TimeOnly(13, 0),
            StandardHours = 8m,
            WorkdayValue = 1.0m
        };

        // 1. Punched on Saturday WITHOUT approved OT
        var noOtRes = AttendanceCalculationEngine.CalculateDaily(new AttendanceDailyCalculationInput(
            WorkDate: satDate,
            AssignedShift: hcShift,
            IsDayOffSchedule: true,
            IsWeeklyDayOff: true,
            Holiday: null,
            Punches: new[]
            {
                new AttendancePunchInput(satDate.ToDateTime(new TimeOnly(8, 2)), "CheckIn", "Machine"),
                new AttendancePunchInput(satDate.ToDateTime(new TimeOnly(12, 10)), "CheckOut", "Machine")
            },
            ApprovedLeaves: Array.Empty<AttendanceApprovedLeaveInput>(),
            ApprovedOvertimes: Array.Empty<AttendanceApprovedOvertimeInput>(),
            ApprovedExplanation: null,
            Policy: policy,
            Statutory: statutory,
            Symbols: catalog));

        noOtRes.WorkdayCount.Should().Be(0m);
        noOtRes.StatusCode.Should().Be("OFF");
        noOtRes.OvertimeHours200Day.Should().Be(0m);
        noOtRes.CalculationNotes.Should().Contain("chưa có đơn OT");

        // 2. Punched on Saturday WITH approved 4h OT -> OT 200%, still 0 normal WorkdayCount
        var withOtRes = AttendanceCalculationEngine.CalculateDaily(new AttendanceDailyCalculationInput(
            WorkDate: satDate,
            AssignedShift: hcShift,
            IsDayOffSchedule: true,
            IsWeeklyDayOff: true,
            Holiday: null,
            Punches: new[]
            {
                new AttendancePunchInput(satDate.ToDateTime(new TimeOnly(8, 0)), "CheckIn", "Machine"),
                new AttendancePunchInput(satDate.ToDateTime(new TimeOnly(12, 0)), "CheckOut", "Machine")
            },
            ApprovedLeaves: Array.Empty<AttendanceApprovedLeaveInput>(),
            ApprovedOvertimes: new[] { new AttendanceApprovedOvertimeInput(new TimeOnly(8, 0), new TimeOnly(12, 0), 4m) },
            ApprovedExplanation: null,
            Policy: policy,
            Statutory: statutory,
            Symbols: catalog));

        withOtRes.WorkdayCount.Should().Be(0m);
        withOtRes.OvertimeHours200Day.Should().Be(4m);
        withOtRes.StatusCode.Should().Be("OFF");
    }

    [Fact]
    public async Task MonthlyBoard_TotalPayableWorkdays_ShouldCapAtStandardWorkdays_UnlessPolicyAllowsExceed()
    {
        await using var db = CreateDbContext();
        var defaultGroup = ApplicationDbContextInitializer.BuildDefaultCompanyPolicyGroup();
        var policy = defaultGroup.AttendancePolicies.First();
        policy.AllowPayableWorkdaysExceedStandard.Should().BeFalse();

        var statutory = new AttendanceStatutoryParameter { EffectiveFrom = new DateOnly(2024, 1, 1) };
        var unit = new OrganizationUnit { UnitCode = "HQ", UnitName = "Khối Văn phòng", UnitType = "Division", WageRegion = 1, IsActive = true };
        var pos = new Position { PositionCode = "CV", PositionName = "Chuyên viên", IsActive = true };
        var hcShift = new WorkShift { ShiftCode = "HC", ShiftName = "Hành chính", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0), BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 0), StandardHours = 8m, WorkdayValue = 1.0m };

        await db.PolicyGroups.AddAsync(defaultGroup);
        await db.AttendanceStatutoryParameters.AddAsync(statutory);
        await db.AttendanceSymbolDefinitions.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultAttendanceSymbols());
        await db.OrganizationUnits.AddAsync(unit);
        await db.Positions.AddAsync(pos);
        await db.WorkShifts.AddAsync(hcShift);
        await db.SaveChangesAsync();

        var emp = new Employee
        {
            EmployeeCode = "VNM-CAP1",
            FullName = "Nguyễn Văn T2T6",
            DateOfBirth = new DateOnly(1995, 1, 1),
            Gender = "Nam",
            OrganizationUnitId = unit.Id,
            PositionId = pos.Id,
            HireDate = new DateOnly(2024, 1, 1),
            WorkStatus = "Official"
        };
        await db.Employees.AddAsync(emp);
        await db.SaveChangesAsync();

        // Populate Nov 2026: 21 Mon-Fri workdays + 2 manual extra Saturday rows (23 worked days)
        for (int day = 1; day <= 30; day++)
        {
            var d = new DateOnly(2026, 11, day);
            bool isWeekday = d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday;
            bool forceTwoSaturdays = (day == 7 || day == 14);
            bool hasWork = isWeekday || forceTwoSaturdays;
            await db.DailyAttendances.AddAsync(new DailyAttendance
            {
                EmployeeId = emp.Id,
                AttendanceDate = d,
                WorkShiftId = hcShift.Id,
                AttendanceSymbol = hasWork ? "X" : "OFF",
                WorkdayCount = hasWork ? 1m : 0m,
                StandardHours = isWeekday ? 8m : 0m,
                WorkingHours = hasWork ? 8m : 0m,
                IsManualEdited = forceTwoSaturdays,
                EditReason = forceTwoSaturdays ? "Chấm thủ công vượt chuẩn" : null
            });
        }
        await db.SaveChangesAsync();
        await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(db, 11, 2026, CancellationToken.None);

        var handler = new GetMonthlyAttendanceBoardQueryHandler(db);
        var board = await handler.Handle(new GetMonthlyAttendanceBoardQuery(11, 2026), CancellationToken.None);

        var row = board.MatrixRows.Single(r => r.EmployeeId == emp.Id);
        board.StandardWorkdaysInMonth.Should().Be(21m);
        row.StandardWorkdays.Should().Be(21m);
        row.TotalPayableWorkdays.Should().Be(21m, "AllowPayableWorkdaysExceedStandard=false không cho phép Công tính lương vượt Công chuẩn (21)");

        // When policy explicitly allows exceeding standard:
        policy.AllowPayableWorkdaysExceedStandard = true;
        await db.SaveChangesAsync();
        await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(db, 11, 2026, CancellationToken.None);

        var boardAllowed = await handler.Handle(new GetMonthlyAttendanceBoardQuery(11, 2026), CancellationToken.None);
        boardAllowed.MatrixRows.Single(r => r.EmployeeId == emp.Id).TotalPayableWorkdays.Should().Be(23m);
    }

    [Fact]
    public async Task SaveCellPunchLog_And_UpdateDaySymbol_ShouldRecalculate_AndWriteAuditLogs()
    {
        await using var db = CreateDbContext();
        var defaultGroup = ApplicationDbContextInitializer.BuildDefaultCompanyPolicyGroup();
        var unit = new OrganizationUnit { UnitCode = "HQ", UnitName = "VP", UnitType = "Division", WageRegion = 1, IsActive = true };
        var pos = new Position { PositionCode = "CV", PositionName = "Chuyên viên", IsActive = true };
        var hcShift = new WorkShift { ShiftCode = "HC", ShiftName = "Hành chính", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0), BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 0), StandardHours = 8m, WorkdayValue = 1.0m };

        await db.PolicyGroups.AddAsync(defaultGroup);
        await db.AttendanceStatutoryParameters.AddAsync(new AttendanceStatutoryParameter { EffectiveFrom = new DateOnly(2024, 1, 1) });
        await db.AttendanceSymbolDefinitions.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultAttendanceSymbols());
        await db.OrganizationUnits.AddAsync(unit);
        await db.Positions.AddAsync(pos);
        await db.WorkShifts.AddAsync(hcShift);
        await db.SaveChangesAsync();

        var emp = new Employee
        {
            EmployeeCode = "VNM-CELL1",
            FullName = "Trần Thị Sửa Công",
            DateOfBirth = new DateOnly(1996, 5, 1),
            Gender = "Nữ",
            OrganizationUnitId = unit.Id,
            PositionId = pos.Id,
            HireDate = new DateOnly(2024, 1, 1),
            WorkStatus = "Official"
        };
        await db.Employees.AddAsync(emp);
        await db.SaveChangesAsync();

        var user = new FakeCurrentUserService();
        var date = new DateOnly(2026, 11, 3); // Tuesday
        var punchHandler = new SaveCellAttendancePunchLogCommandHandler(db, user);

        // Require reason check
        Func<Task> missingReason = () => punchHandler.Handle(new SaveCellAttendancePunchLogCommand(null, emp.Id, date, new TimeOnly(8, 0), "CheckIn", " "), CancellationToken.None);
        await missingReason.Should().ThrowAsync<InvalidOperationException>();

        // Add CheckIn + CheckOut
        await punchHandler.Handle(new SaveCellAttendancePunchLogCommand(null, emp.Id, date, new TimeOnly(8, 0), "CheckIn", "Quên thẻ"), CancellationToken.None);
        var outId = await punchHandler.Handle(new SaveCellAttendancePunchLogCommand(null, emp.Id, date, new TimeOnly(17, 0), "CheckOut", "Quên thẻ"), CancellationToken.None);

        var daily = await db.DailyAttendances.SingleAsync(d => d.EmployeeId == emp.Id && d.AttendanceDate == date);
        daily.AttendanceSymbol.Should().Be("X");
        daily.WorkdayCount.Should().Be(1.0m);

        // Edit CheckOut to 17:15
        await punchHandler.Handle(new SaveCellAttendancePunchLogCommand(outId, emp.Id, date, new TimeOnly(17, 15), "CheckOut", "Cập nhật giờ ra thực tế"), CancellationToken.None);
        var logs = await db.AttendanceLogs.Where(l => l.EmployeeId == emp.Id).ToListAsync();
        logs.Should().HaveCount(2);

        // Manual Symbol Override -> 'P' (Nghỉ phép) sets IsManualEdited = true + audit
        var symHandler = new UpdateAttendanceDaySymbolCommandHandler(db, user);
        await symHandler.Handle(new UpdateAttendanceDaySymbolCommand(emp.Id, date, "P", "HR duyệt bổ sung phép"), CancellationToken.None);

        daily = await db.DailyAttendances.SingleAsync(d => d.EmployeeId == emp.Id && d.AttendanceDate == date);
        daily.AttendanceSymbol.Should().Be("P");
        daily.IsManualEdited.Should().BeTrue();
        daily.EditReason.Should().Contain("HR duyệt bổ sung phép");

        var audits = await db.AuditLogs.AsNoTracking().ToListAsync();
        audits.Should().Contain(a => a.ActionType == "AddAttendanceLog");
        audits.Should().Contain(a => a.ActionType == "UpdateAttendanceLog");
        audits.Should().Contain(a => a.ActionType == "UpdateAttendanceDaySymbol");
    }

    [Fact]
    public async Task ShiftAndHolidaySeeder_ShouldCreateSafe8DayFactoryRotationWithoutCa3ToCa1_AndCopyPreviousWeek()
    {
        await using var db = CreateDbContext();
        var defaultGroup = ApplicationDbContextInitializer.BuildDefaultCompanyPolicyGroup();
        var factoryGroup = ApplicationDbContextInitializer.BuildFactoryMonSatPolicyGroup();

        var officeUnit = new OrganizationUnit { UnitCode = "VP", UnitName = "Văn phòng", UnitType = "Division", WageRegion = 1, IsActive = true };
        var factoryUnit = new OrganizationUnit { UnitCode = "NM-TEST", UnitName = "Nhà máy Sữa", UnitType = "Factory", WageRegion = 2, IsActive = true };
        var pos = new Position { PositionCode = "CN", PositionName = "Công nhân", IsActive = true };
        var ca1 = new WorkShift { ShiftCode = "CA1", ShiftName = "Ca 1", StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(14, 0), StandardHours = 8m, WorkdayValue = 1m };
        var ca2 = new WorkShift { ShiftCode = "CA2", ShiftName = "Ca 2", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(22, 0), StandardHours = 8m, WorkdayValue = 1m };
        var ca3 = new WorkShift { ShiftCode = "CA3", ShiftName = "Ca 3", StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0), ShiftType = "Overnight", IsOvernight = true, StandardHours = 8m, WorkdayValue = 1m };
        var hc = new WorkShift { ShiftCode = "HC", ShiftName = "Hành chính", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0), BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 0), StandardHours = 8m, WorkdayValue = 1m };

        await db.PolicyGroups.AddRangeAsync(defaultGroup, factoryGroup);
        await db.OrganizationUnits.AddRangeAsync(officeUnit, factoryUnit);
        await db.Positions.AddAsync(pos);
        await db.WorkShifts.AddRangeAsync(ca1, ca2, ca3, hc);
        await db.SaveChangesAsync();

        await db.PolicyGroupAssignments.AddAsync(new PolicyGroupAssignment
        {
            PolicyGroupId = factoryGroup.Id,
            OrganizationUnitId = factoryUnit.Id,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            IsActive = true
        });

        var empOffice = new Employee { EmployeeCode = "OFC1", FullName = "NV VP", DateOfBirth = new DateOnly(1995, 1, 1), Gender = "Nam", OrganizationUnitId = officeUnit.Id, PositionId = pos.Id, HireDate = new DateOnly(2024, 1, 1), WorkStatus = "Official" };
        var empFactory = new Employee { EmployeeCode = "FAC1", FullName = "CN Nhà máy", DateOfBirth = new DateOnly(1995, 1, 1), Gender = "Nam", OrganizationUnitId = factoryUnit.Id, PositionId = pos.Id, HireDate = new DateOnly(2024, 1, 1), WorkStatus = "Official" };
        await db.Employees.AddRangeAsync(empOffice, empFactory);
        await db.SaveChangesAsync();

        var weekStart = new DateOnly(2026, 11, 2); // Monday
        await ShiftAndHolidaySeeder.EnsureHolidaysAndSampleSchedulesAsync(db, weekStart.Year, weekStart, CancellationToken.None);

        var officeSchedules = await db.ShiftSchedules.Where(s => s.EmployeeId == empOffice.Id && s.WorkDate >= weekStart && s.WorkDate <= weekStart.AddDays(6)).ToListAsync();
        officeSchedules.Should().Contain(s => s.WorkDate.DayOfWeek == DayOfWeek.Saturday && s.IsDayOff);
        officeSchedules.Should().Contain(s => s.WorkDate.DayOfWeek == DayOfWeek.Sunday && s.IsDayOff);

        // Evaluate factory 14-day schedule: zero CA3 -> CA1 (rest < 12h) conflicts!
        await ShiftAndHolidaySeeder.EnsureHolidaysAndSampleSchedulesAsync(db, weekStart.AddDays(7).Year, weekStart.AddDays(7), CancellationToken.None);
        var facSchedules = await db.ShiftSchedules.Where(s => s.EmployeeId == empFactory.Id).OrderBy(s => s.WorkDate).ToListAsync();
        facSchedules.Should().OnlyContain(s => string.IsNullOrEmpty(s.ConflictWarning), "mẫu xoay 8 ngày CA1-CA1-CA2-CA2-CA3-CA3-OFF-OFF không phát sinh vi phạm nghỉ <12h");

        // Test CopyPreviousWeekScheduleCommand to next week
        var nextWeek = weekStart.AddDays(14);
        var copyHandler = new CopyPreviousWeekScheduleCommandHandler(db, new FakeCurrentUserService());
        var copyRes = await copyHandler.Handle(new CopyPreviousWeekScheduleCommand(nextWeek), CancellationToken.None);
        copyRes.AffectedCount.Should().BeGreaterThan(0);

        // Test DeleteShiftScheduleCommand + Locked month block
        var delHandler = new DeleteShiftScheduleCommandHandler(db, new FakeCurrentUserService());
        var delRes = await delHandler.Handle(new DeleteShiftScheduleCommand(empOffice.Id, weekStart), CancellationToken.None);
        delRes.AffectedCount.Should().BeGreaterThan(0);

        await db.MonthlyAttendanceSummaries.AddAsync(new MonthlyAttendanceSummary
        {
            EmployeeId = empOffice.Id,
            Month = nextWeek.Month,
            Year = nextWeek.Year,
            StandardWorkdays = 21m,
            IsLocked = true
        });
        await db.SaveChangesAsync();

        Func<Task> copyLocked = () => copyHandler.Handle(new CopyPreviousWeekScheduleCommand(nextWeek), CancellationToken.None);
        await copyLocked.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đã khóa*");
    }
}
