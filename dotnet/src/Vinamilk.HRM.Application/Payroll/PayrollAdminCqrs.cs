using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Application.Payroll;

public sealed record PayrollPeriodRowDto(
    int Id,
    string PeriodCode,
    int Month,
    int Year,
    string PeriodTitle,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal StandardWorkdays,
    string Status,
    bool IsLocked,
    int CalculationVersion,
    int? CalculatedByUserId,
    int? WorkflowInstanceId,
    int EmployeeCount,
    decimal TotalGrossIncome,
    decimal TotalPersonalIncomeTax,
    decimal TotalNetSalary);

public sealed record PayrollRecordRowDto(
    long Id,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    int WageRegion,
    decimal StandardWorkdays,
    decimal PayableWorkdays,
    decimal ContractBaseSalary,
    decimal OvertimePay,
    decimal GrossIncome,
    decimal InsuranceSalaryBase,
    decimal TotalEmployeeInsurance,
    decimal SelfDeductionAmount,
    decimal DependentDeductionAmount,
    decimal AssessableIncome,
    decimal PersonalIncomeTax,
    decimal NetSalary,
    decimal? PreviousRevisionNetSalary,
    decimal? PreviousPeriodNetSalary,
    decimal? VariancePercentVsPreviousPeriod,
    bool HasHighVarianceWarning,
    string PaymentStatus);

public sealed record PayrollCalculationRevisionDto(
    long Id,
    int RevisionNumber,
    string Scope,
    int? EmployeeId,
    int AffectedEmployeeCount,
    decimal TotalGrossIncome,
    decimal TotalPersonalIncomeTax,
    decimal TotalNetSalary,
    decimal NetDeltaFromPreviousRevision,
    int HighVarianceWarningCount,
    string? CalculatedByUsername,
    DateTime CalculatedAt,
    string? Notes);

public sealed record PayrollStatutoryParameterDto(
    int Id,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? LegalReference,
    string? Notes,
    decimal StatutoryBaseSalary,
    decimal MinWageRegion1,
    decimal MinWageRegion2,
    decimal MinWageRegion3,
    decimal MinWageRegion4,
    decimal SocialInsuranceCapMultiplier,
    decimal UnemploymentInsuranceCapMultiplier,
    decimal EmployeeSiRate,
    decimal EmployeeHiRate,
    decimal EmployeeUiRate,
    decimal CompanySiRate,
    decimal CompanyHiRate,
    decimal CompanyUiRate,
    decimal CompanyUnionFeeRate,
    decimal SelfDeduction,
    decimal DependentDeduction,
    bool HasLockedPayrollPeriod);

public sealed record PitBracketGroupDto(
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? LegalReference,
    bool HasLockedPayrollPeriod,
    IReadOnlyList<PitBracket> Brackets);

public sealed record PayrollManagementOverviewDto(
    IReadOnlyList<PayrollPeriodRowDto> Periods,
    PayrollPeriodRowDto? SelectedPeriod,
    IReadOnlyList<PayrollRecordRowDto> Records,
    IReadOnlyList<PayrollStatutoryParameterDto> StatutoryParameters,
    IReadOnlyList<PitBracketGroupDto> PitBracketGroups,
    IReadOnlyList<PayrollPeriodRowDto> LockedPeriods,
    IReadOnlyList<PayrollCalculationRevisionDto> Revisions,
    IReadOnlyList<PayrollDepartmentSummaryRowDto> DepartmentSummaryRows,
    PayslipDispatchBatchReportDto? LatestDispatchBatch);

public sealed record GetPayrollManagementOverviewQuery(int? PeriodId = null)
    : IRequest<PayrollManagementOverviewDto>;

