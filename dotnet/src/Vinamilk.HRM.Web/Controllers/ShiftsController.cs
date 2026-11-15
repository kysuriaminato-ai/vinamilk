using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.TimeAttendance;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize]
public class ShiftsController : Controller
{
    private readonly ISender _sender;

    public ShiftsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        DateOnly? weekStart = null,
        int? unitId = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetShiftAndScheduleOverviewQuery(weekStart, unitId, year), cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWorkShift(
        int? id,
        string shiftCode,
        string shiftName,
        TimeOnly startTime,
        TimeOnly endTime,
        TimeOnly? breakStartTime,
        TimeOnly? breakEndTime,
        bool isOvernight,
        decimal standardHours = 8.0m,
        decimal workdayValue = 1.0m,
        decimal nightShiftAllowanceRate = 0.0m,
        int graceLateMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.Send(new SaveWorkShiftCommand(
                id,
                shiftCode,
                shiftName,
                startTime,
                endTime,
                breakStartTime,
                breakEndTime,
                isOvernight,
                standardHours,
                workdayValue,
                nightShiftAllowanceRate,
                graceLateMinutes), cancellationToken);

            TempData["SuccessMessage"] = $"Đã lưu thông tin ca làm việc '{shiftCode} - {shiftName}' thành công.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAssignSchedule(
        int? employeeId,
        int? organizationUnitId,
        DateOnly fromDate,
        DateOnly toDate,
        int? workShiftId,
        bool isDayOff = false,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _sender.Send(new AssignShiftScheduleCommand(
                employeeId,
                organizationUnitId,
                fromDate,
                toDate,
                workShiftId,
                isDayOff,
                notes), cancellationToken);

            TempData["SuccessMessage"] = $"Đã xếp ca thành công cho {count} lượt ngày công trên lịch phân ca.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { weekStart = fromDate, unitId = organizationUnitId });
    }

    public record DragDropAssignRequest(
        int EmployeeId,
        string WorkDate,
        int? WorkShiftId,
        bool IsDayOff);

    [HttpPost]
    public async Task<IActionResult> AssignCellAjax(
        [FromBody] DragDropAssignRequest req,
        CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParse(req.WorkDate, out var workDate))
            return BadRequest(new { success = false, message = "Ngày phân ca không hợp lệ." });

        await _sender.Send(new AssignShiftScheduleCommand(
            req.EmployeeId,
            null,
            workDate,
            workDate,
            req.WorkShiftId,
            req.IsDayOff,
            "Kéo-thả trực tiếp trên Lịch phân ca"), cancellationToken);

        return Ok(new { success = true, message = $"Đã cập nhật ca làm việc ngày {workDate:dd/MM/yyyy}!" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHoliday(
        int? id,
        DateOnly holidayDate,
        string holidayName,
        bool isPaidHoliday = true,
        decimal overtimeMultiplier = 3.0m,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.Send(new SaveHolidayCommand(id, holidayDate, holidayName, isPaidHoliday, overtimeMultiplier), cancellationToken);
            TempData["SuccessMessage"] = $"Đã lưu ngày lễ '{holidayName} ({holidayDate:dd/MM/yyyy})'.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year = holidayDate.Year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHoliday(int id, CancellationToken cancellationToken = default)
    {
        await _sender.Send(new DeleteHolidayCommand(id), cancellationToken);
        TempData["SuccessMessage"] = "Đã xóa ngày nghỉ lễ khỏi danh mục.";
        return RedirectToAction(nameof(Index));
    }
}

