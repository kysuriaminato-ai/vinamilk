using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Application.Analytics;

#region ESS / MSS Portal

public sealed record EssProfileDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    string PositionName,
    string WorkEmail,
    string PhoneNumber,
    string WorkStatus,
    DateOnly HireDate,
    string DirectManagerName);

public sealed record EssPayslipDto(
    long PayrollRecordId,
    string PeriodCode,
    string PeriodTitle,
    decimal PayableWorkdays,
    decimal GrossIncome,
    decimal TotalEmployeeInsurance,
    decimal PersonalIncomeTax,
    decimal NetSalary,
    string PaymentStatus,
    string PeriodStatus,
    decimal StandardWorkdays = 22m,
    decimal ActualBaseSalary = 0m,
    decimal AllowanceTotal = 0m,
    decimal OvertimePay = 0m,
    decimal BonusTotal = 0m,
    decimal FamilyDeductionTotal = 0m,
    decimal AssessableIncome = 0m,
    bool IsRecomputedReference = false,
    IReadOnlyList<Vinamilk.HRM.Application.Payroll.CalculationTraceStepDto>? CondensedSteps = null);

public sealed record EssAttendanceSummaryDto(
    int Month,
    int Year,
    decimal ActualWorkdays,
    decimal PayableWorkdays,
    decimal TotalOtHours,
    int LateCount,
    decimal RemainingAnnualLeave);

public sealed record EssRequestItemDto(
    string RequestType,
    string Code,
    string PeriodText,
    string Reason,
    string Status,
    DateTime CreatedAt);

public sealed record MssPendingApprovalDto(
    int WorkflowInstanceId,
    string EntityType,
    string DocumentTitle,
    string InitiatorName,
    string StepName,
    DateTime CreatedAt);

public sealed record EssLeaveTypeOptionDto(int Id, string LeaveName);

public sealed record EssDailyAttendanceRowDto(
    DateOnly AttendanceDate,
    string ShiftName,
    string AttendanceSymbol,
    DateTime? CheckInTime,
    DateTime? CheckOutTime,
    decimal WorkingHours,
    decimal WorkdayCount,
    decimal OvertimeHours,
    int LateMinutes,
    int EarlyLeaveMinutes,
    decimal PaidLeaveDays,
    decimal UnpaidLeaveDays);

public sealed record EssLeaveBalanceRowDto(
    string LeaveTypeCode,
    string LeaveTypeName,
    decimal StandardEntitlement,
    decimal SeniorityExtraDays,
    decimal CarriedForwardDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal RemainingDays,
    decimal CompensatoryHoursBalance);

public sealed record EssAnnualPitSummaryDto(
    int TaxYear,
    int MonthsPaid,
    decimal TotalGrossIncome,
    decimal TotalInsuranceDeducted,
    decimal TotalPitWithheld,
    string? IssuedCertificateCode);

public sealed record EssMssPortalDto(
    EssProfileDto Profile,
    EssPayslipDto? LatestPayslip,
    List<EssPayslipDto> Payslips,
    EssAttendanceSummaryDto CurrentAttendance,
    List<EssRequestItemDto> MyRequests,
    List<MssPendingApprovalDto> PendingApprovals,
    List<EssLeaveTypeOptionDto> LeaveTypes,
    bool IsManager,
    EssSalaryAdvanceQuotaDto? AdvanceQuota = null,
    List<EssDailyAttendanceRowDto>? MonthlyTimesheet = null,
    List<EssLeaveBalanceRowDto>? LeaveBalanceDetails = null,
    EssAnnualPitSummaryDto? AnnualPitSummary = null);

public sealed record GetEssMssPortalQuery(
    int? EmployeeId = null,
    int? TimesheetYear = null,
    int? TimesheetMonth = null,
    int? PitYear = null) : IRequest<EssMssPortalDto>;

