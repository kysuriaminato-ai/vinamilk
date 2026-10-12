namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Tiến trình xét duyệt tờ trình tăng / điều chỉnh lương.
/// </summary>
public enum SalaryProposalStatus
{
    Draft = 1,                      // Khởi tạo đề xuất
    PendingDepartmentHead = 2,     // Chờ Trưởng bộ phận / Giám đốc khối duyệt
    PendingHRDirector = 3,         // Chờ Giám đốc Nhân sự tập đoàn phê duyệt
    Approved = 4,                  // Đã được Tổng Giám Đốc / HĐQT phê duyệt chính thức
    Rejected = 5                   // Bị từ chối
}
