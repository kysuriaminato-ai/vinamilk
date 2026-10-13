using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Notifications;

/// <summary>
/// Tùy chọn nhận thông báo của người dùng theo danh mục và kênh truyền thông.
/// </summary>
public class NotificationPreference : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public NotificationCategory Category { get; private set; }
    public bool AllowInApp { get; private set; } = true;
    public bool AllowEmail { get; private set; } = true;
    public bool AllowSms { get; private set; } = false;

    protected NotificationPreference() { }

    public NotificationPreference(
        int employeeId,
        NotificationCategory category,
        bool allowInApp = true,
        bool allowEmail = true,
        bool allowSms = false)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_NOTIF_PREF_EMP_INVALID");

        EmployeeId = employeeId;
        Category = category;
        AllowInApp = allowInApp;
        AllowEmail = allowEmail;
        AllowSms = allowSms;
    }

    public void UpdatePreferences(bool allowInApp, bool allowEmail, bool allowSms)
    {
        AllowInApp = allowInApp;
        AllowEmail = allowEmail;
        AllowSms = allowSms;
    }
}