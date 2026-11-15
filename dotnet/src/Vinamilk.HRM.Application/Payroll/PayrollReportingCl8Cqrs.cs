using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Helpers;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Application.Payroll;

// ============================================================================
// 1. BÁO CÁO LƯƠNG CHI TIẾT ĐƠN VỊ (A3 NGANG), QUỸ LƯƠNG, SO SÁNH & COST CENTER
// ============================================================================

public sealed record DetailedUnitPayrollEmployeeRowDto(
    int Stt,
    long PayrollRecordId,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitCode,
    string UnitName,
    string CostCenterCode,
    string PositionName,
    decimal PayableWorkdays,
    decimal StandardWorkdays,
    decimal ContractBaseSalary,
    decimal ActualBaseSalary,
    decimal PositionAndOtherAllowance,
    decimal OvertimePay,
    decimal BonusAndRetroTotal,
    decimal GrossIncome,
    decimal EmployeeSocialInsurance,
    decimal EmployeeHealthInsurance,
    decimal EmployeeUnemploymentInsurance,
    decimal TotalEmployeeInsurance,
    decimal PersonalIncomeTax,
    decimal OtherDeductionsAndAdvances,
    decimal NetSalary,
    decimal CompanyInsuranceTotal,
    decimal CompanyUnionFee,
    decimal TotalCompanyCost);

public sealed record DetailedUnitPayrollGroupDto(
    string UnitCode,
    string UnitName,
    string CostCenterCode,
    int Headcount,
    IReadOnlyList<DetailedUnitPayrollEmployeeRowDto> Employees,
    decimal SubtotalGross,
    decimal SubtotalEmployeeInsurance,
    decimal SubtotalPit,
    decimal SubtotalOtherDeductions,
    decimal SubtotalNet,
    decimal SubtotalCompanyCost);

public sealed record DetailedUnitPayrollReportDto(
    int PayrollPeriodId,
    string PeriodCode,
    string PeriodTitle,
    string PeriodType,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int TotalEmployees,
    IReadOnlyList<DetailedUnitPayrollGroupDto> UnitGroups,
    decimal GrandTotalGross,
    decimal GrandTotalEmployeeInsurance,
    decimal GrandTotalPit,
    decimal GrandTotalOtherDeductions,
    decimal GrandTotalNet,
    decimal GrandTotalCompanyCost);

public sealed record GetDetailedUnitPayrollReportQuery(int PayrollPeriodId, int? OrganizationUnitId = null)
    : IRequest<DetailedUnitPayrollReportDto>;

public sealed class GetDetailedUnitPayrollReportQueryHandler
    : IRequestHandler<GetDetailedUnitPayrollReportQuery, DetailedUnitPayrollReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public GetDetailedUnitPayrollReportQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DetailedUnitPayrollReportDto> Handle(GetDetailedUnitPayrollReportQuery request, CancellationToken cancellationToken)
    {
        var scope = await Common.Security.DataScopeResolver.ResolveAsync(_context, _currentUser, cancellationToken);
        var period = await _context.PayrollPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        var query = _context.PayrollRecords.AsNoTracking()
            .Include(r => r.Employee)
            .Include(r => r.OrganizationUnit)
            .Include(r => r.Position)
            .Where(r => r.PayrollPeriodId == period.Id);

        if (!scope.IsUnrestricted)
        {
            var allowedUnitIds = scope.AllowedUnitIdList;
            var selfEmpId = scope.CurrentEmployeeId;
            query = query.Where(r =>
                allowedUnitIds.Contains(r.OrganizationUnitId) ||
                (selfEmpId.HasValue && r.EmployeeId == selfEmpId.Value));
        }

        if (request.OrganizationUnitId.HasValue && request.OrganizationUnitId.Value > 0)
            query = query.Where(r => r.OrganizationUnitId == request.OrganizationUnitId.Value);

        var records = await query
            .OrderBy(r => r.OrganizationUnit.UnitCode)
            .ThenBy(r => r.Employee.EmployeeCode)
            .ToListAsync(cancellationToken);

        var unitGroups = records
            .GroupBy(r => new
            {
                r.OrganizationUnitId,
                r.OrganizationUnit.UnitCode,
                r.OrganizationUnit.UnitName,
                CostCenterCode = string.IsNullOrWhiteSpace(r.OrganizationUnit.CostCenterCode)
                    ? r.OrganizationUnit.UnitCode
                    : r.OrganizationUnit.CostCenterCode!
            })
            .Select(g =>
            {
                var rows = g.Select((r, idx) =>
                {
                    decimal allowances = r.PositionAllowance + r.OtherAllowance;
                    decimal bonusesAndRetro = r.KpiBonus + r.CommissionBonus + r.ThirteenthMonthBonus + r.RetroEarningAmount + r.UnusedLeavePayout;
                    decimal otherDeds = r.DisciplineDeduction + r.SalaryAdvanceDeduction + r.LoanInstallmentDeduction + r.RetroDeductionAmount + r.EmployeeUnionFee;
                    decimal compIns = r.CompanySocialInsurance + r.CompanyHealthInsurance + r.CompanyUnemploymentInsurance + r.CompanyOccupationalAccidentInsurance;
                    return new DetailedUnitPayrollEmployeeRowDto(
                        idx + 1,
                        r.Id,
                        r.EmployeeId,
                        r.Employee.EmployeeCode,
                        r.Employee.FullName,
                        g.Key.UnitCode,
                        g.Key.UnitName,
                        g.Key.CostCenterCode,
                        r.Position.PositionName,
                        r.PayableWorkdays,
                        r.StandardWorkdays,
                        r.ContractBaseSalary,
                        r.ActualBaseSalary,
                        allowances,
                        r.OvertimePay,
                        bonusesAndRetro,
                        r.GrossIncome,
                        r.EmployeeSocialInsurance,
                        r.EmployeeHealthInsurance,
                        r.EmployeeUnemploymentInsurance,
                        r.TotalEmployeeInsurance,
                        r.PersonalIncomeTax,
                        otherDeds,
                        r.NetSalary,
                        compIns,
                        r.CompanyUnionFee,
                        r.TotalCompanyCost);
                }).ToList();

                return new DetailedUnitPayrollGroupDto(
                    g.Key.UnitCode,
                    g.Key.UnitName,
                    g.Key.CostCenterCode,
                    rows.Count,
                    rows,
                    rows.Sum(x => x.GrossIncome),
                    rows.Sum(x => x.TotalEmployeeInsurance),
                    rows.Sum(x => x.PersonalIncomeTax),
                    rows.Sum(x => x.OtherDeductionsAndAdvances),
                    rows.Sum(x => x.NetSalary),
                    rows.Sum(x => x.TotalCompanyCost));
            })
            .OrderBy(u => u.UnitCode)
            .ToList();

        return new DetailedUnitPayrollReportDto(
            period.Id,
            period.PeriodCode,
            period.PeriodTitle,
            period.PeriodType,
            period.StartDate,
            period.EndDate,
            period.Status,
            records.Count,
            unitGroups,
            unitGroups.Sum(u => u.SubtotalGross),
            unitGroups.Sum(u => u.SubtotalEmployeeInsurance),
            unitGroups.Sum(u => u.SubtotalPit),
            unitGroups.Sum(u => u.SubtotalOtherDeductions),
            unitGroups.Sum(u => u.SubtotalNet),
            unitGroups.Sum(u => u.SubtotalCompanyCost));
    }
}

public sealed record ExportDetailedUnitPayrollA3PdfQuery(int PayrollPeriodId, int? OrganizationUnitId = null)
    : IRequest<PayslipPdfResult>;

public sealed class ExportDetailedUnitPayrollA3PdfQueryHandler
    : IRequestHandler<ExportDetailedUnitPayrollA3PdfQuery, PayslipPdfResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPayrollDocumentService _documentService;

    public ExportDetailedUnitPayrollA3PdfQueryHandler(IApplicationDbContext context, IPayrollDocumentService documentService)
    {
        _context = context;
        _documentService = documentService;
    }

    public async Task<PayslipPdfResult> Handle(ExportDetailedUnitPayrollA3PdfQuery request, CancellationToken cancellationToken)
    {
        var report = await new GetDetailedUnitPayrollReportQueryHandler(_context)
            .Handle(new GetDetailedUnitPayrollReportQuery(request.PayrollPeriodId, request.OrganizationUnitId), cancellationToken);
        var bytes = _documentService.GenerateDetailedUnitPayrollA3LandscapePdf(report);
        return new PayslipPdfResult(bytes, $"BangLuongChiTiet_A3Ngang_{report.PeriodCode}.pdf", "In A3 ngang kèm chữ ký", false);
    }
}

public sealed record PayrollFundMonthlyBucketDto(
    int Year,
    int Month,
    int Quarter,
    int Headcount,
    decimal TotalGross,
    decimal TotalEmployeeInsurance,
    decimal TotalCompanyInsuranceAndUnion,
    decimal TotalPit,
    decimal TotalNetSalaryDisbursed,
    decimal TotalAdvanceDisbursedInMonth,
    decimal TotalCompanyCost);

