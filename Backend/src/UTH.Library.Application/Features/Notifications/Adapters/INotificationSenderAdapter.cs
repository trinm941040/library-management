using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Notifications.Adapters;

public interface INotificationSenderAdapter
{
    NotificationChannel Channel { get; }
    Task<NotificationSendResult> SendAsync(Notification notification, CancellationToken cancellationToken);
}

public sealed record NotificationSendResult(bool Success, string? FailureReason = null);
