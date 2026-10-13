using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Application.DTOs.Salaries;

namespace VinamilkHRM.Application.Interfaces;

public interface ISalaryReviewService
{
    // Quản lý Khung lương (Salary Bands)
    Task<IReadOnlyList<SalaryBandDto>> GetSalaryBandsAsync(CancellationToken cancellationToken = default);
    Task<SalaryBandDto> CreateSalaryBandAsync(CreateSalaryBandRequest request, CancellationToken cancellationToken = default);

    // Chu kỳ xét tăng lương (Salary Review Cycle)
    Task<SalaryReviewCycleDto> CreateCycleAsync(CreateSalaryReviewCycleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalaryReviewCycleDto>> GetCyclesAsync(CancellationToken cancellationToken = default);
    Task<SalaryReviewCycleDto> OpenCycleAsync(int cycleId, decimal defaultBudgetRate = 0.08m, CancellationToken cancellationToken = default);
    Task UpdateBudgetAsync(UpdateSalaryBudgetRequest request, int ceoId, CancellationToken cancellationToken = default);

    // Rà soát & Đề xuất tại Đơn vị (Unit Merit Review)
    Task<IReadOnlyList<UnitMeritSuggestionDto>> GetUnitSuggestionsAsync(int cycleId, int unitId, CancellationToken cancellationToken = default);
    Task SubmitUnitReviewAsync(UnitMeritSubmissionRequest request, int managerId, CancellationToken cancellationToken = default);

    // Đặc cách (Special Adjustment)
    Task<SalaryAdjustmentDetailDto> CreateSpecialAdjustmentAsync(SpecialSalaryAdjustmentRequest request, int proposerId, CancellationToken cancellationToken = default);

    // Quy trình duyệt (Approval Workflow)
    Task ApproveStepAsync(ApproveAdjustmentStepRequest request, CancellationToken cancellationToken = default);
    Task RejectProposalAsync(int proposalId, int reviewerId, string reason, CancellationToken cancellationToken = default);
    Task<SalaryReviewCycleDto> ApproveExecutiveCycleAsync(int cycleId, int ceoId, CancellationToken cancellationToken = default);

    // Báo cáo Total Rewards
    Task<TotalRewardsReportDto> GetTotalRewardsReportAsync(int cycleId, CancellationToken cancellationToken = default);
}