public sealed record PayrollFundQuarterlyBucketDto(
    int Year,
    int Quarter,
    int MonthCountWithData,
    decimal TotalGross,
    decimal TotalEmployeeInsurance,
    decimal TotalCompanyInsuranceAndUnion,
    decimal TotalPit,
    decimal TotalNetSalaryDisbursed,
    decimal TotalCompanyCost);

public sealed record PayrollFundSummaryReportDto(
    int Year,
    IReadOnlyList<PayrollFundMonthlyBucketDto> MonthlyBuckets,
    IReadOnlyList<PayrollFundQuarterlyBucketDto> QuarterlyBuckets,
    decimal YearlyTotalGross,
    decimal YearlyTotalEmployeeInsurance,
    decimal YearlyTotalCompanyInsuranceAndUnion,
    decimal YearlyTotalPit,
    decimal YearlyTotalNetSalaryDisbursed,
    decimal YearlyTotalCompanyCost);

public sealed record GetPayrollFundSummaryQuery(int Year) : IRequest<PayrollFundSummaryReportDto>;

public sealed class GetPayrollFundSummaryQueryHandler : IRequestHandler<GetPayrollFundSummaryQuery, PayrollFundSummaryReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetPayrollFundSummaryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PayrollFundSummaryReportDto> Handle(GetPayrollFundSummaryQuery request, CancellationToken cancellationToken)
    {
        var periods = await _context.PayrollPeriods.AsNoTracking()
            .Where(p => p.Year == request.Year)
            .Select(p => new { p.Id, p.Month, p.PeriodType })
            .ToListAsync(cancellationToken);

        var periodIds = periods.Select(p => p.Id).ToList();
        var records = periodIds.Count > 0
            ? await _context.PayrollRecords.AsNoTracking()
                .Where(r => periodIds.Contains(r.PayrollPeriodId))
                .Select(r => new
                {
                    r.PayrollPeriodId,
                    r.EmployeeId,
                    r.GrossIncome,
                    r.TotalEmployeeInsurance,
                    r.CompanySocialInsurance,
                    r.CompanyHealthInsurance,
                    r.CompanyUnemploymentInsurance,
                    r.CompanyOccupationalAccidentInsurance,
                    r.CompanyUnionFee,
                    r.PersonalIncomeTax,
                    r.NetSalary,
                    r.TotalCompanyCost
                })
                .ToListAsync(cancellationToken)
            : new();

        var periodMap = periods.ToDictionary(p => p.Id);
        var monthlyBuckets = new List<PayrollFundMonthlyBucketDto>();

        for (int m = 1; m <= 12; m++)
        {
            var mRecords = records.Where(r => periodMap.TryGetValue(r.PayrollPeriodId, out var per) && per.Month == m).ToList();
            if (mRecords.Count == 0)
                continue;

            var nonAdv = mRecords.Where(r => periodMap[r.PayrollPeriodId].PeriodType != "MidMonthAdvance").ToList();
            var advOnly = mRecords.Where(r => periodMap[r.PayrollPeriodId].PeriodType == "MidMonthAdvance").ToList();

            decimal gross = nonAdv.Sum(r => r.GrossIncome);
            decimal empIns = nonAdv.Sum(r => r.TotalEmployeeInsurance);
            decimal compInsUnion = nonAdv.Sum(r => r.CompanySocialInsurance + r.CompanyHealthInsurance + r.CompanyUnemploymentInsurance + r.CompanyOccupationalAccidentInsurance + r.CompanyUnionFee);
            decimal pit = nonAdv.Sum(r => r.PersonalIncomeTax);
            decimal advDisbursed = advOnly.Sum(r => r.NetSalary);
            decimal netTotal = mRecords.Sum(r => r.NetSalary);
            decimal compCost = nonAdv.Sum(r => r.TotalCompanyCost);
            int hc = nonAdv.Select(r => r.EmployeeId).Distinct().Count();
            if (hc == 0)
                hc = mRecords.Select(r => r.EmployeeId).Distinct().Count();

            monthlyBuckets.Add(new PayrollFundMonthlyBucketDto(
                request.Year,
                m,
                ((m - 1) / 3) + 1,
                hc,
                gross,
                empIns,
                compInsUnion,
                pit,
                netTotal,
                advDisbursed,
                compCost));
        }

        var quarterlyBuckets = Enumerable.Range(1, 4)
            .Select(q =>
            {
                var qMonths = monthlyBuckets.Where(b => b.Quarter == q).ToList();
                return new PayrollFundQuarterlyBucketDto(
                    request.Year,
                    q,
                    qMonths.Count,
                    qMonths.Sum(x => x.TotalGross),
                    qMonths.Sum(x => x.TotalEmployeeInsurance),
                    qMonths.Sum(x => x.TotalCompanyInsuranceAndUnion),
                    qMonths.Sum(x => x.TotalPit),
                    qMonths.Sum(x => x.TotalNetSalaryDisbursed),
                    qMonths.Sum(x => x.TotalCompanyCost));
            })
            .ToList();

        return new PayrollFundSummaryReportDto(
            request.Year,
            monthlyBuckets,
            quarterlyBuckets,
            monthlyBuckets.Sum(x => x.TotalGross),
            monthlyBuckets.Sum(x => x.TotalEmployeeInsurance),
            monthlyBuckets.Sum(x => x.TotalCompanyInsuranceAndUnion),
            monthlyBuckets.Sum(x => x.TotalPit),
            monthlyBuckets.Sum(x => x.TotalNetSalaryDisbursed),
            monthlyBuckets.Sum(x => x.TotalCompanyCost));
    }
}

public sealed record PayrollPeriodUnitComparisonDto(
    string UnitCode,
    string UnitName,
    string CostCenterCode,
    int CurrentHeadcount,
    int PreviousHeadcount,
    decimal CurrentGross,
    decimal PreviousGross,
    decimal GrossVariance,
    decimal CurrentNet,
    decimal PreviousNet,
    decimal NetVariance,
    decimal CurrentCompanyCost,
    decimal PreviousCompanyCost,
    decimal CompanyCostVariance);

public sealed record PayrollPeriodComparisonDto(
    int CurrentPeriodId,
    string CurrentPeriodCode,
    int PreviousPeriodId,
    string PreviousPeriodCode,
    decimal CurrentTotalGross,
    decimal PreviousTotalGross,
    decimal GrossVariance,
    decimal? GrossVariancePercent,
    decimal CurrentTotalNet,
    decimal PreviousTotalNet,
    decimal NetVariance,
    decimal? NetVariancePercent,
    decimal CurrentTotalCompanyCost,
    decimal PreviousTotalCompanyCost,
    decimal CompanyCostVariance,
    IReadOnlyList<PayrollPeriodUnitComparisonDto> UnitComparisons);

public sealed record ComparePayrollPeriodsQuery(int CurrentPeriodId, int PreviousPeriodId) : IRequest<PayrollPeriodComparisonDto>;

public sealed class ComparePayrollPeriodsQueryHandler : IRequestHandler<ComparePayrollPeriodsQuery, PayrollPeriodComparisonDto>
{
    private readonly IApplicationDbContext _context;

    public ComparePayrollPeriodsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PayrollPeriodComparisonDto> Handle(ComparePayrollPeriodsQuery request, CancellationToken cancellationToken)
    {
        var currentPeriod = await _context.PayrollPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.CurrentPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ hiện tại #{request.CurrentPeriodId}.");
        var previousPeriod = await _context.PayrollPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PreviousPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ đối chiếu #{request.PreviousPeriodId}.");

        var curRecs = await _context.PayrollRecords.AsNoTracking()
            .Include(r => r.OrganizationUnit)
            .Where(r => r.PayrollPeriodId == currentPeriod.Id)
            .ToListAsync(cancellationToken);

        var prevRecs = await _context.PayrollRecords.AsNoTracking()
            .Include(r => r.OrganizationUnit)
            .Where(r => r.PayrollPeriodId == previousPeriod.Id)
            .ToListAsync(cancellationToken);

        var unitKeys = curRecs.Select(r => new { r.OrganizationUnit.UnitCode, r.OrganizationUnit.UnitName, CostCenter = r.OrganizationUnit.CostCenterCode ?? r.OrganizationUnit.UnitCode })
            .Concat(prevRecs.Select(r => new { r.OrganizationUnit.UnitCode, r.OrganizationUnit.UnitName, CostCenter = r.OrganizationUnit.CostCenterCode ?? r.OrganizationUnit.UnitCode }))
            .Distinct()
            .OrderBy(k => k.UnitCode)
            .ToList();

        var unitRows = unitKeys.Select(k =>
        {
            var cList = curRecs.Where(r => r.OrganizationUnit.UnitCode == k.UnitCode).ToList();
            var pList = prevRecs.Where(r => r.OrganizationUnit.UnitCode == k.UnitCode).ToList();
            decimal cg = cList.Sum(x => x.GrossIncome);
            decimal pg = pList.Sum(x => x.GrossIncome);
            decimal cn = cList.Sum(x => x.NetSalary);
            decimal pn = pList.Sum(x => x.NetSalary);
            decimal cc = cList.Sum(x => x.TotalCompanyCost);
            decimal pc = pList.Sum(x => x.TotalCompanyCost);
            return new PayrollPeriodUnitComparisonDto(
                k.UnitCode,
                k.UnitName,
                k.CostCenter,
                cList.Count,
                pList.Count,
                cg,
                pg,
                cg - pg,
                cn,
                pn,
                cn - pn,
                cc,
                pc,
                cc - pc);
        }).ToList();

        decimal curGross = unitRows.Sum(u => u.CurrentGross);
        decimal prevGross = unitRows.Sum(u => u.PreviousGross);
        decimal curNet = unitRows.Sum(u => u.CurrentNet);
        decimal prevNet = unitRows.Sum(u => u.PreviousNet);
        decimal curCost = unitRows.Sum(u => u.CurrentCompanyCost);
        decimal prevCost = unitRows.Sum(u => u.PreviousCompanyCost);

        return new PayrollPeriodComparisonDto(
            currentPeriod.Id,
            currentPeriod.PeriodCode,
            previousPeriod.Id,
            previousPeriod.PeriodCode,
            curGross,
            prevGross,
            curGross - prevGross,
            prevGross > 0m ? Math.Round((curGross - prevGross) / prevGross * 100m, 2, MidpointRounding.AwayFromZero) : null,
            curNet,
            prevNet,
            curNet - prevNet,
            prevNet > 0m ? Math.Round((curNet - prevNet) / prevNet * 100m, 2, MidpointRounding.AwayFromZero) : null,
            curCost,
            prevCost,
            curCost - prevCost,
            unitRows);
    }
}

