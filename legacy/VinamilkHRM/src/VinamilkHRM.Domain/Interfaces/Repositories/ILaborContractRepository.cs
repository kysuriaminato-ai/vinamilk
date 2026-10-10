using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Interfaces.Repositories;

/// <summary>
/// Giao diện repository quản lý hồ sơ Hợp đồng lao động.
/// </summary>
public interface ILaborContractRepository : IRepository<LaborContract>
{
    Task<LaborContract?> GetByContractNumberAsync(string contractNumber, CancellationToken cancellationToken = default);
    Task<LaborContract?> GetActiveContractByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LaborContract>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LaborContract>> GetExpiringContractsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LaborContract>> GetByContractTypeAsync(ContractType contractType, CancellationToken cancellationToken = default);
}
