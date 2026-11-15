using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Application.Common.Security;
using Vinamilk.HRM.Application.CoreHR.Lifecycle;
using Vinamilk.HRM.Application.Talent;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Security;
using Vinamilk.HRM.Infrastructure.Services;
using Vinamilk.HRM.Web.Security;
using Xunit;

namespace Vinamilk.HRM.UnitTests.SecurityAndDomain;

public class SecurityAndDomainUnitTests
{
    private sealed class FakeHostEnv(string envName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = envName;
        public string ApplicationName { get; set; } = "Vinamilk.HRM.Web";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact]
    public void PiiEncryptionService_ShouldThrowInProductionWhenKeyMissing_AndRoundTripWhenConfigured()
    {
        var emptyCfg = new ConfigurationBuilder().Build();
        Action act = () => _ = new PiiEncryptionService(emptyCfg, new FakeHostEnv(Environments.Production));
        act.Should().Throw<InvalidOperationException>().WithMessage("*PiiEncryptionKey*");

        var validCfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Security:PiiEncryptionKey"] = "UnitTest_Secret_Key_32Chars_Only!" })
            .Build();
        var svc = new PiiEncryptionService(validCfg, new FakeHostEnv(Environments.Production));
        var cipher = svc.Encrypt("079090001234");
        cipher.Should().NotBe("079090001234");
        svc.Decrypt(cipher).Should().Be("079090001234");
        svc.ComputeDeterministicHash("079090001234").Should().Be(svc.ComputeDeterministicHash("079090001234"));
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_ShouldEnforceRbacPolicies()
    {
        var handler = new PermissionAuthorizationHandler();
        var req = new PermissionRequirement("System.Backup");

        var superAdmin = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "SUPER_ADMIN")
        }, "Cookie"));
        var adminCtx = new AuthorizationHandlerContext(new[] { req }, superAdmin, null);
        await handler.HandleAsync(adminCtx);
        adminCtx.HasSucceeded.Should().BeTrue();

        var hrUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "hr"),
            new Claim(ClaimTypes.Role, "HR_MANAGER"),
            new Claim("Permission", "Employees.View")
        }, "Cookie"));
        var hrCtx = new AuthorizationHandlerContext(new[] { req }, hrUser, null);
        await handler.HandleAsync(hrCtx);
        hrCtx.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public void DomainCalculators_ShouldComputeRetirement_Leave_Kpi_AndGeofence()
    {
        var retMale = VietnamRetirementCalculator.Calculate(new DateOnly(1975, 5, 15), "Nam", new DateOnly(2026, 11, 15));
        retMale.RetirementAgeYears.Should().BeGreaterOrEqualTo(61);

        var statutoryDefault = Vinamilk.HRM.Infrastructure.Persistence.Seed.ApplicationDbContextInitializer.BuildDefaultAttendanceStatutoryParameters().First();
        var leave = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(new DateOnly(2014, 1, 1), 2026, statutoryDefault);
        leave.SeniorityExtraDays.Should().Be(2);

        var (grade, mult, _) = KpiGradingCalculator.EvaluateGrade(95m);
        grade.Should().Be("A_Excellent");
        mult.Should().Be(1.5m);

        var gps = VinamilkGeofenceCalculator.EvaluateGps(10.7298, 106.7220);
        gps.IsWithinGeofence.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyticsHandlers_ShouldQueryReportsAndDashboardWithInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VnmAnalyticsTest_{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);

        db.OrganizationUnits.Add(new OrganizationUnit { Id = 1, UnitCode = "PB_IT", UnitName = "Phòng CNTT", IsActive = true });
        db.Positions.Add(new Position { Id = 1, PositionCode = "DEV", PositionName = "Kỹ sư", IsActive = true });
        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "VNM-0010",
            FullName = "Trần Văn B",
            Gender = "Nam",
            DateOfBirth = new DateOnly(1992, 4, 20),
            OrganizationUnitId = 1,
            PositionId = 1,
            HireDate = new DateOnly(2021, 6, 1),
            WorkStatus = "Official",
            BaseSalary = 30000000m
        });
        await db.SaveChangesAsync();

        var bm01 = await new GetBm01EmployeeMasterReportQueryHandler(db).Handle(new GetBm01EmployeeMasterReportQuery(), CancellationToken.None);
        bm01.Should().HaveCount(1);

        var dyn = await new RunDynamicReportQueryHandler(db).Handle(
            new RunDynamicReportQuery(["EmployeeCode", "FullName", "BaseSalary"], null, null, "UnitName"),
            CancellationToken.None);
        dyn.Rows.Should().HaveCount(1);
        dyn.Groups.Should().ContainSingle(g => g.GroupKey == "Phòng CNTT");

        using var memCache = new MemoryCache(new MemoryCacheOptions());
        var dash = await new GetRoleDashboardOverviewQueryHandler(db, memCache)
            .Handle(new GetRoleDashboardOverviewQuery("Executive", 2026, 11), CancellationToken.None);
        dash.TotalHeadcount.Should().Be(1);
        dash.IsFromCache.Should().BeFalse();

        var dashCached = await new GetRoleDashboardOverviewQueryHandler(db, memCache)
            .Handle(new GetRoleDashboardOverviewQuery("Executive", 2026, 11), CancellationToken.None);
        dashCached.IsFromCache.Should().BeTrue();
    }
}

