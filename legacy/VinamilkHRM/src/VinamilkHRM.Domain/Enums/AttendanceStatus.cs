namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái chấm công hàng ngày của nhân viên.
/// </summary>
public enum AttendanceStatus
{
    Present = 1,          // Đúng giờ / Đủ công
    Late = 2,             // Đi muộn
    EarlyDeparture = 3,   // Về sớm
    LateAndEarly = 4,     // Vừa đi muộn vừa về sớm
    AbsentUnexcused = 5,  // Vắng mặt không phép
    OnLeave = 6,          // Nghỉ có phép
    Holiday = 7           // Ngày nghỉ lễ / Tết / Nghỉ tuần
}
