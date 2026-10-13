using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Kpis;

/// <summary>
/// Phiếu đánh giá hiệu suất làm việc (KPI Sheet) của một nhân viên trong một kỳ (tháng YYYY-MM).
/// </summary>
public class KpiSheet : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public int KpiPeriodId { get; private set; }
    public virtual KpiPeriod? KpiPeriod { get; private set; }

    public string Period { get; private set; } = string.Empty; // YYYY-MM
    public LeadershipLevel LeadershipLevel { get; private set; }

    public int? JobFamilyId { get; private set; }
    public virtual JobFamily? JobFamily { get; private set; }

    public int? JobTitleId { get; private set; }
    public virtual JobTitle? JobTitle { get; private set; }

    public int OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    public int EvaluatorId { get; private set; }
    public virtual Employee? Evaluator { get; private set; }

    public KpiStatus Status { get; private set; } = KpiStatus.Draft;

    // Ngày công chuẩn và thực tế để kiểm tra điều kiện xếp loại
    public decimal StandardWorkDays { get; private set; } = 26m;
    public decimal ActualWorkDays { get; private set; } = 0m;
    public bool IsStandardWorkingDaysMet { get; private set; } = true; // >= 50% cong chuan

    // Mốc thời gian nộp đánh giá
    public DateTime? SelfReviewSubmittedAt { get; private set; }
    public DateTime? ManagerReviewSubmittedAt { get; private set; }

    // Phạt quy trình
    public decimal SelfReviewLatePenalty { get; private set; } = 0m; // -2 diem neu nop tu danh gia tre
    public decimal ManagerReviewLatePenalty { get; private set; } = 0m; // -1 diem/cap duoi (max -10) neu quan ly cham tre

    // Cổng tuân thủ (áp dụng cho Director)
    public bool HasSevereViolation { get; private set; } = false;

    // Điểm số và xếp loại cuối cùng
    public decimal UnitResultScore { get; private set; } = 0m;
    public decimal IndividualWorkScore { get; private set; } = 0m;
    public decimal PeopleManagementScore { get; private set; } = 0m;
    public decimal ComplianceScore { get; private set; } = 0m;

    public decimal TotalScore { get; private set; } = 0m;
    public KpiGrade FinalGrade { get; private set; } = KpiGrade.C;
    public decimal BonusCoefficient { get; private set; } = 0.8m;

    public string? ManagerFeedback { get; private set; }
    public string? CalibrationNotes { get; private set; }

    private readonly List<KpiItem> _items = new();
    public virtual IReadOnlyCollection<KpiItem> Items => _items.AsReadOnly();

    protected KpiSheet() { }

    public KpiSheet(
        int employeeId,
        int kpiPeriodId,
        string period,
        LeadershipLevel leadershipLevel,
        int organizationalUnitId,
        int evaluatorId,
        int? jobFamilyId = null,
        int? jobTitleId = null,
        decimal standardWorkDays = 26m)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_KPI_SHEET_EMP_INVALID");

        if (kpiPeriodId <= 0)
            throw new DomainException("Mã kỳ đánh giá không hợp lệ.", "ERR_KPI_SHEET_PERIOD_INVALID");

        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("Kỳ đánh giá không được để trống.", "ERR_KPI_SHEET_PERIOD_EMPTY");

        if (evaluatorId <= 0)
            throw new DomainException("Mã người chấm đánh giá không hợp lệ.", "ERR_KPI_SHEET_EVALUATOR_INVALID");

        EmployeeId = employeeId;
        KpiPeriodId = kpiPeriodId;
        Period = period.Trim();
        LeadershipLevel = leadershipLevel;
        OrganizationalUnitId = organizationalUnitId;
        EvaluatorId = evaluatorId;
        JobFamilyId = jobFamilyId;
        JobTitleId = jobTitleId;
        StandardWorkDays = standardWorkDays > 0 ? standardWorkDays : 26m;
        Status = KpiStatus.Draft;
    }

    public void AddItem(KpiItem item)
    {
        if (Status != KpiStatus.Draft && Status != KpiStatus.Assigned)
            throw new DomainException("Không thể thêm chỉ tiêu khi phiếu đã bước vào giai đoạn đánh giá.", "ERR_KPI_SHEET_LOCKED_FOR_EDIT");

        _items.Add(item);
    }

    public void ValidateWeights()
    {
        decimal totalWeight = _items.Sum(i => i.WeightPercentage);
        if (Math.Round(totalWeight, 2) != 100m)
            throw new DomainException($"Tổng trọng số các chỉ tiêu phải bằng 100% (Hiện tại: {totalWeight}%).", "ERR_KPI_TOTAL_WEIGHT_NOT_100");
    }

    public void Publish()
    {
        ValidateWeights();
        Status = KpiStatus.Assigned;
    }

    public void StartProgress()
    {
        if (Status == KpiStatus.Assigned)
        {
            Status = KpiStatus.InProgress;
        }
    }

    public void SubmitSelfReview(DateTime submittedAt, DateOnly deadline)
    {
        if (Status != KpiStatus.InProgress && Status != KpiStatus.Assigned)
            throw new DomainException($"Không thể nộp tự đánh giá khi phiếu đang ở trạng thái {Status}.", "ERR_KPI_SHEET_INVALID_STATE_FOR_SELF_REVIEW");

        SelfReviewSubmittedAt = submittedAt;
        var submittedDateOnly = DateOnly.FromDateTime(submittedAt);

        // Kiểm tra phạt trễ hạn tự đánh giá: trễ hạn trừ 2 điểm tổng
        if (submittedDateOnly > deadline)
        {
            SelfReviewLatePenalty = 2.0m;
        }
        else
        {
            SelfReviewLatePenalty = 0.0m;
        }

        Status = KpiStatus.ManagerReview;
    }

    public void SubmitManagerReview(
        int managerId,
        DateTime submittedAt,
        DateOnly deadline,
        int subordinateCount,
        string? feedback = null)
    {
        if (Status != KpiStatus.ManagerReview)
            throw new DomainException($"Phiếu không ở trạng thái chờ Quản lý chấm (Hiện tại: {Status}).", "ERR_KPI_SHEET_NOT_IN_MANAGER_REVIEW");

        if (managerId != EvaluatorId)
            throw new DomainException("Chỉ người được giao quyền chấm mới có thể thẩm định phiếu này.", "ERR_KPI_NOT_EVALUATOR");

        ManagerReviewSubmittedAt = submittedAt;
        var submittedDateOnly = DateOnly.FromDateTime(submittedAt);

        // Kiểm tra phạt quản lý chấm trễ hạn: -1 điểm/cấp dưới, tối đa -10 điểm
        if (submittedDateOnly > deadline)
        {
            decimal penalty = Math.Min(10m, Math.Max(1m, subordinateCount * 1.0m));
            ManagerReviewLatePenalty = penalty;
        }
        else
        {
            ManagerReviewLatePenalty = 0.0m;
        }

        ManagerFeedback = feedback?.Trim();
        Status = KpiStatus.Calibration;
    }

    public void SetActualWorkDays(decimal actualDays)
    {
        ActualWorkDays = actualDays;
        IsStandardWorkingDaysMet = StandardWorkDays > 0 && (ActualWorkDays / StandardWorkDays) >= 0.5m;
    }

    public void SetSevereViolation(bool hasViolation)
    {
        HasSevereViolation = hasViolation;
    }

    public void SetCalculatedScores(
        decimal unitResultScore,
        decimal individualWorkScore,
        decimal peopleManagementScore,
        decimal complianceScore,
        decimal totalScore,
        KpiGrade grade,
        decimal bonusCoeff)
    {
        UnitResultScore = unitResultScore;
        IndividualWorkScore = individualWorkScore;
        PeopleManagementScore = peopleManagementScore;
        ComplianceScore = complianceScore;
        TotalScore = totalScore;
        FinalGrade = grade;
        BonusCoefficient = bonusCoeff;
    }

    public void ApplyCalibration(decimal newTotalScore, KpiGrade newGrade, decimal newBonusCoeff, string? notes)
    {
        if (Status != KpiStatus.Calibration && Status != KpiStatus.ManagerReview)
            throw new DomainException("Chỉ có thể hiệu chuẩn điểm trong giai đoạn Calibration.", "ERR_KPI_NOT_IN_CALIBRATION");

        TotalScore = newTotalScore;
        FinalGrade = newGrade;
        BonusCoefficient = newBonusCoeff;
        CalibrationNotes = notes?.Trim();
    }

    public KpiResult Lock(string lockedBy)
    {
        if (Status == KpiStatus.Locked)
            throw new DomainException("Phiếu KPI này đã được khóa trước đó.", "ERR_KPI_ALREADY_LOCKED");

        Status = KpiStatus.Locked;

        return new KpiResult(
            EmployeeId,
            Period,
            TotalScore,
            FinalGrade,
            BonusCoefficient,
            UnitResultScore,
            IndividualWorkScore,
            PeopleManagementScore,
            ComplianceScore,
            !IsStandardWorkingDaysMet,
            lockedBy);
    }
}
