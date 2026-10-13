namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Cấp bậc vai trò phê duyệt trong quy trình tăng lương.
/// </summary>
public enum ApprovalRoleLevel
{
    UnitManager = 1,       // Trưởng đơn vị / Trưởng phòng
    DivisionDirector = 2,  // Giám đốc khối / Giám đốc nhà máy
    HRDirector = 3,        // Giám đốc Nhân sự & Total Rewards
    CEO = 4,               // Tổng Giám Đốc (CEO)
    BoardOfDirectors = 5   // Hội đồng quản trị (khi vượt trần đặc cách > 30% hoặc vượt Max khung)
}