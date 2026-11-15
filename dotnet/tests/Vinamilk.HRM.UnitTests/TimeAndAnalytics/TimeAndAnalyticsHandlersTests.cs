using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Xunit;

namespace Vinamilk.HRM.UnitTests.TimeAndAnalytics;

public class TimeAndAnalyticsHandlersTests
{
    private sealed class MockUser(int? userId, int? empId) : ICurrentUserService
    {
        public int? UserId => userId;
        public string Username => "tester";
        public int? EmployeeId => empId;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "UnitTest";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permissionCode) => true;
    }

    private sealed class NoOpEmail : IEmailNotificationService
    {
        public int SentCount { get; private set; }
        public Task SendWorkflowEmailAsync(string toEmail, string recipientName, string subject, string documentTitle, string initiatorName, string currentStepName, string statusBadge, string actionUrl, string? comment = null, CancellationToken cancellationToken = default)
        {
            SentCount++;
            return Task.CompletedTask;
        }
        public Task SendPayslipEmailWithAttachmentAsync(string toEmail, string recipientName, string subject, string periodTitle, string htmlSummary, byte[] pdfAttachment, string attachmentFileName, CancellationToken cancellationToken = default)
        {
            SentCount++;
            return Task.CompletedTask;
        }
        public IReadOnlyList<SentEmailRecordDto> GetRecentSentEmails() => Array.Empty<SentEmailRecordDto>();
    }

    private sealed class MockAiService : IAiHrmExpertService
    {
        public string ProviderName => "DeterministicTestRuleEngine";
        public Task<AiPolicyAnswerDto> AskPolicyAsync(string question, CancellationToken cancellationToken = default)
            => Task.FromResult(new AiPolicyAnswerDto(question, "Phép năm cơ bản 12 ngày.", ProviderName, new List<AiRagCitationDto>()));
        public Task<List<CandidateJobMatchDto>> MatchCandidatesAsync(int? jobPostingId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<CandidateJobMatchDto>());
        public Task<List<AttritionRiskItemDto>> AnalyzeAttritionRisksAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<AttritionRiskItemDto> { new(1, "VNM501", "Nguyễn Văn X", "IT", 75, "High", new List<string> { "OT cao" }, "Review lương") });
    }

    private sealed class StubSender(Func<object, int> wfReturn) : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult((TResponse)(object)wfReturn(request));
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(wfReturn(request));
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        db.AttendanceStatutoryParameters.Add(new AttendanceStatutoryParameter
        {
            EffectiveFrom = new DateOnly(2021, 1, 1),
            EffectiveTo = null,
            LegalReference = "BLLD 2019",
            NeedsHrVerification = true,
            NightWorkStartTime = new TimeOnly(22, 0),
            NightWorkEndTime = new TimeOnly(6, 0),
            MinInterShiftRestHours = 12m,
            MaxNormalDailyHours = 8m,
            MaxNormalWeeklyHours = 48m,
            MaxMonthlyOvertimeHours = 40m,
            MaxYearlyOvertimeHours = 200m
        });
        var defGroup = new PolicyGroup
        {
            PolicyCode = "DEFAULT",
            PolicyName = "Default",
            IsCompanyDefault = true,
            IsActive = true
        };
        defGroup.AttendancePolicies.Add(new AttendancePolicy
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
        db.PolicyGroups.Add(defGroup);
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task ShiftAndAttendanceHandlers_ShouldCheckIn_EnforceOvertimeCap_Recalculate_AndLock()
    {
        using var context = CreateDb();
        var unit = new OrganizationUnit { UnitCode = "PRD", UnitName = "Sản xuất", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "OPR", PositionName = "Công nhân", PositionLevel = 6 };
        await context.AddRangeAsync(unit, pos);
        await context.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "VNM501", FullName = "Trần Văn Công", OrganizationUnitId = unit.Id, PositionId = pos.Id, WorkStatus = "Active" };
        await context.Employees.AddAsync(emp);
        await context.SaveChangesAsync();

        var shiftId = await new SaveWorkShiftCommandHandler(context).Handle(
            new SaveWorkShiftCommand(null, "HC", "Hành chính", new TimeOnly(8, 0), new TimeOnly(17, 0), new TimeOnly(12, 0), new TimeOnly(13, 0), false, 8m, 1m, 0m, 10),
            CancellationToken.None);
        await new SaveWorkShiftCommandHandler(context).Handle(
            new SaveWorkShiftCommand(shiftId, "HC", "Hành chính chuẩn", new TimeOnly(8, 0), new TimeOnly(17, 0), new TimeOnly(12, 0), new TimeOnly(13, 0), false, 8m, 1m, 0m, 15),
            CancellationToken.None);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var assignedCount = await new AssignShiftScheduleCommandHandler(context).Handle(
            new AssignShiftScheduleCommand(emp.Id, null, today, today.AddDays(1), shiftId, false, "Ca ngày"), CancellationToken.None);
        assignedCount.Should().BeGreaterThan(0);
        await new AssignShiftScheduleCommandHandler(context).Handle(
            new AssignShiftScheduleCommand(null, unit.Id, today.AddDays(2), today.AddDays(3), shiftId, false, "Lịch đơn vị"), CancellationToken.None);

        var holidayId = await new SaveHolidayCommandHandler(context).Handle(
            new SaveHolidayCommand(null, new DateOnly(2026, 9, 2), "Quốc khánh", true, 3.0m), CancellationToken.None);
        await new SaveHolidayCommandHandler(context).Handle(
            new SaveHolidayCommand(holidayId, new DateOnly(2026, 9, 2), "Lễ Quốc khánh 2/9", true, 3.0m), CancellationToken.None);
        var shiftOverview = await new GetShiftAndScheduleOverviewQueryHandler(context)
            .Handle(new GetShiftAndScheduleOverviewQuery(today, unit.Id, 2026), CancellationToken.None);
        shiftOverview.WorkShifts.Should().HaveCount(1);
        (await new DeleteHolidayCommandHandler(context).Handle(new DeleteHolidayCommand(holidayId), CancellationToken.None)).Should().BeTrue();

        // CheckIn + CheckOut (valid dynamic QR, MobileGps, and invalid QR rejection)
        var validQr = VinamilkGeofenceCalculator.GenerateDynamicQrToken(DateTime.UtcNow);
        var invalidQrAct = () => new WebCheckInCommandHandler(context).Handle(
            new WebCheckInCommand(emp.Id, "CheckIn", "WebQr", 10.728, 106.721, "INVALID-QR"), CancellationToken.None);
        await invalidQrAct.Should().ThrowAsync<InvalidOperationException>();

        var inMsg = await new WebCheckInCommandHandler(context).Handle(
            new WebCheckInCommand(emp.Id, "CheckIn", "WebQr", 10.728, 106.721, validQr), CancellationToken.None);
        inMsg.Should().NotBeNullOrWhiteSpace();
        var outMsg = await new WebCheckInCommandHandler(context).Handle(
            new WebCheckInCommand(emp.Id, "CheckOut", "MobileGps", 10.728, 106.721, null), CancellationToken.None);
        outMsg.Should().NotBeNullOrWhiteSpace();

        // Overtime within limit & exceeding 40h monthly cap
        var sender = new StubSender(_ => 101);
        var otHandler = new SubmitOvertimeRequestCommandHandler(context, sender);
        var otId = await otHandler.Handle(new SubmitOvertimeRequestCommand(emp.Id, today, new TimeOnly(17, 30), new TimeOnly(21, 30), "OT150", "Xử lý đơn hàng"), CancellationToken.None);
        otId.Should().BeGreaterThan(0);

        // Seed existing 38h OT in the same month to hit 40h monthly cap
        await context.OvertimeRequests.AddAsync(new OvertimeRequest
        {
            EmployeeId = emp.Id,
            OvertimeDate = today.AddDays(-1),
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(22, 0),
            RequestedHours = 36m,
            OvertimeRateType = "OT150",
            Reason = "Dự án cũ",
            Status = "Approved"
        });
        await context.SaveChangesAsync();

        var actOverCap = () => otHandler.Handle(new SubmitOvertimeRequestCommand(emp.Id, today, new TimeOnly(18, 0), new TimeOnly(20, 0), "OT150", "Vượt 40h"), CancellationToken.None);
        await actOverCap.Should().ThrowAsync<InvalidOperationException>().WithMessage("*40 giờ/tháng*");

        // Attendance Explanation & Recalculate & Lock
        var explainId = await new SubmitAttendanceExplanationCommandHandler(context, sender).Handle(
            new SubmitAttendanceExplanationCommand(emp.Id, today, new TimeOnly(8, 0), new TimeOnly(17, 0), "Quên bấm thẻ"), CancellationToken.None);
        explainId.Should().BeGreaterThan(0);

        var recalcCount = await new RecalculateMonthlyAttendanceCommandHandler(context)
            .Handle(new RecalculateMonthlyAttendanceCommand(today.Month, today.Year), CancellationToken.None);
        recalcCount.Should().BeGreaterThanOrEqualTo(0);

        var board = await new GetMonthlyAttendanceBoardQueryHandler(context)
            .Handle(new GetMonthlyAttendanceBoardQuery(today.Month, today.Year, unit.Id, "VNM501"), CancellationToken.None);
        board.MatrixRows.Should().HaveCount(1);

        var lockMsg = await new ToggleLockMonthlyAttendanceCommandHandler(context, new MockUser(1, null))
            .Handle(new ToggleLockMonthlyAttendanceCommand(today.Month, today.Year, true), CancellationToken.None);
        lockMsg.Should().ContainEquivalentOf("khóa");

        var explainWhenLocked = () => new SubmitAttendanceExplanationCommandHandler(context, sender)
            .Handle(new SubmitAttendanceExplanationCommand(emp.Id, today, new TimeOnly(8, 0), new TimeOnly(17, 0), "Sau khóa"), CancellationToken.None);
        await explainWhenLocked.Should().ThrowAsync<InvalidOperationException>();

        var unlockMsg = await new ToggleLockMonthlyAttendanceCommandHandler(context, new MockUser(1, null))
            .Handle(new ToggleLockMonthlyAttendanceCommand(today.Month, today.Year, false), CancellationToken.None);
        unlockMsg.Should().ContainEquivalentOf("mở");
    }

    [Fact]
    public async Task WorkflowDefinitionAndReportsAndEssAiHandlers_ShouldReturnExpectedResults()
    {
        using var context = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var email = new NoOpEmail();

        var unit = new OrganizationUnit { UnitCode = "IT", UnitName = "Công nghệ", UnitLevel = 1, TreePath = "/1/" };
        var pos = new Position { PositionCode = "LEAD", PositionName = "Trưởng nhóm", PositionLevel = 4 };
        await context.AddRangeAsync(unit, pos);
        await context.SaveChangesAsync();

        var emp = new Employee { EmployeeCode = "VNM601", FullName = "Ngô Anh Dũng", OrganizationUnitId = unit.Id, PositionId = pos.Id, BaseSalary = 25_000_000m, WorkStatus = "Active", HireDate = new DateOnly(2023, 1, 1) };
        var emp2 = new Employee { EmployeeCode = "VNM602", FullName = "Lê Thị Mai", OrganizationUnitId = unit.Id, PositionId = pos.Id, BaseSalary = 20_000_000m, WorkStatus = "Official", HireDate = new DateOnly(2022, 1, 1) };
        var emp3 = new Employee { EmployeeCode = "VNM603", FullName = "Trần Minh Quân", OrganizationUnitId = unit.Id, PositionId = pos.Id, BaseSalary = 18_000_000m, WorkStatus = "Official", HireDate = new DateOnly(2021, 1, 1) };
        var leaveType = new LeaveType { LeaveTypeCode = "ANNUAL", LeaveTypeName = "Phép năm", IsPaidByCompany = true, MaxDaysPerYear = 12 };
        var period = new PayrollPeriod { Year = 2026, Month = 5, PeriodCode = "2026-05", PeriodTitle = "Lương T05/2026", StandardWorkdays = 26, Status = "Locked" };
        await context.AddRangeAsync(emp, emp2, emp3, leaveType, period);
        await context.SaveChangesAsync();

        await context.PayrollRecords.AddAsync(new PayrollRecord { PayrollPeriodId = period.Id, EmployeeId = emp.Id, OrganizationUnitId = unit.Id, PositionId = pos.Id, GrossIncome = 25_000_000m, NetSalary = 21_500_000m, PayableWorkdays = 26m, PaymentStatus = "Paid" });
        await context.SaveChangesAsync();

        // SaveWorkflowDefinition & GetWorkflowCenter
        var defId = await new SaveWorkflowDefinitionCommandHandler(context).Handle(
            new SaveWorkflowDefinitionCommand(null, "WF_OT", "Quy trình duyệt OT", "OvertimeRequest", "OT", true,
            [
                new SaveWorkflowStepInput(1, "Quản lý trực tiếp", "DirectManager", null, null, null, 24)
            ]),
            CancellationToken.None);
        defId.Should().BeGreaterThan(0);

        var wfCenter = await new GetWorkflowCenterQueryHandler(context, new MockUser(1, emp.Id), email)
            .Handle(new GetWorkflowCenterQuery(), CancellationToken.None);
        wfCenter.Definitions.Should().Contain(d => d.WorkflowCode == "WF_OT");
        wfCenter.Definitions.Should().Contain(d => d.WorkflowCode == "WF_PAYROLL");

        // Leave overview
        var leaveOverview = await new GetLeaveManagementOverviewQueryHandler(context)
            .Handle(new GetLeaveManagementOverviewQuery(2026, 5, unit.Id), CancellationToken.None);
        leaveOverview.LeaveTypes.Should().NotBeEmpty();

        // Reports BM01-BM04 & Dynamic & Schedule
        (await new GetBm01EmployeeMasterReportQueryHandler(context).Handle(new GetBm01EmployeeMasterReportQuery(unit.Id), CancellationToken.None)).Should().HaveCount(3);
        (await new GetBm02MovementReportQueryHandler(context).Handle(new GetBm02MovementReportQuery(2026, 5, unit.Id), CancellationToken.None)).Should().NotBeNull();
        (await new GetBm03RewardDisciplineReportQueryHandler(context).Handle(new GetBm03RewardDisciplineReportQuery(2026), CancellationToken.None)).Should().NotBeNull();
        (await new GetBm04PayrollSummaryReportQueryHandler(context).Handle(new GetBm04PayrollSummaryReportQuery(2026, 5), CancellationToken.None)).Should().HaveCount(1);
        var dyn = await new RunDynamicReportQueryHandler(context).Handle(new RunDynamicReportQuery(["EmployeeCode", "FullName", "UnitName", "BaseSalary"], unit.Id, null, "Unit"), CancellationToken.None);
        dyn.Rows.Should().HaveCount(3);
        (await new RunDynamicReportQueryHandler(context).Handle(new RunDynamicReportQuery(null, null, null, "Position"), CancellationToken.None)).Rows.Should().HaveCount(3);

        var sched = await new SaveReportScheduleCommandHandler(cache).Handle(
            new SaveReportScheduleCommand("BM01", "Weekly", "hr@vinamilk.local;cfo@vinamilk.local", true), CancellationToken.None);
        (await new GetReportSchedulesQueryHandler(cache).Handle(new GetReportSchedulesQuery(), CancellationToken.None)).Should().NotBeEmpty();
        var sendMsg = await new SendScheduledReportCommandHandler(cache, email, context).Handle(new SendScheduledReportCommand(sched.Id, "https://hrm.local"), CancellationToken.None);
        sendMsg.Should().Contain("2");
        email.SentCount.Should().Be(2);

        // ESS / MSS & AI Expert
        var ess = await new GetEssMssPortalQueryHandler(context, new MockUser(1, emp.Id))
            .Handle(new GetEssMssPortalQuery(emp.Id), CancellationToken.None);
        ess.Profile.EmployeeCode.Should().Be("VNM601");
        ess.LatestPayslip.Should().NotBeNull();

        var aiMock = new MockAiService();
        var policyAnswer = await new AskAiPolicyRagQueryHandler(aiMock).Handle(new AskAiPolicyRagQuery("Phép năm?"), CancellationToken.None);
        policyAnswer.Answer.Should().Contain("12");

        var aiOverview = await new GetAiExpertOverviewQueryHandler(aiMock, context).Handle(new GetAiExpertOverviewQuery(), CancellationToken.None);
        aiOverview.AttritionRisks.Should().HaveCount(1);

        var persistedRisks = await new ScanAndPersistAttritionRisksCommandHandler(aiMock, context).Handle(new ScanAndPersistAttritionRisksCommand(), CancellationToken.None);
        persistedRisks.Should().Be(1);
    }
}
