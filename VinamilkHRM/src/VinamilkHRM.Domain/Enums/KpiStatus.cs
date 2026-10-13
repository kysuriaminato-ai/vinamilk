namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái chu kỳ đánh giá hiệu suất công việc (KPI).
/// Giữ nguyên các giá trị 1-7 để bảo toàn tương thích dữ liệu cũ, mở rộng các trạng thái quy trình mới.
/// </summary>
public enum KpiStatus
{
    Draft = 1,              // Bản thảo chỉ tiêu
    Assigned = 2,           // Đã giao chỉ tiêu cho nhân viên (Chờ thực hiện)
    InProgress = 3,         // Đang triển khai thực hiện
    Submitted = 4,          // Nhân viên đã nộp báo cáo kết quả (Chờ duyệt)
    Approved = 5,           // Lãnh đạo đã đánh giá, phê duyệt điểm chính thức
    Rejected = 6,           // Trả về yêu cầu bổ sung minh chứng
    Cancelled = 7,          // Hủy bỏ chỉ tiêu KPI
    SelfReview = 8,         // Nhân viên đang tự đánh giá
    ManagerReview = 9,      // Quản lý đang thẩm định chấm điểm
    Calibration = 10,       // Đang trong giai đoạn hiệu chuẩn đơn vị
    Locked = 11             // Đã hiệu chuẩn và khóa kỳ đánh giá
}
