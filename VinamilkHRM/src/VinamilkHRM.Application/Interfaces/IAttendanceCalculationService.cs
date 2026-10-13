using System;
using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Domain.Entities.Attendances;

namespace VinamilkHRM.Application.Interfaces;

public interface IAttendanceCalculationService
{
    Task<DailyAttendance> CalculateDailyAttendanceAsync(
        int employeeId,
        DateOnly workDate,
        CancellationToken cancellationToken = default);

    Task<int> CalculateDailyAttendanceForUnitAsync(
        int organizationalUnitId,
        DateOnly workDate,
        CancellationToken cancellationToken = default);

    Task<MonthlyTimesheetSummaryDto> CalculateMonthlySummaryForEmployeeAsync(
        int employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<int> CalculateMonthlySummaryForUnitAsync(
        int organizationalUnitId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
