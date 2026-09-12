namespace UTH.Library.Domain.Entities;

public sealed class ConfigurationPackage
{
    public Guid Id { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public string Data { get; private set; } = "{}";
    public string Checksum { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ConfigurationPackage() { }

    public static ConfigurationPackage Create(
        string version,
        string data,
        string checksum,
        Guid createdByUserId,
        DateTime createdAtUtc)
    {
        return new ConfigurationPackage
        {
            Id = Guid.NewGuid(),
            Version = version,
            Data = data,
            Checksum = checksum,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = createdAtUtc
        };
    }
}
