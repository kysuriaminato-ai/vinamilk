using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Common.Security;
using Vinamilk.HRM.Application.CoreHR.Employees;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Application.Talent;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Security;

public class RbacMatrixAndDataScopeTests
{
    private sealed class StubCurrentUser(
        int? userId,
        int? employeeId,
        string username,
        IReadOnlyList<string> roleCodes,
        IReadOnlyList<string> permissions,
        string? scopeLevelClaim = null) : ICurrentUserService
    {
        public int? UserId => userId;
        public int? EmployeeId => employeeId;
        public string Username => username;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "xUnit";
        public bool IsAuthenticated => userId.HasValue;
        public IReadOnlyList<string> RoleCodes => roleCodes;
        public string? ScopeLevelClaim => scopeLevelClaim;
        public bool HasPermission(string permissionCode) =>
            roleCodes.Any(r => string.Equals(r, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
            || permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class NoOpEncryptionService : IPiiEncryptionService
    {
        public string Encrypt(string? plainText) => plainText ?? string.Empty;
        public string Decrypt(string? cipherText) => cipherText ?? string.Empty;
        public string ComputeDeterministicHash(string? input) => input ?? string.Empty;
    }

    [Fact]
    public void RbacPolicyMatrix_Should_MatchDeclarativeRoleMatrixWithoutWildcards()
    {
        var catalog = RbacPolicyMatrix.BuildPermissionCatalog();
        catalog.Select(c => c.PermissionCode).Should().Contain("Payroll.Submit");
        catalog.Select(c => c.PermissionCode).Should().Contain("System.Backup");
        catalog.Select(c => c.PermissionCode).Should().OnlyHaveUniqueItems();

        var superAdmin = RbacPolicyMatrix.GetRoleDefinition("SUPER_ADMIN")!;
        superAdmin.DefaultDataScope.Should().Be(DataScopeLevels.All);
        superAdmin.PermissionCodes.Should().BeEquivalentTo(catalog.Select(c => c.PermissionCode));

        var hrManager = RbacPolicyMatrix.GetRoleDefinition("HR_MANAGER")!;
        hrManager.DefaultDataScope.Should().Be(DataScopeLevels.All);
        hrManager.PermissionCodes.Should().Contain(new[]
        {
            "Payroll.View", "Payroll.Calculate", "Payroll.Submit", "Payroll.Approve", "Payroll.Lock", "Payroll.Export"
        });
        hrManager.PermissionCodes.Should().NotContain("Payroll.Unlock");
        hrManager.PermissionCodes.Should().NotContain(p => p.StartsWith("System.", StringComparison.OrdinalIgnoreCase));

        var hrStaff = RbacPolicyMatrix.GetRoleDefinition("HR_STAFF")!;
        hrStaff.DefaultDataScope.Should().Be(DataScopeLevels.All);
        hrStaff.PermissionCodes.Should().Contain(new[]
        {
            "Dashboard.View",
            "Employees.View", "Employees.Create", "Employees.Edit", "Employees.Export",
            "Departments.View", "Departments.Create", "Departments.Edit",
            "Contracts.View", "Contracts.Create", "Contracts.Edit", "Contracts.Export",
            "Attendance.View", "Attendance.Create", "Attendance.Edit", "Attendance.Export",
            "Leave.View", "Leave.Create",
            "Recruitment.View", "Recruitment.Create", "Recruitment.Edit",
            "Onboarding.View", "Onboarding.Create", "Onboarding.Edit",
            "Training.View", "Training.Create", "Training.Edit",
            "Payroll.View", "Payroll.Calculate", "Payroll.Submit"
        });
        hrStaff.PermissionCodes.Should().NotContain(new[] { "Payroll.Approve", "Payroll.Lock", "Payroll.Unlock" });
        hrStaff.PermissionCodes.Should().NotContain(p => p.StartsWith("System.", StringComparison.OrdinalIgnoreCase));

        var accountant = RbacPolicyMatrix.GetRoleDefinition("ACCOUNTANT")!;
        accountant.DefaultDataScope.Should().Be(DataScopeLevels.All);
        accountant.PermissionCodes.Should().BeEquivalentTo(new[]
        {
            "Dashboard.View",
            "Employees.View",
            "Attendance.View",
            "Payroll.View",
            "Payroll.Export",
            "Reports.View",
            "Reports.Export"
        });

        var lineManager = RbacPolicyMatrix.GetRoleDefinition("LINE_MANAGER")!;
        lineManager.DefaultDataScope.Should().Be(DataScopeLevels.UnitTree);
        lineManager.PermissionCodes.Should().BeEquivalentTo(new[]
        {
            "Dashboard.View",
            "Employees.View",
            "Attendance.View", "Attendance.Approve",
            "Leave.View", "Leave.Approve",
            "Kpi.View", "Kpi.Evaluate",
            "Training.View"
        });
        lineManager.PermissionCodes.Should().NotContain(p => p.StartsWith("Payroll.", StringComparison.OrdinalIgnoreCase));
        lineManager.PermissionCodes.Should().NotContain(p => p.StartsWith("System.", StringComparison.OrdinalIgnoreCase));
        lineManager.PermissionCodes.Should().NotContain(p => p.StartsWith("Contracts.", StringComparison.OrdinalIgnoreCase));
        lineManager.PermissionCodes.Should().NotContain(p => p.StartsWith("Reports.", StringComparison.OrdinalIgnoreCase));

        var employee = RbacPolicyMatrix.GetRoleDefinition("EMPLOYEE")!;
        employee.DefaultDataScope.Should().Be(DataScopeLevels.SelfOnly);
        employee.PermissionCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureRbacMatrixSynchronizedAsync_Should_CleanLegacyPermissions_AndWriteAuditLog()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmRbacSync_{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);

        // Giả lập trạng thái DB lỗi thời trước sửa:
        // - Thiếu Payroll.Submit, System.Backup
        // - EMPLOYEE có Payroll.View, Employees.View
        // - LINE_MANAGER có Payroll.View, System.View
        // - HR_STAFF chưa có Payroll.*
        var perms = RbacPolicyMatrix.BuildPermissionCatalog()
            .Where(p => p.PermissionCode != "Payroll.Submit" && p.PermissionCode != "System.Backup")
            .Select(p => new Permission
            {
                PermissionCode = p.PermissionCode,
                ModuleName = p.ModuleName,
                ActionName = p.ActionName,
                DisplayName = p.DisplayName,
                SortOrder = p.SortOrder
            })
            .ToList();
        await db.Permissions.AddRangeAsync(perms);

        var rEmp = new Role { RoleCode = "EMPLOYEE", RoleName = "Employee", DefaultDataScope = "SelfOnly", IsSystemRole = true };
        var rMgr = new Role { RoleCode = "LINE_MANAGER", RoleName = "Line Manager", DefaultDataScope = "Department", IsSystemRole = true };
        var rStaff = new Role { RoleCode = "HR_STAFF", RoleName = "HR Staff", DefaultDataScope = "All", IsSystemRole = true };
        var rAcc = new Role { RoleCode = "ACCOUNTANT", RoleName = "Accountant", DefaultDataScope = "All", IsSystemRole = true };
        var rHrMgr = new Role { RoleCode = "HR_MANAGER", RoleName = "HR Manager", DefaultDataScope = "All", IsSystemRole = true };
        var rAdmin = new Role { RoleCode = "SUPER_ADMIN", RoleName = "SuperAdmin", DefaultDataScope = "All", IsSystemRole = true };
        await db.Roles.AddRangeAsync(rEmp, rMgr, rStaff, rAcc, rHrMgr, rAdmin);
        await db.SaveChangesAsync();

        var permMap = await db.Permissions.ToDictionaryAsync(p => p.PermissionCode);
        await db.RolePermissions.AddRangeAsync(
            new RolePermission { RoleId = rEmp.Id, PermissionId = permMap["Payroll.View"].Id },
            new RolePermission { RoleId = rEmp.Id, PermissionId = permMap["Employees.View"].Id },
            new RolePermission { RoleId = rMgr.Id, PermissionId = permMap["Payroll.View"].Id },
            new RolePermission { RoleId = rMgr.Id, PermissionId = permMap["System.View"].Id },
            new RolePermission { RoleId = rAcc.Id, PermissionId = permMap["Payroll.Calculate"].Id },
            new RolePermission { RoleId = rHrMgr.Id, PermissionId = permMap["Payroll.Unlock"].Id },
            new RolePermission { RoleId = rHrMgr.Id, PermissionId = permMap["System.View"].Id });
        await db.SaveChangesAsync();

        var initializer = new ApplicationDbContextInitializer(
            NullLogger<ApplicationDbContextInitializer>.Instance,
            db,
            new NoOpEncryptionService(),
            new PasswordSecurityService());

        await initializer.EnsureRbacMatrixSynchronizedAsync(forceSync: false, CancellationToken.None);

        var syncedPermsByRole = await db.RolePermissions
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .ToListAsync();

        foreach (var expectedRole in RbacPolicyMatrix.RoleDefinitions)
        {
            var actualCodes = syncedPermsByRole
                .Where(rp => rp.Role.RoleCode == expectedRole.RoleCode)
                .Select(rp => rp.Permission.PermissionCode)
                .ToList();
            actualCodes.Should().BeEquivalentTo(expectedRole.PermissionCodes,
                $"Role {expectedRole.RoleCode} must exactly match declarative RBAC matrix after sync");
        }

        (await db.Roles.FirstAsync(r => r.RoleCode == "LINE_MANAGER")).DefaultDataScope.Should().Be("UnitTree");
        (await db.Permissions.AnyAsync(p => p.PermissionCode == "Payroll.Submit")).Should().BeTrue();
        (await db.Permissions.AnyAsync(p => p.PermissionCode == "System.Backup")).Should().BeTrue();

        var syncAudit = await db.AuditLogs.FirstOrDefaultAsync(a => a.ActionType == "SyncRbacMatrix");
        syncAudit.Should().NotBeNull();
        syncAudit!.ModuleName.Should().Be("System");
    }

    [Fact]
    public async Task LineManagerDataScope_Should_IsolateQueriesToUnitTree_AndBlockCrossUnitDetails()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmScopeIsolation_{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);

        // Cây đơn vị: /1/ (Tổng công ty) -> /1/2/ (Khối Kinh doanh) -> /1/2/4/ (Team Miền Nam); /1/3/ (Khối Nhà máy)
        db.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, UnitCode = "HQ", UnitName = "Tổng Công ty", TreePath = "/1/", IsActive = true },
            new OrganizationUnit { Id = 2, UnitCode = "KD", UnitName = "Khối Kinh doanh", TreePath = "/1/2/", ParentUnitId = 1, ManagerEmployeeId = 10, IsActive = true },
            new OrganizationUnit { Id = 4, UnitCode = "KD_MN", UnitName = "Team Kinh doanh Miền Nam", TreePath = "/1/2/4/", ParentUnitId = 2, IsActive = true },
            new OrganizationUnit { Id = 3, UnitCode = "NM", UnitName = "Nhà máy Sữa", TreePath = "/1/3/", ParentUnitId = 1, IsActive = true });
        db.Positions.Add(new Position { Id = 1, PositionCode = "NV", PositionName = "Nhân viên", IsActive = true });

        db.Employees.AddRange(
            new Employee { Id = 10, EmployeeCode = "VNM-MGR", FullName = "Trưởng Khối Kinh doanh", OrganizationUnitId = 2, PositionId = 1, WorkStatus = "Official", HireDate = new DateOnly(2020, 1, 1), BaseSalary = 40_000_000m },
            new Employee { Id = 11, EmployeeCode = "VNM-KD2", FullName = "Nhân viên Kinh doanh Miền Nam", OrganizationUnitId = 4, PositionId = 1, WorkStatus = "Official", HireDate = new DateOnly(2022, 1, 1), BaseSalary = 18_000_000m },
            new Employee { Id = 20, EmployeeCode = "VNM-NM1", FullName = "Công nhân Nhà máy", OrganizationUnitId = 3, PositionId = 1, WorkStatus = "Official", HireDate = new DateOnly(2021, 1, 1), BaseSalary = 15_000_000m });

        var lmRole = new Role { Id = 5, RoleCode = "LINE_MANAGER", RoleName = "Line Manager", DefaultDataScope = DataScopeLevels.UnitTree, IsSystemRole = true };
        db.Roles.Add(lmRole);
        var lmUser = new User { Id = 100, Username = "kd.manager", Email = "kd@vnm.vn", EmployeeId = 10, IsActive = true };
        db.Users.Add(lmUser);
        db.UserRoles.Add(new UserRole { UserId = 100, RoleId = 5 });

        var lt = new LeaveType { Id = 1, LeaveTypeCode = "ANNUAL", LeaveTypeName = "Phép năm", IsPaidByCompany = true, CountsAgainstAnnualLeave = true };
        db.LeaveTypes.Add(lt);
        db.LeaveRequests.AddRange(
            new LeaveRequest { Id = 1, EmployeeId = 11, LeaveTypeId = 1, StartDate = new DateOnly(2026, 9, 10), EndDate = new DateOnly(2026, 9, 10), TotalDays = 1m, Status = "Pending", Reason = "Nghỉ KD" },
            new LeaveRequest { Id = 2, EmployeeId = 20, LeaveTypeId = 1, StartDate = new DateOnly(2026, 9, 10), EndDate = new DateOnly(2026, 9, 10), TotalDays = 1m, Status = "Pending", Reason = "Nghỉ NM" });

        var cycle = new EvaluationCycle { Id = 1, CycleCode = "KPI-2026-Q3", CycleName = "Quý 3/2026", CycleType = "Quarterly", Year = 2026, PeriodNumber = 3, StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 9, 30), Status = "InProgress" };
        db.EvaluationCycles.Add(cycle);
        db.EmployeeKpiEvaluations.AddRange(
            new EmployeeKpiEvaluation { Id = 1, EvaluationCycleId = 1, EmployeeId = 11, ReviewerEmployeeId = 10, TotalWeight = 100m, FinalScore = 90m, RatingGrade = "A", BonusMultiplier = 1.2m, Status = "Draft" },
            new EmployeeKpiEvaluation { Id = 2, EvaluationCycleId = 1, EmployeeId = 20, ReviewerEmployeeId = 20, TotalWeight = 100m, FinalScore = 88m, RatingGrade = "A", BonusMultiplier = 1.1m, Status = "Draft" });

