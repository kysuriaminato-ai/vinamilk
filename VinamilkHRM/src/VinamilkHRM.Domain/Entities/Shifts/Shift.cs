using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Shifts;

/// <summary>
/// Đại diện cho Ca làm việc chuẩn trong hệ thống Vinamilk.
/// </summary>
public class Shift : BaseAuditableEntity
{
    public string ShiftCode { get; private set; } = string.Empty;
    public string ShiftName { get; private set; } = string.Empty;
    public ShiftType ShiftType { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public int BreakMinutes { get; private set; } = 60;
    public decimal MultiplierRate { get; private set; } = 1.0m;

    public int? OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    protected Shift()
    {
    }

    public Shift(
        string shiftCode,
        string shiftName,
        ShiftType shiftType,
        TimeSpan startTime,
        TimeSpan endTime,
        int breakMinutes = 60,
        decimal multiplierRate = 1.0m,
        int? organizationalUnitId = null)
    {
        if (string.IsNullOrWhiteSpace(shiftCode))
            throw new DomainException("Mã ca làm việc không được để trống.", "ERR_SHIFT_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(shiftName))
            throw new DomainException("Tên ca làm việc không được để trống.", "ERR_SHIFT_NAME_EMPTY");

        if (multiplierRate <= 0)
            throw new DomainException("Hệ số lương ca làm việc phải lớn hơn 0.", "ERR_SHIFT_MULTIPLIER_INVALID");

        ShiftCode = shiftCode.Trim().ToUpperInvariant();
        ShiftName = shiftName.Trim();
        ShiftType = shiftType;
        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes >= 0 ? breakMinutes : 0;
        MultiplierRate = multiplierRate;
        OrganizationalUnitId = organizationalUnitId;
    }

    public void UpdateSchedule(TimeSpan startTime, TimeSpan endTime, int breakMinutes, decimal multiplierRate)
    {
        if (multiplierRate <= 0)
            throw new DomainException("Hệ số lương ca làm việc phải lớn hơn 0.", "ERR_SHIFT_MULTIPLIER_INVALID");

        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes >= 0 ? breakMinutes : 0;
        MultiplierRate = multiplierRate;
    }
}
