using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using UTH.Library.Application.Abstractions;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence;

internal sealed class AuditSaveChangesInterceptor(IRequestContext requestContext, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var db = eventData.Context;
        if (db is null) return ValueTask.FromResult(result);
        var entries = db.ChangeTracker.Entries()
            .Where(entry => entry.Entity is Borrowing or Reservation or Violation && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
        foreach (var entry in entries)
        {
            var id = (Guid)(entry.Property("Id").CurrentValue ?? Guid.Empty);
            var action = $"{entry.Metadata.ClrType.Name.ToLowerInvariant()}.{entry.State.ToString().ToLowerInvariant()}";
            var state = JsonSerializer.Serialize(new { EntryState = entry.State.ToString(), Id = id });
            db.Add(AuditLog.Create(requestContext.UserId, action, entry.Metadata.ClrType.Name, id, null, state, timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId));
        }
        return ValueTask.FromResult(result);
    }
}
