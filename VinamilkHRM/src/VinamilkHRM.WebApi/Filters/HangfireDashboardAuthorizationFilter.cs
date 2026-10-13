using Hangfire.Dashboard;
using VinamilkHRM.Application.Common.Security;

namespace VinamilkHRM.WebApi.Filters;

/// <summary>
/// Bộ lọc bảo mật Hangfire Dashboard: Chỉ cho phép người dùng có vai trò SystemAdmin truy cập.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole(Roles.SystemAdmin);
    }
}
