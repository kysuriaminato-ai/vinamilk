using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Kpis;

public record KpiAssignmentDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string UnitName,
    int EvaluatorId,
    string EvaluatorName,
    string Period,
    string Title,
    string Description,
    string TargetMetric,
    decimal WeightPercentage,
    DateTime DueDate,
    decimal BaseScore,
    decimal SlaBonusPenaltyScore,
    decimal ScoreAdjustment,
    string? SlaCalculationDetail,
    DateTime? CompletedDate,
    string? ActualResult,
    decimal? SelfEvaluationScore,
    decimal? FinalScore,
    KpiStatus Status,
    string? Feedback,
    DateTime? SubmissionDate,
    DateTime? EvaluationDate);

public record CreateKpiCommand(
    int EmployeeId,
    int EvaluatorId,
    string Period,
    string Title,
    string Description,
    string TargetMetric,
    decimal WeightPercentage,
    DateTime DueDate,
    decimal BaseScore = 100.0m);

public record SubmitKpiCommand(
    int KpiId,
    string ResultSummary,
    DateTime CompletedDate);

public record LeaderApproveKpiCommand(
    int KpiId,
    int LeaderId,
    decimal ScoreAdjustment,
    string Feedback);

public record KpiKanbanBoardDto(
    IReadOnlyList<KpiAssignmentDto> AssignedTasks,
    IReadOnlyList<KpiAssignmentDto> InProgressTasks,
    IReadOnlyList<KpiAssignmentDto> PendingApprovalTasks,
    IReadOnlyList<KpiAssignmentDto> ApprovedTasks,
    int TotalTasksCount,
    decimal OverallAverageScore);
