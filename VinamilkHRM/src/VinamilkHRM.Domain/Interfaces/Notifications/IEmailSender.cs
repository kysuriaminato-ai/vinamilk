using System.Threading;
using System.Threading.Tasks;

namespace VinamilkHRM.Domain.Interfaces.Notifications;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string bodyHtml, CancellationToken ct = default);
}