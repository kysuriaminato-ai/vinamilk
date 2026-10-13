using System;
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

public class OvertimeService : IOvertimeService
{
    private readonly IApplicationDbContext _context;
    private readonly IRosterConstraintValidator _constraintValidator;

    public OvertimeService(
        IApplicationDbContext context,
        IRosterConstraintValidator constraintValidator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));
    }

    public async Task<OvertimeRequestDto> CreateRequestAsync(CreateOvertimeRequest request, CancellationToken cancellationToken = default)
    {
        var emp = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={request.EmployeeId}.", "ERR_EMP_NOT_FOUND");

        // 1. Kiểm tra Điều 137 BLLĐ: Bảo vệ thai sản & con nhỏ dưới 12 tháng
        _constraintValidator.ValidateMaternalProtection(emp, request.WorkDate, null, request.PlannedHours);

        // 2. Tính lũy kế giờ làm thêm trong tháng và năm
        var firstDayOfMonth = new DateOnly(request.WorkDate.Year, request.WorkDate.Month, 1);
        var firstDayOfYear = new DateOnly(request.WorkDate.Year, 1, 1);

        decimal monthlyOt = await _context.OvertimeRequests
            .Where(o => o.EmployeeId == request.EmployeeId &&
                        o.WorkDate >= firstDayOfMonth &&
                        o.WorkDate <= request.WorkDate &&
                        o.Status == OvertimeStatus.Approved)
            .SumAsync(o => o.PlannedHours, cancellationToken);

        decimal yearlyOt = await _context.OvertimeRequests
            .Where(o => o.EmployeeId == request.EmployeeId &&
                        o.WorkDate >= firstDayOfYear &&
                        o.WorkDate <= request.WorkDate &&
                        o.Status == OvertimeStatus.Approved)
            .SumAsync(o => o.PlannedHours, cancellationToken);

        // 3. Kiểm tra giới hạn trần OT theo Điều 107 BLLĐ 2019
        _constraintValidator.ValidateOvertimeCap(request.PlannedHours, monthlyOt, yearlyOt);

        var ot = new OvertimeRequest(
            request.EmployeeId,
            request.WorkDate,
            request.StartTime,
            request.EndTime,
            request.PlannedHours,
            request.Reason);

        _context.OvertimeRequests.Add(ot);
        await _context.SaveChangesAsync(cancellationToken);

        return new OvertimeRequestDto(
            ot.Id,
            ot.EmployeeId,
            emp.EmployeeCode,
            emp.FullName,
            ot.WorkDate,
            ot.StartTime,
            ot.EndTime,
            ot.PlannedHours,
            ot.Reason,
            ot.Status,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    public async Task<OvertimeRequestDto> ApproveByTeamLeaderAsync(int requestId, int leaderId, CancellationToken cancellationToken = default)
    {
        var ot = await _context.OvertimeRequests
            .Include(o => o.Employee)
            .FirstOrDefaultAsync(o => o.Id == requestId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy phiếu đăng ký làm thêm Id={requestId}.", "ERR_OT_NOT_FOUND");

        ot.ApproveByTeamLeader(leaderId);
        await _context.SaveChangesAsync(cancellationToken);

        var leader = await _context.Employees.FindAsync(new object[] { leaderId }, cancellationToken);

        return new OvertimeRequestDto(
            ot.Id,
            ot.EmployeeId,
            ot.Employee?.EmployeeCode ?? string.Empty,
            ot.Employee?.FullName ?? string.Empty,
            ot.WorkDate,
            ot.StartTime,
            ot.EndTime,
            ot.PlannedHours,
            ot.Reason,
            ot.Status,
            ot.TeamLeaderId,
            leader?.FullName,
            ot.TeamLeaderApprovedAt,
            null,
            null,
            null,
            null);
    }

    public async Task<OvertimeRequestDto> ApproveByUnitManagerAsync(int requestId, int managerId, CancellationToken cancellationToken = default)
    {
        var ot = await _context.OvertimeRequests
            .Include(o => o.Employee)
            .Include(o => o.TeamLeader)
            .FirstOrDefaultAsync(o => o.Id == requestId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy phiếu đăng ký làm thêm Id={requestId}.", "ERR_OT_NOT_FOUND");

        ot.ApproveByUnitManager(managerId);
        await _context.SaveChangesAsync(cancellationToken);

        var manager = await _context.Employees.FindAsync(new object[] { managerId }, cancellationToken);

        return new OvertimeRequestDto(
            ot.Id,
            ot.EmployeeId,
            ot.Employee?.EmployeeCode ?? string.Empty,
            ot.Employee?.FullName ?? string.Empty,
            ot.WorkDate,
            ot.StartTime,
            ot.EndTime,
            ot.PlannedHours,
            ot.Reason,
            ot.Status,
            ot.TeamLeaderId,
            ot.TeamLeader?.FullName,
            ot.TeamLeaderApprovedAt,
            ot.UnitManagerId,
            manager?.FullName,
            ot.UnitManagerApprovedAt,
            null);
    }

    public async Task<OvertimeRequestDto> RejectAsync(int requestId, string reason, CancellationToken cancellationToken = default)
    {
        var ot = await _context.OvertimeRequests
            .Include(o => o.Employee)
            .FirstOrDefaultAsync(o => o.Id == requestId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy phiếu đăng ký làm thêm Id={requestId}.", "ERR_OT_NOT_FOUND");

        ot.Reject(reason);
        await _context.SaveChangesAsync(cancellationToken);

        return new OvertimeRequestDto(
            ot.Id,
            ot.EmployeeId,
            ot.Employee?.EmployeeCode ?? string.Empty,
            ot.Employee?.FullName ?? string.Empty,
            ot.WorkDate,
            ot.StartTime,
            ot.EndTime,
            ot.PlannedHours,
            ot.Reason,
            ot.Status,
            null,
            null,
            null,
            null,
            null,
            null,
            ot.RejectionReason);
    }
}
