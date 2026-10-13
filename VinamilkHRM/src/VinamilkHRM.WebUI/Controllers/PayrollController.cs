using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.DTOs.Salaries;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;

namespace VinamilkHRM.WebUI.Controllers;

/// <summary>
/// Controller Quản trị Tiền lương Đa hình & Quy trình Tăng lương (Polymorphic Payroll & Salary Adjustments).
/// </summary>
public class PayrollController : Controller
{
    private readonly IPayrollService _payrollService;
    private readonly ISalaryAdjustmentService _salaryAdjustmentService;
    private readonly IEmployeeRepository _employeeRepository;

    public PayrollController(
        IPayrollService payrollService,
        ISalaryAdjustmentService salaryAdjustmentService,
        IEmployeeRepository employeeRepository)
    {
        _payrollService = payrollService ?? throw new ArgumentNullException(nameof(payrollService));
        _salaryAdjustmentService = salaryAdjustmentService ?? throw new ArgumentNullException(nameof(salaryAdjustmentService));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
    }

    /// <summary>
    /// Bảng điều khiển Bảng lương Toàn tập đoàn Vinamilk theo kỳ (mặc định tháng hiện tại).
    /// </summary>
    public async Task<IActionResult> Index(string? period)
    {
        string currentPeriod = string.IsNullOrWhiteSpace(period) ? DateTime.UtcNow.ToString("yyyy-MM") : period.Trim();
        var payslips = await _payrollService.GetPaySlipsByPeriodAsync(currentPeriod);

        ViewBag.Period = currentPeriod;
        return View(payslips);
    }

    /// <summary>
    /// Kích hoạt Động cơ tính lương tự động tuân thủ Luật Lao động cho toàn thể CBNV.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CalculatePayroll(string period)
    {
        if (string.IsNullOrWhiteSpace(period))
        {
            period = DateTime.UtcNow.ToString("yyyy-MM");
        }

        var employees = await _employeeRepository.GetAllAsync();
        int calculatedCount = 0;

        foreach (var emp in employees.Where(e => e.IsActive))
        {
            await _payrollService.GenerateMonthlyPayslipAsync(emp.Id, period);
            calculatedCount++;
        }

        TempData["SuccessMessage"] = $"Đã tính toán tự động thành công {calculatedCount} phiếu lương kỳ {period} theo Luật Lao động và Thuế TNCN!";
        return RedirectToAction(nameof(Index), new { period });
    }

    /// <summary>
    /// Xem Phiếu lương chi tiết (Sử dụng ĐA HÌNH Polymorphism để hiển thị mẫu đặc thù).
    /// </summary>
    public async Task<IActionResult> Payslip(int id)
    {
        string html = await _payrollService.GetPayslipHtmlPreviewAsync(id);
        ViewBag.PayslipHtml = html;
        ViewBag.PayslipId = id;
        return View();
    }

    /// <summary>
    /// Mẫu in ấn phiếu lương độc lập chuẩn A4 (@media print).
    /// </summary>
    public async Task<IActionResult> PrintPayslip(int id)
    {
        string html = await _payrollService.GetPayslipHtmlPreviewAsync(id);
        ViewBag.PayslipHtml = html;
        return View();
    }

    /// <summary>
    /// Quản lý Quy trình Tăng lương Thường lệ & Bảng điều khiển Giám đốc ĐẶC CÁCH (CEO Fast-track Override).
    /// </summary>
    public async Task<IActionResult> SalaryAdjustment()
    {
        var proposals = await _salaryAdjustmentService.GetAllProposalsAsync();
        var employees = await _employeeRepository.GetAllAsync();
        var appendices = await _salaryAdjustmentService.GetAllAppendicesAsync();

        ViewBag.Employees = employees;
        ViewBag.Appendices = appendices;

        return View(proposals);
    }

