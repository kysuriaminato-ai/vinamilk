using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Notifications;

/// <summary>
/// Thông báo trong ứng dụng (In-App Notification) hiển thị ở chuông thông báo.
/// </summary>
public class InAppNotification : BaseAuditableEntity
{
    public int RecipientEmployeeId { get; private set; }
    public virtual Employee? RecipientEmployee { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationCategory Category { get; private set; }
    public string? TargetUrl { get; private set; }

    public bool IsRead { get; private set; } = false;
    public DateTimeOffset? ReadAt { get; private set; }

    protected InAppNotification() { }

    public InAppNotification(
        int recipientEmployeeId,
        string title,
        string message,
        NotificationCategory category,
        string? targetUrl = null)
    {
        if (recipientEmployeeId <= 0)
            throw new DomainException("Mã nhân viên nhận thông báo không hợp lệ.", "ERR_NOTIF_RECIPIENT_INVALID");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Tiêu đề thông báo không được để trống.", "ERR_NOTIF_TITLE_EMPTY");

        RecipientEmployeeId = recipientEmployeeId;
        Title = title.Trim();
        Message = message?.Trim() ?? string.Empty;
        Category = category;
        TargetUrl = targetUrl?.Trim();
        IsRead = false;
    }

    public void MarkAsRead(DateTimeOffset now)
    {
        IsRead = true;
        ReadAt = now;
    }
}