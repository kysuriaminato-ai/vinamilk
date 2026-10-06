namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Tiến trình phê duyệt đơn xin nghỉ phép.
/// </summary>
public enum LeaveStatus
{
    Pending = 1,    // Chờ phê duyệt
    Approved = 2,   // Đã phê duyệt
    Rejected = 3,   // Đã từ chối
    Cancelled = 4   // Nhân viên hủy bỏ đơn
}
