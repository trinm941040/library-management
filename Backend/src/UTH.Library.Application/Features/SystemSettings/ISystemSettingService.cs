namespace UTH.Library.Application.Features.SystemSettings;

public interface ISystemSettingService
{
    Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(string? scope = null, CancellationToken cancellationToken = default);
    Task<SystemSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<SystemSettingDto> UpdateAsync(UpdateSettingCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemSettingDto>> BatchUpdateAsync(BatchUpdateSettingsCommand command, CancellationToken cancellationToken = default);
    Task<ExportPackageResponse> ExportPackageAsync(CancellationToken cancellationToken = default);
    Task<PackageValidationResultDto> ValidatePackageAsync(string packageJson, CancellationToken cancellationToken = default);
    Task<ConfirmImportResult> ImportPackageAsync(ConfirmImportCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigurationPackageSummaryDto>> GetPackageHistoryAsync(CancellationToken cancellationToken = default);
    Task<int> ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}
