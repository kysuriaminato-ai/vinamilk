using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Application.Workflows;

namespace Vinamilk.HRM.Web.Areas.ESS.Controllers;

[Area("ESS")]
[Authorize]
public class HomeController : Controller
{
    private readonly IMediator _mediator;

    public HomeController(IMediator mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(
        int? employeeId = null,
        string tab = "home",
        int? timesheetYear = null,
        int? timesheetMonth = null,
        int? pitYear = null,
        CancellationToken cancellationToken = default)
    {
        var model = await _mediator.Send(
            new GetEssMssPortalQuery(employeeId, timesheetYear, timesheetMonth, pitYear),
            cancellationToken);
        ViewBag.ActiveTab = tab.ToLowerInvariant();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitLeave(
        int employeeId,
        int leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(new SubmitLeaveRequestCommand(
            employeeId, leaveTypeId, startDate, endDate, "Full", "Full", null, reason), cancellationToken);

        TempData["EssMessage"] = $"Đã gửi đơn xin nghỉ phép (#{id}) thành công!";
        return RedirectToAction(nameof(Index), new { employeeId, tab = "requests" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOvertime(
        int employeeId,
        DateOnly overtimeDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string overtimeRateType,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(new SubmitOvertimeRequestCommand(
            employeeId, overtimeDate, startTime, endTime, overtimeRateType, reason), cancellationToken);

        TempData["EssMessage"] = $"Đã gửi đăng ký tăng ca OT (#{id}) thành công!";
        return RedirectToAction(nameof(Index), new { employeeId, tab = "requests" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitSalaryAdvance(
        int employeeId,
        DateOnly requestDate,
        int targetYear,
        int targetMonth,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _mediator.Send(
                new SubmitEssSalaryAdvanceCommand(employeeId, requestDate, targetYear, targetMonth, amount, reason),
                cancellationToken);
            TempData["EssMessage"] = $"Đã gửi đề nghị tạm ứng lương {res.AdvanceNumber} ({res.Amount:N0} đ) vào luồng phê duyệt (Workflow #{res.WorkflowInstanceId}).";
        }
        catch (Exception ex)
        {
            TempData["EssError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { employeeId, tab = "requests" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SUPER_ADMIN,HR_DIRECTOR,HR_MANAGER,DEPARTMENT_MANAGER")]
    public async Task<IActionResult> ProcessMssApproval(
        int employeeId,
        int workflowInstanceId,
        string actionType,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ProcessWorkflowActionCommand(workflowInstanceId, actionType, comment), cancellationToken);

        TempData["EssMessage"] = result;
        return RedirectToAction(nameof(Index), new { employeeId, tab = "mss" });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPayslip(long payrollRecordId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(new GetPayrollRecordPdfQuery(payrollRecordId, EnforceSelfOnly: true), cancellationToken);
            return File(result.PdfBytes, "application/pdf", result.FileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPitCertificate(int employeeId, int taxYear, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(
                new IssueAndGetPitCertificatePdfQuery(employeeId, taxYear, PersistCertificate: true, EnforceSelfOnly: true),
                cancellationToken);
            return File(result.PdfBytes, "application/pdf", result.FileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

