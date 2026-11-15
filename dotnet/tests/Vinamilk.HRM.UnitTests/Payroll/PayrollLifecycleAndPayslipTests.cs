using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSharp.Pdf.IO;
using QuestPDF.Infrastructure;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Vinamilk.HRM.Infrastructure.Services;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Payroll;

public class PayrollLifecycleAndPayslipTests
{
    private static IPiiEncryptionService CreateEncryptionService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:PiiEncryptionKey"] = "UnitTest_SecretKey_32Chars_Alpha!"
            })
            .Build();
        return new PiiEncryptionService(config);
    }

    private static PayrollDocumentService CreateDocumentService(IPiiEncryptionService encryptionService)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["QuestPDF:LicenseType"] = "Community"
            })
            .Build();
        return new PayrollDocumentService(encryptionService, config);
    }

    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? $"VnmPayrollLifecycle_{Guid.NewGuid():N}")
            .Options;
        var db = new ApplicationDbContext(options);

        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());

        db.Roles.AddRange(
            new Role { Id = 1, RoleCode = "SUPER_ADMIN", RoleName = "Super Admin", IsSystemRole = true },
            new Role { Id = 2, RoleCode = "HR_DIRECTOR", RoleName = "Giám đốc Nhân sự", IsSystemRole = true },
            new Role { Id = 3, RoleCode = "HR_MANAGER", RoleName = "Trưởng phòng Nhân sự", IsSystemRole = true },
            new Role { Id = 4, RoleCode = "DEPARTMENT_MANAGER", RoleName = "Trưởng đơn vị", IsSystemRole = true });

        db.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, UnitCode = "NM_SG", UnitName = "Nhà máy Sữa Sài Gòn", WageRegion = 1, IsActive = true },
            new OrganizationUnit { Id = 2, UnitCode = "VP_HN", UnitName = "Chi nhánh Hà Nội", WageRegion = 1, IsActive = true });

        db.Positions.Add(new Position { Id = 1, PositionCode = "SPEC", PositionName = "Chuyên viên", IsActive = true });
        await db.SaveChangesAsync();

        await WorkflowSeederHelper.EnsureDefaultWorkflowsAsync(db, CancellationToken.None);
        return db;
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        public int? UserId { get; init; }
        public string Username { get; init; } = "tester";
        public int? EmployeeId { get; init; }
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "xUnit";
        public bool IsAuthenticated => true;
        public bool AllowAllPermissions { get; init; } = true;
        public bool HasPermission(string permissionCode) => AllowAllPermissions;
    }

    private sealed class CapturingEmailNotificationService : IEmailNotificationService
    {
        public List<(string To, string Subject, string FileName, int PdfLength)> SentEmails { get; } = new();

        public Task SendWorkflowEmailAsync(
            string toEmail,
            string recipientName,
            string subject,
            string documentTitle,
            string initiatorName,
            string currentStepName,
            string statusBadge,
            string actionUrl,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPayslipEmailWithAttachmentAsync(
            string toEmail,
            string recipientName,
            string subject,
            string periodTitle,
            string htmlSummary,
            byte[] pdfAttachment,
            string attachmentFileName,
            CancellationToken cancellationToken = default)
        {
            SentEmails.Add((toEmail, periodTitle, attachmentFileName, pdfAttachment.Length));
            return Task.CompletedTask;
        }

        public IReadOnlyList<SentEmailRecordDto> GetRecentSentEmails() => Array.Empty<SentEmailRecordDto>();
    }

    private sealed class NoOpRealtimeDispatcher : IRealtimeNotificationDispatcher
    {
        public Task DispatchToUserAsync(int? recipientUserId, string title, string message, string notificationType, string? targetUrl = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task BroadcastWorkflowUpdateAsync(int workflowInstanceId, string entityType, int entityId, string status, string summaryMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class TestWorkflowSender : ISender
    {
        private readonly IApplicationDbContext _db;
        private readonly Func<ICurrentUserService> _userAccessor;

        public TestWorkflowSender(IApplicationDbContext db, Func<ICurrentUserService> userAccessor)
        {
            _db = db;
            _userAccessor = userAccessor;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var rt = new NoOpRealtimeDispatcher();
            var email = new CapturingEmailNotificationService();

            object? result = request switch
            {
                StartWorkflowInstanceCommand startCmd => await new StartWorkflowInstanceCommandHandler(
                        _db,
                        _userAccessor(),
                        rt,
                        email)
                    .Handle(startCmd, cancellationToken),
                ProcessWorkflowActionCommand procCmd => await new ProcessWorkflowActionCommandHandler(
                        _db,
                        _userAccessor(),
                        rt,
                        email)
                    .Handle(procCmd, cancellationToken),
                _ => throw new NotSupportedException($"Unsupported command {request.GetType().Name}")
            };

            return (TResponse)result!;
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    [Fact]
    public async Task PayrollLifecycle_ShouldEnforceStateMachine_SeparationOfDuties_UnlockAudit_AndPaidGuard()
    {
        await using var db = await CreateSeededDbContextAsync();
        var enc = CreateEncryptionService();

        db.Employees.AddRange(
            new Employee
            {
                Id = 1,
                EmployeeCode = "VNM-001",
                FullName = "Nguyễn Văn Tính",
                OrganizationUnitId = 1,
                PositionId = 1,
                BaseSalary = 25_000_000m,
                WorkEmail = "tinh.nv@vinamilk.com",
                CitizenIdEncrypted = enc.Encrypt("079095001234"),
                BankAccountEncrypted = enc.Encrypt("190012345678"),
                BankName = "Vietcombank",
                WorkStatus = "Official"
            },
            new Employee
            {
                Id = 2,
                EmployeeCode = "VNM-002",
                FullName = "Trần Thị Duyệt",
                OrganizationUnitId = 2,
                PositionId = 1,
                BaseSalary = 35_000_000m,
                WorkEmail = "duyet.tt@vinamilk.com",
                CitizenIdEncrypted = enc.Encrypt("079096009876"),
                BankAccountEncrypted = enc.Encrypt("190098765432"),
                BankName = "Techcombank",
                WorkStatus = "Official"
            });

        db.Users.AddRange(
            new User { Id = 10, Username = "cb_calc", Email = "calc@vinamilk.com", PasswordHash = "hash", EmployeeId = 1, IsActive = true },
            new User { Id = 20, Username = "hr_dir", Email = "hrdir@vinamilk.com", PasswordHash = "hash", EmployeeId = 2, IsActive = true });
        db.UserRoles.AddRange(
            new UserRole { UserId = 10, RoleId = 3 },
            new UserRole { UserId = 20, RoleId = 2 });

        db.MonthlyAttendanceSummaries.AddRange(
            new MonthlyAttendanceSummary { Id = 1, EmployeeId = 1, Month = 9, Year = 2026, StandardWorkdays = 26m, TotalPayableWorkdays = 26m },
            new MonthlyAttendanceSummary { Id = 2, EmployeeId = 2, Month = 9, Year = 2026, StandardWorkdays = 26m, TotalPayableWorkdays = 26m });
        await db.SaveChangesAsync();

        var calcUser = new StubCurrentUser { UserId = 10, Username = "cb_calc", EmployeeId = 1 };
        var approverUser = new StubCurrentUser { UserId = 20, Username = "hr_dir", EmployeeId = 2 };
        ICurrentUserService activeActor = calcUser;
        var wfSender = new TestWorkflowSender(db, () => activeActor);

        var periodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 9, 26m), CancellationToken.None);

        // Chưa tính lương (Draft) -> không thể Submit / Lock
        Func<Task> submitDraft = () => new SubmitPayrollPeriodCommandHandler(db, calcUser, wfSender)
            .Handle(new SubmitPayrollPeriodCommand(periodId), CancellationToken.None);
        await submitDraft.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Calculated*");

        Func<Task> lockDraft = () => new LockPayrollPeriodCommandHandler(db, calcUser)
            .Handle(new LockPayrollPeriodCommand(periodId), CancellationToken.None);
        await lockDraft.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Approved*");

        // 1. Draft -> Calculated
        var calcCount = await new CalculateMonthlyPayrollCommandHandler(db, calcUser)
            .Handle(new CalculateMonthlyPayrollCommand(periodId), CancellationToken.None);
        calcCount.Should().Be(2);
        (await db.PayrollPeriods.FirstAsync(p => p.Id == periodId)).Status.Should().Be(PayrollPeriodStatuses.Calculated);

        // 2. Calculated -> Submitted (tạo WorkflowInstance)
        var wfId = await new SubmitPayrollPeriodCommandHandler(db, calcUser, wfSender)
            .Handle(new SubmitPayrollPeriodCommand(periodId, "Kính trình phê duyệt kỳ T09/2026"), CancellationToken.None);
        wfId.Should().BeGreaterThan(0);

        var submittedPeriod = await db.PayrollPeriods.FirstAsync(p => p.Id == periodId);
        submittedPeriod.Status.Should().Be(PayrollPeriodStatuses.Submitted);
        submittedPeriod.WorkflowInstanceId.Should().Be(wfId);

        // Người tính KHÔNG được tự duyệt
        Func<Task> selfApprove = () => new ApprovePayrollPeriodCommandHandler(db, calcUser, wfSender)
            .Handle(new ApprovePayrollPeriodCommand(periodId, Approve: true), CancellationToken.None);
        await selfApprove.Should().ThrowAsync<InvalidOperationException>().WithMessage("*phân tách nhiệm vụ*");

        // Người dùng KHÔNG có UserId (null hoặc <= 0) KHÔNG được phép phê duyệt kỳ lương
        var nullUser = new StubCurrentUser { UserId = null, Username = "anonymous", EmployeeId = 999 };
        Func<Task> approveWithNullUser = () => new ApprovePayrollPeriodCommandHandler(db, nullUser, wfSender)
            .Handle(new ApprovePayrollPeriodCommand(periodId, Approve: true), CancellationToken.None);
        await approveWithNullUser.Should().ThrowAsync<InvalidOperationException>().WithMessage("*UserId trống*");

        Func<Task> wfApproveWithNullUser = () => new ProcessWorkflowActionCommandHandler(
                db,
                nullUser,
                new NoOpRealtimeDispatcher(),
                new CapturingEmailNotificationService())
            .Handle(new ProcessWorkflowActionCommand(wfId, "Approved", "Thử duyệt không UserId"), CancellationToken.None);
        await wfApproveWithNullUser.Should().ThrowAsync<InvalidOperationException>().WithMessage("*UserId trống*");

        // Trả lại (Reject) -> quay về Calculated
        activeActor = approverUser;
        await new ApprovePayrollPeriodCommandHandler(db, approverUser, wfSender)
            .Handle(new ApprovePayrollPeriodCommand(periodId, Approve: false, Comment: "Kiểm tra lại phụ cấp"), CancellationToken.None);
        (await db.PayrollPeriods.FirstAsync(p => p.Id == periodId)).Status.Should().Be(PayrollPeriodStatuses.Calculated);

        // Trình lại -> Phê duyệt (Approved)
        activeActor = calcUser;
        await new SubmitPayrollPeriodCommandHandler(db, calcUser, wfSender)
            .Handle(new SubmitPayrollPeriodCommand(periodId), CancellationToken.None);
        activeActor = approverUser;
        await new ApprovePayrollPeriodCommandHandler(db, approverUser, wfSender)
            .Handle(new ApprovePayrollPeriodCommand(periodId, Approve: true, Comment: "Đồng ý chi lương"), CancellationToken.None);

        var approvedPeriod = await db.PayrollPeriods.FirstAsync(p => p.Id == periodId);
        approvedPeriod.Status.Should().Be(PayrollPeriodStatuses.Approved);

        // 3. Approved -> Locked
        await new LockPayrollPeriodCommandHandler(db, approverUser)
            .Handle(new LockPayrollPeriodCommand(periodId), CancellationToken.None);
        var lockedPeriod = await db.PayrollPeriods.FirstAsync(p => p.Id == periodId);
        lockedPeriod.Status.Should().Be(PayrollPeriodStatuses.Locked);
        lockedPeriod.IsLocked.Should().BeTrue();

        // Mở khóa không có lý do -> bị chặn
        Func<Task> unlockEmptyReason = () => new UnlockPayrollPeriodCommandHandler(db, approverUser)
            .Handle(new UnlockPayrollPeriodCommand(periodId, "   "), CancellationToken.None);
        await unlockEmptyReason.Should().ThrowAsync<InvalidOperationException>().WithMessage("*lý do*");

        // Mở khóa có lý do -> về Calculated + AuditLog
        await new UnlockPayrollPeriodCommandHandler(db, approverUser)
            .Handle(new UnlockPayrollPeriodCommand(periodId, "Bổ sung thưởng sáng kiến T09"), CancellationToken.None);
        var unlockedPeriod = await db.PayrollPeriods.FirstAsync(p => p.Id == periodId);
        unlockedPeriod.Status.Should().Be(PayrollPeriodStatuses.Calculated);
        unlockedPeriod.IsLocked.Should().BeFalse();
        unlockedPeriod.LastUnlockReason.Should().Be("Bổ sung thưởng sáng kiến T09");
        (await db.AuditLogs.AnyAsync(a => a.ActionType == "PayrollPeriod_Unlock")).Should().BeTrue();

        // Trình -> Duyệt -> Khóa -> Xác nhận Đã chi trả (Paid)
        activeActor = calcUser;
        await new SubmitPayrollPeriodCommandHandler(db, calcUser, wfSender)
            .Handle(new SubmitPayrollPeriodCommand(periodId), CancellationToken.None);
        activeActor = approverUser;
        await new ApprovePayrollPeriodCommandHandler(db, approverUser, wfSender)
            .Handle(new ApprovePayrollPeriodCommand(periodId, Approve: true), CancellationToken.None);
        await new LockPayrollPeriodCommandHandler(db, approverUser)
            .Handle(new LockPayrollPeriodCommand(periodId), CancellationToken.None);
        await new MarkPayrollPeriodPaidCommandHandler(db, approverUser)
            .Handle(new MarkPayrollPeriodPaidCommand(periodId, "UNC-VCB-092026"), CancellationToken.None);

        var paidPeriod = await db.PayrollPeriods.FirstAsync(p => p.Id == periodId);
        paidPeriod.Status.Should().Be(PayrollPeriodStatuses.Paid);
        (await db.PayrollRecords.AllAsync(r => r.PaymentStatus == PayrollPeriodStatuses.Paid)).Should().BeTrue();

        // Đã Paid -> không thể mở khóa nữa
        Func<Task> unlockPaid = () => new UnlockPayrollPeriodCommandHandler(db, approverUser)
            .Handle(new UnlockPayrollPeriodCommand(periodId, "Thử mở kỳ đã chi"), CancellationToken.None);
        await unlockPaid.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Paid*");
    }

    [Fact]
    public async Task PayrollRecalculation_SingleEmployeeAndFullPeriod_ShouldTrackVersionsAndHighVarianceWarningOver20Percent()
    {
        await using var db = await CreateSeededDbContextAsync();

        db.Employees.AddRange(
            new Employee { Id = 1, EmployeeCode = "VNM-010", FullName = "Lê Văn Tăng", OrganizationUnitId = 1, PositionId = 1, BaseSalary = 20_000_000m, WorkStatus = "Official" },
            new Employee { Id = 2, EmployeeCode = "VNM-011", FullName = "Phạm Thị Ổn", OrganizationUnitId = 1, PositionId = 1, BaseSalary = 20_000_000m, WorkStatus = "Official" });

        // Kỳ tháng 7/2026: cả 2 nhân viên hưởng lương chuẩn
        db.MonthlyAttendanceSummaries.AddRange(
            new MonthlyAttendanceSummary { Id = 1, EmployeeId = 1, Month = 7, Year = 2026, StandardWorkdays = 26m, TotalPayableWorkdays = 26m },
            new MonthlyAttendanceSummary { Id = 2, EmployeeId = 2, Month = 7, Year = 2026, StandardWorkdays = 26m, TotalPayableWorkdays = 26m },
            // Kỳ tháng 8/2026: NV1 tăng ca rất nhiều (lệch > 20%), NV2 giữ nguyên
            new MonthlyAttendanceSummary { Id = 3, EmployeeId = 1, Month = 8, Year = 2026, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, TotalOtHours150 = 60m },
            new MonthlyAttendanceSummary { Id = 4, EmployeeId = 2, Month = 8, Year = 2026, StandardWorkdays = 26m, TotalPayableWorkdays = 26m, TotalOtHours150 = 0m });
        await db.SaveChangesAsync();

        var user = new StubCurrentUser { UserId = 5, Username = "cb_admin", EmployeeId = 1 };
        var p7 = await new CreatePayrollPeriodCommandHandler(db).Handle(new CreatePayrollPeriodCommand(2026, 7, 26m), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, user).Handle(new CalculateMonthlyPayrollCommand(p7), CancellationToken.None);

        var p8 = await new CreatePayrollPeriodCommandHandler(db).Handle(new CreatePayrollPeriodCommand(2026, 8, 26m), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, user).Handle(new CalculateMonthlyPayrollCommand(p8), CancellationToken.None);

        var rec1 = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == p8 && r.EmployeeId == 1);
        var rec2 = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == p8 && r.EmployeeId == 2);

        rec1.HasHighVarianceWarning.Should().BeTrue("Lương thực nhận NV1 tăng > 20% so với kỳ tháng 7");
        rec1.VariancePercentVsPreviousPeriod.Should().BeGreaterThan(20m);
        rec2.HasHighVarianceWarning.Should().BeFalse();
        rec2.VariancePercentVsPreviousPeriod.Should().Be(0m);

        var prevNetRec1 = rec1.NetSalary;
        var prevNetRec2 = rec2.NetSalary;

        // Cập nhật OT tháng 8 của NV1 và tính lại RIÊNG cho NV1
        var att1 = await db.MonthlyAttendanceSummaries.FirstAsync(a => a.Id == 3);
        att1.TotalOtHours200 = 12m;
        await db.SaveChangesAsync();

        var singleCount = await new CalculateMonthlyPayrollCommandHandler(db, user)
            .Handle(new CalculateMonthlyPayrollCommand(p8, EmployeeId: 1, Notes: "Bổ sung OT chủ nhật NV1"), CancellationToken.None);
        singleCount.Should().Be(1);

        var updatedRec1 = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == p8 && r.EmployeeId == 1);
        var untouchedRec2 = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == p8 && r.EmployeeId == 2);

        updatedRec1.PreviousRevisionNetSalary.Should().Be(prevNetRec1);
        updatedRec1.NetSalary.Should().BeGreaterThan(prevNetRec1);
        untouchedRec2.NetSalary.Should().Be(prevNetRec2);

        var revisions = await db.PayrollCalculationRevisions
            .Where(v => v.PayrollPeriodId == p8)
            .OrderBy(v => v.RevisionNumber)
            .ToListAsync();
        revisions.Should().HaveCount(2);
        revisions[1].Scope.Should().Be("SingleEmployee");
        revisions[1].EmployeeId.Should().Be(1);
        revisions[1].NetDeltaFromPreviousRevision.Should().Be(updatedRec1.NetSalary - prevNetRec1);
    }

    [Fact]
    public async Task PayslipPdf_ShouldBeEncryptedWithLast6CccdDigits_FailWithoutOrWrongPassword_AndEnforceOwnership()
    {
        await using var db = await CreateSeededDbContextAsync();
        var enc = CreateEncryptionService();

        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "VNM-777",
            FullName = "Nguyễn Thị Lan Hương",
            OrganizationUnitId = 1,
            PositionId = 1,
            BaseSalary = 80_000_000m,
            PositionAllowance = 5_000_000m,
            CitizenIdEncrypted = enc.Encrypt("079090654321"), // 6 số cuối: 654321
            BankAccountEncrypted = enc.Encrypt("0071000889911"),
            BankName = "Vietcombank",
            WorkStatus = "Official"
        });
        db.EmployeeDependents.Add(new EmployeeDependent
        {
            Id = 1,
            EmployeeId = 1,
            FullName = "Nguyễn Bảo An",
            Relationship = "Child",
            IsTaxDeductible = true
        });
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            Id = 1,
            EmployeeId = 1,
            Month = 10,
            Year = 2026,
            StandardWorkdays = 26m,
            TotalPayableWorkdays = 26m,
            TotalOtHours150 = 8m
        });
        await db.SaveChangesAsync();

        var adminUser = new StubCurrentUser { UserId = 1, Username = "admin", EmployeeId = 99 };
        var periodId = await new CreatePayrollPeriodCommandHandler(db).Handle(new CreatePayrollPeriodCommand(2026, 10, 26m), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, adminUser).Handle(new CalculateMonthlyPayrollCommand(periodId), CancellationToken.None);

        var rec = await db.PayrollRecords.FirstAsync(r => r.EmployeeId == 1);
        rec.PitBracketsJson.Should().NotBeNullOrWhiteSpace();
        var slices = JsonSerializer.Deserialize<List<PitBracketTaxSliceDto>>(rec.PitBracketsJson!);
        slices.Should().NotBeNull();
        slices!.Count.Should().BeGreaterThanOrEqualTo(3, "Thu nhập cao áp nhiều bậc thuế TNCN 2026");

        var docService = CreateDocumentService(enc);
        var ownerUser = new StubCurrentUser { UserId = 2, Username = "huong.ntl", EmployeeId = 1 };
        var handler = new GetPayrollRecordPdfQueryHandler(db, ownerUser, docService);

        var pdfResult = await handler.Handle(new GetPayrollRecordPdfQuery(rec.Id, EnforceSelfOnly: true), CancellationToken.None);
        pdfResult.IsPasswordProtected.Should().BeTrue();
        pdfResult.PdfBytes.Length.Should().BeGreaterThan(1000);

        // Mở KHÔNG mật khẩu -> phải lỗi PdfReaderException
        Action openWithoutPassword = () =>
        {
            using var msNoPass = new MemoryStream(pdfResult.PdfBytes);
            using var _ = PdfReader.Open(msNoPass, PdfDocumentOpenMode.Import);
        };
        openWithoutPassword.Should().Throw<PdfReaderException>();

        // Mở SAI mật khẩu -> phải lỗi PdfReaderException
        Action openWrongPassword = () =>
        {
            using var msWrong = new MemoryStream(pdfResult.PdfBytes);
            using var _ = PdfReader.Open(msWrong, "000000", PdfDocumentOpenMode.Import);
        };
        openWrongPassword.Should().Throw<PdfReaderException>();

        // Mở bằng OwnerPassword cũ dễ đoán (<userPassword>_VNM_OWNER) -> phải lỗi PdfReaderException
        Action openPredictableOwnerPass = () =>
        {
            using var msOldOwner = new MemoryStream(pdfResult.PdfBytes);
            using var _ = PdfReader.Open(msOldOwner, "654321_VNM_OWNER", PdfDocumentOpenMode.Import);
        };
        openPredictableOwnerPass.Should().Throw<PdfReaderException>();

        // OwnerPassword được sinh CSPRNG ngẫu nhiên mỗi lần gọi
        var ownerPass1 = PayrollDocumentService.GenerateRandomOwnerPassword();
        var ownerPass2 = PayrollDocumentService.GenerateRandomOwnerPassword();
        ownerPass1.Length.Should().BeGreaterThanOrEqualTo(32);
        ownerPass1.Should().NotBe(ownerPass2);

        // Mở ĐÚNG mật khẩu (6 số cuối CCCD = "654321") -> mở thành công
        using (var msValid = new MemoryStream(pdfResult.PdfBytes))
        using (var openedDoc = PdfReader.Open(msValid, "654321", PdfDocumentOpenMode.Import))
        {
            openedDoc.PageCount.Should().BeGreaterThanOrEqualTo(1);
        }

        // Nhân viên khác truy cập phiếu lương người khác qua ESS -> UnauthorizedAccessException
        var otherUserHandler = new GetPayrollRecordPdfQueryHandler(
            db,
            new StubCurrentUser { UserId = 3, Username = "intruder", EmployeeId = 999, AllowAllPermissions = false },
            docService);
        Func<Task> crossAccess = () => otherUserHandler.Handle(new GetPayrollRecordPdfQuery(rec.Id, EnforceSelfOnly: true), CancellationToken.None);
        await crossAccess.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task BatchEmailAndBankAndDepartmentExports_ShouldProduceExactTotalsAndDispatchReport()
    {
        var dbName = $"VnmBatch_{Guid.NewGuid():N}";
        await using var db = await CreateSeededDbContextAsync(dbName);
        var enc = CreateEncryptionService();

        db.Employees.AddRange(
            new Employee
            {
                Id = 1,
                EmployeeCode = "VNM-201",
                FullName = "Nguyễn Văn Ân",
                OrganizationUnitId = 1,
                PositionId = 1,
                BaseSalary = 25_000_000m,
                WorkEmail = "an.nv@vinamilk.com",
                CitizenIdEncrypted = enc.Encrypt("079095111222"),
                BankAccountEncrypted = enc.Encrypt("19002020001"),
                BankName = "Vietcombank",
                BankBranch = "HCM",
                WorkStatus = "Official"
            },
            new Employee
            {
                Id = 2,
                EmployeeCode = "VNM-202",
                FullName = "Đặng Thị Bích Phượng",
                OrganizationUnitId = 2,
                PositionId = 1,
                BaseSalary = 30_000_000m,
                WorkEmail = null,
                PersonalEmail = null,
                CitizenIdEncrypted = enc.Encrypt("079095333444"),
                BankAccountEncrypted = enc.Encrypt("19003030002"),
                BankName = "BIDV",
                BankBranch = "Hà Nội",
                WorkStatus = "Official"
            });

        db.MonthlyAttendanceSummaries.AddRange(
            new MonthlyAttendanceSummary { Id = 1, EmployeeId = 1, Month = 6, Year = 2026, StandardWorkdays = 26m, TotalPayableWorkdays = 26m },
            new MonthlyAttendanceSummary { Id = 2, EmployeeId = 2, Month = 6, Year = 2026, StandardWorkdays = 26m, TotalPayableWorkdays = 26m });
        await db.SaveChangesAsync();

        var actor = new StubCurrentUser { UserId = 1, Username = "cb_lead" };
        var periodId = await new CreatePayrollPeriodCommandHandler(db).Handle(new CreatePayrollPeriodCommand(2026, 6, 26m), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, actor).Handle(new CalculateMonthlyPayrollCommand(periodId), CancellationToken.None);

        var period = await db.PayrollPeriods.FirstAsync(p => p.Id == periodId);
        period.Status = PayrollPeriodStatuses.Approved;
        await db.SaveChangesAsync();

        var totalNetSalary = await db.PayrollRecords
            .Where(r => r.PayrollPeriodId == periodId)
            .SumAsync(r => r.NetSalary);

        var formatters = new IBankTransferTemplateFormatter[]
        {
            new CommonBankTransferTemplateFormatter(),
            new VietcombankTransferTemplateFormatter()
        };

        // 1. Export Bank Transfer Excel (COMMON) & check total == NetSalary sum + no accents uppercase
        var bankExcel = await new ExportBankTransferFileQueryHandler(db, enc, formatters)
            .Handle(new ExportBankTransferFileQuery(periodId, "excel", "COMMON"), CancellationToken.None);
        bankExcel.TotalAmount.Should().Be(totalNetSalary);
        bankExcel.EmployeeCount.Should().Be(2);

        using (var wb = new XLWorkbook(new MemoryStream(bankExcel.FileBytes)))
        {
            var ws = wb.Worksheet(1);
            ws.Cell(4, 4).GetString().Should().Be("NGUYEN VAN AN");
            ws.Cell(5, 4).GetString().Should().Be("DANG THI BICH PHUONG");
            var rowSum = ws.Cell(4, 7).GetValue<decimal>() + ws.Cell(5, 7).GetValue<decimal>();
            rowSum.Should().Be(totalNetSalary);
            ws.Cell(6, 7).GetValue<decimal>().Should().Be(totalNetSalary);
        }

        // Export CSV & VCB template
        var bankCsv = await new ExportBankTransferFileQueryHandler(db, enc, formatters)
            .Handle(new ExportBankTransferFileQuery(periodId, "csv", "COMMON"), CancellationToken.None);
        Encoding.UTF8.GetString(bankCsv.FileBytes).Should().Contain("NGUYEN VAN AN");

        var bankVcb = await new ExportBankTransferFileQueryHandler(db, enc, formatters)
            .Handle(new ExportBankTransferFileQuery(periodId, "excel", "VCB"), CancellationToken.None);
        bankVcb.TemplateCode.Should().Be("VCB");
        bankVcb.TotalAmount.Should().Be(totalNetSalary);

        // 2. Department Summary Report + Excel
        var deptReport = await new GetPayrollDepartmentSummaryReportQueryHandler(db)
            .Handle(new GetPayrollDepartmentSummaryReportQuery(periodId), CancellationToken.None);
        deptReport.Rows.Should().HaveCount(2);
        deptReport.GrandTotalNet.Should().Be(totalNetSalary);

        var docService = CreateDocumentService(enc);
        var deptExcelBytes = docService.ExportDepartmentSummaryExcel(deptReport);
        using (var deptWb = new XLWorkbook(new MemoryStream(deptExcelBytes)))
        {
            var dws = deptWb.Worksheet(1);
            dws.Cell(7, 3).GetString().Should().Be("TỔNG CỘNG TOÀN KỲ");
            dws.Cell(7, 12).GetValue<decimal>().Should().Be(totalNetSalary);
        }

        // 3. Batch Email Dispatch (In-Process simulation of Hangfire job)
        var emailStub = new CapturingEmailNotificationService();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IApplicationDbContext>(p => p.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPayrollDocumentService>(_ => docService);
        services.AddScoped<IEmailNotificationService>(_ => emailStub);
        services.AddLogging();
        services.AddScoped<IPayslipEmailJob>(p => new PayslipEmailHangfireJob(
            p.GetRequiredService<IApplicationDbContext>(),
            p.GetRequiredService<IPayrollDocumentService>(),
            p.GetRequiredService<IEmailNotificationService>(),
            NullLogger<PayslipEmailHangfireJob>.Instance));
        using var sp = services.BuildServiceProvider();
        var dispatcher = new InProcessPayslipBatchDispatcher(sp, NullLogger<InProcessPayslipBatchDispatcher>.Instance);

        var dispatchReport = await new SendPayslipsBatchCommandHandler(db, actor, dispatcher)
            .Handle(new SendPayslipsBatchCommand(periodId), CancellationToken.None);

        dispatchReport.TotalCount.Should().Be(2);
        dispatchReport.SentCount.Should().Be(1);
        dispatchReport.FailedCount.Should().Be(1);
        emailStub.SentEmails.Should().HaveCount(1);
        emailStub.SentEmails[0].To.Should().Be("an.nv@vinamilk.com");
        emailStub.SentEmails[0].FileName.Should().EndWith(".pdf");
        emailStub.SentEmails[0].PdfLength.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void ResolveQuestPdfLicense_ShouldReadFromConfiguration_AndRejectInvalidValues()
    {
        var communityCfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["QuestPDF:LicenseType"] = "Community" })
            .Build();
        DependencyInjection.ResolveQuestPdfLicense(communityCfg).Should().Be(LicenseType.Community);

        var professionalCfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["QuestPDF:LicenseType"] = "Professional" })
            .Build();
        DependencyInjection.ResolveQuestPdfLicense(professionalCfg).Should().Be(LicenseType.Professional);

        var enterpriseCfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["QuestPDF:LicenseType"] = "Enterprise" })
            .Build();
        DependencyInjection.ResolveQuestPdfLicense(enterpriseCfg).Should().Be(LicenseType.Enterprise);

        var invalidCfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["QuestPDF:LicenseType"] = "UnknownTier" })
            .Build();
        Action parseInvalid = () => DependencyInjection.ResolveQuestPdfLicense(invalidCfg);
        parseInvalid.Should().Throw<InvalidOperationException>().WithMessage("*QuestPDF:LicenseType*");
    }
}
