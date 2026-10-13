using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Interfaces;

namespace VinamilkHRM.Application.Jobs;

/// <summary>
/// Các tác vụ chạy nền định kỳ (Hangfire Recurring Jobs) cho Hệ thống Chấm công & Phân ca.
/// </summary>
public class AttendanceBackgroundJobs
{
    private readonly IApplicationDbContext _context;
    private readonly IAttendanceCalculationService _calculationService;
    private readonly ILogger<AttendanceBackgroundJobs> _logger;

    public AttendanceBackgroundJobs(
        IApplicationDbContext context,
        IAttendanceCalculationService calculationService,
        ILogger<AttendanceBackgroundJobs> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _calculationService = calculationService ?? throw new ArgumentNullException(nameof(calculationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Job tính công tự động mỗi 15 phút (Cron: "*/15 * * * *") cho ngày hiện tại.
    /// </summary>
    public async Task RunDailyAttendanceCalculationAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        _logger.LogInformation("Bắt đầu Job tính công định kỳ 15 phút cho ngày {Today}", today);

        var units = await _context.OrganizationalUnits
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        int totalProcessed = 0;
        foreach (var unitId in units)
        {
            try
            {
                int count = await _calculationService.CalculateDailyAttendanceForUnitAsync(unitId, today, cancellationToken);
                totalProcessed += count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính công tự động cho đơn vị Id={UnitId}", unitId);
            }
        }

        _logger.LogInformation("Hoàn tất Job tính công định kỳ: Đã xử lý {Count} nhân sự.", totalProcessed);
    }

    /// <summary>
    /// Job nhắc nhở xác nhận và phê duyệt bảng công (Cron: "0 8 1 * *" - 08:00 sáng ngày 1 hàng tháng).
    /// </summary>
    public async Task RunMonthlyTimesheetReminderAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.AddHours(7);
        var lastMonthDate = now.AddMonths(-1);
        string lastPeriod = $"{lastMonthDate.Year:D4}-{lastMonthDate.Month:D2}";

        _logger.LogInformation("Bắt đầu Job nhắc nhở phê duyệt bảng công kỳ {Period}", lastPeriod);

        var pendingPeriods = await _context.MonthlyTimesheetPeriods
            .Include(p => p.OrganizationalUnit)
            .Where(p => p.Period == lastPeriod && p.Status != Domain.Enums.TimesheetPeriodStatus.Locked)
            .ToListAsync(cancellationToken);

        foreach (var p in pendingPeriods)
        {
            _logger.LogWarning(
                "[NHẮC NHỞ DUYỆT CÔNG] Đơn vị {UnitName} (Id={UnitId}) chưa hoàn tất khóa sổ kỳ công {Period}. Trạng thái hiện tại: {Status}",
                p.OrganizationalUnit?.UnitName, p.OrganizationalUnitId, p.Period, p.Status);
        }
    }
}
