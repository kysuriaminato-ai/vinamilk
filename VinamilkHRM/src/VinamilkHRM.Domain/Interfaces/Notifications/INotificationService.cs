using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VinamilkHRM.Domain.Entities.Notifications;

namespace VinamilkHRM.Domain.Interfaces.Notifications;

public interface INotificationService
{
    Task QueueNotificationAsync(
        int recipientId,
        string templateCode,
        Dictionary<string, string> parameters,
        string? targetUrl = null,
        CancellationToken ct = default);

    Task<List<InAppNotification>> GetUserNotificationsAsync(
        int employeeId,
        int limit = 20,
        CancellationToken ct = default);

    Task MarkNotificationAsReadAsync(
        int notificationId,
        int employeeId,
        CancellationToken ct = default);

    Task<int> ProcessOutboxMessagesAsync(
        int batchSize = 50,
        CancellationToken ct = default);
}