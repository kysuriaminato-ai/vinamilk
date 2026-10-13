using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.DTOs.Careers;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.WebUI.Controllers;

/// <summary>
/// Cổng Thông tin Tuyển dụng Công khai Vinamilk Careers Portal (Chuẩn SEO & Schema.org JobPosting)
/// và Quản lý Tuyển dụng & Onboarding Tự động sang Nhân viên Thử việc.
/// </summary>
public class CareersController : Controller
{
    private readonly ICareersService _careersService;
    private readonly IEmployeeService _employeeService;

    public CareersController(ICareersService careersService, IEmployeeService employeeService)
    {
        _careersService = careersService;
        _employeeService = employeeService;
    }

    [HttpGet]
    [Route("tuyen-dung")]
    public async Task<IActionResult> Index(
        string? keyword = null,
        UnitType? unitType = null,
        string? city = null,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _careersService.GetActiveJobPostingsAsync(keyword, unitType, city, cancellationToken);
        
        ViewBag.Keyword = keyword;
        ViewBag.SelectedUnitType = unitType;
        ViewBag.SelectedCity = city;

        return View(jobs);
    }

    [HttpGet]
    [Route("tuyen-dung/{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken = default)
    {
        var job = await _careersService.GetJobPostingBySlugAsync(slug, cancellationToken);
        if (job == null)
        {
            return NotFound("Không tìm thấy vị trí tuyển dụng yêu cầu.");
        }

        // Tải các việc làm liên quan cùng khối
        var relatedJobs = await _careersService.GetActiveJobPostingsAsync(unitType: job.UnitType, cancellationToken: cancellationToken);
        ViewBag.RelatedJobs = relatedJobs.Where(j => j.Id != job.Id).Take(3).ToList();

        return View(job);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("tuyen-dung/{slug}/ung-tuyen")]
    public async Task<IActionResult> Apply(string slug, SubmitApplicationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _careersService.SubmitApplicationAsync(request, cancellationToken);
            TempData["SuccessMessage"] = $"Nộp hồ sơ thành công! Mã xác nhận của bạn là APP-{result.Id:D5}. Phòng Nhân sự Vinamilk sẽ liên hệ bạn sớm.";
            return RedirectToAction(nameof(Detail), new { slug });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Detail), new { slug });
        }
    }

    [HttpGet]
    [Route("tuyen-dung/quan-ly-ung-vien")]
    public async Task<IActionResult> AdminApplications(
        int? jobPostingId = null,
        JobApplicationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var applications = await _careersService.GetApplicationsAsync(jobPostingId, status, cancellationToken);
        var jobs = await _careersService.GetActiveJobPostingsAsync(cancellationToken: cancellationToken);
        var units = await _employeeService.GetAllUnitsAsync(cancellationToken);
        var employees = await _employeeService.GetAllEmployeesAsync(cancellationToken);

        ViewData["JobPostings"] = jobs;
        ViewData["Units"] = units;
        ViewData["Employees"] = employees;
        ViewData["SelectedJobId"] = jobPostingId;
        ViewData["SelectedStatus"] = status;

        return View(applications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("tuyen-dung/onboard")]
    public async Task<IActionResult> Onboard(OnboardCandidateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _careersService.OnboardHiredCandidateAsync(request, cancellationToken);
            TempData["SuccessMessage"] = result.Message;
            TempData["OnboardInfo"] = $"Mã NV mới: {result.EmployeeCode} | Tài khoản ESS: {result.GeneratedEssUsername} (Mật khẩu: {result.TempEssPassword})";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi Onboard ứng viên: {ex.Message}";
        }

        return RedirectToAction(nameof(AdminApplications));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("tuyen-dung/cap-nhat-trang-thai")]
    public async Task<IActionResult> UpdateStatus(UpdateApplicationStatusRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _careersService.UpdateApplicationStatusAsync(request, cancellationToken);
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái hồ sơ ứng viên thành công!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi cập nhật: {ex.Message}";
        }

        return RedirectToAction(nameof(AdminApplications));
    }
}
