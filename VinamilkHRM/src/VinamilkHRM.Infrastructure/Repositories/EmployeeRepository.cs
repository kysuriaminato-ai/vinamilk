using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(VinamilkHRMDbContext context) : base(context)
    {
    }

    public async Task<Employee?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.OrganizationalUnit)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode, cancellationToken);
    }

    public async Task<Employee?> GetByCitizenIdAsync(string citizenId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.CitizenId == citizenId, cancellationToken);
    }

    public async Task<Employee?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.OrganizationalUnit)
            .Include(e => e.Manager)
            .Include(e => e.LaborContracts)
            .Include(e => e.AssignedAssets)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetByUnitIdAsync(int unitId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.OrganizationalUnitId == unitId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetByTypeAsync(EmployeeType employeeType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.EmployeeType == employeeType)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCodeUniqueAsync(string employeeCode, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return !await _dbSet.AnyAsync(e => e.EmployeeCode == employeeCode && (!excludeId.HasValue || e.Id != excludeId.Value), cancellationToken);
    }

    public async Task<bool> IsCitizenIdUniqueAsync(string citizenId, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return !await _dbSet.AnyAsync(e => e.CitizenId == citizenId && (!excludeId.HasValue || e.Id != excludeId.Value), cancellationToken);
    }
}
