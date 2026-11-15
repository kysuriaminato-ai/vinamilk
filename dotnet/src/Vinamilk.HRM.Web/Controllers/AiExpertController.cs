using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Analytics;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize]
public class AiExpertController : Controller
{
    private readonly IMediator _mediator;

    public AiExpertController(IMediator mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(int? jobPostingId = null, string? question = null, CancellationToken cancellationToken = default)
    {
        var overview = await _mediator.Send(new GetAiExpertOverviewQuery(jobPostingId), cancellationToken);
        if (!string.IsNullOrWhiteSpace(question))
        {
            ViewBag.PolicyAnswer = await _mediator.Send(new AskAiPolicyRagQuery(question), cancellationToken);
        }
        return View(overview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AskPolicy(string question, CancellationToken cancellationToken = default)
    {
        var answer = await _mediator.Send(new AskAiPolicyRagQuery(question), cancellationToken);
        var overview = await _mediator.Send(new GetAiExpertOverviewQuery(), cancellationToken);
        ViewBag.PolicyAnswer = answer;
        return View("Index", overview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SUPER_ADMIN,HR_DIRECTOR,HR_MANAGER")]
    public async Task<IActionResult> ScanAttrition(CancellationToken cancellationToken = default)
    {
        var added = await _mediator.Send(new ScanAndPersistAttritionRisksCommand(), cancellationToken);
        TempData["AiMessage"] = added > 0
            ? $"Đã quét và lưu {added} cảnh báo nguy cơ nghỉ việc mới vào hệ thống (AiRecommendations)."
            : "Đã quét dữ liệu nhân sự — không có cảnh báo mới cần bổ sung.";
        return RedirectToAction(nameof(Index));
    }
}

