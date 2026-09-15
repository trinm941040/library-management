namespace UTH.Library.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
        Action = string.Empty;
        EntityType = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime RetainUntilUtc { get; private set; }

    public static AuditLog Create(
        Guid? actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        DateTime createdAtUtc,
        string? correlationId = null,
        string? ipAddress = null,
        DateTime? retainUntilUtc = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = Normalize(action, "unknown.action", 100),
            EntityType = Normalize(entityType, "Unknown", 100),
            EntityId = entityId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            CreatedAtUtc = AsUtc(createdAtUtc),
            CorrelationId = NullIfBlank(correlationId, 100),
            IpAddress = NullIfBlank(ipAddress, 45),
            RetainUntilUtc = AsUtc(retainUntilUtc ?? createdAtUtc.AddDays(365))
        };

    private static string Normalize(string? value, string fallback, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized[..Math.Min(normalized.Length, maxLength)];
    }

    private static string? NullIfBlank(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