public sealed record CostCenterLaborCostRowDto(
    string CostCenterCode,
    string UnitNamesSummary,
    int Headcount,
    decimal BaseAndAllowances,
    decimal OtAndBonuses,
    decimal TotalGross,
    decimal TotalEmployeeInsurance,
    decimal TotalCompanyInsuranceAndOa,
    decimal TotalCompanyUnionFee,
    decimal TotalPit,
    decimal TotalNet,
    decimal TotalCompanyCost);

public sealed record CostCenterLaborCostReportDto(
    int PayrollPeriodId,
    string PeriodCode,
    string PeriodTitle,
    IReadOnlyList<CostCenterLaborCostRowDto> CostCenters,
    decimal GrandTotalGross,
    decimal GrandTotalCompanyInsuranceAndUnion,
    decimal GrandTotalPit,
    decimal GrandTotalNet,
    decimal GrandTotalCompanyCost);

public sealed record GetCostCenterLaborCostQuery(int PayrollPeriodId) : IRequest<CostCenterLaborCostReportDto>;

public sealed class GetCostCenterLaborCostQueryHandler : IRequestHandler<GetCostCenterLaborCostQuery, CostCenterLaborCostReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetCostCenterLaborCostQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<CostCenterLaborCostReportDto> Handle(GetCostCenterLaborCostQuery request, CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        var records = await _context.PayrollRecords.AsNoTracking()
            .Include(r => r.OrganizationUnit)
            .Where(r => r.PayrollPeriodId == period.Id)
            .ToListAsync(cancellationToken);

        var rows = records
            .GroupBy(r => string.IsNullOrWhiteSpace(r.OrganizationUnit.CostCenterCode)
                ? r.OrganizationUnit.UnitCode
                : r.OrganizationUnit.CostCenterCode!)
            .Select(g =>
            {
                decimal baseAllow = g.Sum(x => x.ActualBaseSalary + x.PositionAllowance + x.OtherAllowance);
                decimal otBonus = g.Sum(x => x.OvertimePay + x.KpiBonus + x.CommissionBonus + x.ThirteenthMonthBonus + x.RetroEarningAmount + x.UnusedLeavePayout);
                decimal compInsOa = g.Sum(x => x.CompanySocialInsurance + x.CompanyHealthInsurance + x.CompanyUnemploymentInsurance + x.CompanyOccupationalAccidentInsurance);
                decimal compUnion = g.Sum(x => x.CompanyUnionFee);
                string units = string.Join(", ", g.Select(x => x.OrganizationUnit.UnitName).Distinct().OrderBy(n => n));

                return new CostCenterLaborCostRowDto(
                    g.Key,
                    units,
                    g.Count(),
                    baseAllow,
                    otBonus,
                    g.Sum(x => x.GrossIncome),
                    g.Sum(x => x.TotalEmployeeInsurance),
                    compInsOa,
                    compUnion,
                    g.Sum(x => x.PersonalIncomeTax),
                    g.Sum(x => x.NetSalary),
                    g.Sum(x => x.TotalCompanyCost));
            })
            .OrderByDescending(x => x.TotalCompanyCost)
            .ToList();

        return new CostCenterLaborCostReportDto(
            period.Id,
            period.PeriodCode,
            period.PeriodTitle,
            rows,
            rows.Sum(r => r.TotalGross),
            rows.Sum(r => r.TotalCompanyInsuranceAndOa + r.TotalCompanyUnionFee),
            rows.Sum(r => r.TotalPit),
            rows.Sum(r => r.TotalNet),
            rows.Sum(r => r.TotalCompanyCost));
    }
}

// ============================================================================
// 2. BÁO CÁO BẢO HIỂM XÃ HỘI (TĂNG / GIẢM / ĐIỀU CHỈNH, D02-LT & ĐỐI CHIẾU)
// ============================================================================

public sealed record SocialInsuranceMovementRowDto(
    int Stt,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string SocialInsuranceNumber,
    string UnitName,
    string PositionName,
    string ChangeCode,
    string ChangeLabel,
    decimal PreviousSalaryBase,
    decimal CurrentSalaryBase,
    decimal SalaryBaseDelta,
    decimal EmployeeSiHiUiTotal,
    decimal CompanySiHiUiOaTotal,
    string Notes);

public sealed record SocialInsuranceReconciliationDto(
    int InsuredHeadcount,
    decimal TotalInsuranceSalaryBase,
    decimal TotalUnemploymentSalaryBase,
    decimal ExpectedEmployeeContribution,
    decimal ActualEmployeeContributionFromRecords,
    decimal ExpectedCompanyContribution,
    decimal ActualCompanyContributionFromRecords,
    decimal TotalExpectedPayableToAgency,
    decimal TotalActualFromPayrollRecords,
    decimal ReconciliationDifference,
    string LegalReference);

public sealed record SocialInsuranceMonthlyReportDto(
    int Year,
    int Month,
    string CurrentPeriodCode,
    string? PreviousPeriodCode,
    int IncreaseCount,
    int DecreaseCount,
    int SalaryAdjustmentCount,
    int UnchangedCount,
    IReadOnlyList<SocialInsuranceMovementRowDto> Movements,
    SocialInsuranceReconciliationDto Reconciliation);

public sealed record GetSocialInsuranceMonthlyReportQuery(int Year, int Month, int? PayrollPeriodId = null)
    : IRequest<SocialInsuranceMonthlyReportDto>;