public sealed class GetPayrollManagementOverviewQueryHandler
    : IRequestHandler<GetPayrollManagementOverviewQuery, PayrollManagementOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetPayrollManagementOverviewQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PayrollManagementOverviewDto> Handle(
        GetPayrollManagementOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var periodsRaw = await _context.PayrollPeriods
            .AsNoTracking()
            .Include(p => p.PayrollRecords)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync(cancellationToken);

        var periods = periodsRaw.Select(p =>
        {
            var end = p.EndDate != default
                ? p.EndDate
                : new DateOnly(p.Year, p.Month, DateTime.DaysInMonth(p.Year, p.Month));
            return new PayrollPeriodRowDto(
                p.Id,
                p.PeriodCode,
                p.Month,
                p.Year,
                p.PeriodTitle,
                p.StartDate != default ? p.StartDate : new DateOnly(p.Year, p.Month, 1),
                end,
                p.StandardWorkdays,
                p.Status,
                p.IsLocked || string.Equals(p.Status, PayrollPeriodStatuses.Locked, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(p.Status, PayrollPeriodStatuses.Paid, StringComparison.OrdinalIgnoreCase),
                p.CalculationVersion,
                p.CalculatedByUserId,
                p.WorkflowInstanceId,
                p.PayrollRecords.Count,
                p.PayrollRecords.Sum(r => r.GrossIncome),
                p.PayrollRecords.Sum(r => r.PersonalIncomeTax),
                p.PayrollRecords.Sum(r => r.NetSalary));
        }).ToList();

        var selectedPeriod = request.PeriodId.HasValue
            ? periods.FirstOrDefault(p => p.Id == request.PeriodId.Value) ?? periods.FirstOrDefault()
            : periods.FirstOrDefault();

        var records = new List<PayrollRecordRowDto>();
        var revisions = new List<PayrollCalculationRevisionDto>();
        var deptSummaryRows = new List<PayrollDepartmentSummaryRowDto>();
        PayslipDispatchBatchReportDto? latestDispatchBatch = null;

        if (selectedPeriod is not null)
        {
            var rawRecs = await _context.PayrollRecords
                .AsNoTracking()
                .Include(r => r.Employee)
                .Include(r => r.OrganizationUnit)
                .Where(r => r.PayrollPeriodId == selectedPeriod.Id)
                .OrderBy(r => r.Employee.EmployeeCode)
                .ToListAsync(cancellationToken);

            records = rawRecs.Select(r => new PayrollRecordRowDto(
                r.Id,
                r.EmployeeId,
                r.Employee.EmployeeCode,
                r.Employee.FullName,
                r.OrganizationUnit.UnitName,
                r.Employee.WageRegion ?? r.OrganizationUnit.WageRegion,
                r.StandardWorkdays,
                r.PayableWorkdays,
                r.ContractBaseSalary,
                r.OvertimePay,
                r.GrossIncome,
                r.InsuranceSalaryBase,
                r.TotalEmployeeInsurance,
                r.SelfDeductionAmount,
                r.DependentDeductionAmount,
                r.AssessableIncome,
                r.PersonalIncomeTax,
                r.NetSalary,
                r.PreviousRevisionNetSalary,
                r.PreviousPeriodNetSalary,
                r.VariancePercentVsPreviousPeriod,
                r.HasHighVarianceWarning,
                r.PaymentStatus)).ToList();

            deptSummaryRows = rawRecs
                .GroupBy(r => new { r.OrganizationUnit.UnitCode, r.OrganizationUnit.UnitName })
                .Select(g => new PayrollDepartmentSummaryRowDto(
                    g.Key.UnitCode,
                    g.Key.UnitName,
                    g.Count(),
                    g.Sum(x => x.ActualBaseSalary + x.PositionAllowance + x.OtherAllowance),
                    g.Sum(x => x.OvertimePay + x.KpiBonus),
                    g.Sum(x => x.GrossIncome),
                    g.Sum(x => x.TotalEmployeeInsurance),
                    g.Sum(x => x.CompanySocialInsurance + x.CompanyHealthInsurance + x.CompanyUnemploymentInsurance),
                    g.Sum(x => x.CompanyUnionFee),
                    g.Sum(x => x.PersonalIncomeTax),
                    g.Sum(x => x.NetSalary)))
                .OrderByDescending(x => x.TotalGross)
                .ToList();

            var rawRevisions = await _context.PayrollCalculationRevisions
                .AsNoTracking()
                .Where(rev => rev.PayrollPeriodId == selectedPeriod.Id)
                .OrderByDescending(rev => rev.RevisionNumber)
                .Take(15)
                .ToListAsync(cancellationToken);

            revisions = rawRevisions.Select(rev => new PayrollCalculationRevisionDto(
                rev.Id,
                rev.RevisionNumber,
                rev.Scope,
                rev.EmployeeId,
                rev.AffectedEmployeeCount,
                rev.TotalGrossIncome,
                rev.TotalPersonalIncomeTax,
                rev.TotalNetSalary,
                rev.NetDeltaFromPreviousRevision,
                rev.HighVarianceWarningCount,
                rev.CalculatedByUsername,
                rev.CalculatedAt,
                rev.Notes)).ToList();

            var latestLogs = await _context.PayslipDispatchLogs
                .AsNoTracking()
                .Where(l => l.PayrollPeriodId == selectedPeriod.Id)
                .OrderByDescending(l => l.CreatedAt)
                .Take(200)
                .ToListAsync(cancellationToken);

            if (latestLogs.Count > 0)
            {
                var latestBatchId = latestLogs[0].BatchId;
                var batchLogs = latestLogs.Where(l => l.BatchId == latestBatchId).OrderBy(l => l.EmployeeCode).ToList();
                latestDispatchBatch = new PayslipDispatchBatchReportDto(
                    latestBatchId,
                    batchLogs.Count,
                    batchLogs.Count(x => x.Status == "Queued"),
                    batchLogs.Count(x => x.Status == "Sent"),
                    batchLogs.Count(x => x.Status == "Failed"),
                    batchLogs.Select(x => new PayslipDispatchItemDto(
                        x.Id,
                        x.EmployeeCode,
                        x.EmployeeName,
                        x.RecipientEmail,
                        x.Status,
                        x.AttemptCount,
                        x.ErrorMessage,
                        x.SentAt)).ToList());
            }
        }

        var lockedPeriods = periods.Where(p => p.IsLocked).ToList();

        var parametersRaw = await _context.PayrollStatutoryParameters
            .AsNoTracking()
            .OrderByDescending(p => p.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var paramDtos = parametersRaw.Select(p => new PayrollStatutoryParameterDto(
            p.Id,
            p.EffectiveFrom,
            p.EffectiveTo,
            p.LegalReference,
            p.Notes,
            p.StatutoryBaseSalary,
            p.MinWageRegion1,
            p.MinWageRegion2,
            p.MinWageRegion3,
            p.MinWageRegion4,
            p.SocialInsuranceCapMultiplier,
            p.UnemploymentInsuranceCapMultiplier,
            p.EmployeeSiRate,
            p.EmployeeHiRate,
            p.EmployeeUiRate,
            p.CompanySiRate,
            p.CompanyHiRate,
            p.CompanyUiRate,
            p.CompanyUnionFeeRate,
            p.SelfDeduction,
            p.DependentDeduction,
            HasOverlappingLockedPeriod(p.EffectiveFrom, p.EffectiveTo, lockedPeriods))).ToList();

        var bracketsRaw = await _context.PitBrackets
            .AsNoTracking()
            .OrderByDescending(b => b.EffectiveFrom)
            .ThenBy(b => b.BracketLevel)
            .ToListAsync(cancellationToken);

        var bracketGroups = bracketsRaw
            .GroupBy(b => b.EffectiveFrom)
            .Select(g =>
            {
                var effTo = g.First().EffectiveTo;
                var legal = g.First().LegalReference;
                return new PitBracketGroupDto(
                    g.Key,
                    effTo,
                    legal,
                    HasOverlappingLockedPeriod(g.Key, effTo, lockedPeriods),
                    g.OrderBy(x => x.BracketLevel).ToList());
            })
            .ToList();

        return new PayrollManagementOverviewDto(
            periods,
            selectedPeriod,
            records,
            paramDtos,
            bracketGroups,
            lockedPeriods,
            revisions,
            deptSummaryRows,
            latestDispatchBatch);
    }

    public static bool HasOverlappingLockedPeriod(
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        IEnumerable<PayrollPeriodRowDto> lockedPeriods)
    {
        var toDate = effectiveTo ?? DateOnly.MaxValue;
        return lockedPeriods.Any(p => p.EndDate >= effectiveFrom && p.EndDate <= toDate);
    }
}

public sealed record CreatePayrollPeriodCommand(int Year, int Month, decimal StandardWorkdays = 26m)
    : IRequest<int>;

public sealed class CreatePayrollPeriodCommandHandler : IRequestHandler<CreatePayrollPeriodCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreatePayrollPeriodCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<int> Handle(CreatePayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        if (request.Month is < 1 or > 12 || request.Year < 2000)
            throw new ArgumentOutOfRangeException(nameof(request), "Tháng hoặc năm kỳ lương không hợp lệ.");

        var existing = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Year == request.Year && p.Month == request.Month, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var startDate = new DateOnly(request.Year, request.Month, 1);
        var endDate = new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));

        var period = new PayrollPeriod
        {
            PeriodCode = $"PAY-{request.Year}-{request.Month:D2}",
            Year = request.Year,
            Month = request.Month,
            PeriodTitle = $"Kỳ lương Tháng {request.Month:D2}/{request.Year}",
            StartDate = startDate,
            EndDate = endDate,
            StandardWorkdays = request.StandardWorkdays > 0m ? request.StandardWorkdays : 26m,
            Status = "Draft",
            IsLocked = false
        };

        await _context.PayrollPeriods.AddAsync(period, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return period.Id;
    }
}

