using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.WebUI.Controllers;

public class TimekeepingController : Controller
{
    private readonly IShiftAssignmentService _shiftService;
    private readonly IEmployeeService _employeeService;

    public TimekeepingController(
        IShiftAssignmentService shiftService,
        IEmployeeService employeeService)
    {
        _shiftService = shiftService ?? throw new ArgumentNullException(nameof(shiftService));
        _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
    }

    // GET: /Timekeeping
    public async Task<IActionResult> Index(DateTime? targetDate, int? unitId)
    {
        DateTime selectedDate = targetDate?.Date ?? DateTime.Today;
        ViewBag.SelectedDate = selectedDate;
        ViewBag.SelectedUnitId = unitId;

        var shifts = await _shiftService.GetConfiguredShiftsAsync();
        var attendances = await _shiftService.GetAttendanceDashboardDataAsync(selectedDate, unitId);
        var warnings = await _shiftService.GetDisciplinaryWarningsAsync(onlyUnresolved: true);
        var employees = await _employeeService.GetAllEmployeesAsync();

        ViewBag.Shifts = shifts;
        ViewBag.Warnings = warnings;
        ViewBag.Employees = employees;

        return View(attendances);
    }

    // POST: /Timekeeping/ClockIn
    [HttpPost]
    public async Task<IActionResult> ClockIn([FromBody] ClockInRequest model)
    {
        try
        {
            var result = await _shiftService.ClockInAsync(
                model.EmployeeId,
                model.ShiftId,
                model.CheckInTime,
                model.AttendanceDate);

            return Json(new { success = true, message = $"Check-In thành công cho nhân sự {result.EmployeeName}! Trạng thái: {result.Status}", data = result });
        }
        catch (DomainException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
        }
    }

    // POST: /Timekeeping/ClockOut
    [HttpPost]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutRequest model)
    {
        try
        {
            var result = await _shiftService.ClockOutAsync(
                model.EmployeeId,
                model.CheckOutTime,
                model.AttendanceDate);

            return Json(new { 
                success = true, 
                message = $"Check-Out thành công! Giờ công tính được: {result.WorkHours}h, Giờ làm thêm OT: {result.OvertimeHours}h. Trạng thái: {result.Status}", 
                data = result 
            });
        }
        catch (DomainException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
        }
    }

    // POST: /Timekeeping/SubmitUnplannedLeave
    [HttpPost]
    public async Task<IActionResult> SubmitUnplannedLeave([FromBody] UnplannedLeaveDto model)
    {
        try
        {
            string message = await _shiftService.ProcessUnplannedLeaveAsync(model);
            return Json(new { success = true, message = message });
        }
        catch (DomainException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
        }
    }

    // GET: /Timekeeping/GetWarnings
    [HttpGet]
    public async Task<IActionResult> GetWarnings()
    {
        var warnings = await _shiftService.GetDisciplinaryWarningsAsync(onlyUnresolved: true);
        return Json(new { success = true, data = warnings });
    }

    // POST: /Timekeeping/ResolveWarning
    [HttpPost]
    public async Task<IActionResult> ResolveWarning(int warningId, string notes)
    {
        try
        {
            await _shiftService.ResolveDisciplinaryWarningAsync(warningId, notes);
            return Json(new { success = true, message = "Đã lưu biên bản xử lý cảnh báo kỷ luật lao động thành công." });
        }
        catch (DomainException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}

public class ClockInRequest
{
    public int EmployeeId { get; set; }
    public int ShiftId { get; set; }
    public TimeSpan CheckInTime { get; set; }
    public DateTime AttendanceDate { get; set; }
}

public class ClockOutRequest
{
    public int EmployeeId { get; set; }
    public TimeSpan CheckOutTime { get; set; }
    public DateTime AttendanceDate { get; set; }
}