public sealed class GetSocialInsuranceMonthlyReportQueryHandler
    : IRequestHandler<GetSocialInsuranceMonthlyReportQuery, SocialInsuranceMonthlyReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetSocialInsuranceMonthlyReportQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<SocialInsuranceMonthlyReportDto> Handle(GetSocialInsuranceMonthlyReportQuery request, CancellationToken cancellationToken)
    {
        if (request.Month is < 1 or > 12 || request.Year < 2000)
            throw new ArgumentOutOfRangeException(nameof(request), "Tháng/năm báo cáo BHXH không hợp lệ.");

        PayrollPeriod? currentPeriod = request.PayrollPeriodId.HasValue
            ? await _context.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId.Value, cancellationToken)
            : await _context.PayrollPeriods.AsNoTracking()
                .Where(p => p.Year == request.Year && p.Month == request.Month && p.PeriodType == "Monthly")
                .OrderByDescending(p => p.DisbursementSequence)
                .FirstOrDefaultAsync(cancellationToken);

        if (currentPeriod == null)
            throw new KeyNotFoundException($"Chưa có kỳ lương chính thức tháng {request.Month:D2}/{request.Year} để lập báo cáo BHXH.");

        int prevYear = request.Month == 1 ? request.Year - 1 : request.Year;
        int prevMonth = request.Month == 1 ? 12 : request.Month - 1;
        var prevPeriod = await _context.PayrollPeriods.AsNoTracking()
            .Where(p => p.Year == prevYear && p.Month == prevMonth && p.PeriodType == "Monthly")
            .OrderByDescending(p => p.DisbursementSequence)
            .FirstOrDefaultAsync(cancellationToken);

        var curRecords = await _context.PayrollRecords.AsNoTracking()
            .Include(r => r.Employee)
            .Include(r => r.OrganizationUnit)
            .Include(r => r.Position)
            .Where(r => r.PayrollPeriodId == currentPeriod.Id)
            .OrderBy(r => r.Employee.EmployeeCode)
            .ToListAsync(cancellationToken);

        var prevRecords = prevPeriod != null
            ? await _context.PayrollRecords.AsNoTracking()
                .Include(r => r.Employee)
                .Include(r => r.OrganizationUnit)
                .Include(r => r.Position)
                .Where(r => r.PayrollPeriodId == prevPeriod.Id)
                .ToListAsync(cancellationToken)
            : new();

        var curMap = curRecords.ToDictionary(r => r.EmployeeId);
        var prevMap = prevRecords.ToDictionary(r => r.EmployeeId);
        var allEmpIds = curMap.Keys.Union(prevMap.Keys).OrderBy(id => id).ToList();

        var movements = new List<SocialInsuranceMovementRowDto>();
        int stt = 1;
        int incCount = 0;
        int decCount = 0;
        int adjCount = 0;
        int unchCount = 0;

        foreach (var empId in allEmpIds)
        {
            curMap.TryGetValue(empId, out var c);
            prevMap.TryGetValue(empId, out var p);

            decimal prevBase = p?.InsuranceSalaryBase ?? 0m;
            decimal curBase = c?.InsuranceSalaryBase ?? 0m;
            if (prevBase <= 0m && curBase <= 0m)
                continue;

            var emp = (c?.Employee ?? p!.Employee);
            var unitName = (c?.OrganizationUnit ?? p!.OrganizationUnit).UnitName;
            var posName = (c?.Position ?? p!.Position).PositionName;

            string code;
            string label;
            string note;

            if (prevBase <= 0m && curBase > 0m)
            {
                code = "TANG_MOI";
                label = "Tăng mới lao động tham gia BHXH";
                note = emp.HireDate.Year == request.Year && emp.HireDate.Month == request.Month
                    ? $"Tuyển mới ngày {emp.HireDate:dd/MM/yyyy}"
                    : "Bắt đầu tham gia BHXH trong tháng";
                incCount++;
            }
            else if (prevBase > 0m && curBase <= 0m)
            {
                code = "GIAM_NGHI_VIEC";
                label = "Giảm lao động đóng BHXH (Nghỉ việc / Nghỉ >= 14 ngày)";
                note = emp.TerminationDate.HasValue
                    ? $"Nghỉ việc ngày {emp.TerminationDate:dd/MM/yyyy}"
                    : "Không đủ công đóng BHXH tháng này";
                decCount++;
            }
            else if (curBase > prevBase)
            {
                code = "DIEU_CHINH_TANG";
                label = "Điều chỉnh tăng mức lương đóng BHXH";
                note = $"Tăng nền đóng +{curBase - prevBase:N0} đ";
                adjCount++;
            }
            else if (curBase < prevBase)
            {
                code = "DIEU_CHINH_GIAM";
                label = "Điều chỉnh giảm mức lương đóng BHXH";
                note = $"Giảm nền đóng {curBase - prevBase:N0} đ";
                adjCount++;
            }
            else
            {
                code = "ON_DINH";
                label = "Đóng ổn định";
                note = "Không thay đổi nền đóng";
                unchCount++;
            }

            decimal empTotal = c?.TotalEmployeeInsurance ?? 0m;
            decimal compTotal = c != null
                ? c.CompanySocialInsurance + c.CompanyHealthInsurance + c.CompanyUnemploymentInsurance + c.CompanyOccupationalAccidentInsurance
                : 0m;

            movements.Add(new SocialInsuranceMovementRowDto(
                stt++,
                emp.Id,
                emp.EmployeeCode,
                emp.FullName,
                emp.SocialInsuranceNumber ?? string.Empty,
                unitName,
                posName,
                code,
                label,
                prevBase,
                curBase,
                curBase - prevBase,
                empTotal,
                compTotal,
                note));
        }

        // Đối chiếu số phải nộp từ tham số pháp lý có hiệu lực tại cuối kỳ
        var statCtx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(_context, currentPeriod.EndDate, cancellationToken);
        var param = statCtx.Parameter;

        var insuredRecs = curRecords.Where(r => r.InsuranceSalaryBase > 0m || r.UnemploymentInsuranceBase > 0m).ToList();
        decimal sumSiBase = insuredRecs.Sum(r => r.InsuranceSalaryBase);
        decimal sumUiBase = insuredRecs.Sum(r => r.UnemploymentInsuranceBase);

        decimal expectedEmp = insuredRecs.Sum(r =>
            Math.Round(r.InsuranceSalaryBase * param.EmployeeSiRate, 0, MidpointRounding.AwayFromZero)
            + Math.Round(r.InsuranceSalaryBase * param.EmployeeHiRate, 0, MidpointRounding.AwayFromZero)
            + Math.Round(r.UnemploymentInsuranceBase * param.EmployeeUiRate, 0, MidpointRounding.AwayFromZero));

        decimal expectedComp = insuredRecs.Sum(r =>
            Math.Round(r.InsuranceSalaryBase * param.CompanySiRate, 0, MidpointRounding.AwayFromZero)
            + Math.Round(r.InsuranceSalaryBase * param.CompanyHiRate, 0, MidpointRounding.AwayFromZero)
            + Math.Round(r.UnemploymentInsuranceBase * param.CompanyUiRate, 0, MidpointRounding.AwayFromZero)
            + Math.Round(r.InsuranceSalaryBase * param.CompanyOccupationalAccidentRate, 0, MidpointRounding.AwayFromZero));

        decimal actualEmp = insuredRecs.Sum(r => r.TotalEmployeeInsurance);
        decimal actualComp = insuredRecs.Sum(r =>
            r.CompanySocialInsurance + r.CompanyHealthInsurance + r.CompanyUnemploymentInsurance + r.CompanyOccupationalAccidentInsurance);

        decimal totalExpected = expectedEmp + expectedComp;
        decimal totalActual = actualEmp + actualComp;

        var reconciliation = new SocialInsuranceReconciliationDto(
            insuredRecs.Count,
            sumSiBase,
            sumUiBase,
            expectedEmp,
            actualEmp,
            expectedComp,
            actualComp,
            totalExpected,
            totalActual,
            totalActual - totalExpected,
            param.LegalReference ?? "Luật BHXH & NĐ quy định tỷ lệ đóng BH");

        return new SocialInsuranceMonthlyReportDto(
            request.Year,
            request.Month,
            currentPeriod.PeriodCode,
            prevPeriod?.PeriodCode,
            incCount,
            decCount,
            adjCount,
            unchCount,
            movements,
            reconciliation);
    }
}

public sealed record ExportSocialInsuranceExcelQuery(
    int Year,
    int Month,
    string TemplateCode = "D02-LT",
    int? PayrollPeriodId = null) : IRequest<BankTransferExportResult>;

public sealed class ExportSocialInsuranceExcelQueryHandler
    : IRequestHandler<ExportSocialInsuranceExcelQuery, BankTransferExportResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPayrollDocumentService _documentService;

    public ExportSocialInsuranceExcelQueryHandler(IApplicationDbContext context, IPayrollDocumentService documentService)
    {
        _context = context;
        _documentService = documentService;
    }

    public async Task<BankTransferExportResult> Handle(ExportSocialInsuranceExcelQuery request, CancellationToken cancellationToken)
    {
        var report = await new GetSocialInsuranceMonthlyReportQueryHandler(_context)
            .Handle(new GetSocialInsuranceMonthlyReportQuery(request.Year, request.Month, request.PayrollPeriodId), cancellationToken);

        var periodEnd = new DateOnly(request.Year, request.Month, DateTime.DaysInMonth(request.Year, request.Month));
        var template = await _context.SocialInsuranceReportTemplates.AsNoTracking()
            .Where(t => t.IsActive
                && t.TemplateCode == request.TemplateCode
                && t.EffectiveFrom <= periodEnd
                && (t.EffectiveTo == null || t.EffectiveTo >= periodEnd))
            .OrderByDescending(t => t.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        var bytes = _documentService.ExportSocialInsuranceExcel(report, template);
        return new BankTransferExportResult(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BHXH_{request.TemplateCode}_{request.Year}{request.Month:D2}.xlsx",
            request.TemplateCode,
            report.Movements.Count,
            report.Reconciliation.TotalActualFromPayrollRecords);
    }
}

// ============================================================================
// 3. THUẾ TNCN: TỜ KHAI THÁNG/QUÝ, QUYẾT TOÁN NĂM 05/QTT, CHỨNG TỪ PDF & EXCEL
// ============================================================================

public sealed record PitDeclarationResidencyRowDto(
    string TaxResidencyMode,
    string ResidencyLabel,
    int EmployeeCount,
    decimal TotalGrossIncome,
    decimal TotalTaxableIncome,
    decimal TotalAssessableIncome,
    decimal TotalPitWithheld);

public sealed record PitDeclarationSummaryDto(
    int Year,
    int? Month,
    int? Quarter,
    string PeriodLabel,
    int TotalEmployeesTaxed,
    decimal GrandTotalGross,
    decimal GrandTotalTaxableIncome,
    decimal GrandTotalAssessableIncome,
    decimal GrandTotalPitWithheld,
    IReadOnlyList<PitDeclarationResidencyRowDto> ResidencyRows);

public sealed record GetPitDeclarationSummaryQuery(int Year, int? Month = null, int? Quarter = null)
    : IRequest<PitDeclarationSummaryDto>;

