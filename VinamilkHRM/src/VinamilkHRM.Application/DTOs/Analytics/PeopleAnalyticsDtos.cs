using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Analytics;

public record WorkforceSnapshotDto(
    int Id,
    string Month,
    int OrganizationalUnitId,
    string UnitName,
    int StartHeadcount,
    int EndHeadcount,
    decimal AverageHeadcount,
    int Hires,
    int Exits,
    string ExitsByReasonJson,
    int TransfersIn,
    int TransfersOut,
    int Promotions,
    decimal AbsenceRate,
    decimal OvertimeHours,
    decimal LaborCost,
    decimal AvgKpi,
    string GradeDistributionJson,
    DateTimeOffset SnapshotDate,
    bool IsSampleData);

public record ExitSurveyDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    DateOnly ResignationDate,
    ExitReasonCategory ReasonCategory,
    string? DetailedReason,
    int CompensationSatisfaction,
    int ManagementSatisfaction,
    int WorkEnvironmentSatisfaction,
    int CareerGrowthSatisfaction,
    bool WouldRecommendCompany,
    string? ImprovementSuggestions);

public record SubmitExitSurveyRequest(
    int EmployeeId,
    DateOnly ResignationDate,
    ExitReasonCategory ReasonCategory,
    string? DetailedReason,
    int CompensationSatisfaction,
    int ManagementSatisfaction,
    int WorkEnvironmentSatisfaction,
    int CareerGrowthSatisfaction,
    bool WouldRecommendCompany,
    string? ImprovementSuggestions);

public record MarketSalaryBenchmarkDto(
    int Id,
    string SurveySource,
    int Year,
    int JobFamilyId,
    string JobFamilyName,
    int JobGradeId,
    string JobGradeCode,
    MinimumWageRegion Region,
    decimal P25,
    decimal P50,
    decimal P75,
    bool IsSampleData);

public record ImportBenchmarkItemRequest(
    string SurveySource,
    int Year,
    int JobFamilyId,
    int JobGradeId,
    MinimumWageRegion Region,
    decimal P25,
    decimal P50,
    decimal P75,
    bool IsSampleData = false);

public record MacroIndicatorDto(
    int Id,
    string Code,
    string Period,
    decimal Value,
    string? SourceUrl,
    string? Notes,
    bool IsSampleData);

public record CompanyMetricDto(
    int Id,
    string Quarter,
    decimal Revenue,
    decimal NetProfitAfterTax,
    decimal ProductionVolumeLiters,
    bool IsSampleData);

public record PayStanceItemDto(
    int JobFamilyId,
    string JobFamilyName,
    int JobGradeId,
    string JobGradeCode,
    MinimumWageRegion Region,
    decimal InternalMid,
    decimal MarketP50,
    decimal GapPercentage,
    bool IsFlagged,
    string Recommendation);

public record PayStanceReportDto(
    IReadOnlyList<PayStanceItemDto> Items,
    int FlaggedCount,
    string Summary);

public record EmployeeAttritionRiskDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    int OrganizationalUnitId,
    string UnitName,
    int RiskScore,
    AttritionRiskLevel RiskLevel,
    IReadOnlyList<string> ExplainableReasons);

public record AttritionRiskReportDto(
    int UnitId,
    string UnitName,
    int TotalEvaluated,
    int HighRiskCount,
    int CriticalRiskCount,
    IReadOnlyList<EmployeeAttritionRiskDto> HighRiskEmployees);

public record ForecastPointDto(
    string Period,
    decimal PredictedValue,
    decimal LowerBound95,
    decimal UpperBound95,
    string Label);

public record ForecastReportDto(
    IReadOnlyList<ForecastPointDto> HeadcountForecast,
    IReadOnlyList<ForecastPointDto> TurnoverRateForecast,
    IReadOnlyList<ForecastPointDto> PayrollCostForecast,
    string Methodology);

public record UnitTurbulenceItemDto(
    int UnitId,
    string UnitName,
    int NetChange,
    int Hires,
    int Exits,
    decimal TurnoverRate);

public record ExecutiveDashboardDto(
    int TotalHeadcount,
    decimal Rolling12MonthTurnoverRate,
    decimal TotalMonthlyPayrollCost,
    decimal TotalAnnualLaborCost,
    decimal LaborCostToRevenueRatio,
    IReadOnlyDictionary<string, int> KpiGradeDistribution,
    IReadOnlyList<UnitTurbulenceItemDto> Top5TurbulentUnits,
    bool IsSampleData);

public record UnitDashboardDto(
    int UnitId,
    string UnitName,
    int Headcount,
    decimal MonthlyTurnoverRate,
    decimal AbsenceRate,
    decimal OvertimeHoursPerEmployee,
    string? LaborCostDisplay, // Nếu < 5 người thì trả về "không đủ cỡ mẫu"
    bool HasSufficientPrivacySampleSize,
    IReadOnlyList<EmployeeAttritionRiskDto> HighRiskEmployees);

public record MarketDashboardDto(
    IReadOnlyList<PayStanceItemDto> PayStanceItems,
    decimal CurrentNominalWageIncreaseRate,
    decimal CurrentCpiInflationRate,
    decimal RealWageIncreaseRate,
    bool IsWageIncreaseBelowInflationAlert,
    IReadOnlyList<MacroIndicatorDto> MacroIndicators);

public record AiSummaryRequest(
    string Period,
    int? UnitId = null);

public record AiSummaryResponseDto(
    string Period,
    string ExecutiveSummary,
    IReadOnlyList<string> StrategicRecommendations,
    string PromptAuditCopy,
    DateTimeOffset GeneratedAt,
    string ModelUsed);