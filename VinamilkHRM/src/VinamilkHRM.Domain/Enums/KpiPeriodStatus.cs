namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái chu kỳ đánh giá KPI tháng (KpiPeriod).
/// </summary>
public enum KpiPeriodStatus
{
    Planning = 1,       // Lập kế hoạch & giao KPI (25 tháng trước -> ngày 5 tháng này)
    InProgress = 2,     // Đang thực hiện (đến ngày 15 giữa kỳ -> cuối tháng)
    SelfReview = 3,     // Nhân viên tự đánh giá (đến ngày 3 tháng sau)
    ManagerReview = 4,  // Quản lý chấm & phản hồi (đến ngày 7 tháng sau)
    Calibration = 5,    // Hiệu chuẩn phân bố điểm đơn vị (đến ngày 10 tháng sau)
    Locked = 6          // Đã chốt và khóa kỳ (bất biến, sinh KpiResult)
}
