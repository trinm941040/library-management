using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UTH.Library.Application.Abstractions;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace UTH.Library.Infrastructure.Persistence;

internal sealed partial class AuditSaveChangesInterceptor(
    IRequestContext requestContext,
    TimeProvider timeProvider,
    IOptions<AuditRetentionOptions> retentionOptions) : SaveChangesInterceptor
{
    private const string Redacted = "[REDACTED]";

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var db = eventData.Context;
        if (db is null) return ValueTask.FromResult(result);

        foreach (var session in db.ChangeTracker.Entries<RefreshTokenSession>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            session.Entity.RowVersion = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);

        EnrichExplicitLogs(db);
        AddAutomaticLogs(db);
        return ValueTask.FromResult(result);
    }

    private void EnrichExplicitLogs(DbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries<AuditLog>().Where(x => x.State == EntityState.Added))
        {
            entry.Property(x => x.Action).CurrentValue = NormalizeAction(entry.Entity.Action);
            entry.Property(x => x.EntityType).CurrentValue = NormalizeEntity(entry.Entity.EntityType);
            entry.Property(x => x.BeforeJson).CurrentValue = RedactJson(entry.Entity.BeforeJson);
            entry.Property(x => x.AfterJson).CurrentValue = RedactJson(entry.Entity.AfterJson);
            entry.Property(x => x.CorrelationId).CurrentValue = NullIfBlank(entry.Entity.CorrelationId) ?? NullIfBlank(requestContext.CorrelationId);
            entry.Property(x => x.IpAddress).CurrentValue = NullIfBlank(entry.Entity.IpAddress) ?? requestContext.IpAddress;
            entry.Property(x => x.RetainUntilUtc).CurrentValue =
                entry.Entity.CreatedAtUtc.AddDays(retentionOptions.Value.RetentionDays);
        }
    }

    private void AddAutomaticLogs(DbContext db)
    {
        if (string.IsNullOrWhiteSpace(requestContext.CorrelationId)) return;

        var existing = db.ChangeTracker.Entries<AuditLog>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => (x.Entity.EntityType, x.Entity.EntityId))
            .ToHashSet();

        var entries = db.ChangeTracker.Entries()
            .Where(ShouldAudit)
            .ToArray();

        foreach (var entry in entries)
        {
            var entityType = NormalizeEntity(entry.Metadata.ClrType.Name);
            var entityId = GetEntityId(entry);
            if (entityId == Guid.Empty || existing.Contains((entityType, entityId))) continue;

            var before = entry.State is EntityState.Modified or EntityState.Deleted
                ? SerializeValues(entry.OriginalValues)
                : null;
            var after = entry.State is EntityState.Added or EntityState.Modified
                ? SerializeValues(entry.CurrentValues)
                : null;
            var action = $"{ToKebabCase(entityType)}.{entry.State switch
            {
                EntityState.Added => "created",
                EntityState.Modified => "updated",
                EntityState.Deleted => "deleted",
                _ => "changed"
            }}";

            db.Add(AuditLog.Create(
                requestContext.UserId,
                action,
                entityType,
                entityId,
                before,
                after,
                timeProvider.GetUtcNow().UtcDateTime,
                requestContext.CorrelationId,
                requestContext.IpAddress,
                timeProvider.GetUtcNow().UtcDateTime.AddDays(retentionOptions.Value.RetentionDays)));
        }
    }

    private static bool ShouldAudit(EntityEntry entry) =>
        (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) &&
        entry.Entity is not AuditLog and not RefreshTokenSession and not TodoItem &&
        !entry.Metadata.IsOwned();

    private static Guid GetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey()?.Properties
            .FirstOrDefault(property => property.ClrType == typeof(Guid));
        if (key is null) return Guid.Empty;
        var value = entry.State == EntityState.Deleted
            ? entry.OriginalValues[key]
            : entry.CurrentValues[key];
        return value is Guid id ? id : Guid.Empty;
    }

    private static string SerializeValues(PropertyValues values)
    {
        var data = values.Properties.ToDictionary(
            property => property.Name,
            property => IsSensitive(property.Name) ? Redacted : values[property]);
        return JsonSerializer.Serialize(data);
    }

    internal static string? RedactJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var node = JsonNode.Parse(json);
            RedactNode(node);
            return node?.ToJsonString();
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { redacted = true });
        }
    }

    private static void RedactNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToArray())
            {
                if (IsSensitive(property.Key)) obj[property.Key] = Redacted;
                else RedactNode(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array) RedactNode(item);
        }
    }

    private static bool IsSensitive(string name) => SensitiveNameRegex().IsMatch(name);
    private static string NormalizeAction(string value) => value.Trim().ToLowerInvariant();
    private static string NormalizeEntity(string value) => value.Trim();
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string ToKebabCase(string value) => KebabBoundaryRegex().Replace(value, "$1-$2").ToLowerInvariant();

    [GeneratedRegex("password|token|secret|securitystamp|concurrencystamp|email|phone|address|dateofbirth|fullname|displayname|username|borrowername|reservername|contactname|contactinfo", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveNameRegex();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex KebabBoundaryRegex();
}
