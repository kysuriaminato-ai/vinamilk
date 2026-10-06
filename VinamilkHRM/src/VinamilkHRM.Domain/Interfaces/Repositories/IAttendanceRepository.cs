using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Interfaces.Repositories;

/// <summary>
/// Giao diện repository quản lý dữ liệu chấm công hàng ngày và tổng hợp bảng công.
/// </summary>
public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<Attendance?> GetByEmployeeAndDateAsync(int employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attendance>> GetByPeriodAsync(int employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attendance>> GetUnitAttendanceByDateAsync(int unitId, DateTime date, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalWorkHoursAsync(int employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalOvertimeHoursAsync(int employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