public class GetEssMssPortalQueryHandler : IRequestHandler<GetEssMssPortalQuery, EssMssPortalDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetEssMssPortalQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<EssMssPortalDto> Handle(GetEssMssPortalQuery request, CancellationToken cancellationToken)
    {
        var scope = await Common.Security.DataScopeResolver.ResolveAsync(_context, _currentUserService, cancellationToken);

        var employeeQuery = _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Include(e => e.DirectManager)
            .AsQueryable();

        Domain.Entities.Core.Employee? ownEmp = null;
        if (scope.CurrentEmployeeId.HasValue && scope.CurrentEmployeeId.Value > 0)
        {
            ownEmp = await employeeQuery.FirstOrDefaultAsync(e => e.Id == scope.CurrentEmployeeId.Value, cancellationToken);
        }
        else if (_currentUserService.UserId.HasValue)
        {
            ownEmp = await employeeQuery.FirstOrDefaultAsync(
                e => e.UserAccount != null && e.UserAccount.Id == _currentUserService.UserId.Value, cancellationToken);
        }

        Domain.Entities.Core.Employee? targetEmp = null;
        if (request.EmployeeId.HasValue && request.EmployeeId > 0)
        {
            var requestedEmp = await employeeQuery.FirstOrDefaultAsync(e => e.Id == request.EmployeeId.Value, cancellationToken);
            if (_currentUserService.IsAuthenticated)
            {
                bool isSelf = ownEmp is not null && ownEmp.Id == request.EmployeeId.Value;
                if (!isSelf)
                {
                    bool canViewOthers = _currentUserService.HasPermission("Employees.View")
                        || _currentUserService.HasPermission("Payroll.View");
                    if (!canViewOthers)
                    {
                        throw new UnauthorizedAccessException("Bạn chỉ được phép xem hồ sơ và phiếu lương ESS của chính mình.");
                    }

                    if (requestedEmp is null)
                    {
                        if (!scope.IsUnrestricted)
                            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hồ sơ nhân sự ngoài phạm vi quản lý.");
                    }
                    else
                    {
                        scope.EnsureCanAccessEmployee(requestedEmp.Id, requestedEmp.OrganizationUnitId);
                    }
                }
            }

            targetEmp = requestedEmp;
        }

        targetEmp ??= ownEmp;

        if (targetEmp is null && _currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Tài khoản hiện tại chưa được liên kết với hồ sơ nhân sự.");
        }

        targetEmp ??= await employeeQuery
            .Where(e => e.WorkStatus != "Terminated")
            .OrderBy(e => e.Id)
            .FirstAsync(cancellationToken);

        var profile = new EssProfileDto(
            targetEmp.Id,
            targetEmp.EmployeeCode,
            targetEmp.FullName,
            targetEmp.OrganizationUnit.UnitName,
            targetEmp.Position.PositionName,
            targetEmp.WorkEmail ?? "chua-cap-nhat@vinamilk.com.vn",
            targetEmp.PhoneNumber ?? "Chưa cập nhật",
            targetEmp.WorkStatus,
            targetEmp.HireDate,
            targetEmp.DirectManager?.FullName ?? "Ban Tổng Giám đốc");

        var payrollList = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Where(r => r.EmployeeId == targetEmp.Id &&
                        (r.PayrollPeriod.Status == "Approved" ||
                         r.PayrollPeriod.Status == "Locked" ||
                         r.PayrollPeriod.Status == "Paid"))
            .OrderByDescending(r => r.PayrollPeriod.Year)
            .ThenByDescending(r => r.PayrollPeriod.Month)
            .Take(12)
            .ToListAsync(cancellationToken);

        var wageRegion = targetEmp.WageRegion ?? targetEmp.OrganizationUnit.WageRegion;
        var payslips = new List<EssPayslipDto>(payrollList.Count);
        foreach (var p in payrollList)
        {
            IReadOnlyList<Vinamilk.HRM.Application.Payroll.CalculationTraceStepDto>? steps = null;
            bool isRecomputed = false;
            if (payslips.Count == 0)
            {
                var resolved = await Vinamilk.HRM.Application.Payroll.PayrollTraceHelper.ResolveOrRecomputeAsync(
                    _context, p, p.PayrollPeriod.EndDate, wageRegion, cancellationToken);
                steps = resolved.Steps;
                isRecomputed = resolved.IsRecomputedReference;
            }

            payslips.Add(new EssPayslipDto(
                p.Id,
                p.PayrollPeriod.PeriodCode,
                p.PayrollPeriod.PeriodTitle,
                p.PayableWorkdays,
                p.GrossIncome,
                p.TotalEmployeeInsurance,
                p.PersonalIncomeTax,
                p.NetSalary,
                p.PaymentStatus,
                p.PayrollPeriod.Status,
                p.StandardWorkdays,
                p.ActualBaseSalary,
                p.PositionAllowance + p.OtherAllowance,
                p.OvertimePay,
                p.KpiBonus + p.CommissionBonus + p.ThirteenthMonthBonus,
                p.SelfDeductionAmount + p.DependentDeductionAmount,
                p.AssessableIncome,
                isRecomputed,
                steps));
        }

        var latestPayslip = payslips.FirstOrDefault();

        var now = DateTime.Today;
        var attendance = await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .Where(a => a.EmployeeId == targetEmp.Id && a.Year == now.Year && a.Month == now.Month)
            .FirstOrDefaultAsync(cancellationToken);

        attendance ??= await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .Where(a => a.EmployeeId == targetEmp.Id)
            .OrderByDescending(a => a.Year)
            .ThenByDescending(a => a.Month)
            .FirstOrDefaultAsync(cancellationToken);

        var leaveBalance = await _context.LeaveBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == targetEmp.Id && b.Year == now.Year)
            .SumAsync(b => (decimal?)b.RemainingDays, cancellationToken) ?? 12m;

        var currentAttendance = new EssAttendanceSummaryDto(
            attendance?.Month ?? now.Month,
            attendance?.Year ?? now.Year,
            attendance?.ActualWorkdays ?? 0m,
            attendance?.TotalPayableWorkdays ?? 0m,
            attendance is null ? 0m : attendance.TotalOtHours150 + attendance.TotalOtHours200 + attendance.TotalOtHours300,
            attendance?.TotalLateCount ?? 0,
            leaveBalance);

        var leaveRequests = await _context.LeaveRequests
            .AsNoTracking()
            .Include(l => l.LeaveType)
            .Where(l => l.EmployeeId == targetEmp.Id)
            .OrderByDescending(l => l.CreatedAt)
            .Take(6)
            .Select(l => new EssRequestItemDto(
                "Nghỉ phép",
                l.LeaveType.LeaveTypeName,
                $"{l.StartDate:dd/MM/yyyy} - {l.EndDate:dd/MM/yyyy} ({l.TotalDays:0.#} ngày)",
                l.Reason ?? string.Empty,
                l.Status,
                l.CreatedAt))
            .ToListAsync(cancellationToken);

        var otRequests = await _context.OvertimeRequests
            .AsNoTracking()
            .Where(o => o.EmployeeId == targetEmp.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Take(6)
            .Select(o => new EssRequestItemDto(
                "Tăng ca (OT)",
                o.OvertimeRateType,
                $"{o.OvertimeDate:dd/MM/yyyy} ({o.StartTime:HH\\:mm}-{o.EndTime:HH\\:mm})",
                o.Reason ?? string.Empty,
                o.Status,
                o.CreatedAt))
            .ToListAsync(cancellationToken);

        var advRequests = await _context.SalaryAdvances
            .AsNoTracking()
            .Where(a => a.EmployeeId == targetEmp.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Take(6)
            .Select(a => new EssRequestItemDto(
                "Tạm ứng lương",
                a.AdvanceNumber,
                $"Kỳ {a.TargetMonth:D2}/{a.TargetYear} ({a.Amount:N0} đ)",
                a.Reason,
                a.Status,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        var myRequests = leaveRequests.Concat(otRequests).Concat(advRequests)
            .OrderByDescending(r => r.CreatedAt)
            .Take(12)
            .ToList();

        var advanceQuota = await new GetEssSalaryAdvanceQuotaQueryHandler(_context)
            .Handle(new GetEssSalaryAdvanceQuotaQuery(targetEmp.Id), cancellationToken);

        var subordinateCount = await _context.Employees
            .CountAsync(e => e.DirectManagerId == targetEmp.Id, cancellationToken);
        var isManager = subordinateCount > 0
            || _currentUserService.HasPermission("Leave.Approve")
            || _currentUserService.HasPermission("Attendance.Approve");

        var pendingWorkflowsQuery = _context.WorkflowInstances
            .AsNoTracking()
            .Include(w => w.WorkflowDefinition)
            .Include(w => w.InitiatorEmployee)
            .Where(w => w.Status == "InProgress" || w.Status == "Pending");

        if (!scope.IsUnrestricted)
        {
            var allowedUnitIds = scope.AllowedUnitIdList;
            pendingWorkflowsQuery = pendingWorkflowsQuery.Where(w =>
                w.InitiatorEmployeeId == targetEmp.Id ||
                (isManager && allowedUnitIds.Contains(w.InitiatorEmployee.OrganizationUnitId)));
        }

        var pendingWorkflows = await pendingWorkflowsQuery
            .OrderByDescending(w => w.CreatedAt)
            .Take(15)
            .Select(w => new MssPendingApprovalDto(
                w.Id,
                w.EntityType,
                w.WorkflowDefinition.WorkflowName + " #" + w.EntityId,
                w.InitiatorEmployee.FullName,
                $"Bước {w.CurrentStepOrder}",
                w.CreatedAt))
            .ToListAsync(cancellationToken);

        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new EssLeaveTypeOptionDto(t.Id, t.LeaveTypeName))
            .ToListAsync(cancellationToken);

        var targetTsYear = request.TimesheetYear ?? currentAttendance.Year;
        var targetTsMonth = request.TimesheetMonth ?? currentAttendance.Month;
        var tsStart = new DateOnly(targetTsYear, targetTsMonth, 1);
        var tsEnd = new DateOnly(targetTsYear, targetTsMonth, DateTime.DaysInMonth(targetTsYear, targetTsMonth));

        var monthlyTimesheet = await _context.DailyAttendances
            .AsNoTracking()
            .Include(d => d.WorkShift)
            .Where(d => d.EmployeeId == targetEmp.Id && d.AttendanceDate >= tsStart && d.AttendanceDate <= tsEnd)
            .OrderBy(d => d.AttendanceDate)
            .Select(d => new EssDailyAttendanceRowDto(
                d.AttendanceDate,
                d.WorkShift != null ? d.WorkShift.ShiftName : "Ca hành chính",
                d.AttendanceSymbol,
                d.CheckInTime,
                d.CheckOutTime,
                d.WorkingHours,
                d.WorkdayCount,
                d.OvertimeHours150 + d.OvertimeHours200 + d.OvertimeHours300,
                d.LateMinutes,
                d.EarlyLeaveMinutes,
                d.PaidLeaveDays,
                d.UnpaidLeaveDays))
            .ToListAsync(cancellationToken);

        var leaveBalanceDetails = await _context.LeaveBalances
            .AsNoTracking()
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == targetEmp.Id && b.Year == targetTsYear)
            .OrderBy(b => b.LeaveType.DisplayOrder)
            .ThenBy(b => b.LeaveTypeId)
            .Select(b => new EssLeaveBalanceRowDto(
                b.LeaveType.LeaveTypeCode,
                b.LeaveType.LeaveTypeName,
                b.StandardEntitlement,
                b.SeniorityExtraDays,
                b.CarriedForwardDays,
                b.UsedDays,
                b.PendingDays,
                b.RemainingDays,
                b.CompensatoryHoursBalance))
            .ToListAsync(cancellationToken);

        var pitYear = request.PitYear ?? payrollList.FirstOrDefault()?.PayrollPeriod.Year ?? now.Year;
        var yearPayrollRecords = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Where(r => r.EmployeeId == targetEmp.Id
                        && r.PayrollPeriod.Year == pitYear
                        && (r.PayrollPeriod.Status == "Approved"
                            || r.PayrollPeriod.Status == "Locked"
                            || r.PayrollPeriod.Status == "Paid"))
            .ToListAsync(cancellationToken);

        var existingCert = await _context.PitWithholdingCertificates
            .AsNoTracking()
            .Where(c => c.EmployeeId == targetEmp.Id && c.TaxYear == pitYear && c.Status == "Issued")
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var annualPitSummary = new EssAnnualPitSummaryDto(
            pitYear,
            yearPayrollRecords.Select(r => r.PayrollPeriod.Month).Distinct().Count(),
            yearPayrollRecords.Where(r => r.PayrollPeriod.PeriodType != "MidMonthAdvance").Sum(r => r.GrossIncome),
            yearPayrollRecords.Where(r => r.PayrollPeriod.PeriodType != "MidMonthAdvance").Sum(r => r.TotalEmployeeInsurance),
            yearPayrollRecords.Sum(r => r.PersonalIncomeTax),
            existingCert?.CertificateCode);

        return new EssMssPortalDto(
            profile,
            latestPayslip,
            payslips,
            currentAttendance,
            myRequests,
            pendingWorkflows,
            leaveTypes,
            isManager,
            advanceQuota,
            monthlyTimesheet,
            leaveBalanceDetails,
            annualPitSummary);
    }
}

