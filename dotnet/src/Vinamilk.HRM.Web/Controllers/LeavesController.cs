using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.TimeAttendance;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize]
public class LeavesController : Controller
{
    private readonly ISender _sender;

    public LeavesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? year = null,
        int? month = null,
        int? unitId = null,
        CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetLeaveManagementOverviewQuery(year, month, unitId), cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitLeaveRequest(
        int employeeId,
        int leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        string startSession,
        string endSession,
        int? handoverToEmployeeId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _sender.Send(new SubmitLeaveRequestCommand(
                employeeId,
                leaveTypeId,
                startDate,
                endDate,
                startSession,
                endSession,
                handoverToEmployeeId,
                reason), cancellationToken);

            TempData["SuccessMessage"] = $"Đã tạo đơn nghỉ phép / công tác #{id} thành công và chuyển vào luồng phê duyệt Workflow Engine (SignalR + Email)!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year = startDate.Year, month = startDate.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecalculateEntitlement(
        int year,
        decimal carryForwardCap = 5.0m,
        CancellationToken cancellationToken = default)
    {
        var count = await _sender.Send(new RecalculateAnnualLeaveEntitlementCommand(year, carryForwardCap), cancellationToken);
        TempData["SuccessMessage"] = $"Đã tính lại quỹ phép năm {year} theo thâm niên (Điều 113 & 114 BLLĐ 2019: +1 ngày / 5 năm) và kết chuyển phép tồn cho {count} nhân sự!";
        return RedirectToAction(nameof(Index), new { year });
    }
}

