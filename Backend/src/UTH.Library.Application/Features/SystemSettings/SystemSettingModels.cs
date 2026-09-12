using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.SystemSettings;

public sealed record SystemSettingDto(
    Guid Id,
    string Key,
    string Value,
    SettingType ValueType,
    string Scope,
    string? Description,
    bool IsSecret,
    Guid UpdatedByUserId,
    string? UpdatedByDisplayName,
    DateTime UpdatedAtUtc,
    Guid ConcurrencyToken);

public sealed record UpdateSettingCommand(
    string Key,
    string Value,
    Guid? ConcurrencyToken = null);

public sealed record BatchUpdateSettingsCommand(
    IReadOnlyList<UpdateSettingCommand> Settings);

public sealed record ExportedSettingDto(
    string Key,
    string Value,
    SettingType ValueType,
    string Scope,
    string? Description);

public sealed record ExportPackageResponse(
    string Version,
    DateTime ExportedAtUtc,
    string ExportedBy,
    string Checksum,
    IReadOnlyList<ExportedSettingDto> Settings);

public sealed record SettingDiffItemDto(
    string Key,
    string? OldValue,
    string? NewValue,
    SettingType ValueType,
    string Scope,
    string DiffType,
    string? Description,
    bool IsSecret,
    bool HasImpactWarning,
    string? ImpactWarning);

public sealed record PackageValidationResultDto(
    bool IsValid,
    string? ErrorMessage,
    string Version,
    string Checksum,
    int TotalSettings,
    IReadOnlyList<SettingDiffItemDto> Diffs);

public sealed record ConfirmImportCommand(
    string PackageJson,
    bool OverrideExisting = true);

public sealed record ConfirmImportResult(
    bool Success,
    string Message,
    int AddedCount,
    int UpdatedCount,
    int UnchangedCount,
    Guid PackageId);

public sealed record ConfigurationPackageSummaryDto(
    Guid Id,
    string Version,
    string Checksum,
    Guid CreatedByUserId,
    string? CreatedByDisplayName,
    DateTime CreatedAtUtc,
    int SettingsCount);
