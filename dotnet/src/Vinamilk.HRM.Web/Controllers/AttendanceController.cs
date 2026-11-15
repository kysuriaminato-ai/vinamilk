using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly ISender _sender;
    private readonly TimeAttendanceDocumentService _docService;

    public AttendanceController(
        ISender sender,
        TimeAttendanceDocumentService docService)
    {
        _sender = sender;
        _docService = docService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? month = null,
        int? year = null,
        int? unitId = null,
        string? search = null,
        string? tab = null,
        CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetMonthlyAttendanceBoardQuery(month, year, unitId, search), cancellationToken);
        ViewBag.ActiveTab = tab ?? "matrix";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WebCheckIn(
        int employeeId,
        string punchType,
        string sourceType,
        double latitude,
        double longitude,
        string? qrToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = await _sender.Send(new WebCheckInCommand(
                employeeId,
                punchType,
                sourceType,
                latitude,
                longitude,
                qrToken), cancellationToken);

            TempData["SuccessMessage"] = msg;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "gpsqr" });
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
        try
        {
            var otId = await _sender.Send(new SubmitOvertimeRequestCommand(
                employeeId,
                overtimeDate,
                startTime,
                endTime,
                overtimeRateType,
                reason), cancellationToken);

            TempData["SuccessMessage"] = $"Đã tạo đơn đăng ký làm thêm giờ #{otId} và kích hoạt Workflow phê duyệt + gửi thông báo SignalR/Email!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "overtime" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitExplanation(
        int employeeId,
        DateOnly attendanceDate,
        TimeOnly proposedCheckIn,
        TimeOnly proposedCheckOut,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wfId = await _sender.Send(new SubmitAttendanceExplanationCommand(
                employeeId,
                attendanceDate,
                proposedCheckIn,
                proposedCheckOut,
                reason), cancellationToken);

            TempData["SuccessMessage"] = $"Đã gửi đơn Giải trình chấm công ngày {attendanceDate:dd/MM/yyyy} vào luồng phê duyệt #WF-{wfId:D4}!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "explanations" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecalculateMonth(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var count = await _sender.Send(new RecalculateMonthlyAttendanceCommand(month, year), cancellationToken);
        TempData["SuccessMessage"] = $"Đã tổng hợp lại công chuẩn, đi trễ, OT và phép cho {count} nhân sự trong tháng {month:D2}/{year}.";
        return RedirectToAction(nameof(Index), new { month, year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockMonth(
        int month,
        int year,
        bool lockState,
        CancellationToken cancellationToken = default)
    {
        var msg = await _sender.Send(new ToggleLockMonthlyAttendanceCommand(month, year, lockState), cancellationToken);
        TempData["SuccessMessage"] = msg;
        return RedirectToAction(nameof(Index), new { month, year });
    }

    [HttpGet]
    public IActionResult DownloadImportTemplate()
    {
        var bytes = _docService.GenerateAttendanceImportTemplateExcel();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Vinamilk_Mau_Import_MayChamCong.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(
        IFormFile? excelFile,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file Excel (.xlsx) dữ liệu chấm công cần import.";
            return RedirectToAction(nameof(Index), new { month, year });
        }

        try
        {
            using var stream = excelFile.OpenReadStream();
            var result = await _docService.ImportAttendanceFromExcelAsync(stream, cancellationToken);
            TempData["SuccessMessage"] = $"Import hoàn tất: Tổng {result.TotalRows} dòng | Thành công: {result.SuccessCount} | Lỗi: {result.ErrorCount}.";
            TempData["ImportResultJson"] = System.Text.Json.JsonSerializer.Serialize(result.Details);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi đọc file Excel: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { month, year });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        int? month = null,
        int? year = null,
        int? unitId = null,
        CancellationToken cancellationToken = default)
    {
        var board = await _sender.Send(new GetMonthlyAttendanceBoardQuery(month, year, unitId, null), cancellationToken);
        var bytes = _docService.ExportMonthlyTimesheetToExcel(board);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Vinamilk_BangChamCong_T{board.Month:D2}_{board.Year}.xlsx");
    }
}

