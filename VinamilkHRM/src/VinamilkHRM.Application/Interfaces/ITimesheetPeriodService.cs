using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Application.DTOs.Attendances;

namespace VinamilkHRM.Application.Interfaces;

public interface ITimesheetPeriodService
{
    Task<bool> ConfirmByLeaderAsync(string period, int organizationalUnitId, int leaderId, CancellationToken cancellationToken = default);
    Task<bool> ApproveByManagerAsync(string period, int organizationalUnitId, int managerId, CancellationToken cancellationToken = default);
    Task<bool> LockPeriodAsync(string period, int organizationalUnitId, int hrUserId, CancellationToken cancellationToken = default);
    Task<int> CreateAdjustmentAsync(CreateAdjustmentRequest request, string createdBy, CancellationToken cancellationToken = default);
    Task<TodayDashboardDto> GetTodayDashboardAsync(int? organizationalUnitId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyTimesheetSummaryDto>> GetMonthlySummariesAsync(string period, int organizationalUnitId, CancellationToken cancellationToken = default);
}
