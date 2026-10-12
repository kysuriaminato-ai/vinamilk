namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái phiếu lương hàng tháng của nhân viên.
/// </summary>
public enum PaySlipStatus
{
    Draft = 1,              // Đang tính toán sơ bộ
    PendingApproval = 2,    // Chờ Kế toán trưởng & Giám đốc Nhân sự duyệt
    Approved = 3,           // Đã chốt số liệu thanh toán
    Paid = 4                // Đã giải ngân chuyển khoản qua ngân hàng
}
