using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Đề xuất đổi ca làm việc giữa 2 nhân viên (ShiftSwapRequest).
/// Quy trình: Requester đề xuất -> Target chấp thuận -> TeamLeader phê duyệt chính thức.
/// </summary>
public class ShiftSwapRequest : BaseAuditableEntity
{
    public int RequesterEmployeeId { get; private set; }
    public virtual Employee? RequesterEmployee { get; private set; }
    public DateOnly RequesterDate { get; private set; }

    public int TargetEmployeeId { get; private set; }
    public virtual Employee? TargetEmployee { get; private set; }
    public DateOnly TargetDate { get; private set; }

    public string Reason { get; private set; } = string.Empty;
    public ShiftSwapStatus Status { get; private set; } = ShiftSwapStatus.Proposed;

    public int? ApproverId { get; private set; }
    public virtual Employee? Approver { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    protected ShiftSwapRequest()
    {
    }

    public ShiftSwapRequest(
        int requesterEmployeeId,
        DateOnly requesterDate,
        int targetEmployeeId,
        DateOnly targetDate,
        string reason)
    {
        if (requesterEmployeeId <= 0 || targetEmployeeId <= 0)
            throw new DomainException("Mã nhân viên đề xuất hoặc người nhận không hợp lệ.", "ERR_SWAP_EMP_INVALID");

        if (requesterEmployeeId == targetEmployeeId)
            throw new DomainException("Không thể tự đổi ca với chính mình.", "ERR_SWAP_SELF_INVALID");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do đổi ca không được để trống.", "ERR_SWAP_REASON_EMPTY");

        RequesterEmployeeId = requesterEmployeeId;
        RequesterDate = requesterDate;
        TargetEmployeeId = targetEmployeeId;
        TargetDate = targetDate;
        Reason = reason.Trim();
        Status = ShiftSwapStatus.Proposed;
    }

    public void Accept()
    {
        if (Status != ShiftSwapStatus.Proposed)
            throw new DomainException("Đề xuất đổi ca không ở trạng thái chờ xác nhận.", "ERR_SWAP_STATUS_INVALID");

        Status = ShiftSwapStatus.Accepted;
    }

    public void Approve(int approverId)
    {
        if (Status != ShiftSwapStatus.Accepted)
            throw new DomainException("Đồng nghiệp chưa xác nhận chấp thuận đổi ca.", "ERR_SWAP_NOT_ACCEPTED");

        if (approverId <= 0)
            throw new DomainException("Mã người phê duyệt không hợp lệ.", "ERR_SWAP_APPROVER_INVALID");

        ApproverId = approverId;
        ApprovedAt = DateTimeOffset.UtcNow;
        Status = ShiftSwapStatus.Approved;
    }

    public void Reject(string reason)
    {
        Status = ShiftSwapStatus.Rejected;
        RejectionReason = reason?.Trim();
    }
}
