using System.Security.Claims;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var idStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) ? id : null;
        }
    }

    public string Username =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? string.Empty;

    public int? EmployeeId
    {
        get
        {
            var empIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue("EmployeeId");
            return int.TryParse(empIdStr, out var empId) ? empId : null;
        }
    }

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permissionCode)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
            return false;
        if (user.IsInRole("SUPER_ADMIN"))
            return true;
        return user.HasClaim("Permission", permissionCode);
    }

    public IReadOnlyList<string> RoleCodes
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null)
                return Array.Empty<string>();
            return user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public string? ScopeLevelClaim =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("DataScope");
}

