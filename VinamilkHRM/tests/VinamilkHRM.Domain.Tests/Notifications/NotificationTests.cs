using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Entities.Notifications;
using VinamilkHRM.Domain.Enums;
using Xunit;

namespace VinamilkHRM.Domain.Tests.Notifications;

public class NotificationTests
{
    [Fact]
    public void NotificationTemplate_Render_ShouldReplaceTokensCorrectly()
    {
        // ARRANGE: Tạo mẫu thông báo phê duyệt quy trình với các trường trộn
        var template = new NotificationTemplate(
            code: "NOTIF_LEAVE_APPROVED",
            name: "Thông báo Đơn nghỉ phép đã được phê duyệt",
            category: NotificationCategory.Workflow,
            subjectTemplate: "Đơn xin nghỉ phép {LeaveType} của bạn đã được duyệt",
            bodyTemplate: "Chào {EmployeeName}, đơn nghỉ phép {TotalDays} ngày (từ {StartDate} đến {EndDate}) của bạn đã được {ApproverName} phê duyệt.");

        var parameters = new Dictionary<string, string>
        {
            { "LeaveType", "Phép năm" },
            { "EmployeeName", "Nguyễn Văn A" },
            { "TotalDays", "5" },
            { "StartDate", "15/10/2026" },
            { "EndDate", "20/10/2026" },
            { "ApproverName", "Trần Thị Trưởng Đơn Vị" }
        };

        // ACT: Điền các trường trộn vào mẫu thông báo
        var (subject, body) = template.Render(parameters);

        // ASSERT: Toàn bộ token được thay thế chính xác
        Assert.Equal("Đơn xin nghỉ phép Phép năm của bạn đã được duyệt", subject);
        Assert.Contains("Nguyễn Văn A", body);
        Assert.Contains("5 ngày", body);
        Assert.Contains("Trần Thị Trưởng Đơn Vị", body);
        Assert.DoesNotContain("{", body);
        Assert.DoesNotContain("}", body);
    }

    [Fact]
    public void NotificationPreference_ShouldAllowUserToToggleChannels()
    {
        // ARRANGE & ACT: Người dùng tùy chỉnh chỉ nhận thông báo trong ứng dụng, tắt email
        var pref = new NotificationPreference(
            employeeId: 101,
            category: NotificationCategory.Workflow,
            allowInApp: true,
            allowEmail: false,
            allowSms: false);

        // ASSERT
        Assert.True(pref.AllowInApp);
        Assert.False(pref.AllowEmail);
        Assert.False(pref.AllowSms);

        // Cập nhật lại tùy chọn
        pref.UpdatePreferences(allowInApp: true, allowEmail: true, allowSms: true);
        Assert.True(pref.AllowEmail);
        Assert.True(pref.AllowSms);
    }

    [Fact]
    public void NotificationOutboxMessage_MarkSentAndFailed_ShouldManageRetryLifecycle()
    {
        // ARRANGE: Bản ghi Transactional Outbox
        var outbox = new NotificationOutboxMessage(
            recipientEmployeeId: 101,
            recipientEmail: "emp101@vinamilk.com.vn",
            channel: NotificationChannel.Email,
            category: NotificationCategory.Workflow,
            subject: "Thông báo duyệt đơn",
            body: "Nội dung thông báo",
            maxRetries: 3);

        Assert.Equal(OutboxMessageStatus.Pending, outbox.Status);
        Assert.Equal(0, outbox.RetryCount);

        // Lần thử 1 thất bại do lỗi kết nối SMTP
        outbox.MarkFailed("SMTP Connection Timeout");
        Assert.Equal(1, outbox.RetryCount);
        Assert.Equal(OutboxMessageStatus.Pending, outbox.Status); // Vẫn ở Pending để retry

        // Lần thử 2 thất bại
        outbox.MarkFailed("SMTP Auth Failure");
        Assert.Equal(2, outbox.RetryCount);
        Assert.Equal(OutboxMessageStatus.Pending, outbox.Status);

        // Lần thử 3 thất bại (vượt quá MaxRetries = 3)
        outbox.MarkFailed("Fatal Mailbox Unavailable");
        Assert.Equal(3, outbox.RetryCount);
        Assert.Equal(OutboxMessageStatus.Failed, outbox.Status);
        Assert.Equal("Fatal Mailbox Unavailable", outbox.LastError);

        // Kiểm tra trường hợp gửi thành công
        var successOutbox = new NotificationOutboxMessage(
            recipientEmployeeId: 102,
            recipientEmail: "emp102@vinamilk.com.vn",
            channel: NotificationChannel.InApp,
            category: NotificationCategory.Workflow,
            subject: "Thành công",
            body: "Nội dung");

        var sentTime = DateTimeOffset.UtcNow;
        successOutbox.MarkSent(sentTime);

        Assert.Equal(OutboxMessageStatus.Sent, successOutbox.Status);
        Assert.Equal(sentTime, successOutbox.SentAt);
        Assert.Null(successOutbox.LastError);
    }

    [Fact]
    public void InAppNotification_MarkAsRead_ShouldUpdateReadTimestamp()
    {
        var notif = new InAppNotification(
            recipientEmployeeId: 105,
            title: "Quyết định tăng lương",
            message: "Mức lương mới của bạn đã có hiệu lực từ kỳ này.",
            category: NotificationCategory.Payroll,
            targetUrl: "/payroll/my-payslip");

        Assert.False(notif.IsRead);
        Assert.Null(notif.ReadAt);

        var now = DateTimeOffset.UtcNow;
        notif.MarkAsRead(now);

        Assert.True(notif.IsRead);
        Assert.Equal(now, notif.ReadAt);
    }
}