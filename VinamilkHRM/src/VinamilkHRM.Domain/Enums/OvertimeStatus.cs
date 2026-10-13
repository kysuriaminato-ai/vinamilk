namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái quy trình phê duyệt làm thêm giờ (OvertimeRequest).
/// </summary>
public enum OvertimeStatus
{
    Draft = 1,              // Đăng ký mới
    Submitted = 2,          // Đã gửi chờ duyệt
    TeamLeaderApproved = 3, // Trưởng ca / TeamLeader đã duyệt
    Approved = 4,           // Giám đốc đơn vị / UnitManager phê duyệt chính thức
    Rejected = 5            // Bị từ chối
}
