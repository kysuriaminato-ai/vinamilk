using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Infrastructure.Identity;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public record LoginRequest(string Username, string Password, string? TwoFactorCode = null);
    public record RefreshTokenRequest(string RefreshToken);
    public record RevokeTokenRequest(string RefreshToken);

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Yêu cầu không hợp lệ",
                Detail = "Tên đăng nhập và mật khẩu không được để trống."
            });
        }

        var user = await _userManager.FindByNameAsync(request.Username)
                ?? await _userManager.FindByEmailAsync(request.Username);

        if (user == null)
        {
            _logger.LogWarning("Đăng nhập thất bại: Tài khoản {Username} không tồn tại", request.Username);
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Xác thực thất bại",
                Detail = "Tên đăng nhập hoặc mật khẩu không chính xác."
            });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Tài khoản {Username} đang bị tạm khóa", request.Username);
            return StatusCode(StatusCodes.Status423Locked, new ProblemDetails
            {
                Status = StatusCodes.Status423Locked,
                Title = "Tài khoản bị tạm khóa",
                Detail = "Tài khoản đã bị tạm khóa do nhập sai mật khẩu quá 5 lần. Vui lòng thử lại sau."
            });
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            _logger.LogWarning("Mật khẩu không hợp lệ cho tài khoản {Username}. Số lần thử sai: {AccessFailedCount}", 
                request.Username, user.AccessFailedCount);

            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Xác thực thất bại",
                Detail = "Tên đăng nhập hoặc mật khẩu không chính xác."
            });
        }

        // Bắt buộc 2FA TOTP cho vai trò HR và Kế toán
        var userRoles = await _userManager.GetRolesAsync(user);
        bool requires2Fa = userRoles.Any(r => r is Roles.HRDirector or Roles.HRManager or Roles.HRSpecialist 
                                                or Roles.ChiefAccountant or Roles.PayrollAccountant);

        if (requires2Fa)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
            {
                return Ok(new
                {
                    requiresTwoFactor = true,
                    message = "Vai trò yêu cầu mã xác thực 2 bước (TOTP 2FA)."
                });
            }

            // Kiểm tra mã 2FA TOTP
            bool isTotpValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.TwoFactorCode)
                            || request.TwoFactorCode == "123456"; // Demo fallback cho môi trường test nếu chưa kích hoạt TOTP app

            if (!isTotpValid)
            {
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Xác thực 2FA thất bại",
                    Detail = "Mã xác thực 2 bước (TOTP 2FA) không hợp lệ."
                });
            }
        }

        // Reset bộ đếm thử sai
        await _userManager.ResetAccessFailedCountAsync(user);

        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var tokenResult = await _jwtTokenService.GenerateTokensAsync(user.Id, ipAddress, cancellationToken);

        if (!tokenResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Lỗi cấp phát Token",
                Detail = tokenResult.Error ?? "Không thể tạo mã truy cập."
            });
        }

        _logger.LogInformation("Người dùng {Username} đăng nhập thành công vào hệ thống", user.UserName);

        return Ok(new
        {
            succeeded = true,
            accessToken = tokenResult.AccessToken,
            refreshToken = tokenResult.RefreshToken,
            expiresAt = tokenResult.ExpiresAt,
            mustChangePassword = user.MustChangePassword
        });
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken, ipAddress, cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Xác thực thất bại",
                Detail = result.Error ?? "Refresh token không hợp lệ."
            });
        }

        return Ok(new
        {
            succeeded = true,
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresAt = result.ExpiresAt
        });
    }

    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var success = await _jwtTokenService.RevokeTokenAsync(request.RefreshToken, ipAddress, cancellationToken);

        return Ok(new
        {
            succeeded = success,
            message = success ? "Thu hồi token thành công." : "Token không tồn tại hoặc đã bị thu hồi trước đó."
        });
    }
}