#endregion

#region AI Expert (Swappable IAiHrmExpertService + RAG + Matching + Attrition)

public sealed record AiRagCitationDto(string SourceCode, string Title, string Excerpt);

public sealed record AiPolicyAnswerDto(
    string Question,
    string Answer,
    string ProviderName,
    List<AiRagCitationDto> Citations);

public sealed record CandidateJobMatchDto(
    int CandidateId,
    string CandidateName,
    string Email,
    int JobPostingId,
    string JobTitle,
    int MatchScore,
    string MatchReason);

public sealed record AttritionRiskItemDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    int RiskScore,
    string RiskLevel,
    List<string> RiskFactors,
    string RecommendedAction);

public sealed record AiRecommendationRecordDto(
    long Id,
    string RecommendationType,
    string TargetEntityType,
    long TargetEntityId,
    string Title,
    string Description,
    decimal? ConfidenceScore,
    string Status,
    DateTime CreatedAt);

public sealed record AiExpertOverviewDto(
    string ActiveProvider,
    List<CandidateJobMatchDto> CandidateMatches,
    List<AttritionRiskItemDto> AttritionRisks,
    List<AiRecommendationRecordDto> SavedRecommendations);

/// <summary>
/// Abstraction layer allowing seamless swap between Local RAG, Azure OpenAI, OpenAI, or Gemini.
/// </summary>
public interface IAiHrmExpertService
{
    string ProviderName { get; }
    Task<AiPolicyAnswerDto> AskPolicyAsync(string question, CancellationToken cancellationToken = default);
    Task<List<CandidateJobMatchDto>> MatchCandidatesAsync(int? jobPostingId = null, CancellationToken cancellationToken = default);
    Task<List<AttritionRiskItemDto>> AnalyzeAttritionRisksAsync(CancellationToken cancellationToken = default);
}

