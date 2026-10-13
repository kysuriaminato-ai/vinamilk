using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Kpis;

public class KpiDefinitionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public KpiGroup Group { get; set; }
    public string GroupName => Group switch
    {
        KpiGroup.UnitResult => "Kết quả đơn vị",
        KpiGroup.IndividualWork => "Công việc cá nhân",
        KpiGroup.PeopleManagement => "Quản lý con người",
        KpiGroup.Compliance => "Tuân thủ",
        _ => Group.ToString()
    };
    public KpiScoringMethod ScoringMethod { get; set; }
    public string ScoringMethodName => ScoringMethod switch
    {
        KpiScoringMethod.HigherIsBetter => "Càng cao càng tốt",
        KpiScoringMethod.LowerIsBetter => "Càng thấp càng tốt",
        KpiScoringMethod.Binary => "Nhị phân (Đạt/Không đạt)",
        KpiScoringMethod.ThresholdTable => "Tra bảng ngưỡng",
        KpiScoringMethod.Task => "Nhiệm vụ theo hạn",
        _ => ScoringMethod.ToString()
    };
    public KpiDataSource DataSource { get; set; }
    public string DataSourceName => DataSource switch
    {
        KpiDataSource.Manual => "Nhập tay/Báo cáo",
        KpiDataSource.Attendance => "Chấm công tự động",
        KpiDataSource.Production => "Sản lượng sản xuất",
        KpiDataSource.Quality => "Chất lượng KCS",
        KpiDataSource.Sales => "Doanh số bán lẻ",
        _ => DataSource.ToString()
    };
    public int? JobFamilyId { get; set; }
    public string? JobFamilyName { get; set; }
    public LeadershipLevel? ApplicableLeadershipLevel { get; set; }
    public string? Description { get; set; }
    public string? MeasurementGuide { get; set; }
    public bool IsActive { get; set; }
}

public class CreateKpiDefinitionRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public KpiGroup Group { get; set; } = KpiGroup.IndividualWork;
    public KpiScoringMethod ScoringMethod { get; set; } = KpiScoringMethod.HigherIsBetter;
    public KpiDataSource DataSource { get; set; } = KpiDataSource.Manual;
    public int? JobFamilyId { get; set; }
    public LeadershipLevel? ApplicableLeadershipLevel { get; set; }
    public string? Description { get; set; }
    public string? MeasurementGuide { get; set; }
    public List<KpiThresholdDto>? Thresholds { get; set; }
}

public class KpiThresholdDto
{
    public decimal MinThreshold { get; set; }
    public decimal MaxThreshold { get; set; }
    public decimal Score { get; set; }
}

