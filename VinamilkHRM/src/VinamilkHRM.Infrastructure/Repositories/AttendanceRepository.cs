using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Repositories;

public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(VinamilkHRMDbContext context) : base(context)
    {
    }

    public async Task<Attendance?> GetByEmployeeAndDateAsync(int employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        DateTime targetDate = date.Date;
        return await _dbSet
            .Include(a => a.Shift)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == targetDate, cancellationToken);
    }

    public async Task<IReadOnlyList<Attendance>> GetByPeriodAsync(int employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        DateTime start = fromDate.Date;
        DateTime end = toDate.Date;
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.Shift)
            .Where(a => a.EmployeeId == employeeId && a.AttendanceDate >= start && a.AttendanceDate <= end)
            .OrderBy(a => a.AttendanceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Attendance>> GetUnitAttendanceByDateAsync(int unitId, DateTime date, CancellationToken cancellationToken = default)
    {
        DateTime targetDate = date.Date;
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.Employee)
            .Include(a => a.Shift)
            .Where(a => a.Employee != null && a.Employee.OrganizationalUnitId == unitId && a.AttendanceDate == targetDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalWorkHoursAsync(int employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        DateTime start = fromDate.Date;
        DateTime end = toDate.Date;
        return await _dbSet
            .Where(a => a.EmployeeId == employeeId && a.AttendanceDate >= start && a.AttendanceDate <= end)
            .SumAsync(a => a.WorkHours, cancellationToken);
    }

    public async Task<decimal> GetTotalOvertimeHoursAsync(int employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        DateTime start = fromDate.Date;
        DateTime end = toDate.Date;
        return await _dbSet
            .Where(a => a.EmployeeId == employeeId && a.AttendanceDate >= start && a.AttendanceDate <= end)
            .SumAsync(a => a.OvertimeHours, cancellationToken);
    }
}
