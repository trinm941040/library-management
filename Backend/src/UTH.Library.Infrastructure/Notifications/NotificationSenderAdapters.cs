using Microsoft.Extensions.Logging;
using UTH.Library.Application.Features.Notifications;
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

public sealed class EmailNotificationSenderAdapter(
    ISmtpSettingsProvider settingsProvider,
    ILogger<EmailNotificationSenderAdapter> logger) : INotificationSenderAdapter
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<NotificationSendResult> SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            var settings = await settingsProvider.GetAsync(cancellationToken);
            if (!settings.Enabled) return new NotificationSendResult(false, "Kênh email đang bị vô hiệu hóa.");
            SmtpAdministrationService.Validate(settings);
            using var message = SmtpMessageFactory.Create(settings, notification.Destination,
                notification.Subject ?? string.Empty, notification.Body, notification.Body);
            using var client = SmtpMessageFactory.CreateClient(settings);
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("SMTP delivered notification {NotificationId}.", notification.Id);
            return new NotificationSendResult(true);
        }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or FormatException or InvalidOperationException)
        {
            logger.LogWarning("SMTP delivery failed for notification {NotificationId}.", notification.Id);
            return new NotificationSendResult(false, "SMTP tạm thời không khả dụng hoặc cấu hình không hợp lệ.");
        }
    }
}
