using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Infrastructure.Data;
using VinamilkHRM.Infrastructure.Identity;

namespace VinamilkHRM.Infrastructure.Services;

/// <summary>
/// Dịch vụ cấp phát, xoay vòng và thu hồi JWT Bearer token và Refresh Token an toàn.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly VinamilkHRMDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public JwtTokenService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        VinamilkHRMDbContext dbContext,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<AuthTokenResult> GenerateTokensAsync(int userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return new AuthTokenResult(false, null, null, null, "Không tìm thấy người dùng.");
        }

        return await GenerateUserTokensInternalAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthTokenResult> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new AuthTokenResult(false, null, null, null, "Refresh token không được để trống.");
        }

        string incomingHash = HashToken(refreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == incomingHash, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            return new AuthTokenResult(false, null, null, null, "Refresh token không hợp lệ hoặc đã hết hạn/bị thu hồi.");
        }

        // Tạo refresh token mới (Rotation)
        string newRefreshTokenRaw = GenerateSecureTokenString();
        string newRefreshTokenHash = HashToken(newRefreshTokenRaw);

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.RevokedByIp = ipAddress;
        existingToken.ReplacedByTokenHash = newRefreshTokenHash;

        int refreshTokenDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryDays", 7);

        var newRefreshToken = new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = newRefreshTokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshTokenDays)
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);

        var (accessToken, expiresAt) = await GenerateJwtAccessTokenAsync(existingToken.User);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(true, accessToken, newRefreshTokenRaw, expiresAt);
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        string hash = HashToken(refreshToken);
        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == hash, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            return false;
        }

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        existingToken.RevokedByIp = ipAddress;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<AuthTokenResult> GenerateUserTokensInternalAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) = await GenerateJwtAccessTokenAsync(user);

        string rawRefreshToken = GenerateSecureTokenString();
        string refreshTokenHash = HashToken(rawRefreshToken);
        int refreshTokenDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpiryDays", 7);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshTokenDays)
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        user.LastLoginAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(true, accessToken, rawRefreshToken, expiresAt);
    }

    private async Task<(string AccessToken, DateTimeOffset ExpiresAt)> GenerateJwtAccessTokenAsync(ApplicationUser user)
    {
        string secret = _configuration["JwtSettings:Secret"] ?? "Vinamilk_Enterprise_Secure_Secret_Key_For_Jwt_Authentication_2026";
        string issuer = _configuration["JwtSettings:Issuer"] ?? "VinamilkHRM";
        string audience = _configuration["JwtSettings:Audience"] ?? "VinamilkHRM.Clients";
        int expiryMinutes = _configuration.GetValue<int>("JwtSettings:ExpiryMinutes", 15);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new("FullName", user.FullName ?? string.Empty),
            new("MustChangePassword", user.MustChangePassword.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));
            claims.Add(new Claim("employee_id", user.EmployeeId.Value.ToString()));
        }

        // Roles
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // User Claims & Role Claims (Quyền Module.Action)
        var userClaims = await _userManager.GetClaimsAsync(user);
        var permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in userClaims.Where(c => c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase)))
        {
            permissionSet.Add(claim.Value);
        }

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in roleClaims.Where(c => c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase)))
                {
                    permissionSet.Add(claim.Value);
                }
            }
        }

        foreach (var permission in permissionSet)
        {
            claims.Add(new Claim("permission", permission));
            claims.Add(new Claim("Permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt.UtcDateTime,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        string tokenString = tokenHandler.WriteToken(token);

        return (tokenString, expiresAt);
    }

    private static string HashToken(string token)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateSecureTokenString()
    {
        byte[] randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
