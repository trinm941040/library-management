using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace UTH.Library.Infrastructure.Persistence;

internal sealed class AuditRetentionOptions
{
    public const string SectionName = "Audit";
    public int RetentionDays { get; init; } = 365;
}

internal sealed class AuditRetentionService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24), timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
            var now = timeProvider.GetUtcNow().UtcDateTime;
            await db.AuditLogs.Where(log => log.RetainUntilUtc <= now).ExecuteDeleteAsync(stoppingToken);
        }
    }
}
