using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Vinamilk.HRM.Application.Common.Security;
using Vinamilk.HRM.Infrastructure.Security;
using Vinamilk.HRM.Web.Security;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Security;

public class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasSuperAdminRole()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Payroll.Approve");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "SUPER_ADMIN")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue("SuperAdmin có toàn quyền trên toàn bộ hệ thống");
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasMatchingPermissionClaim()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Employees.Edit");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Name, "huyen.ntt"),
            new Claim(ClaimTypes.Role, "HR_MANAGER"),
            new Claim("Permission", "Employees.View"),
            new Claim("Permission", "Employees.Edit")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserLacksRequiredPermissionClaim()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionRequirement("Payroll.Lock");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10"),
            new Claim(ClaimTypes.Name, "tung.pt"),
            new Claim(ClaimTypes.Role, "EMPLOYEE"),
            new Claim("Permission", "Dashboard.View"),
            new Claim("Permission", "Leave.Create")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse("Nhân viên ESS không có quyền khóa sổ bảng lương");
    }

    [Fact]
    public async Task PermissionPolicyProvider_ShouldBuildDynamicPolicy_ForModuleActionCode()
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());
        var provider = new PermissionPolicyProvider(options);

        // Act
        var policy = await provider.GetPolicyAsync("Contracts.Approve");

        // Assert
        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>()
            .Should().ContainSingle(r => r.PermissionCode == "Contracts.Approve");
    }

    [Theory]
    [InlineData(DataScopeLevels.All, 10, 5, "/1/5/", 99, 25, "/1/8/25/", true)]
    [InlineData(DataScopeLevels.UnitTree, 10, 5, "/1/5/", 42, 12, "/1/5/12/", true)]
    [InlineData(DataScopeLevels.UnitTree, 10, 5, "/1/5/", 42, 18, "/1/8/18/", false)]
    [InlineData(DataScopeLevels.Department, 10, 5, "/1/5/", 42, 5, "/1/5/", true)]
    [InlineData(DataScopeLevels.Department, 10, 5, "/1/5/", 42, 12, "/1/5/12/", false)]
    [InlineData(DataScopeLevels.SelfOnly, 10, 5, "/1/5/", 10, 5, "/1/5/", true)]
    [InlineData(DataScopeLevels.SelfOnly, 10, 5, "/1/5/", 11, 5, "/1/5/", false)]
    public void DataScopeEvaluator_ShouldEvaluateHierarchyAccessCorrectly(
        string scopeLevel,
        int userEmployeeId,
        int userUnitId,
        string userTreePath,
        int targetEmployeeId,
        int targetUnitId,
        string targetTreePath,
        bool expectedAllowed)
    {
        // Arrange
        var scopeCtx = new UserDataScopeContext(
            UserId: 100,
            EmployeeId: userEmployeeId,
            OrganizationUnitId: userUnitId,
            UnitTreePath: userTreePath,
            ScopeLevel: scopeLevel,
            ExtraAllowedUnitIds: new HashSet<int>(),
            IsSuperAdmin: false);

        // Act
        var allowed = DataScopeEvaluator.CanAccessEmployee(
            scopeCtx,
            targetEmployeeId,
            targetUnitId,
            targetTreePath);

        // Assert
        allowed.Should().Be(expectedAllowed);
    }

    [Fact]
    public void PasswordSecurityService_ShouldHashVerifyAndValidatePolicy()
    {
        // Arrange
        var service = new PasswordSecurityService();

        // Act & Assert Policy
        service.ValidatePasswordPolicy("short").IsValid.Should().BeFalse();
        service.ValidatePasswordPolicy("alllowercase123!").IsValid.Should().BeFalse();
        service.ValidatePasswordPolicy("Vinamilk@2026#Secure", "huyen.ntt").IsValid.Should().BeTrue();

        // Act & Assert PBKDF2 Hash
        var hash = service.HashPassword("Vinamilk@2026#Secure");
        hash.Should().StartWith("PBKDF2$");
        service.VerifyPassword("Vinamilk@2026#Secure", hash).Should().BeTrue();
        service.VerifyPassword("WrongPassword@123", hash).Should().BeFalse();
    }

    [Fact]
    public void TotpAuthenticatorService_ShouldGenerateAndVerifyRfc6238Code()
    {
        // Arrange
        var totp = new TotpAuthenticatorService();
        var secret = totp.GenerateBase32Secret();
        var fixedTime = new DateTime(2026, 11, 15, 8, 0, 0, DateTimeKind.Utc);

        // Act
        var code = totp.ComputeCurrentTotp(secret, fixedTime);
        var isValidSameWindow = totp.VerifyTotpCode(secret, code, fixedTime, allowDemoFallback: false);
        var isValidNextWindow = totp.VerifyTotpCode(secret, code, fixedTime.AddSeconds(25), allowDemoFallback: false);
        var isInvalidWrongCode = totp.VerifyTotpCode(secret, "000000", fixedTime, allowDemoFallback: false);

        // Assert
        code.Should().HaveLength(6);
        isValidSameWindow.Should().BeTrue();
        isValidNextWindow.Should().BeTrue();
        isInvalidWrongCode.Should().BeFalse();
    }
}

