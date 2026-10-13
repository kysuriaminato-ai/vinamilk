using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Shifts;

/// <summary>
/// Đoạn làm việc trong ca (ShiftSegment).
/// Phục vụ mô hình ca liên tục (1 đoạn) hoặc ca gãy (Split Shift, ví dụ 2 đoạn: vắt sữa sớm và vắt sữa chiều).
/// </summary>
public class ShiftSegment : BaseEntity
{
    public int ShiftDefinitionId { get; private set; }
    public virtual ShiftDefinition? ShiftDefinition { get; private set; }

    public int SegmentIndex { get; private set; } = 1; // Đoạn 1, Đoạn 2...
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int BreakMinutes { get; private set; } = 0; // Phút nghỉ giữa đoạn
    public bool IsPaidBreak { get; private set; } = false; // Nghỉ có hưởng lương/tính công không

    protected ShiftSegment()
    {
    }

    public ShiftSegment(
        int shiftDefinitionId,
        int segmentIndex,
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes = 0,
        bool isPaidBreak = false)
    {
        if (segmentIndex <= 0)
            throw new DomainException("Chỉ số phân đoạn ca phải lớn hơn 0.", "ERR_SEGMENT_INDEX_INVALID");

        ShiftDefinitionId = shiftDefinitionId;
        SegmentIndex = segmentIndex;
        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes >= 0 ? breakMinutes : 0;
        IsPaidBreak = isPaidBreak;
    }

    /// <summary>
    /// Tính tổng số giờ làm việc tiêu chuẩn của phân đoạn (đã trừ nghỉ không hưởng lương).
    /// </summary>
    public decimal CalculateWorkHours()
    {
        double totalSpanMinutes;
        if (EndTime >= StartTime)
        {
            totalSpanMinutes = (EndTime - StartTime).TotalMinutes;
        }
        else
        {
            // Đoạn vắt qua nửa đêm
            totalSpanMinutes = (EndTime.ToTimeSpan() + TimeSpan.FromHours(24) - StartTime.ToTimeSpan()).TotalMinutes;
        }

        if (!IsPaidBreak && BreakMinutes > 0)
        {
            totalSpanMinutes = Math.Max(0, totalSpanMinutes - BreakMinutes);
        }

        return Math.Round((decimal)(totalSpanMinutes / 60.0), 2, MidpointRounding.AwayFromZero);
    }
}
