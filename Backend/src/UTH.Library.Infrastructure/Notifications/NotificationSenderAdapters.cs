using Microsoft.Extensions.Logging;
using UTH.Library.Application.Features.Notifications.Adapters;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Notifications;

public sealed class InAppNotificationSenderAdapter(ILogger<InAppNotificationSenderAdapter> logger) : INotificationSenderAdapter
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public Task<NotificationSendResult> SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[InApp Notification] Delivered to RecipientId: {RecipientId}, Subject: {Subject}",
            notification.RecipientId,
            notification.Subject);

        return Task.FromResult(new NotificationSendResult(true));
    }
}

public sealed class EmailNotificationSenderAdapter(ILogger<EmailNotificationSenderAdapter> logger) : INotificationSenderAdapter
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public Task<NotificationSendResult> SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        // Safe simulated delivery for local testing
        if (notification.Destination.Contains("fail@", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("[Email Notification] Simulated delivery failure to: {Destination}", notification.Destination);
            return Task.FromResult(new NotificationSendResult(false, "Simulated SMTP server connection timeout."));
        }

        logger.LogInformation(
            "[Email Notification] Sent email to: {Destination}, Subject: {Subject}",
            notification.Destination,
            notification.Subject);

        return Task.FromResult(new NotificationSendResult(true));
    }
}

public sealed class SmsNotificationSenderAdapter(ILogger<SmsNotificationSenderAdapter> logger) : INotificationSenderAdapter
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public Task<NotificationSendResult> SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        // Safe simulated delivery for local testing
        if (notification.Destination == "0000000000")
        {
            logger.LogWarning("[SMS Notification] Simulated delivery failure to: {Destination}", notification.Destination);
            return Task.FromResult(new NotificationSendResult(false, "Simulated SMS Gateway rejected phone number."));
        }

        logger.LogInformation(
            "[SMS Notification] Sent SMS to: {Destination}, Body: {Body}",
            notification.Destination,
            notification.Body);

        return Task.FromResult(new NotificationSendResult(true));
    }
}
