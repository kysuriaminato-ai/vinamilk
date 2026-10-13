using VinamilkHRM.Application.DTOs.Analytics;

namespace VinamilkHRM.Application.Interfaces;

/// <summary>
/// Dịch vụ phân tích quản trị cấp chiến lược (Strategic MIS HR Dashboard).
/// Cung cấp dự báo biến động nhân sự, phân tích cơ cấu quỹ lương, mô phỏng chính sách tiền lương và AI HR Advisor.
/// </summary>
public interface IStrategicDashboardService
{
    Task<StrategicDashboardDataDto> GetDashboardDataAsync(CancellationToken cancellationToken = default);

    Task<MinimumWageSimulationResultDto> SimulateWagePolicyImpactAsync(
        MinimumWageSimulationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiHrRecommendationDto>> GetAiHrRecommendationsAsync(CancellationToken cancellationToken = default);
}
