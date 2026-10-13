using Microsoft.AspNetCore.Identity;

namespace VinamilkHRM.Infrastructure.Identity;

/// <summary>
/// Vai trò người dùng trong hệ thống Vinamilk HRM (ASP.NET Core Identity Role).
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
    public string? Description { get; set; }

    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
    }
}
