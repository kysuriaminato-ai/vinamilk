namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Vai trò người phê duyệt trong quy trình theo quan hệ tổ chức.
/// </summary>
public enum WorkflowApproverRole
{
    DirectManager = 1,      // Quản lý trực tiếp (ManagerId của nhân viên)
    UnitLeader = 2,         // Trưởng đơn vị (ManagerEmployeeId của OrganizationalUnit)
    HrManager = 3,          // Trưởng phòng Nhân sự
    Executive = 4,          // Ban Giám đốc / Ban Tổng Giám đốc
    SpecificEmployee = 5    // Nhân viên chỉ định đích danh theo ID
}