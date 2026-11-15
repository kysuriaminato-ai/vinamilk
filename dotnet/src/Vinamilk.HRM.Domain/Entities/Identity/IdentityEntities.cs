using Vinamilk.HRM.Domain.Common;
using Vinamilk.HRM.Domain.Entities.Core;

namespace Vinamilk.HRM.Domain.Entities.Identity;

public class User : BaseAuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    public int AccessFailedCount { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public bool MustChangePassword { get; set; } = false;

    public Employee? Employee { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserDataScope> DataScopes { get; set; } = new List<UserDataScope>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}

public class Role : BaseAuditableEntity
{
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultDataScope { get; set; } = "SelfOnly";
    public bool IsSystemRole { get; set; } = false;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission : BaseAuditableEntity
{
    public string PermissionCode { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class UserRole : BaseAuditableEntity
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public class RolePermission : BaseAuditableEntity
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

public class UserDataScope : BaseAuditableEntity
{
    public int UserId { get; set; }
    public int OrganizationUnitId { get; set; }
    public bool IncludeSubUnits { get; set; } = true;

    public User User { get; set; } = null!;
    public OrganizationUnit OrganizationUnit { get; set; } = null!;
}

public class UserSession : BaseLongAuditableEntity
{
    public int UserId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}

public class DataPrivacyConsent : BaseAuditableEntity
{
    public int EmployeeId { get; set; }
    public string PolicyVersion { get; set; } = "v1.0-ND13-2023";
    public bool IsConsented { get; set; } = true;
    public DateTime ConsentedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? ConsentNote { get; set; }

    public Employee Employee { get; set; } = null!;
}

