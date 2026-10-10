using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Repositories;

public class OrganizationalUnitRepository : Repository<OrganizationalUnit>, IOrganizationalUnitRepository
{
    public OrganizationalUnitRepository(VinamilkHRMDbContext context) : base(context)
    {
    }

    public async Task<OrganizationalUnit?> GetByCodeAsync(string unitCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.ManagerEmployee)
            .Include(u => u.ParentUnit)
            .FirstOrDefaultAsync(u => u.UnitCode == unitCode, cancellationToken);
    }

    public async Task<OrganizationalUnit?> GetWithChildrenAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.ChildUnits)
            .Include(u => u.Employees)
            .Include(u => u.ManagerEmployee)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationalUnit>> GetRootUnitsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.ParentUnitId == null)
            .Include(u => u.ChildUnits)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationalUnit>> GetByUnitTypeAsync(UnitType unitType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.UnitType == unitType)
            .ToListAsync(cancellationToken);
    }
}
