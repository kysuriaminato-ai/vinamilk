namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái khiếu nại kết quả KPI sau khi khóa kỳ.
/// </summary>
public enum KpiAppealStatus
{
    Pending = 1,   // Chờ HR và Lãnh đạo thẩm tra
    InReview = 2,  // Đang xét lại số liệu
    Accepted = 3,  // Chấp thuận điều chỉnh (kết quả áp dụng kỳ sau)
    Rejected = 4   // Bác đơn khiếu nại
}