        var pPeriod = new PayrollPeriod { Id = 1, PeriodCode = "PAY-2026-09", Month = 9, Year = 2026, PeriodTitle = "T09/2026", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 9, 30), StandardWorkdays = 26m, Status = "Approved" };
        db.PayrollPeriods.Add(pPeriod);
        db.PayrollRecords.AddRange(
            new PayrollRecord { Id = 1, PayrollPeriodId = 1, EmployeeId = 11, OrganizationUnitId = 4, PositionId = 1, StandardWorkdays = 26m, PayableWorkdays = 26m, ContractBaseSalary = 18_000_000m, GrossIncome = 18_000_000m, NetSalary = 16_500_000m },
            new PayrollRecord { Id = 2, PayrollPeriodId = 1, EmployeeId = 20, OrganizationUnitId = 3, PositionId = 1, StandardWorkdays = 26m, PayableWorkdays = 26m, ContractBaseSalary = 15_000_000m, GrossIncome = 15_000_000m, NetSalary = 13_800_000m });

        await db.SaveChangesAsync();

        var lmCurrentUser = new StubCurrentUser(
            userId: 100,
            employeeId: 10,
            username: "kd.manager",
            roleCodes: new[] { "LINE_MANAGER" },
            permissions: RbacPolicyMatrix.GetRoleDefinition("LINE_MANAGER")!.PermissionCodes);

        // 1. Employees list: chỉ thấy NV unit 2 & unit con 4, KHÔNG thấy NV unit 3 (Nhà máy)
        var empResult = await new GetEmployeesAdvancedQueryHandler(db, lmCurrentUser)
            .Handle(new GetEmployeesAdvancedQuery(PageSize: 50), CancellationToken.None);
        empResult.Items.Select(e => e.EmployeeCode).Should().BeEquivalentTo(new[] { "VNM-MGR", "VNM-KD2" });
        empResult.Units.Select(u => u.Id).Should().BeEquivalentTo(new[] { 2, 4 });

        // 2. Employee 360 profile: cho phép unit con (11), chặn unit khác (20)
        var allowed360 = await new GetEmployee360ProfileQueryHandler(db, new NoOpEncryptionService(), lmCurrentUser)
            .Handle(new GetEmployee360ProfileQuery(11), CancellationToken.None);
        allowed360.Should().NotBeNull();

        Func<Task> crossUnit360 = () => new GetEmployee360ProfileQueryHandler(db, new NoOpEncryptionService(), lmCurrentUser)
            .Handle(new GetEmployee360ProfileQuery(20), CancellationToken.None);
        await crossUnit360.Should().ThrowAsync<UnauthorizedAccessException>();

        // 3. Monthly Attendance Board: chỉ có công nhân viên trong cây KD
        var attBoard = await new GetMonthlyAttendanceBoardQueryHandler(db, lmCurrentUser)
            .Handle(new GetMonthlyAttendanceBoardQuery(9, 2026), CancellationToken.None);
        attBoard.MatrixRows.Select(r => r.EmployeeCode).Should().BeEquivalentTo(new[] { "VNM-MGR", "VNM-KD2" });

        // 4. Leave Overview: chỉ thấy đơn nghỉ của unit con 4
        var leaveResult = await new GetLeaveManagementOverviewQueryHandler(db, lmCurrentUser)
            .Handle(new GetLeaveManagementOverviewQuery(2026, 9), CancellationToken.None);
        leaveResult.Requests.Select(r => r.EmployeeCode).Should().BeEquivalentTo(new[] { "VNM-KD2" });

        // 5. KPI Overview: chỉ thấy đánh giá của unit con 4
        var kpiResult = await new GetKpiOverviewQueryHandler(db, lmCurrentUser)
            .Handle(new GetKpiOverviewQuery(1), CancellationToken.None);
        kpiResult.Evaluations.Select(e => e.EmployeeCode).Should().BeEquivalentTo(new[] { "VNM-KD2" });

        // 6. Reports BM01: chỉ thấy VNM-MGR và VNM-KD2
        var bm01Result = await new GetBm01EmployeeMasterReportQueryHandler(db, lmCurrentUser)
            .Handle(new GetBm01EmployeeMasterReportQuery(), CancellationToken.None);
        bm01Result.Select(x => x.EmployeeCode).Should().BeEquivalentTo(new[] { "VNM-MGR", "VNM-KD2" });

        // 7. Executive Dashboard: tổng nhân sự chỉ trong unit tree, và không có Payroll.View -> TotalMonthlyPayrollGross = 0
        var dashResult = await new GetRoleDashboardOverviewQueryHandler(db, new MemoryCache(new MemoryCacheOptions()), lmCurrentUser)
            .Handle(new GetRoleDashboardOverviewQuery("Executive", 2026, 9), CancellationToken.None);
        dashResult.TotalHeadcount.Should().Be(2);
        dashResult.TotalMonthlyPayrollGross.Should().Be(0m);
        dashResult.TotalMonthlyPayrollNet.Should().Be(0m);

        // 8. Payroll Trace trực tiếp nếu giả sử gọi qua scope UNIT_TREE -> chặn record của Nhà máy (#2)
        Func<Task> crossUnitTrace = () => new GetPayrollRecordTraceQueryHandler(db, lmCurrentUser)
            .Handle(new GetPayrollRecordTraceQuery(2), CancellationToken.None);
        await crossUnitTrace.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
