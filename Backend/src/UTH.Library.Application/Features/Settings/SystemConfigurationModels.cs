using System.Text.Json;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Settings;

public sealed record SystemSettingModel(
    string Key,
    string DisplayName,
    string Description,
    SettingType ValueType,
    SettingScope Scope,
    bool IsSecret,
    bool HasValue,
    JsonElement? Value,
    JsonElement? DefaultValue,
    Guid? UpdatedByUserId,
    DateTime? UpdatedAtUtc,
    Guid? ConcurrencyToken);

public sealed record UpdateSystemSettingCommand(JsonElement Value, Guid? ConcurrencyToken);

public sealed record ConfigurationExport(
    string FileName,
    string Content,
    string Checksum,
    string SchemaVersion);

public enum ConfigurationDifferenceKind
{
    Add,
    Change,
    Remove
}

public sealed record ConfigurationDifference(
    string Key,
    string DisplayName,
    SettingScope Scope,
    SettingType ValueType,
    ConfigurationDifferenceKind Kind,
    JsonElement? Before,
    JsonElement? After);

public sealed record ConfigurationImportPreview(
    string SchemaVersion,
    string Checksum,
    DateTime ExportedAtUtc,
    DateTime ExpiresAtUtc,
    IReadOnlyList<ConfigurationDifference> Differences,
    string ConfirmationToken);

public sealed record ConfigurationImportResult(
    Guid PackageId,
    string Checksum,
    int Added,
    int Changed,
    int Removed,
    DateTime AppliedAtUtc);

public interface ISystemConfigurationService
{
    Task<IReadOnlyList<SystemSettingModel>> GetSettingsAsync(CancellationToken cancellationToken);
    Task<SystemSettingModel> UpdateSettingAsync(
        string key,
        UpdateSystemSettingCommand command,
        Guid actorUserId,
        CancellationToken cancellationToken);
    Task<ConfigurationExport> ExportAsync(Guid actorUserId, CancellationToken cancellationToken);
    Task<ConfigurationImportPreview> ValidateImportAsync(
        string fileName,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken);
    Task<ConfigurationImportResult> ConfirmImportAsync(
        string confirmationToken,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
