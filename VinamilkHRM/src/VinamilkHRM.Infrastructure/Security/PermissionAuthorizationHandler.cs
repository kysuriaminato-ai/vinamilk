using Microsoft.AspNetCore.Authorization;
using VinamilkHRM.Application.Common.Security;

namespace VinamilkHRM.Infrastructure.Security;

/// <summary>
/// Bộ xử lý kiểm tra quyền động (Permission Authorization Handler).
/// Cho phép SystemAdmin bypass toàn bộ, người dùng khác kiểm tra danh sách claims quyền.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // 1. Quản trị hệ thống cấp cao nhất được cấp mọi quyền
        if (context.User.IsInRole(Roles.SystemAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Kiểm tra claim "permission" hoặc "Permission"
        var hasPermission = context.User.Claims.Any(c =>
            (c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase) ||
             c.Type.Equals("Permission", StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
