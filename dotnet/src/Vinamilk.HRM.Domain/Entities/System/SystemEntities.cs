using Vinamilk.HRM.Domain.Common;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;

namespace Vinamilk.HRM.Domain.Entities.SystemEntities;

public class WorkflowDefinition : BaseAuditableEntity
{
    public string WorkflowCode { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public ICollection<WorkflowInstance> Instances { get; set; } = new List<WorkflowInstance>();
}

public class WorkflowStep : BaseAuditableEntity
{
    public int WorkflowDefinitionId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string ApproverType { get; set; } = "DirectManager";
    public int? ApproverRoleId { get; set; }
    public int? ApproverUserId { get; set; }
    public string? ConditionExpression { get; set; }
    public int SlaHours { get; set; } = 24;

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
}

public class WorkflowInstance : BaseAuditableEntity
{
    public int WorkflowDefinitionId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int InitiatorEmployeeId { get; set; }
    public int CurrentStepOrder { get; set; } = 1;
    public int? CurrentApproverUserId { get; set; }
    public string Status { get; set; } = "InProgress";
    public DateTime? CompletedAt { get; set; }

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    public Employee InitiatorEmployee { get; set; } = null!;
    public ICollection<WorkflowApprovalLog> ApprovalLogs { get; set; } = new List<WorkflowApprovalLog>();
}

public class WorkflowApprovalLog : BaseLongAuditableEntity
{
    public int WorkflowInstanceId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int ApproverUserId { get; set; }
    public string Action { get; set; } = "Approved";
    public string? Comment { get; set; }
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    public User ApproverUser { get; set; } = null!;
}

public class Notification : BaseLongAuditableEntity
{
    public int RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = "SystemAlert";
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public User RecipientUser { get; set; } = null!;
}

public class AiRecommendation : BaseAuditableEntity
{
    public int? EmployeeId { get; set; }
    public int? OrganizationUnitId { get; set; }
    public string RecommendationType { get; set; } = "SalaryPromotion";
    public string PriorityLevel { get; set; } = "Medium";
    public decimal ConfidenceScore { get; set; } = 85m;
    public string Title { get; set; } = string.Empty;
    public string AnalysisContent { get; set; } = string.Empty;
    public string? SuggestedAction { get; set; }
    public string Status { get; set; } = "New";
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Employee? Employee { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }
}

public class AuditLog : BaseLongAuditableEntity
{
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ModuleName { get; set; } = "System";
    public string ActionType { get; set; } = "Update";
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? AffectedColumns { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class MenuItem : BaseAuditableEntity
{
    public string MenuCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? RouteUrl { get; set; }
    public string? RequiredPermissionCode { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

