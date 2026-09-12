using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Features.SystemSettings;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Services;

public sealed class SystemSettingService(
    LibraryDbContext dbContext,
    IRequestContext requestContext,
    TimeProvider timeProvider) : ISystemSettingService
{
    private const string MaskedSecret = "********";

    private sealed record SettingDefinition(
        string Key,
        SettingType ValueType,
        string Scope,
        string DefaultValue,
        string Description,
        bool IsSecret = false);

    private static readonly IReadOnlyDictionary<string, SettingDefinition> Registry = new Dictionary<string, SettingDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["circulation.max_days"] = new("circulation.max_days", SettingType.Number, "circulation", "14", "Thời hạn mượn sách mặc định (ngày)"),
        ["circulation.max_books"] = new("circulation.max_books", SettingType.Number, "circulation", "5", "Số sách mượn tối đa cho mỗi độc giả"),
        ["circulation.max_renewals"] = new("circulation.max_renewals", SettingType.Number, "circulation", "2", "Số lần gia hạn tối đa cho một lượt mượn"),
        ["circulation.hold_days"] = new("circulation.hold_days", SettingType.Number, "circulation", "3", "Thời hạn giữ sách đặt trước (ngày)"),
        ["circulation.fine_per_day"] = new("circulation.fine_per_day", SettingType.Number, "circulation", "5000", "Đơn giá phạt quá hạn mỗi ngày (VNĐ)"),
        ["circulation.block_overdue"] = new("circulation.block_overdue", SettingType.Boolean, "circulation", "true", "Tự động chặn mượn khi có sách quá hạn"),
        ["circulation.lost_penalty_ratio"] = new("circulation.lost_penalty_ratio", SettingType.Number, "circulation", "200", "Tỷ lệ đền bù làm mất sách (%)"),

        ["library.name"] = new("library.name", SettingType.String, "library", "Northstar Library", "Tên thư viện"),
        ["library.email"] = new("library.email", SettingType.String, "library", "contact@northstarlibrary.com", "Email liên hệ chính thức"),
        ["library.address"] = new("library.address", SettingType.String, "library", "123 Đường Điện Biên Phủ, Quận Bình Thạnh, TP.HCM", "Địa chỉ thư viện"),
        ["library.phone"] = new("library.phone", SettingType.String, "library", "028 3812 3456", "Số điện thoại hotline"),
        ["library.hours"] = new("library.hours", SettingType.String, "library", "07:30 - 20:30 (Thứ 2 - Thứ 7)", "Giờ mở cửa"),

        ["notification.smtp_host"] = new("notification.smtp_host", SettingType.String, "notification", "smtp.gmail.com", "Máy chủ SMTP gửi mail"),
        ["notification.smtp_port"] = new("notification.smtp_port", SettingType.Number, "notification", "587", "Cổng máy chủ SMTP"),
        ["notification.smtp_user"] = new("notification.smtp_user", SettingType.String, "notification", "notification@northstarlibrary.com", "Tài khoản gửi email"),
        ["notification.smtp_password"] = new("notification.smtp_password", SettingType.Secret, "notification", "", "Mật khẩu ứng dụng SMTP", IsSecret: true),

        ["system.auto_backup"] = new("system.auto_backup", SettingType.Boolean, "system", "true", "Tự động sao lưu định kỳ"),
        ["system.backup_retention_days"] = new("system.backup_retention_days", SettingType.Number, "system", "30", "Thời gian lưu trữ bản sao lưu (ngày)")
    };

    public async Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(string? scope = null, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSettingsSeededAsync(cancellationToken);

        var query = dbContext.SystemSettings.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(scope))
        {
            var trimmedScope = scope.Trim().ToLowerInvariant();
            query = query.Where(s => s.Key.ToLower().StartsWith(trimmedScope + "."));
        }

        var entities = await query.OrderBy(s => s.Key).ToListAsync(cancellationToken);

        // Fetch user display names
        var userIds = entities.Select(e => e.UpdatedByUserId).Distinct().ToList();
        var userDict = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? u.Email, cancellationToken);

        return entities.Select(e => MapToDto(e, userDict)).ToList();
    }

    public async Task<SystemSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var trimmedKey = key.Trim();
        var entity = await dbContext.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key.ToLower() == trimmedKey.ToLower(), cancellationToken);

        if (entity is null) return null;

        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == entity.UpdatedByUserId, cancellationToken);

        var userDict = user is not null ? new Dictionary<Guid, string?> { [user.Id] = user.DisplayName ?? user.Email } : new Dictionary<Guid, string?>();
        return MapToDto(entity, userDict);
    }

    public async Task<SystemSettingDto> UpdateAsync(UpdateSettingCommand command, CancellationToken cancellationToken = default)
    {
        var key = command.Key.Trim();
        var entity = await dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.Key.ToLower() == key.ToLower(), cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userId = requestContext.UserId ?? Guid.Empty;

        Registry.TryGetValue(key, out var def);
        var valueType = def?.ValueType ?? SettingType.String;
        var description = def?.Description;

        var valueToSave = command.Value;
        if (def?.IsSecret == true && (string.IsNullOrWhiteSpace(valueToSave) || valueToSave == MaskedSecret || valueToSave == "[REDACTED]"))
        {
            // Do not overwrite existing secret if masked value is submitted
            valueToSave = entity?.Value ?? string.Empty;
        }
        else
        {
            ValidateSettingValue(valueType, valueToSave, key);
        }

        if (entity is null)
        {
            entity = SystemSetting.Create(key, valueToSave, valueType, description, userId, now);
            dbContext.SystemSettings.Add(entity);
        }
        else
        {
            if (command.ConcurrencyToken.HasValue && entity.ConcurrencyToken != command.ConcurrencyToken.Value)
            {
                throw new InvalidOperationException("Thiết lập đã bị thay đổi bởi người dùng khác. Vui lòng tải lại trang.");
            }

            entity.Update(valueToSave, valueType, description, userId, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var userDict = user is not null ? new Dictionary<Guid, string?> { [user.Id] = user.DisplayName ?? user.Email } : new Dictionary<Guid, string?>();

        return MapToDto(entity, userDict);
    }

    public async Task<IReadOnlyList<SystemSettingDto>> BatchUpdateAsync(BatchUpdateSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var results = new List<SystemSettingDto>();
        foreach (var item in command.Settings)
        {
            var res = await UpdateAsync(item, cancellationToken);
            results.Add(res);
        }
        return results;
    }

    public async Task<ExportPackageResponse> ExportPackageAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSettingsSeededAsync(cancellationToken);

        var settings = await dbContext.SystemSettings.AsNoTracking()
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);

        var exportedSettings = new List<ExportedSettingDto>();
        foreach (var s in settings)
        {
            Registry.TryGetValue(s.Key, out var def);
            var isSecret = def?.IsSecret ?? s.ValueType == SettingType.Secret;
            var scope = def?.Scope ?? GetScopeFromKey(s.Key);

            // Redact secret on export
            var val = isSecret ? "[REDACTED]" : s.Value;
            exportedSettings.Add(new ExportedSettingDto(s.Key, val, s.ValueType, scope, s.Description));
        }

        var version = "1.0.0";
        var checksum = ComputeChecksum(version, exportedSettings);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var user = requestContext.UserId.HasValue
            ? await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == requestContext.UserId.Value, cancellationToken)
            : null;
        var exportedBy = user?.Email ?? user?.DisplayName ?? "System Administrator";

        return new ExportPackageResponse(version, now, exportedBy, checksum, exportedSettings);
    }

    public async Task<PackageValidationResultDto> ValidatePackageAsync(string packageJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageJson))
        {
            return new PackageValidationResultDto(false, "Nội dung tệp cấu hình rỗng.", string.Empty, string.Empty, 0, []);
        }

        ExportPackageResponse? package;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            package = JsonSerializer.Deserialize<ExportPackageResponse>(packageJson, options);
        }
        catch (Exception ex)
        {
            return new PackageValidationResultDto(false, $"Tệp không đúng định dạng JSON hợp lệ: {ex.Message}", string.Empty, string.Empty, 0, []);
        }

        if (package is null || package.Settings is null || package.Settings.Count == 0)
        {
            return new PackageValidationResultDto(false, "Gói cấu hình không chứa danh sách thiết lập hợp lệ.", string.Empty, string.Empty, 0, []);
        }

        // Validate checksum
        var computedChecksum = ComputeChecksum(package.Version, package.Settings);
        if (!string.Equals(package.Checksum?.Trim(), computedChecksum, StringComparison.OrdinalIgnoreCase))
        {
            return new PackageValidationResultDto(
                false,
                $"Mã kiểm tra tính toàn vẹn (Checksum) không khớp. Gói cấu hình có thể đã bị chỉnh sửa bất hợp pháp hoặc hư hại dữ liệu.",
                package.Version ?? "1.0.0",
                package.Checksum ?? string.Empty,
                package.Settings.Count,
                []);
        }

        // Fetch current DB settings to generate Diff
        var currentSettings = await dbContext.SystemSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.Key, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var diffs = new List<SettingDiffItemDto>();
        foreach (var incoming in package.Settings)
        {
            Registry.TryGetValue(incoming.Key, out var def);
            var isSecret = def?.IsSecret ?? incoming.ValueType == SettingType.Secret;
            var scope = incoming.Scope ?? def?.Scope ?? GetScopeFromKey(incoming.Key);

            if (currentSettings.TryGetValue(incoming.Key, out var current))
            {
                var isModified = !string.Equals(current.Value, incoming.Value, StringComparison.Ordinal);
                var diffType = isModified ? "CHANGE" : "SAME";

                var hasWarning = isModified && (incoming.Key.StartsWith("circulation.fine") || incoming.Key.StartsWith("circulation.block"));
                var warningMsg = hasWarning ? "Thay đổi có thể tác động trực tiếp đến tiền phạt và điều kiện mượn trả hiện tại." : null;

                diffs.Add(new SettingDiffItemDto(
                    incoming.Key,
                    isSecret ? MaskedSecret : current.Value,
                    isSecret ? MaskedSecret : incoming.Value,
                    incoming.ValueType,
                    scope,
                    diffType,
                    incoming.Description ?? current.Description,
                    isSecret,
                    hasWarning,
                    warningMsg));
            }
            else
            {
                diffs.Add(new SettingDiffItemDto(
                    incoming.Key,
                    null,
                    isSecret ? MaskedSecret : incoming.Value,
                    incoming.ValueType,
                    scope,
                    "ADD",
                    incoming.Description ?? def?.Description,
                    isSecret,
                    false,
                    null));
            }
        }

        return new PackageValidationResultDto(
            true,
            null,
            package.Version ?? "1.0.0",
            package.Checksum ?? string.Empty,
            package.Settings.Count,
            diffs);
    }

    public async Task<ConfirmImportResult> ImportPackageAsync(ConfirmImportCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await ValidatePackageAsync(command.PackageJson, cancellationToken);
        if (!validation.IsValid)
        {
            return new ConfirmImportResult(false, validation.ErrorMessage ?? "Xác thực gói cấu hình thất bại.", 0, 0, 0, Guid.Empty);
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var package = JsonSerializer.Deserialize<ExportPackageResponse>(command.PackageJson, options)!;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userId = requestContext.UserId ?? Guid.Empty;

        int added = 0;
        int updated = 0;
        int unchanged = 0;

        // Apply in Database Transaction
        var strategy = dbContext.Database.CreateExecutionStrategy();
        Guid packageId = Guid.Empty;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var currentMap = await dbContext.SystemSettings
                .ToDictionaryAsync(s => s.Key, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach (var item in package.Settings)
            {
                Registry.TryGetValue(item.Key, out var def);
                var isSecret = def?.IsSecret ?? item.ValueType == SettingType.Secret;

                // Skip secrets if marked [REDACTED] or empty
                if (isSecret && (item.Value == "[REDACTED]" || item.Value == MaskedSecret || string.IsNullOrWhiteSpace(item.Value)))
                {
                    unchanged++;
                    continue;
                }

                if (currentMap.TryGetValue(item.Key, out var existing))
                {
                    if (existing.Value != item.Value)
                    {
                        existing.Update(item.Value, item.ValueType, item.Description ?? existing.Description, userId, now);
                        updated++;
                    }
                    else
                    {
                        unchanged++;
                    }
                }
                else
                {
                    var newSetting = SystemSetting.Create(item.Key, item.Value, item.ValueType, item.Description, userId, now);
                    dbContext.SystemSettings.Add(newSetting);
                    added++;
                }
            }

            // Record package in configuration_packages table
            var pkgEntity = ConfigurationPackage.Create(package.Version, command.PackageJson, package.Checksum, userId, now);
            dbContext.ConfigurationPackages.Add(pkgEntity);
            packageId = pkgEntity.Id;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return new ConfirmImportResult(
            true,
            $"Nhập gói cấu hình thành công! Đã thêm {added}, cập nhật {updated}, giữ nguyên {unchanged} thiết lập.",
            added,
            updated,
            unchanged,
            packageId);
    }

    public async Task<IReadOnlyList<ConfigurationPackageSummaryDto>> GetPackageHistoryAsync(CancellationToken cancellationToken = default)
    {
        var packages = await dbContext.ConfigurationPackages.AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        var userIds = packages.Select(p => p.CreatedByUserId).Distinct().ToList();
        var userDict = await dbContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? u.Email, cancellationToken);

        return packages.Select(p =>
        {
            userDict.TryGetValue(p.CreatedByUserId, out var userName);
            int count = 0;
            try
            {
                var doc = JsonDocument.Parse(p.Data);
                if (doc.RootElement.TryGetProperty("settings", out var sProp) && sProp.ValueKind == JsonValueKind.Array)
                {
                    count = sProp.GetArrayLength();
                }
            }
            catch { }

            return new ConfigurationPackageSummaryDto(
                p.Id,
                p.Version,
                p.Checksum,
                p.CreatedByUserId,
                userName,
                p.CreatedAtUtc,
                count);
        }).ToList();
    }

    public async Task<int> ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userId = requestContext.UserId ?? Guid.Empty;

        var existingSettings = await dbContext.SystemSettings
            .ToDictionaryAsync(s => s.Key, StringComparer.OrdinalIgnoreCase, cancellationToken);

        int count = 0;
        foreach (var (key, def) in Registry)
        {
            if (def.IsSecret) continue; // Keep secret as is

            if (existingSettings.TryGetValue(key, out var existing))
            {
                if (existing.Value != def.DefaultValue)
                {
                    existing.Update(def.DefaultValue, def.ValueType, def.Description, userId, now);
                    count++;
                }
            }
            else
            {
                var newSetting = SystemSetting.Create(key, def.DefaultValue, def.ValueType, def.Description, userId, now);
                dbContext.SystemSettings.Add(newSetting);
                count++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return count;
    }

    private async Task EnsureDefaultSettingsSeededAsync(CancellationToken cancellationToken)
    {
        var existingKeys = await dbContext.SystemSettings.AsNoTracking()
            .Select(s => s.Key.ToLower())
            .ToListAsync(cancellationToken);

        var keySet = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
        var missingDefs = Registry.Where(kv => !keySet.Contains(kv.Key)).ToList();

        if (missingDefs.Count > 0)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var userId = requestContext.UserId ?? Guid.Empty;

            foreach (var (key, def) in missingDefs)
            {
                var entity = SystemSetting.Create(key, def.DefaultValue, def.ValueType, def.Description, userId, now);
                dbContext.SystemSettings.Add(entity);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static SystemSettingDto MapToDto(SystemSetting entity, IReadOnlyDictionary<Guid, string?> userDict)
    {
        Registry.TryGetValue(entity.Key, out var def);
        var isSecret = def?.IsSecret ?? entity.ValueType == SettingType.Secret;
        var scope = def?.Scope ?? GetScopeFromKey(entity.Key);
        userDict.TryGetValue(entity.UpdatedByUserId, out var userName);

        var val = isSecret ? MaskedSecret : entity.Value;

        return new SystemSettingDto(
            entity.Id,
            entity.Key,
            val,
            entity.ValueType,
            scope,
            entity.Description ?? def?.Description,
            isSecret,
            entity.UpdatedByUserId,
            userName,
            entity.UpdatedAtUtc,
            entity.ConcurrencyToken);
    }

    private static string GetScopeFromKey(string key)
    {
        var dotIdx = key.IndexOf('.');
        return dotIdx > 0 ? key[..dotIdx] : "general";
    }

    private static void ValidateSettingValue(SettingType type, string value, string key)
    {
        switch (type)
        {
            case SettingType.Number:
                if (!decimal.TryParse(value, out _))
                    throw new ArgumentException($"Giá trị của thiết lập '{key}' phải là số hợp lệ.");
                break;
            case SettingType.Boolean:
                if (!bool.TryParse(value, out _))
                    throw new ArgumentException($"Giá trị của thiết lập '{key}' phải là boolean (true/false).");
                break;
            case SettingType.Json:
                try
                {
                    JsonDocument.Parse(value);
                }
                catch
                {
                    throw new ArgumentException($"Giá trị của thiết lập '{key}' phải là chuỗi JSON hợp lệ.");
                }
                break;
        }
    }

    private static string ComputeChecksum(string version, IEnumerable<ExportedSettingDto> settings)
    {
        var canonicalList = settings
            .OrderBy(s => s.Key, StringComparer.Ordinal)
            .Select(s => $"{s.Key}={s.Value}:{s.ValueType}")
            .ToList();

        var payload = $"{version}\n{string.Join("\n", canonicalList)}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return $"sha256:{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
