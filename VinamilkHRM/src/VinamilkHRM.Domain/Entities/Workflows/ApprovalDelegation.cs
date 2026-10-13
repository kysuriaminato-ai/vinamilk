using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Workflows;

/// <summary>
/// Quản lý ủy quyền phê duyệt có thời hạn (Approval Delegation).
/// Cho phép người duyệt vắng mặt ủy quyền cho người khác duyệt thay trong khoảng thời gian.
/// </summary>
public class ApprovalDelegation : BaseAuditableEntity
{
    public int DelegatorId { get; private set; }
    public virtual Employee? Delegator { get; private set; }

    public int DelegateeId { get; private set; }
    public virtual Employee? Delegatee { get; private set; }

    public DateTimeOffset FromDate { get; private set; }
    public DateTimeOffset ToDate { get; private set; }

    public WorkflowType? WorkflowType { get; private set; } // Null = áp dụng cho mọi quy trình
    public string Reason { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    protected ApprovalDelegation() { }

    public ApprovalDelegation(
        int delegatorId,
        int delegateeId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        string reason,
        WorkflowType? workflowType = null)
    {
        if (delegatorId <= 0)
            throw new DomainException("Mã người ủy quyền không hợp lệ.", "ERR_DELEGATE_DELEGATOR_INVALID");

        if (delegateeId <= 0)
            throw new DomainException("Mã người được ủy quyền không hợp lệ.", "ERR_DELEGATE_DELEGATEE_INVALID");

        if (delegatorId == delegateeId)
            throw new DomainException("Không thể tự ủy quyền cho chính mình.", "ERR_DELEGATE_SELF_INVALID");

        if (toDate <= fromDate)
            throw new DomainException("Thời điểm kết thúc ủy quyền phải sau thời điểm bắt đầu.", "ERR_DELEGATE_DATES_INVALID");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do ủy quyền không được để trống.", "ERR_DELEGATE_REASON_EMPTY");

        DelegatorId = delegatorId;
        DelegateeId = delegateeId;
        FromDate = fromDate;
        ToDate = toDate;
        Reason = reason.Trim();
        WorkflowType = workflowType;
        IsActive = true;
    }

    /// <summary>
    /// Kiểm tra hiệu lực của ủy quyền tại thời điểm chỉ định.
    /// </summary>
    public bool IsValid(DateTimeOffset checkTime, WorkflowType? type = null)
    {
        if (!IsActive) return false;
        if (checkTime < FromDate || checkTime > ToDate) return false;
        if (WorkflowType.HasValue && type.HasValue && WorkflowType.Value != type.Value) return false;
        return true;
    }

    public void Cancel(string cancelReason)
    {
        IsActive = false;
        Reason = $"{Reason} [Đã hủy: {cancelReason?.Trim()}]";
    }
}