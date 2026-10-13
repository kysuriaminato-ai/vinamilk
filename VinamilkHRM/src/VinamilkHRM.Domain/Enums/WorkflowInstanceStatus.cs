namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái của một phiên thực thi quy trình phê duyệt.
/// </summary>
public enum WorkflowInstanceStatus
{
    Draft = 0,
    InProgress = 1,
    Approved = 2,
    Rejected = 3,
    Escalated = 4,
    Cancelled = 5
}