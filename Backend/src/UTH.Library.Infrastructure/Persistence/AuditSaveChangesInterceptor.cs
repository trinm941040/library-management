using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;
using UTH.Library.Application.Abstractions;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace UTH.Library.Infrastructure.Persistence;

internal sealed class AuditSaveChangesInterceptor(IRequestContext requestContext, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var db = eventData.Context;
        if (db is null) return ValueTask.FromResult(result);
        foreach (var session in db.ChangeTracker.Entries<RefreshTokenSession>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            session.Entity.RowVersion = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var entries = db.ChangeTracker.Entries()
            .Where(entry => entry.Entity is Borrowing or Reservation or Violation or ApplicationRole or Permission or RolePermission or IdentityUserRole<Guid> && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
        foreach (var entry in entries)
        {
            var key = entry.Metadata.FindProperty("Id") is not null ? "Id" : "RoleId";
            var id = (Guid)(entry.Property(key).CurrentValue ?? Guid.Empty);
            var action = $"{entry.Metadata.ClrType.Name.ToLowerInvariant()}.{entry.State.ToString().ToLowerInvariant()}";
            var state = JsonSerializer.Serialize(new { EntryState = entry.State.ToString(), Id = id });
            db.Add(AuditLog.Create(requestContext.UserId, action, entry.Metadata.ClrType.Name, id, null, state, timeProvider.GetUtcNow().UtcDateTime, requestContext.CorrelationId));
        }
        return ValueTask.FromResult(result);
    }
}