public sealed class GetPitDeclarationSummaryQueryHandler : IRequestHandler<GetPitDeclarationSummaryQuery, PitDeclarationSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetPitDeclarationSummaryQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PitDeclarationSummaryDto> Handle(GetPitDeclarationSummaryQuery request, CancellationToken cancellationToken)
    {
        var months = request.Month.HasValue
            ? new[] { request.Month.Value }
            : request.Quarter.HasValue
                ? Enumerable.Range(((request.Quarter.Value - 1) * 3) + 1, 3).ToArray()
                : Enumerable.Range(1, 12).ToArray();

        string label = request.Month.HasValue
            ? $"Tháng {request.Month.Value:D2}/{request.Year}"
            : request.Quarter.HasValue
                ? $"Quý {request.Quarter.Value}/{request.Year}"
                : $"Cả năm {request.Year}";

        var periodIds = await _context.PayrollPeriods.AsNoTracking()
            .Where(p => p.Year == request.Year && months.Contains(p.Month) && p.PeriodType != "MidMonthAdvance")
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var records = periodIds.Count > 0
            ? await _context.PayrollRecords.AsNoTracking()
                .Where(r => periodIds.Contains(r.PayrollPeriodId))
                .ToListAsync(cancellationToken)
            : new();

        var groups = records
            .GroupBy(r => r.TaxResidencyMode)
            .Select(g => new PitDeclarationResidencyRowDto(
                g.Key,
                g.Key switch
                {
                    "ResidentStandard" => "Cá nhân cư trú có HĐLĐ >= 3 tháng (Lũy tiến)",
                    "CasualWithholding10" => "Cá nhân cư trú không ký HĐLĐ / < 3 tháng (10%)",
                    "NonResidentFlat20" => "Cá nhân không cư trú (20%)",
                    _ => g.Key
                },
                g.Select(x => x.EmployeeId).Distinct().Count(),
                g.Sum(x => x.GrossIncome),
                g.Sum(x => x.TaxableIncome),
                g.Sum(x => x.AssessableIncome),
                g.Sum(x => x.PersonalIncomeTax)))
            .OrderBy(r => r.TaxResidencyMode)
            .ToList();

        return new PitDeclarationSummaryDto(
            request.Year,
            request.Month,
            request.Quarter,
            label,
            records.Select(r => r.EmployeeId).Distinct().Count(),
            groups.Sum(g => g.TotalGrossIncome),
            groups.Sum(g => g.TotalTaxableIncome),
            groups.Sum(g => g.TotalAssessableIncome),
            groups.Sum(g => g.TotalPitWithheld),
            groups);
    }
}

public sealed record PitAnnualEmployeeFinalizationRowDto(
    int Stt,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string TaxCode,
    string CitizenId,
    string TaxResidencyMode,
    int ActiveMonthsInYear,
    decimal TotalGrossIncome,
    decimal TotalInsuranceDeducted,
    decimal TotalFamilyDeductions,
    decimal TotalTaxableIncome,
    decimal AnnualAssessableIncome,
    decimal AnnualFinalTaxLiability,
    decimal TotalPitWithheld,
    decimal AdditionalTaxPayable,
    decimal RefundableTax);

public sealed record PitAnnualFinalizationReportDto(
    int TaxYear,
    string LegalReference,
    IReadOnlyList<PitAnnualEmployeeFinalizationRowDto> Employees,
    decimal GrandTotalGross,
    decimal GrandTotalTaxableIncome,
    decimal GrandTotalPitWithheld,
    decimal GrandTotalFinalTaxLiability,
    decimal GrandTotalAdditionalPayable,
    decimal GrandTotalRefundable);

public sealed record GetAnnualPitFinalizationQuery(int TaxYear) : IRequest<PitAnnualFinalizationReportDto>;

public sealed class GetAnnualPitFinalizationQueryHandler : IRequestHandler<GetAnnualPitFinalizationQuery, PitAnnualFinalizationReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryptionService;

    public GetAnnualPitFinalizationQueryHandler(IApplicationDbContext context, IPiiEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task<PitAnnualFinalizationReportDto> Handle(GetAnnualPitFinalizationQuery request, CancellationToken cancellationToken)
    {
        var yearEnd = new DateOnly(request.TaxYear, 12, 31);
        var statCtx = await VietnamPayrollEngine.ResolveStatutoryContextAsync(_context, yearEnd, cancellationToken);

        var periods = await _context.PayrollPeriods.AsNoTracking()
            .Where(p => p.Year == request.TaxYear && p.PeriodType != "MidMonthAdvance")
            .Select(p => new { p.Id, p.Month })
            .ToListAsync(cancellationToken);

        var periodMonthMap = periods.ToDictionary(p => p.Id, p => p.Month);
        var periodIds = periods.Select(p => p.Id).ToList();

        var records = periodIds.Count > 0
            ? await _context.PayrollRecords.AsNoTracking()
                .Include(r => r.Employee)
                .Where(r => periodIds.Contains(r.PayrollPeriodId))
                .ToListAsync(cancellationToken)
            : new();

        var rows = records
            .GroupBy(r => r.EmployeeId)
            .OrderBy(g => g.First().Employee.EmployeeCode)
            .Select((g, idx) =>
            {
                var emp = g.First().Employee;
                string mode = g.OrderByDescending(x => x.PayrollPeriodId).First().TaxResidencyMode;
                int activeMonths = Math.Max(1, g.Select(x => periodMonthMap[x.PayrollPeriodId]).Distinct().Count());
                decimal gross = g.Sum(x => x.GrossIncome);
                decimal ins = g.Sum(x => x.TotalEmployeeInsurance);
                decimal famDed = g.Sum(x => x.SelfDeductionAmount + x.DependentDeductionAmount);
                decimal taxable = g.Sum(x => x.TaxableIncome);
                decimal withheld = g.Sum(x => x.PersonalIncomeTax);

                decimal assessable = Math.Max(0m, taxable - famDed);
                decimal finalLiability;
                if (string.Equals(mode, "ResidentStandard", StringComparison.OrdinalIgnoreCase))
                {
                    // Bình quân tháng trên tổng tháng có thu nhập tính thuế trong năm theo đúng nguyên tắc quyết toán năm
                    decimal monthlyAvgAssessable = assessable / activeMonths;
                    decimal monthlyTax = VietnamPayrollEngine.CalculateProgressivePit(monthlyAvgAssessable, statCtx.PitBrackets);
                    finalLiability = Math.Round(monthlyTax * activeMonths, 0, MidpointRounding.AwayFromZero);
                }
                else
                {
                    finalLiability = withheld;
                }

                decimal diff = finalLiability - withheld;
                decimal addPay = diff > 0m ? diff : 0m;
                decimal refund = diff < 0m ? Math.Abs(diff) : 0m;

                return new PitAnnualEmployeeFinalizationRowDto(
                    idx + 1,
                    emp.Id,
                    emp.EmployeeCode,
                    emp.FullName,
                    _encryptionService.Decrypt(emp.TaxCodeEncrypted),
                    _encryptionService.Decrypt(emp.CitizenIdEncrypted),
                    mode,
                    activeMonths,
                    gross,
                    ins,
                    famDed,
                    taxable,
                    assessable,
                    finalLiability,
                    withheld,
                    addPay,
                    refund);
            })
            .ToList();

        return new PitAnnualFinalizationReportDto(
            request.TaxYear,
            statCtx.Parameter.LegalReference ?? "Luật Thuế TNCN & Thông tư 80/2021/TT-BTC (05/QTT-TNCN)",
            rows,
            rows.Sum(r => r.TotalGrossIncome),
            rows.Sum(r => r.TotalTaxableIncome),
            rows.Sum(r => r.TotalPitWithheld),
            rows.Sum(r => r.AnnualFinalTaxLiability),
            rows.Sum(r => r.AdditionalTaxPayable),
            rows.Sum(r => r.RefundableTax));
    }
}

public sealed record PitWithholdingCertificateDto(
    string CertificateCode,
    int TaxYear,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string TaxCode,
    string CitizenId,
    decimal GrossIncome,
    decimal TotalInsuranceDeducted,
    decimal TotalFamilyDeductions,
    decimal TaxableIncome,
    decimal AssessableIncome,
    decimal TotalPitWithheld,
    decimal AnnualFinalTaxLiability,
    decimal PitOverpaidOrUnderpaid,
    DateOnly IssuedDate,
    string LegalReference);

public sealed record IssueAndGetPitCertificatePdfQuery(
    int EmployeeId,
    int TaxYear,
    bool PersistCertificate = true,
    bool EnforceSelfOnly = false) : IRequest<PayslipPdfResult>;

