using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions;
using UTH.Library.Application.Common;
using UTH.Library.Application.Features.Settings;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Settings;

internal sealed class SystemConfigurationService(
    LibraryDbContext db,
    IDataProtectionProvider dataProtectionProvider,
    IRequestContext requestContext,
    TimeProvider timeProvider) : ISystemConfigurationService
{
    internal const string CurrentSchemaVersion = "1.0";
    internal const int MaximumPackageBytes = 1_048_576;
    private static readonly TimeSpan PreviewLifetime = TimeSpan.FromMinutes(15);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private static readonly IReadOnlyDictionary<string, SettingDefinition> Definitions = CreateDefinitions();
    private readonly IDataProtector previewProtector = dataProtectionProvider.CreateProtector("UTH.Library.ConfigurationImport.v1");
    private readonly IDataProtector secretProtector = dataProtectionProvider.CreateProtector("UTH.Library.SystemSettingSecret.v1");

    public async Task<IReadOnlyList<SystemSettingModel>> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var knownKeys = Definitions.Keys.ToArray();
        var stored = await db.SystemSettings.AsNoTracking()
            .Where(setting => knownKeys.Contains(setting.Key))
            .ToDictionaryAsync(setting => setting.Key, cancellationToken);
        return Definitions.Values
            .OrderBy(definition => definition.Scope)
            .ThenBy(definition => definition.DisplayName)
            .Select(definition => Map(definition, stored.GetValueOrDefault(definition.Key)))
            .ToArray();
    }

    public async Task<SystemSettingModel> UpdateSettingAsync(
        string key,
        UpdateSystemSettingCommand command,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var definition = GetDefinition(key);
        var normalizedValue = NormalizeValue(definition, command.Value);
        var storedValue = definition.IsSecret
            ? JsonSerializer.Serialize(secretProtector.Protect(normalizedValue))
            : normalizedValue;
        var setting = await db.SystemSettings.SingleOrDefaultAsync(value => value.Key == definition.Key, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var before = SettingSnapshot(definition, setting);

        if (setting is null)
        {
            if (command.ConcurrencyToken is not null)
                throw new ResourceConflictException("Thiết lập chưa tồn tại; hãy tải lại dữ liệu.");
            setting = SystemSetting.Create(
                definition.Key, storedValue, definition.ValueType, definition.Scope,
                definition.IsSecret, definition.Description, actorUserId, now);
            db.SystemSettings.Add(setting);
        }
        else
        {
            if (command.ConcurrencyToken is null || command.ConcurrencyToken != setting.ConcurrencyToken)
                throw new OptimisticConcurrencyException("Thiết lập đã được cập nhật bởi yêu cầu khác.");
            setting.SynchronizeDefinition(
                definition.ValueType, definition.Scope, definition.IsSecret, definition.Description);
            setting.Update(storedValue, actorUserId, now);
        }

        db.AuditLogs.Add(AuditLog.Create(
            actorUserId,
            "system-setting.updated",
            nameof(SystemSetting),
            setting.Id,
            before,
            SettingSnapshot(definition, setting),
            now,
            requestContext.CorrelationId));
        await db.SaveChangesAsync(cancellationToken);
        return Map(definition, setting);
    }

    public async Task<ConfigurationExport> ExportAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var knownKeys = Definitions.Keys.ToArray();
        var stored = await db.SystemSettings.AsNoTracking()
            .Where(setting => knownKeys.Contains(setting.Key))
            .ToDictionaryAsync(setting => setting.Key, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var settings = Definitions.Values
            .Where(definition => !definition.IsSecret)
            .OrderBy(definition => definition.Key, StringComparer.Ordinal)
            .Select(definition => new PackageSetting(
                definition.Key,
                definition.ValueType.ToString(),
                definition.Scope.ToString(),
                ParseElement(stored.TryGetValue(definition.Key, out var setting)
                    ? setting.Value
                    : definition.DefaultJson)))
            .ToArray();
        var scopes = settings.Select(setting => setting.Scope).Distinct(StringComparer.Ordinal).Order().ToArray();
        var checksum = ComputeChecksum(CurrentSchemaVersion, now, scopes, settings);
        var packageDocument = new PackageDocument(CurrentSchemaVersion, now, scopes, settings, checksum);
        var content = JsonSerializer.Serialize(packageDocument, JsonOptions);
        var package = ConfigurationPackage.Create(CurrentSchemaVersion, content, checksum, actorUserId, now);
        db.ConfigurationPackages.Add(package);
        db.AuditLogs.Add(AuditLog.Create(
            actorUserId,
            "configuration-package.exported",
            nameof(ConfigurationPackage),
            package.Id,
            null,
            JsonSerializer.Serialize(new { schemaVersion = CurrentSchemaVersion, checksum, settingCount = settings.Length }),
            now,
            requestContext.CorrelationId));
        await db.SaveChangesAsync(cancellationToken);
        return new ConfigurationExport(
            $"uth-library-configuration-{now:yyyyMMddHHmmss}.json",
            content,
            checksum,
            CurrentSchemaVersion);
    }

    public async Task<ConfigurationImportPreview> ValidateImportAsync(
        string fileName,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        var package = ParseAndValidatePackage(fileName, content.Span);
        var state = await LoadImportState(package.Scopes, cancellationToken);
        var differences = CreateDifferences(package, state.Settings);
        var expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.Add(PreviewLifetime);
        var envelope = new PreviewEnvelope(
            expiresAtUtc,
            Encoding.UTF8.GetString(content.Span),
            ComputeStateChecksum(state.Settings));
        var token = previewProtector.Protect(JsonSerializer.Serialize(envelope, JsonOptions));
        return new ConfigurationImportPreview(
            package.SchemaVersion,
            package.Checksum,
            package.ExportedAtUtc,
            expiresAtUtc,
            differences,
            token);
    }

    public async Task<ConfigurationImportResult> ConfirmImportAsync(
        string confirmationToken,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        PreviewEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<PreviewEnvelope>(
                previewProtector.Unprotect(confirmationToken), JsonOptions)
                ?? throw InvalidPackage("ConfirmationToken", "Mã xác nhận không hợp lệ.");
        }
        catch (CryptographicException)
        {
            throw InvalidPackage("ConfirmationToken", "Mã xác nhận không hợp lệ hoặc đã bị thay đổi.");
        }
        catch (JsonException)
        {
            throw InvalidPackage("ConfirmationToken", "Mã xác nhận không hợp lệ.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (envelope.ExpiresAtUtc <= now)
            throw new ResourceConflictException("Bản xem trước đã hết hạn; vui lòng tải và kiểm tra lại gói cấu hình.");
        var packageBytes = Encoding.UTF8.GetBytes(envelope.PackageJson);
        var package = ParseAndValidatePackage("configuration.json", packageBytes);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var state = await LoadImportState(package.Scopes, cancellationToken, tracking: true);
        if (!FixedTimeEquals(envelope.StateChecksum, ComputeStateChecksum(state.Settings)))
            throw new ResourceConflictException("Cấu hình đã thay đổi sau khi xem trước; vui lòng kiểm tra lại gói.");

        var differences = CreateDifferences(package, state.Settings);
        var incoming = package.Settings.ToDictionary(setting => setting.Key, StringComparer.Ordinal);
        foreach (var difference in differences)
        {
            var definition = GetDefinition(difference.Key);
            var existing = state.Settings.GetValueOrDefault(difference.Key);
            if (difference.Kind == ConfigurationDifferenceKind.Remove)
            {
                if (existing is not null) db.SystemSettings.Remove(existing);
                continue;
            }

            var value = NormalizeValue(definition, incoming[difference.Key].Value);
            if (existing is null)
            {
                db.SystemSettings.Add(SystemSetting.Create(
                    definition.Key, value, definition.ValueType, definition.Scope, false,
                    definition.Description, actorUserId, now));
            }
            else
            {
                existing.SynchronizeDefinition(
                    definition.ValueType, definition.Scope, false, definition.Description);
                existing.Update(value, actorUserId, now);
            }
        }

        var packageRecord = ConfigurationPackage.Create(
            package.SchemaVersion, envelope.PackageJson, package.Checksum, actorUserId, now);
        db.ConfigurationPackages.Add(packageRecord);
        db.AuditLogs.Add(AuditLog.Create(
            actorUserId,
            "configuration-package.imported",
            nameof(ConfigurationPackage),
            packageRecord.Id,
            JsonSerializer.Serialize(new { stateChecksum = envelope.StateChecksum }),
            JsonSerializer.Serialize(new
            {
                package.Checksum,
                added = differences.Count(value => value.Kind == ConfigurationDifferenceKind.Add),
                changed = differences.Count(value => value.Kind == ConfigurationDifferenceKind.Change),
                removed = differences.Count(value => value.Kind == ConfigurationDifferenceKind.Remove)
            }),
            now,
            requestContext.CorrelationId));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ResourceConflictException("Không thể áp dụng gói cấu hình do dữ liệu đã thay đổi. Vui lòng kiểm tra lại gói.");
        }

        return new ConfigurationImportResult(
            packageRecord.Id,
            package.Checksum,
            differences.Count(value => value.Kind == ConfigurationDifferenceKind.Add),
            differences.Count(value => value.Kind == ConfigurationDifferenceKind.Change),
            differences.Count(value => value.Kind == ConfigurationDifferenceKind.Remove),
            now);
    }

    private async Task<ImportState> LoadImportState(
        IReadOnlyCollection<string> scopes,
        CancellationToken cancellationToken,
        bool tracking = false)
    {
        var parsedScopes = scopes.Select(value => Enum.Parse<SettingScope>(value)).ToArray();
        var knownKeys = Definitions.Keys.ToArray();
        var query = tracking ? db.SystemSettings.AsQueryable() : db.SystemSettings.AsNoTracking();
        var settings = await query.Where(setting =>
                parsedScopes.Contains(setting.Scope) && knownKeys.Contains(setting.Key))
            .ToDictionaryAsync(setting => setting.Key, cancellationToken);
        return new ImportState(settings);
    }

    private static IReadOnlyList<ConfigurationDifference> CreateDifferences(
        PackageDocument package,
        IReadOnlyDictionary<string, SystemSetting> current)
    {
        var incoming = package.Settings.ToDictionary(setting => setting.Key, StringComparer.Ordinal);
        var differences = new List<ConfigurationDifference>();
        foreach (var item in package.Settings)
        {
            var definition = GetDefinition(item.Key);
            var normalized = NormalizeValue(definition, item.Value);
            if (!current.TryGetValue(item.Key, out var existing))
            {
                if (!JsonEquals(definition.DefaultJson, normalized))
                    differences.Add(Difference(definition, ConfigurationDifferenceKind.Add, null, normalized));
            }
            else if (!JsonEquals(existing.Value, normalized))
            {
                differences.Add(Difference(definition, ConfigurationDifferenceKind.Change, existing.Value, normalized));
            }
        }

        foreach (var existing in current.Values.Where(setting =>
                     !setting.IsSecret &&
                     Definitions.ContainsKey(setting.Key) &&
                     !incoming.ContainsKey(setting.Key)))
        {
            var definition = GetDefinition(existing.Key);
            differences.Add(Difference(definition, ConfigurationDifferenceKind.Remove, existing.Value, null));
        }
        return differences.OrderBy(value => value.Scope).ThenBy(value => value.Key).ToArray();
    }

    private static ConfigurationDifference Difference(
        SettingDefinition definition,
        ConfigurationDifferenceKind kind,
        string? before,
        string? after) => new(
            definition.Key,
            definition.DisplayName,
            definition.Scope,
            definition.ValueType,
            kind,
            before is null ? null : ParseElement(before),
            after is null ? null : ParseElement(after));

    private static PackageDocument ParseAndValidatePackage(string fileName, ReadOnlySpan<byte> content)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".json", StringComparison.OrdinalIgnoreCase))
            throw InvalidPackage("File", "Chỉ chấp nhận tệp JSON.");
        if (content.Length is 0 or > MaximumPackageBytes)
            throw InvalidPackage("File", $"Tệp phải có kích thước từ 1 byte đến {MaximumPackageBytes} byte.");

        PackageDocument package;
        try
        {
            var text = new UTF8Encoding(false, true).GetString(content);
            package = JsonSerializer.Deserialize<PackageDocument>(text, JsonOptions)
                ?? throw InvalidPackage("File", "Nội dung gói cấu hình trống.");
        }
        catch (DecoderFallbackException)
        {
            throw InvalidPackage("File", "Tệp phải sử dụng mã hóa UTF-8 hợp lệ.");
        }
        catch (JsonException)
        {
            throw InvalidPackage("File", "Cấu trúc JSON của gói cấu hình không hợp lệ.");
        }

        if (package.SchemaVersion != CurrentSchemaVersion)
            throw InvalidPackage("SchemaVersion", $"Chỉ hỗ trợ phiên bản {CurrentSchemaVersion}.");
        if (package.ExportedAtUtc.Kind != DateTimeKind.Utc)
            throw InvalidPackage("ExportedAtUtc", "Thời điểm xuất phải dùng UTC.");
        if (package.Scopes.Length is 0 or > 10 || package.Scopes.Distinct(StringComparer.Ordinal).Count() != package.Scopes.Length)
            throw InvalidPackage("Scopes", "Danh sách phạm vi không hợp lệ.");
        foreach (var scope in package.Scopes)
            if (!Enum.TryParse<SettingScope>(scope, false, out _))
                throw InvalidPackage("Scopes", $"Phạm vi '{scope}' không được hỗ trợ.");
        if (package.Settings.Length > 200 || package.Settings.Select(value => value.Key).Distinct(StringComparer.Ordinal).Count() != package.Settings.Length)
            throw InvalidPackage("Settings", "Danh sách thiết lập vượt giới hạn hoặc có khóa trùng.");

        var normalizedSettings = package.Settings.Select(item =>
        {
            var definition = GetDefinition(item.Key);
            if (definition.IsSecret)
                throw InvalidPackage("Settings", $"Thiết lập bí mật '{item.Key}' không được phép nhập/xuất.");
            if (item.ValueType != definition.ValueType.ToString() || item.Scope != definition.Scope.ToString())
                throw InvalidPackage("Settings", $"Kiểu hoặc phạm vi của '{item.Key}' không đúng schema.");
            if (!package.Scopes.Contains(item.Scope, StringComparer.Ordinal))
                throw InvalidPackage("Settings", $"Thiết lập '{item.Key}' nằm ngoài phạm vi gói.");
            return item with { Value = ParseElement(NormalizeValue(definition, item.Value)) };
        }).OrderBy(value => value.Key, StringComparer.Ordinal).ToArray();

        if (package.Checksum.Length != 64 || !TryHex(package.Checksum, out var received))
            throw InvalidPackage("Checksum", "Checksum không hợp lệ.");
        var expectedHex = ComputeChecksum(
            package.SchemaVersion, package.ExportedAtUtc, package.Scopes.Order().ToArray(), normalizedSettings);
        _ = TryHex(expectedHex, out var expected);
        if (!CryptographicOperations.FixedTimeEquals(received, expected))
            throw InvalidPackage("Checksum", "Checksum không khớp với nội dung tệp.");
        return package with
        {
            Scopes = package.Scopes.Order().ToArray(),
            Settings = normalizedSettings,
            Checksum = expectedHex
        };
    }

    private static SystemSettingModel Map(SettingDefinition definition, SystemSetting? setting)
    {
        var hasValue = setting is not null && setting.Value != "null";
        return new SystemSettingModel(
            definition.Key,
            definition.DisplayName,
            definition.Description,
            definition.ValueType,
            definition.Scope,
            definition.IsSecret,
            hasValue,
            definition.IsSecret ? null : ParseElement(setting?.Value ?? definition.DefaultJson),
            definition.IsSecret ? null : ParseElement(definition.DefaultJson),
            setting?.UpdatedByUserId,
            setting?.UpdatedAtUtc,
            setting?.ConcurrencyToken);
    }

    private static string SettingSnapshot(SettingDefinition definition, SystemSetting? setting) =>
        JsonSerializer.Serialize(new
        {
            definition.Key,
            value = definition.IsSecret
                ? (object)"[REDACTED]"
                : setting is null ? null : ParseElement(setting.Value),
            definition.ValueType,
            definition.Scope,
            definition.IsSecret,
            setting?.UpdatedByUserId,
            setting?.UpdatedAtUtc,
            setting?.ConcurrencyToken
        });

    private static SettingDefinition GetDefinition(string key)
    {
        if (Definitions.TryGetValue(key.Trim(), out var definition)) return definition;
        throw InvalidPackage("Key", $"Khóa thiết lập '{key}' không được hỗ trợ.");
    }

    private static string NormalizeValue(SettingDefinition definition, JsonElement value)
    {
        var error = definition.Validate(value);
        if (error is not null) throw InvalidPackage("Value", $"{definition.DisplayName}: {error}");
        return definition.ValueType switch
        {
            SettingType.String => JsonSerializer.Serialize(value.GetString()!.Trim()),
            SettingType.Number => value.TryGetInt64(out var integer)
                ? integer.ToString(CultureInfo.InvariantCulture)
                : value.GetDecimal().ToString(CultureInfo.InvariantCulture),
            SettingType.Boolean => value.GetBoolean() ? "true" : "false",
            _ => JsonSerializer.Serialize(value)
        };
    }

    private static string ComputeChecksum(
        string version,
        DateTime exportedAtUtc,
        IReadOnlyCollection<string> scopes,
        IReadOnlyCollection<PackageSetting> settings)
    {
        var canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = version,
            exportedAtUtc,
            scopes,
            settings
        }, JsonOptions);
        return Convert.ToHexStringLower(SHA256.HashData(canonical));
    }

    private static string ComputeStateChecksum(IReadOnlyDictionary<string, SystemSetting> settings)
    {
        var canonical = string.Join('\n', settings.OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => $"{value.Key}:{value.Value.ConcurrencyToken:N}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static bool JsonEquals(string left, string right) =>
        JsonSerializer.Serialize(ParseElement(left)) == JsonSerializer.Serialize(ParseElement(right));
    private static JsonElement ParseElement(string json) => JsonDocument.Parse(json).RootElement.Clone();
    private static bool TryHex(string value, out byte[] bytes)
    {
        try { bytes = Convert.FromHexString(value); return true; }
        catch (FormatException) { bytes = []; return false; }
    }
    private static bool FixedTimeEquals(string left, string right) =>
        TryHex(left, out var leftBytes) && TryHex(right, out var rightBytes) &&
        CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    private static RequestValidationException InvalidPackage(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static IReadOnlyDictionary<string, SettingDefinition> CreateDefinitions()
    {
        static string? String(JsonElement value, int min, int max, IReadOnlySet<string>? allowed = null)
        {
            if (value.ValueKind != JsonValueKind.String) return "Giá trị phải là chuỗi.";
            var text = value.GetString()?.Trim() ?? string.Empty;
            if (text.Length < min || text.Length > max) return $"Độ dài phải từ {min} đến {max} ký tự.";
            return allowed is not null && !allowed.Contains(text) ? "Giá trị không nằm trong danh sách cho phép." : null;
        }
        static string? Number(JsonElement value, decimal min, decimal max)
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number)) return "Giá trị phải là số.";
            return number < min || number > max ? $"Giá trị phải từ {min} đến {max}." : null;
        }
        static string? Boolean(JsonElement value) => value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? null : "Giá trị phải là true hoặc false.";

        var values = new[]
        {
            new SettingDefinition("system.library-name", "Tên thư viện", "Tên hiển thị của thư viện.", SettingType.String, SettingScope.System, false, "\"Thư viện UTH\"", value => String(value, 2, 100)),
            new SettingDefinition("system.default-language", "Ngôn ngữ mặc định", "Ngôn ngữ giao diện mặc định.", SettingType.String, SettingScope.System, false, "\"vi\"", value => String(value, 2, 5, new HashSet<string>(["vi"]))),
            new SettingDefinition("notifications.email.enabled", "Bật gửi email", "Cho phép hệ thống gửi thông báo email.", SettingType.Boolean, SettingScope.Notifications, false, "false", Boolean),
            new SettingDefinition("notifications.smtp.host", "Máy chủ SMTP", "Tên máy chủ SMTP.", SettingType.String, SettingScope.Notifications, false, "\"smtp.gmail.com\"", value => String(value, 1, 253)),
            new SettingDefinition("notifications.smtp.port", "Cổng SMTP", "Cổng kết nối SMTP.", SettingType.Number, SettingScope.Notifications, false, "587", value => Number(value, 1, 65535)),
            new SettingDefinition("notifications.smtp.username", "Tài khoản SMTP", "Tài khoản dùng để xác thực SMTP.", SettingType.String, SettingScope.Notifications, true, "null", value => String(value, 1, 256)),
            new SettingDefinition("notifications.smtp.password", "Mật khẩu SMTP", "Mật khẩu dùng để xác thực SMTP.", SettingType.String, SettingScope.Notifications, true, "null", value => String(value, 1, 1024)),
            new SettingDefinition("operations.backup.enabled", "Tự động sao lưu", "Bật lịch sao lưu dữ liệu tự động.", SettingType.Boolean, SettingScope.Operations, false, "true", Boolean),
            new SettingDefinition("operations.backup.retention-days", "Thời gian lưu bản sao", "Số ngày lưu bản sao dữ liệu.", SettingType.Number, SettingScope.Operations, false, "30", value => Number(value, 1, 3650))
        };
        return values.ToDictionary(value => value.Key, StringComparer.Ordinal);
    }

    private sealed record SettingDefinition(
        string Key,
        string DisplayName,
        string Description,
        SettingType ValueType,
        SettingScope Scope,
        bool IsSecret,
        string DefaultJson,
        Func<JsonElement, string?> Validate);
    private sealed record PackageSetting(string Key, string ValueType, string Scope, JsonElement Value);
    private sealed record PackageDocument(
        string SchemaVersion,
        DateTime ExportedAtUtc,
        string[] Scopes,
        PackageSetting[] Settings,
        string Checksum);
    private sealed record PreviewEnvelope(DateTime ExpiresAtUtc, string PackageJson, string StateChecksum);
    private sealed record ImportState(IReadOnlyDictionary<string, SystemSetting> Settings);
}
