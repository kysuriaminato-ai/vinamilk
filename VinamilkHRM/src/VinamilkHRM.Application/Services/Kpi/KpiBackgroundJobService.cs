using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Services.Kpi;

namespace VinamilkHRM.Application.Services;

public class KpiBackgroundJobService : IKpiBackgroundJobService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<KpiBackgroundJobService> _logger;
    private readonly IWorkCalendar _workCalendar;

    public KpiBackgroundJobService(
        IApplicationDbContext context,
        ILogger<KpiBackgroundJobService> logger,
        IWorkCalendar? workCalendar = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workCalendar = workCalendar ?? new DefaultWorkCalendar();
    }

    public async Task RunKpiRemindersJobAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int dayOfMonth = today.Day;

        _logger.LogInformation("Đang thực thi Job nhắc nhở chu trình KPI ngày {Today}", today);

        // 1. Nhắc giao KPI ngày 25
        if (dayOfMonth == 25)
        {
            _logger.LogInformation("[KPI Reminder] Ngày 25: Bắt đầu mở đợt giao KPI cho kỳ đánh giá tiếp theo.");
        }

        // 2. Nhắc tự đánh giá ngày 1
        if (dayOfMonth == 1)
        {
            _logger.LogInformation("[KPI Reminder] Ngày 01: Nhân viên bắt đầu tự đánh giá KPI kỳ trước (Hạn chót ngày 03).");
        }

        // 3. Nhắc quản lý chấm ngày 5
        if (dayOfMonth == 5)
        {
            _logger.LogInformation("[KPI Reminder] Ngày 05: Quản lý/Lãnh đạo thẩm định và chấm điểm KPI (Hạn chót ngày 07).");
        }

        // 4. Cảnh báo các chỉ tiêu Task còn 2 ngày đến hạn
        var warningDueDate = today.AddDays(2);
        var pendingTasks = await _context.KpiItems
            .Include(i => i.KpiSheet)
                .ThenInclude(s => s!.Employee)
            .Where(i => i.ScoringMethod == KpiScoringMethod.Task &&
                        i.DueDate == warningDueDate &&
                        i.CompletedDate == null)
            .ToListAsync();

        foreach (var task in pendingTasks)
        {
            _logger.LogWarning(
                "[KPI Task Deadline Alert] Chỉ tiêu '{Title}' của nhân viên {Employee} (Mã: {Code}) sẽ đến hạn vào ngày {DueDate} (còn 2 ngày)!",
                task.Title,
                task.KpiSheet?.Employee?.FullName,
                task.KpiSheet?.Employee?.EmployeeCode,
                task.DueDate);
        }
    }

    public async Task ApplyOverduePenaltiesJobAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _logger.LogInformation("Đang kiểm tra và tự động áp phạt quy trình KPI ngày {Today}", today);

        var activePeriods = await _context.KpiPeriods
            .Include(p => p.Sheets)
                .ThenInclude(s => s.Items)
            .Where(p => p.Status != KpiPeriodStatus.Locked)
            .ToListAsync();

        foreach (var period in activePeriods)
        {
            // Tự áp phạt nhân viên không tự đánh giá đúng hạn (quá ngày SelfReviewDeadline)
            if (today > period.SelfReviewDeadline)
            {
                var unreviewedSheets = period.Sheets
                    .Where(s => s.Status == KpiStatus.InProgress || s.Status == KpiStatus.Assigned)
                    .ToList();

                foreach (var sheet in unreviewedSheets)
                {
                    // Tự động chuyển và áp phạt -2 điểm quy trình
                    sheet.SubmitSelfReview(DateTime.UtcNow, period.SelfReviewDeadline);
                    _logger.LogWarning(
                        "Nhân viên {EmpId} không tự đánh giá KPI đúng hạn kỳ {Period} -> Tự động chuyển quản lý chấm và áp phạt -2 điểm tổng.",
                        sheet.EmployeeId,
                        period.Period);
                }
            }

            // Tự áp phạt quản lý chấm trễ hạn (quá ngày ManagerReviewDeadline)
            if (today > period.ManagerReviewDeadline)
            {
                var managerReviewPendingSheets = period.Sheets
                    .Where(s => s.Status == KpiStatus.ManagerReview)
                    .ToList();

                var groupedByEvaluator = managerReviewPendingSheets.GroupBy(s => s.EvaluatorId);

                foreach (var group in groupedByEvaluator)
                {
                    int evaluatorId = group.Key;
                    int subCount = group.Count();

                    foreach (var sheet in group)
                    {
                        sheet.SubmitManagerReview(
                            evaluatorId,
                            DateTime.UtcNow,
                            period.ManagerReviewDeadline,
                            subCount,
                            "Hệ thống tự động áp phạt do quản lý chấm điểm quá hạn.");

                        _logger.LogWarning(
                            "Quản lý {EvaluatorId} chấm trễ hạn kỳ {Period} cho {SubCount} cấp dưới -> Phạt {Penalty} điểm PeopleManagement.",
                            evaluatorId,
                            period.Period,
                            subCount,
                            sheet.ManagerReviewLatePenalty);
                    }
                }
            }
        }

        await _context.SaveChangesAsync();
    }
}
