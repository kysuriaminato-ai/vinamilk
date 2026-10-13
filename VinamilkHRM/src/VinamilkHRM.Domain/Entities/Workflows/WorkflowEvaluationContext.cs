using System.Collections.Generic;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Workflows;

/// <summary>
/// Ngữ cảnh dữ liệu dùng để đánh giá điều kiện rẽ nhánh trong quy trình.
/// </summary>
public class WorkflowEvaluationContext
{
    public decimal? Days { get; set; }
    public decimal? Percentage { get; set; }
    public decimal? Amount { get; set; }
    public LeadershipLevel? LeadershipLevel { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();

    public WorkflowEvaluationContext() { }

    public WorkflowEvaluationContext(decimal? days = null, decimal? percentage = null, decimal? amount = null, LeadershipLevel? leadershipLevel = null)
    {
        Days = days;
        Percentage = percentage;
        Amount = amount;
        LeadershipLevel = leadershipLevel;
    }
}