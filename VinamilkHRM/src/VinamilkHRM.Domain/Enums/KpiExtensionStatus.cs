namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái yêu cầu xin gia hạn chỉ tiêu KPI dạng Task.
/// </summary>
public enum KpiExtensionStatus
{
    Pending = 1,   // Đang chờ người chấm phê duyệt
    Approved = 2,  // Đã chấp thuận gia hạn
    Rejected = 3   // Từ chối gia hạn
}
