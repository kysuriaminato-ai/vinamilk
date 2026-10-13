using System;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Kết quả chấm công chi tiết theo từng phân đoạn ca (AttendanceSegment).
/// </summary>
public class AttendanceSegment : BaseEntity
{
    public int DailyAttendanceId { get; private set; }
    public virtual DailyAttendance? DailyAttendance { get; private set; }

    public int SegmentIndex { get; private set; } = 1;
    public DateTimeOffset? CheckInTime { get; private set; }
    public DateTimeOffset? CheckOutTime { get; private set; }

    public int LateMinutes { get; private set; } = 0;
    public int EarlyLeaveMinutes { get; private set; } = 0;
    public decimal WorkHours { get; private set; } = 0m;
    public decimal NightHours { get; private set; } = 0m;

    protected AttendanceSegment()
    {
    }

    public AttendanceSegment(
        int dailyAttendanceId,
        int segmentIndex,
        DateTimeOffset? checkInTime,
        DateTimeOffset? checkOutTime,
        int lateMinutes,
        int earlyLeaveMinutes,
        decimal workHours,
        decimal nightHours)
    {
        DailyAttendanceId = dailyAttendanceId;
        SegmentIndex = segmentIndex;
        CheckInTime = checkInTime;
        CheckOutTime = checkOutTime;
        LateMinutes = lateMinutes >= 0 ? lateMinutes : 0;
        EarlyLeaveMinutes = earlyLeaveMinutes >= 0 ? earlyLeaveMinutes : 0;
        WorkHours = workHours >= 0 ? workHours : 0m;
        NightHours = nightHours >= 0 ? nightHours : 0m;
    }
}
