using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceCalculationService _calculationService;
    private readonly IAttendanceExplanationService _explanationService;
    private readonly ITimesheetPeriodService _periodService;
    private readonly ICurrentUser _currentUser;

    public AttendanceController(
        IAttendanceCalculationService calculationService,
        IAttendanceExplanationService explanationService,
        ITimesheetPeriodService periodService,
        ICurrentUser currentUser)
    {
        _calculationService = calculationService;
        _explanationService = explanationService;
        _periodService = periodService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Tính công hàng ngày cho một nhân viên hoặc toàn bộ đơn vị.
    /// </summary>
    [HttpPost("calculate-day")]
    [Authorize(Policy = Permissions.ChamcongManage)]
    public async Task<IActionResult> CalculateDailyAttendance(
        [FromQuery] int? employeeId,
        [FromQuery] int? unitId,
        [FromQuery] DateOnly workDate,
        CancellationToken cancellationToken)
    {
        if (employeeId.HasValue)
        {
            var result = await _calculationService.CalculateDailyAttendanceAsync(employeeId.Value, workDate, cancellationToken);
            return Ok(result);
        }

        if (unitId.HasValue)
        {
            int count = await _calculationService.CalculateDailyAttendanceForUnitAsync(unitId.Value, workDate, cancellationToken);
            return Ok(new { message = $"Đã tính công thành công cho {count} nhân sự.", processedCount = count });
        }

        return BadRequest(new { message = "Vui lòng cung cấp EmployeeId hoặc UnitId." });
    }

    /// <summary>
    /// Tính tổng hợp công tháng cho toàn đơn vị.
    /// </summary>
    [HttpPost("calculate-month")]
    [Authorize(Policy = Permissions.ChamcongManage)]
    public async Task<IActionResult> CalculateMonthlySummary(
        [FromQuery] int unitId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        int count = await _calculationService.CalculateMonthlySummaryForUnitAsync(unitId, year, month, cancellationToken);
        return Ok(new { message = $"Đã tổng hợp công tháng cho {count} nhân sự.", processedCount = count });
    }

    /// <summary>
    /// Gửi giải trình quên quẹt thẻ chấm công (giới hạn tối đa 3 lần/tháng).
    /// </summary>
    [HttpPost("explanations")]
    public async Task<IActionResult> SubmitExplanation([FromBody] CreateExplanationRequest request, CancellationToken cancellationToken)
    {
        var result = await _explanationService.SubmitExplanationAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trưởng ca phê duyệt đơn giải trình quên chấm công.
    /// </summary>
    [HttpPost("explanations/{id}/approve")]
    [Authorize(Policy = Permissions.ChamcongApprove)]
    public async Task<IActionResult> ApproveExplanation([FromRoute] int id, CancellationToken cancellationToken)
    {
        int approverId = _currentUser.EmployeeId ?? 0;
        var result = await _explanationService.ApproveExplanationAsync(id, approverId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trưởng ca từ chối đơn giải trình quên chấm công.
    /// </summary>
    [HttpPost("explanations/{id}/reject")]
    [Authorize(Policy = Permissions.ChamcongApprove)]
    public async Task<IActionResult> RejectExplanation([FromRoute] int id, [FromBody] string reason, CancellationToken cancellationToken)
    {
        var result = await _explanationService.RejectExplanationAsync(id, reason, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// TeamLeader xác nhận số liệu công tháng của bộ phận.
    /// </summary>
    [HttpPost("periods/confirm")]
    [Authorize(Policy = Permissions.ChamcongApprove)]
    public async Task<IActionResult> ConfirmPeriod([FromBody] LockTimesheetPeriodRequest request, CancellationToken cancellationToken)
    {
        int leaderId = _currentUser.EmployeeId ?? 0;
        await _periodService.ConfirmByLeaderAsync(request.Period, request.OrganizationalUnitId, leaderId, cancellationToken);
        return Ok(new { message = $"TeamLeader đã xác nhận thành công kỳ công {request.Period}." });
    }

    /// <summary>
    /// UnitManager phê duyệt bảng công đơn vị.
    /// </summary>
    [HttpPost("periods/approve")]
    [Authorize(Policy = Permissions.ChamcongApprove)]
    public async Task<IActionResult> ApprovePeriod([FromBody] LockTimesheetPeriodRequest request, CancellationToken cancellationToken)
    {
        int managerId = _currentUser.EmployeeId ?? 0;
        await _periodService.ApproveByManagerAsync(request.Period, request.OrganizationalUnitId, managerId, cancellationToken);
        return Ok(new { message = $"UnitManager đã phê duyệt bảng công kỳ {request.Period}." });
    }

    /// <summary>
    /// HR Khóa bảng công tháng - Trở thành BẤT BIẾN (chỉ đọc).
    /// </summary>
    [HttpPost("periods/lock")]
    [Authorize(Policy = Permissions.ChamcongLock)]
    public async Task<IActionResult> LockPeriod([FromBody] LockTimesheetPeriodRequest request, CancellationToken cancellationToken)
    {
        int hrId = _currentUser.EmployeeId ?? 0;
        await _periodService.LockPeriodAsync(request.Period, request.OrganizationalUnitId, hrId, cancellationToken);
        return Ok(new { message = $"Kỳ công {request.Period} đã được khóa sổ thành công. Dữ liệu hiện là bất biến." });
    }

    /// <summary>
    /// Ghi nhận khoản điều chỉnh công sau khi kỳ công đã khóa (AttendanceAdjustment).
    /// </summary>
    [HttpPost("adjustments")]
    [Authorize(Policy = Permissions.ChamcongManage)]
    public async Task<IActionResult> CreateAdjustment([FromBody] CreateAdjustmentRequest request, CancellationToken cancellationToken)
    {
        string createdBy = _currentUser.UserName ?? "HR_SYSTEM";
        int id = await _periodService.CreateAdjustmentAsync(request, createdBy, cancellationToken);
        return Ok(new { message = "Đã ghi nhận điều chỉnh công thành công.", adjustmentId = id });
    }

    /// <summary>
    /// Lấy bảng công tháng theo mã công chuẩn của đơn vị.
    /// </summary>
    [HttpGet("monthly-summary")]
    [Authorize(Policy = Permissions.ChamcongView)]
    public async Task<IActionResult> GetMonthlySummaries([FromQuery] string period, [FromQuery] int unitId, CancellationToken cancellationToken)
    {
        var summaries = await _periodService.GetMonthlySummariesAsync(period, unitId, cancellationToken);
        return Ok(summaries);
    }

    /// <summary>
    /// Dashboard tổng hợp tình hình chấm công hôm nay (có mặt, muộn, vắng, tăng ca).
    /// </summary>
    [HttpGet("today-dashboard")]
    [Authorize(Policy = Permissions.ChamcongView)]
    public async Task<IActionResult> GetTodayDashboard([FromQuery] int? unitId, CancellationToken cancellationToken)
    {
        var dashboard = await _periodService.GetTodayDashboardAsync(unitId, cancellationToken);
        return Ok(dashboard);
    }
}
