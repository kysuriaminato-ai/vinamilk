using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Talent;

namespace Vinamilk.HRM.Web.Controllers;

[Authorize]
public class RecruitmentController : Controller
{
    private readonly ISender _sender;

    public RecruitmentController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? jobId = null, string? tab = null, CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetAtsOverviewQuery(jobId), cancellationToken);
        ViewBag.ActiveTab = tab ?? "kanban";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(
        int organizationUnitId,
        int positionId,
        int quantity,
        string reasonType,
        decimal budgetMinSalary,
        decimal budgetMaxSalary,
        DateOnly? expectedOnboardDate,
        string? jobRequirements,
        CancellationToken cancellationToken = default)
    {
        await _sender.Send(new CreateRecruitmentRequestCommand(
            organizationUnitId, positionId, quantity, reasonType,
            budgetMinSalary, budgetMaxSalary, expectedOnboardDate, jobRequirements), cancellationToken);
        TempData["SuccessMessage"] = "Đã tạo Yêu cầu Tuyển dụng mới và chuyển chờ phê duyệt.";
        return RedirectToAction(nameof(Index), new { tab = "requests" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int requestId, bool isApproved, CancellationToken cancellationToken = default)
    {
        var msg = await _sender.Send(new ApproveRecruitmentRequestCommand(requestId, isApproved), cancellationToken);
        TempData["SuccessMessage"] = msg;
        return RedirectToAction(nameof(Index), new { tab = "requests" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateJobPosting(
        int? recruitmentRequestId,
        string title,
        int organizationUnitId,
        int positionId,
        int vacancies,
        string salaryRangeText,
        string workLocation,
        DateOnly closingDate,
        string jobDescription,
        string jobRequirements,
        string benefits,
        CancellationToken cancellationToken = default)
    {
        await _sender.Send(new CreateJobPostingCommand(
            recruitmentRequestId, title, organizationUnitId, positionId, vacancies,
            salaryRangeText, workLocation, closingDate, jobDescription, jobRequirements, benefits), cancellationToken);
        TempData["SuccessMessage"] = $"Đã đăng tin tuyển dụng '{title}' lên trang Careers công khai!";
        return RedirectToAction(nameof(Index), new { tab = "postings" });
    }

    public record MoveStageAjaxRequest(int CandidateId, string NewStage);

    [HttpPost]
    public async Task<IActionResult> MoveStageAjax([FromBody] MoveStageAjaxRequest req, CancellationToken cancellationToken = default)
    {
        var ok = await _sender.Send(new MoveCandidateStageCommand(req.CandidateId, req.NewStage), cancellationToken);
        return Ok(new { success = ok, message = ok ? $"Đã chuyển ứng viên sang bước {req.NewStage}" : "Không tìm thấy ứng viên" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveInterview(
        int candidateId,
        int roundNumber,
        string roundTitle,
        DateTime scheduledStartTime,
        string interviewMode,
        string locationOrLink,
        int? interviewerEmployeeId,
        decimal technicalScore,
        decimal cultureFitScore,
        string? evaluationNotes,
        string result,
        CancellationToken cancellationToken = default)
    {
        await _sender.Send(new ScheduleAndEvaluateInterviewCommand(
            candidateId, roundNumber, roundTitle, scheduledStartTime, interviewMode,
            locationOrLink, interviewerEmployeeId, technicalScore, cultureFitScore, evaluationNotes, result), cancellationToken);
        TempData["SuccessMessage"] = "Đã lưu lịch phỏng vấn & phiếu đánh giá ứng viên.";
        return RedirectToAction(nameof(Index), new { tab = "kanban" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOffer(
        int candidateId,
        decimal offeredProbationSalary,
        decimal offeredOfficialSalary,
        DateOnly expectedStartDate,
        DateOnly offerExpiryDate,
        CancellationToken cancellationToken = default)
    {
        await _sender.Send(new CreateJobOfferCommand(
            candidateId, offeredProbationSalary, offeredOfficialSalary, expectedStartDate, offerExpiryDate), cancellationToken);
        TempData["SuccessMessage"] = "Đã phát hành Thư mời nhận việc (Offer Letter) cho ứng viên!";
        return RedirectToAction(nameof(Index), new { tab = "offers" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToEmployee(int candidateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = await _sender.Send(new ConvertCandidateToEmployeeCommand(candidateId), cancellationToken);
            TempData["SuccessMessage"] = msg;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "kanban" });
    }

    [HttpGet]
    public async Task<IActionResult> PrintOffer(int id, CancellationToken cancellationToken = default)
    {
        var model = await _sender.Send(new GetAtsOverviewQuery(), cancellationToken);
        var offer = model.Offers.FirstOrDefault(o => o.Id == id);
        if (offer == null) return NotFound();
        return View(offer);
    }
}

