using Microsoft.AspNetCore.Authorization;

namespace VinamilkHRM.Infrastructure.Security;

/// <summary>
/// Yêu cầu phân quyền dựa trên quyền hạn chức năng "Module.Action".
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}
