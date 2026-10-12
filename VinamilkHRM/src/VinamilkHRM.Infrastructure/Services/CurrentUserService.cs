using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VinamilkHRM.Application.Common.Interfaces;

namespace VinamilkHRM.Infrastructure.Services;

/// <summary>
/// Triển khai ICurrentUser trích xuất thông tin người dùng từ HttpContext hiện tại.
/// </summary>
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User?.FindFirst("sub")?.Value;

    public string? UserName => User?.Identity?.Name 
                            ?? User?.FindFirst("name")?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public int? EmployeeId
    {
        get
        {
            var claim = User?.FindFirst("EmployeeId")?.Value 
                     ?? User?.FindFirst("employee_id")?.Value;
            return int.TryParse(claim, out var empId) ? empId : null;
        }
    }

    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList() ?? new List<string>();

    public IReadOnlyList<string> Permissions => User?.FindAll("Permission")
        .Concat(User.FindAll("permission"))
        .Select(c => c.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList() ?? new List<string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public bool HasPermission(string permission)
    {
        if (IsInRole("SystemAdmin")) return true;
        return Permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
    }
}