public sealed record AskAiPolicyRagQuery(string Question) : IRequest<AiPolicyAnswerDto>;

public class AskAiPolicyRagQueryHandler : IRequestHandler<AskAiPolicyRagQuery, AiPolicyAnswerDto>
{
    private readonly IAiHrmExpertService _aiService;
    public AskAiPolicyRagQueryHandler(IAiHrmExpertService aiService) => _aiService = aiService;

    public Task<AiPolicyAnswerDto> Handle(AskAiPolicyRagQuery request, CancellationToken cancellationToken)
        => _aiService.AskPolicyAsync(request.Question, cancellationToken);
}

public sealed record GetAiExpertOverviewQuery(int? JobPostingId = null) : IRequest<AiExpertOverviewDto>;

public class GetAiExpertOverviewQueryHandler : IRequestHandler<GetAiExpertOverviewQuery, AiExpertOverviewDto>
{
    private readonly IAiHrmExpertService _aiService;
    private readonly IApplicationDbContext _context;

    public GetAiExpertOverviewQueryHandler(IAiHrmExpertService aiService, IApplicationDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }

    public async Task<AiExpertOverviewDto> Handle(GetAiExpertOverviewQuery request, CancellationToken cancellationToken)
    {
        var matches = await _aiService.MatchCandidatesAsync(request.JobPostingId, cancellationToken);
        var risks = await _aiService.AnalyzeAttritionRisksAsync(cancellationToken);

        var saved = await _context.AiRecommendations
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new AiRecommendationRecordDto(
                r.Id,
                r.RecommendationType,
                "Employee",
                r.EmployeeId ?? 0,
                r.Title,
                r.AnalysisContent,
                r.ConfidenceScore,
                r.Status,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AiExpertOverviewDto(_aiService.ProviderName, matches, risks, saved);
    }
}

public sealed record ScanAndPersistAttritionRisksCommand : IRequest<int>;

public class ScanAndPersistAttritionRisksCommandHandler : IRequestHandler<ScanAndPersistAttritionRisksCommand, int>
{
    private readonly IAiHrmExpertService _aiService;
    private readonly IApplicationDbContext _context;