public class KpiSchemeDto
{
    public int Id { get; set; }
    public LeadershipLevel LeadershipLevel { get; set; }
    public string LeadershipLevelName => LeadershipLevel switch
    {
        LeadershipLevel.None => "Nhân viên / Công nhân",
        LeadershipLevel.TeamLeader => "Trưởng nhóm / Tổ trưởng",
        LeadershipLevel.Manager => "Quản lý / Trưởng phòng",
        LeadershipLevel.Director => "Giám đốc đơn vị",
        LeadershipLevel.Executive => "Ban Điều hành",
        _ => LeadershipLevel.ToString()
    };
    public decimal UnitResultWeight { get; set; }
    public decimal IndividualWorkWeight { get; set; }
    public decimal PeopleManagementWeight { get; set; }
    public decimal ComplianceWeight { get; set; }
    public bool IsComplianceGating { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Description { get; set; }
}

public class KpiPeriodDto
{
    public int Id { get; set; }
    public string Period { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public DateOnly AssignStartDate { get; set; }
    public DateOnly AssignDeadline { get; set; }
    public DateOnly MidTermDate { get; set; }
    public DateOnly SelfReviewDeadline { get; set; }
    public DateOnly ManagerReviewDeadline { get; set; }
    public DateOnly CalibrationDeadline { get; set; }
    public KpiPeriodStatus Status { get; set; }
    public string StatusName => Status switch
    {
        KpiPeriodStatus.Planning => "Lập kế hoạch & Giao KPI",
        KpiPeriodStatus.InProgress => "Đang thực hiện",
        KpiPeriodStatus.SelfReview => "Nhân viên tự đánh giá",
        KpiPeriodStatus.ManagerReview => "Quản lý thẩm định",
        KpiPeriodStatus.Calibration => "Hiệu chuẩn đơn vị",
        KpiPeriodStatus.Locked => "Đã khóa kỳ",
        _ => Status.ToString()
    };
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public int TotalSheetsCount { get; set; }
}

public class KpiSheetDetailDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public LeadershipLevel LeadershipLevel { get; set; }
    public int OrganizationalUnitId { get; set; }
    public string OrganizationalUnitName { get; set; } = string.Empty;
    public int EvaluatorId { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;
    public KpiStatus Status { get; set; }
    public string StatusName => Status.ToString();

    public decimal StandardWorkDays { get; set; }
    public decimal ActualWorkDays { get; set; }
    public bool IsStandardWorkingDaysMet { get; set; }

    public decimal SelfReviewLatePenalty { get; set; }
    public decimal ManagerReviewLatePenalty { get; set; }
    public bool HasSevereViolation { get; set; }

    public decimal UnitResultScore { get; set; }
    public decimal IndividualWorkScore { get; set; }
    public decimal PeopleManagementScore { get; set; }
    public decimal ComplianceScore { get; set; }
    public decimal TotalScore { get; set; }
    public KpiGrade FinalGrade { get; set; }
    public string GradeName => FinalGrade.ToString();
    public decimal BonusCoefficient { get; set; }

    public string? ManagerFeedback { get; set; }
    public string? CalibrationNotes { get; set; }

    public List<KpiItemDto> Items { get; set; } = new();
}

public class KpiItemDto
{
    public int Id { get; set; }
    public int KpiSheetId { get; set; }
    public int? KpiDefinitionId { get; set; }
    public string? KpiDefinitionCode { get; set; }
    public KpiGroup Group { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = "%";
    public KpiScoringMethod ScoringMethod { get; set; }
    public decimal TargetValue { get; set; }
    public decimal ActualValue { get; set; }
    public decimal WeightPercentage { get; set; }

    public DateOnly? DueDate { get; set; }
    public DateOnly? ExtendedDueDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? EvidenceDescription { get; set; }

    public decimal? SelfScore { get; set; }
    public decimal? ManagerScore { get; set; }
    public decimal Adjustment { get; set; }
    public string? AdjustmentReason { get; set; }
    public decimal FinalScore { get; set; }

    public bool HasPendingExtension { get; set; }
    public List<KpiExtensionRequestDto> ExtensionRequests { get; set; } = new();
}

public class KpiExtensionRequestDto
{
    public int Id { get; set; }
    public int KpiItemId { get; set; }
    public DateOnly OriginalDueDate { get; set; }
    public DateOnly RequestedDueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool ObjectiveReason { get; set; }
    public KpiExtensionStatus Status { get; set; }
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comment { get; set; }
}

public class CreateKpiSheetRequest
{
    public int EmployeeId { get; set; }
    public string Period { get; set; } = string.Empty;
    public int EvaluatorId { get; set; }
    public List<CreateKpiItemRequest> Items { get; set; } = new();
}

public class CreateKpiItemRequest
{
    public int? KpiDefinitionId { get; set; }
    public KpiGroup Group { get; set; } = KpiGroup.IndividualWork;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = "%";
    public KpiScoringMethod ScoringMethod { get; set; } = KpiScoringMethod.HigherIsBetter;
    public decimal TargetValue { get; set; }
    public decimal WeightPercentage { get; set; }
    public DateOnly? DueDate { get; set; }
}

public class BulkAssignKpiRequest
{
    public int OrganizationalUnitId { get; set; }
    public string Period { get; set; } = string.Empty;
    public int? JobTitleId { get; set; }
    public int? JobFamilyId { get; set; }
    public int? TemplateId { get; set; }
    public List<CreateKpiItemRequest>? CustomItems { get; set; }
}

public class SubmitSelfReviewRequest
{
    public List<SelfReviewItemInput> ItemReviews { get; set; } = new();
}

public class SelfReviewItemInput
{
    public int ItemId { get; set; }
    public decimal ActualValue { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? EvidenceDescription { get; set; }
    public decimal SelfScore { get; set; }
}

public class SubmitManagerReviewRequest
{
    public string? Feedback { get; set; }
    public List<ManagerEvaluationItemInput> ItemEvaluations { get; set; } = new();
}

public class ManagerEvaluationItemInput
{
    public int ItemId { get; set; }
    public decimal ManagerScore { get; set; }
    public decimal Adjustment { get; set; } // [-10, +10]
    public string? AdjustmentReason { get; set; } // Bat buoc neu Adjustment != 0
}

public class RequestKpiExtensionInput
{
    public int ItemId { get; set; }
    public DateOnly RequestedDueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool ObjectiveReason { get; set; }
}

public class ApproveKpiExtensionInput
{
    public int RequestId { get; set; }
    public bool IsApproved { get; set; }
    public string? Comment { get; set; }
}

public class KpiCalibrationSummaryDto
{
    public int OrganizationalUnitId { get; set; }
    public string OrganizationalUnitName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int GradeACount { get; set; }
    public decimal GradeAPercentage { get; set; }
    public int GradeBCount { get; set; }
    public decimal GradeBPercentage { get; set; }
    public int GradeCCount { get; set; }
    public decimal GradeCPercentage { get; set; }
    public int GradeDCount { get; set; }
    public decimal GradeDPercentage { get; set; }
    public int GradeECount { get; set; }
    public decimal GradeEPercentage { get; set; }
    public int UnratedCount { get; set; }

