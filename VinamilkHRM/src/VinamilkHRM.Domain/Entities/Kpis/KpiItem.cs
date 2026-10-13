using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Kpis;

/// <summary>
/// Chỉ tiêu cụ thể nằm trong phiếu KPI (KpiSheet).
/// </summary>
public class KpiItem : BaseAuditableEntity
{
    public int KpiSheetId { get; private set; }
    public virtual KpiSheet? KpiSheet { get; private set; }

    public int? KpiDefinitionId { get; private set; }
    public virtual KpiDefinition? KpiDefinition { get; private set; }

    public KpiGroup Group { get; private set; } = KpiGroup.IndividualWork;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Unit { get; private set; } = "%";

    public KpiScoringMethod ScoringMethod { get; private set; } = KpiScoringMethod.HigherIsBetter;
    public decimal TargetValue { get; private set; }
    public decimal ActualValue { get; private set; }
    public decimal WeightPercentage { get; private set; } // Trọng số %

    // Quản lý hạn hoàn thành (cho chỉ tiêu dạng Task)
    public DateOnly? DueDate { get; private set; }
    public DateOnly? ExtendedDueDate { get; private set; }
    public DateOnly? CompletedDate { get; private set; }

    public string? EvidenceUrl { get; private set; }
    public string? EvidenceDescription { get; private set; }

    public decimal? SelfScore { get; private set; }
    public decimal? ManagerScore { get; private set; }

    /// <summary>
    /// Điểm điều chỉnh của lãnh đạo khi thẩm định (-10 đến +10).
    /// Bắt buộc phải có AdjustmentReason nếu Adjustment != 0.
    /// </summary>
    public decimal Adjustment { get; private set; } = 0m;
    public string? AdjustmentReason { get; private set; }

    /// <summary>
    /// Điểm cuối cùng của chỉ tiêu (sau khi áp dụng trễ/sớm, điều chỉnh, trần 120, sàn 0).
    /// </summary>
    public decimal FinalScore { get; private set; } = 0m;

    private readonly List<KpiExtensionRequest> _extensionRequests = new();
    public virtual IReadOnlyCollection<KpiExtensionRequest> ExtensionRequests => _extensionRequests.AsReadOnly();

    protected KpiItem() { }

    public KpiItem(
        int kpiSheetId,
        KpiGroup group,
        string title,
        string unit,
        KpiScoringMethod scoringMethod,
        decimal targetValue,
        decimal weightPercentage,
        int? kpiDefinitionId = null,
        string? description = null,
        DateOnly? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Tiêu đề chỉ tiêu không được để trống.", "ERR_KPI_ITEM_TITLE_EMPTY");

        if (weightPercentage <= 0 || weightPercentage > 100)
            throw new DomainException("Trọng số chỉ tiêu phải nằm trong khoảng từ 1% đến 100%.", "ERR_KPI_ITEM_WEIGHT_INVALID");

        KpiSheetId = kpiSheetId;
        KpiDefinitionId = kpiDefinitionId;
        Group = group;
        Title = title.Trim();
        Description = description?.Trim();
        Unit = unit?.Trim() ?? "%";
        ScoringMethod = scoringMethod;
        TargetValue = targetValue;
        WeightPercentage = weightPercentage;
        DueDate = dueDate;
    }

    public void UpdateActualValue(decimal actual, DateOnly? completedDate = null, string? evidenceUrl = null, string? evidenceDesc = null)
    {
        ActualValue = actual;
        CompletedDate = completedDate;
        EvidenceUrl = evidenceUrl?.Trim();
        EvidenceDescription = evidenceDesc?.Trim();
    }

    public void SetSelfScore(decimal score)
    {
        SelfScore = Math.Clamp(score, 0m, 120m);
    }

    public void SetManagerEvaluation(decimal managerScore, decimal adjustment, string? adjustmentReason)
    {
        if (adjustment < -10m || adjustment > 10m)
            throw new DomainException("Điểm điều chỉnh của lãnh đạo phải nằm trong biên độ [-10, +10].", "ERR_KPI_ADJUSTMENT_OUT_OF_RANGE");

        if (adjustment != 0m && string.IsNullOrWhiteSpace(adjustmentReason))
            throw new DomainException("Bắt buộc phải nhập lý do khi điều chỉnh điểm chỉ tiêu.", "ERR_KPI_ADJUSTMENT_REASON_REQUIRED");

        ManagerScore = Math.Clamp(managerScore, 0m, 120m);
        Adjustment = adjustment;
        AdjustmentReason = adjustment != 0m ? adjustmentReason?.Trim() : null;
    }

