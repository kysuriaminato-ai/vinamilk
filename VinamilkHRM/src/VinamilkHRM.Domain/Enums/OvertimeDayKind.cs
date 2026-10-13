namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Loại ngày làm việc phục vụ xác định hệ số tăng ca (Overtime).
/// </summary>
public enum OvertimeDayKind
{
    Weekday = 1,       // Ngày làm việc bình thường trong tuần (150% ngày, đêm 200% / 210%)
    WeeklyRest = 2,    // Ngày nghỉ hàng tuần (Chủ nhật / Ngày nghỉ ca) (200% ngày, đêm 270%)
    PublicHoliday = 3  // Ngày nghỉ Lễ, Tết, ngày nghỉ có hưởng lương (300% ngày, đêm 390%)
}
