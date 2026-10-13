using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Domain.Interfaces.Notifications;

namespace VinamilkHRM.Infrastructure.Services.Notifications;

/// <summary>
/// Dịch vụ gửi thư điện tử qua giao thức SMTP (hỗ trợ MailHog, Google Workspace, Microsoft 365).
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml, CancellationToken ct = default)
    {
        string host = _configuration["Smtp:Host"] ?? "localhost";
        int port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 25;
        string username = _configuration["Smtp:Username"] ?? "";
        string password = _configuration["Smtp:Password"] ?? "";
        bool enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) && ssl;
        string fromEmail = _configuration["Smtp:FromEmail"] ?? "no-reply@vinamilk.com.vn";
        string fromName = _configuration["Smtp:FromName"] ?? "Vinamilk HRM System";

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail, fromName);
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = bodyHtml;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(host, port);
            client.EnableSsl = enableSsl;
            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            _logger.LogInformation("Đang gửi email thông báo tới {ToEmail} qua SMTP {Host}:{Port}...", toEmail, host, port);
            
            // Nếu là chế độ test / localhost mà không kết nối được, bắt exception để ghi log
            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Gửi email thông báo tới {ToEmail} thành công.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi email tới {ToEmail} qua SMTP: {Message}. Tin nhắn đã được lưu vào Outbox.", toEmail, ex.Message);
            // Ném exception để Outbox worker ghi nhận retry count
            throw;
        }
    }
}