public sealed class IssueAndGetPitCertificatePdfQueryHandler
    : IRequestHandler<IssueAndGetPitCertificatePdfQuery, PayslipPdfResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryptionService;
    private readonly IPayrollDocumentService _documentService;
    private readonly ICurrentUserService? _currentUser;

    public IssueAndGetPitCertificatePdfQueryHandler(
        IApplicationDbContext context,
        IPiiEncryptionService encryptionService,
        IPayrollDocumentService documentService,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _encryptionService = encryptionService;
        _documentService = documentService;
        _currentUser = currentUser;
    }

    public async Task<PayslipPdfResult> Handle(IssueAndGetPitCertificatePdfQuery request, CancellationToken cancellationToken)
    {
        var emp = await _context.Employees.AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy nhân viên #{request.EmployeeId}.");

        if (request.EnforceSelfOnly && _currentUser is not null)
        {
            if (!_currentUser.HasPermission("Payroll.View") && !_currentUser.HasPermission("Payroll.Approve"))
            {
                if (!_currentUser.EmployeeId.HasValue || _currentUser.EmployeeId.Value != emp.Id)
                    throw new UnauthorizedAccessException("Bạn chỉ được phép tải chứng từ khấu trừ thuế TNCN của chính mình.");
            }
            else if (_currentUser.EmployeeId.HasValue && _currentUser.EmployeeId.Value != emp.Id && !_currentUser.HasPermission("Payroll.Approve"))
            {
                throw new UnauthorizedAccessException("Bạn chỉ được phép tải chứng từ khấu trừ thuế TNCN của chính mình.");
            }
        }

        var annualReport = await new GetAnnualPitFinalizationQueryHandler(_context, _encryptionService)
            .Handle(new GetAnnualPitFinalizationQuery(request.TaxYear), cancellationToken);

        var row = annualReport.Employees.FirstOrDefault(e => e.EmployeeId == emp.Id)
            ?? new PitAnnualEmployeeFinalizationRowDto(
                1, emp.Id, emp.EmployeeCode, emp.FullName,
                _encryptionService.Decrypt(emp.TaxCodeEncrypted),
                _encryptionService.Decrypt(emp.CitizenIdEncrypted),
                "ResidentStandard", 0, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);

        var existing = await _context.PitWithholdingCertificates
            .FirstOrDefaultAsync(c => c.EmployeeId == emp.Id && c.TaxYear == request.TaxYear && c.Status == "Issued", cancellationToken);

        string certCode = existing?.CertificateCode ?? $"CTKT-{request.TaxYear}-{emp.EmployeeCode}";
        DateOnly issuedDate = existing?.IssuedDate ?? DateOnly.FromDateTime(DateTime.Today);

        if (request.PersistCertificate)
        {
            var userId = _currentUser.RequireUserId();
            var username = _currentUser.RequireUsername();
            if (existing == null)
            {
                existing = new PitWithholdingCertificate
                {
                    CertificateCode = certCode,
                    TaxYear = request.TaxYear,
                    EmployeeId = emp.Id,
                    GrossIncome = row.TotalGrossIncome,
                    TotalInsuranceDeducted = row.TotalInsuranceDeducted,
                    TotalFamilyDeductions = row.TotalFamilyDeductions,
                    TaxableIncome = row.TotalTaxableIncome,
                    AssessableIncome = row.AnnualAssessableIncome,
                    TotalPitWithheld = row.TotalPitWithheld,
                    AnnualFinalTaxLiability = row.AnnualFinalTaxLiability,
                    PitOverpaidOrUnderpaid = row.AnnualFinalTaxLiability - row.TotalPitWithheld,
                    IssuedDate = issuedDate,
                    IssuedByUserId = userId,
                    IssuedByUsername = username,
                    Status = "Issued",
                    LegalReference = "NĐ 123/2020/NĐ-CP, NĐ 70/2025/NĐ-CP & TT 80/2021/TT-BTC",
                    NeedsHrVerification = true
                };
                await _context.PitWithholdingCertificates.AddAsync(existing, cancellationToken);
            }
            else
            {
                existing.GrossIncome = row.TotalGrossIncome;
                existing.TotalInsuranceDeducted = row.TotalInsuranceDeducted;
                existing.TotalFamilyDeductions = row.TotalFamilyDeductions;
                existing.TaxableIncome = row.TotalTaxableIncome;
                existing.AssessableIncome = row.AnnualAssessableIncome;
                existing.TotalPitWithheld = row.TotalPitWithheld;
                existing.AnnualFinalTaxLiability = row.AnnualFinalTaxLiability;
                existing.PitOverpaidOrUnderpaid = row.AnnualFinalTaxLiability - row.TotalPitWithheld;
                existing.IssuedByUserId = userId;
                existing.IssuedByUsername = username;
            }

            await _context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = userId,
                Username = username,
                ModuleName = "Payroll",
                ActionType = "PitCertificate_Issue",
                EntityName = nameof(PitWithholdingCertificate),
                EntityId = certCode,
                NewValuesJson = JsonSerializer.Serialize(new { emp.EmployeeCode, request.TaxYear, row.TotalGrossIncome, row.TotalPitWithheld }),
                IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
                UserAgent = _currentUser?.UserAgent ?? "PayrollWeb",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username
            }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var certDto = new PitWithholdingCertificateDto(
            certCode,
            request.TaxYear,
            emp.Id,
            emp.EmployeeCode,
            emp.FullName,
            row.TaxCode,
            row.CitizenId,
            row.TotalGrossIncome,
            row.TotalInsuranceDeducted,
            row.TotalFamilyDeductions,
            row.TotalTaxableIncome,
            row.AnnualAssessableIncome,
            row.TotalPitWithheld,
            row.AnnualFinalTaxLiability,
            row.AnnualFinalTaxLiability - row.TotalPitWithheld,
            issuedDate,
            annualReport.LegalReference);

        var pdfBytes = _documentService.GeneratePitWithholdingCertificatePdf(certDto, emp);
        return new PayslipPdfResult(pdfBytes, $"ChungTuKhauTruThueTNCN_{emp.EmployeeCode}_{request.TaxYear}.pdf", "Chứng từ khấu trừ thuế TNCN điện tử", false);
    }
}

public sealed record ExportPitSettlementExcelQuery(int TaxYear) : IRequest<BankTransferExportResult>;

public sealed class ExportPitSettlementExcelQueryHandler
    : IRequestHandler<ExportPitSettlementExcelQuery, BankTransferExportResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryptionService;
    private readonly IPayrollDocumentService _documentService;

    public ExportPitSettlementExcelQueryHandler(
        IApplicationDbContext context,
        IPiiEncryptionService encryptionService,
        IPayrollDocumentService documentService)
    {
        _context = context;
        _encryptionService = encryptionService;
        _documentService = documentService;
    }

    public async Task<BankTransferExportResult> Handle(ExportPitSettlementExcelQuery request, CancellationToken cancellationToken)
    {
        var report = await new GetAnnualPitFinalizationQueryHandler(_context, _encryptionService)
            .Handle(new GetAnnualPitFinalizationQuery(request.TaxYear), cancellationToken);
        var bytes = _documentService.ExportPitSettlementExcel(report);
        return new BankTransferExportResult(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"QuyetToanThueTNCN_05QTT_{request.TaxYear}.xlsx",
            "05/QTT-TNCN",
            report.Employees.Count,
            report.GrandTotalPitWithheld);
    }
}

// ============================================================================
// 4. HẠCH TOÁN KẾ TOÁN TIỀN LƯƠNG THEO COST CENTER & EXPORT EXCEL/CSV
// ============================================================================

public sealed record PayrollAccountingJournalLineDto(
    int LineNo,
    string AccountKey,
    string DebitAccountCode,
    string CreditAccountCode,
    string CostCenterCode,
    string Description,
    decimal Amount);

public sealed record PayrollAccountingJournalDto(
    int PayrollPeriodId,
    string PeriodCode,
    string PeriodTitle,
    DateOnly PostingDate,
    IReadOnlyList<PayrollAccountingJournalLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal PayableToEmployeesAccount334,
    bool IsBalanced);

public sealed record GeneratePayrollAccountingEntriesQuery(int PayrollPeriodId) : IRequest<PayrollAccountingJournalDto>;

