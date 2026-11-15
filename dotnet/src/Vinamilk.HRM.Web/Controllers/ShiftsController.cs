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
        string? tab = null,
        CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetShiftAndScheduleOverviewQuery(weekStart, unitId, year), cancellationToken);
        ViewBag.ActiveTab = tab ?? "roster";
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
        bool isOvernight = false,
        decimal standardHours = 8.0m,
        decimal workdayValue = 1.0m,
        decimal nightShiftAllowanceRate = 0.0m,
        int graceLateMinutes = 5,
        string shiftType = "Fixed",
        TimeOnly? coreStartTime = null,
        TimeOnly? coreEndTime = null,
        TimeOnly? splitStartTime2 = null,
        TimeOnly? splitEndTime2 = null,
        bool isBreakPaid = false,
        int maxEarlyCheckInMinutes = 120,
        int maxLateCheckOutMinutes = 180,
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
                graceLateMinutes,
                shiftType,
                coreStartTime,
                coreEndTime,
                splitStartTime2,
                splitEndTime2,
                isBreakPaid,
                maxEarlyCheckInMinutes,
                maxLateCheckOutMinutes), cancellationToken);

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
        bool replaceExisting = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new AssignShiftScheduleDetailedCommand(
                employeeId,
                organizationUnitId,
                fromDate,
                toDate,
                workShiftId,
                isDayOff,
                notes,
                replaceExisting), cancellationToken);

            TempData["SuccessMessage"] = $"Đã xếp ca thành công cho {result.AffectedCount} lượt ngày công trên lịch phân ca.";
            if (result.Warnings.Count > 0)
            {
                TempData["WarningMessage"] = $"Cảnh báo xung đột: {string.Join("; ", result.Warnings.Take(3).Select(w => w.Message))}";
            }
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

        var result = await _sender.Send(new AssignShiftScheduleDetailedCommand(
            req.EmployeeId,
            null,
            workDate,
            workDate,
            req.WorkShiftId,
            req.IsDayOff,
            "Kéo-thả trực tiếp trên Lịch phân ca"), cancellationToken);

        return Ok(new
        {
            success = true,
            message = $"Đã cập nhật ca làm việc ngày {workDate:dd/MM/yyyy}!",
            warnings = result.Warnings.Select(w => w.Message).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveHoliday(
        int? id,
        DateOnly holidayDate,
        string holidayName,
        bool isPaidHoliday = true,
        decimal overtimeMultiplier = 3.0m,
        bool autoGenerateCompensatoryIfWeekend = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.Send(new SaveHolidayCommand(
                id, holidayDate, holidayName, isPaidHoliday, overtimeMultiplier,
                AutoGenerateCompensatoryIfWeekend: autoGenerateCompensatoryIfWeekend), cancellationToken);
            TempData["SuccessMessage"] = $"Đã lưu ngày lễ '{holidayName} ({holidayDate:dd/MM/yyyy})'.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year = holidayDate.Year, tab = "holidays" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHoliday(int id, int year = 2026, CancellationToken cancellationToken = default)
    {
        await _sender.Send(new DeleteHolidayCommand(id), cancellationToken);
        TempData["SuccessMessage"] = "Đã xóa ngày nghỉ lễ khỏi danh mục.";
        return RedirectToAction(nameof(Index), new { year, tab = "holidays" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectNationalDayAdjacentHoliday(int year, string choice, CancellationToken cancellationToken = default)
    {
        var chosenDate = await _sender.Send(new SelectNationalDayAdjacentHolidayCommand(year, choice), cancellationToken);
        TempData["SuccessMessage"] = $"Đã xác nhận ngày nghỉ liền kề Quốc khánh {year}: {chosenDate:dd/MM/yyyy} (đã bỏ cờ chờ HR đối chiếu).";
        return RedirectToAction(nameof(Index), new { year, tab = "holidays" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateHolidaysForYear(
        int year,
        DateOnly? tetStartDate = null,
        DateOnly? hungKingsDate = null,
        string nationalDayAdjacentChoice = "Before",
        bool includeVietnamCultureDay = true,
        bool autoGenerateCompensatoryIfWeekend = true,
        bool overwriteExistingAutoGenerated = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new GenerateHolidaysForYearCommand(
                year,
                tetStartDate,
                hungKingsDate,
                nationalDayAdjacentChoice,
                includeVietnamCultureDay,
                autoGenerateCompensatoryIfWeekend,
                overwriteExistingAutoGenerated), cancellationToken);

            TempData["SuccessMessage"] = result.SummaryMessage;
            if (result.PendingManualItems.Count > 0)
            {
                TempData["WarningMessage"] = $"Cần HR bổ sung ngày Âm lịch cho năm {year}: {string.Join("; ", result.PendingManualItems)}";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { year, tab = "holidays" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRotation(
        int patternId,
        DateOnly startDate,
        int weeksCount,
        int? organizationUnitId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new GenerateRotationScheduleCommand(
                patternId, startDate, weeksCount, organizationUnitId), cancellationToken);
            TempData["SuccessMessage"] = $"Đã sinh lịch xoay ca tự động ({result.AffectedCount} lượt phân công trong {weeksCount} tuần).";
            if (result.Warnings.Count > 0)
            {
                TempData["WarningMessage"] = $"Cảnh báo xung đột: {string.Join("; ", result.Warnings.Take(3).Select(w => w.Message))}";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { weekStart = startDate, unitId = organizationUnitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitSwap(
        int requesterEmployeeId,
        int targetEmployeeId,
        DateOnly requesterWorkDate,
        DateOnly targetWorkDate,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.Send(new SubmitShiftSwapRequestCommand(
                requesterEmployeeId, targetEmployeeId, requesterWorkDate, targetWorkDate, reason), cancellationToken);
            TempData["SuccessMessage"] = "Đã tạo yêu cầu đổi ca thành công.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessSwap(
        int requestId,
        bool approve,
        string? approverComment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new ProcessShiftSwapRequestCommand(requestId, approve, approverComment), cancellationToken);
            TempData["SuccessMessage"] = approve ? "Đã duyệt đơn đổi ca và cập nhật lịch làm việc." : "Đã từ chối đơn đổi ca.";
            if (result.Warnings.Count > 0)
            {
                TempData["WarningMessage"] = $"Lưu ý xung đột sau đổi ca: {string.Join("; ", result.Warnings.Select(w => w.Message))}";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePolicyGroup(
        int? id,
        string policyCode,
        string policyName,
        bool isCompanyDefault,
        int graceLateMinutes,
        int graceEarlyMinutes,
        int roundingIntervalMinutes,
        string roundingMode,
        int halfDayLateThresholdMinutes,
        int zeroDayLateThresholdMinutes,
        int maxMissedPunchExplanationsPerMonth,
        string missedPunchOverQuotaAction,
        decimal missedPunchPenaltyAmount,
        bool requireApprovedOvertimeRequest,
        string weeklyWorkdaysMask,
        decimal? maxWeeklyWorkingHoursOverride,
        string? description,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.Send(new SavePolicyGroupCommand(
                id, policyCode, policyName, isCompanyDefault,
                graceLateMinutes, graceEarlyMinutes, roundingIntervalMinutes, roundingMode,
                halfDayLateThresholdMinutes, zeroDayLateThresholdMinutes,
                maxMissedPunchExplanationsPerMonth, missedPunchOverQuotaAction, missedPunchPenaltyAmount,
                requireApprovedOvertimeRequest, weeklyWorkdaysMask, maxWeeklyWorkingHoursOverride, description), cancellationToken);
            TempData["SuccessMessage"] = $"Đã lưu nhóm chính sách chấm công '{policyCode} - {policyName}'.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPolicyGroup(
        int policyGroupId,
        int? employeeId,
        int? positionId,
        int? organizationUnitId,
        DateOnly? effectiveFrom,
        DateOnly? effectiveTo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.Send(new AssignPolicyGroupCommand(
                policyGroupId, employeeId, positionId, organizationUnitId, effectiveFrom, effectiveTo), cancellationToken);
            TempData["SuccessMessage"] = "Đã gán nhóm chính sách chấm công thành công.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

