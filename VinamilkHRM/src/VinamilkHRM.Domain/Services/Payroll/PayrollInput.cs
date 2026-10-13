using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Services.Payroll;

/// <summary>
/// Đại diện cho một phân đoạn lương trong tháng nếu nhân viên được điều chỉnh mức lương giữa kỳ.
/// </summary>
public record SalarySegment(
    decimal P1BaseSalary,
    decimal FixedAllowances,
    decimal InsurableAllowances,
    decimal TaxExemptAllowances,
    int EffectiveCalendarDays,
    int TotalCalendarDaysInPeriod);

/// <summary>
/// Đầu vào đầy đủ cho Động cơ tính lương thuần (PayrollCalculator).
/// Hoàn toàn độc lập với Cơ sở dữ liệu và Entity Framework.
/// </summary>
public class PayrollInput
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PayrollPeriod { get; set; } = string.Empty; // YYYY-MM
    public DateOnly PeriodEndDate { get; set; }

    public LeadershipLevel LeadershipLevel { get; set; } = LeadershipLevel.None;
    public MinimumWageRegion Region { get; set; } = MinimumWageRegion.RegionI;
    public bool IsUnionMember { get; set; } = true;
    public int DependentCount { get; set; } = 0;

    // Bảng công tháng đã khóa
    public MonthlyTimesheetSummary? TimesheetSummary { get; set; }
    public bool IsTimesheetLocked { get; set; } = false;

    // Lương vị trí P1 và các phân đoạn hợp đồng
    public decimal P1BaseSalary { get; set; }
    public decimal FixedAllowances { get; set; }
    public decimal InsurableAllowances { get; set; }
    public decimal TaxExemptAllowances { get; set; }
    public List<SalarySegment> SalarySegments { get; set; } = new();

    // P2: Phụ cấp năng lực
    public decimal CompetencyAllowanceP2 { get; set; }

    // P3: Kết quả KPI đã khóa từ Phiên 5
    public KpiResult? KpiResult { get; set; }

    // Phụ cấp chuyên cần & quy chế
    public decimal AttendanceAllowance { get; set; } = 0m;
    public int MaxLateTimesForAttendance { get; set; } = 2;

    // Chế độ thôi việc / nghỉ việc
    public bool IsResignedInPeriod { get; set; } = false;
    public decimal UntakenAnnualLeaveDays { get; set; } = 0m;

    // Khấu trừ bồi thường thiệt hại (không có phạt)
    public decimal DamageCompensationAmount { get; set; } = 0m;

    // Phân loại hợp đồng đặc thù
    public bool IsNonResidentOrShortTerm { get; set; } = false;
    public decimal FlatWithholdingRate { get; set; } = 0.10m;

    // Tham số pháp lý hiệu lực tại ngày cuối kỳ
    public decimal BaseSalary { get; set; } = 2_530_000m;
    public decimal RegionalMinimumWage { get; set; } = 5_310_000m;
    public decimal SelfDeduction { get; set; } = 15_500_000m;
    public decimal DependentDeductionPerPerson { get; set; } = 6_200_000m;
    public IReadOnlyList<TaxBracket> TaxBrackets { get; set; } = Array.Empty<TaxBracket>();

    public decimal EmployeeSocialInsuranceRate { get; set; } = 0.08m;
    public decimal EmployeeHealthInsuranceRate { get; set; } = 0.015m;
    public decimal EmployeeUnemploymentInsuranceRate { get; set; } = 0.01m;
    public decimal EmployeeUnionRate { get; set; } = 0.01m;

    public decimal EmployerSocialInsuranceRate { get; set; } = 0.175m; // 14% hưu trí + 3% ốm đau + 0.5% BNN
    public decimal EmployerHealthInsuranceRate { get; set; } = 0.03m;
    public decimal EmployerUnemploymentInsuranceRate { get; set; } = 0.01m;
    public decimal EmployerUnionRate { get; set; } = 0.02m;

    public decimal MaxInsuranceBaseMultiplier { get; set; } = 20m;
    public decimal LunchAllowanceTaxExemptCap { get; set; } = 730_000m;

    public decimal OvertimeWeekdayMultiplier { get; set; } = 1.50m;
    public decimal OvertimeWeekdayNightMultiplier { get; set; } = 2.00m;
    public decimal OvertimeWeekendMultiplier { get; set; } = 2.00m;
    public decimal OvertimeWeekendNightMultiplier { get; set; } = 2.70m;
    public decimal OvertimeHolidayMultiplier { get; set; } = 3.00m;
    public decimal OvertimeHolidayNightMultiplier { get; set; } = 3.90m;
    public decimal NightShiftAllowanceMultiplier { get; set; } = 0.30m;

    public decimal TargetBonusRateExecutive { get; set; } = 0.35m;
    public decimal TargetBonusRateDirector { get; set; } = 0.30m;
    public decimal TargetBonusRateManager { get; set; } = 0.25m;
    public decimal TargetBonusRateTeamLeader { get; set; } = 0.20m;
    public decimal TargetBonusRateWorkerEmployee { get; set; } = 0.15m;

    public decimal GetTargetBonusRate() => LeadershipLevel switch
    {
        LeadershipLevel.Executive => TargetBonusRateExecutive,
        LeadershipLevel.Director => TargetBonusRateDirector,
        LeadershipLevel.Manager => TargetBonusRateManager,
        LeadershipLevel.TeamLeader => TargetBonusRateTeamLeader,
        _ => TargetBonusRateWorkerEmployee
    };
}
