using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.TimeAttendance;

// ============================================================================
// 1. PURE ENGINES: ĐỐI CHIẾU OT & KIỂM TRA TRẦN OT PHÁP LÝ (KHÔNG GHI CỨNG)
// ============================================================================

public sealed record OvertimeLimitEvaluationResult(
    bool IsAllowed,
    bool IsBlocked,
    bool HasWarning,
    decimal EffectiveDailyLimit,
    decimal EffectiveMonthlyLimit,
    decimal EffectiveYearlyLimit,
    decimal ProjectedMonthHours,
    decimal ProjectedYearHours,
    string? Message);

public static class OvertimePolicyEngine
{
    public static OvertimeLimitEvaluationResult EvaluateLimits(
        decimal requestedHours,
        decimal existingMonthHours,
        decimal existingYearHours,
        AttendanceStatutoryParameter statutory,
        AttendancePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(statutory);
        ArgumentNullException.ThrowIfNull(policy);

        decimal dailyCap = statutory.MaxDailyOvertimeHours;
        decimal monthlyCap = statutory.MaxMonthlyOvertimeHours;
        decimal yearlyCap = policy.UseSpecialIndustry300OtLimit
            ? statutory.MaxSpecialIndustryYearlyOvertimeHours
            : statutory.MaxYearlyOvertimeHours;

        decimal projectedMonth = existingMonthHours + requestedHours;
        decimal projectedYear = existingYearHours + requestedHours;

        var exceedReasons = new List<string>();
        if (dailyCap > 0m && requestedHours > dailyCap)
        {
            exceedReasons.Add(
                $"Giờ OT ngày ({requestedHours:0.#}h) vượt giới hạn {dailyCap:0.#} giờ/ngày theo {statutory.LegalReference}");
        }

        if (monthlyCap > 0m && projectedMonth > monthlyCap)
        {
            exceedReasons.Add(
                $"Tổng OT tháng ({projectedMonth:0.#}h = {existingMonthHours:0.#}h + {requestedHours:0.#}h) vượt trần {monthlyCap:0.#} giờ/tháng theo {statutory.LegalReference}");
        }

        if (yearlyCap > 0m && projectedYear > yearlyCap)
        {
            exceedReasons.Add(
                $"Tổng OT năm ({projectedYear:0.#}h = {existingYearHours:0.#}h + {requestedHours:0.#}h) vượt trần {yearlyCap:0.#} giờ/năm theo {statutory.LegalReference}");
        }

        if (exceedReasons.Count > 0)
        {
            var msg = string.Join("; ", exceedReasons) + ".";
            return new OvertimeLimitEvaluationResult(
                IsAllowed: !policy.BlockOvertimeWhenExceedingLimit,
                IsBlocked: policy.BlockOvertimeWhenExceedingLimit,
                HasWarning: true,
                EffectiveDailyLimit: dailyCap,
                EffectiveMonthlyLimit: monthlyCap,
                EffectiveYearlyLimit: yearlyCap,
                ProjectedMonthHours: projectedMonth,
                ProjectedYearHours: projectedYear,
                Message: policy.BlockOvertimeWhenExceedingLimit
                    ? $"Bị chặn vượt trần OT: {msg}"
                    : $"Cảnh báo vượt trần OT (cho phép theo cấu hình): {msg}");
        }

        decimal warningRatio = Math.Clamp(policy.OvertimeWarningThresholdPercent, 1, 100) / 100m;
        var warnings = new List<string>();
        if (monthlyCap > 0m && projectedMonth >= monthlyCap * warningRatio)
            warnings.Add($"OT tháng đạt {projectedMonth:0.#}/{monthlyCap:0.#}h ({projectedMonth / monthlyCap * 100m:0.#}%)");
        if (yearlyCap > 0m && projectedYear >= yearlyCap * warningRatio)
            warnings.Add($"OT năm đạt {projectedYear:0.#}/{yearlyCap:0.#}h ({projectedYear / yearlyCap * 100m:0.#}%)");

        return new OvertimeLimitEvaluationResult(
            IsAllowed: true,
            IsBlocked: false,
            HasWarning: warnings.Count > 0,
            EffectiveDailyLimit: dailyCap,
            EffectiveMonthlyLimit: monthlyCap,
            EffectiveYearlyLimit: yearlyCap,
            ProjectedMonthHours: projectedMonth,
            ProjectedYearHours: projectedYear,
            Message: warnings.Count > 0 ? $"Cảnh báo sắp chạm trần OT: {string.Join("; ", warnings)}." : null);
    }

    public static decimal ReconcileOtHours(
        decimal approvedHours,
        decimal actualWorkedOtHours,
        string reconciliationMode)
    {
        decimal clampedApproved = Math.Max(0m, approvedHours);
        decimal clampedActual = Math.Max(0m, actualWorkedOtHours);

        decimal result = reconciliationMode switch
        {
            "UseApprovedFixed" => clampedApproved,
            "UseActualUpToRegistered" => clampedActual > 0m ? Math.Min(clampedApproved, clampedActual) : clampedApproved,
            _ => Math.Min(clampedApproved, clampedActual)
        };

        return Math.Round(result, 2, MidpointRounding.AwayFromZero);
    }
}

// ============================================================================
// 2. DTOs & QUERIES: BÁO CÁO SỐ DƯ PHÉP TOÀN CÔNG TY & CỔNG ĐƠN TỪ CL4
// ============================================================================

public record EmployeeTimeRequestItemDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    string RequestType,
    string? SubType,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? PlannedTime,
    TimeOnly? ProposedEndTime,
    int? RequestedMinutes,
    string? LocationOrDestination,
    string? AttachmentUrl,
    string Reason,
    string Status,
    int? WorkflowInstanceId,
    string? ApproverComment,
    DateTime CreatedAt,
    DateTime? ApprovedAt);

public record RequestCancellationItemDto(
    int Id,
    string OriginalEntityType,
    int OriginalEntityId,
    int RequesterEmployeeId,
    string RequesterName,
    string Reason,
    string Status,
    int? WorkflowInstanceId,
    string? RestoredBalanceSummary,
    DateTime CreatedAt);

public record CompanyLeaveBalanceReportRowDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitCode,
    string UnitName,
    string PositionName,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    int SeniorityYears,
    decimal StandardEntitlement,
    decimal SeniorityExtraDays,
    decimal CarriedForwardDays,
    decimal UsedCarriedForwardDays,
    decimal ExpiredCarryForwardDays,
    DateOnly? CarryForwardExpiryDate,
    decimal UsedDays,
    decimal PendingDays,
    decimal AdvanceUsedDays,
    decimal RemainingDays,
    decimal CompensatoryHoursBalance,
    decimal TerminationPayoutDays,
    decimal TerminationPayoutAmount,
    bool IsTerminationSettled);

