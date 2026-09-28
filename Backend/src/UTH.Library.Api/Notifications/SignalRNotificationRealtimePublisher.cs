using Microsoft.AspNetCore.SignalR;
using UTH.Library.Application.Features.Notifications;

namespace UTH.Library.Api.Notifications;

public sealed class SignalRNotificationRealtimePublisher(
    IHubContext<NotificationHub> hubContext) : INotificationRealtimePublisher
{
    public Task PublishAsync(NotificationDto notification, CancellationToken cancellationToken) =>
        hubContext.Clients
            .Group(NotificationHub.UserGroup(notification.RecipientId))
            .SendAsync("NotificationReceived", notification, cancellationToken);
}
