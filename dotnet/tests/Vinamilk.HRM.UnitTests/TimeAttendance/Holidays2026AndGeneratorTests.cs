using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Xunit;

namespace Vinamilk.HRM.UnitTests.TimeAttendance;

public class Holidays2026AndGeneratorTests
{
    private sealed class StubCurrentUser : ICurrentUserService
    {
        public int? UserId => 1;
        public string Username => "hr.tester";
        public int? EmployeeId => null;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "xUnit";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permissionCode) => true;
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmHolidayTests_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task BuildDefaultHolidays2026_ShouldContain12StatutoryDays_IncludingNov24_AndDefaultSep01()
    {
        var holidays2026 = ApplicationDbContextInitializer.BuildDefaultHolidays2026();

        holidays2026.Should().HaveCount(12);
        holidays2026.Select(h => h.HolidayDate).Should().OnlyHaveUniqueItems();
        holidays2026.Should().AllSatisfy(h =>
        {
            h.Year.Should().Be(2026);
            h.IsPaidHoliday.Should().BeTrue();
            h.OvertimeMultiplier.Should().Be(3.0m);
        });

        var cultureDay = holidays2026.Single(h => h.HolidayDate == new DateOnly(2026, 11, 24));
        cultureDay.HolidayName.Should().Contain("Ngày Văn hóa Việt Nam");
        cultureDay.NeedsHrVerification.Should().BeTrue();
        cultureDay.LegalReference.Should().Contain("80-NQ/TW").And.Contain("Điều 112");

        // Mặc định Quốc khánh 2026: 01/09 (TB 9441/TB-BNV) & 02/09
        holidays2026.Should().Contain(h => h.HolidayDate == new DateOnly(2026, 9, 1) && h.NeedsHrVerification);
        holidays2026.Should().Contain(h => h.HolidayDate == new DateOnly(2026, 9, 2) && !h.NeedsHrVerification);
        holidays2026.Should().NotContain(h => h.HolidayDate == new DateOnly(2026, 9, 3));

        // Vẫn cho HR chuyển đổi 01/09 <-> 03/09
        await using var db = CreateDb();
        await db.Holidays.AddRangeAsync(holidays2026);
        await db.SaveChangesAsync();

        var switchHandler = new SelectNationalDayAdjacentHolidayCommandHandler(db);
        var chosenAfter = await switchHandler.Handle(new SelectNationalDayAdjacentHolidayCommand(2026, "After"), CancellationToken.None);
        chosenAfter.Should().Be(new DateOnly(2026, 9, 3));
        (await db.Holidays.AnyAsync(h => h.HolidayDate == new DateOnly(2026, 9, 1))).Should().BeFalse();
        (await db.Holidays.SingleAsync(h => h.HolidayDate == new DateOnly(2026, 9, 3))).NeedsHrVerification.Should().BeFalse();

        var chosenBefore = await switchHandler.Handle(new SelectNationalDayAdjacentHolidayCommand(2026, "Before"), CancellationToken.None);
        chosenBefore.Should().Be(new DateOnly(2026, 9, 1));
        (await db.Holidays.AnyAsync(h => h.HolidayDate == new DateOnly(2026, 9, 3))).Should().BeFalse();
        (await db.Holidays.SingleAsync(h => h.HolidayDate == new DateOnly(2026, 9, 1))).NeedsHrVerification.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateHolidaysForYear_ShouldApplyFixedRules_AndAcceptHrLunarInputs()
    {
        await using var db = CreateDb();
        var user = new StubCurrentUser();
        var handler = new GenerateHolidaysForYearCommandHandler(db, user);

        // Trường hợp 1: Chỉ sinh ngày Dương cố định cho 2027 (chưa nhập Tết/Giỗ Tổ -> cảnh báo HR bổ sung)
        var partialRes = await handler.Handle(new GenerateHolidaysForYearCommand(Year: 2027), CancellationToken.None);
        partialRes.CreatedCount.Should().Be(6); // 01/01, 30/04, 01/05, 01/09, 02/09, 24/11
        partialRes.PendingManualItems.Should().HaveCount(2);

        var h2027 = await db.Holidays.Where(h => h.Year == 2027).OrderBy(h => h.HolidayDate).ToListAsync();
        h2027.Select(h => h.HolidayDate).Should().BeEquivalentTo([
            new DateOnly(2027, 1, 1),
            new DateOnly(2027, 4, 30),
            new DateOnly(2027, 5, 1),
            new DateOnly(2027, 9, 1),
            new DateOnly(2027, 9, 2),
            new DateOnly(2027, 11, 24)
        ]);

        // Trường hợp 2: Nhập đủ ngày Âm lịch cho 2028 -> đủ 12 ngày + ghi AuditLog
        var fullRes = await handler.Handle(new GenerateHolidaysForYearCommand(
            Year: 2028,
            TetStartDate: new DateOnly(2028, 1, 25),
            HungKingsDate: new DateOnly(2028, 4, 4),
            NationalDayAdjacentChoice: "Before",
            IncludeVietnamCultureDay: true), CancellationToken.None);

        fullRes.CreatedCount.Should().Be(12);
        fullRes.PendingManualItems.Should().BeEmpty();
        (await db.Holidays.CountAsync(h => h.Year == 2028)).Should().Be(12);
        (await db.AuditLogs.AnyAsync(a => a.ActionType == "GenerateHolidaysForYear" && a.EntityId == "2028")).Should().BeTrue();
    }

    [Fact]
    public async Task MonthlyAttendanceClosing_ShouldWarnWhenYearHasNoHolidays()
    {
        await using var db = CreateDb();
        var user = new StubCurrentUser();

        db.OrganizationUnits.Add(new OrganizationUnit { Id = 1, UnitCode = "HO", UnitName = "Trụ sở chính", IsActive = true });
        db.Positions.Add(new Position { Id = 1, PositionCode = "NV", PositionName = "Nhân viên", IsActive = true });
        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "VNM101",
            FullName = "Nguyễn Kiểm Thử",
            OrganizationUnitId = 1,
            PositionId = 1,
            WorkStatus = "Official",
            HireDate = new DateOnly(2025, 1, 1)
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = 1,
            Month = 5,
            Year = 2027,
            StandardWorkdays = 21m,
            ActualWorkdays = 21m,
            TotalPayableWorkdays = 21m,
            ClosingStatus = "Open"
        });
        await db.SaveChangesAsync();

