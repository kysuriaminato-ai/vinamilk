namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái chu trình kỳ lương tại Vinamilk:
/// Draft (Tính thử nhiều lần) -> Submitted (Kế toán lương nộp duyệt)
/// -> Approved (Kế toán trưởng & Giám đốc Nhân sự phê duyệt) -> Locked (Khóa sổ) -> Paid (Đã giải ngân ngân hàng).
/// RÀNG BUỘC PHÁP LÝ: Từ Approved trở đi là BẤT BIẾN (Immutable). Tính lại ném lỗi ERR_PAY_PERIOD_LOCKED.
/// </summary>
public enum PayrollRunStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Locked = 4,
    Paid = 5
}
