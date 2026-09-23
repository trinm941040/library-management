using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.Notifications;
using UTH.Library.Application.Features.Notifications.Adapters;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Notifications;

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Email outbox batch failed."); }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var settings = await scope.ServiceProvider.GetRequiredService<ISmtpSettingsProvider>().GetAsync(cancellationToken);
        if (!settings.Enabled) return;
        var sender = scope.ServiceProvider.GetServices<INotificationSenderAdapter>()
            .Single(value => value.Channel == NotificationChannel.Email);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var items = await repository.GetPendingBatchAsync(now, settings.BatchSize, cancellationToken);
        foreach (var item in items)
        {
            item.MarkProcessing();
            await repository.UpdateNotificationAsync(item, cancellationToken);
            var result = await sender.SendAsync(item, cancellationToken);
            if (result.Success) item.MarkSent(timeProvider.GetUtcNow().UtcDateTime);
            else if (item.AttemptCount <= settings.MaxRetryCount)
            {
                var delayMinutes = Math.Min(60, (int)Math.Pow(2, Math.Max(0, item.AttemptCount - 1)));
                item.ScheduleRetry(result.FailureReason ?? "SMTP tạm thời không khả dụng.",
                    timeProvider.GetUtcNow().UtcDateTime.AddMinutes(delayMinutes));
            }
            else item.MarkFailed(result.FailureReason ?? "Đã vượt số lần gửi lại SMTP.");
            await repository.UpdateNotificationAsync(item, cancellationToken);
        }
    }
}
