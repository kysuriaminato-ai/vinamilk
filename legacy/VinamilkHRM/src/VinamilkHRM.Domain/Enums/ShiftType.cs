namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Chủng loại ca làm việc tại các đơn vị Vinamilk.
/// </summary>
public enum ShiftType
{
    OfficeStandard = 1,    // Ca hành chính văn phòng (08:00 - 17:00)
    FactoryMorning = 2,    // Ca 1 nhà máy (06:00 - 14:00)
    FactoryAfternoon = 3,  // Ca 2 nhà máy (14:00 - 22:00)
    FactoryNight = 4,      // Ca 3 nhà máy / Ca đêm (22:00 - 06:00)
    FarmEarlyCare = 5,     // Ca trang trại chăm sóc & vắt sữa sớm (04:30 - 12:30)
    FarmLateCare = 6,      // Ca trang trại chăm sóc & vắt sữa chiều (12:30 - 20:30)
    RetailMorning = 7,     // Ca bán lẻ sáng (07:30 - 15:30)
    RetailEvening = 8      // Ca bán lẻ tối (14:30 - 22:00)
}
