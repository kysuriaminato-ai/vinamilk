namespace Vinamilk.HRM.Application.Common.Interfaces;

public record SentEmailRecordDto(
    DateTime SentAtUtc,
    string ToEmail,
    string RecipientName,
    string Subject,
    string DocumentTitle,
    string StatusBadge,
    bool IsDelivered);

public interface IRealtimeNotificationDispatcher
{
    Task DispatchToUserAsync(
        int? recipientUserId,
        string title,
        string message,
        string notificationType,
        string? targetUrl,
        CancellationToken cancellationToken = default);

    Task BroadcastWorkflowUpdateAsync(
        int workflowInstanceId,
        string entityType,
        int entityId,
        string status,
        string summaryMessage,
        CancellationToken cancellationToken = default);
}

public interface IEmailNotificationService
{
    Task SendWorkflowEmailAsync(
        string toEmail,
        string recipientName,
        string subject,
        string documentTitle,
        string initiatorName,
        string currentStepName,
        string statusBadge,
        string actionUrl,
        string? comment = null,
        CancellationToken cancellationToken = default);

    Task SendPayslipEmailWithAttachmentAsync(
        string toEmail,
        string recipientName,
        string subject,
        string periodTitle,
        string htmlSummary,
        byte[] pdfAttachment,
        string attachmentFileName,
        CancellationToken cancellationToken = default);

    IReadOnlyList<SentEmailRecordDto> GetRecentSentEmails();
}

