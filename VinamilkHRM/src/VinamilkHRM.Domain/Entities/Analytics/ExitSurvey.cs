using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Analytics;

/// <summary>
/// Khảo sát thôi việc (Exit Survey) gắn vào quy trình thôi việc của nhân viên.
/// </summary>
public class ExitSurvey : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateOnly ResignationDate { get; private set; }
    public ExitReasonCategory ReasonCategory { get; private set; }
    public string? DetailedReason { get; private set; }

    // Đánh giá các khía cạnh (thang điểm 1 - 5)
    public int CompensationSatisfaction { get; private set; }
    public int ManagementSatisfaction { get; private set; }
    public int WorkEnvironmentSatisfaction { get; private set; }
    public int CareerGrowthSatisfaction { get; private set; }

    public bool WouldRecommendCompany { get; private set; }
    public string? ImprovementSuggestions { get; private set; }

    protected ExitSurvey() { }

    public ExitSurvey(
        int employeeId,
        DateOnly resignationDate,
        ExitReasonCategory reasonCategory,
        string? detailedReason,
        int compensationSatisfaction,
        int managementSatisfaction,
        int workEnvironmentSatisfaction,
        int careerGrowthSatisfaction,
        bool wouldRecommendCompany,
        string? improvementSuggestions)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_SURVEY_EMP_INVALID");

        EmployeeId = employeeId;
        ResignationDate = resignationDate;
        ReasonCategory = reasonCategory;
        DetailedReason = detailedReason?.Trim();
        CompensationSatisfaction = Math.Clamp(compensationSatisfaction, 1, 5);
        ManagementSatisfaction = Math.Clamp(managementSatisfaction, 1, 5);
        WorkEnvironmentSatisfaction = Math.Clamp(workEnvironmentSatisfaction, 1, 5);
        CareerGrowthSatisfaction = Math.Clamp(careerGrowthSatisfaction, 1, 5);
        WouldRecommendCompany = wouldRecommendCompany;
        ImprovementSuggestions = improvementSuggestions?.Trim();
    }
}