using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Interfaces.Repositories;

/// <summary>
/// Giao diện repository quản lý giao chỉ tiêu và đánh giá KPI nhân sự.
/// </summary>
public interface IKpiAssignmentRepository : IRepository<KpiAssignment>
{
    Task<IReadOnlyList<KpiAssignment>> GetByEmployeeAndPeriodAsync(int employeeId, string period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KpiAssignment>> GetPendingApprovalsByEvaluatorAsync(int evaluatorId, CancellationToken cancellationToken = default);
    Task<decimal> CalculateAverageKpiScoreAsync(int employeeId, string period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KpiAssignment>> GetByStatusAsync(KpiStatus status, string period, CancellationToken cancellationToken = default);
}