    public bool HasHighAWarning { get; set; } // A > 15%
    public string? HighAWarningMessage { get; set; }
    public bool HasZeroEWarning { get; set; } // E = 0% 3 ky lien tiep
    public string? ZeroEWarningMessage { get; set; }

    public List<KpiSheetDetailDto> Sheets { get; set; } = new();
}

public class ApplyCalibrationInput
{
    public int SheetId { get; set; }
    public decimal CalibratedScore { get; set; }
    public KpiGrade CalibratedGrade { get; set; }
    public decimal CalibratedBonusCoefficient { get; set; }
    public string? CalibrationNotes { get; set; }
}

public class KpiKanbanDto
{
    public string Period { get; set; } = string.Empty;
    public List<KpiKanbanCardDto> Assigned { get; set; } = new();
    public List<KpiKanbanCardDto> InProgress { get; set; } = new();
    public List<KpiKanbanCardDto> SelfReview { get; set; } = new();
    public List<KpiKanbanCardDto> ManagerReview { get; set; } = new();
    public List<KpiKanbanCardDto> Calibrated { get; set; } = new();
    public List<KpiKanbanCardDto> Locked { get; set; } = new();
}

public class KpiKanbanCardDto
{
    public int SheetId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal TotalScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public bool HasPendingExtension { get; set; }
    public bool IsDueSoon { get; set; }
}

public class KpiHeatmapDto
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public List<string> Periods { get; set; } = new();
    public List<KpiHeatmapCellDto> Cells { get; set; } = new();
}

public class KpiHeatmapCellDto
{
    public string Period { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public decimal AverageScore { get; set; }
    public decimal GradeAPercentage { get; set; }
    public decimal BonusPayoutBudget { get; set; }
}

public class KpiTrendDto
{
    public string Period { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public int TotalEvaluated { get; set; }
    public int GradeACount { get; set; }
    public int GradeBCount { get; set; }
    public int GradeCCount { get; set; }
    public int GradeDCount { get; set; }
    public int GradeECount { get; set; }
}

public class PostKpiMetricRequest
{
    public string MetricCode { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public KpiDataSource Source { get; set; } = KpiDataSource.Production;
    public int? OrganizationalUnitId { get; set; }
    public int? EmployeeId { get; set; }
    public string? BatchReference { get; set; }
    public string? Notes { get; set; }
}

public class SubmitKpiAppealRequest
{
    public int SheetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal ProposedScore { get; set; }
}
