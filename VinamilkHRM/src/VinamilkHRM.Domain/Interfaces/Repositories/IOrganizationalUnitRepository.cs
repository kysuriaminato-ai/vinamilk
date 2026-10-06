using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Interfaces.Repositories;

/// <summary>
/// Giao diện repository quản lý cây cơ cấu tổ chức tập đoàn Vinamilk.
/// </summary>
public interface IOrganizationalUnitRepository : IRepository<OrganizationalUnit>
{
    Task<OrganizationalUnit?> GetByCodeAsync(string unitCode, CancellationToken cancellationToken = default);
    Task<OrganizationalUnit?> GetWithChildrenAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationalUnit>> GetRootUnitsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationalUnit>> GetByUnitTypeAsync(UnitType unitType, CancellationToken cancellationToken = default);
}