public record CompanyLeaveBalanceUnitSummaryDto(
    string UnitCode,
    string UnitName,
    int EmployeeCount,
    decimal TotalEntitlementAndCarryDays,
    decimal TotalUsedDays,
    decimal TotalExpiredCarryDays,
    decimal TotalRemainingDays,
    decimal TotalCompensatoryHours,
    decimal TotalTerminationPayoutAmount);

public record CompanyLeaveBalanceReportDto(
    int Year,
    DateOnly AsOfDate,
    int? UnitId,
    IReadOnlyList<CompanyLeaveBalanceReportRowDto> Rows,
    IReadOnlyList<CompanyLeaveBalanceUnitSummaryDto> UnitSummaries,
    decimal TotalRemainingDaysAllCompany,
    decimal TotalExpiredCarryDaysAllCompany,
    decimal TotalCompensatoryHoursAllCompany,
    decimal TotalTerminationPayoutAllCompany);

public record TerminationLeaveSettlementDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    DateOnly TerminationDate,
    decimal ProRatedEntitlementDays,
    decimal SeniorityExtraDays,
    decimal ValidCarryForwardDays,
    decimal UsedDays,
    decimal UnusedPayableDays,
    decimal BaseSalaryUsed,
    decimal StandardWorkdaysUsed,
    decimal PayoutAmount,
    int TargetPayrollMonth,
    int TargetPayrollYear,
    string Status,
    string? Notes);

public record GetCompanyLeaveBalanceReportQuery(
    int Year,
    DateOnly? AsOfDate = null,
    int? UnitId = null) : IRequest<CompanyLeaveBalanceReportDto>;

public class GetCompanyLeaveBalanceReportQueryHandler : IRequestHandler<GetCompanyLeaveBalanceReportQuery, CompanyLeaveBalanceReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetCompanyLeaveBalanceReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyLeaveBalanceReportDto> Handle(GetCompanyLeaveBalanceReportQuery request, CancellationToken cancellationToken)
    {
        var asOfDate = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var yearStart = new DateOnly(request.Year, 1, 1);
        var yearEnd = new DateOnly(request.Year, 12, 31);

        var statutory = await AttendancePolicyAndShiftEngine.TryResolveStatutoryParameterAsync(_context, yearEnd, cancellationToken)
            ?? await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(_context, yearStart, cancellationToken);

        var annualType = await _context.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LeaveTypeCode == "ANNUAL", cancellationToken);

        var balancesQuery = _context.LeaveBalances
            .AsNoTracking()
            .Include(b => b.Employee).ThenInclude(e => e.OrganizationUnit)
            .Include(b => b.Employee).ThenInclude(e => e.Position)
            .Where(b => b.Year == request.Year && (annualType == null || b.LeaveTypeId == annualType.Id));

        if (request.UnitId.HasValue && request.UnitId.Value > 0)
        {
            var unit = await _context.OrganizationUnits.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UnitId.Value, cancellationToken);
            if (unit != null && !string.IsNullOrEmpty(unit.TreePath))
                balancesQuery = balancesQuery.Where(b => b.Employee.OrganizationUnit.TreePath!.StartsWith(unit.TreePath));
            else
                balancesQuery = balancesQuery.Where(b => b.Employee.OrganizationUnitId == request.UnitId.Value);
        }

        var balances = await balancesQuery
            .OrderBy(b => b.Employee.OrganizationUnit.UnitCode)
            .ThenBy(b => b.Employee.EmployeeCode)
            .ToListAsync(cancellationToken);

        var rows = new List<CompanyLeaveBalanceReportRowDto>();
        foreach (var b in balances)
        {
            var resolved = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, b.EmployeeId, asOfDate, cancellationToken);
            var (senYears, _, _) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(
                b.Employee.HireDate, request.Year, statutory, resolved.Policy, b.Employee.TerminationDate);

            var (_, expiredCarry, grossAvailable, _) = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(
                b, resolved.Policy, asOfDate);

            decimal effectiveExpired = Math.Max(b.ExpiredCarryForwardDays, expiredCarry);
            decimal remaining = Math.Max(0m, grossAvailable);

            rows.Add(new CompanyLeaveBalanceReportRowDto(
                b.EmployeeId,
                b.Employee.EmployeeCode,
                b.Employee.FullName,
                b.Employee.OrganizationUnit.UnitCode,
                b.Employee.OrganizationUnit.UnitName,
                b.Employee.Position.PositionName,
                b.Employee.HireDate,
                b.Employee.TerminationDate,
                senYears,
                b.StandardEntitlement,
                b.SeniorityExtraDays,
                b.CarriedForwardDays,
                b.UsedCarriedForwardDays,
                effectiveExpired,
                b.CarryForwardExpiryDate,
                b.UsedDays,
                b.PendingDays,
                b.AdvanceUsedDays,
                remaining,
                b.CompensatoryHoursBalance,
                b.TerminationPayoutDays,
                b.TerminationPayoutAmount,
                b.IsTerminationSettled));
        }

        var unitSummaries = rows
            .GroupBy(r => new { r.UnitCode, r.UnitName })
            .OrderBy(g => g.Key.UnitCode)
            .Select(g => new CompanyLeaveBalanceUnitSummaryDto(
                g.Key.UnitCode,
                g.Key.UnitName,
                g.Count(),
                g.Sum(x => x.StandardEntitlement + x.SeniorityExtraDays + x.CarriedForwardDays),
                g.Sum(x => x.UsedDays),
                g.Sum(x => x.ExpiredCarryForwardDays),
                g.Sum(x => x.RemainingDays),
                g.Sum(x => x.CompensatoryHoursBalance),
                g.Sum(x => x.TerminationPayoutAmount)))
            .ToList();

        return new CompanyLeaveBalanceReportDto(
            request.Year,
            asOfDate,
            request.UnitId,
            rows,
            unitSummaries,
            rows.Sum(r => r.RemainingDays),
            rows.Sum(r => r.ExpiredCarryForwardDays),
            rows.Sum(r => r.CompensatoryHoursBalance),
            rows.Sum(r => r.TerminationPayoutAmount));
    }
}

// ============================================================================
// 3. COMMAND: CẤU HÌNH LOẠI NGHỈ (LEAVETYPE CRUD)
// ============================================================================

public record SaveLeaveTypeCommand(
    int? Id,
    string LeaveTypeCode,
    string LeaveTypeName,
    bool IsPaidByCompany,
    bool IsPaidBySocialInsurance,
    bool CountsAgainstAnnualLeave,
    bool AllowHalfDay,
    bool AllowHourly,
    bool RequiresAttachment,
    decimal? MaxDaysPerRequest,
    decimal MaxDaysPerYear,
    bool AllowCarryForward,
    string AttendanceSymbol,
    int DisplayOrder = 0,
    bool IsActive = true,
    string? LegalReference = null,
    bool NeedsHrVerification = true,
    string? Description = null) : IRequest<int>;

