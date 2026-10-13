using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Đơn giải trình quên quẹt thẻ chấm công (AttendanceExplanation).
/// Quy trình: Nhân viên gửi -> TeamLeader duyệt -> Cập nhật lại DailyAttendance.
/// Ràng buộc: Giới hạn tối đa N lần/tháng (cấu hình trong bảng tham số, mặc định 3 lần/tháng).
/// </summary>
public class AttendanceExplanation : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateOnly AttendanceDate { get; private set; }
    public MissingPunchType MissingPunchType { get; private set; }
    public TimeOnly? ActualCheckIn { get; private set; }
    public TimeOnly? ActualCheckOut { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public ExplanationStatus Status { get; private set; } = ExplanationStatus.Submitted;
    public int? ApproverId { get; private set; }
    public virtual Employee? Approver { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    protected AttendanceExplanation()
    {
    }

    public AttendanceExplanation(
        int employeeId,
        DateOnly attendanceDate,
        MissingPunchType missingPunchType,
        TimeOnly? actualCheckIn,
        TimeOnly? actualCheckOut,
        string reason)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên giải trình không hợp lệ.", "ERR_EXPLANATION_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do giải trình không được để trống.", "ERR_EXPLANATION_REASON_EMPTY");

        EmployeeId = employeeId;
        AttendanceDate = attendanceDate;
        MissingPunchType = missingPunchType;
        ActualCheckIn = actualCheckIn;
        ActualCheckOut = actualCheckOut;
        Reason = reason.Trim();
        Status = ExplanationStatus.Submitted;
    }

    public void Approve(int approverId)
    {
        if (approverId <= 0)
            throw new DomainException("Mã người phê duyệt giải trình không hợp lệ.", "ERR_EXPLANATION_APPROVER_INVALID");

        ApproverId = approverId;
        ApprovedAt = DateTimeOffset.UtcNow;
        Status = ExplanationStatus.Approved;
    }

    public void Reject(string reason)
    {
        Status = ExplanationStatus.Rejected;
        RejectionReason = reason?.Trim();
    }
}
