using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Vinamilk.HRM.Infrastructure.Services;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Payroll;

/// <summary>
/// Kiểm thử toàn diện PHIÊN CL7 (tính tay độc lập bằng số liệu cụ thể trong comment, KHÔNG tham chiếu hằng số nội bộ):
/// 1. Nhiều đợt chi trong cùng tháng (Tạm ứng giữa tháng + Lương chính + Thưởng T13 / đảo thứ tự tính vẫn ra đúng tổng thuế TNCN tháng).
/// 2. Tạm ứng lương ESS qua Workflow + Kiểm tra hạn mức % lương theo PolicyGroup (Nhân viên > Chức danh > Đơn vị > Mặc định công ty) + Tự động trừ vào kỳ lương.
/// 3. Khoản vay & trả góp nội bộ: theo dõi lịch từng kỳ, trừ lương hàng tháng, tự động PaidOff và tất toán sớm.
/// 4. Điều chỉnh sau khi khóa kỳ: gán nguồn kỳ khóa, bắt buộc lý do, tự động đưa vào kỳ mở kế tiếp, tính lại chênh lệch thuế TNCN đúng kỳ gốc.
/// 5. Quyết toán nghỉ việc (Final Settlement): lương ngày công cuối, phép tồn, trợ cấp thôi việc luật định (miễn thuế TNCN), trừ tạm ứng + tất toán vay + xuất PDF.
/// </summary>
public class PayrollDisbursementAndSettlementCl7Tests
{
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

