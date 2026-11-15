using ClosedXML.Excel;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Application.TimeAttendance;
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
/// KIỂM THỬ SONG SONG END-TO-END CHO 30 NHÂN VIÊN ĐA DẠNG (THÁNG 09/2026):
/// Chạy trọn chu trình:
///   Log chấm công thô (quên chấm, đi muộn, ca 3 kíp ngày/chiều/đêm, OT đêm, trực Lễ 02/09, phép P, nghỉ KL Ro)
///   -> Tính công lô (InProcessAttendanceRecalculationDispatcher)
///   -> Chốt công 3 bước (UnitSubmit -> ManagerConfirm -> FinalLock)
///   -> Tạo kỳ lương T09/2026 -> Tính lương (RequireLockedAttendance: true)
///   -> Trình duyệt (Submit) -> Phê duyệt SoD (Approve) -> Khóa kỳ (Lock)
///   -> Sinh 30 Phiếu lương PDF (mã hóa mật khẩu CCCD)
///   -> File Ngân hàng (Bank Transfer)
///   -> Báo cáo A3 Ngang PDF, Cost Center, BHXH D02-LT + Đối chiếu, Thuế TNCN 05/KK & 05/QTT, Bút toán Kế toán TT200
///   -> Xuất file Excel đối chiếu cho kế toán kiểm tra tay tại: dotnet/DoiChieu_Luong_30NV_T09_2026.xlsx
/// </summary>
public class PayrollParallelVerification30EmployeesTests
{
    private sealed class StubCurrentUser : ICurrentUserService
    {
        public int? UserId { get; init; }
        public string Username { get; init; } = "tester";
        public int? EmployeeId { get; init; }
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "ParallelAudit-T09-2026";
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
        public Task SendWorkflowEmailAsync(string toEmail, string recipientName, string subject, string documentTitle, string initiatorName, string currentStepName, string statusBadge, string actionUrl, string? comment = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SendPayslipEmailWithAttachmentAsync(string toEmail, string recipientName, string subject, string periodTitle, string htmlSummary, byte[] pdfAttachment, string attachmentFileName, CancellationToken cancellationToken = default)
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
            object? res = request switch
            {
                StartWorkflowInstanceCommand start => await new StartWorkflowInstanceCommandHandler(_db, _userAccessor(), rt, email).Handle(start, cancellationToken),
                ProcessWorkflowActionCommand proc => await new ProcessWorkflowActionCommandHandler(_db, _userAccessor(), rt, email).Handle(proc, cancellationToken),
                _ => throw new NotSupportedException($"Unsupported {request.GetType().Name}")
            };
            return (TResponse)res!;
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotSupportedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed record EmployeeScenarioSpec(
        int Id,
        string Code,
        string Name,
        string CohortGroup,
        int UnitId,
        int PositionId,
        int ShiftId,
        string WorkStatus,
        string ContractType,
        DateOnly HireDate,
        DateOnly? TerminationDate,
        decimal BaseSalary,
        decimal PosAllowance,
        decimal OtherAllowance,
        bool IsProbation,
        decimal ProbationRate,
        decimal InsuranceSalary,
        string TaxResidencyMode,
        bool HasCommitment08,
        bool IsUnionMember,
        int DependentCount,
        string CitizenId,
        string BankAccount,
        string BankName,
        string ScenarioNote);

    [Fact]
    public async Task EndToEndParallelVerification_30DiverseEmployees_September2026_ReconcilesAllReportsAndExportsAuditWorkbook()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:PiiEncryptionKey"] = "UnitTest_SecretKey_32Chars_Alpha!",
                ["QuestPDF:LicenseType"] = "Community"
            })
            .Build();
        var pii = new PiiEncryptionService(config);
        var docs = new PayrollDocumentService(pii, config);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmParallel30_{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);

