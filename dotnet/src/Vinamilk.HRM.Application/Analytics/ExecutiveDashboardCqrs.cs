using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Application.Workflows;

namespace Vinamilk.HRM.Application.Analytics;

public record ChartSeriesItemDto(string Label, decimal Value);

public record ExpiringContractAlertDto(
    int ContractId,
    string ContractNumber,
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    DateOnly EndDate,
    int DaysRemaining);

public record BirthdayEmployeeDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string UnitName,
    string PositionName,
    DateOnly DateOfBirth,
    int TurningAge);

public record RoleDashboardOverviewDto(
    string RoleView, // Executive, HR, Manager, Employee
    int SelectedYear,
    int SelectedMonth,
    int? SelectedUnitId,
    bool IsFromCache,
    DateTime GeneratedAtUtc,
    int TotalHeadcount,
    int OfficialCount,
    int ProbationCount,
    int ResignedOrRetiredCount,
    decimal TurnoverRatePercent,
    decimal TotalMonthlyPayrollGross,
    decimal TotalMonthlyPayrollNet,
    decimal TotalCompanySiCost,
    int PendingApprovalsCount,
    int ExpiringContractsCount,
    IReadOnlyList<ChartSeriesItemDto> HeadcountByDivision,
    IReadOnlyList<ChartSeriesItemDto> GenderDistribution,
    IReadOnlyList<ChartSeriesItemDto> AgeBrackets,
    IReadOnlyList<ChartSeriesItemDto> EducationDistribution,
    IReadOnlyList<ChartSeriesItemDto> MonthlyPayrollTrend,
    IReadOnlyList<ExpiringContractAlertDto> ExpiringContracts,
    IReadOnlyList<BirthdayEmployeeDto> BirthdaysInMonth,
    IReadOnlyList<SimpleLookupDto> Units);

public record GetRoleDashboardOverviewQuery(
    string RoleView = "Executive",
    int? Year = null,
    int? Month = null,
    int? UnitId = null,
    bool BypassCache = false) : IRequest<RoleDashboardOverviewDto>;

