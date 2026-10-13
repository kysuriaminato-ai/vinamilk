using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Workflows;

/// <summary>
/// Định nghĩa một bước trong cấu hình quy trình phê duyệt.
/// Có SLA, rẽ nhánh theo dữ liệu và tự động chuyển cấp.
/// </summary>
public class WorkflowStepDefinition : BaseAuditableEntity
{
    public int WorkflowDefinitionId { get; private set; }
    public virtual WorkflowDefinition? WorkflowDefinition { get; private set; }

    public int StepOrder { get; private set; }
    public string StepName { get; private set; } = string.Empty;
    public WorkflowApproverRole ApproverRole { get; private set; }
    public int? SpecificApproverId { get; private set; }
    
    public int SlaHours { get; private set; } = 48;
    public bool IsAutoEscalate { get; private set; } = true;
    public WorkflowApproverRole? EscalateToRole { get; private set; }

    // Điều kiện rẽ nhánh theo dữ liệu
    public decimal? MinDays { get; private set; }
    public decimal? MaxDays { get; private set; }
    public decimal? MinPercentage { get; private set; }
    public decimal? MinAmount { get; private set; }
    public LeadershipLevel? TargetLeadershipLevel { get; private set; }

    protected WorkflowStepDefinition() { }

    public WorkflowStepDefinition(
        int stepOrder,
        string stepName,
        WorkflowApproverRole approverRole,
        int slaHours = 48,
        bool isAutoEscalate = true,
        WorkflowApproverRole? escalateToRole = null,
        decimal? minDays = null,
        decimal? maxDays = null,
        decimal? minPercentage = null,
        decimal? minAmount = null,
        LeadershipLevel? targetLeadershipLevel = null,
        int? specificApproverId = null)
    {
        if (stepOrder <= 0)
            throw new DomainException("Thứ tự bước duyệt phải lớn hơn 0.", "ERR_WF_STEP_ORDER_INVALID");

        if (string.IsNullOrWhiteSpace(stepName))
            throw new DomainException("Tên bước duyệt không được để trống.", "ERR_WF_STEP_NAME_EMPTY");

        if (slaHours <= 0)
            throw new DomainException("Thời hạn SLA phải lớn hơn 0 giờ.", "ERR_WF_SLA_INVALID");

        StepOrder = stepOrder;
        StepName = stepName.Trim();
        ApproverRole = approverRole;
        SlaHours = slaHours;
        IsAutoEscalate = isAutoEscalate;
        EscalateToRole = escalateToRole ?? (approverRole switch
        {
            WorkflowApproverRole.DirectManager => WorkflowApproverRole.UnitLeader,
            WorkflowApproverRole.UnitLeader => WorkflowApproverRole.Executive,
            _ => WorkflowApproverRole.Executive
        });
        MinDays = minDays;
        MaxDays = maxDays;
        MinPercentage = minPercentage;
        MinAmount = minAmount;
        TargetLeadershipLevel = targetLeadershipLevel;
        SpecificApproverId = specificApproverId;
    }

    /// <summary>
    /// Đánh giá xem bước này có cần thực hiện dựa trên ngữ cảnh dữ liệu hay không.
    /// </summary>
    public bool ShouldExecute(WorkflowEvaluationContext context)
    {
        if (context == null) return true;

        if (MinDays.HasValue && (!context.Days.HasValue || context.Days.Value < MinDays.Value))
            return false;

        if (MaxDays.HasValue && (context.Days.HasValue && context.Days.Value > MaxDays.Value))
            return false;

        if (MinPercentage.HasValue && (!context.Percentage.HasValue || context.Percentage.Value < MinPercentage.Value))
            return false;

        if (MinAmount.HasValue && (!context.Amount.HasValue || context.Amount.Value < MinAmount.Value))
            return false;

        if (TargetLeadershipLevel.HasValue && context.LeadershipLevel.HasValue && context.LeadershipLevel.Value != TargetLeadershipLevel.Value)
            return false;

        return true;
    }
}