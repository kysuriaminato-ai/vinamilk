using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Talent;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize]
public class OnboardingController : Controller
{
    private readonly ISender _sender;

    public OnboardingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? tab = null, CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetOnboardingAndAssetOverviewQuery(), cancellationToken);
        ViewBag.ActiveTab = tab ?? "tasks";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateChecklist(
        int employeeId,
        string processType,
        int? picEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var count = await _sender.Send(new GenerateEmployeeChecklistFromTemplateCommand(employeeId, processType, picEmployeeId), cancellationToken);
        TempData["SuccessMessage"] = $"Đã sinh {count} đầu việc {processType} từ mẫu chuẩn và giao cho bộ phận/cán bộ phụ trách!";
        return RedirectToAction(nameof(Index), new { tab = "tasks" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTask(int taskId, CancellationToken cancellationToken = default)
    {
        await _sender.Send(new ToggleOnboardingTaskCommand(taskId), cancellationToken);
        TempData["SuccessMessage"] = "Đã cập nhật trạng thái hoàn thành công việc Checklist.";
        return RedirectToAction(nameof(Index), new { tab = "tasks" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignAsset(
        int assetId,
        int employeeId,
        string? conditionOnAssign,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = await _sender.Send(new AssignAssetToEmployeeCommand(assetId, employeeId, conditionOnAssign), cancellationToken);
            TempData["SuccessMessage"] = msg;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "assets" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnAsset(
        int assetId,
        string? conditionOnReturn,
        CancellationToken cancellationToken = default)
    {
        var msg = await _sender.Send(new ReturnAssetCommand(assetId, conditionOnReturn), cancellationToken);
        TempData["SuccessMessage"] = msg;
        return RedirectToAction(nameof(Index), new { tab = "assets" });
    }
}