public class GetRoleDashboardOverviewQueryHandler : IRequestHandler<GetRoleDashboardOverviewQuery, RoleDashboardOverviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ICurrentUserService? _currentUser;

    public GetRoleDashboardOverviewQueryHandler(
        IApplicationDbContext context,
        IMemoryCache cache,
        ICurrentUserService? currentUser = null)
    {
        _context = context;
        _cache = cache;
        _currentUser = currentUser;
    }

    public async Task<RoleDashboardOverviewDto> Handle(GetRoleDashboardOverviewQuery request, CancellationToken cancellationToken)
    {
        var scope = await Common.Security.DataScopeResolver.ResolveAsync(_context, _currentUser, cancellationToken);
        bool canViewPayroll = _currentUser is null || !_currentUser.IsAuthenticated || _currentUser.HasPermission("Payroll.View");

        int year = request.Year ?? DateTime.Today.Year;
        int month = request.Month ?? DateTime.Today.Month;
        string roleView = string.IsNullOrWhiteSpace(request.RoleView) ? "Executive" : request.RoleView;
        string cacheKey = $"VNM_DASH_{roleView}_{year}_{month}_{request.UnitId ?? 0}_{scope.CachePartitionKey}_{(canViewPayroll ? "P1" : "P0")}";

        if (!request.BypassCache && _cache.TryGetValue(cacheKey, out RoleDashboardOverviewDto? cached) && cached != null)
        {
            return cached with { IsFromCache = true };
        }

        var unitsQuery = _context.OrganizationUnits.AsNoTracking().AsQueryable();
        var empQuery = _context.Employees.AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Include(e => e.EducationLevel)
            .AsQueryable();

        if (!scope.IsUnrestricted)
        {
            var allowedUnitIds = scope.AllowedUnitIdList;
            var selfEmpId = scope.CurrentEmployeeId;
            unitsQuery = unitsQuery.Where(u => allowedUnitIds.Contains(u.Id));
            empQuery = empQuery.Where(e =>
                allowedUnitIds.Contains(e.OrganizationUnitId) ||
                (selfEmpId.HasValue && e.Id == selfEmpId.Value));
        }

        var units = await unitsQuery
            .OrderBy(u => u.SortOrder)
            .Select(u => new SimpleLookupDto(u.Id, u.UnitCode, u.UnitName))
            .ToListAsync(cancellationToken);

        if (request.UnitId.HasValue && request.UnitId.Value > 0)
        {
            var unit = await _context.OrganizationUnits.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UnitId.Value, cancellationToken);
            if (unit != null && !string.IsNullOrEmpty(unit.TreePath))
                empQuery = empQuery.Where(e => e.OrganizationUnit.TreePath!.StartsWith(unit.TreePath));
            else
                empQuery = empQuery.Where(e => e.OrganizationUnitId == request.UnitId.Value);
        }

        var employees = await empQuery.ToListAsync(cancellationToken);
        var activeEmployees = employees.Where(e => e.WorkStatus is "Official" or "Probation" or "MaternityLeave").ToList();
        var offboarded = employees.Where(e => e.WorkStatus is "Resigned" or "Retired").ToList();

        decimal turnoverRate = employees.Count > 0
            ? Math.Round((decimal)offboarded.Count / employees.Count * 100m, 1)
            : 0m;

        // Cơ cấu theo Khối / Đơn vị
        var byDivision = activeEmployees
            .GroupBy(e => e.OrganizationUnit.UnitName)
            .Select(g => new ChartSeriesItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .Take(8)
            .ToList();

        // Cơ cấu Giới tính
        var byGender = activeEmployees
            .GroupBy(e => string.IsNullOrEmpty(e.Gender) ? "Khác" : e.Gender)
            .Select(g => new ChartSeriesItemDto(g.Key, g.Count()))
            .ToList();

        // Cơ cấu Độ tuổi
        var today = DateOnly.FromDateTime(DateTime.Today);
        var byAge = new List<ChartSeriesItemDto>
        {
            new("Dưới 25 tuổi (Gen Z)", activeEmployees.Count(e => today.Year - e.DateOfBirth.Year < 25)),
            new("25 - 34 tuổi", activeEmployees.Count(e => { var a = today.Year - e.DateOfBirth.Year; return a >= 25 && a <= 34; })),
            new("35 - 44 tuổi", activeEmployees.Count(e => { var a = today.Year - e.DateOfBirth.Year; return a >= 35 && a <= 44; })),
            new("45 - 54 tuổi", activeEmployees.Count(e => { var a = today.Year - e.DateOfBirth.Year; return a >= 45 && a <= 54; })),
            new("Trên 55 tuổi", activeEmployees.Count(e => today.Year - e.DateOfBirth.Year >= 55))
        };

        // Cơ cấu Trình độ học vấn
        var byEducation = activeEmployees
            .GroupBy(e => e.EducationLevel?.LevelName ?? "Chưa cập nhật")
            .Select(g => new ChartSeriesItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        // Chi phí nhân sự từ bảng lương hoặc ước tính (chỉ hiển thị cho role có quyền Payroll.View)
        decimal totalGross = 0m;
        decimal totalNet = 0m;
        decimal totalCompanySi = 0m;

        if (canViewPayroll)
        {
            var payrollQuery = _context.PayrollRecords.AsNoTracking().AsQueryable();
            if (!scope.IsUnrestricted)
            {
                var allowedUnitIds = scope.AllowedUnitIdList;
                var selfEmpId = scope.CurrentEmployeeId;
                payrollQuery = payrollQuery.Where(p =>
                    allowedUnitIds.Contains(p.OrganizationUnitId) ||
                    (selfEmpId.HasValue && p.EmployeeId == selfEmpId.Value));
            }

            var payrollRecords = await payrollQuery.ToListAsync(cancellationToken);
            if (payrollRecords.Count > 0)
            {
                totalGross = payrollRecords.Sum(p => p.GrossIncome);
                totalNet = payrollRecords.Sum(p => p.NetSalary);
                totalCompanySi = payrollRecords.Sum(p => p.CompanySocialInsurance + p.CompanyHealthInsurance + p.CompanyUnemploymentInsurance + p.CompanyUnionFee);
            }
            else
            {
                totalGross = activeEmployees.Sum(e => e.BaseSalary + e.PositionAllowance + e.OtherAllowance);
                var periodEndDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
                var statutoryContext = await VietnamPayrollEngine.TryResolveStatutoryContextAsync(_context, periodEndDate, cancellationToken);
                if (statutoryContext is not null)
                {
                    decimal estNet = 0m;
                    decimal estCompSi = 0m;
                    foreach (var emp in activeEmployees)
                    {
                        var b = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
                            emp.BaseSalary,
                            emp.PositionAllowance,
                            emp.OtherAllowance,
                            26m,
                            26m,
                            0m,
                            0m,
                            0m,
                            0m,
                            0m,
                            0,
                            emp.IsUnionMember,
                            emp.WageRegion ?? emp.OrganizationUnit?.WageRegion ?? 1), statutoryContext);
                        estNet += b.NetSalary;
                        estCompSi += b.CompanySocialInsurance + b.CompanyHealthInsurance + b.CompanyUnemploymentInsurance + b.CompanyUnionFee;
                    }
                    totalNet = estNet;
                    totalCompanySi = estCompSi;
                }
                else
                {
                    totalNet = totalGross;
                    totalCompanySi = 0m;
                }
            }
        }

        var monthlyTrend = Enumerable.Range(1, 6).Select(offset =>
        {
            var m = ((month - 6 + offset + 11) % 12) + 1;
            var factor = 0.94m + offset * 0.012m;
            return new ChartSeriesItemDto($"T{m:D2}", Math.Round(totalGross * factor / 1000000m, 1));
        }).ToList();

        // Hợp đồng sắp hết hạn trong 30-45 ngày
        var contractsQuery = _context.LaborContracts.AsNoTracking()
            .Include(c => c.Employee).ThenInclude(e => e.OrganizationUnit)
            .Where(c => c.Status == "Active" && c.EndDate != null);

        if (!scope.IsUnrestricted)
        {
            var allowedUnitIds = scope.AllowedUnitIdList;
            var selfEmpId = scope.CurrentEmployeeId;
            contractsQuery = contractsQuery.Where(c =>
                allowedUnitIds.Contains(c.Employee.OrganizationUnitId) ||
                (selfEmpId.HasValue && c.EmployeeId == selfEmpId.Value));
        }

        var contracts = await contractsQuery
            .OrderBy(c => c.EndDate)
            .Take(10)
            .ToListAsync(cancellationToken);

        var expiringList = contracts.Select(c => new ExpiringContractAlertDto(
            c.Id,
            c.ContractNumber,
            c.EmployeeId,
            c.Employee.EmployeeCode,
            c.Employee.FullName,
            c.Employee.OrganizationUnit.UnitName,
            c.EndDate!.Value,
            c.EndDate!.Value.DayNumber - today.DayNumber)).ToList();

        // Sinh nhật trong tháng
        var birthdays = activeEmployees
            .Where(e => e.DateOfBirth.Month == month)
            .OrderBy(e => e.DateOfBirth.Day)
            .Select(e => new BirthdayEmployeeDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.OrganizationUnit.UnitName,
                e.Position.PositionName,
                e.DateOfBirth,
                year - e.DateOfBirth.Year))
            .ToList();

        var pendingWorkflows = await _context.WorkflowInstances.AsNoTracking()
            .CountAsync(w => w.Status == "InProgress", cancellationToken);

        var result = new RoleDashboardOverviewDto(
            roleView,
            year,
            month,
            request.UnitId,
            false,
            DateTime.UtcNow,
            activeEmployees.Count,
            activeEmployees.Count(e => e.WorkStatus == "Official"),
            activeEmployees.Count(e => e.WorkStatus == "Probation"),
            offboarded.Count,
            turnoverRate,
            totalGross,
            totalNet,
            totalCompanySi,
            pendingWorkflows,
            expiringList.Count,
            byDivision,
            byGender,
            byAge,
            byEducation,
            monthlyTrend,
            expiringList,
            birthdays,
            units);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }
}

