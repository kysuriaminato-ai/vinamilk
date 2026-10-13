using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Workflows;

/// <summary>
/// Bước phê duyệt cụ thể trong một phiên thực thi quy trình (WorkflowStepInstance).
/// Lưu lịch sử duyệt, người duyệt thực tế (trực tiếp hoặc ủy quyền), và kiểm soát SLA.
/// </summary>
public class WorkflowStepInstance : BaseAuditableEntity
{
    public int WorkflowInstanceId { get; private set; }
    public virtual WorkflowInstance? WorkflowInstance { get; private set; }

    public int StepOrder { get; private set; }
    public string StepName { get; private set; } = string.Empty;
    public WorkflowApproverRole RequiredRole { get; private set; }

    public int? AssignedApproverId { get; private set; }
    public virtual Employee? AssignedApprover { get; private set; }

    public int? ActualApproverId { get; private set; }
    public virtual Employee? ActualApprover { get; private set; }

    public int? DelegatedFromId { get; private set; }
    public virtual Employee? DelegatedFrom { get; private set; }

    public WorkflowStepStatus Status { get; private set; } = WorkflowStepStatus.Pending;
    public int SlaHours { get; private set; } = 48;
    public DateTimeOffset? SlaDeadline { get; private set; }
    public DateTimeOffset? ActionAt { get; private set; }
    public string? Comment { get; private set; }

    public bool IsAutoEscalate { get; private set; } = true;
    public WorkflowApproverRole? EscalateToRole { get; private set; }
    public DateTimeOffset? EscalatedAt { get; private set; }

    protected WorkflowStepInstance() { }

    public WorkflowStepInstance(
        int stepOrder,
        string stepName,
        WorkflowApproverRole requiredRole,
        int? assignedApproverId,
        int slaHours = 48,
        bool isAutoEscalate = true,
        WorkflowApproverRole? escalateToRole = null,
        DateTimeOffset? slaDeadline = null)
    {
        StepOrder = stepOrder;
        StepName = stepName?.Trim() ?? string.Empty;
        RequiredRole = requiredRole;
        AssignedApproverId = assignedApproverId;
        SlaHours = slaHours;
        IsAutoEscalate = isAutoEscalate;
        EscalateToRole = escalateToRole;
        SlaDeadline = slaDeadline;
        Status = WorkflowStepStatus.Pending;
    }

    public void SetPending(DateTimeOffset now)
    {
        Status = WorkflowStepStatus.Pending;
        SlaDeadline = now.AddHours(SlaHours);
    }

    public void Approve(int actualApproverId, int? delegatedFromId, string? comment, DateTimeOffset now)
    {
        if (Status != WorkflowStepStatus.Pending)
            throw new DomainException($"Bước duyệt không ở trạng thái Chờ duyệt (Trạng thái hiện tại: {Status}).", "ERR_WF_STEP_NOT_PENDING");

        ActualApproverId = actualApproverId;
        DelegatedFromId = delegatedFromId;
        Comment = comment?.Trim() ?? "Đã chấp thuận bước duyệt.";
        ActionAt = now;
        Status = WorkflowStepStatus.Approved;
    }

    public void Reject(int actualApproverId, string reason, DateTimeOffset now)
    {
        if (Status != WorkflowStepStatus.Pending)
            throw new DomainException($"Bước duyệt không ở trạng thái Chờ duyệt (Trạng thái hiện tại: {Status}).", "ERR_WF_STEP_NOT_PENDING");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do từ chối không được để trống.", "ERR_WF_REJECT_REASON_EMPTY");

        ActualApproverId = actualApproverId;
        Comment = reason.Trim();
        ActionAt = now;
        Status = WorkflowStepStatus.Rejected;
    }

    public void Escalate(int newApproverId, string reason, DateTimeOffset now)
    {
        AssignedApproverId = newApproverId;
        EscalatedAt = now;
        Status = WorkflowStepStatus.Pending; // Tiếp tục chờ ở cấp mới
        SlaDeadline = now.AddHours(SlaHours);
        Comment = $"[TỰ ĐỘNG CHUYỂN CẤP QUÁ HẠN SLA {SlaHours}h] {reason}".Trim();
    }

    public void Skip(string reason)
    {
        Status = WorkflowStepStatus.Skipped;
        Comment = $"[BỎ QUA DO ĐIỀU KIỆN RẼ NHÁNH]: {reason}";
    }
}