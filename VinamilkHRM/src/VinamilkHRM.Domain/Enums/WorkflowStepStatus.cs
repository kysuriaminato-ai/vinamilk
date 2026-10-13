namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái bước duyệt trong luồng quy trình.
/// </summary>
public enum WorkflowStepStatus
{
    Pending = 0,    // Đang chờ phê duyệt
    Approved = 1,   // Đã chấp thuận
    Rejected = 2,   // Đã từ chối
    Escalated = 3,  // Đã tự chuyển cấp do quá hạn SLA
    Skipped = 4     // Bỏ qua (không thỏa điều kiện rẽ nhánh)
}