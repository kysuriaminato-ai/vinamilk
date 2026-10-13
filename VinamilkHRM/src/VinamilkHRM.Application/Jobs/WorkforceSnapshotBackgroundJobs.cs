using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.Application.Jobs;

/// <summary>
/// Hangfire Background Job chạy tự động đêm cuối tháng (ví dụ 23:59 ngày cuối tháng).
/// Chụp ảnh số liệu nhân sự WorkforceSnapshot cho toàn bộ các đơn vị để lưu trữ bất biến.
/// </summary>
public class WorkforceSnapshotBackgroundJobs
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPeopleAnalyticsService _analyticsService;
    private readonly ILogger<WorkforceSnapshotBackgroundJobs> _logger;

    public WorkforceSnapshotBackgroundJobs(
        IApplicationDbContext dbContext,
        IPeopleAnalyticsService analyticsService,
        ILogger<WorkforceSnapshotBackgroundJobs> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Chụp snapshot tự động toàn bộ đơn vị trong tập đoàn vào đêm cuối tháng.
    /// </summary>
    public async Task CaptureMonthlyWorkforceSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        string currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        _logger.LogInformation("Bắt đầu job chụp ảnh số liệu nhân sự WorkforceSnapshot kỳ {Month}...", currentMonth);

        var units = await _dbContext.OrganizationalUnits.AsNoTracking().ToListAsync(cancellationToken);
        int count = 0;

        foreach (var unit in units)
        {
            try
            {
                await _analyticsService.CaptureSnapshotAsync(currentMonth, unit.Id, cancellationToken);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chụp snapshot đơn vị {UnitId} ({UnitName}) kỳ {Month}", unit.Id, unit.UnitName, currentMonth);
            }
        }

        _logger.LogInformation("Hoàn tất chụp snapshot kỳ {Month}: Đã ghi nhận {Count} đơn vị tổ chức thành công.", currentMonth, count);
    }
}