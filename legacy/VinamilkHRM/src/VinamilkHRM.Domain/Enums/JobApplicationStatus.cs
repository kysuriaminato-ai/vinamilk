namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái hồ sơ ứng tuyển của ứng viên tại Cổng thông tin Tuyển dụng Vinamilk.
/// </summary>
public enum JobApplicationStatus
{
    Submitted = 1,     // Đã nộp hồ sơ trực tuyến
    Screening = 2,     // Đang sàng lọc hồ sơ CV
    Interviewed = 3,   // Đã tham gia phỏng vấn chuyên môn
    Offered = 4,       // Đã gửi thư mời nhận việc (Offer Letter)
    Hired = 5,         // Đã trúng tuyển & onboard thành Nhân viên Thử việc (Probation)
    Rejected = 6       // Không phù hợp đợt tuyển dụng
}