    public ScanAndPersistAttritionRisksCommandHandler(IAiHrmExpertService aiService, IApplicationDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }

    public async Task<int> Handle(ScanAndPersistAttritionRisksCommand request, CancellationToken cancellationToken)
    {
        var risks = await _aiService.AnalyzeAttritionRisksAsync(cancellationToken);
        var highOrMedium = risks.Where(r => r.RiskScore >= 40).Take(5).ToList();

        var added = 0;
        foreach (var item in highOrMedium)
        {
            var title = $"[AI Cảnh báo] Nguy cơ nghỉ việc ({item.RiskLevel}): {item.EmployeeCode} - {item.FullName}";
            var exists = await _context.AiRecommendations
                .AnyAsync(a => a.EmployeeId == item.EmployeeId && a.RecommendationType == "AttritionRisk" && a.Status == "New", cancellationToken);

            if (!exists)
            {
                _context.AiRecommendations.Add(new AiRecommendation
                {
                    EmployeeId = item.EmployeeId,
                    RecommendationType = "AttritionRisk",
                    PriorityLevel = item.RiskLevel == "Cao" ? "High" : "Medium",
                    Title = title,
                    AnalysisContent = string.Join("; ", item.RiskFactors),
                    SuggestedAction = item.RecommendedAction,
                    ConfidenceScore = item.RiskScore,
                    Status = "New"
                });
                added++;
            }
        }

        if (added > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return added;
    }
}

#endregion