    private sealed class NoOpRealtimeDispatcher : IRealtimeNotificationDispatcher
    {
        public Task DispatchToUserAsync(int? recipientUserId, string title, string message, string notificationType, string? targetUrl = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task BroadcastWorkflowUpdateAsync(int workflowInstanceId, string entityType, int entityId, string status, string summaryMessage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpEmailNotificationService : IEmailNotificationService
    {
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
            => Task.CompletedTask;

        public IReadOnlyList<SentEmailRecordDto> GetRecentSentEmails() => Array.Empty<SentEmailRecordDto>();
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
            var email = new NoOpEmailNotificationService();

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

    private static PayrollDocumentService CreateDocumentService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:PiiEncryptionKey"] = "UnitTest_SecretKey_32Chars_Alpha!",
                ["QuestPDF:LicenseType"] = "Community"
            })
            .Build();
        var encryptionService = new PiiEncryptionService(config);
        return new PayrollDocumentService(encryptionService, config);
    }

    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmPayrollCl7_{Guid.NewGuid():N}")
            .Options;
        var db = new ApplicationDbContext(options);

        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());
        await db.PayrollParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultTaxExemptPayrollParameters());

        var defaultGroup = ApplicationDbContextInitializer.BuildDefaultCompanyPolicyGroup();
        defaultGroup.Id = 1;
        defaultGroup.PayrollPolicies.First().Id = 1;
        defaultGroup.PayrollPolicies.First().PolicyGroupId = 1;
        await db.PolicyGroups.AddAsync(defaultGroup);

        db.Roles.AddRange(
            new Role { Id = 1, RoleCode = "SUPER_ADMIN", RoleName = "Super Admin", IsSystemRole = true },
            new Role { Id = 2, RoleCode = "HR_MANAGER", RoleName = "Trưởng phòng Nhân sự", IsSystemRole = true },
            new Role { Id = 3, RoleCode = "DEPARTMENT_MANAGER", RoleName = "Trưởng đơn vị", IsSystemRole = true });

        db.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, UnitCode = "HQ", UnitName = "Trụ sở chính", WageRegion = 1, IsActive = true });

        db.Positions.AddRange(
            new Position { Id = 1, PositionCode = "NV", PositionName = "Chuyên viên", IsActive = true },
            new Position { Id = 2, PositionCode = "PROB_ASSOC", PositionName = "Cộng tác viên", IsActive = true });

        await db.SaveChangesAsync();
        await WorkflowSeederHelper.EnsureDefaultWorkflowsAsync(db, CancellationToken.None);
        return db;
    }

    // =========================================================================
    // TEST 01: NHIỀU ĐỢT CHI TRONG THÁNG (TẠM ỨNG GIỮA THÁNG + LƯƠNG + THƯỞNG) & TỔNG THUẾ TNCN THÁNG BẤT BIẾN THEO THỨ TỰ
    // =========================================================================
    [Fact]
    public async Task MultiDisbursementInMonth_AdvanceMonthlyAndBonus_ShouldComputeCombinedMonthlyPitAccurately()
    {
        // Tính tay độc lập kỳ 08/2026 (Biểu thuế 5 bậc: 0-10tr = 5%, 10-30tr = 10%, 30-60tr = 20%; GT bản thân = 15.500.000đ):
        // - Nhân viên A (NV001): LCB = 30.000.000đ, 0 NPT, Không đoàn viên, Vùng 1, công 26/26.
        //   + Đợt 1 (MidMonthAdvance): tạm ứng đã duyệt 5.000.000đ -> Chi giữa tháng Net = 5.000.000đ, PIT = 0đ (chưa giảm trừ hay khấu trừ tạm).
        //   + Đợt 2 (Monthly): Lương 30.000.000đ -> BH NLĐ = 10.5% * 30.000.000 = 3.150.000đ.
        //     Thu nhập chịu thuế lương = 30.000.000 - 3.150.000 = 26.850.000đ.
        //     Thu nhập tính thuế lương = 26.850.000 - 15.500.000 = 11.350.000đ.
        //     Thuế TNCN đợt lương = 10.000.000 * 5% + 1.350.000 * 10% = 500.000 + 135.000 = 635.000đ.
        //     Khấu trừ lại tạm ứng đã nhận 5.000.000đ -> Net đợt lương = 30.000.000 - 3.150.000 - 635.000 - 5.000.000 = 21.215.000đ.
        //   + Đợt 3 (Bonus13): Thưởng 10.000.000đ (không nhân đôi giảm trừ gia cảnh 15.500.000đ).
        //     Tổng TNCT cả tháng = 26.850.000 + 10.000.000 = 36.850.000đ -> Tổng TNTT cả tháng = 36.850.000 - 15.500.000 = 21.350.000đ.
        //     Tổng thuế TNCN cả tháng = 10.000.000 * 5% + 11.350.000 * 10% = 500.000 + 1.135.000 = 1.635.000đ.
        //     Thuế TNCN phải khấu trừ thêm ở đợt Thưởng = 1.635.000 - 635.000 = 1.000.000đ; Net đợt Thưởng = 9.000.000đ.
        //
        // - Nhân viên B (NV002 - kiểm chứng tính bất biến thứ tự chạy): cùng mức LCB = 30.000.000đ, Thưởng = 10.000.000đ ở kỳ 09/2026 nhưng CHẠY ĐỢT THƯỞNG TRƯỚC, LƯƠNG CHÍNH SAU:
        //   + Chạy Bonus13 trước (09/2026): TNCT = 10.000.000đ <= GT bản thân 15.500.000đ -> PIT đợt 1 = 0đ, Net = 10.000.000đ.
        //   + Chạy Monthly sau (09/2026): TNCT đợt 2 = 26.850.000đ -> Tổng TNCT gộp = 36.850.000đ -> Tổng TNTT = 21.350.000đ -> Tổng PIT = 1.635.000đ -> PIT đợt 2 = 1.635.000đ!
        //   => Tổng PIT cả tháng của NV001 (1.635.000đ) BẰNG ĐÚNG Tổng PIT cả tháng của NV002 (1.635.000đ) bất kể thứ tự chạy đợt chi!
        await using var db = await CreateSeededDbContextAsync();

        var empA = new Employee
        {
            Id = 1, EmployeeCode = "NV001", FullName = "Nguyễn Văn A",
            OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active",
            HireDate = new DateOnly(2023, 1, 1), BaseSalary = 30_000_000m, IsUnionMember = false
        };
        var empB = new Employee
        {
            Id = 2, EmployeeCode = "NV002", FullName = "Trần Thị B",
            OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active",
            HireDate = new DateOnly(2023, 1, 1), BaseSalary = 30_000_000m, IsUnionMember = false
        };
        db.Employees.AddRange(empA, empB);

        db.SalaryAdvances.Add(new SalaryAdvance
        {
            Id = 1, AdvanceNumber = "ADV-08-001", EmployeeId = 1,
            RequestDate = new DateOnly(2026, 8, 12), TargetYear = 2026, TargetMonth = 8,
            Amount = 5_000_000m, Reason = "Tạm ứng giữa tháng 08", Status = "Approved"
        });
        db.PayrollAdjustments.AddRange(
            new PayrollAdjustment
            {
                Id = 1, AdjustmentCode = "ADJ-B13-08", EmployeeId = 1,
                TargetYear = 2026, TargetMonth = 8, AdjustmentType = "Bonus13",
                Amount = 10_000_000m, IsTaxable = true, Reason = "Thưởng hiệu suất đợt riêng T08", Status = "Approved"
            },
            new PayrollAdjustment
            {
                Id = 2, AdjustmentCode = "ADJ-B13-09", EmployeeId = 2,
                TargetYear = 2026, TargetMonth = 9, AdjustmentType = "Bonus13",
                Amount = 10_000_000m, IsTaxable = true, Reason = "Thưởng hiệu suất đợt riêng T09", Status = "Approved"
            });
        await db.SaveChangesAsync();

        var hrUser = new StubCurrentUser { UserId = 10, Username = "cb_admin", EmployeeId = 99 };
        var createPeriod = new CreatePayrollPeriodCommandHandler(db);
        var calcHandler = new CalculateMonthlyPayrollCommandHandler(db, hrUser);

        // --- Tháng 08/2026: Advance -> Monthly -> Bonus13 ---
        int pAdv = await createPeriod.Handle(new CreatePayrollPeriodCommand(2026, 8, 26m, "MidMonthAdvance", CreateNewBatch: true), CancellationToken.None);
        int pMonth08 = await createPeriod.Handle(new CreatePayrollPeriodCommand(2026, 8, 26m, "Monthly"), CancellationToken.None);
        int pBonus08 = await createPeriod.Handle(new CreatePayrollPeriodCommand(2026, 8, 26m, "Bonus13"), CancellationToken.None);

        await calcHandler.Handle(new CalculateMonthlyPayrollCommand(pAdv, EmployeeId: 1, RequireLockedAttendance: false), CancellationToken.None);
        var advRec = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == pAdv && r.EmployeeId == 1);
        advRec.GrossIncome.Should().Be(5_000_000m);
        advRec.PersonalIncomeTax.Should().Be(0m);
        advRec.NetSalary.Should().Be(5_000_000m);

        await calcHandler.Handle(new CalculateMonthlyPayrollCommand(pMonth08, EmployeeId: 1, RequireLockedAttendance: false), CancellationToken.None);
        var month08Rec = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == pMonth08 && r.EmployeeId == 1);
        month08Rec.GrossIncome.Should().Be(30_000_000m);
        month08Rec.TotalEmployeeInsurance.Should().Be(3_150_000m);
        month08Rec.PersonalIncomeTax.Should().Be(635_000m);
        month08Rec.SalaryAdvanceDeduction.Should().Be(5_000_000m);
        month08Rec.NetSalary.Should().Be(21_215_000m);

        await calcHandler.Handle(new CalculateMonthlyPayrollCommand(pBonus08, EmployeeId: 1, RequireLockedAttendance: false), CancellationToken.None);
        var bonus08Rec = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == pBonus08 && r.EmployeeId == 1);
        bonus08Rec.GrossIncome.Should().Be(10_000_000m);
        bonus08Rec.SelfDeductionAmount.Should().Be(0m);
        bonus08Rec.PersonalIncomeTax.Should().Be(1_000_000m);
        bonus08Rec.NetSalary.Should().Be(9_000_000m);

        decimal totalPitA = month08Rec.PersonalIncomeTax + bonus08Rec.PersonalIncomeTax;
        totalPitA.Should().Be(1_635_000m);

        // --- Tháng 09/2026: Chạy Bonus13 TRƯỚC, Monthly SAU cho NV B ---
        int pBonus09 = await createPeriod.Handle(new CreatePayrollPeriodCommand(2026, 9, 26m, "Bonus13"), CancellationToken.None);
        int pMonth09 = await createPeriod.Handle(new CreatePayrollPeriodCommand(2026, 9, 26m, "Monthly"), CancellationToken.None);

        await calcHandler.Handle(new CalculateMonthlyPayrollCommand(pBonus09, EmployeeId: 2, RequireLockedAttendance: false), CancellationToken.None);
        var bonus09Rec = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == pBonus09 && r.EmployeeId == 2);
        bonus09Rec.PersonalIncomeTax.Should().Be(0m);
        bonus09Rec.NetSalary.Should().Be(10_000_000m);

        await calcHandler.Handle(new CalculateMonthlyPayrollCommand(pMonth09, EmployeeId: 2, RequireLockedAttendance: false), CancellationToken.None);
        var month09Rec = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == pMonth09 && r.EmployeeId == 2);
        month09Rec.PersonalIncomeTax.Should().Be(1_635_000m);

        decimal totalPitB = bonus09Rec.PersonalIncomeTax + month09Rec.PersonalIncomeTax;
        totalPitB.Should().Be(totalPitA);
    }

    // =========================================================================
    // TEST 02: TẠM ỨNG QUA ESS + WORKFLOW + KIỂM TRA HẠN MỨC % THEO POLICYGROUP + TỰ ĐỘNG TRỪ LƯƠNG
    // =========================================================================
    [Fact]
    public async Task EssSalaryAdvance_ShouldEnforcePolicyGroupHierarchyAndWorkflowApprovalAndDeductInMonthlyPayroll()
    {
        // Tính tay hạn mức tạm ứng theo thứ tự ưu tiên PolicyGroup (Nhân viên > Chức danh > Đơn vị > Mặc định công ty):
        // - Chính sách Mặc định công ty: 50% lương CB.
        // - Chính sách riêng Chức danh PositionId=2 (Cộng tác viên): trần 30% lương CB.
        // - NV010 (Chức danh Id=2, LCB = 20.000.000đ): ưu tiên Chức danh -> hạn mức tối đa = 30% * 20.000.000 = 6.000.000đ.
        //   + Xin ứng 5.000.000đ lần 1 -> Hợp lệ (Status = Pending, tạo WorkflowInstance EntityType="SalaryAdvance").
        //   + Xin ứng thêm 2.000.000đ (tổng 7.000.000đ > 6.000.000đ) -> Bị chặn!
        // - NV011 (Chức danh Id=1, LCB = 20.000.000đ, dùng mặc định 50% = 10.000.000đ):
        //   + Xin ứng 4.000.000đ nhưng bị Quản lý TỪ CHỐI -> Status = Rejected, KHÔNG được khấu trừ vào bảng lương!
        // - Quản lý PHÊ DUYỆT đơn NV010 -> Status = Approved -> Kỳ lương 08/2026 tự động trừ 5.000.000đ và chuyển Status = Deducted.
        await using var db = await CreateSeededDbContextAsync();

        var customGroup = new PolicyGroup
        {
            Id = 2, PolicyCode = "POS_ASSOC_LIMIT", PolicyName = "Hạn mức 30% CTV", IsCompanyDefault = false, IsActive = true
        };
        customGroup.PayrollPolicies.Add(new PayrollPolicy
        {
            Id = 2, PolicyGroupId = 2, MaxAdvanceLimitPercent = 30.00m, AdvanceMinTenureDays = 15, AdvanceCutoffDayOfMonth = 28
        });
        db.PolicyGroups.Add(customGroup);
        db.PolicyGroupAssignments.Add(new PolicyGroupAssignment
        {
            Id = 1, PolicyGroupId = 2, PositionId = 2, IsActive = true
        });

        db.Employees.AddRange(
            new Employee
            {
                Id = 10, EmployeeCode = "NV010", FullName = "Lê Minh C",
                OrganizationUnitId = 1, PositionId = 2, DirectManagerId = 90, WorkStatus = "Active",
                HireDate = new DateOnly(2025, 1, 1), BaseSalary = 20_000_000m, IsUnionMember = false
            },
            new Employee
            {
                Id = 11, EmployeeCode = "NV011", FullName = "Phạm Thị D",
                OrganizationUnitId = 1, PositionId = 1, DirectManagerId = 90, WorkStatus = "Active",
                HireDate = new DateOnly(2025, 1, 1), BaseSalary = 20_000_000m, IsUnionMember = false
            },
            new Employee
            {
                Id = 90, EmployeeCode = "QL001", FullName = "Trần Quản Lý",
                OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active",
                HireDate = new DateOnly(2020, 1, 1), BaseSalary = 40_000_000m, IsUnionMember = false
            });

        db.Users.AddRange(
            new User { Id = 10, Username = "nv010", EmployeeId = 10, IsActive = true, Email = "nv010@vnm.vn", PasswordHash = "x" },
            new User { Id = 11, Username = "nv011", EmployeeId = 11, IsActive = true, Email = "nv011@vnm.vn", PasswordHash = "x" },
            new User { Id = 90, Username = "ql001", EmployeeId = 90, IsActive = true, Email = "ql001@vnm.vn", PasswordHash = "x" });
        await db.SaveChangesAsync();

        var quota = await new GetEssSalaryAdvanceQuotaQueryHandler(db)
            .Handle(new GetEssSalaryAdvanceQuotaQuery(10, 2026, 8), CancellationToken.None);
        quota.MaxAdvanceLimitPercent.Should().Be(30.00m);
        quota.PolicyScopeLevel.Should().Be("Position");
        quota.MaxAllowedAmount.Should().Be(6_000_000m);

        var emp10User = new StubCurrentUser { UserId = 10, Username = "nv010", EmployeeId = 10 };
        var emp11User = new StubCurrentUser { UserId = 11, Username = "nv011", EmployeeId = 11 };
        var mgrUser = new StubCurrentUser { UserId = 90, Username = "ql001", EmployeeId = 90 };

        ICurrentUserService activeActor = emp10User;
        var wfSender = new TestWorkflowSender(db, () => activeActor);

        var submitRes = await new SubmitEssSalaryAdvanceCommandHandler(db, emp10User, wfSender)
            .Handle(new SubmitEssSalaryAdvanceCommand(10, new DateOnly(2026, 8, 10), 2026, 8, 5_000_000m, "Ứng lương tháng 08"), CancellationToken.None);
        submitRes.Status.Should().Be("Pending");
        submitRes.MaxAllowedAmount.Should().Be(6_000_000m);
        submitRes.WorkflowInstanceId.Should().BeGreaterThan(0);

        // Gửi thêm đơn vượt trần 30% (5tr + 2tr = 7tr > 6tr) -> bị từ chối
        Func<Task> exceedCall = () => new SubmitEssSalaryAdvanceCommandHandler(db, emp10User, wfSender)
            .Handle(new SubmitEssSalaryAdvanceCommand(10, new DateOnly(2026, 8, 11), 2026, 8, 2_000_000m, "Vượt trần"), CancellationToken.None);
        await exceedCall.Should().ThrowAsync<InvalidOperationException>().WithMessage("*vượt hạn mức*");

        // NV011 gửi đơn 4tr và bị từ chối bởi quản lý
        activeActor = emp11User;
        var submit11 = await new SubmitEssSalaryAdvanceCommandHandler(db, emp11User, wfSender)
            .Handle(new SubmitEssSalaryAdvanceCommand(11, new DateOnly(2026, 8, 10), 2026, 8, 4_000_000m, "Ứng tháng 08"), CancellationToken.None);

        activeActor = mgrUser;
        var procHandler = new ProcessWorkflowActionCommandHandler(db, mgrUser, new NoOpRealtimeDispatcher(), new NoOpEmailNotificationService());
        await procHandler.Handle(new ProcessWorkflowActionCommand(submitRes.WorkflowInstanceId, "Approve", "Đồng ý"), CancellationToken.None);
        await procHandler.Handle(new ProcessWorkflowActionCommand(submit11.WorkflowInstanceId, "Reject", "Chưa phù hợp"), CancellationToken.None);

        (await db.SalaryAdvances.AsNoTracking().FirstAsync(a => a.Id == submitRes.SalaryAdvanceId)).Status.Should().Be("Approved");
        (await db.SalaryAdvances.AsNoTracking().FirstAsync(a => a.Id == submit11.SalaryAdvanceId)).Status.Should().Be("Rejected");

        // Tính lương T08: NV010 bị trừ 5.000.000đ và chuyển Deducted; NV011 KHÔNG bị trừ 4.000.000đ!
        var pId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 8, 26m, "Monthly"), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, mgrUser)
            .Handle(new CalculateMonthlyPayrollCommand(pId, RequireLockedAttendance: false), CancellationToken.None);

        var rec10 = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == pId && r.EmployeeId == 10);
        var rec11 = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == pId && r.EmployeeId == 11);
        rec10.SalaryAdvanceDeduction.Should().Be(5_000_000m);
        rec11.SalaryAdvanceDeduction.Should().Be(0m);
        (await db.SalaryAdvances.AsNoTracking().FirstAsync(a => a.Id == submitRes.SalaryAdvanceId)).Status.Should().Be("Deducted");
    }

    // =========================================================================
    // TEST 03: KHOẢN VAY / TRẢ GÓP NỘI BỘ: TRỪ TỪNG KỲ, TỰ ĐỘNG PAIDOFF & TẤT TOÁN SỚM
    // =========================================================================
    [Fact]
    public async Task EmployeeLoan_ShouldDeductMonthlyInstallmentsAutoPaidOffAndSupportEarlyPayoff()
    {
        // Tính tay:
        // - Khoản vay #1 (NV020): 6.000.000đ trả góp 2 kỳ (08/2026 = 3.000.000đ; 09/2026 = 3.000.000đ).
        //   + Sau lương T08: kỳ 1 Deducted, dư nợ còn 3.000.000đ, trạng thái Active.
        //   + Sau lương T09: kỳ 2 Deducted, dư nợ còn 0đ, trạng thái tự động chuyển PaidOff!
        // - Khoản vay #2 (NV021): 9.000.000đ trả góp 3 kỳ (08, 09, 10/2026 = 3.000.000đ/kỳ).
        //   + Sau lương T08: đã trừ 3.000.000đ, dư nợ còn 6.000.000đ.
        //   + Thực hiện tất toán sớm (SettleEmployeeLoanEarlyCommand) -> thu hồi 6.000.000đ, kỳ 2-3 chuyển Waived, Loan = PaidOff!
        await using var db = await CreateSeededDbContextAsync();

        db.Employees.AddRange(
            new Employee { Id = 20, EmployeeCode = "NV020", FullName = "Vay Đủ Kỳ", OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active", HireDate = new DateOnly(2024, 1, 1), BaseSalary = 20_000_000m },
            new Employee { Id = 21, EmployeeCode = "NV021", FullName = "Vay Tất Toán Sớm", OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active", HireDate = new DateOnly(2024, 1, 1), BaseSalary = 20_000_000m });
        await db.SaveChangesAsync();

        var hrUser = new StubCurrentUser { UserId = 1, Username = "cb_admin", EmployeeId = 99 };
        var createLoan = new CreateEmployeeLoanCommandHandler(db, hrUser);

        int loan1Id = await createLoan.Handle(new CreateEmployeeLoanCommand(20, new DateOnly(2026, 8, 1), 6_000_000m, 2, 2026, 8, "Vay hỗ trợ học phí"), CancellationToken.None);
        int loan2Id = await createLoan.Handle(new CreateEmployeeLoanCommand(21, new DateOnly(2026, 8, 1), 9_000_000m, 3, 2026, 8, "Mua laptop trả góp"), CancellationToken.None);

        var detailBefore = await new GetEmployeeLoanDetailQueryHandler(db).Handle(new GetEmployeeLoanDetailQuery(loan1Id), CancellationToken.None);
        detailBefore.RemainingPrincipal.Should().Be(6_000_000m);
        detailBefore.Installments.Should().HaveCount(2);

        var createPeriod = new CreatePayrollPeriodCommandHandler(db);
        var calcHandler = new CalculateMonthlyPayrollCommandHandler(db, hrUser);

        int p08 = await createPeriod.Handle(new CreatePayrollPeriodCommand(2026, 8, 26m, "Monthly"), CancellationToken.None);
        await calcHandler.Handle(new CalculateMonthlyPayrollCommand(p08, RequireLockedAttendance: false), CancellationToken.None);

        var rec20T08 = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == p08 && r.EmployeeId == 20);
        rec20T08.LoanInstallmentDeduction.Should().Be(3_000_000m);

        var detailMid = await new GetEmployeeLoanDetailQueryHandler(db).Handle(new GetEmployeeLoanDetailQuery(loan1Id), CancellationToken.None);
        detailMid.Status.Should().Be("Active");
        detailMid.RemainingPrincipal.Should().Be(3_000_000m);

        // Kỳ T09 -> hoàn tất kỳ cuối của Loan #1 -> Auto PaidOff
        int p09 = await createPeriod.Handle(new CreatePayrollPeriodCommand(2026, 9, 26m, "Monthly"), CancellationToken.None);
        await calcHandler.Handle(new CalculateMonthlyPayrollCommand(p09, EmployeeId: 20, RequireLockedAttendance: false), CancellationToken.None);

        var detailAfter = await new GetEmployeeLoanDetailQueryHandler(db).Handle(new GetEmployeeLoanDetailQuery(loan1Id), CancellationToken.None);
        detailAfter.Status.Should().Be("PaidOff");
        detailAfter.RemainingPrincipal.Should().Be(0m);
        detailAfter.Installments.All(i => i.Status == "Deducted").Should().BeTrue();

        // Loan #2 sau T08 còn 6.000.000đ -> Tất toán sớm
        decimal settledAmt = await new SettleEmployeeLoanEarlyCommandHandler(db, hrUser)
            .Handle(new SettleEmployeeLoanEarlyCommand(loan2Id, "Nhân viên chủ động trả trước phần còn lại"), CancellationToken.None);
        settledAmt.Should().Be(6_000_000m);

        var loan2Detail = await new GetEmployeeLoanDetailQueryHandler(db).Handle(new GetEmployeeLoanDetailQuery(loan2Id), CancellationToken.None);
        loan2Detail.Status.Should().Be("PaidOff");
        loan2Detail.RemainingPrincipal.Should().Be(0m);
        loan2Detail.Installments.Count(i => i.Status == "Waived").Should().Be(2);
    }

    // =========================================================================
    // TEST 04: ĐIỀU CHỈNH SAU KHÓA KỲ: GÁN KỲ MỞ KẾ TIẾP, CHẶN THIẾU LÝ DO & TÍNH LẠI CHÊNH LỆCH THUẾ KỲ GỐC
    // =========================================================================
    [Fact]
    public async Task PostLockAdjustment_ShouldRequireReasonAutoAssignNextOpenAndRecalculateSourcePeriodTaxDelta()
    {
        // Tính tay kỳ gốc 08/2026 (đã khóa):
        // - NV030: LCB = 25.000.000đ, 0 NPT, Không đoàn viên, công 26/26.
        //   + BH NLĐ = 10.5% * 25.000.000 = 2.625.000đ.
        //   + TNCT = 25.000.000 - 2.625.000 = 22.375.000đ; GT bản thân = 15.500.000đ -> TNTT = 6.875.000đ.
        //   + Thuế TNCN kỳ gốc (bậc 1: 5%) = 6.875.000 * 5% = 343.750đ.
        // - Sau khi khóa kỳ 08/2026, phát sinh phiếu Truy lĩnh (RetroEarning) = 5.000.000đ cho kỳ 08/2026:
        //   + Theo chế độ RecalculateSourcePeriod: TNCT kỳ gốc tăng lên 22.375.000 + 5.000.000 = 27.375.000đ -> TNTT = 11.875.000đ.
        //   + Thuế mới tại kỳ gốc = 10.000.000 * 5% + 1.875.000 * 10% = 500.000 + 187.500 = 687.500đ.
        //   + Chênh lệch thuế TNCN đúng kỳ gốc (SourcePeriodTaxDelta) = 687.500 - 343.750 = 343.750đ!
        //   + Phiếu tự động gán vào kỳ mở 09/2026: tại T09/2026 (cũng 25.000.000đ -> thuế thường T09 = 343.750đ),
        //     tổng PIT phải nộp ở T09 = 343.750 (của T09) + 343.750 (delta kỳ T08) = 687.500đ (KHÔNG đánh thuế lần nữa phần 5.000.000đ vào bậc thuế T09!).
        //     Net T09 = 25.000.000 + 5.000.000 - 2.625.000 - 687.500 = 26.687.500đ!
        await using var db = await CreateSeededDbContextAsync();

        db.Employees.AddRange(
            new Employee
            {
                Id = 30, EmployeeCode = "NV030", FullName = "Hoàng Truy Lĩnh",
                OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active",
                HireDate = new DateOnly(2023, 1, 1), BaseSalary = 25_000_000m, IsUnionMember = false
            },
            new Employee
            {
                Id = 99, EmployeeCode = "CB099", FullName = "Chuyên viên C&B",
                OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Resigned",
                HireDate = new DateOnly(2020, 1, 1), BaseSalary = 25_000_000m, IsUnionMember = false
            });
        db.Users.AddRange(
            new User { Id = 10, Username = "cb_admin", EmployeeId = 99, IsActive = true, Email = "cb@vnm.vn", PasswordHash = "x" },
            new User { Id = 20, Username = "hr_dir", EmployeeId = 30, IsActive = true, Email = "hrdir@vnm.vn", PasswordHash = "x" });
        await db.SaveChangesAsync();

        var hrUser = new StubCurrentUser { UserId = 10, Username = "cb_admin", EmployeeId = 99 };
        var mgrUser = new StubCurrentUser { UserId = 20, Username = "hr_dir", EmployeeId = 88 };
        ICurrentUserService activeActor = hrUser;
        var wfSender = new TestWorkflowSender(db, () => activeActor);

        var createPeriod = new CreatePayrollPeriodCommandHandler(db);
        int p08 = await createPeriod.Handle(new CreatePayrollPeriodCommand(2026, 8, 26m, "Monthly"), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, hrUser)
            .Handle(new CalculateMonthlyPayrollCommand(p08, RequireLockedAttendance: false), CancellationToken.None);

        await new SubmitPayrollPeriodCommandHandler(db, hrUser, wfSender).Handle(new SubmitPayrollPeriodCommand(p08), CancellationToken.None);
        activeActor = mgrUser;
        await new ApprovePayrollPeriodCommandHandler(db, mgrUser, wfSender).Handle(new ApprovePayrollPeriodCommand(p08, true), CancellationToken.None);
        await new LockPayrollPeriodCommandHandler(db, mgrUser).Handle(new LockPayrollPeriodCommand(p08), CancellationToken.None);

        var adjHandler = new CreatePayrollAdjustmentCommandHandler(db, hrUser);

        // Chặn tạo điều chỉnh nếu thiếu lý do
        Func<Task> emptyReason = () => adjHandler.Handle(new CreatePayrollAdjustmentCommand(30, "RetroEarning", 2026, 9, 5_000_000m, "   ", p08), CancellationToken.None);
        await emptyReason.Should().ThrowAsync<InvalidOperationException>().WithMessage("*lý do*");

        // Tạo phiếu truy lĩnh gắn kỳ khóa 08/2026 + AutoAssignNextOpenPeriod + RecalculateSourcePeriod
        int adjId = await adjHandler.Handle(new CreatePayrollAdjustmentCommand(
            EmployeeId: 30,
            AdjustmentType: "RetroEarning",
            TargetYear: 0,
            TargetMonth: 0,
            Amount: 5_000_000m,
            Reason: "Truy lĩnh phụ cấp thiếu tháng 08/2026",
            SourceLockedPeriodId: p08,
            IsTaxable: true,
            RecalculateTaxInSourcePeriod: true,
            AutoAssignNextOpenPeriod: true), CancellationToken.None);

        var savedAdj = await db.PayrollAdjustments.AsNoTracking().FirstAsync(a => a.Id == adjId);
        savedAdj.TargetYear.Should().Be(2026);
        savedAdj.TargetMonth.Should().Be(9);
        savedAdj.RecalculateTaxInSourcePeriod.Should().BeTrue();
        savedAdj.SourcePeriodTaxDelta.Should().Be(343_750m);

        // Mở và tính kỳ T09/2026 -> tự động lấy phiếu điều chỉnh và cộng đúng 343.750đ chênh lệch thuế kỳ gốc
        int p09 = await createPeriod.Handle(new CreatePayrollPeriodCommand(2026, 9, 26m, "Monthly"), CancellationToken.None);
        await new CalculateMonthlyPayrollCommandHandler(db, hrUser)
            .Handle(new CalculateMonthlyPayrollCommand(p09, RequireLockedAttendance: false), CancellationToken.None);

        var rec09 = await db.PayrollRecords.AsNoTracking().FirstAsync(r => r.PayrollPeriodId == p09 && r.EmployeeId == 30);
        rec09.RetroEarningAmount.Should().Be(5_000_000m);
        rec09.PersonalIncomeTax.Should().Be(687_500m);
        rec09.NetSalary.Should().Be(26_687_500m);
    }

    // =========================================================================
    // TEST 05: QUYẾT TOÁN NGHỈ VIỆC (FINAL SETTLEMENT): LƯƠNG CUỐI, PHÉP TỒN, TCTV MIỄN THUẾ, CẤN TRỪ TẠM ỨNG & VAY + PDF
    // =========================================================================
    [Fact]
    public async Task TerminationSettlement_ShouldComputeFinalPayLeaveSeveranceLoanPayoffAndGeneratePdf()
    {
        // Tính tay hoàn chỉnh cho NV040 nghỉ việc ngày 31/08/2026 (Resignation):
        // - Ngày vào làm: 01/01/2007 -> Ngày nghỉ: 31/08/2026 (tổng thâm niên = 236 tháng).
        // - Tham gia BHTN bắt buộc từ 01/01/2009 -> 31/08/2026 = 212 tháng -> Thời gian chưa đóng BHTN = 236 - 212 = 24 tháng = 2.0 năm.
        // - Lương hợp đồng: 20.000.000đ, 0 NPT, Không đoàn viên, công tháng cuối = 26/26.
        //   + 1. Lương tháng cuối = 20.000.000đ.
        //   + 2. Chi trả phép năm tồn (3 ngày) = Round(20.000.000 / 26 * 3, 0) = 2.307.692đ.
        //   + 3. Trợ cấp thôi việc luật định (0.5 tháng/năm * 2.0 năm * 20.000.000) = 20.000.000đ (Miễn thuế TNCN theo TT 111/2013).
        //   => Tổng Gross quyết toán = 20.000.000 + 2.307.692 + 20.000.000 = 42.307.692đ.
        // - Khấu trừ:
        //   + BH NLĐ trên lương tháng cuối (10.5% * 20.000.000) = 2.100.000đ.
        //   + Thu nhập chịu thuế = 42.307.692 - 20.000.000 (TCTV miễn thuế) - 2.100.000 (BH) = 20.207.692đ.
        //   + Thu nhập tính thuế = 20.207.692 - 15.500.000 (GT bản thân) = 4.707.692đ (Bậc 1: 5%).
        //   + Thuế TNCN = Round(4.707.692 * 5%, 0) = 235.385đ.
        //   + Tạm ứng đã duyệt chưa trừ = 2.000.000đ.
        //   + Tất toán toàn bộ dư nợ vay nội bộ còn 2 kỳ * 2.000.000 = 4.000.000đ.
        // - Thực nhận cuối cùng (Net Settlement) = 42.307.692 - 2.100.000 - 235.385 - 2.000.000 - 4.000.000 = 33.972.307đ!
        // - Sau khi chốt & chi: Employee.WorkStatus = "Terminated", SalaryAdvance = "Deducted", Loan = "PaidOff", Installments = "SettledOnTermination", PDF hợp lệ.
        await using var db = await CreateSeededDbContextAsync();

        var emp = new Employee
        {
            Id = 40, EmployeeCode = "NV040", FullName = "Lê Quyết Toán",
            OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active",
            HireDate = new DateOnly(2007, 1, 1), BaseSalary = 20_000_000m, IsUnionMember = false
        };
        db.Employees.Add(emp);

        db.TerminationLeaveSettlements.Add(new TerminationLeaveSettlement
        {
            Id = 1, EmployeeId = 40, SettlementYear = 2026, TerminationDate = new DateOnly(2026, 8, 31),
            ProRatedEntitlementDays = 8m, SeniorityExtraDays = 3m, UsedDays = 8m, UnusedPayableDays = 3m,
            BaseSalaryUsed = 20_000_000m, StandardWorkdaysUsed = 26m, PayoutAmount = 2_307_692m,
            TargetPayrollMonth = 8, TargetPayrollYear = 2026, Status = "PushedToPayroll"
        });

        db.SalaryAdvances.Add(new SalaryAdvance
        {
            Id = 40, AdvanceNumber = "ADV-T08-040", EmployeeId = 40,
            RequestDate = new DateOnly(2026, 8, 5), TargetYear = 2026, TargetMonth = 8,
            Amount = 2_000_000m, Reason = "Tạm ứng tháng cuối", Status = "Approved"
        });

        var hrUser = new StubCurrentUser { UserId = 10, Username = "cb_admin", EmployeeId = 99 };
        await db.SaveChangesAsync();

        await new CreateEmployeeLoanCommandHandler(db, hrUser)
            .Handle(new CreateEmployeeLoanCommand(40, new DateOnly(2026, 8, 1), 4_000_000m, 2, 2026, 9, "Vay nội bộ chưa trả"), CancellationToken.None);

        var calcSettle = new CalculateTerminationSettlementCommandHandler(db, hrUser);
        var dto = await calcSettle.Handle(new CalculateTerminationSettlementCommand(
            EmployeeId: 40,
            LastWorkingDate: new DateOnly(2026, 8, 31),
            TerminationType: "Resignation",
            StandardWorkdays: 26m,
            DeductEmployeeInsuranceOnFinalMonth: true), CancellationToken.None);

        dto.FinalMonthSalaryAmount.Should().Be(20_000_000m);
        dto.UnusedLeaveDays.Should().Be(3m);
        dto.UnusedLeavePayoutAmount.Should().Be(2_307_692m);
        dto.EligibleSeveranceYears.Should().Be(2.0m);
        dto.StatutorySeveranceAmount.Should().Be(20_000_000m);
        dto.TotalGrossSettlement.Should().Be(42_307_692m);
        dto.EmployeeInsuranceDeduction.Should().Be(2_100_000m);
        dto.PersonalIncomeTaxDeduction.Should().Be(235_385m);
        dto.SalaryAdvanceDeduction.Should().Be(2_000_000m);
        dto.LoanPayoffDeduction.Should().Be(4_000_000m);
        dto.NetSettlementAmount.Should().Be(33_972_307m);
        dto.RemainingDebtToRecover.Should().Be(0m);

        var approver = new StubCurrentUser { UserId = 20, Username = "hr_dir", EmployeeId = 88 };
        await new ApproveAndSettleTerminationCommandHandler(db, approver)
            .Handle(new ApproveAndSettleTerminationCommand(dto.Id, MarkAsPaid: true), CancellationToken.None);

        var savedEmp = await db.Employees.AsNoTracking().FirstAsync(e => e.Id == 40);
        savedEmp.WorkStatus.Should().Be("Resigned");
        savedEmp.TerminationDate.Should().Be(new DateOnly(2026, 8, 31));

        var savedLoan = await db.EmployeeLoans.AsNoTracking().Include(l => l.Installments).FirstAsync(l => l.EmployeeId == 40);
        savedLoan.Status.Should().Be("PaidOff");
        savedLoan.Installments.All(i => i.Status == "SettledOnTermination" && i.TerminationSettlementId == dto.Id).Should().BeTrue();

        var savedAdv = await db.SalaryAdvances.AsNoTracking().FirstAsync(a => a.Id == 40);
        savedAdv.Status.Should().Be("Deducted");
        savedAdv.TerminationSettlementId.Should().Be(dto.Id);

        var pdf = await new GetTerminationSettlementPdfQueryHandler(db, CreateDocumentService())
            .Handle(new GetTerminationSettlementPdfQuery(dto.Id), CancellationToken.None);
        pdf.FileName.Should().StartWith("QuyetToanNghiViec_NV040_");
        pdf.PdfBytes.Length.Should().BeGreaterThan(1500);
        Encoding.ASCII.GetString(pdf.PdfBytes, 0, 4).Should().Be("%PDF");
    }

    // =========================================================================
    // TEST 06: QUYẾT TOÁN NGHỈ VIỆC KHI CÔNG NỢ LỚN HƠN THU NHẬP CUỐI (KHÔNG ĐỦ TRỪ)
    // =========================================================================
    [Fact]
    public async Task TerminationSettlement_WhenDebtExceedsEarnings_ShouldClampNetToZeroAndTrackRemainingDebt()
    {
        // Tính tay cho NV050 bị sa thải kỷ luật (Dismissal -> KHÔNG hưởng TCTV = 0đ):
        // - Làm việc tháng cuối 08/2026 công 26/26, LCB = 10.000.000đ, 0 NPT, Không phép tồn.
        // - Gross = 10.000.000đ; BH NLĐ (10.5%) = 1.050.000đ -> Thu nhập chịu thuế = 8.950.000đ < 15.500.000đ -> PIT = 0đ.
        // - Số dư sau BH & thuế = 8.950.000đ.
        // - Đang nợ khoản vay = 15.000.000đ -> Khấu trừ vượt quá 8.950.000đ => Thực nhận Net = 0đ, Công nợ còn phải thu thêm = 15.000.000 - 8.950.000 = 6.050.000đ!
        await using var db = await CreateSeededDbContextAsync();

        db.Employees.Add(new Employee
        {
            Id = 50, EmployeeCode = "NV050", FullName = "Trần Công Nợ",
            OrganizationUnitId = 1, PositionId = 1, WorkStatus = "Active",
            HireDate = new DateOnly(2020, 1, 1), BaseSalary = 10_000_000m, IsUnionMember = false
        });
        await db.SaveChangesAsync();

        var hrUser = new StubCurrentUser { UserId = 10, Username = "cb_admin", EmployeeId = 99 };
        await new CreateEmployeeLoanCommandHandler(db, hrUser)
            .Handle(new CreateEmployeeLoanCommand(50, new DateOnly(2026, 8, 1), 15_000_000m, 5, 2026, 9, "Khoản vay lớn"), CancellationToken.None);

        var dto = await new CalculateTerminationSettlementCommandHandler(db, hrUser)
            .Handle(new CalculateTerminationSettlementCommand(
                EmployeeId: 50,
                LastWorkingDate: new DateOnly(2026, 8, 31),
                TerminationType: "Dismissal",
                StandardWorkdays: 26m,
                DeductEmployeeInsuranceOnFinalMonth: true), CancellationToken.None);

        dto.EligibleSeveranceYears.Should().Be(0m);
        dto.StatutorySeveranceAmount.Should().Be(0m);
        dto.TotalGrossSettlement.Should().Be(10_000_000m);
        dto.EmployeeInsuranceDeduction.Should().Be(1_050_000m);
        dto.PersonalIncomeTaxDeduction.Should().Be(0m);
        dto.LoanPayoffDeduction.Should().Be(15_000_000m);
        dto.NetSettlementAmount.Should().Be(0m);
        dto.RemainingDebtToRecover.Should().Be(6_050_000m);
    }
}
