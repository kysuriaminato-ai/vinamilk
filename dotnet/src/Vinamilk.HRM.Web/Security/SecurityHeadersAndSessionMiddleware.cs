using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.Security;

/// <summary>
/// Middleware bảo mật tầng HTTP:
/// 1. Thiết lập các Security Headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy).
/// 2. Kiểm tra trạng thái thu hồi phiên đăng nhập (UserSession.RevokedAt) theo thời gian thực.
/// 3. Bắt buộc đổi mật khẩu lần đầu nếu tài khoản có cờ MustChangePassword = true.
/// </summary>
public sealed class SecurityHeadersAndSessionMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersAndSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext)
    {
        // 1. Thiết lập Security Headers chống Clickjacking, MIME Sniffing & XSS
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        headers["Content-Security-Policy"] =
            "default-src 'self' https://cdn.jsdelivr.net https://fonts.googleapis.com https://fonts.gstatic.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
            "font-src 'self' data: https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
            "img-src 'self' data: https:;";

        // 2. Kiểm tra phiên đăng nhập & bắt buộc đổi mật khẩu lần đầu
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var isStaticOrAuthRoute =
                path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/health/live", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Auth/Login", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Auth/Logout", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Auth/ChangePassword", StringComparison.OrdinalIgnoreCase);

            if (!isStaticOrAuthRoute)
            {
                var isApiRequest = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
                var sessionIdStr = context.User.FindFirstValue("SessionId");
                if (long.TryParse(sessionIdStr, out var sessionId) && sessionId > 0)
                {
                    var session = await dbContext.UserSessions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == sessionId, context.RequestAborted);

                    if (session is null || session.RevokedAt.HasValue || session.ExpiresAt <= DateTime.UtcNow)
                    {
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        if (isApiRequest)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                        context.Response.Redirect("/Auth/Login?revoked=true");
                        return;
                    }
                }

                var mustChangePass = string.Equals(
                    context.User.FindFirstValue("MustChangePassword"),
                    "true",
                    StringComparison.OrdinalIgnoreCase);

                var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out var userId) && userId > 0)
                {
                    var dbUser = await dbContext.Users
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.IsActive, u.IsSystemAccount, u.MustChangePassword })
                        .FirstOrDefaultAsync(context.RequestAborted);

                    if (dbUser is not null)
                    {
                        if (!dbUser.IsActive || dbUser.IsSystemAccount)
                        {
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            if (isApiRequest)
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                return;
                            }
                            context.Response.Redirect("/Auth/Login");
                            return;
                        }

                        mustChangePass = dbUser.MustChangePassword;
                    }
                }

                if (mustChangePass)
                {
                    if (isApiRequest)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync(
                            "{\"error\":\"Tài khoản bắt buộc đổi mật khẩu trước khi truy cập hệ thống.\",\"redirectUrl\":\"/Auth/ChangePassword?firstLogin=true\"}",
                            context.RequestAborted);
                        return;
                    }

                    context.Response.Redirect("/Auth/ChangePassword?firstLogin=true");
                    return;
                }
            }
        }

        await _next(context);
    }
}

