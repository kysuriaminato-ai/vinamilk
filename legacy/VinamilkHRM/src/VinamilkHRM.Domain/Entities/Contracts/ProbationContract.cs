using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Contracts;

/// <summary>
/// Đại diện cho Hợp đồng thử việc tại Vinamilk.
/// </summary>
public class ProbationContract : LaborContract
{
    public decimal? ProbationEvaluationScore { get; private set; }
    public int? MentorEmployeeId { get; private set; }
    public virtual Employee? MentorEmployee { get; private set; }
    public bool? ProbationPassed { get; private set; }
    public string? EvaluationFeedback { get; private set; }

    protected ProbationContract() : base()
    {
    }

    public ProbationContract(
        string contractNumber,
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        decimal basicSalary,
        decimal insuranceSalary,
        DateTime signDate,
        int? mentorEmployeeId = null,
        int workingHoursPerWeek = 40)
        : base(
            contractNumber,
            employeeId,
            ContractType.Probation,
            startDate,
            endDate,
            basicSalary,
            insuranceSalary,
            signDate,
            workingHoursPerWeek)
    {
        MentorEmployeeId = mentorEmployeeId;
    }

    /// <summary>
    /// Đánh giá kết quả thử việc và xác định nhân sự có đạt yêu cầu ký hợp đồng chính thức hay không.
    /// </summary>
    public void CompleteProbation(decimal evaluationScore, bool passed, string feedback)
    {
        if (evaluationScore < 0 || evaluationScore > 100)
            throw new DomainException("Điểm đánh giá thử việc phải nằm trong thang điểm 0 - 100.", "ERR_PROBATION_SCORE_INVALID");

        if (string.IsNullOrWhiteSpace(feedback))
            throw new DomainException("Phải có nhận xét đánh giá chi tiết kết quả thử việc của nhân sự.", "ERR_PROBATION_FEEDBACK_EMPTY");

        ProbationEvaluationScore = evaluationScore;
        ProbationPassed = passed;
        EvaluationFeedback = feedback.Trim();

        if (!passed)
        {
            Terminate(DateTime.UtcNow, $"Không đạt đánh giá thử việc (Điểm: {evaluationScore}/100)");
        }
    }
}
