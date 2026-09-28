namespace UTH.Library.Application.Features.Notifications;

public interface INotificationRealtimePublisher
{
    Task PublishAsync(NotificationDto notification, CancellationToken cancellationToken);
}
