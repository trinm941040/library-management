namespace UTH.Library.Domain.Entities;
public sealed class ConfigurationPackage { public Guid Id { get; private set; } public string Version { get; private set; } = string.Empty; public string Data { get; private set; } = "{}"; public string Checksum { get; private set; } = string.Empty; public Guid CreatedByUserId { get; private set; } public DateTime CreatedAtUtc { get; private set; } }
