using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Talent;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize(Policy = "Training.View")]
public class TrainingController : Controller
{
    private readonly ISender _sender;
    private readonly TalentDocumentService _docService;

    public TrainingController(ISender sender, TalentDocumentService docService)
    {
        _sender = sender;
        _docService = docService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year = null, string? tab = null, CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetTrainingOverviewQuery(year), cancellationToken);
        ViewBag.ActiveTab = tab ?? "classes";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(
        string courseCode,
        string courseName,
        string courseCategory,
        int durationHours,
        decimal standardCostPerPerson,
        int commitmentMonths,
        string? description,
        CancellationToken cancellationToken = default)
    {
        await _sender.Send(new CreateTrainingCourseCommand(
            courseCode, courseName, courseCategory, durationHours, standardCostPerPerson, commitmentMonths, description), cancellationToken);
        TempData["SuccessMessage"] = $"Đã thêm khóa học '{courseCode} - {courseName}' vào danh mục Vinamilk Academy.";
        return RedirectToAction(nameof(Index), new { tab = "courses" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClass(
        int trainingCourseId,
        string className,
        DateOnly startDate,
        DateOnly endDate,
        string instructorName,
        string location,
        int maxCapacity,
        decimal totalActualCost,
        CancellationToken cancellationToken = default)
    {
        await _sender.Send(new CreateTrainingClassCommand(
            trainingCourseId, className, startDate, endDate, instructorName, location, maxCapacity, totalActualCost), cancellationToken);
        TempData["SuccessMessage"] = $"Đã mở lớp đào tạo '{className}' trong kế hoạch năm {startDate.Year}.";
        return RedirectToAction(nameof(Index), new { tab = "classes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnrollEmployee(int trainingClassId, int employeeId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _sender.Send(new EnrollEmployeeToClassCommand(trainingClassId, employeeId), cancellationToken);
            TempData["SuccessMessage"] = "Đã ghi danh nhân viên vào lớp đào tạo thành công.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "enrollments" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EvaluateEnrollment(
        int enrollmentId,
        decimal attendanceRatePercent,
        decimal finalScore,
        int? feedbackRating,
        string? feedbackComment,
        CancellationToken cancellationToken = default)
    {
        var msg = await _sender.Send(new EvaluateTrainingEnrollmentCommand(
            enrollmentId, attendanceRatePercent, finalScore, feedbackRating, feedbackComment), cancellationToken);
        TempData["SuccessMessage"] = msg;
        return RedirectToAction(nameof(Index), new { tab = "enrollments" });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(int? year = null, CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetTrainingOverviewQuery(year), cancellationToken);
        var bytes = _docService.ExportTrainingPlanExcel(model);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Vinamilk_KeHoach_DaoTao_{model.Year}.xlsx");
    }
}

