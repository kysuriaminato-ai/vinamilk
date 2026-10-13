namespace VinamilkHRM.Infrastructure.Identity;

/// <summary>
/// Thực thể lưu trữ Refresh Token (xoay vòng và thu hồi) dùng cho xác thực JWT API.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Mã băm SHA-256 của Refresh Token bí mật (không lưu token thô trong DB).
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByIp { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
