using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Phiếu đăng ký và phê duyệt làm thêm giờ (OvertimeRequest).
/// Quy trình: Nhân viên đăng ký -> TeamLeader duyệt -> UnitManager duyệt.
/// Ràng buộc: Giờ làm vượt ca không có phiếu duyệt chỉ được ghi nhận cảnh báo, không trả tiền tăng ca.
/// </summary>
public class OvertimeRequest : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateOnly WorkDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public decimal PlannedHours { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public OvertimeStatus Status { get; private set; } = OvertimeStatus.Draft;

    public int? TeamLeaderId { get; private set; }
    public virtual Employee? TeamLeader { get; private set; }
    public DateTimeOffset? TeamLeaderApprovedAt { get; private set; }

    public int? UnitManagerId { get; private set; }
    public virtual Employee? UnitManager { get; private set; }
    public DateTimeOffset? UnitManagerApprovedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    protected OvertimeRequest()
    {
    }

    public OvertimeRequest(
        int employeeId,
        DateOnly workDate,
        TimeOnly startTime,
        TimeOnly endTime,
        decimal plannedHours,
        string reason)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_OT_EMP_INVALID");

        if (plannedHours <= 0)
            throw new DomainException("Số giờ đăng ký làm thêm phải lớn hơn 0.", "ERR_OT_HOURS_INVALID");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do làm thêm giờ không được để trống.", "ERR_OT_REASON_EMPTY");

        EmployeeId = employeeId;
        WorkDate = workDate;
        StartTime = startTime;
        EndTime = endTime;
        PlannedHours = plannedHours;
        Reason = reason.Trim();
        Status = OvertimeStatus.Submitted; // Mặc định gửi chờ duyệt
    }

    public void ApproveByTeamLeader(int teamLeaderId)
    {
        if (teamLeaderId <= 0)
            throw new DomainException("Mã Trưởng ca phê duyệt không hợp lệ.", "ERR_OT_TL_INVALID");

        TeamLeaderId = teamLeaderId;
        TeamLeaderApprovedAt = DateTimeOffset.UtcNow;
        Status = OvertimeStatus.TeamLeaderApproved;
    }

    public void ApproveByUnitManager(int unitManagerId)
    {
        if (unitManagerId <= 0)
            throw new DomainException("Mã Giám đốc đơn vị phê duyệt không hợp lệ.", "ERR_OT_MGR_INVALID");

        UnitManagerId = unitManagerId;
        UnitManagerApprovedAt = DateTimeOffset.UtcNow;
        Status = OvertimeStatus.Approved;
    }

    public void Reject(string reason)
    {
        Status = OvertimeStatus.Rejected;
        RejectionReason = reason?.Trim();
    }
}
