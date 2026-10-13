using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Salaries;

public record SalaryBandDto(
    int Id,
    int JobGradeId,
    string GradeCode,
    string GradeName,
    decimal Min,
    decimal Mid,
    decimal Max,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Description,
    decimal Spread);

public record CreateSalaryBandRequest(
    int JobGradeId,
    decimal Min,
    decimal Mid,
    decimal Max,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    string? Description = null);

public record SalaryReviewCycleDto(
    int Id,
    int Year,
    string Name,
    DateOnly EffectiveDate,
    SalaryReviewCycleStatus Status,
    int MinTenureMonths,
    bool ExcludeDisciplined,
    int ExcludeRecentPromotionMonths,
    decimal TotalBudget,
    decimal TotalUsed,
    decimal RemainingBudget);

public record CreateSalaryReviewCycleRequest(
    int Year,
    string Name,
    DateOnly EffectiveDate,
    int MinTenureMonths = 6,
    bool ExcludeDisciplined = true,
    int ExcludeRecentPromotionMonths = 6);

public record SalaryBudgetDto(
    int Id,
    int SalaryReviewCycleId,
    int OrganizationalUnitId,
    string UnitCode,
    string UnitName,
    decimal Amount,
    decimal Used,
    decimal Remaining,
    string? Notes);

public record UpdateSalaryBudgetRequest(
    int BudgetId,
    decimal NewAmount,
    string Reason);

public record UnitMeritSuggestionDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    int JobGradeId,
    string JobGradeCode,
    decimal CurrentP1,
    decimal Mid,
    decimal CompaRatio,
    decimal AnnualKpiScore,
    KpiGrade AnnualKpiGrade,
    decimal StandardSuggestedPercentage,
    decimal ProratedPercentage,
    decimal SuggestedIncreaseAmount,
    decimal ProposedP1,
    decimal MinAllowedPercentage,
    decimal MaxAllowedPercentage);

public record UnitMeritSubmissionItem(
    int EmployeeId,
    decimal ProposedP1,
    decimal AdjustmentPercentage,
    string? Reason);

public record UnitMeritSubmissionRequest(
    int SalaryReviewCycleId,
    int OrganizationalUnitId,
    IReadOnlyList<UnitMeritSubmissionItem> Items);

public record SpecialSalaryAdjustmentRequest(
    int EmployeeId,
    decimal ProposedP1,
    decimal ProposedAllowance,
    string Reason,
    string Evidence,
    DateTime EffectiveDate);

public record SalaryAdjustmentApprovalStepDto(
    int StepNumber,
    ApprovalRoleLevel RoleLevel,
    string RoleName,
    int? ApproverId,
    string? ApproverName,
    ApprovalStepStatus Status,
    string? Comment,
    DateTimeOffset? ApprovedAt,
    bool IsConflictOfInterestFlagged);

public record SalaryAdjustmentDetailDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    SalaryAdjustmentType Type,
    decimal CurrentP1,
    decimal ProposedP1,
    decimal IncreaseAmount,
    decimal IncreasePercentage,
    decimal CompaRatioBefore,
    decimal CompaRatioAfter,
    decimal? AnnualKpiScore,
    KpiGrade? AnnualKpiGrade,
    string Reason,
    string? Evidence,
    SalaryFundSource FundSource,
    SalaryProposalStatus Status,
    DateTime EffectiveDate,
    IReadOnlyList<SalaryAdjustmentApprovalStepDto> ApprovalSteps);

public record ApproveAdjustmentStepRequest(
    int ProposalId,
    int ApproverId,
    string? Comment = null);

// Báo cáo Total Rewards
public record UnitBudgetUsageReportItem(
    int UnitId,
    string UnitName,
    decimal BudgetAmount,
    decimal UsedAmount,
    decimal RemainingAmount,
    decimal UsagePercentage,
    int EmployeeCount,
    int IncreasedEmployeeCount);

public record GradeIncreaseDistributionItem(
    KpiGrade Grade,
    string GradeDescription,
    int EmployeeCount,
    decimal AverageIncreasePercentage,
    decimal TotalIncreaseAmount);

public record GenderPayEquityItem(
    int GradeNumber,
    string GradeCode,
    decimal MaleAverageP1,
    int MaleCount,
    decimal FemaleAverageP1,
    int FemaleCount,
    decimal GapPercentage);

public record TotalRewardsReportDto(
    int CycleId,
    string CycleName,
    IReadOnlyList<UnitBudgetUsageReportItem> UnitBudgetUsages,
    IReadOnlyList<GradeIncreaseDistributionItem> GradeDistributions,
    IReadOnlyList<GenderPayEquityItem> GenderPayEquities,
    decimal TotalAnnualAdditionalGross,
    decimal TotalAnnualEmployerCost);