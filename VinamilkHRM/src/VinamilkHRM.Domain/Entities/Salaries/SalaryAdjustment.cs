using System;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Thực thể điều chỉnh lương (SalaryAdjustment) mở rộng từ SalaryProposal.
/// </summary>
public class SalaryAdjustment : SalaryProposal
{
    protected SalaryAdjustment() : base() { }

    public SalaryAdjustment(
        int employeeId,
        int proposedById,
        decimal currentSalary,
        decimal proposedSalary,
        decimal currentAllowance,
        decimal proposedAllowance,
        string reason,
        DateTime effectiveDate,
        SalaryAdjustmentType adjustmentType = SalaryAdjustmentType.Merit,
        decimal? compaRatioBefore = null,
        decimal? compaRatioAfter = null,
        decimal? annualKpiScore = null,
        KpiGrade? annualKpiGrade = null,
        string? evidence = null,
        SalaryFundSource fundSource = SalaryFundSource.CycleBudget,
        int? salaryReviewCycleId = null,
        int? organizationalUnitId = null)
        : base(
            employeeId,
            proposedById,
            currentSalary,
            proposedSalary,
            currentAllowance,
            proposedAllowance,
            reason,
            effectiveDate,
            adjustmentType,
            compaRatioBefore,
            compaRatioAfter,
            annualKpiScore,
            annualKpiGrade,
            evidence,
            fundSource,
            salaryReviewCycleId,
            organizationalUnitId)
    {
    }
}