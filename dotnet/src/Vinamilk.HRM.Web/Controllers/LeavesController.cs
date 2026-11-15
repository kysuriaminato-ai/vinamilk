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
        string leaveDurationMode = "Days",
        decimal? requestedHours = null,
        string? attachmentUrl = null,
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
                reason,
                leaveDurationMode,
                requestedHours,
                attachmentUrl), cancellationToken);

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
        try
        {
            var count = await _sender.Send(new RecalculateAnnualLeaveEntitlementCommand(year, carryForwardCap), cancellationToken);
            TempData["SuccessMessage"] = $"Đã tính lại quỹ phép năm {year} theo thâm niên và kết chuyển phép tồn cho {count} nhân sự!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLeaveType(
        int? id,
        string leaveTypeCode,
        string leaveTypeName,
        bool isPaidByCompany,
        bool isPaidBySocialInsurance,
        bool countsAgainstAnnualLeave,
        bool allowHalfDay,
        bool allowHourly,
        bool requiresAttachment,
        decimal? maxDaysPerRequest,
        decimal maxDaysPerYear,
        bool allowCarryForward,
        string attendanceSymbol,
        int displayOrder = 0,
        bool isActive = true,
        string? legalReference = null,
        bool needsHrVerification = true,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var savedId = await _sender.Send(new SaveLeaveTypeCommand(
                id,
                leaveTypeCode,
                leaveTypeName,
                isPaidByCompany,
                isPaidBySocialInsurance,
                countsAgainstAnnualLeave,
                allowHalfDay,
                allowHourly,
                requiresAttachment,
                maxDaysPerRequest,
                maxDaysPerYear,
                allowCarryForward,
                attendanceSymbol,
                displayOrder,
                isActive,
                legalReference,
                needsHrVerification,
                description), cancellationToken);

            TempData["SuccessMessage"] = $"Đã lưu cấu hình loại nghỉ '{leaveTypeCode}' (#{savedId}) thành công!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitEmployeeTimeRequest(
        int employeeId,
        string requestType,
        string? subType,
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly? plannedTime,
        TimeOnly? proposedEndTime,
        int? requestedMinutes,
        string? locationOrDestination,
        string? attachmentUrl,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _sender.Send(new SubmitEmployeeTimeRequestCommand(
                employeeId,
                requestType,
                subType,
                startDate,
                endDate,
                plannedTime,
                proposedEndTime,
                requestedMinutes,
                locationOrDestination,
                attachmentUrl,
                reason), cancellationToken);

            TempData["SuccessMessage"] = $"Đã gửi đơn {requestType} #{id} thành công qua Workflow Engine!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year = startDate.Year, month = startDate.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitGroupOvertime(
        List<int> employeeIds,
        DateOnly overtimeDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string overtimeRateType,
        string compensationMode,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new SubmitGroupOvertimeRequestCommand(
                employeeIds,
                overtimeDate,
                startTime,
                endTime,
                overtimeRateType,
                compensationMode,
                reason), cancellationToken);

            TempData["SuccessMessage"] = $"Đã tạo lô đơn OT {result.OvertimeBatchCode} cho {result.CreatedRequestIds.Count} nhân sự!" +
                (result.Warnings.Count > 0 ? $" Lưu ý: {string.Join(" | ", result.Warnings)}" : string.Empty);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year = overtimeDate.Year, month = overtimeDate.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReconcileOvertime(
        int month,
        int year,
        int? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new ReconcileOvertimeWithAttendanceCommand(month, year, employeeId), cancellationToken);
            TempData["SuccessMessage"] = $"Đã đối chiếu OT với chấm công thực tế tháng {month:D2}/{year}: xử lý {result.ReconciledCount} đơn, cộng {result.TotalCreditedCompensatoryHours:0.#}h nghỉ bù.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year, month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrRevokeRequest(
        string entityType,
        int entityId,
        string reason,
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var res = await _sender.Send(new CancelOrRequestRevocationCommand(entityType, entityId, reason), cancellationToken);
            TempData["SuccessMessage"] = res.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year, month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PushTerminationLeavePayout(
        int employeeId,
        DateOnly terminationDate,
        int targetPayrollMonth,
        int targetPayrollYear,
        decimal standardWorkdays = 26m,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await _sender.Send(new CalculateAndPushTerminationLeavePayoutCommand(
                employeeId,
                terminationDate,
                targetPayrollMonth,
                targetPayrollYear,
                standardWorkdays,
                notes), cancellationToken);

            TempData["SuccessMessage"] = $"Đã quyết toán {dto.UnusedPayableDays:0.#} ngày phép tồn cho {dto.FullName}: {dto.PayoutAmount:N0} đ đẩy sang kỳ lương {targetPayrollMonth:D2}/{targetPayrollYear}!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year = terminationDate.Year, month = terminationDate.Month });
    }
}

