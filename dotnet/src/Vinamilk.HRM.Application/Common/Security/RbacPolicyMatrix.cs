namespace Vinamilk.HRM.Application.Common.Security;

public sealed record PermissionCatalogDefinition(
    string PermissionCode,
    string ModuleName,
    string ActionName,
    string DisplayName,
    int SortOrder);

public sealed record RoleMatrixDefinition(
    string RoleCode,
    string RoleName,
    string Description,
    string DefaultDataScope,
    IReadOnlyList<string> PermissionCodes);

/// <summary>
/// Ma trận phân quyền RBAC khai báo tường minh cho từng Role & Module.
/// Không dùng quy tắc lọc mờ ("mọi View") để tránh lọt quyền nhạy cảm như Payroll.View / System.*.
/// </summary>
public static class RbacPolicyMatrix
{
    public static IReadOnlyList<PermissionCatalogDefinition> BuildPermissionCatalog()
    {
        var modules = new (string Code, string Name, string[] Actions)[]
        {
            ("Dashboard", "Bảng điều khiển", new[] { "View" }),
            ("Employees", "Quản lý Hồ sơ Nhân sự", new[] { "View", "Create", "Edit", "Delete", "Export" }),
            ("Departments", "Cơ cấu Tổ chức & Đơn vị", new[] { "View", "Create", "Edit", "Delete" }),
            ("Contracts", "Hợp đồng Lao động", new[] { "View", "Create", "Edit", "Approve", "Export" }),
            ("Attendance", "Chấm công & Ca làm việc", new[] { "View", "Create", "Edit", "Approve", "Lock", "Export" }),
            ("Leave", "Nghỉ phép & Quỹ phép", new[] { "View", "Create", "Approve" }),
            ("Payroll", "Tiền lương 3P, BHXH & Thuế", new[] { "View", "Calculate", "Submit", "Approve", "Lock", "Unlock", "Export" }),
            ("Recruitment", "Tuyển dụng (ATS)", new[] { "View", "Create", "Edit", "Approve" }),
            ("Onboarding", "Onboarding & Tài sản", new[] { "View", "Create", "Edit" }),
            ("Training", "Đào tạo & Phát triển", new[] { "View", "Create", "Edit" }),
            ("Kpi", "Đánh giá KPI / OKR", new[] { "View", "Create", "Evaluate", "Approve" }),
            ("Transfers", "Điều chuyển & Bổ nhiệm", new[] { "View", "Create", "Approve" }),
            ("Rewards", "Khen thưởng & Kỷ luật", new[] { "View", "Create", "Approve" }),
            ("Reports", "Báo cáo Thống kê BM01-BM04", new[] { "View", "Export" }),
            ("Analytics", "Phân tích Nhân sự & AI Expert", new[] { "View" }),
            ("System", "Quản trị Hệ thống & Phân quyền", new[] { "View", "Manage", "Backup" })
        };

        var result = new List<PermissionCatalogDefinition>();
        int sortOrder = 1;
        foreach (var mod in modules)
        {
            foreach (var act in mod.Actions)
            {
                result.Add(new PermissionCatalogDefinition(
                    $"{mod.Code}.{act}",
                    mod.Code,
                    act,
                    $"{mod.Name} - {act}",
                    sortOrder++));
            }
        }

        return result;
    }

