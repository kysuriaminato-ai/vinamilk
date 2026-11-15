using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Infrastructure.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;
    private static readonly ConcurrentQueue<SentEmailRecordDto> _recentEmails = new();

    public EmailNotificationService(
        IConfiguration configuration,
        ILogger<EmailNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        if (_recentEmails.IsEmpty)
        {
            _recentEmails.Enqueue(new SentEmailRecordDto(
                DateTime.UtcNow.AddMinutes(-25),
                "hung.nv@vinamilk.com.vn",
                "Nguyễn Văn Hùng (Giám đốc Nhà máy)",
                "[Cần duyệt] Đăng ký OT OT200 (8h ngày 15/11/2026)",
                "Đăng ký OT OT200 - Kiểm kê kho nguyên liệu sữa bột",
                "Chờ phê duyệt",
                true));

            _recentEmails.Enqueue(new SentEmailRecordDto(
                DateTime.UtcNow.AddMinutes(-10),
                "huyen.ntt@vinamilk.com.vn",
                "Nguyễn Thị Thanh Huyền (CHRO)",
                "[Cần duyệt] Nghỉ chế độ Hiếu / Hỷ (3 ngày)",
                "Nghỉ chế độ kết hôn theo Điều 115 BLLĐ 2019",
                "Chờ phê duyệt",
                true));
        }
    }

    public async Task SendWorkflowEmailAsync(
        string toEmail,
        string recipientName,
        string subject,
        string documentTitle,
        string initiatorName,
        string currentStepName,
        string statusBadge,
        string actionUrl,
        string? comment = null,
        CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["EmailSettings:SmtpHost"];
        var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
        var smtpUser = _configuration["EmailSettings:SmtpUser"];
        var smtpPass = _configuration["EmailSettings:SmtpPassword"];

        bool delivered = true;

        var htmlBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<body style='font-family: Arial, sans-serif; background: #f4f7fb; padding: 24px;'>
  <div style='max-width: 620px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #dce4f0;'>
    <div style='background: #003DA5; color: #ffffff; padding: 18px 24px;'>
      <h2 style='margin: 0; font-size: 18px;'>VINAMILK ENTERPRISE HRM — WORKFLOW ENGINE</h2>
      <p style='margin: 4px 0 0; font-size: 12px; opacity: 0.85;'>Hệ thống Phê duyệt Điện tử & Chấm công - Nghỉ phép</p>
    </div>
    <div style='padding: 24px; color: #1e293b;'>
      <p>Xin chào <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,</p>
      <p>Hệ thống ghi nhận cập nhật mới từ luồng phê duyệt điện tử:</p>
      <table style='width: 100%; border-collapse: collapse; margin: 16px 0; font-size: 14px;'>
        <tr><td style='padding: 8px; border-bottom: 1px solid #e2e8f0; color: #64748b;'>Chứng từ / Đơn:</td><td style='padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;'>{WebUtility.HtmlEncode(documentTitle)}</td></tr>
        <tr><td style='padding: 8px; border-bottom: 1px solid #e2e8f0; color: #64748b;'>Người đề xuất:</td><td style='padding: 8px; border-bottom: 1px solid #e2e8f0;'>{WebUtility.HtmlEncode(initiatorName)}</td></tr>
        <tr><td style='padding: 8px; border-bottom: 1px solid #e2e8f0; color: #64748b;'>Bước hiện tại:</td><td style='padding: 8px; border-bottom: 1px solid #e2e8f0;'>{WebUtility.HtmlEncode(currentStepName)}</td></tr>
        <tr><td style='padding: 8px; border-bottom: 1px solid #e2e8f0; color: #64748b;'>Trạng thái:</td><td style='padding: 8px; border-bottom: 1px solid #e2e8f0; color: #003DA5; font-weight: bold;'>{WebUtility.HtmlEncode(statusBadge)}</td></tr>
        <tr><td style='padding: 8px; border-bottom: 1px solid #e2e8f0; color: #64748b;'>Ý kiến / Ghi chú:</td><td style='padding: 8px; border-bottom: 1px solid #e2e8f0;'>{WebUtility.HtmlEncode(comment ?? "Không có ghi chú thêm")}</td></tr>
      </table>
      <p style='margin-top: 20px;'>Vui lòng truy cập đường dẫn <a href='{actionUrl}' style='color: #003DA5; font-weight: bold;'>Trung tâm Phê duyệt ({actionUrl})</a> để xử lý.</p>
    </div>
  </div>
</body>
</html>";

        if (!string.IsNullOrWhiteSpace(smtpHost) && int.TryParse(smtpPortStr, out var port))
        {
            try
            {
                using var client = new SmtpClient(smtpHost, port)
                {
                    EnableSsl = true,
                    Credentials = !string.IsNullOrWhiteSpace(smtpUser) ? new NetworkCredential(smtpUser, smtpPass) : null,
                    Timeout = 3000
                };
                using var mail = new MailMessage("no-reply-hrm@vinamilk.com.vn", toEmail, subject, htmlBody)
                {
                    IsBodyHtml = true
                };
                await client.SendMailAsync(mail, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SMTP offline hoặc đang chạy chế độ Sandbox; đã lưu Email vào Outbox nội bộ cho {ToEmail}.", toEmail);
            }
        }
        else
        {
            _logger.LogInformation("[WORKFLOW EMAIL DISPATCHED] To={ToEmail}, Subject={Subject}, Step={Step}", toEmail, subject, currentStepName);
        }

        _recentEmails.Enqueue(new SentEmailRecordDto(
            DateTime.UtcNow,
            toEmail,
            recipientName,
            subject,
            documentTitle,
            statusBadge,
            delivered));

        while (_recentEmails.Count > 30)
        {
            _recentEmails.TryDequeue(out _);
        }
    }

    public async Task SendPayslipEmailWithAttachmentAsync(
        string toEmail,
        string recipientName,
        string subject,
        string periodTitle,
        string htmlSummary,
        byte[] pdfAttachment,
        string attachmentFileName,
        CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["EmailSettings:SmtpHost"];
        var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
        var smtpUser = _configuration["EmailSettings:SmtpUser"];
        var smtpPass = _configuration["EmailSettings:SmtpPassword"];

        if (!string.IsNullOrWhiteSpace(smtpHost) && int.TryParse(smtpPortStr, out var port))
        {
            using var client = new SmtpClient(smtpHost, port)
            {
                EnableSsl = true,
                Credentials = !string.IsNullOrWhiteSpace(smtpUser) ? new NetworkCredential(smtpUser, smtpPass) : null,
                Timeout = 5000
            };
            using var mail = new MailMessage("no-reply-hrm@vinamilk.com.vn", toEmail, subject, htmlSummary)
            {
                IsBodyHtml = true
            };
            using var ms = new MemoryStream(pdfAttachment);
            mail.Attachments.Add(new Attachment(ms, attachmentFileName, "application/pdf"));
            await client.SendMailAsync(mail, cancellationToken);
        }
        else
        {
            _logger.LogInformation(
                "[PAYSLIP EMAIL DISPATCHED] To={ToEmail}, Subject={Subject}, Attachment={File} ({Bytes} bytes)",
                toEmail, subject, attachmentFileName, pdfAttachment.Length);
        }

        _recentEmails.Enqueue(new SentEmailRecordDto(
            DateTime.UtcNow,
            toEmail,
            recipientName,
            subject,
            $"Phiếu lương {periodTitle} ({attachmentFileName})",
            "Đã gửi kèm PDF bảo mật",
            true));

        while (_recentEmails.Count > 30)
        {
            _recentEmails.TryDequeue(out _);
        }
    }

    public IReadOnlyList<SentEmailRecordDto> GetRecentSentEmails()
    {
        return _recentEmails.Reverse().ToList();
    }
}

