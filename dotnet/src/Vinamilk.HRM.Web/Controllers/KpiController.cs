using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Talent;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize(Policy = "Kpi.View")]
public class KpiController : Controller
{
    private readonly ISender _sender;

    public KpiController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? cycleId = null, CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetKpiOverviewQuery(cycleId), cancellationToken);
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = "Kpi.Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCycle(
        string cycleCode,
        string cycleName,
        string cycleType,
        int year,
        int periodNumber,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var id = await _sender.Send(new CreateEvaluationCycleCommand(
            cycleCode, cycleName, cycleType, year, periodNumber, startDate, endDate), cancellationToken);
        TempData["SuccessMessage"] = $"Đã tạo chu kỳ đánh giá '{cycleName}' thành công.";
        return RedirectToAction(nameof(Index), new { cycleId = id });
    }

    [HttpPost]
    [Authorize(Policy = "Kpi.Evaluate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployeeKpi(
        int evaluationCycleId,
        int employeeId,
        int? reviewerEmployeeId,
        string goalTitle,
        string perspective,
        string measureUnit,
        decimal targetValue,
        decimal actualValue,
        decimal selfScore,
        string? employeeComment,
        CancellationToken cancellationToken = default)
    {
        await _sender.Send(new CreateEmployeeKpiCommand(
            evaluationCycleId, employeeId, reviewerEmployeeId, goalTitle,
            perspective, measureUnit, targetValue, actualValue, selfScore, employeeComment), cancellationToken);
        TempData["SuccessMessage"] = "Đã tạo phiếu giao chỉ tiêu & Tự đánh giá KPI/OKR, chuyển sang bước Quản lý đánh giá.";
        return RedirectToAction(nameof(Index), new { cycleId = evaluationCycleId });
    }

    [HttpPost]
    [Authorize(Policy = "Kpi.Evaluate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalibrateAndApprove(
        int evaluationCycleId,
        int evaluationId,
        decimal managerScore,
        decimal? calibratedFinalScore,
        string? reviewerAnd360Comment,
        CancellationToken cancellationToken = default)
    {
        var msg = await _sender.Send(new CalibrateAndApproveKpiCommand(
            evaluationId, managerScore, calibratedFinalScore, reviewerAnd360Comment), cancellationToken);
        TempData["SuccessMessage"] = msg;
        return RedirectToAction(nameof(Index), new { cycleId = evaluationCycleId });
    }
}