    public static readonly IReadOnlyList<RoleMatrixDefinition> RoleDefinitions =
    [
        new(
            RoleCode: "SUPER_ADMIN",
            RoleName: "SuperAdmin",
            Description: "Quản trị viên hệ thống tối cao",
            DefaultDataScope: DataScopeLevels.All,
            PermissionCodes:
            [
                "Dashboard.View",
                "Employees.View", "Employees.Create", "Employees.Edit", "Employees.Delete", "Employees.Export",
                "Departments.View", "Departments.Create", "Departments.Edit", "Departments.Delete",
                "Contracts.View", "Contracts.Create", "Contracts.Edit", "Contracts.Approve", "Contracts.Export",
                "Attendance.View", "Attendance.Create", "Attendance.Edit", "Attendance.Approve", "Attendance.Lock", "Attendance.Export",
                "Leave.View", "Leave.Create", "Leave.Approve",
                "Payroll.View", "Payroll.Calculate", "Payroll.Submit", "Payroll.Approve", "Payroll.Lock", "Payroll.Unlock", "Payroll.Export",
                "Recruitment.View", "Recruitment.Create", "Recruitment.Edit", "Recruitment.Approve",
                "Onboarding.View", "Onboarding.Create", "Onboarding.Edit",
                "Training.View", "Training.Create", "Training.Edit",
                "Kpi.View", "Kpi.Create", "Kpi.Evaluate", "Kpi.Approve",
                "Transfers.View", "Transfers.Create", "Transfers.Approve",
                "Rewards.View", "Rewards.Create", "Rewards.Approve",
                "Reports.View", "Reports.Export",
                "Analytics.View",
                "System.View", "System.Manage", "System.Backup"
            ]),
        new(
            RoleCode: "HR_MANAGER",
            RoleName: "HR Manager",
            Description: "Giám đốc / Trưởng phòng Nhân sự",
            DefaultDataScope: DataScopeLevels.All,
            PermissionCodes:
            [
                "Dashboard.View",
                "Employees.View", "Employees.Create", "Employees.Edit", "Employees.Delete", "Employees.Export",
                "Departments.View", "Departments.Create", "Departments.Edit", "Departments.Delete",
                "Contracts.View", "Contracts.Create", "Contracts.Edit", "Contracts.Approve", "Contracts.Export",
                "Attendance.View", "Attendance.Create", "Attendance.Edit", "Attendance.Approve", "Attendance.Lock", "Attendance.Export",
                "Leave.View", "Leave.Create", "Leave.Approve",
                "Payroll.View", "Payroll.Calculate", "Payroll.Submit", "Payroll.Approve", "Payroll.Lock", "Payroll.Export",
                "Recruitment.View", "Recruitment.Create", "Recruitment.Edit", "Recruitment.Approve",
                "Onboarding.View", "Onboarding.Create", "Onboarding.Edit",
                "Training.View", "Training.Create", "Training.Edit",
                "Kpi.View", "Kpi.Create", "Kpi.Evaluate", "Kpi.Approve",
                "Transfers.View", "Transfers.Create", "Transfers.Approve",
                "Rewards.View", "Rewards.Create", "Rewards.Approve",
                "Reports.View", "Reports.Export",
                "Analytics.View"
            ]),
        new(
            RoleCode: "HR_STAFF",
            RoleName: "HR Staff",
            Description: "Chuyên viên Nhân sự (Hồ sơ, Tuyển dụng, C&B)",
            DefaultDataScope: DataScopeLevels.All,
            PermissionCodes:
            [
                "Dashboard.View",
                "Employees.View", "Employees.Create", "Employees.Edit", "Employees.Export",
                "Departments.View", "Departments.Create", "Departments.Edit",
                "Contracts.View", "Contracts.Create", "Contracts.Edit", "Contracts.Export",
                "Attendance.View", "Attendance.Create", "Attendance.Edit", "Attendance.Export",
                "Leave.View", "Leave.Create",
                "Recruitment.View", "Recruitment.Create", "Recruitment.Edit",
                "Onboarding.View", "Onboarding.Create", "Onboarding.Edit",
                "Training.View", "Training.Create", "Training.Edit",
                "Payroll.View", "Payroll.Calculate", "Payroll.Submit"
            ]),
        new(
            RoleCode: "ACCOUNTANT",
            RoleName: "Accountant",
            Description: "Kế toán Tiền lương, BHXH & Thuế TNCN",
            DefaultDataScope: DataScopeLevels.All,
            PermissionCodes:
            [
                "Dashboard.View",
                "Employees.View",
                "Attendance.View",
                "Payroll.View", "Payroll.Export",
                "Reports.View", "Reports.Export"
            ]),
        new(
            RoleCode: "LINE_MANAGER",
            RoleName: "Line Manager",
            Description: "Trưởng đơn vị / Quản lý trực tiếp",
            DefaultDataScope: DataScopeLevels.UnitTree,
            PermissionCodes:
            [
                "Dashboard.View",
                "Employees.View",
                "Attendance.View", "Attendance.Approve",
                "Leave.View", "Leave.Approve",
                "Kpi.View", "Kpi.Evaluate",
                "Training.View"
            ]),
        new(
            RoleCode: "EMPLOYEE",
            RoleName: "Employee",
            Description: "Nhân viên (Cổng thông tin tự phục vụ ESS)",
            DefaultDataScope: DataScopeLevels.SelfOnly,
            PermissionCodes: Array.Empty<string>())
    ];

    public static RoleMatrixDefinition? GetRoleDefinition(string roleCode) =>
        RoleDefinitions.FirstOrDefault(r => string.Equals(r.RoleCode, roleCode, StringComparison.OrdinalIgnoreCase));
}
