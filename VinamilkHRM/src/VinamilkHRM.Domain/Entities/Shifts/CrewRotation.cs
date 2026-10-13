using System;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Shifts;

/// <summary>
/// Gán chu kỳ xoay ca và ngày neo cho kíp sản xuất (CrewRotation).
/// </summary>
public class CrewRotation : BaseAuditableEntity
{
    public int CrewId { get; private set; }
    public virtual Crew? Crew { get; private set; }

    public int RotationPatternId { get; private set; }
    public virtual RotationPattern? RotationPattern { get; private set; }

    /// <summary>
    /// Ngày neo gốc làm mốc tính chu kỳ xoay vòng.
    /// </summary>
    public DateOnly AnchorDate { get; private set; }

    /// <summary>
    /// Độ lệch ngày khởi đầu của kíp so với chu kỳ mẫu (0, 2, 4, 6...).
    /// </summary>
    public int StartDayOffset { get; private set; } = 0;

    protected CrewRotation()
    {
    }

    public CrewRotation(int crewId, int rotationPatternId, DateOnly anchorDate, int startDayOffset = 0)
    {
        if (crewId <= 0)
            throw new DomainException("Mã kíp không hợp lệ.", "ERR_CREW_ROT_CREW_INVALID");

        if (rotationPatternId <= 0)
            throw new DomainException("Mã mẫu chu kỳ không hợp lệ.", "ERR_CREW_ROT_PATTERN_INVALID");

        CrewId = crewId;
        RotationPatternId = rotationPatternId;
        AnchorDate = anchorDate;
        StartDayOffset = startDayOffset;
    }

    /// <summary>
    /// Tính toán mã ca làm việc của kíp tại ngày mục tiêu dựa theo ngày neo và chu kỳ.
    /// </summary>
    public string CalculateShiftCodeForDate(DateOnly targetDate, RotationPattern pattern)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        int daysDifference = targetDate.DayNumber - AnchorDate.DayNumber;
        int dayIndexInCycle = daysDifference + StartDayOffset;

        return pattern.GetShiftCodeForDay(dayIndexInCycle);
    }
}
