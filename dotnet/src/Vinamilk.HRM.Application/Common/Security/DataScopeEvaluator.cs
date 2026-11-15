using System.Security.Claims;

namespace Vinamilk.HRM.Application.Common.Security;

/// <summary>
/// Cấp độ phân quyền dữ liệu (Data Scope) chuẩn HRM Doanh nghiệp:
/// - All: Toàn công ty (SuperAdmin, HR Manager, HR Staff, Accountant)
/// - UnitTree: Đơn vị/Khối/Nhà máy đang phụ trách và toàn bộ đơn vị cấp dưới theo TreePath (Line Manager / Giám đốc Khối)
/// - Department: Chỉ trong phòng ban trực tiếp của người dùng
/// - SelfOnly: Chỉ xem/thao tác hồ sơ của chính bản thân nhân viên (ESS)
/// </summary>
public static class DataScopeLevels
{
    public const string All = "All";
    public const string UnitTree = "UnitTree";
    public const string Department = "Department";
    public const string SelfOnly = "SelfOnly";
}

public sealed record UserDataScopeContext(
    int UserId,
    int? EmployeeId,
    int? OrganizationUnitId,
    string? UnitTreePath,
    string ScopeLevel,
    IReadOnlySet<int> ExtraAllowedUnitIds,
    bool IsSuperAdmin = false);

public static class DataScopeEvaluator
{
    /// <summary>
    /// Kiểm tra người dùng có quyền truy cập dữ liệu của một nhân sự cụ thể hay không
    /// dựa trên Data Scope (Toàn công ty / Đơn vị-Khối / Phòng ban / Bản thân).
    /// </summary>
    public static bool CanAccessEmployee(
        UserDataScopeContext userContext,
        int targetEmployeeId,
        int targetOrganizationUnitId,
        string? targetUnitTreePath)
    {
        if (userContext.IsSuperAdmin ||
            string.Equals(userContext.ScopeLevel, DataScopeLevels.All, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Luôn cho phép xem hồ sơ của chính mình
        if (userContext.EmployeeId.HasValue && userContext.EmployeeId.Value == targetEmployeeId)
        {
            return true;
        }

        // Kiểm tra các đơn vị được gán bổ sung trong bảng UserDataScopes
        if (userContext.ExtraAllowedUnitIds.Contains(targetOrganizationUnitId))
        {
            return true;
        }

        if (string.Equals(userContext.ScopeLevel, DataScopeLevels.Department, StringComparison.OrdinalIgnoreCase))
        {
            return userContext.OrganizationUnitId.HasValue &&
                   userContext.OrganizationUnitId.Value == targetOrganizationUnitId;
        }

        if (string.Equals(userContext.ScopeLevel, DataScopeLevels.UnitTree, StringComparison.OrdinalIgnoreCase))
        {
            if (userContext.OrganizationUnitId.HasValue &&
                userContext.OrganizationUnitId.Value == targetOrganizationUnitId)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(userContext.UnitTreePath) &&
                !string.IsNullOrWhiteSpace(targetUnitTreePath))
            {
                var normalizedParent = userContext.UnitTreePath.EndsWith('/')
                    ? userContext.UnitTreePath
                    : userContext.UnitTreePath + "/";
                var normalizedTarget = targetUnitTreePath.EndsWith('/')
                    ? targetUnitTreePath
                    : targetUnitTreePath + "/";

                return normalizedTarget.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra ClaimsPrincipal có quyền (Permission) cụ thể hay không.
    /// </summary>
    public static bool HasPermissionClaim(ClaimsPrincipal? principal, string permissionCode)
    {
        if (principal?.Identity is null || !principal.Identity.IsAuthenticated)
            return false;

        if (principal.IsInRole("SUPER_ADMIN"))
            return true;

        return principal.Claims.Any(c =>
            string.Equals(c.Type, "Permission", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, permissionCode, StringComparison.OrdinalIgnoreCase));
    }
}

