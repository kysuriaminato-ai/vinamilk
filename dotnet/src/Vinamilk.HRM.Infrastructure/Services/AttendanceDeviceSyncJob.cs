using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Infrastructure.Services;

/// <summary>
/// Hangfire recurring job đồng bộ dữ liệu chấm công từ các máy chấm công đang hoạt động.
/// </summary>
public sealed class AttendanceDeviceSyncJob : IAttendanceDeviceSyncJob
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IAttendanceDeviceConnector> _connectors;
    private readonly ILogger<AttendanceDeviceSyncJob> _logger;

    public AttendanceDeviceSyncJob(
        IApplicationDbContext context,
        IEnumerable<IAttendanceDeviceConnector> connectors,
        ILogger<AttendanceDeviceSyncJob> logger)
    {
        _context = context;
        _connectors = connectors;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeviceSyncSummaryDto>> SyncAllActiveDevicesAsync(CancellationToken cancellationToken = default)
    {
        var devices = await _context.AttendanceDevices
            .Where(d => d.IsActive)
            .OrderBy(d => d.DeviceCode)
            .ToListAsync(cancellationToken);

        var summaries = new List<DeviceSyncSummaryDto>();
        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connector = _connectors.FirstOrDefault(c =>
                string.Equals(c.ConnectorType, device.ConnectorType, StringComparison.OrdinalIgnoreCase));
            if (connector == null)
            {
                summaries.Add(new DeviceSyncSummaryDto(
                    device.DeviceCode, 0, 0, 0, 0,
                    $"Không tìm thấy connector loại '{device.ConnectorType}'."));
                continue;
            }

            try
            {
                var punches = await connector.PullPunchesAsync(device, cancellationToken);
                var summary = await AttendanceDeviceIngestionEngine.IngestDevicePunchesAsync(
                    _context,
                    device.DeviceCode,
                    punches,
                    SystemJobAccount.Username,
                    cancellationToken);

                device.LastSyncAt = DateTime.UtcNow;
                var systemUserId = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Username == SystemJobAccount.Username)
                    .Select(u => (int?)u.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                await _context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = systemUserId,
                    Username = SystemJobAccount.Username,
                    ActionType = "AttendanceDeviceSync",
                    ModuleName = "TimeAttendance",
                    EntityName = "AttendanceDevice",
                    EntityId = device.DeviceCode,
                    NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        device.DeviceCode,
                        summary.TotalRead,
                        summary.ImportedCount,
                        summary.SkippedDuplicateCount,
                        summary.UnmappedCount,
                        summary.Message
                    }),
                    IpAddress = "127.0.0.1",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                summaries.Add(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đồng bộ máy chấm công {DeviceCode}", device.DeviceCode);
                summaries.Add(new DeviceSyncSummaryDto(
                    device.DeviceCode, 0, 0, 0, 0,
                    $"Lỗi đồng bộ: {ex.Message}"));
            }
        }

        return summaries;
    }
}
