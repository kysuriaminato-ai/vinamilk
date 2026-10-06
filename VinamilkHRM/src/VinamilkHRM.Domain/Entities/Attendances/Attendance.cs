using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Shifts;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Đại diện cho Dữ liệu chấm công hàng ngày của nhân viên.
/// Encapsulation thông qua các domain methods ghi nhận giờ vào, giờ ra, tính công chuẩn và giờ làm thêm.
/// </summary>
public class Attendance : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public int ShiftId { get; private set; }
    public virtual Shift? Shift { get; private set; }

    public DateTime AttendanceDate { get; private set; }
    public TimeSpan? CheckInTime { get; private set; }
    public TimeSpan? CheckOutTime { get; private set; }
    public decimal WorkHours { get; private set; }
    public decimal OvertimeHours { get; private set; }
    public AttendanceStatus Status { get; private set; } = AttendanceStatus.AbsentUnexcused;
    public string? Note { get; private set; }

    protected Attendance()
    {
    }

    public Attendance(int employeeId, int shiftId, DateTime attendanceDate)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên chấm công không hợp lệ.", "ERR_ATT_EMP_INVALID");

        if (shiftId <= 0)
            throw new DomainException("Mã ca làm việc không hợp lệ.", "ERR_ATT_SHIFT_INVALID");

        EmployeeId = employeeId;
        ShiftId = shiftId;
        AttendanceDate = attendanceDate.Date;
        WorkHours = 0m;
        OvertimeHours = 0m;
        Status = AttendanceStatus.AbsentUnexcused;
    }

    /// <summary>
    /// Ghi nhận giờ Check-In từ máy quét vân tay / Face ID.
    /// </summary>
    public void RecordCheckIn(TimeSpan checkInTime, TimeSpan expectedStartTime, int lateToleranceMinutes = 15)
    {
        CheckInTime = checkInTime;

        TimeSpan threshold = expectedStartTime.Add(TimeSpan.FromMinutes(lateToleranceMinutes));
        if (checkInTime > threshold)
        {
            Status = AttendanceStatus.Late;
            Note = $"Đi muộn {(int)(checkInTime - expectedStartTime).TotalMinutes} phút.";
        }
        else
        {
            Status = AttendanceStatus.Present;
            Note = "Vào ca đúng giờ.";
        }
    }

    /// <summary>
    /// Ghi nhận giờ Check-Out và tự động tính số giờ công cùng giờ làm thêm.
    /// </summary>
    public void RecordCheckOut(TimeSpan checkOutTime, TimeSpan expectedEndTime, decimal scheduledShiftHours = 8.0m)
    {
        if (!CheckInTime.HasValue)
            throw new DomainException("Không thể ghi nhận giờ ra khi chưa có dữ liệu giờ vào (Check-In).", "ERR_ATT_NO_CHECKIN");

        CheckOutTime = checkOutTime;

        TimeSpan duration = checkOutTime - CheckInTime.Value;
        if (duration < TimeSpan.Zero)
        {
            // Xử lý ca đêm qua ngày
            duration = duration.Add(TimeSpan.FromHours(24));
        }

        decimal totalHours = Math.Max(0m, (decimal)duration.TotalHours);

        if (totalHours >= scheduledShiftHours)
        {
            WorkHours = scheduledShiftHours;
            OvertimeHours = Math.Round(totalHours - scheduledShiftHours, 2);
        }
        else
        {
            WorkHours = Math.Round(totalHours, 2);
            OvertimeHours = 0m;
            if (checkOutTime < expectedEndTime)
            {
                Status = Status == AttendanceStatus.Late ? AttendanceStatus.LateAndEarly : AttendanceStatus.EarlyDeparture;
            }
        }
    }

    /// <summary>
    /// Điều chỉnh chấm công thủ công có lý do từ cấp quản lý hoặc phòng nhân sự.
    /// </summary>
    public void ManualAdjustment(
        TimeSpan? checkIn,
        TimeSpan? checkOut,
        decimal workHours,
        decimal overtimeHours,
        AttendanceStatus status,
        string adjustmentReason)
    {
        if (string.IsNullOrWhiteSpace(adjustmentReason))
            throw new DomainException("Phải ghi rõ lý do điều chỉnh dữ liệu chấm công.", "ERR_ATT_ADJUST_REASON_EMPTY");

        if (workHours < 0 || overtimeHours < 0)
            throw new DomainException("Số giờ làm việc và giờ làm thêm không thể âm.", "ERR_ATT_HOURS_NEGATIVE");

        CheckInTime = checkIn;
        CheckOutTime = checkOut;
        WorkHours = workHours;
        OvertimeHours = overtimeHours;
        Status = status;
        Note = $"[Điều chỉnh thủ công]: {adjustmentReason.Trim()}";
    }

    public void MarkAsLeave(LeaveType leaveType)
    {
        Status = AttendanceStatus.OnLeave;
        WorkHours = 8.0m; // Công hưởng lương chế độ
        OvertimeHours = 0m;
        Note = $"Nghỉ theo chế độ: {leaveType}";
    }
}
