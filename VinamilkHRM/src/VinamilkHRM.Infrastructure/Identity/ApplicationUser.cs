using Microsoft.AspNetCore.Identity;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Infrastructure.Identity;

/// <summary>
/// Tài khoản đăng nhập định danh của hệ thống Vinamilk HRM (ASP.NET Core Identity).
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    public int? EmployeeId { get; set; }
    public virtual Employee? Employee { get; set; }

    /// <summary>
    /// Cờ bắt buộc đổi mật khẩu trong lần đăng nhập đầu tiên hoặc sau khi reset mật khẩu.
    /// </summary>
    public bool MustChangePassword { get; set; } = true;

    /// <summary>
    /// Thời điểm đăng nhập thành công gần nhất.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Thời điểm tạo tài khoản người dùng.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Họ và tên hiển thị của tài khoản.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách Refresh Tokens đã cấp phát cho tài khoản.
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
