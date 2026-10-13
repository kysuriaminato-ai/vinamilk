using System;
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

public class AttendanceExplanationService : IAttendanceExplanationService
{
    private readonly IApplicationDbContext _context;
    private readonly IAttendanceCalculationService _calculationService;

    // Giới hạn giải trình tối đa 3 lần/tháng theo nội quy Vinamilk
    private const int MaxExplanationsPerMonth = 3;

    public AttendanceExplanationService(
        IApplicationDbContext context,
        IAttendanceCalculationService calculationService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _calculationService = calculationService ?? throw new ArgumentNullException(nameof(calculationService));
    }

    public async Task<AttendanceExplanationDto> SubmitExplanationAsync(CreateExplanationRequest request, CancellationToken cancellationToken = default)
    {
        string period = $"{request.AttendanceDate.Year:D4}-{request.AttendanceDate.Month:D2}";

        var emp = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={request.EmployeeId}.", "ERR_EMP_NOT_FOUND");

        // 1. Kiểm tra khóa kỳ công
        var lockedPeriod = await _context.MonthlyTimesheetPeriods
            .FirstOrDefaultAsync(p => p.Period == period && p.OrganizationalUnitId == emp.OrganizationalUnitId && p.Status == TimesheetPeriodStatus.Locked, cancellationToken);
        if (lockedPeriod != null)
        {
            throw new DomainException($"Kỳ công {period} đã bị khóa sổ, không thể gửi đơn giải trình.", "ERR_ATT_PERIOD_LOCKED");
        }

        // 2. Kiểm tra giới hạn số lần giải trình trong tháng
        int countThisMonth = await _context.AttendanceExplanations
            .CountAsync(e => e.EmployeeId == request.EmployeeId &&
                             e.AttendanceDate.Year == request.AttendanceDate.Year &&
                             e.AttendanceDate.Month == request.AttendanceDate.Month, cancellationToken);

        if (countThisMonth >= MaxExplanationsPerMonth)
        {
            throw new DomainException(
                $"Vượt quá giới hạn cho phép tối đa {MaxExplanationsPerMonth} lần giải trình quên chấm công trong tháng.",
                "ERR_ATT_EXPLANATION_LIMIT_EXCEEDED");
        }

        var explanation = new AttendanceExplanation(
            request.EmployeeId,
            request.AttendanceDate,
            request.MissingPunchType,
            request.ActualCheckIn,
            request.ActualCheckOut,
            request.Reason);

        _context.AttendanceExplanations.Add(explanation);
        await _context.SaveChangesAsync(cancellationToken);

        return new AttendanceExplanationDto(
            explanation.Id,
            explanation.EmployeeId,
            emp.EmployeeCode,
            emp.FullName,
            explanation.AttendanceDate,
            explanation.MissingPunchType,
            explanation.ActualCheckIn,
            explanation.ActualCheckOut,
            explanation.Reason,
            explanation.Status,
            null,
            null,
            null,
            null);
    }

    public async Task<AttendanceExplanationDto> ApproveExplanationAsync(int explanationId, int approverId, CancellationToken cancellationToken = default)
    {
        var explanation = await _context.AttendanceExplanations
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.Id == explanationId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đơn giải trình Id={explanationId}.", "ERR_EXPLANATION_NOT_FOUND");

        string period = $"{explanation.AttendanceDate.Year:D4}-{explanation.AttendanceDate.Month:D2}";
        int unitId = explanation.Employee?.OrganizationalUnitId ?? 0;

        var lockedPeriod = await _context.MonthlyTimesheetPeriods
            .FirstOrDefaultAsync(p => p.Period == period && p.OrganizationalUnitId == unitId && p.Status == TimesheetPeriodStatus.Locked, cancellationToken);
        if (lockedPeriod != null)
        {
            throw new DomainException($"Kỳ công {period} đã bị khóa sổ, không thể phê duyệt giải trình.", "ERR_ATT_PERIOD_LOCKED");
        }

        explanation.Approve(approverId);

        // Bổ sung TimeEvent nhân tạo theo giờ giải trình
        var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        if (explanation.ActualCheckIn.HasValue)
        {
            var checkInLocal = explanation.AttendanceDate.ToDateTime(explanation.ActualCheckIn.Value);
            var checkInUtc = TimeZoneInfo.ConvertTimeToUtc(checkInLocal, tz);
            _context.TimeEvents.Add(new TimeEvent(
                explanation.EmployeeId,
                checkInUtc,
                deviceId: "MANUAL_EXPLANATION",
                method: PunchMethod.Manual));
        }

        if (explanation.ActualCheckOut.HasValue)
        {
            var checkOutLocal = explanation.AttendanceDate.ToDateTime(explanation.ActualCheckOut.Value);
            var checkOutUtc = TimeZoneInfo.ConvertTimeToUtc(checkOutLocal, tz);
            _context.TimeEvents.Add(new TimeEvent(
                explanation.EmployeeId,
                checkOutUtc,
                deviceId: "MANUAL_EXPLANATION",
                method: PunchMethod.Manual));
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Tính lại DailyAttendance của ngày đó
        await _calculationService.CalculateDailyAttendanceAsync(explanation.EmployeeId, explanation.AttendanceDate, cancellationToken);

        var approver = await _context.Employees.FindAsync(new object[] { approverId }, cancellationToken);

        return new AttendanceExplanationDto(
            explanation.Id,
            explanation.EmployeeId,
            explanation.Employee?.EmployeeCode ?? string.Empty,
            explanation.Employee?.FullName ?? string.Empty,
            explanation.AttendanceDate,
            explanation.MissingPunchType,
            explanation.ActualCheckIn,
            explanation.ActualCheckOut,
            explanation.Reason,
            explanation.Status,
            explanation.ApproverId,
            approver?.FullName,
            explanation.ApprovedAt,
            null);
    }

    public async Task<AttendanceExplanationDto> RejectExplanationAsync(int explanationId, string reason, CancellationToken cancellationToken = default)
    {
        var explanation = await _context.AttendanceExplanations
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.Id == explanationId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đơn giải trình Id={explanationId}.", "ERR_EXPLANATION_NOT_FOUND");

        explanation.Reject(reason);
        await _context.SaveChangesAsync(cancellationToken);

        return new AttendanceExplanationDto(
            explanation.Id,
            explanation.EmployeeId,
            explanation.Employee?.EmployeeCode ?? string.Empty,
            explanation.Employee?.FullName ?? string.Empty,
            explanation.AttendanceDate,
            explanation.MissingPunchType,
            explanation.ActualCheckIn,
            explanation.ActualCheckOut,
            explanation.Reason,
            explanation.Status,
            null,
            null,
            null,
            explanation.RejectionReason);
    }
}
