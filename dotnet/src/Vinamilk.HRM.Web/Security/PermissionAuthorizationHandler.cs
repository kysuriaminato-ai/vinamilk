using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Vinamilk.HRM.Application.Common.Security;

namespace Vinamilk.HRM.Web.Security;

/// <summary>
/// Yêu cầu quyền RBAC theo mã Module.Action (ví dụ: Employees.View, Payroll.Approve, System.Manage).
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }

    public PermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode ?? throw new ArgumentNullException(nameof(permissionCode));
    }
}

/// <summary>
/// Bộ xử lý kiểm tra quyền RBAC Policy-Based.
/// Cho phép SUPER_ADMIN vượt qua mọi kiểm tra hoặc kiểm tra Claim "Permission" trong phiên của User.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (DataScopeEvaluator.HasPermissionClaim(context.User, requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Tự động ánh xạ mọi tên Policy dạng "Module.Action" thành PermissionRequirement động mà không cần đăng ký thủ công từng quyền.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!string.IsNullOrWhiteSpace(policyName) && policyName.Contains('.'))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// Attribute đánh dấu Controller hoặc Action yêu cầu quyền RBAC cụ thể.
/// Ví dụ: [HasPermission("Employees.Edit")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permissionCode)
        : base(policy: permissionCode)
    {
    }
}

