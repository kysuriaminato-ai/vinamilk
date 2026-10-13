using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Analytics;

public record TurnoverForecastDto(
    string UnitCategory, // Khối Nhà máy Mega, Khối Bán lẻ Giấc Mơ Sữa Việt, Khối Trang trại Eco Farm, Khối Văn phòng HQ
    decimal CurrentRate, // % thực tế kỳ hiện tại
    decimal ForecastM1,  // % dự báo tháng T+1
    decimal ForecastM2,  // % dự báo tháng T+2
    decimal ForecastM3,  // % dự báo tháng T+3
    decimal ForecastM4,  // % dự báo tháng T+4
    decimal ForecastM5,  // % dự báo tháng T+5
    decimal ForecastM6,  // % dự báo tháng T+6
    decimal IndustryBenchmark, // Chuẩn ngành FMCG Sữa Việt Nam
    string RiskLevel,    // Thấp (Xanh), Trung bình (Vàng), Cao (Đỏ)
    string PrimaryDriver // Nguyên nhân chính tác động biến động
);

public record PayrollCostStructureDto(
    decimal TotalBaseSalary,
    decimal TotalAllowances,
    decimal TotalNightShiftAndOvertime,
    decimal TotalKpiBonuses,
    decimal TotalOtherBonuses,
    decimal TotalGrossPayroll,
    decimal EmployerInsuranceCost, // 23.5% = 17.5% BHXH + 3% BHYT + 1% BHTN + 2% KPCĐ
    decimal TotalCompanyLaborCost  // Gross + Employer Insurances
);

public record UnitCostShareDto(
    string UnitName,
    UnitType UnitType,
    int Headcount,
    decimal MonthlyLaborCost,
    decimal PercentageOfTotal
);

public record MinimumWageSimulationRequest(
    decimal MinimumWageIncreasePercentage = 6.0m, // % tăng lương tối thiểu vùng (Nghị định Chính phủ)
    decimal InsuranceCapIncreasePercentage = 7.3m, // % tăng trần đóng bảo hiểm
    decimal KpiBudgetAdjustmentPercentage = 5.0m   // % điều chỉnh quỹ thưởng KPI
);

public record MinimumWageSimulationResultDto(
    decimal CurrentMonthlyPayroll,
    decimal SimulatedMonthlyPayroll,
    decimal MonthlyDeltaCost,
    decimal AnnualizedDeltaCost,
    decimal CurrentEmployerInsuranceCost,
    decimal SimulatedEmployerInsuranceCost,
    decimal AnnualizedInsuranceDeltaCost,
    decimal TotalAnnualizedFinancialImpact,
    decimal EstimatedEbitdaImpactBasisPoints, // Điểm cơ bản tác động vào biên EBITDA (vd: -45 bps)
    IReadOnlyList<UnitImpactDto> DepartmentalImpacts
);

public record UnitImpactDto(
    string UnitName,
    int AffectedEmployeeCount,
    decimal AdditionalCostPerMonth,
    decimal PercentageIncrease
);

public record AiHrRecommendationDto(
    string Id,
    string Category, // Quản lý Ca kíp & An toàn, Giữ chân Nhân tài, Cải cách Lương thưởng, Kế thừa Lãnh đạo
    string TargetKhối,
    string RiskLevel, // HIGH, MEDIUM, LOW
    string KeyFinding,
    string ActionableRecommendation,
    string ProjectedRoiOrBenefit,
    int ConfidenceScorePercentage // 85% - 98%
);

public record StrategicDashboardDataDto(
    int TotalHeadcount,
    decimal OverallTurnoverRate,
    decimal AverageTenureYears,
    decimal TotalMonthlyLaborCost,
    decimal EmployeeNetPromoterScore, // eNPS (vd: +48)
    IReadOnlyList<TurnoverForecastDto> TurnoverForecasts,
    PayrollCostStructureDto PayrollStructure,
    IReadOnlyList<UnitCostShareDto> UnitCostBreakdown,
    MinimumWageSimulationResultDto BaselineSimulation,
    IReadOnlyList<AiHrRecommendationDto> AiRecommendations
);
