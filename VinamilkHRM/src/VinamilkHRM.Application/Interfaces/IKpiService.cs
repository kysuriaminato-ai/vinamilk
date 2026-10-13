using VinamilkHRM.Application.DTOs.Kpis;

namespace VinamilkHRM.Application.Interfaces;

public interface IKpiService
{
    // Thư viện chỉ tiêu & Khung tỷ trọng
    Task<List<KpiDefinitionDto>> GetDefinitionsAsync(int? jobFamilyId = null, CancellationToken ct = default);
    Task<KpiDefinitionDto> GetDefinitionByIdAsync(int id, CancellationToken ct = default);
    Task<KpiDefinitionDto> CreateDefinitionAsync(CreateKpiDefinitionRequest request, CancellationToken ct = default);

    Task<List<KpiSchemeDto>> GetSchemesAsync(CancellationToken ct = default);

    // Kỳ đánh giá
    Task<List<KpiPeriodDto>> GetPeriodsAsync(CancellationToken ct = default);
    Task<KpiPeriodDto> GetPeriodByCodeAsync(string period, CancellationToken ct = default);
    Task<KpiPeriodDto> CreatePeriodAsync(int year, int month, CancellationToken ct = default);
    Task LockPeriodAsync(string period, string lockedBy, CancellationToken ct = default);

    // Phiếu KPI & Chỉ tiêu
    Task<KpiSheetDetailDto> GetSheetByIdAsync(int sheetId, CancellationToken ct = default);
    Task<KpiSheetDetailDto?> GetEmployeeSheetInPeriodAsync(int employeeId, string period, CancellationToken ct = default);
    Task<List<KpiSheetDetailDto>> GetSheetsByUnitAsync(int unitId, string period, CancellationToken ct = default);
    Task<KpiSheetDetailDto> CreateSheetAsync(CreateKpiSheetRequest request, CancellationToken ct = default);
    Task PublishSheetAsync(int sheetId, CancellationToken ct = default);
    Task<int> BulkAssignKpiAsync(BulkAssignKpiRequest request, CancellationToken ct = default);

    // Đánh giá
    Task SubmitSelfReviewAsync(int sheetId, SubmitSelfReviewRequest request, CancellationToken ct = default);
    Task SubmitManagerReviewAsync(int sheetId, int managerId, SubmitManagerReviewRequest request, CancellationToken ct = default);

    // Gia hạn chỉ tiêu Task
    Task<KpiExtensionRequestDto> RequestExtensionAsync(RequestKpiExtensionInput input, CancellationToken ct = default);
    Task ApproveExtensionAsync(ApproveKpiExtensionInput input, int approverId, CancellationToken ct = default);

    // Khiếu nại
    Task SubmitAppealAsync(SubmitKpiAppealRequest request, int employeeId, CancellationToken ct = default);

    // Báo cáo & Trực quan hóa
    Task<KpiKanbanDto> GetKanbanBoardAsync(string period, int? unitId = null, CancellationToken ct = default);
    Task<KpiHeatmapDto> GetUnitHeatmapAsync(int unitId, int months = 6, CancellationToken ct = default);
    Task<List<KpiTrendDto>> Get12MonthTrendAsync(int? unitId = null, CancellationToken ct = default);

    // Tương thích ngược KpiAssignment cũ (Phiên 2 / WebUI)
    Task<KpiKanbanBoardDto> GetLegacyKanbanBoardAsync(string period, int? unitId = null, CancellationToken ct = default);
    Task<KpiAssignmentDto> CreateKpiAsync(CreateKpiCommand command, CancellationToken ct = default);
    Task StartKpiAsync(int kpiId, CancellationToken ct = default);
    Task<KpiAssignmentDto> SubmitKpiResultAsync(SubmitKpiCommand command, CancellationToken ct = default);
    Task<KpiAssignmentDto> LeaderApproveKpiAsync(LeaderApproveKpiCommand command, CancellationToken ct = default);
    Task RejectKpiAsync(int kpiId, int leaderId, string reason, CancellationToken ct = default);
    Task<KpiAssignmentDto?> GetKpiByIdAsync(int id, CancellationToken ct = default);
}

public interface IKpiCalibrationService
{
    Task<KpiCalibrationSummaryDto> GetCalibrationSummaryAsync(int unitId, string period, CancellationToken ct = default);
    Task ApplyCalibrationAsync(List<ApplyCalibrationInput> inputs, string reviewer, CancellationToken ct = default);
}

public interface IKpiMetricService
{
    Task RecordMetricAsync(PostKpiMetricRequest request, CancellationToken ct = default);
    Task SyncAttendanceMetricsAsync(string period, int? unitId = null, CancellationToken ct = default);
    Task ImportMetricsFromExcelAsync(Stream fileStream, string period, CancellationToken ct = default);
}

public interface IKpiBackgroundJobService
{
    Task RunKpiRemindersJobAsync();
    Task ApplyOverduePenaltiesJobAsync();
}
