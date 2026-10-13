using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Services.Kpi;

/// <summary>
/// Giao diện trừu tượng hóa việc tính toán ngày làm việc theo WorkPattern của đơn vị (thuần Domain).
/// </summary>
public interface IWorkCalendar
{
    /// <summary>
    /// Đếm số ngày làm việc giữa hai mốc ngày (dựa theo WorkPattern của đơn vị và ngày nghỉ lễ).
    /// </summary>
    int CountWorkingDaysBetween(DateOnly startDate, DateOnly endDate, WorkPattern workPattern);

    /// <summary>
    /// Kiểm tra một ngày có phải là ngày làm việc của đơn vị hay không.
    /// </summary>
    bool IsWorkingDay(DateOnly date, WorkPattern workPattern);
}

/// <summary>
/// Bộ lịch mặc định của hệ thống Vinamilk theo chuẩn BLLĐ Việt Nam.
/// </summary>
public class DefaultWorkCalendar : IWorkCalendar
{
    private readonly HashSet<DateOnly> _holidays;

    public DefaultWorkCalendar(IEnumerable<DateOnly>? holidays = null)
    {
        _holidays = holidays != null ? new HashSet<DateOnly>(holidays) : new HashSet<DateOnly>();
    }

    public bool IsWorkingDay(DateOnly date, WorkPattern workPattern)
    {
        if (_holidays.Contains(date))
            return false;

        return workPattern switch
        {
            WorkPattern.OfficeWeek => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday,
            WorkPattern.ThreeShiftRotation => true, // 3 ca 4 kíp chạy liên tục
            WorkPattern.FarmSplitShift => true,     // Trang trại hoạt động 7 ngày/tuần
            WorkPattern.RetailRotation => true,     // Bán lẻ xoay ca cả cuối tuần
            _ => date.DayOfWeek != DayOfWeek.Sunday
        };
    }

    public int CountWorkingDaysBetween(DateOnly startDate, DateOnly endDate, WorkPattern workPattern)
    {
        if (startDate > endDate)
            return -CountWorkingDaysBetween(endDate, startDate, workPattern);

        int workingDays = 0;
        var current = startDate;
        while (current <= endDate)
        {
            if (IsWorkingDay(current, workPattern))
            {
                workingDays++;
            }
            current = current.AddDays(1);
        }

        return workingDays;
    }
}
