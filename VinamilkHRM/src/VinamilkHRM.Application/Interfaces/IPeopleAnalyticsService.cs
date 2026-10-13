using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Application.DTOs.Analytics;

namespace VinamilkHRM.Application.Interfaces;

public interface IPeopleAnalyticsService
{
    Task<IReadOnlyList<WorkforceSnapshotDto>> GetSnapshotsAsync(string? month, int? unitId, CancellationToken cancellationToken = default);
    Task<WorkforceSnapshotDto> CaptureSnapshotAsync(string month, int unitId, CancellationToken cancellationToken = default);

    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(CancellationToken cancellationToken = default);
    Task<UnitDashboardDto> GetUnitDashboardAsync(int unitId, CancellationToken cancellationToken = default);
    Task<MarketDashboardDto> GetMarketDashboardAsync(CancellationToken cancellationToken = default);

    Task<AttritionRiskReportDto> GetUnitAttritionRisksAsync(int unitId, CancellationToken cancellationToken = default);
    Task<PayStanceReportDto> GetPayStanceReportAsync(int? jobFamilyId, int? regionId, CancellationToken cancellationToken = default);
    Task<ForecastReportDto> GetSixMonthForecastAsync(CancellationToken cancellationToken = default);

    Task SubmitExitSurveyAsync(SubmitExitSurveyRequest request, CancellationToken cancellationToken = default);
    Task ImportMarketBenchmarksAsync(IReadOnlyList<ImportBenchmarkItemRequest> benchmarks, CancellationToken cancellationToken = default);

    Task<AiSummaryResponseDto> GenerateAiMonthlySummaryAsync(AiSummaryRequest request, int userId, CancellationToken cancellationToken = default);
}