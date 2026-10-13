using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Kpis;

/// <summary>
/// Thực thể giao và đánh giá chỉ tiêu hiệu suất công việc (KPI) theo SLA.
/// Cài đặt Động cơ SLA tự động: Sớm hạn (+2đ/ngày, max +10đ), Trễ hạn (-5đ/ngày, sàn 0đ),
/// và quy trình thẩm định, điều chỉnh điểm số của Lãnh đạo (Leader Approval).
/// </summary>
public class KpiAssignment : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public int EvaluatorId { get; private set; }
    public virtual Employee? Evaluator { get; private set; }

    public string Period { get; private set; } = string.Empty; // vd: "2025-Q1", "2025-M01"
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string TargetMetric { get; private set; } = string.Empty;
    public decimal WeightPercentage { get; private set; } // Tỷ trọng điểm % (Tổng các KPI = 100%)

    // Động cơ SLA (Service Level Agreement)
    public DateTime DueDate { get; private set; }
    public decimal BaseScore { get; private set; } = 100.0m; // Điểm cơ bản giao việc ban đầu
    public decimal SlaBonusPenaltyScore { get; private set; } = 0.0m; // Điểm thưởng/phạt theo tiến độ thời gian SLA
    public decimal ScoreAdjustment { get; private set; } = 0.0m; // Điểm lãnh đạo điều chỉnh khi thẩm định
    public string? SlaCalculationDetail { get; private set; }
    public DateTime? CompletedDate { get; private set; }

    public string? ActualResult { get; private set; }
    public decimal? SelfEvaluationScore { get; private set; }
    public decimal? FinalScore { get; private set; }
    public KpiStatus Status { get; private set; } = KpiStatus.Assigned;
    public string? Feedback { get; private set; }
    public DateTime? SubmissionDate { get; private set; }
    public DateTime? EvaluationDate { get; private set; }

    protected KpiAssignment()
    {
    }

    public KpiAssignment(
        int employeeId,
        int evaluatorId,
        string period,
        string title,
        string description,
        string targetMetric,
        decimal weightPercentage,
        DateTime dueDate,
        decimal baseScore = 100.0m)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân sự được giao KPI không hợp lệ.", "ERR_KPI_EMP_INVALID");

        if (evaluatorId <= 0)
            throw new DomainException("Mã cấp quản lý đánh giá KPI không hợp lệ.", "ERR_KPI_EVALUATOR_INVALID");

        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("Kỳ đánh giá KPI không được để trống.", "ERR_KPI_PERIOD_EMPTY");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Tiêu đề mục tiêu KPI không được để trống.", "ERR_KPI_TITLE_EMPTY");

        if (weightPercentage <= 0 || weightPercentage > 100)
            throw new DomainException("Tỷ trọng chỉ tiêu phải nằm trong khoảng từ 1% đến 100%.", "ERR_KPI_WEIGHT_INVALID");

        EmployeeId = employeeId;
        EvaluatorId = evaluatorId;
        Period = period.Trim().ToUpperInvariant();
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        TargetMetric = targetMetric?.Trim() ?? string.Empty;
        WeightPercentage = weightPercentage;
        DueDate = dueDate;
        BaseScore = baseScore > 0 ? baseScore : 100.0m;
        Status = KpiStatus.Assigned;
    }

    /// <summary>
    /// Chuyển trạng thái sang Đang thực hiện.
    /// </summary>
    public void StartProgress()
    {
        if (Status == KpiStatus.Assigned)
        {
            Status = KpiStatus.InProgress;
        }
    }

    /// <summary>
    /// Nhân sự nộp báo cáo kết quả hoàn thành công việc.
    /// Kích hoạt Động cơ tự động tính toán SLA (Bonus/Penalty):
    /// - Hoàn thành sớm hơn DueDate: +2 điểm/ngày (tối đa +10 điểm).
    /// - Hoàn thành trễ hơn DueDate: -5 điểm/ngày (sàn = 0).
    /// Trạng thái chuyển thành PendingApproval chờ Lãnh đạo thẩm định.
    /// </summary>
    public void SubmitTask(string resultSummary, DateTime completedDate)
    {
        if (Status != KpiStatus.Assigned && Status != KpiStatus.InProgress && Status != KpiStatus.Rejected)
            throw new DomainException($"Không thể nộp báo cáo khi chỉ tiêu đang ở trạng thái '{Status}'.", "ERR_KPI_INVALID_STATE_FOR_SUBMIT");

        if (string.IsNullOrWhiteSpace(resultSummary))
            throw new DomainException("Báo cáo kết quả thực hiện không được để trống.", "ERR_KPI_RESULT_EMPTY");

        ActualResult = resultSummary.Trim();
        CompletedDate = completedDate;
        SubmissionDate = DateTime.UtcNow;

        // Tính toán độ lệch thời gian so với DueDate
        DateTime dueDateOnly = DueDate.Date;
        DateTime completedDateOnly = completedDate.Date;
        int diffDays = (dueDateOnly - completedDateOnly).Days;

        if (diffDays > 0)
        {
            // Sớm hạn: +2 điểm mỗi ngày, tối đa +10 điểm
            decimal bonus = Math.Min(10.0m, diffDays * 2.0m);
            SlaBonusPenaltyScore = bonus;
            SlaCalculationDetail = $"Hoàn thành sớm {diffDays} ngày trước hạn (+{bonus} điểm SLA thưởng)";
        }
        else if (diffDays < 0)
        {
            // Trễ hạn: -5 điểm mỗi ngày trễ
            int lateDays = Math.Abs(diffDays);
            decimal penalty = lateDays * 5.0m;
            SlaBonusPenaltyScore = -penalty;
            SlaCalculationDetail = $"Hoàn thành trễ hạn {lateDays} ngày (-{penalty} điểm SLA phạt)";
        }
        else
        {
            // Đúng hạn ngày DueDate
            SlaBonusPenaltyScore = 0.0m;
            SlaCalculationDetail = "Hoàn thành đúng ngày hạn định SLA (0 điểm thưởng/phạt)";
        }

        // Điểm tạm tính tự động theo SLA
        decimal preliminaryScore = Math.Max(0.0m, BaseScore + SlaBonusPenaltyScore);
        SelfEvaluationScore = preliminaryScore;

        // Chuyển sang trạng thái chờ Lãnh đạo duyệt (Submitted)
        Status = KpiStatus.Submitted;
    }

    /// <summary>
    /// Lãnh đạo thẩm định, áp dụng điều chỉnh điểm số (scoreAdjustment) và phê duyệt chính thức.
    /// Chỉ khi phương thức này được gọi, FinalScore mới được chốt để chuyển sang phân hệ Tính lương.
    /// </summary>
    public void LeaderApproveKpi(int leaderId, decimal scoreAdjustment, string feedback)
    {
        if (Status != KpiStatus.Submitted)
            throw new DomainException("Chỉ tiêu chưa được nhân viên nộp kết quả hoặc đã được chốt trước đó.", "ERR_KPI_NOT_PENDING_APPROVAL");

        if (leaderId <= 0)
            throw new DomainException("Mã người phê duyệt không hợp lệ.", "ERR_KPI_APPROVER_INVALID");

        ScoreAdjustment = scoreAdjustment;

        // Tính FinalScore = Clamp(BaseScore + SlaBonusPenalty + ScoreAdjustment, 0, 120)
        decimal calculatedScore = BaseScore + SlaBonusPenaltyScore + scoreAdjustment;
        FinalScore = Math.Clamp(calculatedScore, 0.0m, 120.0m);

        Feedback = string.IsNullOrWhiteSpace(feedback) 
            ? $"Lãnh đạo đã duyệt điểm. Điều chỉnh: {scoreAdjustment:+#;-#;0} điểm." 
            : feedback.Trim();

        EvaluationDate = DateTime.UtcNow;
        Status = KpiStatus.Approved;
    }

    /// <summary>
    /// Lãnh đạo từ chối và yêu cầu làm lại.
    /// </summary>
    public void RejectByLeader(int leaderId, string rejectionReason)
    {
        if (Status != KpiStatus.Submitted)
            throw new DomainException("Chỉ tiêu hiện không ở trạng thái chờ duyệt.", "ERR_KPI_NOT_SUBMITTED");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new DomainException("Phải nêu rõ lý do trả về hoặc yêu cầu bổ sung số liệu.", "ERR_KPI_REJECTION_REASON_EMPTY");

        Feedback = rejectionReason.Trim();
        EvaluationDate = DateTime.UtcNow;
        Status = KpiStatus.Rejected;
    }
}
