using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Common.Security;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Xunit;

namespace Vinamilk.HRM.UnitTests.TimeAttendance;

public class LeaveAndRequestCl4Tests
{
    private sealed class MockCurrentUser(int? userId, int? empId, string username = "tester") : ICurrentUserService
    {
        public int? UserId => userId;
        public string Username => username;
        public int? EmployeeId => empId;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "CL4-UnitTest";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permissionCode) => true;
    }

    private sealed class NoOpRealtimeDispatcher : IRealtimeNotificationDispatcher
    {
        public Task DispatchToUserAsync(int? recipientUserId, string title, string message, string notificationType, string? targetUrl = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task BroadcastWorkflowUpdateAsync(int workflowInstanceId, string entityType, int entityId, string status, string summaryMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpEmailService : IEmailNotificationService
    {
        public Task SendWorkflowEmailAsync(string toEmail, string recipientName, string subject, string documentTitle, string initiatorName, string currentStepName, string statusBadge, string actionUrl, string? comment = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPayslipEmailWithAttachmentAsync(string toEmail, string recipientName, string subject, string periodTitle, string htmlSummary, byte[] pdfAttachment, string attachmentFileName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public IReadOnlyList<SentEmailRecordDto> GetRecentSentEmails() => Array.Empty<SentEmailRecordDto>();
    }

    private sealed class InlineSender(Func<object, CancellationToken, Task<object?>> dispatch) : ISender
    {
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => (TResponse)(await dispatch(request, cancellationToken))!;

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => dispatch(request, cancellationToken);

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => dispatch(request, cancellationToken);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private static ApplicationDbContext CreateDb(string prefix)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"{prefix}_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(ApplicationDbContext Db, Employee Emp1, Employee Emp2, Employee Manager, AttendanceStatutoryParameter Statutory, AttendancePolicy Policy, WorkShift Shift)>
        SeedCl4CoreAsync(string prefix)
    {
        var db = CreateDb(prefix);

        db.Roles.AddRange(
            new Role { Id = 1, RoleCode = "HR_STAFF", RoleName = "Chuyên viên Nhân sự" },
            new Role { Id = 2, RoleCode = "HR_MANAGER", RoleName = "Trưởng phòng Nhân sự" },
            new Role { Id = 3, RoleCode = "EMPLOYEE", RoleName = "Nhân viên" });

        db.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, UnitCode = "NM_BD", UnitName = "Nhà máy Bình Dương", TreePath = "/1/", ManagerEmployeeId = 3, WageRegion = 1 },
            new OrganizationUnit { Id = 2, UnitCode = "KHO_SG", UnitName = "Kho vận Sài Gòn", TreePath = "/2/", ManagerEmployeeId = 3, WageRegion = 1 });

        db.Positions.Add(new Position { Id = 1, PositionCode = "NV", PositionName = "Nhân viên" });

        var mgr = new Employee
        {
            Id = 3,
            EmployeeCode = "VNM-MGR",
            FullName = "Trần Quản Lý",
            OrganizationUnitId = 1,
            PositionId = 1,
            HireDate = new DateOnly(2016, 1, 1),
            BaseSalary = 40_000_000m,
            WorkEmail = "mgr@vnm.vn",
            WorkStatus = "Official"
        };

        var emp1 = new Employee
        {
            Id = 1,
            EmployeeCode = "VNM-001",
            FullName = "Nguyễn Văn A",
            OrganizationUnitId = 1,
            PositionId = 1,
            DirectManagerId = 3,
            HireDate = new DateOnly(2020, 1, 1),
            BaseSalary = 26_000_000m,
            WorkEmail = "a@vnm.vn",
            WorkStatus = "Official"
        };

        var emp2 = new Employee
        {
            Id = 2,
            EmployeeCode = "VNM-002",
            FullName = "Lê Thị B",
            OrganizationUnitId = 2,
            PositionId = 1,
            DirectManagerId = 3,
            HireDate = new DateOnly(2025, 1, 1),
            BaseSalary = 15_600_000m,
            WorkEmail = "b@vnm.vn",
            WorkStatus = "Official"
        };

        db.Employees.AddRange(mgr, emp1, emp2);

        db.Users.AddRange(
            new User { Id = 10, Username = "nv.a", Email = "a@vnm.vn", PasswordHash = "hash", EmployeeId = 1, IsActive = true },
            new User { Id = 20, Username = "nv.b", Email = "b@vnm.vn", PasswordHash = "hash", EmployeeId = 2, IsActive = true },
            new User { Id = 30, Username = "hung.nv", Email = "mgr@vnm.vn", PasswordHash = "hash", EmployeeId = 3, IsActive = true },
            new User { Id = 40, Username = "huyen.ntt", Email = "chro@vnm.vn", PasswordHash = "hash", EmployeeId = null, IsActive = true });

        db.UserRoles.AddRange(
            new UserRole { UserId = 30, RoleId = 2 },
            new UserRole { UserId = 40, RoleId = 1 });

        var statutory = ApplicationDbContextInitializer.BuildDefaultAttendanceStatutoryParameters().First();
        await db.AttendanceStatutoryParameters.AddAsync(statutory);

        var policyGroup = ApplicationDbContextInitializer.BuildDefaultCompanyPolicyGroup();
        var policy = policyGroup.AttendancePolicies.First();
        await db.PolicyGroups.AddAsync(policyGroup);

        var shift = new WorkShift
        {
            Id = 1,
            ShiftCode = "HC_8H",
            ShiftName = "Ca Hành chính 08:00 - 17:00",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            BreakStartTime = new TimeOnly(12, 0),
            BreakEndTime = new TimeOnly(13, 0),
            StandardHours = 8.0m,
            WorkdayValue = 1.0m
        };
        await db.WorkShifts.AddAsync(shift);

        await db.LeaveTypes.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultLeaveTypes());
        await db.SaveChangesAsync();

        await WorkflowSeederHelper.EnsureDefaultWorkflowsAsync(db, CancellationToken.None);
        return (db, emp1, emp2, mgr, statutory, policy, shift);
    }

    private static ISender CreateWorkflowBridge(ApplicationDbContext db, ICurrentUserService user)
    {
        return new InlineSender(async (req, ct) =>
        {
            if (req is StartWorkflowInstanceCommand startCmd)
            {
                var h = new StartWorkflowInstanceCommandHandler(db, user, new NoOpRealtimeDispatcher(), new NoOpEmailService());
                return await h.Handle(startCmd, ct);
            }
            if (req is ProcessWorkflowActionCommand procCmd)
            {
                var h = new ProcessWorkflowActionCommandHandler(db, user, new NoOpRealtimeDispatcher(), new NoOpEmailService());
                return await h.Handle(procCmd, ct);
            }
            throw new NotSupportedException($"Unexpected request type: {req.GetType().Name}");
        });
    }

    [Fact]
    public void AnnualLeave_ShouldProRateMidYearHireAndTermination_WithSeniorityBonus()
    {
        var statutory = new AttendanceStatutoryParameter
        {
            StandardAnnualLeaveDays = 12m,
            SeniorityBonusYearsInterval = 5,
            SeniorityBonusDaysPerInterval = 1m
        };
        var policy = new AttendancePolicy { MidMonthHireCutoffDay = 15 };

        // Manual math 1: Hire 2026-07-10 (day 10 <= 15) -> counts July through Dec = 6 months.
        // 12 * 6 / 12 = 6.0 standard days, 0 seniority.
        var (sen1, std1, extra1) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(
            new DateOnly(2026, 7, 10), 2026, statutory, policy);
        sen1.Should().Be(0);
        std1.Should().Be(6.0m);
        extra1.Should().Be(0m);

        // Manual math 2: Hire 2026-07-20 (day 20 > 15) -> counts Aug through Dec = 5 months.
        // 12 * 5 / 12 = 5.0 standard days.
        var (_, std2, _) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(
            new DateOnly(2026, 7, 20), 2026, statutory, policy);
        std2.Should().Be(5.0m);

        // Manual math 3: Hire 2021-02-01, terminates 2026-06-20 -> 5 full years -> +1 seniority day;
        // months Jan..Jun (day 20 >= 15) = 6 months -> 12 * 6 / 12 = 6.0 standard days + 1.0 seniority.
        var (sen3, std3, extra3) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(
            new DateOnly(2021, 2, 1), 2026, statutory, policy, terminationDate: new DateOnly(2026, 6, 20));
        sen3.Should().Be(5);
        std3.Should().Be(6.0m);
        extra3.Should().Be(1.0m);
    }

    [Fact]
    public async Task CarryForwardExpiration_AndAdvanceLeave_ShouldExpireAfterCutoffAndRespectAdvanceLimit()
    {
        var (db, emp1, _, _, _, policy, _) = await SeedCl4CoreAsync("CL4_CarryAdvance");
        var annualType = await db.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "ANNUAL");

        var bal = new LeaveBalance
        {
            EmployeeId = emp1.Id,
            LeaveTypeId = annualType.Id,
            Year = 2026,
            StandardEntitlement = 12m,
            SeniorityExtraDays = 1m,
            CarriedForwardDays = 4m,
            UsedCarriedForwardDays = 1m,
            UsedDays = 12m, // 11 days of 2026 pool + 1 used carry-forward day. Total pool = 12 + 1 + 4 = 17 -> 5.0 left before expiry.
            CarryForwardExpiryDate = new DateOnly(2026, 3, 31)
        };
        await db.LeaveBalances.AddAsync(bal);
        await db.SaveChangesAsync();

        // Manual math before 31/03 (2026-03-20): validCarry in pool = 4.0, expired = 0, available = (12 + 1 + 4) - 12 = 5.0
        var beforeExpiry = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(bal, policy, new DateOnly(2026, 3, 20));
        beforeExpiry.ValidCarryForwardDays.Should().Be(4.0m);
        beforeExpiry.ExpiredCarryForwardDays.Should().Be(0m);
        beforeExpiry.TotalAvailableDays.Should().Be(5.0m);

        // Manual math after 31/03 (2026-04-05): unused carry (4 - 1 = 3.0) expires -> validCarry kept = 1.0, expired = 3.0, available = (12 + 1 + 1) - 12 = 2.0
        var afterExpiry = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(bal, policy, new DateOnly(2026, 4, 5));
        afterExpiry.ValidCarryForwardDays.Should().Be(1.0m);
        afterExpiry.ExpiredCarryForwardDays.Should().Be(3.0m);
        afterExpiry.TotalAvailableDays.Should().Be(2.0m);

        // Submit 3.0-day leave starting 2026-04-06 when AllowAdvanceAnnualLeave = false -> must fail (needs 3 > 2 remaining)
        var user = new MockCurrentUser(10, emp1.Id, "nv.a");
        var sender = CreateWorkflowBridge(db, user);
        var submitHandler = new SubmitLeaveRequestCommandHandler(db, sender, user);

        var blockedAct = async () => await submitHandler.Handle(new SubmitLeaveRequestCommand(
            emp1.Id, annualType.Id, new DateOnly(2026, 4, 6), new DateOnly(2026, 4, 8), "FullDay", "FullDay", null, "Nghỉ 3 ngày sau hạn phép tồn"), CancellationToken.None);
        await blockedAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*không đủ*");

        // Enable AllowAdvanceAnnualLeave = true, MaxAdvanceAnnualLeaveDays = 2 -> available becomes 2 + 2 = 4.0 -> 3.0 days succeeds!
        policy.AllowAdvanceAnnualLeave = true;
        policy.MaxAdvanceAnnualLeaveDays = 2m;
        await db.SaveChangesAsync();

        var leaveId = await submitHandler.Handle(new SubmitLeaveRequestCommand(
            emp1.Id, annualType.Id, new DateOnly(2026, 4, 6), new DateOnly(2026, 4, 8), "FullDay", "FullDay", null, "Ứng trước 1 ngày phép"), CancellationToken.None);
        leaveId.Should().BeGreaterThan(0);

        var updatedBal = await db.LeaveBalances.FirstAsync(b => b.Id == bal.Id);
        updatedBal.ExpiredCarryForwardDays.Should().Be(3.0m);
        updatedBal.PendingDays.Should().Be(3.0m);
    }

    [Fact]
    public async Task ConfigurableLeaveTypes_ShouldEnforceAttachment_MaxDays_HourlyMode_AndPreserveAnnualForMarriage()
    {
        var (db, emp1, _, _, _, _, _) = await SeedCl4CoreAsync("CL4_LeaveTypes");
        var annualType = await db.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "ANNUAL");
        var marriageType = await db.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "MARRIAGE");

        await db.LeaveBalances.AddAsync(new LeaveBalance
        {
            EmployeeId = emp1.Id,
            LeaveTypeId = annualType.Id,
            Year = 2026,
            StandardEntitlement = 12m,
            RemainingDays = 12m
        });
        await db.SaveChangesAsync();

        var user = new MockCurrentUser(10, emp1.Id, "nv.a");
        var sender = CreateWorkflowBridge(db, user);
        var handler = new SubmitLeaveRequestCommandHandler(db, sender, user);

        // 1. Marriage leave requires attachment
        var noAttachAct = async () => await handler.Handle(new SubmitLeaveRequestCommand(
            emp1.Id, marriageType.Id, new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 6), "FullDay", "FullDay", null, "Kết hôn"), CancellationToken.None);
        await noAttachAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*chứng từ*");

        // 2. Marriage max 3 days per request (4 days Mon-Thu should fail)
        var overMaxAct = async () => await handler.Handle(new SubmitLeaveRequestCommand(
            emp1.Id, marriageType.Id, new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 7), "FullDay", "FullDay", null, "Kết hôn", AttachmentUrl: "/docs/cert.pdf"), CancellationToken.None);
        await overMaxAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tối đa*");

        // 3. Valid 2-day marriage leave does NOT reduce ANNUAL pending/remaining
        var marriageReqId = await handler.Handle(new SubmitLeaveRequestCommand(
            emp1.Id, marriageType.Id, new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 5), "FullDay", "FullDay", null, "Nghỉ cưới", AttachmentUrl: "/docs/cert.pdf"), CancellationToken.None);
        marriageReqId.Should().BeGreaterThan(0);

        var balAfterMarriage = await db.LeaveBalances.FirstAsync(b => b.EmployeeId == emp1.Id && b.LeaveTypeId == annualType.Id && b.Year == 2026);
        balAfterMarriage.PendingDays.Should().Be(0m);
        balAfterMarriage.RemainingDays.Should().Be(12m);

        // 4. Hourly leave on ANNUAL (2 hours / 8 hours = 0.25 days)
        var hourlyReqId = await handler.Handle(new SubmitLeaveRequestCommand(
            emp1.Id, annualType.Id, new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 11), "FullDay", "FullDay", null, "Nghỉ 2 tiếng việc riêng",
            LeaveDurationMode: "Hours", RequestedHours: 2m), CancellationToken.None);

        var hourlyEntity = await db.LeaveRequests.FirstAsync(r => r.Id == hourlyReqId);
        hourlyEntity.TotalDays.Should().Be(0.25m);
    }

    [Fact]
    public void OvertimePolicyEngine_ShouldEnforceDailyMonthlyYearlyCaps_WarnNearThreshold_AndSupportSpecialIndustry300h()
    {
        var statutory = new AttendanceStatutoryParameter
        {
            MaxDailyOvertimeHours = 4m,
            MaxMonthlyOvertimeHours = 40m,
            MaxYearlyOvertimeHours = 200m,
            MaxSpecialIndustryYearlyOvertimeHours = 300m,
            LegalReference = "Điều 107 BLLĐ 2019"
        };
        var strictPolicy = new AttendancePolicy
        {
            BlockOvertimeWhenExceedingLimit = true,
            OvertimeWarningThresholdPercent = 85,
            UseSpecialIndustry300OtLimit = false
        };

        // 1. Daily > 4h blocked
        var dailyBlock = OvertimePolicyEngine.EvaluateLimits(5m, 0m, 0m, statutory, strictPolicy);
        dailyBlock.IsAllowed.Should().BeFalse();
        dailyBlock.IsBlocked.Should().BeTrue();

        // 2. Monthly 38 + 3 = 41 > 40 blocked
        var monthBlock = OvertimePolicyEngine.EvaluateLimits(3m, 38m, 38m, statutory, strictPolicy);
        monthBlock.IsBlocked.Should().BeTrue();

        // 3. Monthly 34 + 3 = 37 (92.5% >= 85% warn threshold), Year 170 + 3 = 173 (86.5% >= 85%) -> allowed with warning
        var warnOnly = OvertimePolicyEngine.EvaluateLimits(3m, 34m, 170m, statutory, strictPolicy);
        warnOnly.IsAllowed.Should().BeTrue();
        warnOnly.IsBlocked.Should().BeFalse();
        warnOnly.HasWarning.Should().BeTrue();

        // 4. Year 199 + 3 = 202 > 200 blocked under standard policy, allowed under special 300h policy
        var stdYearBlock = OvertimePolicyEngine.EvaluateLimits(3m, 0m, 199m, statutory, strictPolicy);
        stdYearBlock.IsBlocked.Should().BeTrue();

        strictPolicy.UseSpecialIndustry300OtLimit = true;
        var specialYearAllow = OvertimePolicyEngine.EvaluateLimits(3m, 0m, 199m, statutory, strictPolicy);
        specialYearAllow.IsAllowed.Should().BeTrue();
        specialYearAllow.EffectiveYearlyLimit.Should().Be(300m);

        // 5. BlockOvertimeWhenExceedingLimit = false -> exceeds cap only warns instead of blocking
        strictPolicy.BlockOvertimeWhenExceedingLimit = false;
        strictPolicy.UseSpecialIndustry300OtLimit = false;
        var softWarn = OvertimePolicyEngine.EvaluateLimits(5m, 40m, 200m, statutory, strictPolicy);
        softWarn.IsAllowed.Should().BeTrue();
        softWarn.IsBlocked.Should().BeFalse();
        softWarn.HasWarning.Should().BeTrue();
    }

    [Fact]
    public async Task GroupOvertime_ReconciliationWithAttendance_AndCompensatoryLeave_ShouldWorkEndToEnd()
    {
        var (db, emp1, emp2, mgr, _, _, shift) = await SeedCl4CoreAsync("CL4_GroupOtReconcile");
        var otDate = new DateOnly(2026, 6, 10);
        await db.ShiftSchedules.AddRangeAsync(
            new ShiftSchedule { EmployeeId = emp1.Id, WorkDate = otDate, WorkShiftId = shift.Id },
            new ShiftSchedule { EmployeeId = emp2.Id, WorkDate = otDate, WorkShiftId = shift.Id });
        await db.SaveChangesAsync();

        var requesterUser = new MockCurrentUser(10, emp1.Id, "nv.a");
        var mgrUser = new MockCurrentUser(30, mgr.Id, "hung.nv");
        var groupHandler = new SubmitGroupOvertimeRequestCommandHandler(db, CreateWorkflowBridge(db, requesterUser), requesterUser);

        // Both emp1 and emp2 register 2.0h OT (17:00 - 19:00) converting to CompensatoryLeave
        var groupRes = await groupHandler.Handle(new SubmitGroupOvertimeRequestCommand(
            new[] { emp1.Id, emp2.Id },
            otDate,
            new TimeOnly(17, 0),
            new TimeOnly(19, 0),
            "WeekdayDay",
            "CompensatoryLeave",
            "Kiểm kê cuối quý"), CancellationToken.None);

        groupRes.CreatedRequestIds.Should().HaveCount(2);

        // Manager approves both OT workflows
        var wfHandler = new ProcessWorkflowActionCommandHandler(db, mgrUser, new NoOpRealtimeDispatcher(), new NoOpEmailService());
        var wfIds = await db.WorkflowInstances.Where(w => w.EntityType == "OvertimeRequest").Select(w => w.Id).ToListAsync();
        foreach (var wfId in wfIds)
            await wfHandler.Handle(new ProcessWorkflowActionCommand(wfId, "Approve", "Duyệt OT nhóm"), CancellationToken.None);

        // Actual punches:
        // Emp1 left early at 18:30 -> actual OT = 1.5h (< 2.0h registered) -> MIN(2.0, 1.5) = 1.5h
        // Emp2 stayed until 20:00 -> actual OT = 3.0h (> 2.0h registered) -> MIN(2.0, 3.0) = 2.0h
        await db.AttendanceLogs.AddRangeAsync(
            new AttendanceLog { EmployeeId = emp1.Id, PunchTime = otDate.ToDateTime(new TimeOnly(7, 55)), PunchType = "CheckIn", SourceType = "GPS", GeofenceStatus = "InsideValid" },
            new AttendanceLog { EmployeeId = emp1.Id, PunchTime = otDate.ToDateTime(new TimeOnly(18, 30)), PunchType = "CheckOut", SourceType = "GPS", GeofenceStatus = "InsideValid" },
            new AttendanceLog { EmployeeId = emp2.Id, PunchTime = otDate.ToDateTime(new TimeOnly(7, 58)), PunchType = "CheckIn", SourceType = "GPS", GeofenceStatus = "InsideValid" },
            new AttendanceLog { EmployeeId = emp2.Id, PunchTime = otDate.ToDateTime(new TimeOnly(20, 0)), PunchType = "CheckOut", SourceType = "GPS", GeofenceStatus = "InsideValid" });
        await db.SaveChangesAsync();

        var reconcileHandler = new ReconcileOvertimeWithAttendanceCommandHandler(db, mgrUser);
        var rec1 = await reconcileHandler.Handle(new ReconcileOvertimeWithAttendanceCommand(6, 2026), CancellationToken.None);
        rec1.ReconciledCount.Should().Be(2);
        // Manual math: Emp1 = 1.5h, Emp2 = 2.0h => total credited = 3.5h
        rec1.TotalCreditedCompensatoryHours.Should().Be(3.5m);

        // Idempotent second run credits 0 additional delta
        var rec2 = await reconcileHandler.Handle(new ReconcileOvertimeWithAttendanceCommand(6, 2026), CancellationToken.None);
        rec2.TotalCreditedCompensatoryHours.Should().Be(0m);

        var annualType = await db.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "ANNUAL");
        var compLeaveType = await db.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "COMP_LEAVE");

        var emp1Bal = await db.LeaveBalances.FirstAsync(b => b.EmployeeId == emp1.Id && b.LeaveTypeId == annualType.Id && b.Year == 2026);
        emp1Bal.CompensatoryHoursBalance.Should().Be(1.5m);

        // Emp1 takes 1.5h COMP_LEAVE -> consumes CompensatoryHoursBalance down to 0 upon approval
        var submitLeave = new SubmitLeaveRequestCommandHandler(db, CreateWorkflowBridge(db, requesterUser), requesterUser);
        var compLeaveId = await submitLeave.Handle(new SubmitLeaveRequestCommand(
            emp1.Id, compLeaveType.Id, new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 12),
            "FullDay", "FullDay", null, "Nghỉ bù 1.5h", LeaveDurationMode: "Hours", RequestedHours: 1.5m), CancellationToken.None);

        var compWf = await db.WorkflowInstances.FirstAsync(w => w.EntityType == "LeaveRequest" && w.EntityId == compLeaveId);
        await wfHandler.Handle(new ProcessWorkflowActionCommand(compWf.Id, "Approve", "Duyệt nghỉ bù"), CancellationToken.None);

        var emp1BalAfterComp = await db.LeaveBalances.FirstAsync(b => b.EmployeeId == emp1.Id && b.LeaveTypeId == annualType.Id && b.Year == 2026);
        emp1BalAfterComp.CompensatoryHoursBalance.Should().Be(0m);
    }

    [Fact]
    public async Task AllRequestTypes_LateEarly_BusinessTrip_WFH_Explanation_AndShiftSwap_ShouldFlowThroughWorkflow()
    {
        var (db, emp1, emp2, mgr, _, _, shift) = await SeedCl4CoreAsync("CL4_AllRequests");
        var shift2 = new WorkShift
        {
            Id = 2,
            ShiftCode = "CA_CHIEU",
            ShiftName = "Ca Chiều",
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(22, 0),
            StandardHours = 8.0m,
            WorkdayValue = 1.0m
        };
        await db.WorkShifts.AddAsync(shift2);
        var swapDate = new DateOnly(2026, 6, 18);
        await db.ShiftSchedules.AddRangeAsync(
            new ShiftSchedule { EmployeeId = emp1.Id, WorkDate = swapDate, WorkShiftId = shift.Id },
            new ShiftSchedule { EmployeeId = emp2.Id, WorkDate = swapDate, WorkShiftId = shift2.Id });
        await db.SaveChangesAsync();

        var user1 = new MockCurrentUser(10, emp1.Id, "nv.a");
        var mgrUser = new MockCurrentUser(30, mgr.Id, "hung.nv");
        var sender = CreateWorkflowBridge(db, user1);

        var trHandler = new SubmitEmployeeTimeRequestCommandHandler(db, sender, user1);
        await trHandler.Handle(new SubmitEmployeeTimeRequestCommand(emp1.Id, "LateEarly", "Late", new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 15), new TimeOnly(8, 30), null, 30, null, null, "Kẹt xe"), CancellationToken.None);
        await trHandler.Handle(new SubmitEmployeeTimeRequestCommand(emp1.Id, "BusinessTrip", null, new DateOnly(2026, 6, 16), new DateOnly(2026, 6, 16), null, null, null, "Nhà máy Cần Thơ", null, "Khảo sát dây chuyền"), CancellationToken.None);
        await trHandler.Handle(new SubmitEmployeeTimeRequestCommand(emp1.Id, "RemoteWork", null, new DateOnly(2026, 6, 17), new DateOnly(2026, 6, 17), null, null, null, "Tại nhà", null, "Làm việc trực tuyến"), CancellationToken.None);
        await trHandler.Handle(new SubmitEmployeeTimeRequestCommand(emp1.Id, "AttendanceExplanation", "Quên chấm công", new DateOnly(2026, 6, 14), new DateOnly(2026, 6, 14), new TimeOnly(8, 0), new TimeOnly(17, 0), null, null, "/proof.jpg", "Máy chấm công bảo trì"), CancellationToken.None);

        var swapHandler = new SubmitShiftSwapRequestCommandHandler(db, user1, sender);
        var idSwap = await swapHandler.Handle(new SubmitShiftSwapRequestCommand(emp1.Id, emp2.Id, swapDate, swapDate, "Đổi ca khám sức khỏe"), CancellationToken.None);

        // All 5 have created workflow instances and are pending
        (await db.EmployeeTimeRequests.CountAsync(t => t.Status == "Pending")).Should().Be(4);
        var swapReq = await db.ShiftSwapRequests.FirstAsync(s => s.Id == idSwap);
        swapReq.Status.Should().Be("Pending");
        swapReq.WorkflowInstanceId.Should().NotBeNull();

        // Approve Step 1 for all 5 via DirectManager
        var wfHandlerMgr = new ProcessWorkflowActionCommandHandler(db, mgrUser, new NoOpRealtimeDispatcher(), new NoOpEmailService());
        var pendingWfs = await db.WorkflowInstances.Where(w => w.Status == "InProgress").ToListAsync();
        pendingWfs.Should().HaveCount(5);

        foreach (var wf in pendingWfs)
            await wfHandlerMgr.Handle(new ProcessWorkflowActionCommand(wf.Id, "Approve", "Quản lý trực tiếp đồng ý"), CancellationToken.None);

        // AttendanceExplanation requires Step 2 (C&B / HR_STAFF)
        var hrUser = new MockCurrentUser(40, null, "huyen.ntt");
        var wfHandlerHr = new ProcessWorkflowActionCommandHandler(db, hrUser, new NoOpRealtimeDispatcher(), new NoOpEmailService());
        var step2Wfs = await db.WorkflowInstances.Where(w => w.Status == "InProgress").ToListAsync();
        step2Wfs.Should().HaveCount(1);
        foreach (var wf in step2Wfs)
            await wfHandlerHr.Handle(new ProcessWorkflowActionCommand(wf.Id, "Approve", "C&B hậu kiểm đồng ý"), CancellationToken.None);

        (await db.EmployeeTimeRequests.CountAsync(t => t.Status == "Approved")).Should().Be(4);
        (await db.ShiftSwapRequests.FirstAsync(s => s.Id == idSwap)).Status.Should().Be("Approved");

        // Verify DailyAttendance symbols created for Trip ("CT"), WFH ("X" + "[WFH duyệt...]"), Explanation ("X" + IsMissedPunch=false) and shifts swapped!
        var attTrip = await db.DailyAttendances.FirstAsync(d => d.EmployeeId == emp1.Id && d.AttendanceDate == new DateOnly(2026, 6, 16));
        attTrip.AttendanceSymbol.Should().Be("CT");

        var attWfh = await db.DailyAttendances.FirstAsync(d => d.EmployeeId == emp1.Id && d.AttendanceDate == new DateOnly(2026, 6, 17));
        attWfh.AttendanceSymbol.Should().Be("X");
        attWfh.EditReason.Should().Contain("WFH");

        var attExplain = await db.DailyAttendances.FirstAsync(d => d.EmployeeId == emp1.Id && d.AttendanceDate == new DateOnly(2026, 6, 14));
        attExplain.AttendanceSymbol.Should().Be("X");
        attExplain.IsMissedPunch.Should().BeFalse();

        var emp1NewSched = await db.ShiftSchedules.FirstAsync(s => s.EmployeeId == emp1.Id && s.WorkDate == swapDate);
        emp1NewSched.WorkShiftId.Should().Be(shift2.Id);
    }

    [Fact]
    public async Task RevocationOfApprovedLeave_ShouldRestoreBalance_AndBlockWhenMonthIsLocked()
    {
        var (db, emp1, _, mgr, _, _, _) = await SeedCl4CoreAsync("CL4_Revocation");
        var annualType = await db.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "ANNUAL");

        await db.LeaveBalances.AddAsync(new LeaveBalance
        {
            EmployeeId = emp1.Id,
            LeaveTypeId = annualType.Id,
            Year = 2026,
            StandardEntitlement = 12m,
            RemainingDays = 12m
        });
        await db.SaveChangesAsync();

        var user1 = new MockCurrentUser(10, emp1.Id, "nv.a");
        var mgrUser = new MockCurrentUser(30, mgr.Id, "hung.nv");
        var senderUser = CreateWorkflowBridge(db, user1);

        var submitLeave = new SubmitLeaveRequestCommandHandler(db, senderUser, user1);
        var wfHandler = new ProcessWorkflowActionCommandHandler(db, mgrUser, new NoOpRealtimeDispatcher(), new NoOpEmailService());

        // Submit & approve 2-day leave (Mon 2026-06-08 to Tue 2026-06-09)
        var leaveId = await submitLeave.Handle(new SubmitLeaveRequestCommand(
            emp1.Id, annualType.Id, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 9), "FullDay", "FullDay", null, "Nghỉ việc gia đình"), CancellationToken.None);

        var leaveWf = await db.WorkflowInstances.FirstAsync(w => w.EntityType == "LeaveRequest" && w.EntityId == leaveId);
        await wfHandler.Handle(new ProcessWorkflowActionCommand(leaveWf.Id, "Approve", "OK"), CancellationToken.None);

        var balAfterApprove = await db.LeaveBalances.FirstAsync(b => b.EmployeeId == emp1.Id && b.LeaveTypeId == annualType.Id && b.Year == 2026);
        balAfterApprove.UsedDays.Should().Be(2m);
        balAfterApprove.RemainingDays.Should().Be(10m);

        // Request revocation of approved leave -> creates RequestCancellationRequest (WF_REQUEST_CANCEL)
        var cancelHandler = new CancelOrRequestRevocationCommandHandler(db, senderUser, user1);
        var cancelRes = await cancelHandler.Handle(new CancelOrRequestRevocationCommand("LeaveRequest", leaveId, "Hoãn lịch về quê"), CancellationToken.None);
        cancelRes.Mode.Should().Be("CancellationWorkflowStarted");
        cancelRes.CancellationRequestId.Should().NotBeNull();

        // Approve cancellation workflow -> restores 2 days back to RemainingDays = 12, UsedDays = 0
        var cancelWf = await db.WorkflowInstances.FirstAsync(w => w.EntityType == "RequestCancellationRequest" && w.EntityId == cancelRes.CancellationRequestId!.Value);
        await wfHandler.Handle(new ProcessWorkflowActionCommand(cancelWf.Id, "Approve", "Đồng ý thu hồi đơn đã duyệt"), CancellationToken.None);

        var leaveAfterRevoke = await db.LeaveRequests.FirstAsync(l => l.Id == leaveId);
        leaveAfterRevoke.Status.Should().Be("Cancelled");

        var balAfterRevoke = await db.LeaveBalances.FirstAsync(b => b.EmployeeId == emp1.Id && b.LeaveTypeId == annualType.Id && b.Year == 2026);
        balAfterRevoke.UsedDays.Should().Be(0m);
        balAfterRevoke.RemainingDays.Should().Be(12m);

        // Lock month 06/2026 and verify revoking another approved request in locked month is blocked!
        var leave2Id = await submitLeave.Handle(new SubmitLeaveRequestCommand(
            emp1.Id, annualType.Id, new DateOnly(2026, 6, 22), new DateOnly(2026, 6, 22), "FullDay", "FullDay", null, "Nghỉ 1 ngày"), CancellationToken.None);
        var leave2Wf = await db.WorkflowInstances.FirstAsync(w => w.EntityType == "LeaveRequest" && w.EntityId == leave2Id);
        await wfHandler.Handle(new ProcessWorkflowActionCommand(leave2Wf.Id, "Approve", "OK"), CancellationToken.None);

        await db.MonthlyAttendanceSummaries.AddAsync(new MonthlyAttendanceSummary
        {
            EmployeeId = emp1.Id,
            Month = 6,
            Year = 2026,
            IsLocked = true
        });
        await db.SaveChangesAsync();

        var lockedRevokeAct = async () => await cancelHandler.Handle(
            new CancelOrRequestRevocationCommand("LeaveRequest", leave2Id, "Xin thu hồi sau khi khóa"), CancellationToken.None);
        await lockedRevokeAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*khóa*");
    }

    [Fact]
    public async Task TerminationLeavePayout_ShouldCalculateUnusedDays_AndPushIntoMonthlyPayroll()
    {
        var (db, emp1, _, _, _, _, _) = await SeedCl4CoreAsync("CL4_TerminationPayroll");
        var annualType = await db.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "ANNUAL");

        // Payroll static context 2026
        var payrollParam = ApplicationDbContextInitializer.BuildDefaultStatutoryParameters()
            .First(p => p.EffectiveFrom >= new DateOnly(2026, 1, 1));
        await db.PayrollStatutoryParameters.AddAsync(payrollParam);
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets()
            .Where(b => b.EffectiveFrom >= new DateOnly(2026, 1, 1)));

        // Emp1 hired 2020-01-01, BaseSalary = 26,000,000, terminates 2026-06-30.
        // Manual math:
        // - Standard entitlement Jan..Jun 2026 (6 months) = 12 * 6 / 12 = 6.0 days
        // - Seniority on 2026-06-30: 6 years (2020 -> 2026) -> 6 / 5 = +1.0 extra day
        // - CarriedForward = 0, UsedDays = 2.0 -> UnusedPayableDays = (6.0 + 1.0 + 0) - 2.0 = 5.0 days
        // - Daily salary = 26,000,000 / 26 = 1,000,000 -> PayoutAmount = 5.0 * 1,000,000 = 5,000,000 VND!
        await db.LeaveBalances.AddAsync(new LeaveBalance
        {
            EmployeeId = emp1.Id,
            LeaveTypeId = annualType.Id,
            Year = 2026,
            StandardEntitlement = 12m,
            SeniorityExtraDays = 1m,
            UsedDays = 2m,
            RemainingDays = 11m
        });

        var period = new PayrollPeriod
        {
            Id = 50,
            PeriodCode = "2026-06",
            PeriodTitle = "Bảng lương tháng 06/2026",
            Year = 2026,
            Month = 6,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 30),
            StandardWorkdays = 26m,
            Status = PayrollPeriodStatuses.Draft
        };
        await db.PayrollPeriods.AddAsync(period);
        await db.MonthlyAttendanceSummaries.AddAsync(new MonthlyAttendanceSummary
        {
            EmployeeId = emp1.Id,
            Year = 2026,
            Month = 6,
            StandardWorkdays = 26m,
            ActualWorkdays = 26m,
            TotalPayableWorkdays = 26m
        });
        await db.SaveChangesAsync();

        var hrUser = new MockCurrentUser(40, null, "huyen.ntt");
        var payoutHandler = new CalculateAndPushTerminationLeavePayoutCommandHandler(db, hrUser);
        var settlement = await payoutHandler.Handle(new CalculateAndPushTerminationLeavePayoutCommand(
            emp1.Id,
            new DateOnly(2026, 6, 30),
            6,
            2026,
            26m), CancellationToken.None);

        settlement.UnusedPayableDays.Should().Be(5.0m);
        settlement.PayoutAmount.Should().Be(5_000_000m);
        settlement.Status.Should().Be("PushedToPayroll");

        // Now run payroll calculation for period 50 (06/2026). Even though emp1.WorkStatus became "Resigned",
        // CalculateMonthlyPayrollCommandHandler must still include emp1 and populate UnusedLeavePayout = 5,000,000.
        var payrollHandler = new CalculateMonthlyPayrollCommandHandler(db, hrUser);
        await payrollHandler.Handle(new CalculateMonthlyPayrollCommand(period.Id), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == period.Id && r.EmployeeId == emp1.Id);
        rec.UnusedLeavePayout.Should().Be(5_000_000m);
        // BaseSalary 26,000,000 + UnusedLeavePayout 5,000,000 = 31,000,000 GrossIncome
        rec.GrossIncome.Should().Be(31_000_000m);
    }

    [Fact]
    public async Task CompanyLeaveBalanceReportQuery_ShouldAggregateAcrossUnitsAndTotals()
    {
        var (db, emp1, emp2, _, _, _, _) = await SeedCl4CoreAsync("CL4_CompanyReport");
        var annualType = await db.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "ANNUAL");

        await db.LeaveBalances.AddRangeAsync(
            new LeaveBalance
            {
                EmployeeId = emp1.Id,
                LeaveTypeId = annualType.Id,
                Year = 2026,
                StandardEntitlement = 12m,
                SeniorityExtraDays = 1m,
                CarriedForwardDays = 3m,
                CarryForwardExpiryDate = new DateOnly(2026, 3, 31),
                UsedDays = 2m,
                CompensatoryHoursBalance = 4.5m,
                RemainingDays = 14m
            },
            new LeaveBalance
            {
                EmployeeId = emp2.Id,
                LeaveTypeId = annualType.Id,
                Year = 2026,
                StandardEntitlement = 12m,
                SeniorityExtraDays = 0m,
                CarriedForwardDays = 2m,
                CarryForwardExpiryDate = new DateOnly(2026, 3, 31),
                UsedDays = 1m,
                CompensatoryHoursBalance = 1.5m,
                RemainingDays = 13m
            });
        await db.SaveChangesAsync();

        var reportHandler = new GetCompanyLeaveBalanceReportQueryHandler(db);
        // Query as of 2026-04-10 (past 31/03 carry-forward expiry date)
        var report = await reportHandler.Handle(new GetCompanyLeaveBalanceReportQuery(2026, new DateOnly(2026, 4, 10)), CancellationToken.None);

        report.Rows.Should().HaveCount(2);
        report.UnitSummaries.Should().HaveCount(2);
        report.TotalCompensatoryHoursAllCompany.Should().Be(6.0m);
        // Expired carry: emp1 had 3 unused carry -> 3 expired; emp2 had 2 unused carry -> 2 expired -> total 5.0 expired
        report.TotalExpiredCarryDaysAllCompany.Should().Be(5.0m);
        // Remaining after expired carry: emp1 = 12+1-2 = 11, emp2 = 12-1 = 11 -> total 22.0 remaining
        report.TotalRemainingDaysAllCompany.Should().Be(22.0m);
    }
}
