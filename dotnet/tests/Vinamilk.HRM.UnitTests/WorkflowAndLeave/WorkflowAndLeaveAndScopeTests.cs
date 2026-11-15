using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Common.Security;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Xunit;

namespace Vinamilk.HRM.UnitTests.WorkflowAndLeave;

public class WorkflowAndLeaveAndScopeTests
{
    private sealed class MockCurrentUser(int? userId, int? empId, string username = "approver") : ICurrentUserService
    {
        public int? UserId => userId;
        public string Username => username;
        public int? EmployeeId => empId;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "UnitTest";
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

    [Fact]
    public void DataScopeEvaluator_ShouldIsolateDepartments_AndAllowHierarchyTree()
    {
        var deptAUser = new UserDataScopeContext(10, 100, OrganizationUnitId: 5, UnitTreePath: "/1/5/", DataScopeLevels.Department, new HashSet<int>());
        DataScopeEvaluator.CanAccessEmployee(deptAUser, 101, 5, "/1/5/").Should().BeTrue();   // Cùng phòng A
        DataScopeEvaluator.CanAccessEmployee(deptAUser, 202, 6, "/1/6/").Should().BeFalse();  // Khác phòng B -> bị chặn
        DataScopeEvaluator.CanAccessEmployee(deptAUser, 100, 6, "/1/6/").Should().BeTrue();   // Chính bản thân

        var divisionHead = new UserDataScopeContext(11, 102, OrganizationUnitId: 2, UnitTreePath: "/1/2/", DataScopeLevels.UnitTree, new HashSet<int> { 99 });
        DataScopeEvaluator.CanAccessEmployee(divisionHead, 300, 8, "/1/2/8/").Should().BeTrue();  // Đơn vị con của /1/2/
        DataScopeEvaluator.CanAccessEmployee(divisionHead, 301, 99, "/1/9/99/").Should().BeTrue(); // ExtraAllowedUnitIds
        DataScopeEvaluator.CanAccessEmployee(divisionHead, 302, 7, "/1/3/7/").Should().BeFalse(); // Khối khác
    }

    [Fact]
    public async Task WorkflowEngine_ShouldSupportMultiStepApprove_Reject_Delegate_AndBlockSelfApproval()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmWfTest_{Guid.NewGuid():N}").Options;
        await using var db = new ApplicationDbContext(options);

        db.OrganizationUnits.Add(new OrganizationUnit { Id = 1, UnitCode = "PB_A", UnitName = "Phòng A", ManagerEmployeeId = 2 });
        db.Positions.Add(new Position { Id = 1, PositionCode = "NV", PositionName = "Nhân viên" });
        db.Employees.AddRange(
            new Employee { Id = 1, EmployeeCode = "VNM-01", FullName = "Nguyễn Văn Gửi", OrganizationUnitId = 1, PositionId = 1, DirectManagerId = 2, WorkEmail = "gui@vnm.vn" },
            new Employee { Id = 2, EmployeeCode = "VNM-02", FullName = "Trần Quản Lý", OrganizationUnitId = 1, PositionId = 1, WorkEmail = "ql@vnm.vn" },
            new Employee { Id = 3, EmployeeCode = "VNM-03", FullName = "Lê Giám Đốc HR", OrganizationUnitId = 1, PositionId = 1, WorkEmail = "hr@vnm.vn" });
        db.Users.AddRange(
            new User { Id = 1, Username = "gui", Email = "gui@vnm.vn", EmployeeId = 1, IsActive = true },
            new User { Id = 2, Username = "ql", Email = "ql@vnm.vn", EmployeeId = 2, IsActive = true },
            new User { Id = 3, Username = "hr", Email = "hr@vnm.vn", EmployeeId = 3, IsActive = true });
        await db.SaveChangesAsync();

        // 1. Khởi tạo workflow cho đơn nghỉ phép >= 3 ngày (qua nhiều bước)
        var startHandler = new StartWorkflowInstanceCommandHandler(db, new MockCurrentUser(1, 1, "gui"), new NoOpRealtimeDispatcher(), new NoOpEmailService());
        var wfId = await startHandler.Handle(new StartWorkflowInstanceCommand("LeaveRequest", 10, 1, "Nghỉ phép 4 ngày", 4m), CancellationToken.None);
        wfId.Should().BeGreaterThan(0);

        // 2. Người gửi tự duyệt đơn của mình -> phải bị chặn
        var selfApproveHandler = new ProcessWorkflowActionCommandHandler(db, new MockCurrentUser(1, 1, "gui"), new NoOpRealtimeDispatcher(), new NoOpEmailService());
        Func<Task> selfAct = () => selfApproveHandler.Handle(new ProcessWorkflowActionCommand(wfId, "Approved", "Tự duyệt"), CancellationToken.None);
        await selfAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tự phê duyệt*");

        // 3. Quản lý ủy quyền cho User 3
        var managerHandler = new ProcessWorkflowActionCommandHandler(db, new MockCurrentUser(2, 2, "ql"), new NoOpRealtimeDispatcher(), new NoOpEmailService());
        var delMsg = await managerHandler.Handle(new ProcessWorkflowActionCommand(wfId, "Delegated", "Đi công tác", DelegateToUserId: 3), CancellationToken.None);
        delMsg.Should().Contain("ủy quyền");

