namespace VinamilkHRM.Application.Common.Interfaces;

/// <summary>
/// Cung cấp thông tin danh tính và vai trò của người dùng hiện tại đang thực thi yêu cầu.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    int? EmployeeId { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
}