public sealed class GeneratePayrollAccountingEntriesQueryHandler
    : IRequestHandler<GeneratePayrollAccountingEntriesQuery, PayrollAccountingJournalDto>
{
    private readonly IApplicationDbContext _context;

    public GeneratePayrollAccountingEntriesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PayrollAccountingJournalDto> Handle(GeneratePayrollAccountingEntriesQuery request, CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        var records = await _context.PayrollRecords.AsNoTracking()
            .Include(r => r.OrganizationUnit)
            .Where(r => r.PayrollPeriodId == period.Id)
            .ToListAsync(cancellationToken);

        var configs = await _context.PayrollAccountingAccountConfigs.AsNoTracking()
            .Where(c => c.IsActive && c.EffectiveFrom <= period.EndDate && (c.EffectiveTo == null || c.EffectiveTo >= period.EndDate))
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        string ResolveAccountCode(string preferredKey, string? costCenter = null, string fallback = "6421")
        {
            if (!string.IsNullOrWhiteSpace(costCenter) && preferredKey == "EXPENSE_SALARY")
            {
                var salaryExpenses = configs.Where(c => c.AccountKey.StartsWith("EXPENSE_SALARY", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var cfg in salaryExpenses)
                {
                    if (!string.IsNullOrWhiteSpace(cfg.CostCenterPattern) && cfg.CostCenterPattern != "*")
                    {
                        var patterns = cfg.CostCenterPattern.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var pat in patterns)
                        {
                            var prefix = pat.TrimEnd('*');
                            if (costCenter.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                return cfg.AccountCode;
                        }
                    }
                }
                return salaryExpenses.FirstOrDefault(c => c.AccountKey == "EXPENSE_SALARY_ADMIN")?.AccountCode
                    ?? salaryExpenses.FirstOrDefault()?.AccountCode
                    ?? fallback;
            }

            return configs.FirstOrDefault(c => string.Equals(c.AccountKey, preferredKey, StringComparison.OrdinalIgnoreCase))?.AccountCode
                ?? fallback;
        }

        string acctCompIns = ResolveAccountCode("EXPENSE_INSURANCE_COMP", fallback: "6421");
        string acctCompUnion = ResolveAccountCode("EXPENSE_UNION_COMP", fallback: "6421");
        string acctEmpPayable = ResolveAccountCode("PAYABLE_EMPLOYEE", fallback: "3341");
        string acctSi = ResolveAccountCode("PAYABLE_SI", fallback: "3383");
        string acctHi = ResolveAccountCode("PAYABLE_HI", fallback: "3384");
        string acctUi = ResolveAccountCode("PAYABLE_UI", fallback: "3386");
        string acctOa = ResolveAccountCode("PAYABLE_OA", fallback: "3389");
        string acctUnion = ResolveAccountCode("PAYABLE_UNION", fallback: "3382");
        string acctPit = ResolveAccountCode("PAYABLE_PIT", fallback: "3335");
        string acctAdv = ResolveAccountCode("RECEIVABLE_ADVANCE", fallback: "1411");
        string acctOtherRec = ResolveAccountCode("OTHER_RECOVERY", fallback: "1388");

        var lines = new List<PayrollAccountingJournalLineDto>();
        int lineNo = 1;

        var ccGroups = records
            .GroupBy(r => string.IsNullOrWhiteSpace(r.OrganizationUnit.CostCenterCode)
                ? r.OrganizationUnit.UnitCode
                : r.OrganizationUnit.CostCenterCode!)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var g in ccGroups)
        {
            string cc = g.Key;
            decimal gross = g.Sum(x => x.GrossIncome);
            decimal empSi = g.Sum(x => x.EmployeeSocialInsurance);
            decimal empHi = g.Sum(x => x.EmployeeHealthInsurance);
            decimal empUi = g.Sum(x => x.EmployeeUnemploymentInsurance);
            decimal empUnion = g.Sum(x => x.EmployeeUnionFee);
            decimal pit = g.Sum(x => x.PersonalIncomeTax);
            decimal adv = g.Sum(x => x.SalaryAdvanceDeduction);
            decimal otherDed = g.Sum(x => x.LoanInstallmentDeduction + x.DisciplineDeduction + x.RetroDeductionAmount);
            decimal net = g.Sum(x => x.NetSalary);
            decimal compSi = g.Sum(x => x.CompanySocialInsurance);
            decimal compHi = g.Sum(x => x.CompanyHealthInsurance);
            decimal compUi = g.Sum(x => x.CompanyUnemploymentInsurance);
            decimal compOa = g.Sum(x => x.CompanyOccupationalAccidentInsurance);
            decimal compUnion = g.Sum(x => x.CompanyUnionFee);

            string acctSalaryExp = ResolveAccountCode("EXPENSE_SALARY", cc, "6421");

            // 1. Nợ Chi phí lương theo CostCenter / Có 3341 Toàn bộ Thu nhập Gross (sau đó khấu trừ qua Nợ 3341 để Có 3341 còn đúng Thực lĩnh Net!)
            // Cách hạch toán chuẩn VAS: Nợ 6xx / Có 3341 = Tổng thu nhập trả NLĐ; Nợ 3341 / Có 338x, 3335, 141, 1388 = Các khoản khấu trừ;
            // Số dư Có TK 3341 còn lại CHÍNH XÁC BẰNG TỔNG THỰC LĨNH (NET) CHUYỂN KHOẢN NGÂN HÀNG!
            if (gross > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "EXPENSE_SALARY", acctSalaryExp, acctEmpPayable, cc,
                    $"Hạch toán chi phí tiền lương kỳ {period.PeriodCode} ({cc})", gross));
            }
            if (empSi > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_SI_EMP", acctEmpPayable, acctSi, cc,
                    $"Khấu trừ BHXH phần NLĐ đóng ({cc})", empSi));
            }
            if (empHi > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_HI_EMP", acctEmpPayable, acctHi, cc,
                    $"Khấu trừ BHYT phần NLĐ đóng ({cc})", empHi));
            }
            if (empUi > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_UI_EMP", acctEmpPayable, acctUi, cc,
                    $"Khấu trừ BHTN phần NLĐ đóng ({cc})", empUi));
            }
            if (empUnion > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_UNION_EMP", acctEmpPayable, acctUnion, cc,
                    $"Khấu trừ Đoàn phí CĐ phần NLĐ ({cc})", empUnion));
            }
            if (pit > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_PIT", acctEmpPayable, acctPit, cc,
                    $"Khấu trừ Thuế TNCN phải nộp ({cc})", pit));
            }
            if (adv > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "RECEIVABLE_ADVANCE", acctEmpPayable, acctAdv, cc,
                    $"Thu hồi tạm ứng lương qua bảng lương ({cc})", adv));
            }
            if (otherDed > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "OTHER_RECOVERY", acctEmpPayable, acctOtherRec, cc,
                    $"Thu hồi trả góp khoản vay / truy thu / phạt ({cc})", otherDed));
            }

            // Điều chỉnh cân bằng nếu kỳ lương phát sinh hoàn thuế TNCN từ truy thu kỳ gốc (Credit 3341 > Gross - Ded)
            decimal netCredit334Remaining = gross - empSi - empHi - empUi - empUnion - pit - adv - otherDed;
            if (net > netCredit334Remaining)
            {
                decimal refundAdj = net - netCredit334Remaining;
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PIT_REFUND_ADJUST", acctPit, acctEmpPayable, cc,
                    $"Hoàn/Điều chỉnh giảm thuế TNCN từ truy thu kỳ gốc ({cc})", refundAdj));
            }

            // 2. Chi phí bảo hiểm & KPCĐ phần doanh nghiệp đóng (Nợ 6xx / Có 338x)
            if (compSi > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_SI_COMP", acctCompIns, acctSi, cc,
                    $"Trích BHXH phần doanh nghiệp đóng ({cc})", compSi));
            }
            if (compHi > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_HI_COMP", acctCompIns, acctHi, cc,
                    $"Trích BHYT phần doanh nghiệp đóng ({cc})", compHi));
            }
            if (compUi > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_UI_COMP", acctCompIns, acctUi, cc,
                    $"Trích BHTN phần doanh nghiệp đóng ({cc})", compUi));
            }
            if (compOa > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_OA_COMP", acctCompIns, acctOa, cc,
                    $"Trích BHTNLĐ-BNN phần doanh nghiệp đóng ({cc})", compOa));
            }
            if (compUnion > 0m)
            {
                lines.Add(new PayrollAccountingJournalLineDto(lineNo++, "PAYABLE_UNION_COMP", acctCompUnion, acctUnion, cc,
                    $"Trích Kinh phí công đoàn 2% phần doanh nghiệp ({cc})", compUnion));
            }
        }

        decimal totalAmount = lines.Sum(l => l.Amount);
        decimal netPayable334 = lines.Where(l => l.CreditAccountCode == acctEmpPayable).Sum(l => l.Amount)
            - lines.Where(l => l.DebitAccountCode == acctEmpPayable).Sum(l => l.Amount);

        return new PayrollAccountingJournalDto(
            period.Id,
            period.PeriodCode,
            period.PeriodTitle,
            period.EndDate,
            lines,
            totalAmount,
            totalAmount,
            netPayable334,
            totalAmount >= 0m && Math.Abs(netPayable334 - records.Sum(r => r.NetSalary)) == 0m);
    }
}

public sealed record ExportAccountingJournalQuery(int PayrollPeriodId, string Format = "excel")
    : IRequest<BankTransferExportResult>;

public sealed class ExportAccountingJournalQueryHandler : IRequestHandler<ExportAccountingJournalQuery, BankTransferExportResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPayrollDocumentService _documentService;

    public ExportAccountingJournalQueryHandler(IApplicationDbContext context, IPayrollDocumentService documentService)
    {
        _context = context;
        _documentService = documentService;
    }

    public async Task<BankTransferExportResult> Handle(ExportAccountingJournalQuery request, CancellationToken cancellationToken)
    {
        var journal = await new GeneratePayrollAccountingEntriesQueryHandler(_context)
            .Handle(new GeneratePayrollAccountingEntriesQuery(request.PayrollPeriodId), cancellationToken);

        bool isCsv = string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase);
        var bytes = _documentService.ExportAccountingJournalFile(journal, isCsv ? "csv" : "excel");
        return new BankTransferExportResult(
            bytes,
            isCsv ? "text/csv; charset=utf-8" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"HachToanLuong_{journal.PeriodCode}.{(isCsv ? "csv" : "xlsx")}",
            isCsv ? "VAS_CSV" : "VAS_EXCEL",
            journal.Lines.Count,
            journal.PayableToEmployeesAccount334);
    }
}

// ============================================================================
// 5. DASHBOARD C&B (QUỸ LƯƠNG, CƠ CẤU THU NHẬP, TỶ LỆ OT, CHI PHÍ BH, TOP BIẾN ĐỘNG)
// ============================================================================

public sealed record TopPayrollVarianceEmployeeDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    decimal PreviousNetSalary,
    decimal CurrentNetSalary,
    decimal VarianceAmount,
    decimal VariancePercent);

public sealed record CompensationDashboardDto(
    int Year,
    int? FocusPeriodId,
    string FocusPeriodCode,
    decimal FocusTotalGross,
    decimal FocusBaseAndAllowances,
    decimal FocusOvertimePay,
    decimal FocusBonusesAndRetro,
    decimal FocusOtSharePercent,
    decimal FocusEmployeeInsurance,
    decimal FocusCompanyInsuranceAndUnion,
    decimal FocusTotalInsuranceCost,
    decimal FocusTotalPit,
    decimal FocusTotalNet,
    decimal FocusTotalCompanyCost,
    IReadOnlyList<PayrollFundMonthlyBucketDto> MonthlyFundTrend,
    IReadOnlyList<TopPayrollVarianceEmployeeDto> TopVariances);

public sealed record GetCompensationDashboardQuery(int Year, int? FocusPeriodId = null) : IRequest<CompensationDashboardDto>;

public sealed class GetCompensationDashboardQueryHandler : IRequestHandler<GetCompensationDashboardQuery, CompensationDashboardDto>
{
    private readonly IApplicationDbContext _context;

    public GetCompensationDashboardQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<CompensationDashboardDto> Handle(GetCompensationDashboardQuery request, CancellationToken cancellationToken)
    {
        var fund = await new GetPayrollFundSummaryQueryHandler(_context)
            .Handle(new GetPayrollFundSummaryQuery(request.Year), cancellationToken);

        PayrollPeriod? focusPeriod = request.FocusPeriodId.HasValue
            ? await _context.PayrollPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.FocusPeriodId.Value, cancellationToken)
            : await _context.PayrollPeriods.AsNoTracking()
                .Where(p => p.Year == request.Year && p.PeriodType != "MidMonthAdvance")
                .OrderByDescending(p => p.Month)
                .ThenByDescending(p => p.DisbursementSequence)
                .FirstOrDefaultAsync(cancellationToken);

        focusPeriod ??= await _context.PayrollPeriods.AsNoTracking()
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync(cancellationToken);

        if (focusPeriod == null)
        {
            return new CompensationDashboardDto(
                request.Year, null, "N/A",
                0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m,
                fund.MonthlyBuckets,
                Array.Empty<TopPayrollVarianceEmployeeDto>());
        }

        var recs = await _context.PayrollRecords.AsNoTracking()
            .Include(r => r.Employee)
            .Include(r => r.OrganizationUnit)
            .Where(r => r.PayrollPeriodId == focusPeriod.Id)
            .ToListAsync(cancellationToken);

        decimal totalGross = recs.Sum(r => r.GrossIncome);
        decimal baseAllow = recs.Sum(r => r.ActualBaseSalary + r.PositionAllowance + r.OtherAllowance);
        decimal otPay = recs.Sum(r => r.OvertimePay);
        decimal bonusesRetro = recs.Sum(r => r.KpiBonus + r.CommissionBonus + r.ThirteenthMonthBonus + r.RetroEarningAmount + r.UnusedLeavePayout);
        decimal otShare = totalGross > 0m ? Math.Round(otPay / totalGross * 100m, 2, MidpointRounding.AwayFromZero) : 0m;
        decimal empIns = recs.Sum(r => r.TotalEmployeeInsurance);
        decimal compInsUnion = recs.Sum(r => r.CompanySocialInsurance + r.CompanyHealthInsurance + r.CompanyUnemploymentInsurance + r.CompanyOccupationalAccidentInsurance + r.CompanyUnionFee);
        decimal totalPit = recs.Sum(r => r.PersonalIncomeTax);
        decimal totalNet = recs.Sum(r => r.NetSalary);
        decimal totalCompCost = recs.Sum(r => r.TotalCompanyCost);

        var topVariances = recs
            .Where(r => r.PreviousPeriodNetSalary.HasValue && r.PreviousPeriodNetSalary.Value > 0m)
            .Select(r =>
            {
                decimal prev = r.PreviousPeriodNetSalary!.Value;
                decimal diff = r.NetSalary - prev;
                decimal pct = r.VariancePercentVsPreviousPeriod ?? Math.Round(Math.Abs(diff) / prev * 100m, 2, MidpointRounding.AwayFromZero);
                return new TopPayrollVarianceEmployeeDto(
                    r.EmployeeId,
                    r.Employee.EmployeeCode,
                    r.Employee.FullName,
                    r.OrganizationUnit.UnitName,
                    prev,
                    r.NetSalary,
                    diff,
                    pct);
            })
            .OrderByDescending(v => Math.Abs(v.VarianceAmount))
            .Take(10)
            .ToList();

        return new CompensationDashboardDto(
            request.Year,
            focusPeriod.Id,
            focusPeriod.PeriodCode,
            totalGross,
            baseAllow,
            otPay,
            bonusesRetro,
            otShare,
            empIns,
            compInsUnion,
            empIns + compInsUnion,
            totalPit,
            totalNet,
            totalCompCost,
            fund.MonthlyBuckets,
            topVariances);
    }
}

// ============================================================================
// 6. CẤU HÌNH TÀI KHOẢN KẾ TOÁN & MẪU DỮ LIỆU BHXH ĐỘNG (DB-DRIVEN)
// ============================================================================

public sealed record GetAccountingAccountConfigsQuery : IRequest<IReadOnlyList<PayrollAccountingAccountConfig>>;

public sealed class GetAccountingAccountConfigsQueryHandler
    : IRequestHandler<GetAccountingAccountConfigsQuery, IReadOnlyList<PayrollAccountingAccountConfig>>
{
    private readonly IApplicationDbContext _context;

    public GetAccountingAccountConfigsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<PayrollAccountingAccountConfig>> Handle(
        GetAccountingAccountConfigsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.PayrollAccountingAccountConfigs.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }
}

public sealed record SaveAccountingAccountConfigCommand(
    int Id,
    string AccountCode,
    string CostCenterPattern,
    string LegalReference,
    bool NeedsHrVerification = false) : IRequest<int>;

public sealed class SaveAccountingAccountConfigCommandHandler : IRequestHandler<SaveAccountingAccountConfigCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public SaveAccountingAccountConfigCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SaveAccountingAccountConfigCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        var entity = await _context.PayrollAccountingAccountConfigs
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy cấu hình tài khoản kế toán #{request.Id}.");

        var oldAcc = entity.AccountCode;
        entity.AccountCode = request.AccountCode.Trim();
        entity.CostCenterPattern = string.IsNullOrWhiteSpace(request.CostCenterPattern) ? "*" : request.CostCenterPattern.Trim();
        entity.LegalReference = string.IsNullOrWhiteSpace(request.LegalReference) ? "Thông tư 200/2014/TT-BTC" : request.LegalReference.Trim();
        entity.NeedsHrVerification = request.NeedsHrVerification;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "AccountingConfig_Update",
            EntityName = nameof(PayrollAccountingAccountConfig),
            EntityId = entity.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { AccountCode = oldAcc }),
            NewValuesJson = JsonSerializer.Serialize(new { entity.AccountCode, entity.CostCenterPattern, entity.LegalReference }),
            IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser?.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        });

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public sealed record GetSocialInsuranceTemplatesQuery : IRequest<IReadOnlyList<SocialInsuranceReportTemplate>>;

public sealed class GetSocialInsuranceTemplatesQueryHandler
    : IRequestHandler<GetSocialInsuranceTemplatesQuery, IReadOnlyList<SocialInsuranceReportTemplate>>
{
    private readonly IApplicationDbContext _context;

    public GetSocialInsuranceTemplatesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<SocialInsuranceReportTemplate>> Handle(
        GetSocialInsuranceTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.SocialInsuranceReportTemplates.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }
}

public sealed record SaveSocialInsuranceTemplateCommand(
    int Id,
    string ColumnMappingsJson,
    string SoftwareVendor,
    string LegalReference,
    bool NeedsHrVerification = false) : IRequest<int>;

public sealed class SaveSocialInsuranceTemplateCommandHandler : IRequestHandler<SaveSocialInsuranceTemplateCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUser;

    public SaveSocialInsuranceTemplateCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(SaveSocialInsuranceTemplateCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var username = _currentUser.RequireUsername();

        using var _ = JsonDocument.Parse(request.ColumnMappingsJson);

        var entity = await _context.SocialInsuranceReportTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy mẫu xuất BHXH #{request.Id}.");

        entity.ColumnMappingsJson = request.ColumnMappingsJson;
        entity.SoftwareVendor = request.SoftwareVendor.Trim();
        entity.LegalReference = request.LegalReference.Trim();
        entity.NeedsHrVerification = request.NeedsHrVerification;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Username = username,
            ModuleName = "Payroll",
            ActionType = "SiTemplate_Update",
            EntityName = nameof(SocialInsuranceReportTemplate),
            EntityId = entity.Id.ToString(),
            NewValuesJson = JsonSerializer.Serialize(new { entity.TemplateCode, entity.SoftwareVendor, entity.LegalReference }),
            IpAddress = _currentUser?.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser?.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username
        });

        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
