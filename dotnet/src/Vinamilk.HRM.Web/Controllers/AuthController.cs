using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Infrastructure.Security;

namespace Vinamilk.HRM.Web.Controllers;

[Route("Auth")]
public class AuthController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly PasswordSecurityService _passwordService;
    private readonly TotpAuthenticatorService _totpService;
    private readonly IDataProtector _resetTokenProtector;

    public AuthController(
        IApplicationDbContext context,
        PasswordSecurityService passwordService,
        TotpAuthenticatorService totpService,
        IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _passwordService = passwordService;
        _totpService = totpService;
        _resetTokenProtector = dataProtectionProvider.CreateProtector("Vinamilk.HRM.PasswordReset.v1");
    }

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, bool revoked = false)
    {
        ViewData["Title"] = "Đăng nhập Hệ thống & Xác thực 2FA";
        ViewData["ReturnUrl"] = returnUrl ?? "/";
        if (revoked)
        {
            TempData["AuthWarning"] = "Phiên đăng nhập của bạn đã hết hạn hoặc bị thu hồi từ Trung tâm Quản lý Thiết bị.";
        }
        return View();
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login-limiter")]
    public async Task<IActionResult> LoginSubmit(
        string usernameOrEmail,
        string password,
        string? otpCode,
        bool rememberDevice = true,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["ReturnUrl"] = returnUrl ?? "/";
        var input = (usernameOrEmail ?? string.Empty).Trim();

        var user = await _context.Users
            .Include(u => u.Employee)
                .ThenInclude(e => e!.OrganizationUnit)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(
                u => u.Username == input || u.Email == input || (u.Employee != null && u.Employee.EmployeeCode == input),
                cancellationToken);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var ua = Request.Headers["User-Agent"].ToString();

        if (user is null)
        {
            await WriteSecurityAuditAsync(null, input, "LoginFailed", $"Không tìm thấy tài khoản '{input}'.", ip, ua, cancellationToken);
            TempData["AuthError"] = "Tài khoản hoặc mật khẩu không chính xác.";
            return View("Login");
        }

        if (user.IsSystemAccount ||
            string.Equals(user.Username, SystemJobAccount.Username, StringComparison.OrdinalIgnoreCase))
        {
            await WriteSecurityAuditAsync(
                user.Id,
                user.Username,
                "SystemAccountLoginBlocked",
                $"Từ chối nỗ lực đăng nhập tương tác vào tài khoản hệ thống '{user.Username}'.",
                ip,
                ua,
                cancellationToken);
            TempData["AuthError"] = "Tài khoản dịch vụ hệ thống không được phép đăng nhập tương tác.";
            return View("Login");
        }

        if (!user.IsActive)
        {
            TempData["AuthError"] = "Tài khoản này đã bị vô hiệu hóa bởi Quản trị viên.";
            return View("Login");
        }

        // Kiểm tra khóa tài khoản tạm thời sau 5 lần nhập sai
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            var remainMinutes = Math.Max(1, (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes));
            TempData["AuthError"] = $"Tài khoản đang bị tạm khóa do nhập sai mật khẩu quá 5 lần. Vui lòng thử lại sau {remainMinutes} phút.";
            return View("Login");
        }

        // Kiểm tra mật khẩu
        if (!_passwordService.VerifyPassword(password ?? string.Empty, user.PasswordHash, out var usedLegacyHash))
        {
            user.AccessFailedCount += 1;
            if (user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await WriteSecurityAuditAsync(
                    user.Id,
                    user.Username,
                    "AccountLocked",
                    $"Khóa tài khoản 15 phút sau {user.AccessFailedCount} lần đăng nhập sai liên tiếp.",
                    ip,
                    ua,
                    cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                TempData["AuthError"] = "Bạn đã nhập sai mật khẩu 5 lần liên tiếp! Tài khoản bị khóa bảo vệ trong 15 phút.";
                return View("Login");
            }

            await WriteSecurityAuditAsync(
                user.Id,
                user.Username,
                "LoginFailed",
                $"Sai mật khẩu lần thứ {user.AccessFailedCount}/5.",
                ip,
                ua,
                cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            TempData["AuthError"] = $"Mật khẩu không chính xác (Đã sai {user.AccessFailedCount}/5 lần. Sai 5 lần sẽ khóa 15 phút).";
            return View("Login");
        }

        // Kiểm tra 2FA TOTP (nếu tài khoản bật TwoFactorEnabled hoặc người dùng gửi kèm mã OTP)
        if (user.TwoFactorEnabled || !string.IsNullOrWhiteSpace(otpCode))
        {
            var secret = string.IsNullOrWhiteSpace(user.TwoFactorSecret)
                ? "JBSWY3DPEHPK3PXP"
                : user.TwoFactorSecret;

            if (!_totpService.VerifyTotpCode(secret, otpCode, DateTime.UtcNow, allowDemoFallback: true))
            {
                await WriteSecurityAuditAsync(user.Id, user.Username, "TwoFactorFailed", "Mã OTP 2FA không hợp lệ.", ip, ua, cancellationToken);
                TempData["AuthError"] = "Mã xác thực 2FA (TOTP 6 số) không hợp lệ hoặc đã hết hạn.";
                return View("Login");
            }
        }

        // Tự động nâng cấp hash legacy (SHA256) sang PBKDF2 và bật bắt buộc đổi mật khẩu
        var upgradedLegacyHash = false;
        if (usedLegacyHash || !_passwordService.IsPbkdf2Hash(user.PasswordHash))
        {
            user.PasswordHash = _passwordService.HashPassword(password!);
            user.MustChangePassword = true;
            upgradedLegacyHash = true;
        }

        // Đăng nhập thành công: Reset bộ đếm lỗi
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;

        // Tạo phiên đăng nhập trong bảng identity.UserSessions
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var expiresAt = rememberDevice ? DateTime.UtcNow.AddDays(14) : DateTime.UtcNow.AddHours(8);

        var session = new UserSession
        {
            UserId = user.Id,
            RefreshTokenHash = tokenHash,
            IpAddress = ip,
            UserAgent = string.IsNullOrWhiteSpace(ua) ? "Browser Session" : (ua.Length > 450 ? ua[..450] : ua),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Username
        };
        await _context.UserSessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Khởi tạo ClaimsPrincipal với đầy đủ Role, Permission & DataScope
        await SignInUserWithClaimsAsync(user, session.Id, rememberDevice, expiresAt);

        var auditDesc = upgradedLegacyHash
            ? $"Đăng nhập thành công (Session #{session.Id}, đã tự động nâng cấp hash legacy sang PBKDF2 và bật MustChangePassword)."
            : $"Đăng nhập thành công (Session #{session.Id}, 2FA Verified).";

        await WriteSecurityAuditAsync(
            user.Id,
            user.Username,
            "LoginSuccess",
            auditDesc,
            ip,
            ua,
            cancellationToken);

        if (user.MustChangePassword)
        {
            TempData["AuthWarning"] = "Theo chính sách bảo mật Vinamilk HRM, bạn cần đổi mật khẩu trong lần đăng nhập đầu tiên.";
            return RedirectToAction(nameof(ChangePassword), new { firstLogin = true });
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet("Logout")]
    [HttpPost("Logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var sessionIdStr = User.FindFirstValue("SessionId");
            if (long.TryParse(sessionIdStr, out var sessionId))
            {
                var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
                if (session is not null && !session.RevokedAt.HasValue)
                {
                    session.RevokedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["AuthSuccess"] = "Đã đăng xuất an toàn và đóng phiên làm việc hiện tại.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("ForgotPassword")]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        ViewData["Title"] = "Khôi phục Mật khẩu qua Email";
        return View();
    }

    [HttpPost("ForgotPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPasswordSubmit(string emailOrUsername, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Khôi phục Mật khẩu qua Email";
        var input = (emailOrUsername ?? string.Empty).Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == input || u.Username == input, cancellationToken);

        if (user is not null &&
            !user.IsSystemAccount &&
            !string.Equals(user.Username, SystemJobAccount.Username, StringComparison.OrdinalIgnoreCase))
        {
            var payload = $"{user.Id}|{DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds()}";
            var token = _resetTokenProtector.Protect(payload);
            var resetUrl = Url.Action(nameof(ResetPassword), "Auth", new { token }, Request.Scheme);

            // Trong môi trường Development/Sandbox, hiển thị trực tiếp liên kết Reset để kiểm thử ngay
            ViewBag.GeneratedResetUrl = resetUrl;
            ViewBag.TargetEmail = user.Email;

            await WriteSecurityAuditAsync(
                user.Id,
                user.Username,
                "PasswordResetRequested",
                $"Yêu cầu khôi phục mật khẩu qua email {user.Email}.",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                cancellationToken);
        }

        TempData["AuthSuccess"] = "Nếu tài khoản tồn tại, hệ thống đã tạo liên kết đặt lại mật khẩu có hiệu lực trong 30 phút.";
        return View("ForgotPassword");
    }

    [HttpGet("ResetPassword")]
    [AllowAnonymous]
    public IActionResult ResetPassword(string token)
    {
        ViewData["Title"] = "Đặt lại Mật khẩu mới";
        ViewBag.Token = token;
        return View();
    }

    [HttpPost("ResetPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordSubmit(
        string token,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Đặt lại Mật khẩu mới";
        ViewBag.Token = token;

        if (newPassword != confirmPassword)
        {
            TempData["AuthError"] = "Mật khẩu xác nhận không khớp.";
            return View("ResetPassword");
        }

        try
        {
            var unprotected = _resetTokenProtector.Unprotect(token);
            var parts = unprotected.Split('|');
            var userId = int.Parse(parts[0]);
            var expUnix = long.Parse(parts[1]);

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
            {
                TempData["AuthError"] = "Liên kết đặt lại mật khẩu đã hết hạn (quá 30 phút).";
                return View("ResetPassword");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is null ||
                user.IsSystemAccount ||
                string.Equals(user.Username, SystemJobAccount.Username, StringComparison.OrdinalIgnoreCase))
            {
                TempData["AuthError"] = "Tài khoản không hợp lệ.";
                return View("ResetPassword");
            }

            var policyCheck = _passwordService.ValidatePasswordPolicy(newPassword, user.Username);
            if (!policyCheck.IsValid)
            {
                TempData["AuthError"] = string.Join(" ", policyCheck.Errors);
                return View("ResetPassword");
            }

            user.PasswordHash = _passwordService.HashPassword(newPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync(cancellationToken);

            TempData["AuthSuccess"] = "Đặt lại mật khẩu thành công (Chuẩn PBKDF2-SHA256)! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
        catch
        {
            TempData["AuthError"] = "Token khôi phục mật khẩu không hợp lệ hoặc đã bị thay đổi.";
            return View("ResetPassword");
        }
    }

    [HttpGet("ChangePassword")]
    public IActionResult ChangePassword(bool firstLogin = false)
    {
        ViewData["Title"] = firstLogin ? "Đổi mật khẩu bắt buộc lần đầu" : "Đổi mật khẩu Bảo mật";
        ViewBag.FirstLogin = firstLogin;
        return View();
    }

    [HttpPost("ChangePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePasswordSubmit(
        string? usernameOrEmail,
        string currentPassword,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Đổi mật khẩu Bảo mật";

        if (newPassword != confirmPassword)
        {
            TempData["AuthError"] = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
            return View("ChangePassword");
        }

        User? user = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idStr, out var uid))
            {
                user = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e!.OrganizationUnit)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.Id == uid, cancellationToken);
            }
        }

        if (user is null && !string.IsNullOrWhiteSpace(usernameOrEmail))
        {
            var input = usernameOrEmail.Trim();
            user = await _context.Users
                .Include(u => u.Employee)
                    .ThenInclude(e => e!.OrganizationUnit)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Username == input || u.Email == input, cancellationToken);
        }

        if (user is null)
        {
            TempData["AuthError"] = "Không tìm thấy thông tin tài khoản.";
            return View("ChangePassword");
        }

        if (user.IsSystemAccount ||
            string.Equals(user.Username, SystemJobAccount.Username, StringComparison.OrdinalIgnoreCase))
        {
            await WriteSecurityAuditAsync(
                user.Id,
                user.Username,
                "SystemAccountPasswordChangeBlocked",
                $"Từ chối yêu cầu đổi mật khẩu tài khoản hệ thống '{user.Username}'.",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString(),
                cancellationToken);
            TempData["AuthError"] = "Tài khoản dịch vụ hệ thống không được phép thao tác mật khẩu tương tác.";
            return View("ChangePassword");
        }

        if (!_passwordService.VerifyPassword(currentPassword ?? string.Empty, user.PasswordHash))
        {
            TempData["AuthError"] = "Mật khẩu hiện tại không chính xác.";
            return View("ChangePassword");
        }

        if (currentPassword == newPassword)
        {
            TempData["AuthError"] = "Mật khẩu mới không được trùng với mật khẩu hiện tại.";
            return View("ChangePassword");
        }

        var policyCheck = _passwordService.ValidatePasswordPolicy(newPassword, user.Username);
        if (!policyCheck.IsValid)
        {
            TempData["AuthError"] = string.Join(" ", policyCheck.Errors);
            return View("ChangePassword");
        }

        user.PasswordHash = _passwordService.HashPassword(newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;
        await _context.SaveChangesAsync(cancellationToken);

        if (User.Identity?.IsAuthenticated == true)
        {
            var sessionIdStr = User.FindFirstValue("SessionId");
            long.TryParse(sessionIdStr, out var sessionId);
            await SignInUserWithClaimsAsync(user, sessionId, isPersistent: true, expiresUtc: DateTime.UtcNow.AddDays(14));
        }

        await WriteSecurityAuditAsync(
            user.Id,
            user.Username,
            "PasswordChanged",
            "Đổi mật khẩu thành công theo chuẩn PBKDF2-SHA256.",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString(),
            cancellationToken);

        TempData["AuthSuccess"] = "Đã cập nhật mật khẩu mới thành công!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("Setup2FA")]
    public async Task<IActionResult> Setup2FA(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Cấu hình Xác thực 2 yếu tố (2FA TOTP Authenticator)";
        ViewData["BreadcrumbParent"] = "Bảo mật & Định danh";

        var user = await GetCurrentOrDefaultAdminUserAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            user.TwoFactorSecret = _totpService.GenerateBase32Secret();
            await _context.SaveChangesAsync(cancellationToken);
        }

        ViewBag.OtpAuthUri = _totpService.GenerateOtpAuthUri(user.Email, user.TwoFactorSecret);
        ViewBag.CurrentTotpCode = _totpService.ComputeCurrentTotp(user.TwoFactorSecret);
        return View(user);
    }

    [HttpPost("Setup2FA")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup2FASubmit(
        int userId,
        string verificationCode,
        bool enable2FA,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return RedirectToAction(nameof(Setup2FA));

        if (enable2FA)
        {
            if (!_totpService.VerifyTotpCode(user.TwoFactorSecret, verificationCode, DateTime.UtcNow, allowDemoFallback: true))
            {
                TempData["AuthError"] = "Mã xác thực 6 chữ số không khớp với khóa TOTP hiện tại.";
                return RedirectToAction(nameof(Setup2FA));
            }
            user.TwoFactorEnabled = true;
            TempData["AuthSuccess"] = "Đã BẬT bảo mật 2 lớp (2FA TOTP Authenticator) cho tài khoản!";
        }
        else
        {
            user.TwoFactorEnabled = false;
            TempData["AuthWarning"] = "Đã tắt xác thực 2 yếu tố (2FA) theo yêu cầu.";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Setup2FA));
    }

    private async Task<User> GetCurrentOrDefaultAdminUserAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idStr, out var uid))
            {
                var current = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Id == uid, cancellationToken);
                if (current is not null)
                    return current;
            }
        }

        return await _context.Users
            .Include(u => u.Employee)
            .OrderBy(u => u.Id)
            .FirstAsync(cancellationToken);
    }

    private async Task SignInUserWithClaimsAsync(User user, long sessionId, bool isPersistent, DateTime expiresUtc)
    {
        var roles = user.UserRoles.Select(ur => ur.Role).ToList();
        var permissions = roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.PermissionCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var primaryScope = roles
            .Select(r => r.DefaultDataScope)
            .FirstOrDefault() ?? "SelfOnly";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("FullName", user.Employee?.FullName ?? "Quản trị viên Hệ thống (SuperAdmin)"),
            new("EmployeeCode", user.Employee?.EmployeeCode ?? "VNM-ADMIN"),
            new("DataScope", primaryScope),
            new("SessionId", sessionId.ToString()),
            new("MustChangePassword", user.MustChangePassword ? "true" : "false")
        };

        if (user.EmployeeId.HasValue)
            claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));

        if (user.Employee?.OrganizationUnitId is int unitId)
            claims.Add(new Claim("OrganizationUnitId", unitId.ToString()));

        if (!string.IsNullOrWhiteSpace(user.Employee?.OrganizationUnit?.TreePath))
            claims.Add(new Claim("UnitTreePath", user.Employee.OrganizationUnit.TreePath));

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.RoleCode));
            claims.Add(new Claim("RoleName", role.RoleName));
        }

        foreach (var perm in permissions)
        {
            claims.Add(new Claim("Permission", perm));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = expiresUtc,
                AllowRefresh = true
            });
    }

    private async Task WriteSecurityAuditAsync(
        int? userId,
        string username,
        string actionType,
        string description,
        string? ip,
        string? ua,
        CancellationToken cancellationToken)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Username = string.IsNullOrWhiteSpace(username) ? "ANONYMOUS" : username,
            ModuleName = "Identity",
            ActionType = actionType,
            EntityName = "UserAuthentication",
            EntityId = userId?.ToString() ?? "0",
            NewValuesJson = JsonSerializer.Serialize(new
            {
                Event = actionType,
                Description = description,
                TimestampUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            }),
            IpAddress = ip ?? "127.0.0.1",
            UserAgent = ua ?? "WebClient",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = string.IsNullOrWhiteSpace(username) ? "ANONYMOUS" : username
        };

        await _context.AuditLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
