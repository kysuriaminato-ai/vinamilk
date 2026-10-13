namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái giải trình quên chấm công (AttendanceExplanation).
/// </summary>
public enum ExplanationStatus
{
    Submitted = 1, // Chờ duyệt
    Approved = 2,  // Đã được duyệt
    Rejected = 3   // Bị từ chối
}
