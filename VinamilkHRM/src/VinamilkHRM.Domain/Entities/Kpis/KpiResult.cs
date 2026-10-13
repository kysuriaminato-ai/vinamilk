using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Kpis;

/// <summary>
/// Kết quả đánh giá hiệu suất bất biến (KpiResult).
/// Được sinh ra duy nhất khi KpiSheet/KpiPeriod được khóa.
/// Là đầu vào duy nhất cho Động cơ tính lương 3P (Phiên 6).
/// </summary>
public class KpiResult : BaseEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public string Period { get; private set; } = string.Empty; // Format: YYYY-MM
    public decimal TotalScore { get; private set; }
    public KpiGrade Grade { get; private set; }
    public decimal BonusCoefficient { get; private set; } // 1.2, 1.0, 0.8, 0.5, 0.0

    public decimal UnitResultScore { get; private set; }
    public decimal IndividualWorkScore { get; private set; }
    public decimal PeopleManagementScore { get; private set; }
    public decimal ComplianceScore { get; private set; }

    public bool IsUnrated { get; private set; } // < 50% cong chuan
    public DateTime LockedAt { get; private set; } = DateTime.UtcNow;
    public string LockedBy { get; private set; } = "SYSTEM";

    protected KpiResult() { }

    public KpiResult(
        int employeeId,
        string period,
        decimal totalScore,
        KpiGrade grade,
        decimal bonusCoefficient,
        decimal unitResultScore,
        decimal individualWorkScore,
        decimal peopleManagementScore,
        decimal complianceScore,
        bool isUnrated,
        string lockedBy)
    {
        EmployeeId = employeeId;
        Period = period.Trim();
        TotalScore = totalScore;
        Grade = grade;
        BonusCoefficient = bonusCoefficient;
        UnitResultScore = unitResultScore;
        IndividualWorkScore = individualWorkScore;
        PeopleManagementScore = peopleManagementScore;
        ComplianceScore = complianceScore;
        IsUnrated = isUnrated;
        LockedBy = lockedBy;
        LockedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Khiếu nại kết quả KPI trong vòng 3 ngày sau khi khóa kỳ.
/// Kết quả xét duyệt được áp dụng điều chỉnh vào kỳ đánh giá tiếp theo.
/// </summary>
public class KpiAppeal : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public string Period { get; private set; } = string.Empty; // YYYY-MM
    public int KpiSheetId { get; private set; }
    public virtual KpiSheet? KpiSheet { get; private set; }

    public string Reason { get; private set; } = string.Empty;
    public decimal CurrentScore { get; private set; }
    public decimal ProposedScore { get; private set; }

    public KpiAppealStatus Status { get; private set; } = KpiAppealStatus.Pending;
    public int? ReviewerId { get; private set; }
    public string? ReviewerComment { get; private set; }
    public decimal? AdjustedScore { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    protected KpiAppeal() { }

    public KpiAppeal(
        int employeeId,
        string period,
        int kpiSheetId,
        string reason,
        decimal currentScore,
        decimal proposedScore)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do khiếu nại không được để trống.", "ERR_KPI_APPEAL_REASON_EMPTY");

        EmployeeId = employeeId;
        Period = period.Trim();
        KpiSheetId = kpiSheetId;
        Reason = reason.Trim();
        CurrentScore = currentScore;
        ProposedScore = proposedScore;
        Status = KpiAppealStatus.Pending;
    }

    public void Review(int reviewerId, KpiAppealStatus status, decimal? adjustedScore, string comment)
    {
        if (Status != KpiAppealStatus.Pending && Status != KpiAppealStatus.InReview)
            throw new DomainException("Đơn khiếu nại này đã được đóng trước đó.", "ERR_KPI_APPEAL_ALREADY_CLOSED");

        ReviewerId = reviewerId;
        Status = status;
        AdjustedScore = adjustedScore;
        ReviewerComment = comment.Trim();
        ReviewedAt = DateTime.UtcNow;
    }
}
