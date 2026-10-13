using System.Collections.Generic;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Notifications;

/// <summary>
/// Mẫu thông báo (NotificationTemplate).
/// Hỗ trợ trường trộn động dạng {Key} cho cả tiêu đề và nội dung.
/// </summary>
public class NotificationTemplate : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public NotificationCategory Category { get; private set; }
    public string SubjectTemplate { get; private set; } = string.Empty;
    public string BodyTemplate { get; private set; } = string.Empty;
    public NotificationChannel SupportedChannels { get; private set; } = NotificationChannel.InApp | NotificationChannel.Email;
    public bool IsActive { get; private set; } = true;

    protected NotificationTemplate() { }

    public NotificationTemplate(
        string code,
        string name,
        NotificationCategory category,
        string subjectTemplate,
        string bodyTemplate,
        NotificationChannel supportedChannels = NotificationChannel.InApp | NotificationChannel.Email)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã mẫu thông báo không được để trống.", "ERR_NOTIF_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên mẫu thông báo không được để trống.", "ERR_NOTIF_NAME_EMPTY");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = category;
        SubjectTemplate = subjectTemplate?.Trim() ?? string.Empty;
        BodyTemplate = bodyTemplate?.Trim() ?? string.Empty;
        SupportedChannels = supportedChannels;
        IsActive = true;
    }

    /// <summary>
    /// Thay thế các trường trộn động {Key} bằng giá trị thực tế.
    /// </summary>
    public (string Subject, string Body) Render(Dictionary<string, string>? parameters)
    {
        string subject = SubjectTemplate;
        string body = BodyTemplate;

        if (parameters != null)
        {
            foreach (var kvp in parameters)
            {
                subject = subject.Replace("{" + kvp.Key + "}", kvp.Value);
                body = body.Replace("{" + kvp.Key + "}", kvp.Value);
            }
        }

        return (subject, body);
    }
}