        // Không seed Holidays cho 2027 -> Board cảnh báo HolidayCountInYear == 0
        var board = await new GetMonthlyAttendanceBoardQueryHandler(db)
            .Handle(new GetMonthlyAttendanceBoardQuery(5, 2027), CancellationToken.None);
        board.HolidayCountInYear.Should().Be(0);

        // AdvanceClosingCommand cũng trả về cảnh báo
        var stepMsg = await new AdvanceAttendanceClosingCommandHandler(db, user)
            .Handle(new AdvanceAttendanceClosingCommand(5, 2027, "UnitSubmit"), CancellationToken.None);
        stepMsg.Should().Contain("CẢNH BÁO").And.Contain("2027");

        // ToggleLock cũng trả về cảnh báo
        var lockMsg = await new ToggleLockMonthlyAttendanceCommandHandler(db, user)
            .Handle(new ToggleLockMonthlyAttendanceCommand(5, 2027, true), CancellationToken.None);
        lockMsg.Should().Contain("CẢNH BÁO").And.Contain("2027");
    }

    [Fact]
    public async Task November2026_VietnamCultureDay24Nov_ShouldAwardHolidayWorkday_HolidayOt300_AndExcludePostHolidayHires()
    {
        await using var db = CreateDb();
        var user = new StubCurrentUser();

        await db.AttendanceStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultAttendanceStatutoryParameters());
        await db.AttendanceSymbolDefinitions.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultAttendanceSymbols());
        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());
        await db.PayrollParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultTaxExemptPayrollParameters());
        await db.Holidays.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultHolidays2026());

        var defaultGroup = ApplicationDbContextInitializer.BuildDefaultCompanyPolicyGroup();
        defaultGroup.Id = 1;
        await db.PolicyGroups.AddAsync(defaultGroup);

        db.OrganizationUnits.Add(new OrganizationUnit { Id = 1, UnitCode = "HO", UnitName = "Văn phòng HQ", CostCenterCode = "HO", WageRegion = 1, IsActive = true });
        db.Positions.Add(new Position { Id = 1, PositionCode = "NV", PositionName = "Chuyên viên", IsActive = true });
        var shiftHc = new WorkShift
        {
            Id = 1,
            ShiftCode = "HC",
            ShiftName = "Hành chính",
            ShiftType = "Fixed",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            BreakStartTime = new TimeOnly(12, 0),
            BreakEndTime = new TimeOnly(13, 0),
            StandardHours = 8m,
            WorkdayValue = 1m,
            GraceLateMinutes = 5
        };
        db.WorkShifts.Add(shiftHc);

        // E1: Nhân viên làm đủ tháng 11/2026, nghỉ Lễ 24/11 hưởng nguyên lương
        // E2: Nhân viên làm đủ tháng 11/2026, trực OT ngày Lễ 24/11 (8h x 300%)
        // E3: Nhân viên vào làm ngày 25/11/2026 (SAU 24/11) -> KHÔNG được tính Lễ 24/11
        db.Employees.AddRange(
            new Employee
            {
                Id = 1, EmployeeCode = "NV_T11_01", FullName = "Lê Nghỉ Lễ", OrganizationUnitId = 1, PositionId = 1,
                WorkStatus = "Official", CurrentContractType = "Indefinite", HireDate = new DateOnly(2024, 1, 1),
                BaseSalary = 21_000_000m
            },
            new Employee
            {
                Id = 2, EmployeeCode = "NV_T11_02", FullName = "Trần Trực Lễ OT", OrganizationUnitId = 1, PositionId = 1,
                WorkStatus = "Official", CurrentContractType = "Indefinite", HireDate = new DateOnly(2024, 1, 1),
                BaseSalary = 21_000_000m
            },
            new Employee
            {
                Id = 3, EmployeeCode = "NV_T11_03", FullName = "Phạm Nhận Việc Sau Lễ", OrganizationUnitId = 1, PositionId = 1,
                WorkStatus = "Official", CurrentContractType = "Fixed12M", HireDate = new DateOnly(2026, 11, 25),
                BaseSalary = 21_000_000m
            });

        foreach (int empId in new[] { 1, 2, 3 })
        {
            db.EmployeeSalaryProfiles.Add(new EmployeeSalaryProfile
            {
                EmployeeId = empId,
                EffectiveFrom = new DateOnly(2026, 1, 1),
                BaseSalary = 21_000_000m,
                InsuranceSalary = 21_000_000m,
                PayMode = "Monthly",
                IsActive = true
            });
        }

        // Đơn OT ngày 24/11 được duyệt cho E2
        db.OvertimeRequests.Add(new OvertimeRequest
        {
            EmployeeId = 2,
            OvertimeDate = new DateOnly(2026, 11, 24),
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            RequestedHours = 8m,
            ApprovedHours = 8m,
            OvertimeRateType = "OT300",
            Reason = "Vận hành hệ thống ngày Văn hóa VN 24/11",
            Status = "Approved"
        });

        var novStart = new DateOnly(2026, 11, 1);
        var novEnd = new DateOnly(2026, 11, 30);
        for (var d = novStart; d <= novEnd; d = d.AddDays(1))
        {
            bool isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            bool isNov24 = d == new DateOnly(2026, 11, 24);

            foreach (int empId in new[] { 1, 2, 3 })
            {
                bool beforeHire = empId == 3 && d < new DateOnly(2026, 11, 25);
                db.ShiftSchedules.Add(new ShiftSchedule
                {
                    EmployeeId = empId,
                    WorkShiftId = 1,
                    WorkDate = d,
                    IsDayOff = isWeekend || beforeHire
                });

                if (isWeekend || beforeHire)
                    continue;

                if (isNov24 && empId != 2)
                    continue; // E1 nghỉ Lễ hưởng lương; E2 trực OT có quẹt thẻ

                db.AttendanceLogs.AddRange(
                    new AttendanceLog { EmployeeId = empId, PunchTime = d.ToDateTime(new TimeOnly(8, 0)), PunchType = "CheckIn", SourceType = "BiometricDevice" },
                    new AttendanceLog { EmployeeId = empId, PunchTime = d.ToDateTime(new TimeOnly(17, 0)), PunchType = "CheckOut", SourceType = "BiometricDevice" });
            }
        }
        await db.SaveChangesAsync();

        // Tính lại công toàn bộ tháng 11/2026 (21 ngày T2-T6: 20 ngày thường + 24/11)
        var dispatcher = new InProcessAttendanceRecalculationDispatcher(db, new AttendanceRecalculationService(db));
        var batchId = await new StartAttendanceRecalculationCommandHandler(dispatcher, user)
            .Handle(new StartAttendanceRecalculationCommand(11, 2026), CancellationToken.None);
        var batch = await new GetRecalculationBatchStatusQueryHandler(db)
            .Handle(new GetRecalculationBatchStatusQuery(batchId), CancellationToken.None);
        batch!.Status.Should().Be("Completed");

        var day24Logs = await db.DailyAttendances.AsNoTracking()
            .Where(d => d.AttendanceDate == new DateOnly(2026, 11, 24))
            .ToDictionaryAsync(d => d.EmployeeId);
        var summaries = await db.MonthlyAttendanceSummaries.AsNoTracking()
            .Where(m => m.Month == 11 && m.Year == 2026)
            .ToDictionaryAsync(m => m.EmployeeId);

        // E1: nghỉ Lễ 24/11 -> ký hiệu L, 1 công, tổng tháng 21 công hưởng lương (20 ngày T2-T6 + 24/11); bảng công T2-T7 = 25 công chuẩn
        day24Logs[1].AttendanceSymbol.Should().Be("L");
        day24Logs[1].WorkdayCount.Should().Be(1m);
        summaries[1].StandardWorkdays.Should().Be(25m);
        summaries[1].HolidayDays.Should().Be(1m);
        summaries[1].TotalPayableWorkdays.Should().Be(21m);

        // E2: trực OT ngày 24/11 -> ký hiệu L (1 công gốc) + 8 giờ OT300
        day24Logs[2].AttendanceSymbol.Should().Be("L");
        day24Logs[2].WorkdayCount.Should().Be(1m);
        day24Logs[2].OvertimeHours300.Should().Be(8m);
        summaries[2].StandardWorkdays.Should().Be(25m);
        summaries[2].HolidayDays.Should().Be(1m);
        summaries[2].TotalOtHours300.Should().Be(8m);
        summaries[2].TotalPayableWorkdays.Should().Be(21m);

        // E3: vào làm từ 25/11 (sau 24/11) -> KHÔNG được tính Lễ 24/11, chỉ hưởng 4 công (25, 26, 27, 30/11)
        day24Logs[3].AttendanceSymbol.Should().Be("OFF");
        day24Logs[3].WorkdayCount.Should().Be(0m);
        summaries[3].StandardWorkdays.Should().Be(25m);
        summaries[3].HolidayDays.Should().Be(0m);
        summaries[3].TotalPayableWorkdays.Should().Be(4m);

        // Chốt công & Tính lương kiểm tra công chuẩn Mon-Fri (21) + tiền công Lễ + OT 300% tháng 11/2026
        await new ToggleLockMonthlyAttendanceCommandHandler(db, user)
            .Handle(new ToggleLockMonthlyAttendanceCommand(11, 2026, true), CancellationToken.None);

        int periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 11, StandardWorkdays: 21m), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(periodId, RequireLockedAttendance: true), CancellationToken.None);

        var payslips = await db.PayrollRecords.AsNoTracking()
            .Where(r => r.PayrollPeriodId == periodId)
            .ToDictionaryAsync(r => r.EmployeeId);

        payslips[1].StandardWorkdays.Should().Be(21m);
        payslips[1].PayableWorkdays.Should().Be(21m);
        payslips[1].ActualBaseSalary.Should().Be(21_000_000m); // Đủ 21/21 công (gồm 24/11)
        payslips[2].ActualBaseSalary.Should().Be(21_000_000m);
        payslips[2].OtHours300.Should().Be(8m);
        payslips[2].OvertimePay.Should().Be(3_000_000m); // 21,000,000 / 21 / 8 * 8h * 300% = 3,000,000
        payslips[3].PayableWorkdays.Should().Be(4m);
        payslips[3].ActualBaseSalary.Should().Be(4_000_000m); // Chỉ tính 4 công sau 24/11 (4/21 * 21,000,000)
        payslips[3].OvertimePay.Should().Be(0m);
    }
}