public sealed record SavePayrollStatutoryParameterCommand(
    int? Id,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? LegalReference,
    string? Notes,
    decimal StatutoryBaseSalary,
    decimal MinWageRegion1,
    decimal MinWageRegion2,
    decimal MinWageRegion3,
    decimal MinWageRegion4,
    decimal SocialInsuranceCapMultiplier,
    decimal UnemploymentInsuranceCapMultiplier,
    decimal EmployeeSiRate,
    decimal EmployeeHiRate,
    decimal EmployeeUiRate,
    decimal CompanySiRate,
    decimal CompanyHiRate,
    decimal CompanyUiRate,
    decimal CompanyUnionFeeRate,
    decimal SelfDeduction,
    decimal DependentDeduction) : IRequest<int>;

public sealed class SavePayrollStatutoryParameterCommandHandler
    : IRequestHandler<SavePayrollStatutoryParameterCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SavePayrollStatutoryParameterCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SavePayrollStatutoryParameterCommand request, CancellationToken cancellationToken)
    {
        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom)
            throw new InvalidOperationException("Ngày hết hiệu lực phải lớn hơn hoặc bằng ngày bắt đầu hiệu lực.");

        if (request.StatutoryBaseSalary <= 0 || request.MinWageRegion1 <= 0 || request.MinWageRegion2 <= 0 ||
            request.MinWageRegion3 <= 0 || request.MinWageRegion4 <= 0)
            throw new InvalidOperationException("Lương cơ sở và lương tối thiểu 4 vùng phải lớn hơn 0.");

        var lockedPeriods = await _context.PayrollPeriods
            .AsNoTracking()
            .Where(p => p.IsLocked || p.Status == PayrollPeriodStatuses.Locked || p.Status == PayrollPeriodStatuses.Paid)
            .ToListAsync(cancellationToken);

        PayrollStatutoryParameter entity;
        string actionType;
        string? oldJson = null;

        if (request.Id.HasValue && request.Id.Value > 0)
        {
            entity = await _context.PayrollStatutoryParameters
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy tham số lương #{request.Id.Value}.");

            if (PayrollPeriodOverlapHelper.FindLockedOverlap(lockedPeriods, entity.EffectiveFrom, entity.EffectiveTo) is { } blockerOld)
            {
                throw new InvalidOperationException(
                    $"Không được sửa tham số lương cho giai đoạn đã có kỳ lương bị khóa ({blockerOld.PeriodCode}).");
            }
            if (PayrollPeriodOverlapHelper.FindLockedOverlap(lockedPeriods, request.EffectiveFrom, request.EffectiveTo) is { } blockerNew)
            {
                throw new InvalidOperationException(
                    $"Không được sửa tham số lương cho giai đoạn đã có kỳ lương bị khóa ({blockerNew.PeriodCode}).");
            }

            actionType = "Update";
            oldJson = JsonSerializer.Serialize(new
            {
                entity.EffectiveFrom,
                entity.EffectiveTo,
                entity.StatutoryBaseSalary,
                entity.MinWageRegion1,
                entity.MinWageRegion2,
                entity.MinWageRegion3,
                entity.MinWageRegion4,
                entity.SelfDeduction,
                entity.DependentDeduction,
                entity.LegalReference
            });
        }
        else
        {
            if (PayrollPeriodOverlapHelper.FindLockedOverlap(lockedPeriods, request.EffectiveFrom, request.EffectiveTo) is { } blocker)
            {
                throw new InvalidOperationException(
                    $"Không được thêm tham số lương có khoảng hiệu lực bao trùm kỳ lương đã khóa ({blocker.PeriodCode}).");
            }

            entity = new PayrollStatutoryParameter();
            actionType = "Create";
            await _context.PayrollStatutoryParameters.AddAsync(entity, cancellationToken);
        }

        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.LegalReference = request.LegalReference?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.StatutoryBaseSalary = request.StatutoryBaseSalary;
        entity.MinWageRegion1 = request.MinWageRegion1;
        entity.MinWageRegion2 = request.MinWageRegion2;
        entity.MinWageRegion3 = request.MinWageRegion3;
        entity.MinWageRegion4 = request.MinWageRegion4;
        entity.SocialInsuranceCapMultiplier = request.SocialInsuranceCapMultiplier;
        entity.UnemploymentInsuranceCapMultiplier = request.UnemploymentInsuranceCapMultiplier;
        entity.EmployeeSiRate = request.EmployeeSiRate;
        entity.EmployeeHiRate = request.EmployeeHiRate;
        entity.EmployeeUiRate = request.EmployeeUiRate;
        entity.CompanySiRate = request.CompanySiRate;
        entity.CompanyHiRate = request.CompanyHiRate;
        entity.CompanyUiRate = request.CompanyUiRate;
        entity.CompanyUnionFeeRate = request.CompanyUnionFeeRate;
        entity.SelfDeduction = request.SelfDeduction;
        entity.DependentDeduction = request.DependentDeduction;

        await _context.SaveChangesAsync(cancellationToken);

        var newJson = JsonSerializer.Serialize(new
        {
            entity.Id,
            entity.EffectiveFrom,
            entity.EffectiveTo,
            entity.StatutoryBaseSalary,
            entity.MinWageRegion1,
            entity.MinWageRegion2,
            entity.MinWageRegion3,
            entity.MinWageRegion4,
            entity.SelfDeduction,
            entity.DependentDeduction,
            entity.LegalReference
        });

        var actorUserId = _currentUser.RequireUserId();
        var actorUsername = _currentUser.RequireUsername();

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "Payroll",
            ActionType = $"PayrollStatutoryParameter_{actionType}",
            EntityName = nameof(PayrollStatutoryParameter),
            EntityId = entity.Id.ToString(),
            OldValuesJson = oldJson,
            NewValuesJson = newJson,
            AffectedColumns = "EffectiveFrom,EffectiveTo,StatutoryBaseSalary,MinWageRegion1..4,SelfDeduction,DependentDeduction,Rates",
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "HR_Admin",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public sealed record PitBracketInputItem(
    int BracketLevel,
    decimal LowerBound,
    decimal? UpperBound,
    decimal TaxRatePercent);

