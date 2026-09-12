using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UTH.Library.Application.Abstractions;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.Infrastructure.Persistence;

internal sealed class AuditSaveChangesInterceptor(IRequestContext requestContext, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var db = eventData.Context;
        if (db is null) return ValueTask.FromResult(result);

        foreach (var session in db.ChangeTracker.Entries<RefreshTokenSession>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            session.Entity.RowVersion = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        }

        var entries = db.ChangeTracker.Entries()
            .Where(entry => entry.Entity is not AuditLog &&
                           (entry.Entity is Borrowing or Reservation or Violation or ApplicationRole or Permission or RolePermission or IdentityUserRole<Guid> or Book or CirculationPolicy or Member or Employee or ApplicationUser or SystemSetting or ConfigurationPackage) &&
                            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in entries)
        {
            var key = entry.Metadata.FindProperty("Id") is not null ? "Id" : "RoleId";
            var id = (Guid)(entry.Property(key).CurrentValue ?? Guid.Empty);
            var entityName = entry.Metadata.ClrType.Name;
            var action = $"{entityName.ToLowerInvariant()}.{entry.State.ToString().ToLowerInvariant()}";

            string? beforeJson = null;
            string? afterJson = null;

            if (entry.State == EntityState.Added)
            {
                afterJson = SerializeValues(entry, useOriginal: false);
            }
            else if (entry.State == EntityState.Deleted)
            {
                beforeJson = SerializeValues(entry, useOriginal: true);
            }
            else if (entry.State == EntityState.Modified)
            {
                beforeJson = SerializeValues(entry, useOriginal: true);
                afterJson = SerializeValues(entry, useOriginal: false);
            }

            db.Add(AuditLog.Create(
                requestContext.UserId,
                action,
                entityName,
                id,
                beforeJson,
                afterJson,
                now,
                requestContext.CorrelationId,
                requestContext.IpAddress));
        }

        return ValueTask.FromResult(result);
    }

    private static string? SerializeValues(EntityEntry entry, bool useOriginal)
    {
        try
        {
            var isSetting = entry.Entity is SystemSetting;
            var isSecretSetting = false;
            if (isSetting)
            {
                var typeProp = useOriginal ? entry.Property("ValueType").OriginalValue : entry.Property("ValueType").CurrentValue;
                if (typeProp is SettingType st && st == SettingType.Secret)
                {
                    isSecretSetting = true;
                }
            }

            var dict = new Dictionary<string, object?>();
            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.IsShadowProperty()) continue;
                var name = prop.Metadata.Name;

                if (name is "PasswordHash" or "SecurityStamp" or "ConcurrencyStamp" or "RowVersion")
                {
                    dict[name] = "[REDACTED]";
                    continue;
                }

                if (isSecretSetting && name == "Value")
                {
                    dict[name] = "[REDACTED]";
                    continue;
                }

                var val = useOriginal ? prop.OriginalValue : prop.CurrentValue;
                dict[name] = val;
            }
            return JsonSerializer.Serialize(dict);
        }
        catch
        {
            return null;
        }
    }
}
