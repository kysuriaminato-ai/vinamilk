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
[Route("api/v1/roster")]
[Authorize]
public class RosterController : ControllerBase
{
    private readonly IRosterService _rosterService;
    private readonly ICurrentUser _currentUser;

    public RosterController(IRosterService rosterService, ICurrentUser currentUser)
    {
        _rosterService = rosterService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Sinh lịch phân ca tự động trong tháng từ chu kỳ xoay ca (RotationPattern).
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Policy = Permissions.ChamcongManage)]
    public async Task<IActionResult> GenerateRoster([FromBody] GenerateRosterRequest request, CancellationToken cancellationToken)
    {
        int generatedCount = await _rosterService.GenerateMonthlyRosterAsync(request, cancellationToken);
        return Ok(new { message = $"Đã sinh lịch thành công {generatedCount} bản ghi.", count = generatedCount });
    }

    /// <summary>
    /// Điều chỉnh ca làm việc thủ công (TeamLeader chỉnh tay).
    /// </summary>
    [HttpPut("entry")]
    [Authorize(Policy = Permissions.ChamcongManage)]
    public async Task<IActionResult> UpdateRosterEntry([FromBody] UpdateRosterEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await _rosterService.UpdateRosterEntryAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Công bố lịch phân ca cho toàn bộ nhân viên đơn vị trong tháng.
    /// </summary>
    [HttpPost("publish")]
    [Authorize(Policy = Permissions.ChamcongApprove)]
    public async Task<IActionResult> PublishRoster([FromBody] PublishRosterRequest request, CancellationToken cancellationToken)
    {
        int publishedCount = await _rosterService.PublishRosterAsync(request, cancellationToken);
        return Ok(new { message = $"Đã công bố {publishedCount} ca làm việc.", count = publishedCount });
    }

    /// <summary>
    /// Lấy lịch phân ca dạng lưới (nhân viên × ngày) theo đơn vị và tháng.
    /// </summary>
    [HttpGet("grid")]
    [Authorize(Policy = Permissions.ChamcongView)]
    public async Task<IActionResult> GetRosterGrid([FromQuery] int unitId, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var grid = await _rosterService.GetRosterGridAsync(unitId, year, month, cancellationToken);
        return Ok(grid);
    }

    /// <summary>
    /// Đề xuất đổi ca làm việc giữa 2 nhân viên (ShiftSwap).
    /// </summary>
    [HttpPost("swap/propose")]
    public async Task<IActionResult> ProposeShiftSwap([FromBody] CreateShiftSwapRequest request, CancellationToken cancellationToken)
    {
        var swap = await _rosterService.ProposeShiftSwapAsync(request, cancellationToken);
        return Ok(swap);
    }

    /// <summary>
    /// Chấp thuận đề xuất đổi ca từ đồng nghiệp.
    /// </summary>
    [HttpPost("swap/{id}/accept")]
    public async Task<IActionResult> AcceptShiftSwap([FromRoute] int id, CancellationToken cancellationToken)
    {
        int empId = _currentUser.EmployeeId ?? 0;
        var swap = await _rosterService.AcceptShiftSwapAsync(id, empId, cancellationToken);
        return Ok(swap);
    }

    /// <summary>
    /// Trưởng ca phê duyệt đơn đổi ca làm việc.
    /// </summary>
    [HttpPost("swap/{id}/approve")]
    [Authorize(Policy = Permissions.ChamcongApprove)]
    public async Task<IActionResult> ApproveShiftSwap([FromRoute] int id, CancellationToken cancellationToken)
    {
        int approverId = _currentUser.EmployeeId ?? 0;
        var swap = await _rosterService.ApproveShiftSwapAsync(id, approverId, cancellationToken);
        return Ok(swap);
    }
}