    public void SetFinalScore(decimal score)
    {
        FinalScore = Math.Clamp(score, 0m, 120m);
    }

    public KpiExtensionRequest RequestExtension(DateOnly requestedDueDate, string reason, bool objectiveReason, DateOnly currentDate)
    {
        if (ScoringMethod != KpiScoringMethod.Task && !DueDate.HasValue)
            throw new DomainException("Chỉ tiêu không có thời hạn DueDate để xin gia hạn.", "ERR_KPI_NOT_TASK_OR_NO_DUEDATE");

        if (!DueDate.HasValue || currentDate > DueDate.Value)
            throw new DomainException("Yêu cầu gia hạn phải được nộp trước khi đến hạn DueDate ban đầu.", "ERR_KPI_EXTENSION_AFTER_DUEDATE");

        if (requestedDueDate <= DueDate.Value)
            throw new DomainException("Hạn mới xin gia hạn phải sau ngày hạn hiện tại.", "ERR_KPI_EXTENSION_DATE_INVALID");

        // Ràng buộc: tối đa 1 lần / chỉ tiêu
        if (_extensionRequests.Any(r => r.Status == KpiExtensionStatus.Approved || r.Status == KpiExtensionStatus.Pending))
            throw new DomainException("Mỗi chỉ tiêu chỉ được phép xin gia hạn tối đa 1 lần.", "ERR_KPI_EXTENSION_MAX_ONCE");

        var request = new KpiExtensionRequest(Id, DueDate.Value, requestedDueDate, reason, objectiveReason);
        _extensionRequests.Add(request);
        return request;
    }

    public void ApplyApprovedExtension(DateOnly newDueDate)
    {
        ExtendedDueDate = newDueDate;
    }
}

/// <summary>
/// Yêu cầu xin gia hạn DueDate chỉ tiêu dạng Task.
/// </summary>
public class KpiExtensionRequest : BaseAuditableEntity
{
    public int KpiItemId { get; private set; }
    public virtual KpiItem? KpiItem { get; private set; }

    public DateOnly OriginalDueDate { get; private set; }
    public DateOnly RequestedDueDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// Cờ lý do khách quan (sự cố thiên tai, gián đoạn chuỗi cung ứng đối tác, yêu cầu đột xuất từ Ban Điều hành...).
    /// </summary>
    public bool ObjectiveReason { get; private set; }

    public KpiExtensionStatus Status { get; private set; } = KpiExtensionStatus.Pending;
    public int? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? Comment { get; private set; }

    protected KpiExtensionRequest() { }

    public KpiExtensionRequest(
        int kpiItemId,
        DateOnly originalDueDate,
        DateOnly requestedDueDate,
        string reason,
        bool objectiveReason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do xin gia hạn không được để trống.", "ERR_KPI_EXTENSION_REASON_EMPTY");

        KpiItemId = kpiItemId;
        OriginalDueDate = originalDueDate;
        RequestedDueDate = requestedDueDate;
        Reason = reason.Trim();
        ObjectiveReason = objectiveReason;
        Status = KpiExtensionStatus.Pending;
    }

    public void Approve(int approvedBy, string? comment)
    {
        if (Status != KpiExtensionStatus.Pending)
            throw new DomainException("Yêu cầu gia hạn này đã được xử lý trước đó.", "ERR_KPI_EXTENSION_ALREADY_PROCESSED");

        Status = KpiExtensionStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        Comment = comment?.Trim();

        KpiItem?.ApplyApprovedExtension(RequestedDueDate);
    }

    public void Reject(int rejectedBy, string reason)
    {
        if (Status != KpiExtensionStatus.Pending)
            throw new DomainException("Yêu cầu gia hạn này đã được xử lý trước đó.", "ERR_KPI_EXTENSION_ALREADY_PROCESSED");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Vui lòng nêu rõ lý do từ chối gia hạn.", "ERR_KPI_EXTENSION_REJECT_REASON_EMPTY");

        Status = KpiExtensionStatus.Rejected;
        ApprovedBy = rejectedBy;
        ApprovedAt = DateTime.UtcNow;
        Comment = reason.Trim();
    }
}
