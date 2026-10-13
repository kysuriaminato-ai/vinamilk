namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái chu trình khóa bảng chấm công tháng.
/// </summary>
public enum TimesheetPeriodStatus
{
    Draft = 1,               // Bảng công dự thảo trong tháng
    ConfirmedByLeader = 2,   // TeamLeader đã xác nhận số liệu công
    ApprovedByManager = 3,   // UnitManager đã duyệt bảng công đơn vị
    Locked = 4               // HR đã khóa sổ - Bất biến (chỉ đọc)
}
