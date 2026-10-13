namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái của chu kỳ xét tăng lương định kỳ tập đoàn (SalaryReviewCycle).
/// </summary>
public enum SalaryReviewCycleStatus
{
    Draft = 1,               // Khởi tạo, cấu hình điều kiện & phân bổ ngân sách
    Open = 2,                // Mở chu kỳ, cho phép các đơn vị bắt đầu xét duyệt
    UnitReview = 3,          // Trưởng đơn vị/phòng ban đang rà soát & điều chỉnh
    DivisionReview = 4,      // Giám đốc khối thẩm định
    HRReview = 5,            // Ban Nhân sự & C&B tổng hợp & đối soát
    ExecutiveApproval = 6,   // Chờ Tổng Giám Đốc phê duyệt toàn bộ
    Approved = 7,            // Tổng Giám Đốc đã phê duyệt chính thức
    Applied = 8              // Đã áp dụng mức lương mới & sinh phụ lục/truy lĩnh
}