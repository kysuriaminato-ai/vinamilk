using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/kpis")]
[Authorize]
public class KpiController : ControllerBase
{
    private readonly IKpiService _kpiService;
    private readonly IKpiCalibrationService _calibrationService;
    private readonly ICurrentUser _currentUser;

    public KpiController(
        IKpiService kpiService,
        IKpiCalibrationService calibrationService,
        ICurrentUser currentUser)
    {
        _kpiService = kpiService ?? throw new ArgumentNullException(nameof(kpiService));
        _calibrationService = calibrationService ?? throw new ArgumentNullException(nameof(calibrationService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    // --- 1. Thư viện chỉ tiêu (KpiDefinition) ---
    [HttpGet("definitions")]
    public async Task<IActionResult> GetDefinitions([FromQuery] int? jobFamilyId, CancellationToken ct)
    {
        var result = await _kpiService.GetDefinitionsAsync(jobFamilyId, ct);
        return Ok(result);
    }

    [HttpGet("definitions/{id:int}")]
    public async Task<IActionResult> GetDefinitionById(int id, CancellationToken ct)
    {
        var result = await _kpiService.GetDefinitionByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("definitions")]
    public async Task<IActionResult> CreateDefinition([FromBody] CreateKpiDefinitionRequest request, CancellationToken ct)
    {
        var result = await _kpiService.CreateDefinitionAsync(request, ct);
        return CreatedAtAction(nameof(GetDefinitionById), new { id = result.Id }, result);
    }

    // --- 2. Khung tỷ trọng 4 nhóm (KpiScheme) ---
    [HttpGet("schemes")]
    public async Task<IActionResult> GetSchemes(CancellationToken ct)
    {
        var result = await _kpiService.GetSchemesAsync(ct);
        return Ok(result);
    }

    // --- 3. Kỳ đánh giá (KpiPeriod) ---
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods(CancellationToken ct)
    {
        var result = await _kpiService.GetPeriodsAsync(ct);
        return Ok(result);
    }

    [HttpGet("periods/{period}")]
    public async Task<IActionResult> GetPeriodByCode(string period, CancellationToken ct)
    {
        var result = await _kpiService.GetPeriodByCodeAsync(period, ct);
        return Ok(result);
    }

    [HttpPost("periods")]
    public async Task<IActionResult> CreatePeriod([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var result = await _kpiService.CreatePeriodAsync(year, month, ct);
        return Ok(result);
    }

    [HttpPost("periods/{period}/lock")]
    public async Task<IActionResult> LockPeriod(string period, CancellationToken ct)
    {
        string lockedBy = _currentUser.UserName ?? "SYSTEM";
        await _kpiService.LockPeriodAsync(period, lockedBy, ct);
        return Ok(new { message = $"Đã khóa kỳ đánh giá {period} thành công. Dữ liệu KpiResult bất biến đã được sinh ra." });
    }

    // --- 4. Phiếu KPI & Giao chỉ tiêu (KpiSheet) ---
    [HttpGet("sheets/{id:int}")]
    public async Task<IActionResult> GetSheetById(int id, CancellationToken ct)
    {
        var result = await _kpiService.GetSheetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("sheets/employee/{employeeId:int}")]
    public async Task<IActionResult> GetEmployeeSheet(int employeeId, [FromQuery] string period, CancellationToken ct)
    {
        var result = await _kpiService.GetEmployeeSheetInPeriodAsync(employeeId, period, ct);
        return result != null ? Ok(result) : NotFound(new { message = $"Không tìm thấy phiếu KPI của nhân viên {employeeId} trong kỳ {period}." });
    }

    [HttpGet("sheets/unit/{unitId:int}")]
    public async Task<IActionResult> GetSheetsByUnit(int unitId, [FromQuery] string period, CancellationToken ct)
    {
        var result = await _kpiService.GetSheetsByUnitAsync(unitId, period, ct);
        return Ok(result);
    }

    [HttpPost("sheets")]
    public async Task<IActionResult> CreateSheet([FromBody] CreateKpiSheetRequest request, CancellationToken ct)
    {
        var result = await _kpiService.CreateSheetAsync(request, ct);
        return CreatedAtAction(nameof(GetSheetById), new { id = result.Id }, result);
    }

    [HttpPost("sheets/{id:int}/publish")]
    public async Task<IActionResult> PublishSheet(int id, CancellationToken ct)
    {
        await _kpiService.PublishSheetAsync(id, ct);
        return Ok(new { message = "Đã công bố phiếu KPI thành công." });
    }

    [HttpPost("sheets/bulk-assign")]
    public async Task<IActionResult> BulkAssignKpi([FromBody] BulkAssignKpiRequest request, CancellationToken ct)
    {
        int count = await _kpiService.BulkAssignKpiAsync(request, ct);
        return Ok(new { assignedCount = count, message = $"Đã giao KPI hàng loạt thành công cho {count} nhân sự trong đơn vị." });
    }

    // --- 5. Đánh giá & Thẩm định ---
    [HttpPost("sheets/{id:int}/self-review")]
    public async Task<IActionResult> SubmitSelfReview(int id, [FromBody] SubmitSelfReviewRequest request, CancellationToken ct)
    {
        await _kpiService.SubmitSelfReviewAsync(id, request, ct);
        return Ok(new { message = "Nhân viên đã nộp kết quả tự đánh giá KPI thành công." });
    }

    [HttpPost("sheets/{id:int}/manager-review")]
    public async Task<IActionResult> SubmitManagerReview(int id, [FromBody] SubmitManagerReviewRequest request, CancellationToken ct)
    {
        int managerId = _currentUser.EmployeeId ?? 1;
        await _kpiService.SubmitManagerReviewAsync(id, managerId, request, ct);
        return Ok(new { message = "Quản lý đã thẩm định và chốt điểm KPI thành công." });
    }

    // --- 6. Gia hạn chỉ tiêu Task & Khiếu nại ---
    [HttpPost("extensions")]
    public async Task<IActionResult> RequestExtension([FromBody] RequestKpiExtensionInput input, CancellationToken ct)
    {
        var result = await _kpiService.RequestExtensionAsync(input, ct);
        return Ok(result);
    }

    [HttpPost("extensions/approve")]
    public async Task<IActionResult> ApproveExtension([FromBody] ApproveKpiExtensionInput input, CancellationToken ct)
    {
        int approverId = _currentUser.EmployeeId ?? 1;
        await _kpiService.ApproveExtensionAsync(input, approverId, ct);
        return Ok(new { message = "Đã xử lý phê duyệt yêu cầu gia hạn thành công." });
    }

    [HttpPost("appeals")]
    public async Task<IActionResult> SubmitAppeal([FromBody] SubmitKpiAppealRequest request, CancellationToken ct)
    {
        int empId = _currentUser.EmployeeId ?? 1;
        await _kpiService.SubmitAppealAsync(request, empId, ct);
        return Ok(new { message = "Đã nộp khiếu nại kết quả KPI thành công. Kết quả xét lại sẽ áp dụng vào kỳ sau." });
    }

    // --- 7. Hiệu chuẩn & Phân bố điểm ---
    [HttpGet("calibration/{unitId:int}")]
    public async Task<IActionResult> GetCalibrationSummary(int unitId, [FromQuery] string period, CancellationToken ct)
    {
        var result = await _calibrationService.GetCalibrationSummaryAsync(unitId, period, ct);
        return Ok(result);
    }

    [HttpPost("calibration/apply")]
    public async Task<IActionResult> ApplyCalibration([FromBody] List<ApplyCalibrationInput> inputs, CancellationToken ct)
    {
        string reviewer = _currentUser.UserName ?? "LEADER";
        await _calibrationService.ApplyCalibrationAsync(inputs, reviewer, ct);
        return Ok(new { message = $"Đã cập nhật hiệu chuẩn cho {inputs.Count} phiếu KPI thành công." });
    }

    // --- 8. Kanban, Heatmap, Xu hướng ---
    [HttpGet("kanban")]
    public async Task<IActionResult> GetKanbanBoard([FromQuery] string period, [FromQuery] int? unitId, CancellationToken ct)
    {
        var result = await _kpiService.GetKanbanBoardAsync(period, unitId, ct);
        return Ok(result);
    }

    [HttpGet("heatmap/{unitId:int}")]
    public async Task<IActionResult> GetHeatmap(int unitId, [FromQuery] int months = 6, CancellationToken ct = default)
    {
        var result = await _kpiService.GetUnitHeatmapAsync(unitId, months, ct);
        return Ok(result);
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] int? unitId, CancellationToken ct)
    {
        var result = await _kpiService.Get12MonthTrendAsync(unitId, ct);
        return Ok(result);
    }
}
