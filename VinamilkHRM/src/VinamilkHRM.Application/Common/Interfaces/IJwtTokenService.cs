namespace VinamilkHRM.Application.Common.Interfaces;

public record AuthTokenResult(
    bool Succeeded,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt,
    string? Error = null);

public record UserProfileDto(
    int Id,
    string UserName,
    string FullName,
    string? Email,
    int? EmployeeId,
    bool MustChangePassword,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public interface IJwtTokenService
{
    Task<AuthTokenResult> GenerateTokensAsync(int userId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthTokenResult> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
}
