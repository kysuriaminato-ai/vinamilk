using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.WebUI.Controllers;

public class KpiController : Controller
{
    private readonly IKpiService _kpiService;
    private readonly IEmployeeService _employeeService;

    public KpiController(
        IKpiService kpiService,
        IEmployeeService employeeService)
    {
        _kpiService = kpiService ?? throw new ArgumentNullException(nameof(kpiService));
        _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
    }

    // GET: /Kpi
    public async Task<IActionResult> Index(string? period, int? unitId)
    {
        string selectedPeriod = string.IsNullOrWhiteSpace(period) ? "2025-Q1" : period.Trim();
        ViewBag.SelectedPeriod = selectedPeriod;
        ViewBag.SelectedUnitId = unitId;

        var employees = await _employeeService.GetAllEmployeesAsync();
        ViewBag.Employees = employees;

        var kanbanBoard = await _kpiService.GetKanbanBoardAsync(selectedPeriod, unitId);
        return View(kanbanBoard);
    }

    // GET: /Kpi/GetKanbanData (Hỗ trợ gọi AJAX & Skeleton Loading)
    [HttpGet]
    public async Task<IActionResult> GetKanbanData(string? period, int? unitId)
    {
        string selectedPeriod = string.IsNullOrWhiteSpace(period) ? "2025-Q1" : period.Trim();
        var kanbanBoard = await _kpiService.GetKanbanBoardAsync(selectedPeriod, unitId);
        return Json(new { success = true, data = kanbanBoard });
    }

    // POST: /Kpi/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateKpiCommand command)
    {
        try
        {
            var result = await _kpiService.CreateKpiAsync(command);
            return Json(new { success = true, message = $"Đã giao chỉ tiêu KPI '{result.Title}' cho nhân sự thành công!", data = result });
        }
        catch (DomainException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    // POST: /Kpi/Start
    [HttpPost]
    public async Task<IActionResult> Start(int id)
    {
        try
        {
            await _kpiService.StartKpiAsync(id);
            return Json(new { success = true, message = "Đã chuyển trạng thái sang Đang triển khai thực hiện." });
        }
        catch (DomainException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // POST: /Kpi/SubmitResult
    [HttpPost]
    public async Task<IActionResult> SubmitResult([FromBody] SubmitKpiCommand command)
    {
        try
        {
            var result = await _kpiService.SubmitKpiResultAsync(command);
            return Json(new { 
                success = true, 
                message = $"Đã nộp báo cáo hoàn thành! Động cơ SLA tự động: {result.SlaCalculationDetail}. Điểm tạm tính: {result.SelfEvaluationScore}đ. Chờ Lãnh đạo thẩm định.", 
                data = result 
            });
        }
        catch (DomainException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    // POST: /Kpi/LeaderApprove
    [HttpPost]
    public async Task<IActionResult> LeaderApprove([FromBody] LeaderApproveKpiCommand command)
    {
        try
        {
            var result = await _kpiService.LeaderApproveKpiAsync(command);
            return Json(new { 
                success = true, 
                message = $"Lãnh đạo đã thẩm định và phê duyệt chính thức! Điểm chốt: {result.FinalScore}đ. Đã chuyển dữ liệu sang Phân hệ Tính lương.", 
                data = result 
            });
        }
        catch (DomainException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    // POST: /Kpi/Reject
    [HttpPost]
    public async Task<IActionResult> Reject(int id, int leaderId, string reason)
    {
        try
        {
            await _kpiService.RejectKpiAsync(id, leaderId, reason);
            return Json(new { success = true, message = "Đã trả về yêu cầu chỉnh sửa/bổ sung minh chứng." });
        }
        catch (DomainException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
