using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize]
public class WorkflowsController : Controller
{
    private readonly ISender _sender;
    private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;

    public WorkflowsController(
        ISender sender,
        IRealtimeNotificationDispatcher realtimeDispatcher)
    {
        _sender = sender;
        _realtimeDispatcher = realtimeDispatcher;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? entityType = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetWorkflowCenterQuery(entityType, status), cancellationToken);
        ViewBag.EntityTypeFilter = entityType;
        ViewBag.StatusFilter = status;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessAction(
        int workflowInstanceId,
        string actionType,
        string? comment,
        int? delegateToUserId,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = await _sender.Send(new ProcessWorkflowActionCommand(
                workflowInstanceId,
                actionType,
                comment,
                delegateToUserId), cancellationToken);

            TempData["SuccessMessage"] = msg;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDefinition(
        int? id,
        string workflowCode,
        string workflowName,
        string entityType,
        string? description,
        string step1Name,
        string step1ApproverType,
        int? step1RoleId,
        int? step1UserId,
        int step1SlaHours,
        string? step2Name,
        string? step2ApproverType,
        int? step2RoleId,
        int? step2UserId,
        string? step2Condition,
        int step2SlaHours = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var steps = new List<SaveWorkflowStepInput>
            {
                new(1, step1Name, step1ApproverType, step1RoleId, step1UserId, "Always", step1SlaHours)
            };

            if (!string.IsNullOrWhiteSpace(step2Name))
            {
                steps.Add(new SaveWorkflowStepInput(
                    2,
                    step2Name,
                    step2ApproverType ?? "SpecificRole",
                    step2RoleId,
                    step2UserId,
                    step2Condition ?? "Always",
                    step2SlaHours));
            }

            await _sender.Send(new SaveWorkflowDefinitionCommand(
                id,
                workflowCode,
                workflowName,
                entityType,
                description,
                true,
                steps), cancellationToken);

            TempData["SuccessMessage"] = $"Đã lưu cấu hình quy trình '{workflowName}' thành công.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestRealtimeBroadcast(CancellationToken cancellationToken)
    {
        await _realtimeDispatcher.DispatchToUserAsync(
            null,
            "[SignalR Realtime Test] Thông báo Phê duyệt Khẩn",
            "Hệ thống Workflow Engine Vinamilk HRM vừa phát tín hiệu Realtime qua WebSockets/SignalR thành công!",
            "WorkflowApproval",
            "/Workflows",
            cancellationToken);

        TempData["SuccessMessage"] = "Đã phát thông báo Realtime qua SignalR tới toàn bộ trình duyệt đang mở!";
        return RedirectToAction(nameof(Index));
    }
}

