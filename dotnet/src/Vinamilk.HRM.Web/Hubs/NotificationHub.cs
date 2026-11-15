using Microsoft.AspNetCore.SignalR;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.Hubs;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
        await base.OnConnectedAsync();
    }
}

public class SignalRNotificationDispatcher : IRealtimeNotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationDispatcher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task DispatchToUserAsync(
        int? recipientUserId,
        string title,
        string message,
        string notificationType,
        string? targetUrl,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            recipientUserId,
            title,
            message,
            notificationType,
            targetUrl = targetUrl ?? "/Workflows",
            timestamp = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")
        };

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", payload, cancellationToken);
    }

    public async Task BroadcastWorkflowUpdateAsync(
        int workflowInstanceId,
        string entityType,
        int entityId,
        string status,
        string summaryMessage,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            workflowInstanceId,
            entityType,
            entityId,
            status,
            summaryMessage,
            timestamp = DateTime.Now.ToString("HH:mm:ss")
        };

        await _hubContext.Clients.All.SendAsync("WorkflowStatusUpdated", payload, cancellationToken);
    }
}

