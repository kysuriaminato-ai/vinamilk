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
[Route("api/v1/overtime-requests")]
[Authorize]
public class OvertimeRequestsController : ControllerBase
{
    private readonly IOvertimeService _overtimeService;
    private readonly ICurrentUser _currentUser;

    public OvertimeRequestsController(IOvertimeService overtimeService, ICurrentUser currentUser)
    {
        _overtimeService = overtimeService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Đăng ký làm thêm giờ (OvertimeRequest).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOvertimeRequest([FromBody] CreateOvertimeRequest request, CancellationToken cancellationToken)
    {
        var result = await _overtimeService.CreateRequestAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trưởng ca / TeamLeader phê duyệt đơn làm thêm giờ.
    /// </summary>
    [HttpPost("{id}/approve-leader")]
    [Authorize(Policy = Permissions.ChamcongApprove)]
    public async Task<IActionResult> ApproveByTeamLeader([FromRoute] int id, CancellationToken cancellationToken)
    {
        int leaderId = _currentUser.EmployeeId ?? 0;
        var result = await _overtimeService.ApproveByTeamLeaderAsync(id, leaderId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Giám đốc đơn vị / UnitManager phê duyệt chính thức làm thêm giờ.
    /// </summary>
    [HttpPost("{id}/approve-manager")]
    [Authorize(Policy = Permissions.ChamcongApprove)]
    public async Task<IActionResult> ApproveByUnitManager([FromRoute] int id, CancellationToken cancellationToken)
    {
        int managerId = _currentUser.EmployeeId ?? 0;
        var result = await _overtimeService.ApproveByUnitManagerAsync(id, managerId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Từ chối đơn đăng ký làm thêm giờ.
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Policy = Permissions.ChamcongApprove)]
    public async Task<IActionResult> RejectOvertimeRequest([FromRoute] int id, [FromBody] string reason, CancellationToken cancellationToken)
    {
        var result = await _overtimeService.RejectAsync(id, reason, cancellationToken);
        return Ok(result);
    }
}
