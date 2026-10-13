using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.WebApi.Controllers;

[ApiController]
[Route("api/v1/time-events")]
[Authorize]
public class TimeEventsController : ControllerBase
{
    private readonly ITimeEventService _timeEventService;
    private readonly ICurrentUser _currentUser;

    public TimeEventsController(ITimeEventService timeEventService, ICurrentUser currentUser)
    {
        _timeEventService = timeEventService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Nhận dữ liệu quẹt thẻ hàng loạt từ máy chấm công (Batch Punches). Idempotent theo (DeviceId, EmployeeId, Timestamp).
    /// </summary>
    [HttpPost("batch")]
    [AllowAnonymous] // Cho phép máy chấm công đẩy dữ liệu qua API key hoặc m2m token
    public async Task<IActionResult> BatchCreateTimeEvents([FromBody] BatchTimeEventsRequest request, CancellationToken cancellationToken)
    {
        var response = await _timeEventService.BatchCreateAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Chấm công di động qua kiểm tra tọa độ Geofence (Mobile GPS Punch).
    /// </summary>
    [HttpPost("mobile")]
    public async Task<IActionResult> MobilePunch([FromBody] MobilePunchRequest request, CancellationToken cancellationToken)
    {
        var result = await _timeEventService.MobilePunchAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Nhập dữ liệu quẹt thẻ từ tệp Excel máy chấm công.
    /// </summary>
    [HttpPost("import-excel")]
    [Authorize(Policy = Permissions.ChamcongAdd)]
    public async Task<IActionResult> ImportFromExcel(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn tệp Excel hợp lệ." });
        }

        using var stream = file.OpenReadStream();
        var result = await _timeEventService.ImportFromExcelAsync(stream, cancellationToken);
        return Ok(result);
    }
}
