using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;

namespace Vinamilk.HRM.Infrastructure.Services;

public sealed class PayslipEmailHangfireJob : IPayslipEmailJob
{
    private readonly IApplicationDbContext _context;
    private readonly IPayrollDocumentService _documentService;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<PayslipEmailHangfireJob> _logger;

    public PayslipEmailHangfireJob(
        IApplicationDbContext context,
        IPayrollDocumentService documentService,
        IEmailNotificationService emailService,
        ILogger<PayslipEmailHangfireJob> logger)
    {
        _context = context;
        _documentService = documentService;
        _emailService = emailService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 10, 30, 60 })]
    public async Task SendSinglePayslipAsync(long dispatchLogId, CancellationToken cancellationToken = default)
    {
        var log = await _context.PayslipDispatchLogs
            .FirstOrDefaultAsync(l => l.Id == dispatchLogId, cancellationToken);

        if (log is null)
            return;

        var systemJobUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == SystemJobAccount.Username, cancellationToken);
        var jobUsername = systemJobUser?.Username ?? SystemJobAccount.Username;

        log.AttemptCount += 1;
        log.UpdatedBy = jobUsername;

        var record = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Include(r => r.OrganizationUnit)
            .Include(r => r.Position)
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == log.PayrollRecordId, cancellationToken);

        if (record is null)
        {
            log.Status = "Failed";
            log.ErrorMessage = $"Không tìm thấy bản ghi lương #{log.PayrollRecordId}.";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(log.RecipientEmail) || !log.RecipientEmail.Contains('@'))
        {
            log.Status = "Failed";
            log.ErrorMessage = "Nhân viên chưa có địa chỉ email hợp lệ để nhận phiếu lương.";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            var periodEnd = record.PayrollPeriod.EndDate != default
                ? record.PayrollPeriod.EndDate
                : new DateOnly(record.PayrollPeriod.Year, record.PayrollPeriod.Month,
                    DateTime.DaysInMonth(record.PayrollPeriod.Year, record.PayrollPeriod.Month));

            var statutory = await VietnamPayrollEngine.ResolveStatutoryContextAsync(_context, periodEnd, cancellationToken);
            var pdfResult = _documentService.GeneratePayslipPdf(
                record,
                record.Employee,
                record.PayrollPeriod,
                statutory.PitBrackets,
                applyPassword: true);

            var htmlSummary =
                $"<p>Kính gửi <strong>{record.Employee.FullName}</strong> ({record.Employee.EmployeeCode}),</p>" +
                $"<p>Đính kèm là Phiếu lương chi tiết kỳ <strong>{record.PayrollPeriod.PeriodTitle}</strong>.</p>" +
                $"<ul><li>Thu nhập Gross: <strong>{record.GrossIncome:N0} VNĐ</strong></li>" +
                $"<li>Thực lĩnh (Net): <strong>{record.NetSalary:N0} VNĐ</strong></li></ul>" +
                $"<p><em>{pdfResult.PasswordHint}</em></p>";

            await _emailService.SendPayslipEmailWithAttachmentAsync(
                log.RecipientEmail,
                record.Employee.FullName,
                $"[Vinamilk HRM] Phiếu lương {record.PayrollPeriod.PeriodTitle} - {record.Employee.EmployeeCode}",
                record.PayrollPeriod.PeriodTitle,
                htmlSummary,
                pdfResult.PdfBytes,
                pdfResult.FileName,
                cancellationToken);

            log.Status = "Sent";
            log.ErrorMessage = null;
            log.SentAt = DateTime.UtcNow;
            log.UpdatedBy = jobUsername;
            await _context.AuditLogs.AddAsync(new Domain.Entities.SystemEntities.AuditLog
            {
                UserId = systemJobUser?.Id,
                Username = jobUsername,
                ModuleName = "Payroll",
                ActionType = "PayslipEmail_Sent",
                EntityName = nameof(Domain.Entities.Payroll.PayslipDispatchLog),
                EntityId = log.Id.ToString(),
                NewValuesJson = $"{{\"PayrollPeriodId\":{log.PayrollPeriodId},\"EmployeeCode\":\"{log.EmployeeCode}\",\"Status\":\"Sent\"}}",
                IpAddress = "127.0.0.1",
                UserAgent = "HangfireJob",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = jobUsername
            }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
            log.UpdatedBy = jobUsername;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning(ex, "[PayslipEmailHangfireJob] Gửi phiếu lương thất bại (lần {Attempt}) cho {Email}",
                log.AttemptCount, log.RecipientEmail);
            throw;
        }
    }
}

public sealed class HangfirePayslipBatchDispatcher : IPayslipBatchDispatcher
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IApplicationDbContext _context;

    public HangfirePayslipBatchDispatcher(
        IBackgroundJobClient backgroundJobClient,
        IApplicationDbContext context)
    {
        _backgroundJobClient = backgroundJobClient;
        _context = context;
    }

    public async Task<Guid> EnqueueBatchAsync(
        int payrollPeriodId,
        IReadOnlyList<long> dispatchLogIds,
        CancellationToken cancellationToken = default)
    {
        var logs = await _context.PayslipDispatchLogs
            .Where(l => dispatchLogIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
        {
            log.HangfireJobId = _backgroundJobClient.Enqueue<IPayslipEmailJob>(
                job => job.SendSinglePayslipAsync(log.Id, CancellationToken.None));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return logs.FirstOrDefault()?.BatchId ?? Guid.NewGuid();
    }
}

public sealed class InProcessPayslipBatchDispatcher : IPayslipBatchDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InProcessPayslipBatchDispatcher> _logger;

    public InProcessPayslipBatchDispatcher(
        IServiceProvider serviceProvider,
        ILogger<InProcessPayslipBatchDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Guid> EnqueueBatchAsync(
        int payrollPeriodId,
        IReadOnlyList<long> dispatchLogIds,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IPayslipEmailJob>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        foreach (var id in dispatchLogIds)
        {
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await job.SendSinglePayslipAsync(id, cancellationToken);
                    break;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogWarning(ex, "[InProcessPayslipDispatcher] Retry {Attempt}/{Max} cho log #{LogId}",
                        attempt, maxAttempts, id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[InProcessPayslipDispatcher] Thất bại sau {Max} lần thử cho log #{LogId}",
                        maxAttempts, id);
                }
            }
        }

        var batchId = await db.PayslipDispatchLogs
            .AsNoTracking()
            .Where(l => dispatchLogIds.Contains(l.Id))
            .Select(l => (Guid?)l.BatchId)
            .FirstOrDefaultAsync(cancellationToken);

        return batchId ?? Guid.NewGuid();
    }
}

