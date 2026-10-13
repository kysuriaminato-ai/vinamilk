namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái quy trình đề xuất đổi ca làm việc (ShiftSwapRequest).
/// </summary>
public enum ShiftSwapStatus
{
    Proposed = 1,  // Người yêu cầu đề xuất đổi ca với đồng nghiệp
    Accepted = 2,  // Đồng nghiệp được yêu cầu đã chấp thuận đổi ca
    Approved = 3,  // Trưởng ca / TeamLeader phê duyệt chính thức
    Rejected = 4,  // Bị từ chối
    Cancelled = 5  // Người yêu cầu hủy đề xuất
}
