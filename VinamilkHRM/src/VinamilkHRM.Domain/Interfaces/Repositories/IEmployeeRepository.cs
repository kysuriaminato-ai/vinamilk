using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Interfaces.Repositories;

/// <summary>
/// Giao diện repository chuyên biệt cho thực thể Cán bộ / Nhân viên Vinamilk.
/// </summary>
public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<Employee?> GetByCitizenIdAsync(string citizenId, CancellationToken cancellationToken = default);
    Task<Employee?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetByUnitIdAsync(int unitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetByTypeAsync(EmployeeType employeeType, CancellationToken cancellationToken = default);
    Task<bool> IsCodeUniqueAsync(string employeeCode, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsCitizenIdUniqueAsync(string citizenId, int? excludeId = null, CancellationToken cancellationToken = default);
}
