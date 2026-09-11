using System.Text.RegularExpressions;

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

    public static AuditLog Create(
        Guid? actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        DateTime createdAtUtc,
        string? correlationId = null,
        string? ipAddress = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action.Trim().ToLowerInvariant(),
            EntityType = entityType.Trim(),
            EntityId = entityId,
            BeforeJson = RedactJson(beforeJson),
            AfterJson = RedactJson(afterJson),
            CreatedAtUtc = createdAtUtc,
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim(),
            IpAddress = NormalizeIp(ipAddress)
        };

    public static string? RedactJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        // Redact sensitive keys in JSON (passwords, tokens, secrets, private keys, hashes)
        const string pattern = @"\""(password|passwordhash|token|refreshtoken|secret|securitystamp|concurrencystamp|privatekey|jwt)\""\s*:\s*(\""[^\""]*\""|null|[0-9]+)";
        return Regex.Replace(json, pattern, "\"$1\": \"[REDACTED]\"", RegexOptions.IgnoreCase);
    }

    private static string? NormalizeIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        var trimmed = ip.Trim();
        if (trimmed.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[7..];
        return trimmed.Length > 64 ? trimmed[..64] : trimmed;
    }
}
