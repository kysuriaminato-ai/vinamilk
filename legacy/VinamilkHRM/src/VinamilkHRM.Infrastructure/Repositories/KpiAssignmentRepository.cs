using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Repositories;

public class KpiAssignmentRepository : Repository<KpiAssignment>, IKpiAssignmentRepository
{
    public KpiAssignmentRepository(VinamilkHRMDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<KpiAssignment>> GetByEmployeeAndPeriodAsync(int employeeId, string period, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(k => k.EmployeeId == employeeId && k.Period == period)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KpiAssignment>> GetPendingApprovalsByEvaluatorAsync(int evaluatorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(k => k.Employee)
            .Where(k => k.EvaluatorId == evaluatorId && k.Status == KpiStatus.Submitted)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> CalculateAverageKpiScoreAsync(int employeeId, string period, CancellationToken cancellationToken = default)
    {
        var items = await _dbSet
            .Where(k => k.EmployeeId == employeeId && k.Period == period && k.FinalScore.HasValue)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
            return 0m;

        decimal weightedSum = items.Sum(k => (k.FinalScore ?? 0m) * (k.WeightPercentage / 100m));
        decimal totalWeight = items.Sum(k => k.WeightPercentage);

        if (totalWeight == 0)
            return 0m;

        return Math.Round(weightedSum / (totalWeight / 100m), 2);
    }

    public async Task<IReadOnlyList<KpiAssignment>> GetByStatusAsync(KpiStatus status, string period, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(k => k.Employee)
            .Where(k => k.Status == status && k.Period == period)
            .ToListAsync(cancellationToken);
    }
}
