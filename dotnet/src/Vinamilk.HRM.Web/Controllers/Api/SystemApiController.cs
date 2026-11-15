using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Web.Controllers.Api;

[ApiController]
[Route("api/v1/system")]
[Authorize(Policy = "System.Backup")]
[Produces("application/json")]
public class SystemApiController : ControllerBase
{
    private readonly DatabaseBackupService _backupService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SystemApiController(
        DatabaseBackupService backupService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _backupService = backupService;
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Kích hoạt sao lưu dữ liệu định kỳ trên máy chủ (chỉ SuperAdmin / System.Backup, có rate limit & AuditLog).
    /// </summary>
    [HttpPost("backup")]
    [EnableRateLimiting("backup-limiter")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> TriggerBackup(CancellationToken cancellationToken = default)
    {
        var result = await _backupService.RunScheduledBackupAsync(cancellationToken: cancellationToken);

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Username = _currentUserService.Username ?? "SYSTEM",
            ModuleName = "System",
            ActionType = "Backup",
            EntityName = "Database",
            EntityId = "VinamilkHRM",
            NewValuesJson = $"{{\"mode\":\"{result.Mode}\",\"success\":{result.Success.ToString().ToLowerInvariant()}}}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        });
        await _context.SaveChangesAsync(cancellationToken);

        // Không bao giờ trả đường dẫn file vật lý hay dữ liệu backup về response
        return Ok(new
        {
            success = result.Success,
            message = "Sao lưu dữ liệu định kỳ trên máy chủ đã hoàn tất.",
            timestampUtc = DateTime.UtcNow
        });
    }
}

