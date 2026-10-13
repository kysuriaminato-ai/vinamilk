using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Notifications;

/// <summary>
/// Bản ghi Transactional Outbox Pattern cho thông báo.
/// Đảm bảo không mất thông báo khi gặp sự cố mạng hoặc máy chủ email.
/// </summary>
public class NotificationOutboxMessage : BaseAuditableEntity
{
    public int RecipientEmployeeId { get; private set; }
    public string? RecipientEmail { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationCategory Category { get; private set; }

    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? TargetUrl { get; private set; }

    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;
    public int RetryCount { get; private set; } = 0;
    public int MaxRetries { get; private set; } = 3;
    public DateTimeOffset? SentAt { get; private set; }
    public string? LastError { get; private set; }

    protected NotificationOutboxMessage() { }

    public NotificationOutboxMessage(
        int recipientEmployeeId,
        string? recipientEmail,
        NotificationChannel channel,
        NotificationCategory category,
        string subject,
        string body,
        string? targetUrl = null,
        int maxRetries = 3)
    {
        RecipientEmployeeId = recipientEmployeeId;
        RecipientEmail = recipientEmail?.Trim();
        Channel = channel;
        Category = category;
        Subject = subject?.Trim() ?? string.Empty;
        Body = body?.Trim() ?? string.Empty;
        TargetUrl = targetUrl?.Trim();
        Status = OutboxMessageStatus.Pending;
        RetryCount = 0;
        MaxRetries = maxRetries;
    }

    public void MarkSent(DateTimeOffset now)
    {
        Status = OutboxMessageStatus.Sent;
        SentAt = now;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        LastError = error?.Trim();
        Status = RetryCount >= MaxRetries ? OutboxMessageStatus.Failed : OutboxMessageStatus.Pending;
    }
}