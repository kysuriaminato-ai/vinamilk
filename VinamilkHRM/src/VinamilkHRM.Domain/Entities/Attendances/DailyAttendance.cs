using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Shifts;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Kết quả tính công hàng ngày cho mỗi RosterEntry (DailyAttendance).
/// Lưu trữ tổng số giờ làm, giờ đêm, giờ tăng ca theo loại ngày, mã công chuẩn và danh sách AttendanceSegment.
/// </summary>
public class DailyAttendance : BaseAuditableEntity
{
    public int? RosterEntryId { get; private set; }
    public virtual RosterEntry? RosterEntry { get; private set; }

    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateOnly AttendanceDate { get; private set; }

    public int? ShiftDefinitionId { get; private set; }
    public virtual ShiftDefinition? ShiftDefinition { get; private set; }

    public decimal TotalWorkHours { get; private set; } = 0m;
    public int LateMinutes { get; private set; } = 0;
    public int EarlyLeaveMinutes { get; private set; } = 0;
    public decimal NightHours { get; private set; } = 0m;

    // Giờ tăng ca được phê duyệt phân tách theo loại ngày
    public decimal RegularOtHours { get; private set; } = 0m; // Ngày thường (hệ số 150%)
    public decimal RestDayOtHours { get; private set; } = 0m; // Nghỉ tuần (hệ số 200%)
    public decimal HolidayOtHours { get; private set; } = 0m; // Ngày lễ (hệ số 300%)
    public decimal NightOtHours { get; private set; } = 0m;   // Tăng ca đêm

    /// <summary>
    /// Giờ làm vượt ca không có phiếu phê duyệt: chỉ đánh dấu cảnh báo, không trả tiền làm thêm.
    /// </summary>
    public decimal UnapprovedOtHours { get; private set; } = 0m;

    /// <summary>
    /// Mã công: X (đủ công 1.0), X/2 (nửa công 0.5), P (phép), Ô (ốm), TS (thai sản), L (lễ), NB (nghỉ bù), CT, ĐT, KP, KL...
    /// </summary>
    public string AttendanceCode { get; private set; } = "KP";
    public decimal WorkDayCredit { get; private set; } = 0m; // 1.0, 0.5, 0.0

    public bool IsComplete { get; private set; } = false;
    public AttendanceStatus Status { get; private set; } = AttendanceStatus.AbsentUnexcused;

    public bool HasWarning { get; private set; } = false;
    public string? WarningReason { get; private set; }
    public string? Remarks { get; private set; }

    private readonly List<AttendanceSegment> _segments = new();
    public virtual IReadOnlyCollection<AttendanceSegment> Segments => _segments.AsReadOnly();

    protected DailyAttendance()
    {
    }

    public DailyAttendance(
        int? rosterEntryId,
        int employeeId,
        DateOnly attendanceDate,
        int? shiftDefinitionId)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên chấm công không hợp lệ.", "ERR_DAILY_ATT_EMP_INVALID");

        RosterEntryId = rosterEntryId;
        EmployeeId = employeeId;
        AttendanceDate = attendanceDate;
        ShiftDefinitionId = shiftDefinitionId;
        AttendanceCode = "KP";
        WorkDayCredit = 0m;
        Status = AttendanceStatus.AbsentUnexcused;
        IsComplete = false;
    }

    public void AddSegment(
        int segmentIndex,
        DateTimeOffset? checkInTime,
        DateTimeOffset? checkOutTime,
        int lateMinutes,
        int earlyLeaveMinutes,
        decimal workHours,
        decimal nightHours)
    {
        var segment = new AttendanceSegment(
            Id,
            segmentIndex,
            checkInTime,
            checkOutTime,
            lateMinutes,
            earlyLeaveMinutes,
            workHours,
            nightHours);

        _segments.Add(segment);
    }

    public void SetSummary(
        decimal totalWorkHours,
        int lateMinutes,
        int earlyLeaveMinutes,
        decimal nightHours,
        decimal regularOtHours,
        decimal restDayOtHours,
        decimal holidayOtHours,
        decimal nightOtHours,
        decimal unapprovedOtHours,
        string attendanceCode,
        decimal workDayCredit,
        bool isComplete,
        AttendanceStatus status,
        bool hasWarning,
        string? warningReason = null,
        string? remarks = null)
    {
        TotalWorkHours = totalWorkHours >= 0 ? totalWorkHours : 0m;
        LateMinutes = lateMinutes >= 0 ? lateMinutes : 0;
        EarlyLeaveMinutes = earlyLeaveMinutes >= 0 ? earlyLeaveMinutes : 0;
        NightHours = nightHours >= 0 ? nightHours : 0m;

        RegularOtHours = regularOtHours >= 0 ? regularOtHours : 0m;
        RestDayOtHours = restDayOtHours >= 0 ? restDayOtHours : 0m;
        HolidayOtHours = holidayOtHours >= 0 ? holidayOtHours : 0m;
        NightOtHours = nightOtHours >= 0 ? nightOtHours : 0m;
        UnapprovedOtHours = unapprovedOtHours >= 0 ? unapprovedOtHours : 0m;

        AttendanceCode = attendanceCode;
        WorkDayCredit = workDayCredit;
        IsComplete = isComplete;
        Status = status;

        HasWarning = hasWarning;
        WarningReason = warningReason;
        Remarks = remarks;
    }

    public void ClearSegments()
    {
        _segments.Clear();
    }
}
