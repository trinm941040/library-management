using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Api.Contracts.Settings;

public sealed record SystemSettingResponse(
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

public sealed record UpdateSystemSettingRequest(
    JsonElement Value,
    Guid? ConcurrencyToken);

public sealed record ConfigurationDifferenceResponse(
    string Key,
    string DisplayName,
    SettingScope Scope,
    SettingType ValueType,
    string Kind,
    JsonElement? Before,
    JsonElement? After);

public sealed record ConfigurationImportPreviewResponse(
    string SchemaVersion,
    string Checksum,
    DateTime ExportedAtUtc,
    DateTime ExpiresAtUtc,
    IReadOnlyList<ConfigurationDifferenceResponse> Differences,
    string ConfirmationToken);

public sealed record ConfirmConfigurationImportRequest(
    [Required, StringLength(2_000_000, MinimumLength = 1)] string ConfirmationToken);

public sealed record ConfigurationImportResultResponse(
    Guid PackageId,
    string Checksum,
    int Added,
    int Changed,
    int Removed,
    DateTime AppliedAtUtc);
