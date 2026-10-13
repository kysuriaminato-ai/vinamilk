using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/kpi-metrics")]
[Authorize]
public class KpiMetricsController : ControllerBase
{
    private readonly IKpiMetricService _metricService;

    public KpiMetricsController(IKpiMetricService metricService)
    {
        _metricService = metricService ?? throw new ArgumentNullException(nameof(metricService));
    }

    [HttpPost]
    public async Task<IActionResult> RecordMetric([FromBody] PostKpiMetricRequest request, CancellationToken ct)
    {
        await _metricService.RecordMetricAsync(request, ct);
        return Ok(new { message = $"Đã ghi nhận chỉ số {request.MetricCode} giá trị {request.Value} {request.Unit} thành công." });
    }

    [HttpPost("sync-attendance")]
    public async Task<IActionResult> SyncAttendance([FromQuery] string period, [FromQuery] int? unitId, CancellationToken ct)
    {
        await _metricService.SyncAttendanceMetricsAsync(period, unitId, ct);
        return Ok(new { message = $"Đã đồng bộ chỉ số chuyên cần từ bảng công kỳ {period} thành công." });
    }

    [HttpPost("import-excel")]
    public async Task<IActionResult> ImportExcel(IFormFile file, [FromQuery] string period, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng đính kèm file Excel số liệu KPI (.xlsx)." });

        using var stream = file.OpenReadStream();
        await _metricService.ImportMetricsFromExcelAsync(stream, period, ct);
        return Ok(new { message = $"Đã import số liệu chỉ số KPI từ Excel vào kỳ {period} thành công." });
    }
}
