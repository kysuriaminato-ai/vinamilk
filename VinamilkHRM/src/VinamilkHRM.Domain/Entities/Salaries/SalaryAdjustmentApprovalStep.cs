using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Bước phê duyệt trong quy trình tăng lương (Approval Step).
/// Lưu trữ trạng thái, người duyệt, thời gian duyệt và cờ xung đột lợi ích.
/// </summary>
public class SalaryAdjustmentApprovalStep : BaseAuditableEntity
{
    public int SalaryProposalId { get; private set; }
    public virtual SalaryProposal? SalaryProposal { get; private set; }

    public int StepNumber { get; private set; }
    public ApprovalRoleLevel RoleLevel { get; private set; }
    public string RoleName { get; private set; } = string.Empty;

    public int? ApproverId { get; private set; }
    public virtual Employee? Approver { get; private set; }

    public ApprovalStepStatus Status { get; private set; } = ApprovalStepStatus.Pending;
    public string? Comment { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public bool IsConflictOfInterestFlagged { get; private set; }

    protected SalaryAdjustmentApprovalStep() { }

    public SalaryAdjustmentApprovalStep(
        int stepNumber,
        ApprovalRoleLevel roleLevel,
        string roleName,
        int? approverId = null)
    {
        StepNumber = stepNumber;
        RoleLevel = roleLevel;
        RoleName = roleName?.Trim() ?? roleLevel.ToString();
        ApproverId = approverId;
        Status = ApprovalStepStatus.Pending;
    }

    public void Approve(int approverId, string? comment)
    {
        if (Status != ApprovalStepStatus.Pending)
            throw new DomainException("Bước duyệt này không ở trạng thái Chờ duyệt (Pending).", "ERR_SAL_STEP_NOT_PENDING");

        ApproverId = approverId;
        Comment = comment?.Trim() ?? "Đồng ý phê duyệt.";
        Status = ApprovalStepStatus.Approved;
        ApprovedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(int approverId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do từ chối bước duyệt không được để trống.", "ERR_SAL_REJECT_REASON_EMPTY");

        ApproverId = approverId;
        Comment = reason.Trim();
        Status = ApprovalStepStatus.Rejected;
        ApprovedAt = DateTimeOffset.UtcNow;
    }

    public void FlagConflict(string reason)
    {
        IsConflictOfInterestFlagged = true;
        Comment = $"[CẢNH BÁO XUNG ĐỘT LỢI ÍCH] {reason}".Trim();
    }
}