public sealed record SavePitBracketGroupCommand(
    DateOnly? OriginalEffectiveFrom,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? LegalReference,
    IReadOnlyList<PitBracketInputItem> Brackets) : IRequest<int>;

public sealed class SavePitBracketGroupCommandHandler : IRequestHandler<SavePitBracketGroupCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SavePitBracketGroupCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SavePitBracketGroupCommand request, CancellationToken cancellationToken)
    {
        if (request.Brackets is null || request.Brackets.Count == 0)
            throw new InvalidOperationException("Biểu thuế TNCN phải có ít nhất 1 bậc.");
        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom)
            throw new InvalidOperationException("Ngày hết hiệu lực biểu thuế phải lớn hơn hoặc bằng ngày bắt đầu hiệu lực.");

        var lockedPeriods = await _context.PayrollPeriods
            .AsNoTracking()
            .Where(p => p.IsLocked || p.Status == PayrollPeriodStatuses.Locked || p.Status == PayrollPeriodStatuses.Paid)
            .ToListAsync(cancellationToken);

        List<PitBracket> existing = new();
        if (request.OriginalEffectiveFrom.HasValue)
        {
            existing = await _context.PitBrackets
                .Where(b => b.EffectiveFrom == request.OriginalEffectiveFrom.Value)
                .OrderBy(b => b.BracketLevel)
                .ToListAsync(cancellationToken);

            if (existing.Count > 0 &&
                PayrollPeriodOverlapHelper.FindLockedOverlap(lockedPeriods, existing[0].EffectiveFrom, existing[0].EffectiveTo) is { } oldBlocker)
            {
                throw new InvalidOperationException(
                    $"Không được sửa biểu thuế TNCN thuộc giai đoạn đã có kỳ lương bị khóa ({oldBlocker.PeriodCode}).");
            }
        }

        if (PayrollPeriodOverlapHelper.FindLockedOverlap(lockedPeriods, request.EffectiveFrom, request.EffectiveTo) is { } newBlocker)
        {
            throw new InvalidOperationException(
                $"Không được cấu hình biểu thuế TNCN bao trùm kỳ lương đã khóa ({newBlocker.PeriodCode}).");
        }

        string? oldJson = existing.Count > 0
            ? JsonSerializer.Serialize(existing.Select(x => new { x.BracketLevel, x.LowerBound, x.UpperBound, x.TaxRate }))
            : null;

        if (existing.Count > 0)
        {
            _context.PitBrackets.RemoveRange(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var ordered = request.Brackets.OrderBy(b => b.BracketLevel).ToList();
        var newEntities = ordered.Select(b => new PitBracket
        {
            BracketLevel = b.BracketLevel,
            LowerBound = b.LowerBound,
            UpperBound = b.UpperBound,
            TaxRate = b.TaxRatePercent > 1m ? b.TaxRatePercent / 100m : b.TaxRatePercent,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            LegalReference = request.LegalReference?.Trim()
        }).ToList();

        await _context.PitBrackets.AddRangeAsync(newEntities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var actorUserId = _currentUser.RequireUserId();
        var actorUsername = _currentUser.RequireUsername();

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "Payroll",
            ActionType = existing.Count > 0 ? "PitBracketGroup_Update" : "PitBracketGroup_Create",
            EntityName = nameof(PitBracket),
            EntityId = request.EffectiveFrom.ToString("yyyy-MM-dd"),
            OldValuesJson = oldJson,
            NewValuesJson = JsonSerializer.Serialize(newEntities.Select(x => new { x.BracketLevel, x.LowerBound, x.UpperBound, x.TaxRate })),
            AffectedColumns = "BracketLevel,LowerBound,UpperBound,TaxRate,EffectiveFrom,EffectiveTo",
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "HR_Admin",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return newEntities.Count;
    }
}

internal static class PayrollPeriodOverlapHelper
{
    public static PayrollPeriod? FindLockedOverlap(
        IReadOnlyList<PayrollPeriod> lockedPeriods,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        var upper = effectiveTo ?? DateOnly.MaxValue;
        return lockedPeriods.FirstOrDefault(p =>
        {
            var end = p.EndDate != default
                ? p.EndDate
                : new DateOnly(p.Year, p.Month, DateTime.DaysInMonth(p.Year, p.Month));
            return end >= effectiveFrom && end <= upper;
        });
    }
}
