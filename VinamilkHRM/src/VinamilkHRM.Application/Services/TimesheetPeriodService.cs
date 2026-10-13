using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public class TimesheetPeriodService : ITimesheetPeriodService
{
    private readonly IApplicationDbContext _context;

    public TimesheetPeriodService(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> ConfirmByLeaderAsync(string period, int organizationalUnitId, int leaderId, CancellationToken cancellationToken = default)
    {
        var timesheetPeriod = await GetOrCreatePeriodAsync(period, organizationalUnitId, cancellationToken);
        timesheetPeriod.ConfirmByLeader(leaderId);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ApproveByManagerAsync(string period, int organizationalUnitId, int managerId, CancellationToken cancellationToken = default)
    {
        var timesheetPeriod = await GetOrCreatePeriodAsync(period, organizationalUnitId, cancellationToken);
        timesheetPeriod.ApproveByManager(managerId);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> LockPeriodAsync(string period, int organizationalUnitId, int hrUserId, CancellationToken cancellationToken = default)
    {
        var timesheetPeriod = await GetOrCreatePeriodAsync(period, organizationalUnitId, cancellationToken);
        timesheetPeriod.Lock(hrUserId);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> CreateAdjustmentAsync(CreateAdjustmentRequest request, string createdBy, CancellationToken cancellationToken = default)
    {
        // Kiểm tra kỳ gốc phải là kỳ ĐÃ KHÓA
        var emp = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={request.EmployeeId}.", "ERR_EMP_NOT_FOUND");

        var originalPeriod = await _context.MonthlyTimesheetPeriods
            .FirstOrDefaultAsync(p => p.Period == request.OriginalPeriod && p.OrganizationalUnitId == emp.OrganizationalUnitId, cancellationToken);

        if (originalPeriod == null || originalPeriod.Status != TimesheetPeriodStatus.Locked)
        {
            throw new DomainException($"Kỳ công gốc {request.OriginalPeriod} chưa bị khóa, vui lòng chỉnh sửa trực tiếp trên kỳ hiện tại.", "ERR_ORIGINAL_PERIOD_NOT_LOCKED");
        }

        var targetPeriod = await GetOrCreatePeriodAsync(request.TargetPeriod, emp.OrganizationalUnitId, cancellationToken);
        targetPeriod.EnsureNotLocked();

        var adjustment = new AttendanceAdjustment(
            request.OriginalPeriod,
            request.TargetPeriod,
            request.EmployeeId,
            request.AdjustedWorkDays,
            request.AdjustedOtHours,
            request.AdjustedNightHours,
            request.Reason);

        _context.AttendanceAdjustments.Add(adjustment);
        await _context.SaveChangesAsync(cancellationToken);

        return adjustment.Id;
    }

    public async Task<TodayDashboardDto> GetTodayDashboardAsync(int? organizationalUnitId = null, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)); // Giờ Việt Nam

        var employeesQuery = _context.Employees.Where(e => e.IsActive);
        if (organizationalUnitId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e => e.OrganizationalUnitId == organizationalUnitId.Value);
        }

        int totalHeadcount = await employeesQuery.CountAsync(cancellationToken);

        var dailyQuery = _context.DailyAttendances
            .Include(d => d.Employee)
            .Where(d => d.AttendanceDate == today);

        if (organizationalUnitId.HasValue)
        {
            dailyQuery = dailyQuery.Where(d => d.Employee!.OrganizationalUnitId == organizationalUnitId.Value);
        }

        var records = await dailyQuery.ToListAsync(cancellationToken);

        int presentCount = records.Count(r => r.AttendanceCode == "X" || r.AttendanceCode == "X/2");
        int lateCount = records.Count(r => r.LateMinutes > 0);
        int onLeaveCount = records.Count(r => r.AttendanceCode == "P" || r.AttendanceCode == "Ô" || r.AttendanceCode == "TS" || r.AttendanceCode == "NB");
        int absentCount = records.Count(r => r.AttendanceCode == "KP");
        int overtimeCount = records.Count(r => (r.RegularOtHours + r.RestDayOtHours + r.HolidayOtHours + r.NightOtHours) > 0);

        decimal rate = totalHeadcount > 0 ? Math.Round((decimal)presentCount / totalHeadcount * 100m, 1) : 0m;

        return new TodayDashboardDto(
            today,
            totalHeadcount,
            presentCount,
            lateCount,
            absentCount,
            onLeaveCount,
            overtimeCount,
            rate);
    }

    public async Task<IReadOnlyList<MonthlyTimesheetSummaryDto>> GetMonthlySummariesAsync(string period, int organizationalUnitId, CancellationToken cancellationToken = default)
    {
        var summaries = await _context.MonthlyTimesheetSummaries
            .Include(s => s.Employee)
            .Include(s => s.OrganizationalUnit)
            .Where(s => s.Period == period && s.OrganizationalUnitId == organizationalUnitId)
            .OrderBy(s => s.Employee!.EmployeeCode)
            .ToListAsync(cancellationToken);

        return summaries.Select(s => new MonthlyTimesheetSummaryDto(
            s.Id,
            s.Period,
            s.EmployeeId,
            s.Employee?.EmployeeCode ?? string.Empty,
            s.Employee?.FullName ?? string.Empty,
            s.OrganizationalUnitId,
            s.OrganizationalUnit?.UnitName ?? string.Empty,
            s.StandardWorkDays,
            s.ActualWorkDays,
            s.PaidLeaveDays,
            s.SickLeaveDays,
            s.MaternityLeaveDays,
            s.HolidayDays,
            s.CompensatoryLeaveDays,
            s.BusinessTripDays,
            s.TrainingDays,
            s.UnpaidLeaveDays,
            s.UnexcusedAbsenceDays,
            s.ConsecutiveUnexcusedDays,
            s.TotalWorkHours,
            s.NightHours,
            s.TotalOtHours,
            s.RegularOtHours,
            s.RestDayOtHours,
            s.HolidayOtHours,
            s.NightOtHours,
            s.LateTimes,
            s.EarlyLeaveTimes)).ToList();
    }

    private async Task<MonthlyTimesheetPeriod> GetOrCreatePeriodAsync(string period, int unitId, CancellationToken cancellationToken)
    {
        var existing = await _context.MonthlyTimesheetPeriods
            .FirstOrDefaultAsync(p => p.Period == period && p.OrganizationalUnitId == unitId, cancellationToken);

        if (existing == null)
        {
            existing = new MonthlyTimesheetPeriod(period, unitId);
            _context.MonthlyTimesheetPeriods.Add(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }
}
