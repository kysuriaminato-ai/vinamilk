using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.DTOs.Analytics;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.WebUI.Controllers;

/// <summary>
/// Bảng Điều khiển Thông tin Quản trị Cấp Chiến lược (Strategic MIS HR Dashboard).
/// Tích hợp dự báo tỷ lệ biến động nhân sự, phân tích cơ cấu quỹ lương, mô phỏng chính sách tiền lương và AI HR Advisor.
/// </summary>
public class StrategicDashboardController : Controller
{
    private readonly IStrategicDashboardService _strategicService;

    public StrategicDashboardController(IStrategicDashboardService strategicService)
    {
        _strategicService = strategicService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var data = await _strategicService.GetDashboardDataAsync(cancellationToken);
        return View(data);
    }

    [HttpPost]
    public async Task<IActionResult> SimulatePolicy([FromBody] MinimumWageSimulationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _strategicService.SimulateWagePolicyImpactAsync(request, cancellationToken);
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> AiAdvice(CancellationToken cancellationToken = default)
    {
        var recommendations = await _strategicService.GetAiHrRecommendationsAsync(cancellationToken);
        return Json(new { success = true, data = recommendations });
    }
}