public class SaveLeaveTypeCommandHandler : IRequestHandler<SaveLeaveTypeCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SaveLeaveTypeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SaveLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (string.IsNullOrWhiteSpace(request.LeaveTypeCode) || string.IsNullOrWhiteSpace(request.LeaveTypeName))
            throw new InvalidOperationException("Mã loại nghỉ và Tên loại nghỉ không được để trống.");

        var normalizedCode = request.LeaveTypeCode.Trim().ToUpperInvariant();
        LeaveType entity;
        bool isNew = !request.Id.HasValue || request.Id.Value <= 0;

        if (isNew)
        {
            if (await _context.LeaveTypes.AnyAsync(l => l.LeaveTypeCode == normalizedCode, cancellationToken))
                throw new InvalidOperationException($"Mã loại nghỉ '{normalizedCode}' đã tồn tại.");
            entity = new LeaveType();
            await _context.LeaveTypes.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity = await _context.LeaveTypes.FirstOrDefaultAsync(l => l.Id == request.Id!.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy loại nghỉ #{request.Id}.");
        }

        entity.LeaveTypeCode = normalizedCode;
        entity.LeaveTypeName = request.LeaveTypeName.Trim();
        entity.IsPaidByCompany = request.IsPaidByCompany;
        entity.IsPaidBySocialInsurance = request.IsPaidBySocialInsurance;
        entity.CountsAgainstAnnualLeave = request.CountsAgainstAnnualLeave;
        entity.AllowHalfDay = request.AllowHalfDay;
        entity.AllowHourly = request.AllowHourly;
        entity.RequiresAttachment = request.RequiresAttachment;
        entity.MaxDaysPerRequest = request.MaxDaysPerRequest.HasValue && request.MaxDaysPerRequest.Value > 0 ? request.MaxDaysPerRequest : null;
        entity.MaxDaysPerYear = Math.Max(0m, request.MaxDaysPerYear);
        entity.AllowCarryForward = request.AllowCarryForward;
        entity.AttendanceSymbol = string.IsNullOrWhiteSpace(request.AttendanceSymbol) ? "P" : request.AttendanceSymbol.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        entity.LegalReference = request.LegalReference?.Trim();
        entity.NeedsHrVerification = request.NeedsHrVerification;
        entity.Description = request.Description?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "TimeAttendance",
            ActionType = isNew ? "LeaveType_Create" : "LeaveType_Update",
            EntityName = nameof(LeaveType),
            EntityId = entity.Id.ToString(),
            NewValuesJson = $"{{\"LeaveTypeCode\":\"{entity.LeaveTypeCode}\",\"CountsAgainstAnnualLeave\":{entity.CountsAgainstAnnualLeave.ToString().ToLowerInvariant()},\"MaxDaysPerYear\":{entity.MaxDaysPerYear}}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}

// ============================================================================
// 4. COMMAND: ĐĂNG KÝ OT NHÓM & ĐỐI CHIẾU OT THỰC TẾ + QUY ĐỔI NGHỈ BÙ
// ============================================================================

public record SubmitGroupOvertimeResultDto(
    string OvertimeBatchCode,
    IReadOnlyList<int> CreatedRequestIds,
    IReadOnlyList<string> Warnings);

public record SubmitGroupOvertimeRequestCommand(
    IReadOnlyList<int> EmployeeIds,
    DateOnly OvertimeDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string OvertimeRateType,
    string CompensationMode,
    string Reason) : IRequest<SubmitGroupOvertimeResultDto>;

public class SubmitGroupOvertimeRequestCommandHandler : IRequestHandler<SubmitGroupOvertimeRequestCommand, SubmitGroupOvertimeResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public SubmitGroupOvertimeRequestCommandHandler(
        IApplicationDbContext context,
        ISender sender,
        ICurrentUserService currentUser)
    {
        _context = context;
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<SubmitGroupOvertimeResultDto> Handle(SubmitGroupOvertimeRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (request.EmployeeIds == null || request.EmployeeIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một nhân viên để đăng ký làm thêm giờ.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do tăng ca.");

        double hours = (request.EndTime - request.StartTime).TotalHours;
        if (hours <= 0) hours += 24.0;
        decimal requestedHours = Math.Round((decimal)hours, 1);
        if (requestedHours <= 0)
            throw new InvalidOperationException("Khoảng thời gian OT không hợp lệ.");

        var normalizedCompMode = string.Equals(request.CompensationMode, "CompensatoryLeave", StringComparison.OrdinalIgnoreCase)
            ? "CompensatoryLeave"
            : "Paid";

        var statutory = await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(_context, request.OvertimeDate, cancellationToken);
        var distinctEmpIds = request.EmployeeIds.Where(id => id > 0).Distinct().ToList();
        var empMap = await _context.Employees.AsNoTracking()
            .Where(e => distinctEmpIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        var batchCode = $"OTB-{request.OvertimeDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var createdIds = new List<int>();
        var warnings = new List<string>();

        foreach (var empId in distinctEmpIds)
        {
            if (!empMap.TryGetValue(empId, out var emp))
                throw new KeyNotFoundException($"Không tìm thấy nhân viên #{empId}.");

            var monthLocked = await _context.MonthlyAttendanceSummaries.AsNoTracking()
                .AnyAsync(m => m.EmployeeId == empId && m.Month == request.OvertimeDate.Month && m.Year == request.OvertimeDate.Year && m.IsLocked, cancellationToken);
            if (monthLocked)
                throw new InvalidOperationException($"Bảng công tháng {request.OvertimeDate.Month:D2}/{request.OvertimeDate.Year} của {emp.FullName} đã khóa.");

            var resolved = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, empId, request.OvertimeDate, cancellationToken);
            if (normalizedCompMode == "CompensatoryLeave" && !resolved.Policy.AllowCompensatoryLeaveForOt)
                throw new InvalidOperationException($"Nhóm chính sách '{resolved.PolicyCode}' không cho phép quy đổi OT sang nghỉ bù cho {emp.FullName}.");

            var monthOt = await _context.OvertimeRequests.AsNoTracking()
                .Where(o => o.EmployeeId == empId && o.OvertimeDate.Year == request.OvertimeDate.Year && o.OvertimeDate.Month == request.OvertimeDate.Month && o.Status != "Rejected" && o.Status != "Cancelled")
                .SumAsync(o => o.RequestedHours, cancellationToken);

            var yearOt = await _context.OvertimeRequests.AsNoTracking()
                .Where(o => o.EmployeeId == empId && o.OvertimeDate.Year == request.OvertimeDate.Year && o.Status != "Rejected" && o.Status != "Cancelled")
                .SumAsync(o => o.RequestedHours, cancellationToken);

            var limitEval = OvertimePolicyEngine.EvaluateLimits(requestedHours, monthOt, yearOt, statutory, resolved.Policy);
            if (!limitEval.IsAllowed)
                throw new InvalidOperationException($"{emp.FullName} ({emp.EmployeeCode}): {limitEval.Message}");

            if (limitEval.HasWarning && !string.IsNullOrWhiteSpace(limitEval.Message))
                warnings.Add($"{emp.FullName}: {limitEval.Message}");

            var otEntity = new OvertimeRequest
            {
                EmployeeId = empId,
                OvertimeBatchCode = distinctEmpIds.Count > 1 ? batchCode : null,
                OvertimeDate = request.OvertimeDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                RequestedHours = requestedHours,
                ApprovedHours = 0m,
                ActualWorkedOtHours = 0m,
                ReconciledOtHours = 0m,
                OvertimeRateType = request.OvertimeRateType,
                CompensationMode = normalizedCompMode,
                LimitWarningMessage = limitEval.Message,
                Reason = request.Reason.Trim(),
                Status = "Pending"
            };

            await _context.OvertimeRequests.AddAsync(otEntity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var wfId = await _sender.Send(new StartWorkflowInstanceCommand(
                "OvertimeRequest",
                otEntity.Id,
                empId,
                $"Đăng ký OT {(normalizedCompMode == "CompensatoryLeave" ? "[Nghỉ bù] " : string.Empty)}{request.OvertimeRateType} ({requestedHours:0.#}h ngày {request.OvertimeDate:dd/MM/yyyy})",
                requestedHours,
                "/Leaves?tab=overtime"), cancellationToken);

            otEntity.WorkflowInstanceId = wfId;
            await _context.SaveChangesAsync(cancellationToken);
            createdIds.Add(otEntity.Id);
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "TimeAttendance",
            ActionType = "Overtime_Submit",
            EntityName = nameof(OvertimeRequest),
            EntityId = batchCode,
            NewValuesJson = $"{{\"BatchCode\":\"{batchCode}\",\"EmployeeCount\":{createdIds.Count},\"HoursPerEmp\":{requestedHours},\"CompensationMode\":\"{normalizedCompMode}\"}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new SubmitGroupOvertimeResultDto(batchCode, createdIds, warnings);
    }
}

public record ReconcileOvertimeResultDto(
    int ReconciledCount,
    decimal TotalReconciledPaidHours,
    decimal TotalCreditedCompensatoryHours);

public record ReconcileOvertimeWithAttendanceCommand(
    int Month,
    int Year,
    int? EmployeeId = null) : IRequest<ReconcileOvertimeResultDto>;

public class ReconcileOvertimeWithAttendanceCommandHandler : IRequestHandler<ReconcileOvertimeWithAttendanceCommand, ReconcileOvertimeResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReconcileOvertimeWithAttendanceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ReconcileOvertimeResultDto> Handle(ReconcileOvertimeWithAttendanceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var monthLocked = await _context.MonthlyAttendanceSummaries.AsNoTracking()
            .AnyAsync(m => m.Year == request.Year && m.Month == request.Month && m.IsLocked && (!request.EmployeeId.HasValue || m.EmployeeId == request.EmployeeId.Value), cancellationToken);
        if (monthLocked)
            throw new InvalidOperationException($"Bảng công tháng {request.Month:D2}/{request.Year} đã khóa, không thể đối chiếu lại OT trực tiếp.");

        var query = _context.OvertimeRequests
            .Where(o => o.Status == "Approved" && o.OvertimeDate.Year == request.Year && o.OvertimeDate.Month == request.Month);
        if (request.EmployeeId.HasValue)
            query = query.Where(o => o.EmployeeId == request.EmployeeId.Value);

        var approvedOts = await query.ToListAsync(cancellationToken);
        if (approvedOts.Count == 0)
            return new ReconcileOvertimeResultDto(0, 0m, 0m);

        var empIds = approvedOts.Select(o => o.EmployeeId).Distinct().ToList();
        var monthStart = new DateOnly(request.Year, request.Month, 1).AddDays(-1).ToDateTime(TimeOnly.MinValue);
        var monthEnd = new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month)).AddDays(2).ToDateTime(TimeOnly.MinValue);

        var logs = await _context.AttendanceLogs
            .AsNoTracking()
            .Where(l => empIds.Contains(l.EmployeeId) && l.PunchTime >= monthStart && l.PunchTime <= monthEnd && l.GeofenceStatus != "OutsideRejected")
            .OrderBy(l => l.PunchTime)
            .ToListAsync(cancellationToken);

        var annualLeaveType = await _context.LeaveTypes.AsNoTracking()
            .FirstOrDefaultAsync(l => l.LeaveTypeCode == "ANNUAL", cancellationToken);

        decimal totalPaidReconciled = 0m;
        decimal totalCompCreditedDelta = 0m;

        foreach (var ot in approvedOts)
        {
            var resolved = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, ot.EmployeeId, ot.OvertimeDate, cancellationToken);
            var otStart = ot.OvertimeDate.ToDateTime(ot.StartTime);
            var otEnd = ot.EndTime <= ot.StartTime
                ? ot.OvertimeDate.AddDays(1).ToDateTime(ot.EndTime)
                : ot.OvertimeDate.ToDateTime(ot.EndTime);

            var dayLogs = logs
                .Where(l => l.EmployeeId == ot.EmployeeId && l.PunchTime >= ot.OvertimeDate.AddDays(-1).ToDateTime(new TimeOnly(12, 0)) && l.PunchTime <= ot.OvertimeDate.AddDays(1).ToDateTime(new TimeOnly(12, 0)))
                .Select(l => l.PunchTime)
                .OrderBy(t => t)
                .ToList();

            decimal actualHours = 0m;
            if (dayLogs.Count >= 2)
            {
                var firstIn = dayLogs.First();
                var lastOut = dayLogs.Last();
                var effStart = otStart > firstIn ? otStart : firstIn;
                var effEnd = otEnd < lastOut ? otEnd : lastOut;
                if (effEnd > effStart)
                    actualHours = Math.Round((decimal)(effEnd - effStart).TotalMinutes / 60m, 2, MidpointRounding.AwayFromZero);
            }

            ot.ActualWorkedOtHours = actualHours;
            ot.ReconciledOtHours = OvertimePolicyEngine.ReconcileOtHours(
                ot.ApprovedHours > 0m ? ot.ApprovedHours : ot.RequestedHours,
                actualHours,
                resolved.Policy.OtReconciliationMode);

            if (string.Equals(ot.CompensationMode, "CompensatoryLeave", StringComparison.OrdinalIgnoreCase))
            {
                decimal deltaCompHours = ot.ReconciledOtHours - ot.CompensatoryHoursCredited;
                if (deltaCompHours != 0m && annualLeaveType != null)
                {
                    var balance = await _context.LeaveBalances
                        .FirstOrDefaultAsync(b => b.EmployeeId == ot.EmployeeId && b.LeaveTypeId == annualLeaveType.Id && b.Year == ot.OvertimeDate.Year, cancellationToken);
                    if (balance == null)
                    {
                        balance = new LeaveBalance
                        {
                            EmployeeId = ot.EmployeeId,
                            LeaveTypeId = annualLeaveType.Id,
                            Year = ot.OvertimeDate.Year,
                            StandardEntitlement = 0m,
                            RemainingDays = 0m,
                            CompensatoryHoursBalance = 0m
                        };
                        await _context.LeaveBalances.AddAsync(balance, cancellationToken);
                    }

                    balance.CompensatoryHoursBalance = Math.Max(0m, balance.CompensatoryHoursBalance + deltaCompHours);
                    ot.CompensatoryHoursCredited = ot.ReconciledOtHours;
                    totalCompCreditedDelta += Math.Max(0m, deltaCompHours);
                }
            }
            else
            {
                totalPaidReconciled += ot.ReconciledOtHours;
            }
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "TimeAttendance",
            ActionType = "Overtime_Reconcile",
            EntityName = nameof(OvertimeRequest),
            EntityId = $"{request.Year}-{request.Month:D2}",
            NewValuesJson = $"{{\"ReconciledCount\":{approvedOts.Count},\"PaidHours\":{totalPaidReconciled},\"CompHoursDelta\":{totalCompCreditedDelta}}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return new ReconcileOvertimeResultDto(approvedOts.Count, totalPaidReconciled, totalCompCreditedDelta);
    }
}

// ============================================================================
// 5. COMMAND: ĐƠN ĐI MUỘN/VỀ SỚM, CÔNG TÁC, LÀM TỪ XA (WFH), GIẢI TRÌNH CÔNG
// ============================================================================

public record SubmitEmployeeTimeRequestCommand(
    int EmployeeId,
    string RequestType, // LateEarly | BusinessTrip | RemoteWork | AttendanceExplanation
    string? SubType,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? PlannedTime,
    TimeOnly? ProposedEndTime,
    int? RequestedMinutes,
    string? LocationOrDestination,
    string? AttachmentUrl,
    string Reason) : IRequest<int>;

public class SubmitEmployeeTimeRequestCommandHandler : IRequestHandler<SubmitEmployeeTimeRequestCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public SubmitEmployeeTimeRequestCommandHandler(
        IApplicationDbContext context,
        ISender sender,
        ICurrentUserService currentUser)
    {
        _context = context;
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SubmitEmployeeTimeRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (request.EmployeeId <= 0)
            throw new InvalidOperationException("Vui lòng chọn nhân sự.");
        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do.");

        var normalizedType = request.RequestType.Trim() switch
        {
            "LateEarly" => "LateEarly",
            "BusinessTrip" => "BusinessTrip",
            "RemoteWork" => "RemoteWork",
            "AttendanceExplanation" => "AttendanceExplanation",
            _ => throw new InvalidOperationException($"Loại đơn '{request.RequestType}' không được hỗ trợ.")
        };

        var monthLocked = await _context.MonthlyAttendanceSummaries.AsNoTracking()
            .AnyAsync(m => m.EmployeeId == request.EmployeeId
                && m.IsLocked
                && ((m.Year > request.StartDate.Year || (m.Year == request.StartDate.Year && m.Month >= request.StartDate.Month))
                 && (m.Year < request.EndDate.Year || (m.Year == request.EndDate.Year && m.Month <= request.EndDate.Month))),
                cancellationToken);
        if (monthLocked)
            throw new InvalidOperationException("Bảng công tháng liên quan đã khóa, không thể gửi đơn mới (chỉ tạo điều chỉnh kỳ sau).");

        var entity = new EmployeeTimeRequest
        {
            EmployeeId = request.EmployeeId,
            RequestType = normalizedType,
            SubType = request.SubType?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PlannedTime = request.PlannedTime,
            ProposedEndTime = request.ProposedEndTime,
            RequestedMinutes = request.RequestedMinutes,
            LocationOrDestination = request.LocationOrDestination?.Trim(),
            AttachmentUrl = request.AttachmentUrl?.Trim(),
            Reason = request.Reason.Trim(),
            Status = "Pending"
        };

        await _context.EmployeeTimeRequests.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var title = normalizedType switch
        {
            "LateEarly" => $"Đơn Đi muộn/Về sớm ({request.RequestedMinutes ?? 0}p ngày {request.StartDate:dd/MM/yyyy})",
            "BusinessTrip" => $"Đơn Công tác ({request.LocationOrDestination ?? "Nội địa"}: {request.StartDate:dd/MM}-{request.EndDate:dd/MM/yyyy})",
            "RemoteWork" => $"Đơn Làm việc từ xa WFH ({request.StartDate:dd/MM}-{request.EndDate:dd/MM/yyyy})",
            _ => $"Đơn Giải trình công ({request.StartDate:dd/MM/yyyy})"
        };

        var wfId = await _sender.Send(new StartWorkflowInstanceCommand(
            $"TimeRequest:{normalizedType}",
            entity.Id,
            request.EmployeeId,
            title,
            request.EndDate.DayNumber - request.StartDate.DayNumber + 1,
            "/Leaves?tab=time-requests"), cancellationToken);

        entity.WorkflowInstanceId = wfId;
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "TimeAttendance",
            ActionType = "EmployeeTimeRequest_Submit",
            EntityName = nameof(EmployeeTimeRequest),
            EntityId = entity.Id.ToString(),
            NewValuesJson = $"{{\"RequestType\":\"{normalizedType}\",\"StartDate\":\"{request.StartDate:yyyy-MM-dd}\"}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

// ============================================================================
// 6. COMMAND: HỦY ĐƠN CHỜ DUYỆT HOẶC YÊU CẦU THU HỒI ĐƠN ĐÃ DUYỆT QUA WORKFLOW
// ============================================================================

public record RequestCancelOrRevokeResultDto(
    string Mode, // CancelledDirectly | CancellationWorkflowStarted
    int? CancellationRequestId,
    int? WorkflowInstanceId,
    string Message);

public record CancelOrRequestRevocationCommand(
    string EntityType, // LeaveRequest | OvertimeRequest | EmployeeTimeRequest | ShiftSwapRequest
    int EntityId,
    string Reason) : IRequest<RequestCancelOrRevokeResultDto>;

public class CancelOrRequestRevocationCommandHandler : IRequestHandler<CancelOrRequestRevocationCommand, RequestCancelOrRevokeResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public CancelOrRequestRevocationCommandHandler(
        IApplicationDbContext context,
        ISender sender,
        ICurrentUserService currentUser)
    {
        _context = context;
        _sender = sender;
        _currentUser = currentUser;
    }

    public async Task<RequestCancelOrRevokeResultDto> Handle(CancelOrRequestRevocationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do hủy/thu hồi đơn.");

        int initiatorEmpId;
        string currentStatus;
        int? existingWfId;
        DateOnly refDate;

        switch (request.EntityType)
        {
            case "LeaveRequest":
                var lv = await _context.LeaveRequests.Include(l => l.LeaveType).FirstOrDefaultAsync(l => l.Id == request.EntityId, cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy đơn nghỉ phép.");
                initiatorEmpId = lv.EmployeeId;
                currentStatus = lv.Status;
                existingWfId = lv.WorkflowInstanceId;
                refDate = lv.StartDate;
                break;

            case "OvertimeRequest":
                var ot = await _context.OvertimeRequests.FirstOrDefaultAsync(o => o.Id == request.EntityId, cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy đơn làm thêm giờ.");
                initiatorEmpId = ot.EmployeeId;
                currentStatus = ot.Status;
                existingWfId = ot.WorkflowInstanceId;
                refDate = ot.OvertimeDate;
                break;

            case "EmployeeTimeRequest":
                var et = await _context.EmployeeTimeRequests.FirstOrDefaultAsync(t => t.Id == request.EntityId, cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy đơn chấm công.");
                initiatorEmpId = et.EmployeeId;
                currentStatus = et.Status;
                existingWfId = et.WorkflowInstanceId;
                refDate = et.StartDate;
                break;

            case "ShiftSwapRequest":
                var sw = await _context.ShiftSwapRequests.FirstOrDefaultAsync(s => s.Id == request.EntityId, cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy đơn đổi ca.");
                initiatorEmpId = sw.RequesterEmployeeId;
                currentStatus = sw.Status;
                existingWfId = sw.WorkflowInstanceId;
                refDate = sw.RequesterWorkDate;
                break;

            default:
                throw new InvalidOperationException($"Loại đơn '{request.EntityType}' không hỗ trợ hủy/thu hồi.");
        }

        if (currentStatus is "Cancelled" or "Rejected")
            throw new InvalidOperationException($"Đơn đang ở trạng thái '{currentStatus}', không thể hủy.");

        // 1. Nếu đang Pending -> hủy trực tiếp (nếu có Workflow InProgress thì chuyển Action Cancelled qua WorkflowEngine)
        if (currentStatus == "Pending")
        {
            if (existingWfId.HasValue)
            {
                await _sender.Send(new ProcessWorkflowActionCommand(existingWfId.Value, "Cancelled", request.Reason.Trim()), cancellationToken);
            }
            else
            {
                await RequestReversalHelper.ReverseEntityDirectlyAsync(_context, request.EntityType, request.EntityId, userId, username, request.Reason.Trim(), cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new RequestCancelOrRevokeResultDto("CancelledDirectly", null, existingWfId, "Đã hủy đơn đang chờ duyệt và hoàn lại số dư tạm giữ.");
        }

        // 2. Nếu đã Approved -> Bắt buộc kiểm tra chưa khóa bảng công và đi qua Workflow phê duyệt thu hồi
        var isLocked = await _context.MonthlyAttendanceSummaries.AsNoTracking()
            .AnyAsync(m => m.EmployeeId == initiatorEmpId && m.Year == refDate.Year && m.Month == refDate.Month && m.IsLocked, cancellationToken);
        if (isLocked)
            throw new InvalidOperationException($"Bảng công tháng {refDate.Month:D2}/{refDate.Year} đã khóa, không thể thu hồi đơn đã duyệt (vui lòng lập phiếu điều chỉnh kỳ sau).");

        var hasPendingCancel = await _context.RequestCancellationRequests.AsNoTracking()
            .AnyAsync(c => c.OriginalEntityType == request.EntityType && c.OriginalEntityId == request.EntityId && c.Status == "Pending", cancellationToken);
        if (hasPendingCancel)
            throw new InvalidOperationException("Đơn này đã có yêu cầu thu hồi đang chờ phê duyệt.");

        var cancelReq = new RequestCancellationRequest
        {
            RequesterEmployeeId = initiatorEmpId,
            OriginalEntityType = request.EntityType,
            OriginalEntityId = request.EntityId,
            Reason = request.Reason.Trim(),
            Status = "Pending"
        };

        await _context.RequestCancellationRequests.AddAsync(cancelReq, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var cancelWfId = await _sender.Send(new StartWorkflowInstanceCommand(
            "RequestCancellationRequest",
            cancelReq.Id,
            initiatorEmpId,
            $"Yêu cầu thu hồi {request.EntityType} #{request.EntityId} đã duyệt",
            1m,
            "/Leaves?tab=cancellations"), cancellationToken);

        cancelReq.WorkflowInstanceId = cancelWfId;
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "TimeAttendance",
            ActionType = "RequestCancellation_Submit",
            EntityName = nameof(RequestCancellationRequest),
            EntityId = cancelReq.Id.ToString(),
            NewValuesJson = $"{{\"OriginalEntityType\":\"{request.EntityType}\",\"OriginalEntityId\":{request.EntityId}}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return new RequestCancelOrRevokeResultDto(
            "CancellationWorkflowStarted",
            cancelReq.Id,
            cancelWfId,
            $"Đã gửi yêu cầu thu hồi #{cancelReq.Id} vào quy trình phê duyệt (#WF-{cancelWfId:D4}). Sau khi duyệt sẽ tự động hoàn lại số dư phép/công.");
    }
}

public static class RequestReversalHelper
{
    public static async Task<string> ReverseEntityDirectlyAsync(
        IApplicationDbContext context,
        string entityType,
        int entityId,
        int actorUserId,
        string actorUsername,
        string reason,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var summary = string.Empty;

        switch (entityType)
        {
            case "LeaveRequest":
                var lv = await context.LeaveRequests
                    .Include(l => l.LeaveType)
                    .FirstOrDefaultAsync(l => l.Id == entityId, cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy đơn nghỉ phép để hoàn trả.");

                bool wasApproved = lv.Status == "Approved";
                lv.Status = "Cancelled";
                lv.CancelReason = reason;
                lv.CancelledAt = now;
                lv.CancelledByUsername = actorUsername;

                if (string.Equals(lv.LeaveType.LeaveTypeCode, "COMP_LEAVE", StringComparison.OrdinalIgnoreCase))
                {
                    var annualType = await context.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(l => l.LeaveTypeCode == "ANNUAL", cancellationToken);
                    if (annualType != null)
                    {
                        var compBal = await context.LeaveBalances
                            .FirstOrDefaultAsync(b => b.EmployeeId == lv.EmployeeId && b.LeaveTypeId == annualType.Id && b.Year == lv.StartDate.Year, cancellationToken);
                        if (compBal != null)
                        {
                            var statutory = await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(context, lv.StartDate, cancellationToken);
                            decimal hoursToRestore = lv.RequestedHours ?? Math.Round(lv.TotalDays * statutory.MaxNormalDailyHours, 2, MidpointRounding.AwayFromZero);
                            compBal.CompensatoryHoursBalance += hoursToRestore;
                            summary = $"Hoàn {hoursToRestore:0.##}h nghỉ bù";
                        }
                    }
                }
                else
                {
                    var bal = await context.LeaveBalances
                        .FirstOrDefaultAsync(b => b.EmployeeId == lv.EmployeeId && b.LeaveTypeId == lv.LeaveTypeId && b.Year == lv.StartDate.Year, cancellationToken);

                    if (bal != null)
                    {
                        if (wasApproved)
                        {
                            bal.UsedDays = Math.Max(0m, bal.UsedDays - lv.TotalDays);
                            decimal restoreCarry = Math.Min(bal.UsedCarriedForwardDays, lv.TotalDays);
                            bal.UsedCarriedForwardDays = Math.Max(0m, bal.UsedCarriedForwardDays - restoreCarry);
                            bal.AdvanceUsedDays = Math.Max(0m, bal.AdvanceUsedDays - lv.TotalDays);
                        }
                        else
                        {
                            bal.PendingDays = Math.Max(0m, bal.PendingDays - lv.TotalDays);
                        }

                        var resolvedPolicy = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(context, lv.EmployeeId, lv.StartDate, cancellationToken);
                        var (_, _, available, _) = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(bal, resolvedPolicy.Policy, lv.StartDate);
                        bal.RemainingDays = Math.Max(0m, available);
                        summary = $"Hoàn {lv.TotalDays:0.#} ngày phép vào quỹ năm {lv.StartDate.Year} (Còn lại: {bal.RemainingDays:0.#} ngày)";
                    }
                }

                if (wasApproved)
                {
                    var dailies = await context.DailyAttendances
                        .Where(d => d.EmployeeId == lv.EmployeeId && d.AttendanceDate >= lv.StartDate && d.AttendanceDate <= lv.EndDate && d.EditReason != null && d.EditReason.Contains($"#{lv.Id}"))
                        .ToListAsync(cancellationToken);
                    foreach (var d in dailies)
                    {
                        d.AttendanceSymbol = d.CheckInTime.HasValue ? "X" : "KL";
                        d.WorkdayCount = d.CheckInTime.HasValue ? 1m : 0m;
                        d.PaidLeaveDays = 0m;
                        d.UnpaidLeaveDays = 0m;
                        d.EditReason = $"[Đã thu hồi đơn #{lv.Id}: {reason}]";
                    }

                    await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(context, lv.StartDate.Month, lv.StartDate.Year, cancellationToken);
                }
                break;

            case "OvertimeRequest":
                var ot = await context.OvertimeRequests.FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy đơn OT.");
                bool otWasApproved = ot.Status == "Approved";
                ot.Status = "Cancelled";
                ot.CancelReason = reason;
                ot.CancelledAt = now;
                ot.CancelledByUsername = actorUsername;

                if (otWasApproved && ot.CompensatoryHoursCredited > 0m)
                {
                    var annualType = await context.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(l => l.LeaveTypeCode == "ANNUAL", cancellationToken);
                    if (annualType != null)
                    {
                        var bal = await context.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == ot.EmployeeId && b.LeaveTypeId == annualType.Id && b.Year == ot.OvertimeDate.Year, cancellationToken);
                        if (bal != null)
                            bal.CompensatoryHoursBalance = Math.Max(0m, bal.CompensatoryHoursBalance - ot.CompensatoryHoursCredited);
                    }
                    summary = $"Thu hồi {ot.CompensatoryHoursCredited:0.##}h nghỉ bù từ OT #{ot.Id}";
                }
                else
                {
                    summary = $"Đã hủy đơn OT #{ot.Id} ({ot.OvertimeDate:dd/MM/yyyy})";
                }

                if (otWasApproved)
                {
                    var daily = await context.DailyAttendances.FirstOrDefaultAsync(d => d.EmployeeId == ot.EmployeeId && d.AttendanceDate == ot.OvertimeDate, cancellationToken);
                    if (daily != null)
                    {
                        daily.OvertimeHours150 = Math.Max(0m, daily.OvertimeHours150 - (ot.OvertimeRateType == "OT150" ? ot.ApprovedHours : 0m));
                        daily.OvertimeHours200 = Math.Max(0m, daily.OvertimeHours200 - (ot.OvertimeRateType == "OT200" ? ot.ApprovedHours : 0m));
                        daily.OvertimeHours300 = Math.Max(0m, daily.OvertimeHours300 - (ot.OvertimeRateType == "OT300" ? ot.ApprovedHours : 0m));
                    }
                    await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(context, ot.OvertimeDate.Month, ot.OvertimeDate.Year, cancellationToken);
                }
                break;

            case "EmployeeTimeRequest":
                var et = await context.EmployeeTimeRequests.FirstOrDefaultAsync(t => t.Id == entityId, cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy đơn chấm công.");
                bool etWasApproved = et.Status == "Approved";
                et.Status = "Cancelled";
                et.CancelReason = reason;
                et.CancelledAt = now;
                et.CancelledByUsername = actorUsername;
                summary = $"Đã hủy đơn {et.RequestType} #{et.Id}";
                if (etWasApproved)
                    await AttendanceBoardSeeder.RecalculateSummariesInternalAsync(context, et.StartDate.Month, et.StartDate.Year, cancellationToken);
                break;

            case "ShiftSwapRequest":
                var sw = await context.ShiftSwapRequests.FirstOrDefaultAsync(s => s.Id == entityId, cancellationToken)
                    ?? throw new KeyNotFoundException("Không tìm thấy đơn đổi ca.");
                bool swWasApproved = sw.Status == "Approved";
                sw.Status = "Cancelled";
                sw.CancelReason = reason;
                sw.CancelledAt = now;
                sw.CancelledByUsername = actorUsername;

                if (swWasApproved)
                {
                    var reqSched = await context.ShiftSchedules.FirstOrDefaultAsync(s => s.EmployeeId == sw.RequesterEmployeeId && s.WorkDate == sw.RequesterWorkDate, cancellationToken);
                    var tgtSched = await context.ShiftSchedules.FirstOrDefaultAsync(s => s.EmployeeId == sw.TargetEmployeeId && s.WorkDate == sw.TargetWorkDate, cancellationToken);
                    if (reqSched != null && sw.RequesterOriginalShiftId.HasValue)
                    {
                        reqSched.WorkShiftId = sw.RequesterOriginalShiftId.Value;
                        reqSched.Notes = $"Khôi phục ca gốc sau hủy đổi ca #{sw.Id}";
                    }
                    if (tgtSched != null && sw.TargetOriginalShiftId.HasValue)
                    {
                        tgtSched.WorkShiftId = sw.TargetOriginalShiftId.Value;
                        tgtSched.Notes = $"Khôi phục ca gốc sau hủy đổi ca #{sw.Id}";
                    }
                }
                summary = $"Đã hủy đơn đổi ca #{sw.Id}";
                break;
        }

        await context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "TimeAttendance",
            ActionType = "Request_CancelOrRevoke",
            EntityName = entityType,
            EntityId = entityId.ToString(),
            NewValuesJson = $"{{\"Reason\":\"{reason.Replace("\"", "'")}\",\"Summary\":\"{summary.Replace("\"", "'")}\"}}",
            CreatedAt = now,
            CreatedBy = actorUsername
        }, cancellationToken);

        return summary;
    }
}

// ============================================================================
// 7. COMMAND: THANH TOÁN TIỀN PHÉP CHƯA NGHỈ KHI NGHỈ VIỆC (ĐẨY SANG LƯƠNG)
// ============================================================================

public record CalculateAndPushTerminationLeavePayoutCommand(
    int EmployeeId,
    DateOnly TerminationDate,
    int TargetPayrollMonth,
    int TargetPayrollYear,
    decimal StandardWorkdays = 26m,
    string? Notes = null) : IRequest<TerminationLeaveSettlementDto>;

public class CalculateAndPushTerminationLeavePayoutCommandHandler
    : IRequestHandler<CalculateAndPushTerminationLeavePayoutCommand, TerminationLeaveSettlementDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CalculateAndPushTerminationLeavePayoutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TerminationLeaveSettlementDto> Handle(CalculateAndPushTerminationLeavePayoutCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        if (request.StandardWorkdays <= 0m)
            throw new InvalidOperationException("Số ngày công chuẩn phải lớn hơn 0.");

        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        var targetPeriod = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Year == request.TargetPayrollYear && p.Month == request.TargetPayrollMonth, cancellationToken);
        if (targetPeriod != null && (targetPeriod.IsLocked || targetPeriod.Status is "Locked" or "Paid"))
            throw new InvalidOperationException($"Kỳ lương {request.TargetPayrollMonth:D2}/{request.TargetPayrollYear} đã khóa/chi trả, không thể đẩy quyết toán phép chưa nghỉ trực tiếp.");

        var statutory = await AttendancePolicyAndShiftEngine.ResolveStatutoryParameterAsync(_context, request.TerminationDate, cancellationToken);
        var resolved = await AttendancePolicyAndShiftEngine.ResolveAttendancePolicyAsync(_context, emp.Id, request.TerminationDate, cancellationToken);

        var (_, proRatedEntitlement, seniorityExtra) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(
            emp.HireDate, request.TerminationDate.Year, statutory, resolved.Policy, request.TerminationDate);

        var annualType = await _context.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(l => l.LeaveTypeCode == "ANNUAL", cancellationToken);
        var balance = annualType != null
            ? await _context.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == emp.Id && b.LeaveTypeId == annualType.Id && b.Year == request.TerminationDate.Year, cancellationToken)
            : null;

        decimal usedDays = balance?.UsedDays ?? 0m;
        decimal validCarryForward = 0m;
        if (balance != null)
        {
            var (validCarry, _, _, _) = VietnamAnnualLeavePolicyCalculator.ComputeAvailableBalance(balance, resolved.Policy, request.TerminationDate);
            validCarryForward = validCarry;
        }

        decimal unusedPayableDays = Math.Max(0m, proRatedEntitlement + seniorityExtra + validCarryForward - usedDays);
        decimal dailySalary = emp.BaseSalary / request.StandardWorkdays;
        decimal payoutAmount = decimal.Round(dailySalary * unusedPayableDays, 0, MidpointRounding.AwayFromZero);

        var settlement = await _context.TerminationLeaveSettlements
            .FirstOrDefaultAsync(s => s.EmployeeId == emp.Id && s.SettlementYear == request.TerminationDate.Year, cancellationToken);

        if (settlement == null)
        {
            settlement = new TerminationLeaveSettlement
            {
                EmployeeId = emp.Id,
                SettlementYear = request.TerminationDate.Year
            };
            await _context.TerminationLeaveSettlements.AddAsync(settlement, cancellationToken);
        }

        settlement.TerminationDate = request.TerminationDate;
        settlement.ProRatedEntitlementDays = proRatedEntitlement;
        settlement.SeniorityExtraDays = seniorityExtra;
        settlement.ValidCarryForwardDays = validCarryForward;
        settlement.UsedDays = usedDays;
        settlement.UnusedPayableDays = unusedPayableDays;
        settlement.BaseSalaryUsed = emp.BaseSalary;
        settlement.StandardWorkdaysUsed = request.StandardWorkdays;
        settlement.PayoutAmount = payoutAmount;
        settlement.TargetPayrollMonth = request.TargetPayrollMonth;
        settlement.TargetPayrollYear = request.TargetPayrollYear;
        settlement.PayrollPeriodId = targetPeriod?.Id;
        settlement.Status = "PushedToPayroll";
        settlement.Notes = string.IsNullOrWhiteSpace(request.Notes)
            ? $"Thanh toán {unusedPayableDays:0.#} ngày phép chưa nghỉ (Điều 113 BLLĐ 2019) vào kỳ lương {request.TargetPayrollMonth:D2}/{request.TargetPayrollYear}"
            : request.Notes.Trim();

        emp.TerminationDate = request.TerminationDate;
        if (emp.WorkStatus is "Official" or "Probation" or "Active")
            emp.WorkStatus = "Resigned";

        if (balance != null)
        {
            balance.TerminationPayoutDays = unusedPayableDays;
            balance.TerminationPayoutAmount = payoutAmount;
            balance.IsTerminationSettled = true;
            balance.TerminationSettledAt = DateTime.UtcNow;
            balance.TerminationPayrollPeriodId = targetPeriod?.Id;
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "TimeAttendance",
            ActionType = "TerminationLeavePayout_PushToPayroll",
            EntityName = nameof(TerminationLeaveSettlement),
            EntityId = emp.Id.ToString(),
            NewValuesJson = $"{{\"EmployeeCode\":\"{emp.EmployeeCode}\",\"UnusedPayableDays\":{unusedPayableDays},\"PayoutAmount\":{payoutAmount},\"TargetPeriod\":\"{request.TargetPayrollYear}-{request.TargetPayrollMonth:D2}\"}}",
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new TerminationLeaveSettlementDto(
            settlement.Id,
            emp.Id,
            emp.EmployeeCode,
            emp.FullName,
            settlement.TerminationDate,
            settlement.ProRatedEntitlementDays,
            settlement.SeniorityExtraDays,
            settlement.ValidCarryForwardDays,
            settlement.UsedDays,
            settlement.UnusedPayableDays,
            settlement.BaseSalaryUsed,
            settlement.StandardWorkdaysUsed,
            settlement.PayoutAmount,
            settlement.TargetPayrollMonth,
            settlement.TargetPayrollYear,
            settlement.Status,
            settlement.Notes);
    }
}