        // 4. Người được ủy quyền duyệt bước 1 -> chuyển bước tiếp hoặc hoàn tất; duyệt hết các bước
        var delegateHandler = new ProcessWorkflowActionCommandHandler(db, new MockCurrentUser(3, 3, "hr"), new NoOpRealtimeDispatcher(), new NoOpEmailService());
        await delegateHandler.Handle(new ProcessWorkflowActionCommand(wfId, "Approved", "Đồng ý bước 1"), CancellationToken.None);
        var inst = await db.WorkflowInstances.FirstAsync(w => w.Id == wfId);
        if (inst.Status == "InProgress")
        {
            await delegateHandler.Handle(new ProcessWorkflowActionCommand(wfId, "Approved", "Đồng ý bước 2"), CancellationToken.None);
        }
        (await db.WorkflowInstances.FirstAsync(w => w.Id == wfId)).Status.Should().Be("Approved");

        // 5. Tạo đơn mới và kiểm tra Từ chối (Rejected)
        var wfRejectId = await startHandler.Handle(new StartWorkflowInstanceCommand("LeaveRequest", 11, 1, "Đơn khác", 1m), CancellationToken.None);
        var rejMsg = await managerHandler.Handle(new ProcessWorkflowActionCommand(wfRejectId, "Rejected", "Không đủ nhân sự trực"), CancellationToken.None);
        rejMsg.Should().Contain("từ chối");
        (await db.WorkflowInstances.FirstAsync(w => w.Id == wfRejectId)).Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task LeaveManagement_ShouldCalculateSeniorityAndCarryForward_AndBlockOverBalanceRequest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmLeaveTest_{Guid.NewGuid():N}").Options;
        await using var db = new ApplicationDbContext(options);

        db.OrganizationUnits.Add(new OrganizationUnit { Id = 1, UnitCode = "PB_A", UnitName = "Phòng A" });
        db.Positions.Add(new Position { Id = 1, PositionCode = "NV", PositionName = "Nhân viên" });
        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "VNM-200",
            FullName = "Phạm Thâm Niên",
            OrganizationUnitId = 1,
            PositionId = 1,
            HireDate = new DateOnly(2014, 1, 1), // 12 năm -> +2 ngày thâm niên
            WorkStatus = "Official"
        });
        db.AttendanceStatutoryParameters.AddRange(ApplicationDbContextInitializer.BuildDefaultAttendanceStatutoryParameters());
        db.LeaveTypes.Add(new LeaveType { Id = 1, LeaveTypeCode = "ANNUAL", LeaveTypeName = "Nghỉ phép năm", CountsAgainstAnnualLeave = true, MaxDaysPerYear = 12m, AllowCarryForward = true });
        db.LeaveBalances.Add(new LeaveBalance
        {
            Id = 1,
            EmployeeId = 1,
            LeaveTypeId = 1,
            Year = 2026,
            StandardEntitlement = 12m,
            SeniorityExtraDays = 0m,
            CarriedForwardDays = 4m,
            UsedCarriedForwardDays = 4m,
            UsedDays = 14m,
            PendingDays = 0m,
            RemainingDays = 2m
        });
        await db.SaveChangesAsync();

        // 1. Tính lại phép năm 2026 theo thâm niên + phép tồn tối đa 5 ngày
        var recalcCount = await new RecalculateAnnualLeaveEntitlementCommandHandler(db)
            .Handle(new RecalculateAnnualLeaveEntitlementCommand(2026, DefaultCarryForwardCap: 5m), CancellationToken.None);
        recalcCount.Should().Be(1);

        var bal = await db.LeaveBalances.FirstAsync(b => b.EmployeeId == 1);
        bal.SeniorityExtraDays.Should().Be(2m); // 12 + 2 + 4 - 14 = 4 ngày còn lại
        bal.RemainingDays.Should().Be(4m);

        // 2. Đăng ký nghỉ 6 ngày làm việc (vượt 4 ngày còn lại) -> phải bị chặn
        var sender = new InlineSender((req, ct) => Task.FromResult<object?>(999));
        var submitHandler = new SubmitLeaveRequestCommandHandler(db, sender);

        Func<Task> overAct = () => submitHandler.Handle(new SubmitLeaveRequestCommand(
            1, 1, new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 21), "FullDay", "FullDay", null, "Nghỉ du lịch"), CancellationToken.None);
        await overAct.Should().ThrowAsync<InvalidOperationException>().WithMessage("*không đủ*");

        // 3. Đăng ký nghỉ hợp lệ 2 ngày -> thành công và trừ RemainingDays
        var leaveId = await submitHandler.Handle(new SubmitLeaveRequestCommand(
            1, 1, new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 17), "FullDay", "FullDay", null, "Việc gia đình"), CancellationToken.None);
        leaveId.Should().BeGreaterThan(0);

        var updatedBal = await db.LeaveBalances.FirstAsync(b => b.EmployeeId == 1);
        updatedBal.PendingDays.Should().Be(2m);
        updatedBal.RemainingDays.Should().Be(2m);
    }
}

