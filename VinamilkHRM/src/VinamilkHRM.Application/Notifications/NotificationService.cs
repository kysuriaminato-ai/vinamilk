using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Notifications;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Notifications;

namespace VinamilkHRM.Application.Notifications;

/// <summary>
/// Dịch vụ quản lý thông báo tập trung, tích hợp Transactional Outbox Pattern và tùy chọn người dùng.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender? _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext context,
        ILogger<NotificationService> logger,
        IEmailSender? emailSender = null)
    {
        _context = context;
        _logger = logger;
        _emailSender = emailSender;
    }

    public async Task QueueNotificationAsync(
        int recipientId,
        string templateCode,
        Dictionary<string, string> parameters,
        string? targetUrl = null,
        CancellationToken ct = default)
    {
        var recipient = await _context.Employees.FindAsync(new object[] { recipientId }, ct);
        if (recipient == null)
            throw new DomainException($"Không tìm thấy nhân viên ID {recipientId} để gửi thông báo.", "ERR_NOTIF_EMP_NOT_FOUND");

        var template = await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Code == templateCode && t.IsActive, ct);

        string subject;
        string body;
        NotificationCategory category = NotificationCategory.Workflow;

        if (template != null)
        {
            category = template.Category;
            var rendered = template.Render(parameters);
            subject = rendered.Subject;
            body = rendered.Body;
        }
        else
        {
            // Mẫu dự phòng mặc định
            subject = $"Thông báo từ Hệ thống Vinamilk HRM: {templateCode}";
            body = string.Join("\n", parameters.Select(p => $"{p.Key}: {p.Value}"));
        }

        // Kiểm tra tùy chọn nhận thông báo của người dùng
        var pref = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.EmployeeId == recipientId && p.Category == category, ct);

        bool allowInApp = pref?.AllowInApp ?? true;
        bool allowEmail = (pref?.AllowEmail ?? true) && !string.IsNullOrWhiteSpace(recipient.Email);

        // Lưu vào Transactional Outbox
        if (allowInApp)
        {
            var inAppOutbox = new NotificationOutboxMessage(
                recipientId,
                recipient.Email,
                NotificationChannel.InApp,
                category,
                subject,
                body,
                targetUrl);
            _context.NotificationOutboxMessages.Add(inAppOutbox);
        }

        if (allowEmail)
        {
            var emailOutbox = new NotificationOutboxMessage(
                recipientId,
                recipient.Email,
                NotificationChannel.Email,
                category,
                subject,
                body,
                targetUrl);
            _context.NotificationOutboxMessages.Add(emailOutbox);
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Đã xếp hàng thông báo '{TemplateCode}' cho nhân viên {RecipientId}.", templateCode, recipientId);
    }

    public async Task<List<InAppNotification>> GetUserNotificationsAsync(
        int employeeId,
        int limit = 20,
        CancellationToken ct = default)
    {
        return await _context.InAppNotifications
            .Where(n => n.RecipientEmployeeId == employeeId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task MarkNotificationAsReadAsync(
        int notificationId,
        int employeeId,
        CancellationToken ct = default)
    {
        var notif = await _context.InAppNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientEmployeeId == employeeId, ct);

        if (notif != null && !notif.IsRead)
        {
            notif.MarkAsRead(DateTimeOffset.UtcNow);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<int> ProcessOutboxMessagesAsync(
        int batchSize = 50,
        CancellationToken ct = default)
    {
        var pendingMessages = await _context.NotificationOutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        int processedCount = 0;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var msg in pendingMessages)
        {
            try
            {
                if (msg.Channel == NotificationChannel.InApp)
                {
                    var inApp = new InAppNotification(
                        msg.RecipientEmployeeId,
                        msg.Subject,
                        msg.Body,
                        msg.Category,
                        msg.TargetUrl);
                    _context.InAppNotifications.Add(inApp);
                    msg.MarkSent(now);
                    processedCount++;
                }
                else if (msg.Channel == NotificationChannel.Email && !string.IsNullOrEmpty(msg.RecipientEmail) && _emailSender != null)
                {
                    await _emailSender.SendEmailAsync(msg.RecipientEmail, msg.Subject, msg.Body, ct);
                    msg.MarkSent(now);
                    processedCount++;
                }
                else
                {
                    // Kênh không hỗ trợ hoặc không có cấu hình email -> đánh dấu hoàn tất
                    msg.MarkSent(now);
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi xử lý outbox message ID {Id}: {Error}", msg.Id, ex.Message);
                msg.MarkFailed(ex.Message);
            }
        }

        if (pendingMessages.Any())
        {
            await _context.SaveChangesAsync(ct);
        }

        return processedCount;
    }
}