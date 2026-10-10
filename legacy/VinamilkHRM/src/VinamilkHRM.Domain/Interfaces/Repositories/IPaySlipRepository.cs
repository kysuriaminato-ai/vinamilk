using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Interfaces.Repositories;

/// <summary>
/// Giao diện repository quản lý bảng lương và phiếu lương cán bộ nhân viên.
/// </summary>
public interface IPaySlipRepository : IRepository<PaySlip>
{
    Task<PaySlip?> GetByEmployeeAndPeriodAsync(int employeeId, string payrollPeriod, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaySlip>> GetByPeriodAsync(string payrollPeriod, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaySlip>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaySlip>> GetByStatusAsync(PaySlipStatus status, string payrollPeriod, CancellationToken cancellationToken = default);
}
