namespace UTH.Library.Domain.Entities;

public sealed class ConfigurationPackage
{
    private ConfigurationPackage() { }

    public Guid Id { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public string Data { get; private set; } = "{}";
    public string Checksum { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static ConfigurationPackage Create(
        string version,
        string data,
        string checksum,
        Guid actorUserId,
        DateTime createdAtUtc) => new()
        {
            Id = Guid.NewGuid(),
            Version = version,
            Data = data,
            Checksum = checksum,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = createdAtUtc.Kind == DateTimeKind.Utc
                ? createdAtUtc
                : createdAtUtc.ToUniversalTime()
        };
}
