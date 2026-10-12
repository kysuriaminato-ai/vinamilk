using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Repositories;

public class LaborContractRepository : Repository<LaborContract>, ILaborContractRepository
{
    public LaborContractRepository(VinamilkHRMDbContext context) : base(context)
    {
    }

    public async Task<LaborContract?> GetByContractNumberAsync(string contractNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.ContractNumber == contractNumber, cancellationToken);
    }

    public async Task<LaborContract?> GetActiveContractByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.EmployeeId == employeeId && c.Status == ContractStatus.Active, cancellationToken);
    }

    public async Task<IReadOnlyList<LaborContract>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LaborContract>> GetExpiringContractsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.Employee)
            .Where(c => c.Status == ContractStatus.Active && c.EndDate.HasValue && c.EndDate.Value <= beforeDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LaborContract>> GetByContractTypeAsync(ContractType contractType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.ContractType == contractType)
            .ToListAsync(cancellationToken);
    }
}
