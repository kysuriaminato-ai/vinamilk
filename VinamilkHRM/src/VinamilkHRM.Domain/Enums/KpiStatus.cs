namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái chu kỳ đánh giá hiệu suất công việc (KPI) theo SLA.
/// </summary>
public enum KpiStatus
{
    Draft = 1,              // Bản thảo chỉ tiêu
    Assigned = 2,           // Đã giao chỉ tiêu cho nhân viên (Chờ thực hiện)
    InProgress = 3,         // Đang triển khai thực hiện
    Submitted = 4,          // Nhân viên đã nộp báo cáo kết quả
    PendingApproval = 4,    // Chờ Lãnh đạo thẩm định và duyệt điểm (Alias cho Submitted)
    Approved = 5,           // Lãnh đạo đã đánh giá, phê duyệt điểm chính thức
    Rejected = 6            // Trả về yêu cầu bổ sung minh chứng
}
