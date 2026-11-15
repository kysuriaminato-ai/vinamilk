using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Talent;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize(Policy = "Rewards.View")]
public class RewardsController : Controller
{
    private readonly ISender _sender;
    private readonly TalentDocumentService _docService;

    public RewardsController(ISender sender, TalentDocumentService docService)
    {
        _sender = sender;
        _docService = docService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year = null, string? tab = null, CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetRewardsAndBm03OverviewQuery(year), cancellationToken);
        ViewBag.ActiveTab = tab ?? "decisions";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDecision(
        int employeeId,
        int categoryId,
        DateOnly decisionDate,
        DateOnly effectiveDate,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var id = await _sender.Send(new CreateRewardDisciplineCommand(
            employeeId, categoryId, decisionDate, effectiveDate, amount, reason, true), cancellationToken);
        TempData["SuccessMessage"] = $"Đã ban hành Quyết định Khen thưởng / Kỷ luật #{id} và đồng bộ vào Báo cáo BM03.";
        return RedirectToAction(nameof(Index), new { tab = "decisions" });
    }

    [HttpGet]
    public async Task<IActionResult> PrintDecision(int id, CancellationToken cancellationToken = default)
    {
        var detail = await _sender.Send(new GetRewardDisciplinePrintDetailQuery(id), cancellationToken);
        if (detail == null) return NotFound();
        return View(detail);
    }

    [HttpGet]
    public async Task<IActionResult> ExportBm03Excel(int? year = null, CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetRewardsAndBm03OverviewQuery(year), cancellationToken);
        var bytes = _docService.ExportBm03RewardDisciplineExcel(model);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Vinamilk_BaoCao_BM03_{model.Year}.xlsx");
    }
}

