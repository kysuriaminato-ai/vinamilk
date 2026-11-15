using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly ISender _sender;
    private readonly TimeAttendanceDocumentService _docService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAttendanceDeviceSyncJob _deviceSyncJob;

    public AttendanceController(
        ISender sender,
        TimeAttendanceDocumentService docService,
        ICurrentUserService currentUser,
        IAttendanceDeviceSyncJob deviceSyncJob)
    {
        _sender = sender;
        _docService = docService;
        _currentUser = currentUser;
        _deviceSyncJob = deviceSyncJob;
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
        string? clientDeviceId = null,
        string? photoUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ipAddress = _currentUser.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var msg = await _sender.Send(new WebCheckInCommand(
                employeeId,
                punchType,
                sourceType,
                latitude,
                longitude,
                qrToken,
                ClientDeviceId: clientDeviceId,
                IpAddress: ipAddress,
                PhotoUrl: photoUrl), cancellationToken);

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
        if (msg.Contains("CẢNH BÁO", StringComparison.OrdinalIgnoreCase))
        {
            TempData["WarningMessage"] = $"Năm {year} chưa có lịch ngày nghỉ lễ! Vui lòng vào Ca làm việc & Ngày lễ để sinh lịch lễ năm {year} trước khi tính lương.";
        }
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
    public async Task<IActionResult> PreviewImportExcel(
        IFormFile? excelFile,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file Excel (.xlsx) dữ liệu chấm công cần kiểm tra.";
            return RedirectToAction(nameof(Index), new { month, year, tab = "devices" });
        }

        try
        {
            using var stream = excelFile.OpenReadStream();
            var preview = await _docService.PreviewAttendanceImportFromExcelAsync(stream, cancellationToken);
            TempData["ImportPreviewJson"] = System.Text.Json.JsonSerializer.Serialize(preview);
            TempData["SuccessMessage"] = $"Kiểm tra Preview (chưa ghi DB): Tổng {preview.TotalRows} dòng | Hợp lệ: {preview.ValidCount} | Lỗi: {preview.ErrorCount}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi kiểm tra file Excel: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { month, year, tab = "devices" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmImportExcel(
        string? previewRowsJson,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(previewRowsJson))
        {
            TempData["ErrorMessage"] = "Không có dữ liệu Preview hợp lệ để xác nhận import.";
            return RedirectToAction(nameof(Index), new { month, year, tab = "devices" });
        }

        try
        {
            var rows = System.Text.Json.JsonSerializer.Deserialize<List<AttendanceImportPreviewRow>>(previewRowsJson)
                ?? new List<AttendanceImportPreviewRow>();
            var ipAddress = _currentUser.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _docService.ConfirmAttendanceImportAsync(
                rows,
                _currentUser.RequireUserId(),
                _currentUser.RequireUsername(),
                ipAddress,
                cancellationToken);
            TempData["SuccessMessage"] = $"Xác nhận Import hoàn tất: Tổng {result.TotalRows} dòng | Thành công: {result.SuccessCount} | Lỗi/Trùng: {result.ErrorCount}.";
            TempData["ImportResultJson"] = System.Text.Json.JsonSerializer.Serialize(result.Details);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi xác nhận import Excel: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { month, year, tab = "devices" });
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
            var result = await _docService.ImportAttendanceFromExcelAsync(
                stream,
                _currentUser.RequireUserId(),
                _currentUser.RequireUsername(),
                cancellationToken);
            TempData["SuccessMessage"] = $"Import hoàn tất: Tổng {result.TotalRows} dòng | Thành công: {result.SuccessCount} | Lỗi: {result.ErrorCount}.";
            TempData["ImportResultJson"] = System.Text.Json.JsonSerializer.Serialize(result.Details);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi đọc file Excel: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { month, year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IngestBiometricFile(
        string deviceCode,
        IFormFile? logFile,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (logFile == null || logFile.Length == 0 || string.IsNullOrWhiteSpace(deviceCode))
        {
            TempData["ErrorMessage"] = "Vui lòng chọn mã máy chấm công và file log (.csv/.txt/.dat).";
            return RedirectToAction(nameof(Index), new { month, year, tab = "devices" });
        }

        try
        {
            using var ms = new MemoryStream();
            await logFile.CopyToAsync(ms, cancellationToken);
            var summary = await _sender.Send(new IngestBiometricFileCommand(deviceCode, ms.ToArray()), cancellationToken);
            TempData["SuccessMessage"] = summary.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi đọc file máy chấm công: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { month, year, tab = "devices" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MapDeviceEmployee(
        int employeeId,
        string deviceUserCode,
        string? deviceCode,
        string? notes,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var replayed = await _sender.Send(new MapEmployeeDeviceAndReplayCommand(
                employeeId,
                deviceUserCode,
                deviceCode,
                notes), cancellationToken);
            TempData["SuccessMessage"] = $"Đã ánh xạ mã máy '{deviceUserCode}' và đẩy lại {replayed} log chờ vào bảng công.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi ánh xạ mã chấm công: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { month, year, tab = "devices" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunDeviceSyncNow(
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summaries = await _deviceSyncJob.SyncAllActiveDevicesAsync(cancellationToken);
            TempData["SuccessMessage"] = $"Đã đồng bộ {summaries.Count} máy chấm công: {string.Join(" | ", summaries.Select(s => s.DeviceCode + "=" + s.ImportedCount))}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi đồng bộ định kỳ: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { month, year, tab = "devices" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertManualAttendance(
        int employeeId,
        DateOnly workDate,
        TimeOnly checkInTime,
        TimeOnly? checkOutTime,
        string attendanceSymbol,
        string reason,
        string? photoUrl,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.Send(new UpsertManualAttendanceLogCommand(
                employeeId,
                workDate,
                checkInTime,
                checkOutTime,
                attendanceSymbol,
                reason,
                photoUrl), cancellationToken);
            TempData["SuccessMessage"] = $"Đã ghi nhận/cập nhật công nhập tay HR ngày {workDate:dd/MM/yyyy} (có lưu AuditLog).";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { month, year, tab = "matrix" });
    }

    [HttpGet]
    public async Task<IActionResult> CellDetails(
        int employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var detail = await _sender.Send(new GetAttendanceCellDetailsQuery(employeeId, date), cancellationToken);
        return detail == null ? NotFound() : Json(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartRecalculationBatch(
        int month,
        int year,
        int? unitId = null,
        int? employeeId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var batchId = await _sender.Send(new StartAttendanceRecalculationCommand(
                month, year, unitId, employeeId, fromDate, toDate), cancellationToken);
            TempData["ActiveBatchId"] = batchId;
            TempData["SuccessMessage"] = $"Đã chạy lô tính công #{batchId} qua AttendanceCalculationEngine cho tháng {month:D2}/{year}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { month, year, unitId, tab = "matrix" });
    }

    [HttpGet]
    public async Task<IActionResult> RecalculationProgress(
        int batchId,
        CancellationToken cancellationToken = default)
    {
        var status = await _sender.Send(new GetRecalculationBatchStatusQuery(batchId), cancellationToken);
        return status == null ? NotFound() : Json(status);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvanceClosingStep(
        int month,
        int year,
        string actionStep,
        int? unitId = null,
        bool forceLock = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = await _sender.Send(new AdvanceAttendanceClosingCommand(
                month, year, actionStep, unitId, forceLock), cancellationToken);
            TempData["SuccessMessage"] = msg;
            if (msg.Contains("CẢNH BÁO", StringComparison.OrdinalIgnoreCase))
            {
                TempData["WarningMessage"] = $"Năm {year} chưa có lịch ngày nghỉ lễ! Vui lòng mở Ca làm việc & Ngày lễ để sinh lịch lễ năm {year} trước khi khóa sổ lương.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { month, year, unitId, tab = "matrix" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePostLockAdjustment(
        int employeeId,
        int targetMonth,
        int targetYear,
        DateOnly adjustmentDate,
        int appliedPayrollMonth,
        int appliedPayrollYear,
        string adjustmentType,
        decimal workdayDelta,
        decimal otHours150Delta,
        decimal otHours200Delta,
        decimal otHours300Delta,
        decimal nightHoursDelta,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _sender.Send(new CreatePostLockAdjustmentCommand(
                employeeId,
                targetMonth,
                targetYear,
                adjustmentDate,
                appliedPayrollMonth,
                appliedPayrollYear,
                adjustmentType,
                workdayDelta,
                otHours150Delta,
                otHours200Delta,
                otHours300Delta,
                nightHoursDelta,
                reason), cancellationToken);
            TempData["SuccessMessage"] = $"Đã tạo phiếu điều chỉnh công kỳ sau #{id} (chỉ áp dụng kỳ {appliedPayrollMonth:D2}/{appliedPayrollYear}, giữ nguyên kỳ đã khóa).";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { month = targetMonth, year = targetYear, tab = "matrix" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DecidePostLockAdjustment(
        long adjustmentId,
        bool approve,
        int month,
        int year,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = await _sender.Send(new DecidePostLockAdjustmentCommand(adjustmentId, approve, comment), cancellationToken);
            TempData["SuccessMessage"] = msg;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { month, year, tab = "matrix" });
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

