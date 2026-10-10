using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Repositories;

public class PaySlipRepository : Repository<PaySlip>, IPaySlipRepository
{
    public PaySlipRepository(VinamilkHRMDbContext context) : base(context)
    {
    }

    public async Task<PaySlip?> GetByEmployeeAndPeriodAsync(int employeeId, string payrollPeriod, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.PayrollPeriod == payrollPeriod, cancellationToken);
    }

    public async Task<IReadOnlyList<PaySlip>> GetByPeriodAsync(string payrollPeriod, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.PayrollPeriod == payrollPeriod)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaySlip>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.PayrollPeriod)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaySlip>> GetByStatusAsync(PaySlipStatus status, string payrollPeriod, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.Status == status && p.PayrollPeriod == payrollPeriod)
            .ToListAsync(cancellationToken);
    }
}
