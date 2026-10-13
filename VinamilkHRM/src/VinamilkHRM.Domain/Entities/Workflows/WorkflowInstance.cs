using System;
using System.Collections.Generic;
using System.Linq;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Workflows;

/// <summary>
/// Đại diện cho một phiên chạy quy trình phê duyệt (WorkflowInstance).
/// Quản lý vòng đời từ Draft -> InProgress -> Approved / Rejected / Escalated.
/// </summary>
public class WorkflowInstance : BaseAuditableEntity
{
    public int WorkflowDefinitionId { get; private set; }
    public virtual WorkflowDefinition? WorkflowDefinition { get; private set; }

    public string EntityType { get; private set; } = string.Empty; // vd: "LeaveRequest", "OvertimeRequest", "SalaryAdjustment"
    public int EntityId { get; private set; }

    public int RequesterId { get; private set; }
    public virtual Employee? Requester { get; private set; }

    public int CurrentStepOrder { get; private set; } = 1;
    public WorkflowInstanceStatus Status { get; private set; } = WorkflowInstanceStatus.Draft;

    private readonly List<WorkflowStepInstance> _steps = new();
    public virtual IReadOnlyCollection<WorkflowStepInstance> Steps => _steps.AsReadOnly();

    protected WorkflowInstance() { }

    public WorkflowInstance(
        int workflowDefinitionId,
        string entityType,
        int entityId,
        int requesterId)
    {
        if (workflowDefinitionId <= 0)
            throw new DomainException("Mã định nghĩa quy trình không hợp lệ.", "ERR_WF_DEF_INVALID");

        if (string.IsNullOrWhiteSpace(entityType))
            throw new DomainException("Loại thực thể xử lý quy trình không được để trống.", "ERR_WF_ENTITY_TYPE_EMPTY");

        if (entityId <= 0)
            throw new DomainException("Mã thực thể nghiệp vụ không hợp lệ.", "ERR_WF_ENTITY_ID_INVALID");

        if (requesterId <= 0)
            throw new DomainException("Mã nhân viên tạo đơn không hợp lệ.", "ERR_WF_REQUESTER_INVALID");

        WorkflowDefinitionId = workflowDefinitionId;
        EntityType = entityType.Trim();
        EntityId = entityId;
        RequesterId = requesterId;
        Status = WorkflowInstanceStatus.InProgress;
        CurrentStepOrder = 1;
    }

    public void AddStep(WorkflowStepInstance step)
    {
        if (step == null) throw new DomainException("Bước duyệt không thể là null.", "ERR_WF_STEP_NULL");
        _steps.Add(step);
    }

    public WorkflowStepInstance? GetCurrentStep()
    {
        return _steps.OrderBy(s => s.StepOrder).FirstOrDefault(s => s.StepOrder == CurrentStepOrder && s.Status == WorkflowStepStatus.Pending);
    }

    public void AdvanceToNextStep(DateTimeOffset now)
    {
        var nextStep = _steps.OrderBy(s => s.StepOrder).FirstOrDefault(s => s.StepOrder > CurrentStepOrder && s.Status == WorkflowStepStatus.Pending);
        if (nextStep != null)
        {
            CurrentStepOrder = nextStep.StepOrder;
            nextStep.SetPending(now);
        }
        else
        {
            // Đã hoàn thành tất cả các bước
            Status = WorkflowInstanceStatus.Approved;
        }
    }

    public void MarkRejected()
    {
        Status = WorkflowInstanceStatus.Rejected;
    }

    public void MarkEscalated()
    {
        Status = WorkflowInstanceStatus.Escalated;
    }

    public void Cancel(string reason)
    {
        Status = WorkflowInstanceStatus.Cancelled;
    }
}