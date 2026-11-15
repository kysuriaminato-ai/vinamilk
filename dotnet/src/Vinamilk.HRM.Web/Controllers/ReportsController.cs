using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize(Policy = "Reports.View")]
public class ReportsController : Controller
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;
    private readonly AnalyticsReportExcelService _excelService;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(
        IMediator mediator,
        IApplicationDbContext context,
        AnalyticsReportExcelService excelService,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _context = context;
        _excelService = excelService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(
        string tab = "BM01",
        int? unitId = null,
        int? year = null,
        int? month = null,
        string? workStatus = null,
        string? recordType = null,
        List<string>? cols = null,
        string groupBy = "None",
        CancellationToken cancellationToken = default)
    {
        var activeYear = year ?? DateTime.Today.Year;
        var activeMonth = month ?? DateTime.Today.Month;

        ViewBag.Tab = tab.ToUpperInvariant();
        ViewBag.UnitId = unitId;
        ViewBag.Year = activeYear;
        ViewBag.Month = activeMonth;
        ViewBag.WorkStatus = workStatus;
        ViewBag.RecordType = recordType;
        ViewBag.GroupBy = groupBy;
        ViewBag.SelectedCols = cols is { Count: > 0 }
            ? cols
            : new List<string> { "EmployeeCode", "FullName", "UnitName", "PositionName", "WorkStatus", "BaseSalary" };
        ViewBag.AllColumns = DynamicReportCatalog.AllColumns;

        var scope = await Application.Common.Security.DataScopeResolver.ResolveAsync(_context, _currentUser, cancellationToken);
        var unitsQuery = _context.OrganizationUnits
            .AsNoTracking()
            .Where(u => u.IsActive);

        if (!scope.IsUnrestricted)
        {
            var allowedUnitIds = scope.AllowedUnitIdList;
            unitsQuery = unitsQuery.Where(u => allowedUnitIds.Contains(u.Id));
        }

        ViewBag.Units = await unitsQuery
            .OrderBy(u => u.UnitName)
            .Select(u => new { u.Id, u.UnitName })
            .ToListAsync(cancellationToken);

        switch (ViewBag.Tab)
        {
            case "BM02":
                ViewBag.Bm02 = await _mediator.Send(new GetBm02MovementReportQuery(activeYear, null, unitId), cancellationToken);
                break;
            case "BM03":
                ViewBag.Bm03 = await _mediator.Send(new GetBm03RewardDisciplineReportQuery(activeYear, recordType), cancellationToken);
                break;
            case "BM04":
                ViewBag.Bm04 = await _mediator.Send(new GetBm04PayrollSummaryReportQuery(activeYear, activeMonth), cancellationToken);
                break;
            case "DYNAMIC":
                ViewBag.Dynamic = await _mediator.Send(new RunDynamicReportQuery(ViewBag.SelectedCols, unitId, workStatus, groupBy), cancellationToken);
                break;
            case "SCHEDULE":
                ViewBag.Schedules = await _mediator.Send(new GetReportSchedulesQuery(), cancellationToken);
                break;
            default:
                ViewBag.Tab = "BM01";
                ViewBag.Bm01 = await _mediator.Send(new GetBm01EmployeeMasterReportQuery(unitId, workStatus), cancellationToken);
                break;
        }

        return View();
    }

    [HttpGet]
    [Authorize(Policy = "Reports.Export")]
    public async Task<IActionResult> ExportExcel(
        string tab = "BM01",
        int? unitId = null,
        int? year = null,
        int? month = null,
        string? workStatus = null,
        string? recordType = null,
        List<string>? cols = null,
        string groupBy = "None",
        CancellationToken cancellationToken = default)
    {
        var activeYear = year ?? DateTime.Today.Year;
        var activeMonth = month ?? DateTime.Today.Month;
        const string mime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return tab.ToUpperInvariant() switch
        {
            "BM02" => File(_excelService.ExportBm02(await _mediator.Send(new GetBm02MovementReportQuery(activeYear, null, unitId), cancellationToken)), mime, $"BM02_BienDong_{activeYear}.xlsx"),
            "BM03" => File(_excelService.ExportBm03(await _mediator.Send(new GetBm03RewardDisciplineReportQuery(activeYear, recordType), cancellationToken)), mime, $"BM03_KhenThuongKyLuat_{activeYear}.xlsx"),
            "BM04" => File(_excelService.ExportBm04(await _mediator.Send(new GetBm04PayrollSummaryReportQuery(activeYear, activeMonth), cancellationToken)), mime, $"BM04_QuyLuong_{activeMonth}_{activeYear}.xlsx"),
            "DYNAMIC" => File(_excelService.ExportDynamicReport(await _mediator.Send(new RunDynamicReportQuery(cols, unitId, workStatus, groupBy), cancellationToken)), mime, $"BaoCaoTuyBien_{DateTime.Today:yyyyMMdd}.xlsx"),
            _ => File(_excelService.ExportBm01(await _mediator.Send(new GetBm01EmployeeMasterReportQuery(unitId, workStatus), cancellationToken)), mime, $"BM01_NhanSu_{DateTime.Today:yyyyMMdd}.xlsx")
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSchedule(string reportCode, string frequency, string recipients, bool isActive = true, CancellationToken cancellationToken = default)
    {
        var saved = await _mediator.Send(new SaveReportScheduleCommand(reportCode, frequency, recipients, isActive), cancellationToken);
        TempData["SuccessMessage"] = $"Đã lưu lịch gửi tự động {saved.ReportName} ({saved.Frequency}).";
        return RedirectToAction(nameof(Index), new { tab = "SCHEDULE" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendScheduleNow(string scheduleId, CancellationToken cancellationToken = default)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var msg = await _mediator.Send(new SendScheduledReportCommand(scheduleId, baseUrl), cancellationToken);
        TempData["SuccessMessage"] = msg;
        return RedirectToAction(nameof(Index), new { tab = "SCHEDULE" });
    }
}

