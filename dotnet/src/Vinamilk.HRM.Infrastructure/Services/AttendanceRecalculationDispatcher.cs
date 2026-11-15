using Hangfire;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Infrastructure.Services;

public sealed class HangfireAttendanceRecalculationDispatcher : IAttendanceRecalculationDispatcher
{
    private readonly IApplicationDbContext _context;

    public HangfireAttendanceRecalculationDispatcher(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> EnqueueOrRunAsync(
        int month,
        int year,
        int? organizationUnitId,
        int? employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        string requestedByUsername,
        CancellationToken cancellationToken = default)
    {
        var batch = new AttendanceRecalculationBatch
        {
            BatchCode = $"RECALC-{year}{month:D2}-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            Month = month,
            Year = year,
            OrganizationUnitId = organizationUnitId,
            EmployeeId = employeeId,
            FromDate = fromDate,
            ToDate = toDate,
            Status = "Queued",
            RequestedByUsername = requestedByUsername
        };

        await _context.AttendanceRecalculationBatches.AddAsync(batch, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        BackgroundJob.Enqueue<IAttendanceRecalculationJob>(job => job.ExecuteBatchAsync(batch.Id, CancellationToken.None));
        return batch.Id;
    }
}
