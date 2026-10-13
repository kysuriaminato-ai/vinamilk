using System.Collections.Generic;
using System.Linq;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Workflows;

/// <summary>
/// Định nghĩa cấu hình quy trình phê duyệt (WorkflowDefinition).
/// Hỗ trợ các bước động, rẽ nhánh theo điều kiện dữ liệu và SLA.
/// </summary>
public class WorkflowDefinition : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public WorkflowType WorkflowType { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<WorkflowStepDefinition> _steps = new();
    public virtual IReadOnlyCollection<WorkflowStepDefinition> Steps => _steps.AsReadOnly();

    protected WorkflowDefinition() { }

    public WorkflowDefinition(string code, string name, WorkflowType workflowType, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã quy trình không được để trống.", "ERR_WF_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên quy trình không được để trống.", "ERR_WF_NAME_EMPTY");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        WorkflowType = workflowType;
        Description = description?.Trim();
        IsActive = true;
    }

    public void AddStep(WorkflowStepDefinition step)
    {
        if (step == null)
            throw new DomainException("Bước quy trình không thể là null.", "ERR_WF_STEP_NULL");

        if (_steps.Any(s => s.StepOrder == step.StepOrder))
            throw new DomainException($"Đã tồn tại bước số {step.StepOrder} trong quy trình {Code}.", "ERR_WF_STEP_ORDER_DUPLICATE");

        _steps.Add(step);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}