    /// <summary>
    /// Gửi đề xuất tăng lương cấp phòng ban.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SubmitProposal(CreateSalaryProposalRequest request)
    {
        try
        {
            await _salaryAdjustmentService.SubmitDepartmentProposalAsync(request);
            TempData["SuccessMessage"] = "Đã gửi tờ trình đề xuất tăng lương thành công lên Trưởng bộ phận!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(SalaryAdjustment));
    }

    /// <summary>
    /// Trưởng phòng thẩm định đề xuất.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ApproveDepartment(ApproveProposalRequest request)
    {
        try
        {
            await _salaryAdjustmentService.ApproveByDepartmentHeadAsync(request.ProposalId, request.ReviewerId, request.Notes);
            TempData["SuccessMessage"] = "Trưởng bộ phận đã phê duyệt đề xuất và chuyển tiếp lên Giám đốc Nhân sự & C&B!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(SalaryAdjustment));
    }

    /// <summary>
    /// Giám đốc Nhân sự phê duyệt đề xuất thường lệ.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ApproveHR(ApproveProposalRequest request)
    {
        try
        {
            await _salaryAdjustmentService.ApproveByHRDirectorAsync(request.ProposalId, request.ReviewerId, request.Notes);
            TempData["SuccessMessage"] = "Giám đốc Nhân sự đã phê duyệt đề xuất, tự động cập nhật mức lương mới và sinh Phụ lục Hợp đồng điện tử!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(SalaryAdjustment));
    }

    /// <summary>
    /// Từ chối đề xuất tăng lương.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RejectProposal(RejectProposalRequest request)
    {
        try
        {
            await _salaryAdjustmentService.RejectProposalAsync(request.ProposalId, request.ReviewerId, request.RejectionReason);
            TempData["SuccessMessage"] = "Đã từ chối tờ trình đề xuất tăng lương thành công!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(SalaryAdjustment));
    }

    /// <summary>
    /// Tổng Giám đốc (CEO) kích hoạt FAST-TRACK EXECUTIVE OVERRIDE trên đề xuất có sẵn.
    /// Bỏ qua toàn bộ thẩm định C&B, sinh phụ lục hợp đồng điện tử và ghi AuditLog bảo mật cao.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> FastTrackOverride(FastTrackCeoOverrideRequest request)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "10.0.0.1";
            var requestWithIp = request with { IpAddress = ip };
            var appendix = await _salaryAdjustmentService.FastTrackExecutiveOverrideAsync(requestWithIp);

            TempData["SuccessMessage"] = $"[CEO FAST-TRACK OVERRIDE THÀNH CÔNG] Đã kích hoạt đặc cách tăng lương cho nhân viên! Phụ lục HĐ điện tử số {appendix.AppendixNumber} đã sinh kèm chữ ký số cryptographic token và lưu vết AuditLog tối mật!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi kích hoạt đặc cách CEO: {ex.Message}";
        }

        return RedirectToAction(nameof(SalaryAdjustment));
    }

    /// <summary>
    /// Tổng Giám đốc (CEO) ra quyết định ĐẶC CÁCH TRỰC TIẾP không cần tạo proposal trước.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DirectCeoOverride(DirectCeoOverrideRequest request)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "10.0.0.1";
            var requestWithIp = request with { IpAddress = ip };
            var appendix = await _salaryAdjustmentService.CreateDirectCeoOverrideAsync(requestWithIp);

            TempData["SuccessMessage"] = $"[CEO ĐẶC CÁCH TRỰC TIẾP THÀNH CÔNG] Quyết định nâng bậc lương có hiệu lực ngay lập tức! Mã phụ lục: {appendix.AppendixNumber}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi thực thi quyết định CEO: {ex.Message}";
        }

        return RedirectToAction(nameof(SalaryAdjustment));
    }

    /// <summary>
    /// Danh sách Phụ lục Hợp đồng Lao động Điện tử.
    /// </summary>
    public async Task<IActionResult> Appendices()
    {
        var appendices = await _salaryAdjustmentService.GetAllAppendicesAsync();
        return View(appendices);
    }
}
