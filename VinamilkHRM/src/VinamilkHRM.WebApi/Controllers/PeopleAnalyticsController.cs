#pragma warning disable CS8602
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Analytics;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public class PeopleAnalyticsController : ControllerBase
{
    private readonly IPeopleAnalyticsService _analyticsService;
    private readonly ICurrentUser _currentUser;
    private readonly IDataScope _dataScope;

    public PeopleAnalyticsController(
        IPeopleAnalyticsService analyticsService,
        ICurrentUser currentUser,
        IDataScope dataScope)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _dataScope = dataScope ?? throw new ArgumentNullException(nameof(dataScope));
    }

    private int CurrentEmployeeId => _currentUser.EmployeeId ?? 1;

    /// <summary>
    /// Lấy danh sách bản chụp số liệu nhân sự WorkforceSnapshot (Schema rpt).
    /// </summary>
    [HttpGet("snapshots")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkforceSnapshotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnapshots([FromQuery] string? month, [FromQuery] int? unitId, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetSnapshotsAsync(month, unitId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Chụp ảnh số liệu nhân sự WorkforceSnapshot cho đơn vị tại cuối tháng.
    /// </summary>
    [HttpPost("snapshots/capture")]
    [ProducesResponseType(typeof(WorkforceSnapshotDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CaptureSnapshot([FromQuery] string month, [FromQuery] int unitId, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.CaptureSnapshotAsync(month, unitId, cancellationToken);
        return Created($"/api/v1/analytics/snapshots?month={month}&unitId={unitId}", result);
    }

    /// <summary>
    /// Executive Dashboard: Headcount, nghỉ việc 12 tháng trượt, quỹ lương, tỷ lệ chi phí/doanh thu, phân bố KPI, top 5 biến động.
    /// </summary>
    [HttpGet("dashboards/executive")]
    [ProducesResponseType(typeof(ExecutiveDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutiveDashboard(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetExecutiveDashboardAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Unit Dashboard: Chỉ số đơn vị và con, danh sách người rủi ro cao.
    /// BẢO VỆ RIÊNG TƯ: Nhóm dưới 5 người thì ẩn số liệu lương tổng hợp và trả về "không đủ cỡ mẫu".
    /// </summary>
    [HttpGet("dashboards/unit/{unitId}")]
    [ProducesResponseType(typeof(UnitDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnitDashboard(int unitId, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetUnitDashboardAsync(unitId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Market Dashboard: Vị thế lương so với thị trường, CPI so với tăng lương, xu hướng lương tối thiểu vùng.
    /// </summary>
    [HttpGet("dashboards/market")]
    [ProducesResponseType(typeof(MarketDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarketDashboard(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetMarketDashboardAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Báo cáo rủi ro nghỉ việc dạng luật cộng điểm minh bạch.
    /// PHÂN QUYỀN: Chỉ UnitManager của người đó, HR và Executive xem được.
    /// </summary>
    [HttpGet("attrition-risks/unit/{unitId}")]
    [ProducesResponseType(typeof(AttritionRiskReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnitAttritionRisks(int unitId, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetUnitAttritionRisksAsync(unitId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bảng Vị thế lương (Pay Stance): So sánh Internal Mid với Market P50 theo JobFamily x Grade x Region.
    /// Gắn cờ khi thấp hơn thị trường > 10% và gợi ý MarketAdjustment.
    /// </summary>
    [HttpGet("pay-stance")]
    [ProducesResponseType(typeof(PayStanceReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayStanceReport([FromQuery] int? jobFamilyId, [FromQuery] int? regionId, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetPayStanceReportAsync(jobFamilyId, regionId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Dự báo chuỗi thời gian 6 tháng cho Headcount, Tỷ lệ nghỉ việc, Quỹ lương kèm khoảng tin cậy 95% và nhãn "DỰ BÁO".
    /// </summary>
    [HttpGet("forecasts")]
    [ProducesResponseType(typeof(ForecastReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSixMonthForecast(CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetSixMonthForecastAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Nộp phiếu khảo sát thôi việc (Exit Survey).
    /// </summary>
    [HttpPost("exit-surveys")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitExitSurvey([FromBody] SubmitExitSurveyRequest request, CancellationToken cancellationToken)
    {
        await _analyticsService.SubmitExitSurveyAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { success = true, message = "Ghi nhận phiếu khảo sát thôi việc thành công." });
    }

    /// <summary>
    /// Nhập số liệu khảo sát lương thị trường (Market Salary Benchmark) từ Excel.
    /// Dữ liệu demo phải gắn nhãn "DỮ LIỆU MẪU".
    /// </summary>
    [HttpPost("benchmarks/import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportMarketBenchmarks([FromBody] IReadOnlyList<ImportBenchmarkItemRequest> benchmarks, CancellationToken cancellationToken)
    {
        await _analyticsService.ImportMarketBenchmarksAsync(benchmarks, cancellationToken);
        return Ok(new { success = true, message = $"Nhập thành công {benchmarks.Count} bản ghi khảo sát lương thị trường." });
    }

    /// <summary>
    /// Tóm tắt điều hành bằng AI (Tùy chọn, tắt mặc định).
    /// Ẩn danh hóa 100%: Chỉ gửi số liệu tổng hợp vĩ mô, lưu prompt và kết quả vào AiAuditLog.
    /// </summary>
    [HttpPost("ai-summary")]
    [ProducesResponseType(typeof(AiSummaryResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateAiSummary([FromBody] AiSummaryRequest request, CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GenerateAiMonthlySummaryAsync(request, CurrentEmployeeId, cancellationToken);
        return Ok(result);
    }
}