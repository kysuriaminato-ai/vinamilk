namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Cấp bậc lãnh đạo quản lý quyết định cơ cấu KPI và thẩm quyền phê duyệt.
/// </summary>
public enum LeadershipLevel
{
    None = 0,        // Nhân viên chuyên môn / Chuyên viên / Công nhân
    TeamLeader = 1,  // Trưởng nhóm / Tổ trưởng sản xuất / Trưởng ca
    Manager = 2,     // Trưởng phòng / Quản đốc xưởng / Trưởng trại
    Director = 3,    // Giám đốc Nhà máy / Giám đốc Chi nhánh / Giám đốc chức năng
    Executive = 4    // Ban Điều hành / Phó Tổng Giám Đốc / Tổng Giám Đốc (CEO)
}
