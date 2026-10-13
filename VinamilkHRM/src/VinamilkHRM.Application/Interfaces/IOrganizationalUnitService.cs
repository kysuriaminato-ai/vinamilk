using VinamilkHRM.Application.DTOs.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Interfaces;

public interface IOrganizationalUnitService
{
    Task<List<OrganizationalUnitTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<OrganizationalUnitDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task MoveUnitAsync(int unitId, int? newParentUnitId, CancellationToken cancellationToken = default);
    Task AssignManagerAsync(int unitId, AssignManagerRequest request, CancellationToken cancellationToken = default);
    Task ChangeWageRegionAsync(int unitId, ChangeWageRegionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitManagerHistoryDto>> GetManagerHistoriesAsync(int unitId, CancellationToken cancellationToken = default);
}