        // 1. Seed tham số luật định, biểu thuế, tài khoản kế toán & mẫu BHXH
        await db.AttendanceStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultAttendanceStatutoryParameters());
        await db.AttendanceSymbolDefinitions.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultAttendanceSymbols());
        await db.LeaveTypes.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultLeaveTypes());
        await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
        await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());
        await db.PayrollParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultTaxExemptPayrollParameters());
        await db.PayrollAccountingAccountConfigs.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultAccountingConfigs());
        await db.SocialInsuranceReportTemplates.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultSocialInsuranceTemplates());

        var defaultGroup = ApplicationDbContextInitializer.BuildDefaultCompanyPolicyGroup();
        defaultGroup.Id = 1;
        await db.PolicyGroups.AddAsync(defaultGroup);

        db.Roles.AddRange(
            new Role { Id = 1, RoleCode = "SUPER_ADMIN", RoleName = "Super Admin", IsSystemRole = true },
            new Role { Id = 2, RoleCode = "HR_DIRECTOR", RoleName = "Giám đốc Nhân sự", IsSystemRole = true },
            new Role { Id = 3, RoleCode = "HR_MANAGER", RoleName = "Trưởng phòng C&B", IsSystemRole = true },
            new Role { Id = 4, RoleCode = "DEPARTMENT_MANAGER", RoleName = "Trưởng đơn vị", IsSystemRole = true });

        db.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, UnitCode = "HO_ADMIN", UnitName = "Khối Văn phòng Trụ sở chính", CostCenterCode = "HO_ADMIN", WageRegion = 1, IsActive = true },
            new OrganizationUnit { Id = 2, UnitCode = "NM_BS", UnitName = "Nhà máy Sữa Bình Sơn (3 Ca)", CostCenterCode = "NM_BS", WageRegion = 2, IsActive = true },
            new OrganizationUnit { Id = 3, UnitCode = "SALES_VN", UnitName = "Khối Kinh doanh Toàn quốc", CostCenterCode = "SALES_VN", WageRegion = 1, IsActive = true });

        db.Positions.AddRange(
            new Position { Id = 1, PositionCode = "NV_VP", PositionName = "Chuyên viên Văn phòng", PositionLevel = 5, IsActive = true },
            new Position { Id = 2, PositionCode = "CN_NM", PositionName = "Công nhân / KTV Nhà máy", PositionLevel = 6, IsActive = true },
            new Position { Id = 3, PositionCode = "NV_KD", PositionName = "Chuyên viên Kinh doanh", PositionLevel = 5, IsActive = true },
            new Position { Id = 4, PositionCode = "EXPAT", PositionName = "Chuyên gia Tư vấn Quốc tế", PositionLevel = 3, IsActive = true });

        db.WorkShifts.AddRange(
            new WorkShift { Id = 1, ShiftCode = "HC", ShiftName = "Ca Hành chính (08:00-17:00)", ShiftType = "Fixed", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0), BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 0), StandardHours = 8m, WorkdayValue = 1m, GraceLateMinutes = 5, MaxEarlyCheckInMinutes = 120, MaxLateCheckOutMinutes = 240 },
            new WorkShift { Id = 2, ShiftCode = "CA_SANG", ShiftName = "Nhà máy Ca 1 Sáng (06:00-14:00)", ShiftType = "Fixed", StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(14, 0), StandardHours = 8m, WorkdayValue = 1m, GraceLateMinutes = 5, MaxEarlyCheckInMinutes = 120, MaxLateCheckOutMinutes = 240 },
            new WorkShift { Id = 3, ShiftCode = "CA_CHIEU", ShiftName = "Nhà máy Ca 2 Chiều (14:00-22:00)", ShiftType = "Fixed", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(22, 0), StandardHours = 8m, WorkdayValue = 1m, GraceLateMinutes = 5, MaxEarlyCheckInMinutes = 120, MaxLateCheckOutMinutes = 240 },
            new WorkShift { Id = 4, ShiftCode = "CA_DEM", ShiftName = "Nhà máy Ca 3 Đêm (22:00-06:00)", ShiftType = "Overnight", StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0), IsOvernight = true, StandardHours = 8m, WorkdayValue = 1m, NightShiftAllowanceRate = 0.30m, GraceLateMinutes = 5, MaxEarlyCheckInMinutes = 120, MaxLateCheckOutMinutes = 240 });

        // Seed đủ 2 ngày Lễ Quốc khánh 2026 (01/09 mặc định theo TB 9441/TB-BNV + 02/09)
        db.Holidays.AddRange(ApplicationDbContextInitializer.BuildDefaultHolidays2026()
            .Where(h => h.HolidayDate.Month == 9));
        await db.SaveChangesAsync();

        // Kiểm chứng tính năng cho HR chuyển chọn ngày liền kề 03/09 (Sau) <-> 01/09 (Trước) và giữ mặc định 01/09
        var selectHolidayHandler = new SelectNationalDayAdjacentHolidayCommandHandler(db);
        (await selectHolidayHandler.Handle(new SelectNationalDayAdjacentHolidayCommand(2026, "After"), CancellationToken.None))
            .Should().Be(new DateOnly(2026, 9, 3));
        (await selectHolidayHandler.Handle(new SelectNationalDayAdjacentHolidayCommand(2026, "Before"), CancellationToken.None))
            .Should().Be(new DateOnly(2026, 9, 1));

        await db.SaveChangesAsync();
        await WorkflowSeederHelper.EnsureDefaultWorkflowsAsync(db, CancellationToken.None);

        // 2. Xây dựng danh sách 30 nhân viên đa dạng
        var specs = Build30EmployeeSpecs();
        specs.Should().HaveCount(30);

        foreach (var s in specs)
        {
            db.Employees.Add(new Employee
            {
                Id = s.Id,
                EmployeeCode = s.Code,
                FullName = s.Name,
                Gender = s.Id % 2 == 0 ? "Nữ" : "Nam",
                DateOfBirth = new DateOnly(1990 + (s.Id % 10), ((s.Id - 1) % 12) + 1, 15),
                OrganizationUnitId = s.UnitId,
                PositionId = s.PositionId,
                WorkStatus = s.WorkStatus,
                CurrentContractType = s.ContractType,
                HireDate = s.HireDate,
                TerminationDate = null, // Cập nhật sau bước tính công để E30 có bảng công 01-19/09 trước khi chuyển Terminated
                BaseSalary = s.BaseSalary,
                PositionAllowance = s.PosAllowance,
                OtherAllowance = s.OtherAllowance,
                CitizenIdEncrypted = pii.Encrypt(s.CitizenId),
                CitizenIdHash = pii.ComputeDeterministicHash(s.CitizenId),
                TaxCodeEncrypted = pii.Encrypt($"80{(10000000 + s.Id):D8}"),
                BankAccountEncrypted = pii.Encrypt(s.BankAccount),
                BankName = s.BankName,
                SocialInsuranceNumber = s.IsProbation ? null : $"79{(26000000 + s.Id):D8}",
                IsUnionMember = s.IsUnionMember,
                WorkEmail = $"{s.Code.ToLowerInvariant()}@vinamilk.com.vn"
            });

            db.EmployeeSalaryProfiles.Add(new EmployeeSalaryProfile
            {
                EmployeeId = s.Id,
                EffectiveFrom = new DateOnly(2026, 1, 1),
                BaseSalary = s.BaseSalary,
                InsuranceSalary = s.InsuranceSalary,
                PayMode = "Monthly",
                IsProbation = s.IsProbation,
                ProbationRatePercent = s.ProbationRate,
                PositionAllowance = s.PosAllowance,
                OtherAllowance = s.OtherAllowance,
                IsUnionMember = s.IsUnionMember,
                TaxResidencyMode = s.TaxResidencyMode,
                HasPitCommitment08 = s.HasCommitment08,
                IsActive = true
            });

            for (int d = 1; d <= s.DependentCount; d++)
            {
                db.EmployeeDependents.Add(new EmployeeDependent
                {
                    EmployeeId = s.Id,
                    FullName = $"Người phụ thuộc {d} ({s.Code})",
                    Relationship = "Child",
                    IsTaxDeductible = true,
                    DeductionFromDate = new DateOnly(2026, 1, 1)
                });
            }
        }

        // Users phục vụ SoD (tính công/tính lương != người duyệt) + tài khoản system.job
        db.Users.AddRange(
            new User { Id = 10, Username = "cb.specialist", Email = "cb@vinamilk.com.vn", PasswordHash = "h", EmployeeId = 1, IsActive = true },
            new User { Id = 20, Username = "hr.director", Email = "dir@vinamilk.com.vn", PasswordHash = "h", EmployeeId = 3, IsActive = true },
            new User { Id = 99, Username = "system.job", Email = "sys@vinamilk.com.vn", PasswordHash = "h", IsActive = true });
        db.UserRoles.AddRange(
            new UserRole { UserId = 10, RoleId = 3 },
            new UserRole { UserId = 20, RoleId = 2 });

        // 3. Seed kỳ nền T08/2026 (29 NV cũ: E01..E28 + E30) để D02-LT T09/2026 bắt biến động TANG_MOI cho E29
        var augPeriod = new PayrollPeriod
        {
            Id = 100,
            PeriodCode = "PAY-2026-08",
            PeriodTitle = "Kỳ lương nền Tháng 08/2026",
            Year = 2026,
            Month = 8,
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 31),
            StandardWorkdays = 21m,
            PeriodType = "Monthly",
            Status = "Paid",
            IsLocked = true
        };
        db.PayrollPeriods.Add(augPeriod);
        foreach (var s in specs.Where(x => x.Id != 29 && !x.IsProbation && x.InsuranceSalary > 0m))
        {
            // E03 tăng lương đóng BHXH ở T09 (T08 là 35tr -> T09 lên 46.8tr trần = DIEU_CHINH_TANG)
            decimal augIns = s.Id == 3 ? 35_000_000m : s.InsuranceSalary;
            db.PayrollRecords.Add(new PayrollRecord
            {
                PayrollPeriodId = 100,
                EmployeeId = s.Id,
                OrganizationUnitId = s.UnitId,
                PositionId = s.PositionId,
                InsuranceSalaryBase = augIns,
                UnemploymentInsuranceBase = augIns,
                GrossIncome = augIns,
                NetSalary = augIns
            });
        }

        // 4. Sự kiện nghiệp vụ tháng 09/2026: Tạm ứng (E04), Khen thưởng/Hoa hồng (E21, E22), Kỷ luật (E24)
        db.SalaryAdvances.Add(new SalaryAdvance
        {
            EmployeeId = 4,
            AdvanceNumber = "ADV-2026-09-001",
            RequestDate = new DateOnly(2026, 9, 12),
            TargetYear = 2026,
            TargetMonth = 9,
            Amount = 3_000_000m,
            Reason = "Tạm ứng chi phí gia đình giữa tháng 09/2026",
            Status = "Approved",
            ApprovedAt = DateTime.UtcNow
        });

        db.PayrollAdjustments.AddRange(
            new PayrollAdjustment
            {
                AdjustmentCode = "COM-202609-E21",
                EmployeeId = 21,
                TargetYear = 2026,
                TargetMonth = 9,
                AdjustmentType = "Commission",
                Amount = 6_500_000m,
                Reason = "Hoa hồng doanh số kênh GT tháng 09/2026",
                IsTaxable = true,
                Status = "Approved"
            },
            new PayrollAdjustment
            {
                AdjustmentCode = "COM-202609-E22",
                EmployeeId = 22,
                TargetYear = 2026,
                TargetMonth = 9,
                AdjustmentType = "Commission",
                Amount = 4_200_000m,
                Reason = "Hoa hồng doanh số kênh MT tháng 09/2026",
                IsTaxable = true,
                Status = "Approved"
            });

        var rdCat = await db.RewardDisciplineCategories.FirstOrDefaultAsync(c => c.CategoryType == "Discipline");
        if (rdCat is null)
        {
            rdCat = new RewardDisciplineCategory
            {
                CategoryCode = "KL_NOI_QUY",
                CategoryName = "Vi phạm nội quy lao động",
                CategoryType = "Discipline",
                DefaultAmount = 300_000m
            };
            db.RewardDisciplineCategories.Add(rdCat);
            await db.SaveChangesAsync();
        }

        db.RewardDisciplines.Add(new RewardDiscipline
        {
            EmployeeId = 24,
            CategoryId = rdCat.Id,
            Type = "Discipline",
            DecisionNumber = "QD-KL-202609-24",
            DecisionDate = new DateOnly(2026, 9, 25),
            EffectiveDate = new DateOnly(2026, 9, 25),
            Amount = 300_000m,
            Reason = "Phạt vi phạm nội quy báo cáo tuần",
            Status = "Approved"
        });

        // 5. Đơn Nghỉ phép (E07: 16-17/09, E08 không lương: 21/09, E23 công tác: 18/09) & Đơn OT (E11, E14 trực lễ, E18 trực đêm lễ)
        var leaveTypes = await db.LeaveTypes.AsNoTracking().ToListAsync();
        var alType = leaveTypes.First(l => l.LeaveTypeCode is "ANNUAL" or "AL");
        var unpaidType = leaveTypes.First(l => l.LeaveTypeCode == "UNPAID" || l.LeaveTypeCode == "KL" || !l.IsPaidByCompany);
        var btType = leaveTypes.FirstOrDefault(l => l.LeaveTypeCode == "BUSINESS_TRIP") ?? alType;

        db.LeaveRequests.AddRange(
            new LeaveRequest { EmployeeId = 7, LeaveTypeId = alType.Id, StartDate = new DateOnly(2026, 9, 16), EndDate = new DateOnly(2026, 9, 17), TotalDays = 2m, Reason = "Nghỉ phép năm việc gia đình", Status = "Approved" },
            new LeaveRequest { EmployeeId = 8, LeaveTypeId = unpaidType.Id, StartDate = new DateOnly(2026, 9, 21), EndDate = new DateOnly(2026, 9, 21), TotalDays = 1m, Reason = "Nghỉ việc riêng không lương", Status = "Approved" },
            new LeaveRequest { EmployeeId = 23, LeaveTypeId = btType.Id, StartDate = new DateOnly(2026, 9, 18), EndDate = new DateOnly(2026, 9, 18), TotalDays = 1m, Reason = "Đi công tác khách hàng khu vực miền Tây", Status = "Approved" });

        db.OvertimeRequests.AddRange(
            // E11 (Ca sáng 06-14h): OT ngày thường 14:00-16:00 (2h x 2 ngày = 4h OT150)
            new OvertimeRequest { EmployeeId = 11, OvertimeDate = new DateOnly(2026, 9, 10), StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(16, 0), RequestedHours = 2m, ApprovedHours = 2m, OvertimeRateType = "OT150", CompensationMode = "Paid", Status = "Approved", Reason = "Bảo trì dây chuyền đóng hộp" },
            new OvertimeRequest { EmployeeId = 11, OvertimeDate = new DateOnly(2026, 9, 17), StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(16, 0), RequestedHours = 2m, ApprovedHours = 2m, OvertimeRateType = "OT150", CompensationMode = "Paid", Status = "Approved", Reason = "Chạy bù sản lượng" },
            // E14 (Ca chiều 14-22h): Trực ngày Lễ Quốc khánh 02/09 (8h OT300)
            new OvertimeRequest { EmployeeId = 14, OvertimeDate = new DateOnly(2026, 9, 2), StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(22, 0), RequestedHours = 8m, ApprovedHours = 8m, OvertimeRateType = "OT300", CompensationMode = "Paid", Status = "Approved", Reason = "Trực vận hành lò hơi Lễ 02/09" },
            // E18 (Ca đêm 22-06h): Trực đêm Lễ Quốc khánh 02/09 (8h OT300 đêm)
            new OvertimeRequest { EmployeeId = 18, OvertimeDate = new DateOnly(2026, 9, 2), StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0), RequestedHours = 8m, ApprovedHours = 8m, OvertimeRateType = "OT300", CompensationMode = "Paid", Status = "Approved", Reason = "Trực ca đêm Lễ 02/09" });

        // 6. Sinh ShiftSchedule & Log chấm công thô (AttendanceLog) cho tháng 09/2026 (01/09 -> 30/09)
        var shiftsById = await db.WorkShifts.AsNoTracking().ToDictionaryAsync(w => w.Id);
        var sepStart = new DateOnly(2026, 9, 1);
        var sepEnd = new DateOnly(2026, 9, 30);

        for (var d = sepStart; d <= sepEnd; d = d.AddDays(1))
        {
            bool isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            bool isHoliday = d == new DateOnly(2026, 9, 1) || d == new DateOnly(2026, 9, 2);

            foreach (var s in specs)
            {
                bool beforeHire = d < s.HireDate;
                bool afterTermination = s.TerminationDate.HasValue && d >= s.TerminationDate.Value;
                bool scheduleOff = isWeekend || beforeHire || afterTermination;

                db.ShiftSchedules.Add(new ShiftSchedule
                {
                    EmployeeId = s.Id,
                    WorkShiftId = s.ShiftId,
                    WorkDate = d,
                    IsDayOff = scheduleOff
                });

                if (scheduleOff)
                    continue;

                var shift = shiftsById[s.ShiftId];

                // Nghỉ phép hoặc Công tác cả ngày -> không sinh punch (trừ Lễ nếu được phân trực OT)
                if ((s.Id == 7 && (d == new DateOnly(2026, 9, 16) || d == new DateOnly(2026, 9, 17)))
                    || (s.Id == 8 && d == new DateOnly(2026, 9, 21))
                    || (s.Id == 23 && d == new DateOnly(2026, 9, 18)))
                {
                    continue;
                }

                // Ngày lễ 01/09 & 02/09 nghỉ hưởng nguyên lương; chỉ E14 & E18 trực OT ngày 02/09
                bool hasApprovedHolidayOt = d == new DateOnly(2026, 9, 2) && (s.Id == 14 || s.Id == 18);
                if (isHoliday && !hasApprovedHolidayOt)
                    continue;

                DateTime inTime = d.ToDateTime(shift.StartTime);
                DateTime outTime = shift.IsOvernight
                    ? d.AddDays(1).ToDateTime(shift.EndTime)
                    : d.ToDateTime(shift.EndTime);

                // Kịch bản đi muộn vượt Grace 5 phút
                if (s.Id == 5 && (d == new DateOnly(2026, 9, 7) || d == new DateOnly(2026, 9, 14)))
                    inTime = inTime.AddMinutes(25);
                if (s.Id == 10 && (d == new DateOnly(2026, 9, 8) || d == new DateOnly(2026, 9, 15)))
                    inTime = inTime.AddMinutes(20);
                if (s.Id == 19 && d == new DateOnly(2026, 9, 9))
                    inTime = inTime.AddMinutes(20);

                // Kịch bản tăng ca OT tối của E11 (14:00 -> 16:00)
                if (s.Id == 11 && (d == new DateOnly(2026, 9, 10) || d == new DateOnly(2026, 9, 17)))
                    outTime = outTime.AddHours(2);

                // Kịch bản quên chấm công (chỉ có 1 lượt quẹt thẻ -> engine đánh TC)
                bool skipCheckOut = (s.Id == 6 && d == new DateOnly(2026, 9, 10))
                                    || (s.Id == 29 && d == new DateOnly(2026, 9, 24));
                bool skipCheckIn = s.Id == 15 && d == new DateOnly(2026, 9, 11);

                if (!skipCheckIn)
                {
                    db.AttendanceLogs.Add(new AttendanceLog
                    {
                        EmployeeId = s.Id,
                        PunchTime = inTime,
                        PunchType = "CheckIn",
                        SourceType = s.UnitId == 2 ? "BiometricDevice" : "Web",
                        GeofenceStatus = "Inside"
                    });
                }

                if (!skipCheckOut)
                {
                    db.AttendanceLogs.Add(new AttendanceLog
                    {
                        EmployeeId = s.Id,
                        PunchTime = outTime,
                        PunchType = "CheckOut",
                        SourceType = s.UnitId == 2 ? "BiometricDevice" : "Web",
                        GeofenceStatus = "Inside"
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        // =========================================================================
        // BƯỚC 1: TÍNH CÔNG LÔ TỪ LOG THÔ & CHỐT CÔNG 3 BƯỚC
        // =========================================================================
        var cbUser = new StubCurrentUser { UserId = 10, Username = "cb.specialist", EmployeeId = 1 };
        var dirUser = new StubCurrentUser { UserId = 20, Username = "hr.director", EmployeeId = 3 };
        ICurrentUserService activeActor = cbUser;
        var wfSender = new TestWorkflowSender(db, () => activeActor);

        var dispatcher = new InProcessAttendanceRecalculationDispatcher(db, new AttendanceRecalculationService(db));
        var batchId = await new StartAttendanceRecalculationCommandHandler(dispatcher, cbUser)
            .Handle(new StartAttendanceRecalculationCommand(9, 2026), CancellationToken.None);
        var batchStatus = await new GetRecalculationBatchStatusQueryHandler(db)
            .Handle(new GetRecalculationBatchStatusQuery(batchId), CancellationToken.None);
        batchStatus!.Status.Should().Be("Completed");
        batchStatus.ProcessedEmployees.Should().Be(30);

        await new AdvanceAttendanceClosingCommandHandler(db, cbUser)
            .Handle(new AdvanceAttendanceClosingCommand(9, 2026, "UnitSubmit"), CancellationToken.None);
        await new AdvanceAttendanceClosingCommandHandler(db, dirUser)
            .Handle(new AdvanceAttendanceClosingCommand(9, 2026, "ManagerConfirm"), CancellationToken.None);
        await new AdvanceAttendanceClosingCommandHandler(db, dirUser)
            .Handle(new AdvanceAttendanceClosingCommand(9, 2026, "FinalLock", ForceLockBeforeReviewDeadline: true), CancellationToken.None);

        (await db.MonthlyAttendanceSummaries.CountAsync(m => m.Month == 9 && m.Year == 2026 && m.IsLocked)).Should().Be(30);

        // Kiểm chứng log lỗi chấm công thực tế đã được engine bắt đúng:
        var dailies = await db.DailyAttendances.AsNoTracking().Include(d => d.WorkShift).Where(d => d.AttendanceDate >= sepStart && d.AttendanceDate <= sepEnd).ToListAsync();
        dailies.Count(d => d.IsMissedPunch && d.AttendanceSymbol == "TC").Should().Be(3); // E06, E15, E29
        dailies.Sum(d => d.LateMinutes).Should().BeGreaterThan(0); // E05, E10, E19
        dailies.Where(d => d.EmployeeId == 14 && d.AttendanceDate == new DateOnly(2026, 9, 2)).Sum(d => d.OvertimeHours300).Should().Be(8m);
        dailies.Where(d => d.EmployeeId == 18 && d.AttendanceDate == new DateOnly(2026, 9, 2)).Sum(d => d.OvertimeHours300Night).Should().Be(8m);
        dailies.Where(d => d.EmployeeId is >= 17 and <= 20).Sum(d => d.NightShiftHours).Should().BeGreaterThan(0m);

        // E30 nghỉ việc chính thức ngày 20/09 sau khi chốt công
        var e30 = await db.Employees.FirstAsync(e => e.Id == 30);
        e30.WorkStatus = "Terminated";
        e30.TerminationDate = new DateOnly(2026, 9, 20);
        await db.SaveChangesAsync();

        // =========================================================================
        // BƯỚC 2: TẠO KỲ LƯƠNG -> TÍNH LƯƠNG (BẮT BUỘC CHỐT CÔNG) -> DUYỆT SoD -> KHÓA
        // =========================================================================
        int sepPeriodId = await new CreatePayrollPeriodCommandHandler(db)
            .Handle(new CreatePayrollPeriodCommand(2026, 9, StandardWorkdays: 22m, DisbursementDate: new DateOnly(2026, 10, 5)), CancellationToken.None);

        int calcCount = await new CalculateMonthlyPayrollCommandHandler(db, cbUser)
            .Handle(new CalculateMonthlyPayrollCommand(sepPeriodId, RequireLockedAttendance: true), CancellationToken.None);
        calcCount.Should().Be(30);

        activeActor = cbUser;
        await new SubmitPayrollPeriodCommandHandler(db, cbUser, wfSender)
            .Handle(new SubmitPayrollPeriodCommand(sepPeriodId, "Trình bảng lương song song 30 NV T09/2026"), CancellationToken.None);

        activeActor = dirUser;
        await new ApprovePayrollPeriodCommandHandler(db, dirUser, wfSender)
            .Handle(new ApprovePayrollPeriodCommand(sepPeriodId, Approve: true, Comment: "Phê duyệt T09/2026"), CancellationToken.None);
        await new LockPayrollPeriodCommandHandler(db, dirUser)
            .Handle(new LockPayrollPeriodCommand(sepPeriodId), CancellationToken.None);

        var lockedPeriod = await db.PayrollPeriods.FirstAsync(p => p.Id == sepPeriodId);
        lockedPeriod.Status.Should().Be(PayrollPeriodStatuses.Locked);
        lockedPeriod.IsLocked.Should().BeTrue();

        // =========================================================================
        // BƯỚC 3: PHIẾU LƯƠNG PDF (30 NV), NGÂN HÀNG, BÁO CÁO A3/COST CENTER/BHXH/PIT/KẾ TOÁN
        // =========================================================================
        var records = await db.PayrollRecords
            .AsNoTracking()
            .Include(r => r.Employee)
            .Include(r => r.OrganizationUnit)
            .Include(r => r.Position)
            .Where(r => r.PayrollPeriodId == sepPeriodId)
            .OrderBy(r => r.Employee.EmployeeCode)
            .ToListAsync();
        records.Should().HaveCount(30);

        var pdfHandler = new GetPayrollRecordPdfQueryHandler(db, cbUser, docs);
        foreach (var rec in records)
        {
            var pdf = await pdfHandler.Handle(new GetPayrollRecordPdfQuery(rec.Id), CancellationToken.None);
            pdf.PdfBytes.Take(4).Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        }

        var bankExport = await new ExportBankTransferFileQueryHandler(db, pii, new IBankTransferTemplateFormatter[] { new CommonBankTransferTemplateFormatter() })
            .Handle(new ExportBankTransferFileQuery(sepPeriodId, "excel", "COMMON"), CancellationToken.None);
        var unitReport = await new GetDetailedUnitPayrollReportQueryHandler(db)
            .Handle(new GetDetailedUnitPayrollReportQuery(sepPeriodId), CancellationToken.None);
        var unitA3Pdf = await new ExportDetailedUnitPayrollA3PdfQueryHandler(db, docs)
            .Handle(new ExportDetailedUnitPayrollA3PdfQuery(sepPeriodId), CancellationToken.None);
        var costCenterReport = await new GetCostCenterLaborCostQueryHandler(db)
            .Handle(new GetCostCenterLaborCostQuery(sepPeriodId), CancellationToken.None);
        var siReport = await new GetSocialInsuranceMonthlyReportQueryHandler(db)
            .Handle(new GetSocialInsuranceMonthlyReportQuery(2026, 9, sepPeriodId), CancellationToken.None);
        var siExcel = await new ExportSocialInsuranceExcelQueryHandler(db, docs)
            .Handle(new ExportSocialInsuranceExcelQuery(2026, 9, "D02-LT", sepPeriodId), CancellationToken.None);
        var pitDecl = await new GetPitDeclarationSummaryQueryHandler(db)
            .Handle(new GetPitDeclarationSummaryQuery(2026, Month: 9), CancellationToken.None);
        var pitFinal = await new GetAnnualPitFinalizationQueryHandler(db, pii)
            .Handle(new GetAnnualPitFinalizationQuery(2026), CancellationToken.None);
        var journal = await new GeneratePayrollAccountingEntriesQueryHandler(db)
            .Handle(new GeneratePayrollAccountingEntriesQuery(sepPeriodId), CancellationToken.None);
        var journalExcel = await new ExportAccountingJournalQueryHandler(db, docs)
            .Handle(new ExportAccountingJournalQuery(sepPeriodId, "excel"), CancellationToken.None);
        var dashboard = await new GetCompensationDashboardQueryHandler(db)
            .Handle(new GetCompensationDashboardQuery(2026, sepPeriodId), CancellationToken.None);
        var essSample = await new GetEssMssPortalQueryHandler(db, cbUser)
            .Handle(new GetEssMssPortalQuery(EmployeeId: 14, TimesheetYear: 2026, TimesheetMonth: 9, PitYear: 2026), CancellationToken.None);

        // =========================================================================
        // BƯỚC 4: KIỂM CHỨNG ĐỐI CHIẾU TỪNG ĐỒNG TOÀN BỘ HỆ THỐNG BÁO CÁO
        // =========================================================================
        decimal totalNetRecords = records.Sum(r => r.NetSalary);
        decimal totalGrossRecords = records.Sum(r => r.GrossIncome);
        decimal totalPitRecords = records.Sum(r => r.PersonalIncomeTax);
        decimal totalCompCostRecords = records.Sum(r => r.TotalCompanyCost);

        unitReport.TotalEmployees.Should().Be(30);
        unitReport.GrandTotalGross.Should().Be(totalGrossRecords);
        unitReport.GrandTotalNet.Should().Be(totalNetRecords);
        unitReport.GrandTotalCompanyCost.Should().Be(totalCompCostRecords);
        unitA3Pdf.PdfBytes.Take(4).Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        costCenterReport.GrandTotalNet.Should().Be(totalNetRecords);
        costCenterReport.GrandTotalGross.Should().Be(totalGrossRecords);
        costCenterReport.GrandTotalCompanyCost.Should().Be(totalCompCostRecords);

        bankExport.TotalAmount.Should().Be(totalNetRecords);

        journal.IsBalanced.Should().BeTrue();
        journal.TotalDebit.Should().Be(journal.TotalCredit);
        journal.PayableToEmployeesAccount334.Should().Be(totalNetRecords);
        journalExcel.FileBytes.Length.Should().BeGreaterThan(1000);

        siReport.Movements.Should().Contain(m => m.EmployeeCode == "VNM029" && m.ChangeCode == "TANG_MOI");
        siReport.Movements.Should().Contain(m => m.EmployeeCode == "VNM003" && m.ChangeCode == "DIEU_CHINH_TANG");
        siReport.Reconciliation.ReconciliationDifference.Should().Be(0m);
        siExcel.FileBytes.Length.Should().BeGreaterThan(1000);

        pitDecl.GrandTotalPitWithheld.Should().Be(totalPitRecords);
        pitFinal.Employees.Should().HaveCount(30);
        dashboard.FocusTotalGross.Should().Be(totalGrossRecords);
        essSample.MonthlyTimesheet.Should().NotBeEmpty();

        // Kiểm chứng tính đúng các trường hợp đặc thù:
        var rProbNo08 = records.First(r => r.EmployeeId == 25); // Thử việc không Cam kết 08 -> 10% PIT, 0 BHXH
        var rProbWith08 = records.First(r => r.EmployeeId == 26); // Thử việc CÓ Cam kết 08 -> 0 PIT
        var rNonRes = records.First(r => r.EmployeeId == 27); // Chuyên gia không cư trú -> 20% PIT phẳng
        rProbNo08.TotalEmployeeInsurance.Should().Be(0m);
        rProbNo08.PersonalIncomeTax.Should().BeGreaterThan(0m);
        rProbWith08.PersonalIncomeTax.Should().Be(0m);
        rNonRes.TaxResidencyMode.Should().Be("NonResident");
        rNonRes.PersonalIncomeTax.Should().Be(Math.Round(rNonRes.AssessableIncome * 0.20m, 0, MidpointRounding.AwayFromZero));
        dailies.Count(x => x.AttendanceSymbol == "L").Should().Be(58); // 29 NV hiện hữu x 2 ngày Quốc khánh (02/09 & 03/09)

        // =========================================================================
        // BƯỚC 5: XUẤT FILE EXCEL ĐỐI CHIẾU CHO KẾ TOÁN KIỂM TRA TAY
        // =========================================================================
        var statutory = await VietnamPayrollEngine.ResolveStatutoryContextAsync(db, new DateOnly(2026, 9, 30));
        var summariesByEmp = (await db.MonthlyAttendanceSummaries.AsNoTracking().Where(m => m.Month == 9 && m.Year == 2026).ToListAsync())
            .ToDictionary(m => m.EmployeeId);

        string outputPath = ResolveAuditWorkbookPath();
        ExportAccountantVerificationWorkbook(
            outputPath,
            specs,
            records,
            summariesByEmp,
            dailies,
            unitReport,
            costCenterReport,
            bankExport,
            siReport,
            pitDecl,
            journal,
            statutory);

        File.Exists(outputPath).Should().BeTrue();
        new FileInfo(outputPath).Length.Should().BeGreaterThan(10_000);
    }

    private static string ResolveAuditWorkbookPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Vinamilk.HRM.sln")))
            dir = dir.Parent;

        string root = dir?.FullName ?? Directory.GetCurrentDirectory();
        string targetDir = Path.Combine(root, "docs", "testdata");
        Directory.CreateDirectory(targetDir);
        return Path.Combine(targetDir, "DoiChieu_Luong_30NV_T09_2026.xlsx");
    }

    private static void ExportAccountantVerificationWorkbook(
        string outputPath,
        IReadOnlyList<EmployeeScenarioSpec> specs,
        IReadOnlyList<PayrollRecord> records,
        IReadOnlyDictionary<int, MonthlyAttendanceSummary> summariesByEmp,
        IReadOnlyList<DailyAttendance> dailies,
        DetailedUnitPayrollReportDto unitReport,
        CostCenterLaborCostReportDto costCenterReport,
        BankTransferExportResult bankExport,
        SocialInsuranceMonthlyReportDto siReport,
        PitDeclarationSummaryDto pitDecl,
        PayrollAccountingJournalDto journal,
        PayrollStatutoryContext statutory)
    {
        var specMap = specs.ToDictionary(s => s.Id);
        using var wb = new XLWorkbook();

        // SHEET 1: 01_TongHop_30NV
        var ws1 = wb.Worksheets.Add("01_TongHop_30NV");
        ws1.Cell(1, 1).Value = "BẢNG ĐỐI CHIẾU CHI TIẾT CÔNG – LƯƠNG – BHXH – THUẾ TNCN 30 NHÂN VIÊN (THÁNG 09/2026)";
        ws1.Cell(1, 1).Style.Font.Bold = true;
        ws1.Cell(1, 1).Style.Font.FontSize = 13;

        string[] h1 =
        [
            "STT", "Mã NV", "Họ và tên", "Nhóm kịch bản", "Đơn vị", "Cost Center",
            "Hợp đồng / Thuế", "Lương HĐ", "Công chuẩn", "Công thực tế", "Số lần trễ", "Quên chấm (TC)",
            "Giờ OT150", "Giờ OT300", "Giờ Đêm", "Lương thực tế", "Phụ cấp", "OT + Đêm",
            "Thưởng/HH", "Tổng Gross", "Quỹ BHXH", "BH NLĐ", "Thuế TNCN", "Tạm ứng/Phạt/CĐ",
            "Thực lĩnh Net", "BH + CĐ DN", "Tổng Chi phí DN", "Ghi chú kịch bản"
        ];
        for (int c = 0; c < h1.Length; c++)
        {
            ws1.Cell(3, c + 1).Value = h1[c];
            ws1.Cell(3, c + 1).Style.Font.Bold = true;
            ws1.Cell(3, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            ws1.Cell(3, c + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 4;
        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            var sp = specMap[r.EmployeeId];
            summariesByEmp.TryGetValue(r.EmployeeId, out var att);
            decimal otPay = r.OvertimePay;
            decimal bonuses = r.KpiBonus + r.CommissionBonus + r.ThirteenthMonthBonus + r.RetroEarningAmount + r.UnusedLeavePayout;
            decimal otherDeds = r.EmployeeUnionFee + r.SalaryAdvanceDeduction + r.LoanInstallmentDeduction + r.DisciplineDeduction + r.RetroDeductionAmount;
            decimal compBurden = r.CompanySocialInsurance + r.CompanyHealthInsurance + r.CompanyUnemploymentInsurance + r.CompanyOccupationalAccidentInsurance + r.CompanyUnionFee;

            ws1.Cell(row, 1).Value = i + 1;
            ws1.Cell(row, 2).Value = r.Employee.EmployeeCode;
            ws1.Cell(row, 3).Value = r.Employee.FullName;
            ws1.Cell(row, 4).Value = sp.CohortGroup;
            ws1.Cell(row, 5).Value = r.OrganizationUnit.UnitName;
            ws1.Cell(row, 6).Value = r.OrganizationUnit.CostCenterCode ?? r.OrganizationUnit.UnitCode;
            ws1.Cell(row, 7).Value = $"{sp.ContractType} / {r.TaxResidencyMode}";
            ws1.Cell(row, 8).Value = r.ContractBaseSalary;
            ws1.Cell(row, 9).Value = r.StandardWorkdays;
            ws1.Cell(row, 10).Value = r.PayableWorkdays;
            ws1.Cell(row, 11).Value = att?.TotalLateCount ?? 0;
            ws1.Cell(row, 12).Value = att?.TotalMissedPunchCount ?? 0;
            ws1.Cell(row, 13).Value = att?.TotalOtHours150 ?? 0m;
            ws1.Cell(row, 14).Value = att?.TotalOtHours300 ?? 0m;
            ws1.Cell(row, 15).Value = att?.TotalNightHours ?? 0m;
            ws1.Cell(row, 16).Value = r.ActualBaseSalary;
            ws1.Cell(row, 17).Value = r.PositionAllowance + r.OtherAllowance;
            ws1.Cell(row, 18).Value = otPay;
            ws1.Cell(row, 19).Value = bonuses;
            ws1.Cell(row, 20).Value = r.GrossIncome;
            ws1.Cell(row, 21).Value = r.InsuranceSalaryBase;
            ws1.Cell(row, 22).Value = r.TotalEmployeeInsurance;
            ws1.Cell(row, 23).Value = r.PersonalIncomeTax;
            ws1.Cell(row, 24).Value = otherDeds;
            ws1.Cell(row, 25).Value = r.NetSalary;
            ws1.Cell(row, 26).Value = compBurden;
            ws1.Cell(row, 27).Value = r.TotalCompanyCost;
            ws1.Cell(row, 28).Value = sp.ScenarioNote;

            for (int c = 8; c <= 27; c++)
            {
                if (c is 9 or 10 or 13 or 14 or 15)
                    ws1.Cell(row, c).Style.NumberFormat.Format = "0.##";
                else if (c is 11 or 12)
                    ws1.Cell(row, c).Style.NumberFormat.Format = "#,##0";
                else
                    ws1.Cell(row, c).Style.NumberFormat.Format = "#,##0";
            }
            row++;
        }

        ws1.Cell(row, 3).Value = "TỔNG CỘNG 30 NHÂN VIÊN (CÔNG THỨC EXCEL)";
        ws1.Cell(row, 3).Style.Font.Bold = true;
        foreach (int colIdx in new[] { 8, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27 })
        {
            string colLet = ws1.Column(colIdx).ColumnLetter();
            ws1.Cell(row, colIdx).FormulaA1 = $"=SUM({colLet}4:{colLet}{row - 1})";
            ws1.Cell(row, colIdx).Style.Font.Bold = true;
            ws1.Cell(row, colIdx).Style.NumberFormat.Format = "#,##0";
            ws1.Cell(row, colIdx).Style.Fill.BackgroundColor = XLColor.FromHtml("#eef4ff");
        }
        ws1.Columns().AdjustToContents();

        // SHEET 2: 02_ChamCong_BatThuong
        var ws2 = wb.Worksheets.Add("02_ChamCong_BatThuong");
        ws2.Cell(1, 1).Value = "CHI TIẾT NGÀY CÔNG BẤT THƯỜNG / ĐẶC BIỆT (QUÊN CHẤM TC, ĐI MUỘN, CA ĐÊM, LỄ 01-02/09, PHÉP)";
        ws2.Cell(1, 1).Style.Font.Bold = true;
        string[] h2 = ["Ngày", "Mã NV", "Họ tên", "Ca làm việc", "Ký hiệu", "Giờ vào", "Giờ ra", "Công ngày", "Trễ (phút)", "Quên chấm", "OT150", "OT300 (Lễ)", "Giờ đêm", "Diễn giải engine"];
        for (int c = 0; c < h2.Length; c++)
        {
            ws2.Cell(3, c + 1).Value = h2[c];
            ws2.Cell(3, c + 1).Style.Font.Bold = true;
            ws2.Cell(3, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            ws2.Cell(3, c + 1).Style.Font.FontColor = XLColor.White;
        }
        var empMap = records.ToDictionary(r => r.EmployeeId, r => r.Employee);
        var abnormalDailies = dailies
            .Where(d => d.IsMissedPunch || d.LateMinutes > 0 || d.OvertimeHours150 > 0m || d.OvertimeHours300 > 0m
                        || d.NightShiftHours > 0m || d.AttendanceSymbol is "L" or "P" or "Ro" or "KL" or "CT")
            .OrderBy(d => d.AttendanceDate).ThenBy(d => d.EmployeeId).ToList();
        int r2 = 4;
        foreach (var d in abnormalDailies)
        {
            var e = empMap[d.EmployeeId];
            ws2.Cell(r2, 1).Value = d.AttendanceDate.ToString("dd/MM/yyyy");
            ws2.Cell(r2, 2).Value = e.EmployeeCode;
            ws2.Cell(r2, 3).Value = e.FullName;
            ws2.Cell(r2, 4).Value = d.WorkShift?.ShiftName ?? "Mặc định";
            ws2.Cell(r2, 5).Value = d.AttendanceSymbol;
            ws2.Cell(r2, 6).Value = d.CheckInTime?.ToString("dd/MM HH:mm") ?? "--";
            ws2.Cell(r2, 7).Value = d.CheckOutTime?.ToString("dd/MM HH:mm") ?? "--";
            ws2.Cell(r2, 8).Value = d.WorkdayCount + d.PaidLeaveDays;
            ws2.Cell(r2, 9).Value = d.LateMinutes;
            ws2.Cell(r2, 10).Value = d.IsMissedPunch ? "CÓ (TC)" : "";
            ws2.Cell(r2, 11).Value = d.OvertimeHours150;
            ws2.Cell(r2, 12).Value = d.OvertimeHours300;
            ws2.Cell(r2, 13).Value = d.NightShiftHours;
            ws2.Cell(r2, 14).Value = d.EditReason ?? string.Empty;
            r2++;
        }
        ws2.Columns().AdjustToContents();

        // SHEET 3: 03_DoiChieu_TongHop
        var ws3 = wb.Worksheets.Add("03_DoiChieu_TongHop");
        ws3.Cell(1, 1).Value = "BẢNG KIỂM TOÁN ĐỐI CHIẾU TỔNG HỢP (RECONCILIATION MATRIX)";
        ws3.Cell(1, 1).Style.Font.Bold = true;
        string[] h3 = ["STT", "Chỉ tiêu kiểm tra chéo", "Nguồn A (Báo cáo / Ngân hàng / Kế toán)", "Giá trị A", "Nguồn B (Tổng 30 Phiếu lương)", "Giá trị B", "Chênh lệch (A - B)", "Kết luận"];
        for (int c = 0; c < h3.Length; c++)
        {
            ws3.Cell(3, c + 1).Value = h3[c];
            ws3.Cell(3, c + 1).Style.Font.Bold = true;
            ws3.Cell(3, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            ws3.Cell(3, c + 1).Style.Font.FontColor = XLColor.White;
        }
        decimal sumNet = records.Sum(x => x.NetSalary);
        decimal sumGross = records.Sum(x => x.GrossIncome);
        decimal sumPit = records.Sum(x => x.PersonalIncomeTax);
        decimal sumCompCost = records.Sum(x => x.TotalCompanyCost);

        (string Name, string SrcA, decimal ValA, string SrcB, decimal ValB)[] checks =
        [
            ("Tổng Thực lĩnh Net vs UNC Ngân hàng", "BankTransferExport.TotalAmount", bankExport.TotalAmount, "Sum(30 PayrollRecord.NetSalary)", sumNet),
            ("Tổng Thực lĩnh Net vs Báo cáo Đơn vị A3 PDF", "DetailedUnitPayrollReport.GrandTotalNet", unitReport.GrandTotalNet, "Sum(30 PayrollRecord.NetSalary)", sumNet),
            ("Tổng Thực lĩnh Net vs Báo cáo Cost Center", "CostCenterLaborCost.GrandTotalNet", costCenterReport.GrandTotalNet, "Sum(30 PayrollRecord.NetSalary)", sumNet),
            ("Tổng Thực lĩnh Net vs Dư Có TK 3341 Kế toán", "AccountingJournal.Payable3341", journal.PayableToEmployeesAccount334, "Sum(30 PayrollRecord.NetSalary)", sumNet),
            ("Cân đối Bút toán Kế toán (Nợ == Có)", "AccountingJournal.TotalDebit", journal.TotalDebit, "AccountingJournal.TotalCredit", journal.TotalCredit),
            ("Tổng Gross vs Tổng Chi phí Lương Cost Center", "CostCenterLaborCost.GrandTotalGross", costCenterReport.GrandTotalGross, "Sum(30 PayrollRecord.GrossIncome)", sumGross),
            ("Tổng Chi phí DN (Gross + BH/CĐ DN)", "CostCenterLaborCost.GrandTotalCompanyCost", costCenterReport.GrandTotalCompanyCost, "Sum(30 PayrollRecord.TotalCompanyCost)", sumCompCost),
            ("Đối chiếu BHXH phải nộp theo Tỷ lệ Pháp định", "SocialInsurance.ExpectedPayable", siReport.Reconciliation.TotalExpectedPayableToAgency, "SocialInsurance.ActualFromRecords", siReport.Reconciliation.TotalActualFromPayrollRecords),
            ("Đối chiếu Thuế TNCN vs Tờ khai 05/KK-TNCN", "PitDeclaration05KK.GrandTotalPit", pitDecl.GrandTotalPitWithheld, "Sum(30 PayrollRecord.PersonalIncomeTax)", sumPit)
        ];

        for (int i = 0; i < checks.Length; i++)
        {
            int rIdx = 4 + i;
            var chk = checks[i];
            ws3.Cell(rIdx, 1).Value = i + 1;
            ws3.Cell(rIdx, 2).Value = chk.Name;
            ws3.Cell(rIdx, 3).Value = chk.SrcA;
            ws3.Cell(rIdx, 4).Value = chk.ValA;
            ws3.Cell(rIdx, 5).Value = chk.SrcB;
            ws3.Cell(rIdx, 6).Value = chk.ValB;
            ws3.Cell(rIdx, 7).FormulaA1 = $"=D{rIdx}-F{rIdx}";
            ws3.Cell(rIdx, 8).Value = chk.ValA == chk.ValB ? "KHỚP 100%" : "LỆCH!";
            ws3.Cell(rIdx, 4).Style.NumberFormat.Format = "#,##0";
            ws3.Cell(rIdx, 6).Style.NumberFormat.Format = "#,##0";
            ws3.Cell(rIdx, 7).Style.NumberFormat.Format = "#,##0";
        }
        ws3.Columns().AdjustToContents();

        // SHEET 4: 04_ButToan_KeToan
        var ws4 = wb.Worksheets.Add("04_ButToan_KeToan");
        ws4.Cell(1, 1).Value = $"BÚT TOÁN HẠCH TOÁN KẾ TOÁN THÔNG TƯ 200 ({journal.PeriodCode})";
        ws4.Cell(1, 1).Style.Font.Bold = true;
        string[] h4 = ["STT", "Mã khóa", "TK Nợ", "TK Có", "Cost Center", "Diễn giải", "Số tiền (VNĐ)"];
        for (int c = 0; c < h4.Length; c++)
        {
            ws4.Cell(3, c + 1).Value = h4[c];
            ws4.Cell(3, c + 1).Style.Font.Bold = true;
            ws4.Cell(3, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            ws4.Cell(3, c + 1).Style.Font.FontColor = XLColor.White;
        }
        int r4 = 4;
        foreach (var l in journal.Lines)
        {
            ws4.Cell(r4, 1).Value = l.LineNo;
            ws4.Cell(r4, 2).Value = l.AccountKey;
            ws4.Cell(r4, 3).Value = l.DebitAccountCode;
            ws4.Cell(r4, 4).Value = l.CreditAccountCode;
            ws4.Cell(r4, 5).Value = l.CostCenterCode;
            ws4.Cell(r4, 6).Value = l.Description;
            ws4.Cell(r4, 7).Value = l.Amount;
            ws4.Cell(r4, 7).Style.NumberFormat.Format = "#,##0";
            r4++;
        }
        ws4.Columns().AdjustToContents();

        // SHEET 5: 05_BHXH_ThueTNCN
        var ws5 = wb.Worksheets.Add("05_BHXH_ThueTNCN");
        ws5.Cell(1, 1).Value = "BIẾN ĐỘNG BHXH (D02-LT) & TỜ KHAI THUẾ TNCN (05/KK-TNCN) THÁNG 09/2026";
        ws5.Cell(1, 1).Style.Font.Bold = true;
        string[] h5 = ["STT", "Mã NV", "Họ tên", "Mã số BHXH", "Mã biến động", "Diễn giải", "Mức T08", "Mức T09", "Chênh lệch", "Ghi chú"];
        for (int c = 0; c < h5.Length; c++)
        {
            ws5.Cell(3, c + 1).Value = h5[c];
            ws5.Cell(3, c + 1).Style.Font.Bold = true;
            ws5.Cell(3, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            ws5.Cell(3, c + 1).Style.Font.FontColor = XLColor.White;
        }
        int r5 = 4;
        foreach (var m in siReport.Movements)
        {
            ws5.Cell(r5, 1).Value = m.Stt;
            ws5.Cell(r5, 2).Value = m.EmployeeCode;
            ws5.Cell(r5, 3).Value = m.FullName;
            ws5.Cell(r5, 4).Value = m.SocialInsuranceNumber;
            ws5.Cell(r5, 5).Value = m.ChangeCode;
            ws5.Cell(r5, 6).Value = m.ChangeLabel;
            ws5.Cell(r5, 7).Value = m.PreviousSalaryBase;
            ws5.Cell(r5, 8).Value = m.CurrentSalaryBase;
            ws5.Cell(r5, 9).Value = m.SalaryBaseDelta;
            ws5.Cell(r5, 10).Value = m.Notes;
            for (int c = 7; c <= 9; c++) ws5.Cell(r5, c).Style.NumberFormat.Format = "#,##0";
            r5++;
        }
        r5 += 2;
        ws5.Cell(r5, 1).Value = "TỔNG HỢP TỜ KHAI 05/KK-TNCN";
        ws5.Cell(r5, 1).Style.Font.Bold = true;
        r5++;
        foreach (var pr in pitDecl.ResidencyRows)
        {
            ws5.Cell(r5, 1).Value = pr.ResidencyLabel;
            ws5.Cell(r5, 2).Value = $"{pr.EmployeeCount} NV";
            ws5.Cell(r5, 3).Value = pr.TotalGrossIncome;
            ws5.Cell(r5, 4).Value = pr.TotalAssessableIncome;
            ws5.Cell(r5, 5).Value = pr.TotalPitWithheld;
            ws5.Cell(r5, 3).Style.NumberFormat.Format = "#,##0";
            ws5.Cell(r5, 4).Style.NumberFormat.Format = "#,##0";
            ws5.Cell(r5, 5).Style.NumberFormat.Format = "#,##0";
            r5++;
        }
        ws5.Columns().AdjustToContents();

        // SHEET 6: 06_GiaDinh_ApDung
        var ws6 = wb.Worksheets.Add("06_GiaDinh_ApDung");
        ws6.Cell(1, 1).Value = "LIỆT KÊ TOÀN BỘ GIẢ ĐỊNH NGHIỆP VỤ & THAM SỐ LUẬT ÁP DỤNG KHI KIỂM THỬ SONG SONG T09/2026";
        ws6.Cell(1, 1).Style.Font.Bold = true;
        string[] h6 = ["STT", "Nhóm giả định", "Nội dung giả định đã áp dụng", "Giá trị thực tế từ hệ thống / Căn cứ"];
        for (int c = 0; c < h6.Length; c++)
        {
            ws6.Cell(3, c + 1).Value = h6[c];
            ws6.Cell(3, c + 1).Style.Font.Bold = true;
            ws6.Cell(3, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            ws6.Cell(3, c + 1).Style.Font.FontColor = XLColor.White;
        }
        var pStat = statutory.Parameter;
        (string Group, string Assumption, string Value)[] assumptions =
        [
            ("Chế độ công chuẩn", "Tuần làm việc Thứ 2 – Thứ 6 (Mon..Fri), tháng 09/2026 gồm 22 ngày công chuẩn (20 ngày làm việc thường + 2 ngày Lễ Quốc khánh 01/09 & 02/09 hưởng nguyên lương).", "StandardWorkdays = 22 (gồm 2 ngày L)"),
            ("Ngày Lễ & Làm thêm Lễ", "Quốc khánh 2026 gồm 2 ngày: 02/09 + 1 ngày liền kề (mặc định 01/09 theo Thông báo 9441/TB-BNV, HR có thể chọn 01/09 hoặc 03/09). E14 & E18 trực Lễ 02/09 có OvertimeRequest Approved.", "Hệ số OT300 = 300%; Đêm Lễ có phụ trội đêm"),
            ("Ca 3 kíp & Phụ cấp đêm", "Nhà máy chia CA_SANG (06-14h), CA_CHIEU (14-22h), CA_DEM (22-06h qua đêm, IsOvernight = true). Đêm nghỉ Lễ 01/09 & 02/09 -> 20 ca đêm thường (160h).", $"NightShiftAllowanceRate = {0.30m:P0}"),
            ("Xử lý lỗi chấm công", "Đi muộn vượt Grace 5 phút bị ghi nhận LateMinutes; Quên chấm vào/ra (chỉ 1 punch, không giải trình) đánh TC = 0 công ngày đó.", "E05, E10, E19 đi muộn; E06, E15, E29 quên chấm TC"),
            ("Mức lương cơ sở & Vùng BHTN", "HO_ADMIN & SALES_VN thuộc Vùng I; Nhà máy NM_BS thuộc Vùng II.", $"LCS = {pStat.StatutoryBaseSalary:N0} đ; Vùng 1 = {pStat.MinWageRegion1:N0} đ; Vùng 2 = {pStat.MinWageRegion2:N0} đ"),
            ("Tỷ lệ BHXH-BHYT-BHTN & CĐ", "Áp dụng đồng nhất trên Quỹ BH có trần 20 x LCS (BHXH/BHYT) & 20 x LTT Vùng (BHTN). DN BHXH = 17% (HT-TT 14% + ÔĐ-TS 3%) + 0.5% TNLĐ-BNN = 17.5%; KPCĐ 2% tách riêng.", $"NLĐ: {pStat.EmployeeSiRate + pStat.EmployeeHiRate + pStat.EmployeeUiRate:P1}; DN: {pStat.CompanySiRate + pStat.CompanyOccupationalAccidentRate + pStat.CompanyHiRate + pStat.CompanyUiRate:P1} (BHXH {pStat.CompanySiRate + pStat.CompanyOccupationalAccidentRate:P1}); KPCĐ DN: {pStat.CompanyUnionFeeRate:P1}"),
            ("Thuế TNCN 2026 & Giảm trừ", "Áp dụng tham số hiệu lực 30/09/2026 trong bảng PayrollStatutoryParameters & PitBrackets.", $"Bản thân = {pStat.SelfDeduction:N0} đ; NPT = {pStat.DependentDeduction:N0} đ; Không cư trú = {pStat.PitNonResidentRate:P0}; HĐ<3T = {pStat.PitShortTermRate:P0}"),
            ("Nhóm Thử việc (E25, E26)", "Lương thử việc bằng 85% lương cơ bản, không đóng BHXH bắt buộc (InsuranceSalary = 0), thuế ResidentShortTerm 10% (E26 miễn do có Cam kết 08).", "E25: Khấu trừ 10%; E26: HasPitCommitment08 = true => 0 đ"),
            ("Chuyên gia nước ngoài (E27, E28)", "E27 không cư trú (TaxResidencyMode = NonResident, không công đoàn); E28 cư trú dài hạn (ResidentStandard).", "E27 chịu thuế 20% toàn phần; E28 tính biểu lũy tiến"),
            ("Vào/Nghỉ giữa tháng (E29, E30)", "E29 nhận việc 15/09 (lương tỷ lệ công, báo TANG_MOI D02-LT); E30 làm đến hết 19/09 rồi nghỉ việc 20/09 (hưởng lương theo công, vẫn đóng BH T09 theo mốc >=14 ngày công).", "E30 chuyển Terminated sau khi chốt công T09 trước khi khóa lương"),
            ("Hệ thống tài khoản Kế toán", "Định khoản theo Thông tư 200 từ bảng PayrollAccountingAccountConfigs: NM_BS -> 6221, SALES_VN -> 6411, HO_ADMIN -> 6421; Công nợ trả NLĐ -> 3341.", $"TK 3341 = {journal.PayableToEmployeesAccount334:N0} đ (khớp Bank Transfer)")
        ];
        for (int i = 0; i < assumptions.Length; i++)
        {
            int rIdx = 4 + i;
            ws6.Cell(rIdx, 1).Value = i + 1;
            ws6.Cell(rIdx, 2).Value = assumptions[i].Group;
            ws6.Cell(rIdx, 3).Value = assumptions[i].Assumption;
            ws6.Cell(rIdx, 4).Value = assumptions[i].Value;
        }
        ws6.Columns().AdjustToContents();

        wb.SaveAs(outputPath);
    }

    private static List<EmployeeScenarioSpec> Build30EmployeeSpecs()
    {
        var list = new List<EmployeeScenarioSpec>(30);
        // 1-8: Văn phòng HO_ADMIN (Unit 1, Pos 1, Shift 1)
        list.Add(new(1, "VNM001", "Nguyễn An Bình", "Văn phòng HQ", 1, 1, 1, "Official", "Indefinite", new(2021, 3, 1), null, 28_000_000m, 2_000_000m, 500_000m, false, 100m, 28_000_000m, "ResidentStandard", false, true, 2, "079090000001", "0071000000001", "Vietcombank", "Văn phòng chuẩn, 2 người phụ thuộc"));
        list.Add(new(2, "VNM002", "Trần Thị Cúc", "Văn phòng HQ", 1, 1, 1, "Official", "Indefinite", new(2022, 1, 10), null, 24_000_000m, 1_500_000m, 500_000m, false, 100m, 24_000_000m, "ResidentStandard", false, true, 1, "079090000002", "0071000000002", "Vietcombank", "Văn phòng chuẩn, 1 người phụ thuộc"));
        list.Add(new(3, "VNM003", "Lê Hoàng Dũng", "Văn phòng HQ", 1, 1, 1, "Official", "Indefinite", new(2019, 6, 1), null, 52_000_000m, 5_000_000m, 1_000_000m, false, 100m, 46_800_000m, "ResidentStandard", false, true, 1, "079090000003", "0071000000003", "Techcombank", "Quản lý HQ chạm trần BHXH 46.8tr & tăng mức đóng so với T08"));
        list.Add(new(4, "VNM004", "Phạm Minh Đức", "Văn phòng HQ", 1, 1, 1, "Official", "Fixed12M", new(2024, 2, 1), null, 20_000_000m, 1_000_000m, 0m, false, 100m, 20_000_000m, "ResidentStandard", false, true, 0, "079090000004", "0071000000004", "VietinBank", "Có phiếu tạm ứng lương 3,000,000đ trừ cuối tháng"));
        list.Add(new(5, "VNM005", "Võ Thị Em", "Văn phòng HQ", 1, 1, 1, "Official", "Fixed12M", new(2024, 5, 1), null, 18_000_000m, 0m, 500_000m, false, 100m, 18_000_000m, "ResidentStandard", false, true, 0, "079090000005", "0071000000005", "BIDV", "Đi muộn 2 ngày (07/09 & 14/09, mỗi lần 25 phút)"));
        list.Add(new(6, "VNM006", "Đặng Quốc Gia", "Văn phòng HQ", 1, 1, 1, "Official", "Fixed12M", new(2023, 8, 15), null, 22_000_000m, 1_000_000m, 0m, false, 100m, 22_000_000m, "ResidentStandard", false, true, 0, "079090000006", "0071000000006", "ACB", "Quên chấm giờ ra ngày 10/09 -> bị đánh TC mất 1 công"));
        list.Add(new(7, "VNM007", "Bùi Thu Hà", "Văn phòng HQ", 1, 1, 1, "Official", "Indefinite", new(2021, 11, 1), null, 21_000_000m, 500_000m, 500_000m, false, 100m, 21_000_000m, "ResidentStandard", false, true, 1, "079090000007", "0071000000007", "MBBank", "Nghỉ phép năm 2 ngày (16-17/09) vẫn hưởng đủ lương"));
        list.Add(new(8, "VNM008", "Hoàng Văn Hải", "Văn phòng HQ", 1, 1, 1, "Official", "Fixed12M", new(2024, 1, 20), null, 19_000_000m, 0m, 0m, false, 100m, 19_000_000m, "ResidentStandard", false, true, 0, "079090000008", "0071000000008", "Sacombank", "Nghỉ không lương 1 ngày (21/09) -> trừ tỷ lệ công"));

        // 9-12: Nhà máy Ca Sáng (Unit 2, Pos 2, Shift 2)
        list.Add(new(9, "VNM009", "Ngô Tấn Khang", "Nhà máy Ca Sáng", 2, 2, 2, "Official", "Indefinite", new(2020, 4, 1), null, 15_000_000m, 500_000m, 500_000m, false, 100m, 15_000_000m, "ResidentStandard", false, true, 1, "079090000009", "0071000000009", "Vietcombank", "Nhà máy Vùng II - Ca 1 Sáng chuẩn đủ 22 công"));
        list.Add(new(10, "VNM010", "Lý Ngọc Lan", "Nhà máy Ca Sáng", 2, 2, 2, "Official", "Fixed12M", new(2023, 3, 1), null, 14_000_000m, 0m, 500_000m, false, 100m, 14_000_000m, "ResidentStandard", false, true, 0, "079090000010", "0071000000010", "VietinBank", "Nhà máy Vùng II - Ca Sáng đi muộn 2 ngày (20 phút/lần)"));
        list.Add(new(11, "VNM011", "Trương Quang Minh", "Nhà máy Ca Sáng", 2, 2, 2, "Official", "Indefinite", new(2021, 7, 1), null, 16_000_000m, 500_000m, 500_000m, false, 100m, 16_000_000m, "ResidentStandard", false, true, 2, "079090000011", "0071000000011", "BIDV", "Nhà máy Vùng II - Ca Sáng OT150 tổng 4 giờ (10/09 & 17/09)"));
        list.Add(new(12, "VNM012", "Phan Thị Nga", "Nhà máy Ca Sáng", 2, 2, 2, "Official", "Fixed12M", new(2024, 6, 1), null, 13_500_000m, 0m, 500_000m, false, 100m, 13_500_000m, "ResidentStandard", false, true, 0, "079090000012", "0071000000012", "Techcombank", "Nhà máy Vùng II - Ca Sáng chuẩn"));

        // 13-16: Nhà máy Ca Chiều (Unit 2, Pos 2, Shift 3)
        list.Add(new(13, "VNM013", "Huỳnh Văn Oanh", "Nhà máy Ca Chiều", 2, 2, 3, "Official", "Indefinite", new(2020, 9, 1), null, 15_500_000m, 500_000m, 500_000m, false, 100m, 15_500_000m, "ResidentStandard", false, true, 1, "079090000013", "0071000000013", "Vietcombank", "Nhà máy Vùng II - Ca 2 Chiều chuẩn"));
        list.Add(new(14, "VNM014", "Đoàn Thanh Phúc", "Nhà máy Ca Chiều", 2, 2, 3, "Official", "Indefinite", new(2021, 2, 1), null, 16_500_000m, 1_000_000m, 500_000m, false, 100m, 16_500_000m, "ResidentStandard", false, true, 0, "079090000014", "0071000000014", "ACB", "Nhà máy Vùng II - Trực Lễ Quốc khánh 02/09 (8h OT 300%)"));
        list.Add(new(15, "VNM015", "Mai Thị Quyên", "Nhà máy Ca Chiều", 2, 2, 3, "Official", "Fixed12M", new(2023, 10, 1), null, 14_500_000m, 0m, 500_000m, false, 100m, 14_500_000m, "ResidentStandard", false, true, 0, "079090000015", "0071000000015", "MBBank", "Nhà máy Vùng II - Quên chấm vào ngày 11/09 (TC)"));
        list.Add(new(16, "VNM016", "Lâm Quốc Rạng", "Nhà máy Ca Chiều", 2, 2, 3, "Official", "Fixed12M", new(2024, 4, 1), null, 14_000_000m, 0m, 500_000m, false, 100m, 14_000_000m, "ResidentStandard", false, true, 0, "079090000016", "0071000000016", "VietinBank", "Nhà máy Vùng II - Ca Chiều chuẩn"));

        // 17-20: Nhà máy Ca Đêm (Unit 2, Pos 2, Shift 4)
        list.Add(new(17, "VNM017", "Tô Văn Sơn", "Nhà máy Ca Đêm", 2, 2, 4, "Official", "Indefinite", new(2021, 1, 5), null, 16_000_000m, 500_000m, 500_000m, false, 100m, 16_000_000m, "ResidentStandard", false, true, 1, "079090000017", "0071000000017", "Vietcombank", "Nhà máy Vùng II - Ca 3 Đêm hưởng phụ cấp +30%"));
        list.Add(new(18, "VNM018", "Châu Minh Tâm", "Nhà máy Ca Đêm", 2, 2, 4, "Official", "Indefinite", new(2020, 11, 15), null, 17_000_000m, 1_000_000m, 500_000m, false, 100m, 17_000_000m, "ResidentStandard", false, true, 2, "079090000018", "0071000000018", "Techcombank", "Nhà máy Vùng II - Ca Đêm + Trực Đêm Lễ 02/09 (8h OT300 Đêm)"));
        list.Add(new(19, "VNM019", "Kiều Thị Trang", "Nhà máy Ca Đêm", 2, 2, 4, "Official", "Fixed12M", new(2023, 7, 1), null, 15_000_000m, 0m, 500_000m, false, 100m, 15_000_000m, "ResidentStandard", false, true, 0, "079090000019", "0071000000019", "BIDV", "Nhà máy Vùng II - Ca Đêm đi muộn 09/09 (20 phút)"));
        list.Add(new(20, "VNM020", "Dương Văn Uy", "Nhà máy Ca Đêm", 2, 2, 4, "Official", "Fixed12M", new(2024, 3, 1), null, 15_000_000m, 0m, 500_000m, false, 100m, 15_000_000m, "ResidentStandard", false, true, 0, "079090000020", "0071000000020", "Sacombank", "Nhà máy Vùng II - Ca 3 Đêm chuẩn"));

        // 21-24: Kinh doanh SALES_VN (Unit 3, Pos 3, Shift 1)
        list.Add(new(21, "VNM021", "Nguyễn Đức Vinh", "Kinh doanh", 3, 3, 1, "Official", "Indefinite", new(2022, 4, 1), null, 20_000_000m, 2_000_000m, 1_500_000m, false, 100m, 20_000_000m, "ResidentStandard", false, true, 1, "079090000021", "0071000000021", "Vietcombank", "Kinh doanh kênh GT + Hoa hồng 6,500,000đ"));
        list.Add(new(22, "VNM022", "Lê Thị Xuân", "Kinh doanh", 3, 3, 1, "Official", "Fixed12M", new(2023, 9, 1), null, 18_000_000m, 1_500_000m, 1_500_000m, false, 100m, 18_000_000m, "ResidentStandard", false, true, 0, "079090000022", "0071000000022", "Techcombank", "Kinh doanh kênh MT + Hoa hồng 4,200,000đ"));
        list.Add(new(23, "VNM023", "Phạm Gia Yên", "Kinh doanh", 3, 3, 1, "Official", "Fixed12M", new(2024, 1, 5), null, 17_000_000m, 1_000_000m, 1_500_000m, false, 100m, 17_000_000m, "ResidentStandard", false, true, 0, "079090000023", "0071000000023", "ACB", "Kinh doanh có ngày công tác miền Tây 18/09"));
        list.Add(new(24, "VNM024", "Trần Bảo Zuy", "Kinh doanh", 3, 3, 1, "Official", "Fixed12M", new(2024, 5, 10), null, 16_000_000m, 1_000_000m, 1_000_000m, false, 100m, 16_000_000m, "ResidentStandard", false, true, 0, "079090000024", "0071000000024", "MBBank", "Kinh doanh bị phạt kỷ luật 300,000đ"));

        // 25-26: Thử việc 85%
        list.Add(new(25, "VNM025", "Đinh Thị Ánh", "Thử việc (85%)", 1, 1, 1, "Probation", "Probation", new(2026, 8, 15), null, 16_000_000m, 0m, 0m, true, 85m, 0m, "ResidentShortTerm", false, false, 0, "079090000025", "0071000000025", "Vietcombank", "Thử việc 85%, không BHXH, thuế 10% (chưa có Cam kết 08)"));
        list.Add(new(26, "VNM026", "Vũ Hoàng Bảo", "Thử việc (85%)", 3, 3, 1, "Probation", "Probation", new(2026, 8, 20), null, 14_000_000m, 0m, 500_000m, true, 85m, 0m, "ResidentShortTerm", true, false, 0, "079090000026", "0071000000026", "VietinBank", "Thử việc 85%, không BHXH, CÓ Cam kết 08 -> thuế 0đ"));

        // 27-28: Chuyên gia Nước ngoài
        list.Add(new(27, "VNM027", "Kenji Takahashi", "Chuyên gia (Không cư trú)", 1, 4, 1, "Official", "Fixed12M", new(2026, 4, 1), null, 65_000_000m, 5_000_000m, 0m, false, 100m, 0m, "NonResident", false, false, 0, "PPJP990027", "0071000000027", "HSBC", "Expat Không cư trú (<183 ngày): thuế phẳng 20%, không BHXH/CĐ"));
        list.Add(new(28, "VNM028", "Elena Rostova", "Chuyên gia (Cư trú)", 2, 4, 1, "Official", "Indefinite", new(2024, 1, 1), null, 55_000_000m, 5_000_000m, 0m, false, 100m, 46_800_000m, "ResidentStandard", false, true, 1, "PPFR990028", "0071000000028", "StandardChartered", "Expat Cư trú: BHXH trần 46.8tr, BHTN không đóng hoặc Vùng 2, thuế lũy tiến"));

        // 29-30: Biến động vào / nghỉ giữa tháng
        list.Add(new(29, "VNM029", "Phan Minh Tiến", "Vào giữa tháng (15/09)", 1, 1, 1, "Official", "Fixed12M", new(2026, 9, 15), null, 20_000_000m, 1_000_000m, 0m, false, 100m, 20_000_000m, "ResidentStandard", false, true, 0, "079090000029", "0071000000029", "Vietcombank", "Nhận việc 15/09 + quên chấm ngày 24/09 -> báo TANG_MOI D02-LT"));
        list.Add(new(30, "VNM030", "Nguyễn Thị Tạm Biệt", "Nghỉ việc (20/09)", 2, 2, 2, "Official", "Indefinite", new(2022, 6, 1), new(2026, 9, 20), 18_000_000m, 500_000m, 0m, false, 100m, 18_000_000m, "ResidentStandard", false, true, 0, "079090000030", "0071000000030", "BIDV", "Làm đến hết 19/09 rồi nghỉ 20/09 -> tính lương công thực tế"));

        return list;
    }
}
