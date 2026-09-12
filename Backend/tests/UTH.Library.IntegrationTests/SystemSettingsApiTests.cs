using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UTH.Library.Application.Features.SystemSettings;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.IntegrationTests;

public sealed class SystemSettingsApiTests(UserManagementApiFactory factory)
    : IClassFixture<UserManagementApiFactory>
{
    [Fact]
    public async Task GetAllSettings_ReturnsSeededSettingsWithDefaultValues()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/settings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<List<SystemSettingDto>>();
        Assert.NotNull(settings);
        Assert.NotEmpty(settings);

        // Check key circulation setting
        var maxDays = settings.FirstOrDefault(s => s.Key == "circulation.max_days");
        Assert.NotNull(maxDays);
        Assert.Equal("14", maxDays.Value);
        Assert.Equal(SettingType.Number, maxDays.ValueType);
        Assert.Equal("circulation", maxDays.Scope);

        // Check secret is masked
        var smtpPass = settings.FirstOrDefault(s => s.Key == "notification.smtp_password");
        Assert.NotNull(smtpPass);
        Assert.True(smtpPass.IsSecret);
        Assert.Equal("********", smtpPass.Value);
    }

    [Fact]
    public async Task UpdateSetting_ValidValue_UpdatesValueAndRecordsAuditLog()
    {
        var client = factory.CreateClient();

        // 1. Get current setting
        var getRes = await client.GetAsync("/api/v1/settings/circulation.max_books");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var current = await getRes.Content.ReadFromJsonAsync<SystemSettingDto>();
        Assert.NotNull(current);

        // 2. Update with valid number
        var updateCmd = new UpdateSettingCommand("circulation.max_books", "8", current.ConcurrencyToken);
        var putRes = await client.PutAsJsonAsync("/api/v1/settings/circulation.max_books", updateCmd);
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);

        var updated = await putRes.Content.ReadFromJsonAsync<SystemSettingDto>();
        Assert.NotNull(updated);
        Assert.Equal("8", updated.Value);
        Assert.NotEqual(current.ConcurrencyToken, updated.ConcurrencyToken);

        // 3. Check AuditLog entry
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var audit = await db.AuditLogs
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(a => a.EntityType == "SystemSetting" && a.EntityId == updated.Id);

        Assert.NotNull(audit);
        Assert.Contains("8", audit.AfterJson ?? string.Empty);

        // 4. Update with invalid number type should return 400
        var invalidCmd = new UpdateSettingCommand("circulation.max_books", "not_a_number", updated.ConcurrencyToken);
        var badRes = await client.PutAsJsonAsync("/api/v1/settings/circulation.max_books", invalidCmd);
        Assert.Equal(HttpStatusCode.BadRequest, badRes.StatusCode);
    }

    [Fact]
    public async Task ExportValidateAndImportPackage_ValidFlow_AppliesTransactionAndRejectsTamperedChecksum()
    {
        var client = factory.CreateClient();

        // 1. Export package
        var exportRes = await client.GetAsync("/api/v1/settings/export-package");
        Assert.Equal(HttpStatusCode.OK, exportRes.StatusCode);
        var packageJson = await exportRes.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(packageJson));

        var package = JsonSerializer.Deserialize<ExportPackageResponse>(packageJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(package);
        Assert.NotNull(package.Checksum);
        Assert.StartsWith("sha256:", package.Checksum);
        Assert.NotEmpty(package.Settings);

        // Verify secrets are redacted in exported package
        var secretInExport = package.Settings.FirstOrDefault(s => s.Key == "notification.smtp_password");
        Assert.NotNull(secretInExport);
        Assert.Equal("[REDACTED]", secretInExport.Value);

        // 2. Validate valid package -> should succeed with diffs
        var validateRes = await client.PostAsJsonAsync("/api/v1/settings/validate-package", JsonDocument.Parse(packageJson).RootElement);
        Assert.Equal(HttpStatusCode.OK, validateRes.StatusCode);
        var validResult = await validateRes.Content.ReadFromJsonAsync<PackageValidationResultDto>();
        Assert.NotNull(validResult);
        Assert.True(validResult.IsValid);
        Assert.Equal(package.Checksum, validResult.Checksum);

        // 3. Tamper with checksum -> should be rejected by validator
        var tamperedDoc = JsonDocument.Parse(packageJson);
        var tamperedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(packageJson)!;
        tamperedDict["checksum"] = "sha256:0000000000000000000000000000000000000000000000000000000000000000";
        var tamperedJson = JsonSerializer.Serialize(tamperedDict);

        var tamperedValidateRes = await client.PostAsJsonAsync("/api/v1/settings/validate-package", JsonDocument.Parse(tamperedJson).RootElement);
        Assert.Equal(HttpStatusCode.OK, tamperedValidateRes.StatusCode);
        var invalidResult = await tamperedValidateRes.Content.ReadFromJsonAsync<PackageValidationResultDto>();
        Assert.NotNull(invalidResult);
        Assert.False(invalidResult.IsValid);
        Assert.Contains("Checksum", invalidResult.ErrorMessage);

        // 4. Import package with confirm command
        var importCmd = new ConfirmImportCommand(packageJson, OverrideExisting: true);
        var importRes = await client.PostAsJsonAsync("/api/v1/settings/import-package", importCmd);
        Assert.Equal(HttpStatusCode.OK, importRes.StatusCode);
        var importResult = await importRes.Content.ReadFromJsonAsync<ConfirmImportResult>();
        Assert.NotNull(importResult);
        Assert.True(importResult.Success);
        Assert.NotEqual(Guid.Empty, importResult.PackageId);

        // 5. Check configuration package history
        var historyRes = await client.GetAsync("/api/v1/settings/packages");
        Assert.Equal(HttpStatusCode.OK, historyRes.StatusCode);
        var history = await historyRes.Content.ReadFromJsonAsync<List<ConfigurationPackageSummaryDto>>();
        Assert.NotNull(history);
        Assert.Contains(history, h => h.Id == importResult.PackageId);
    }

    [Fact]
    public async Task ResetToDefaults_RestoresSystemDefaultValues()
    {
        var client = factory.CreateClient();

        // 1. Change a setting first
        var getRes = await client.GetAsync("/api/v1/settings/circulation.max_days");
        var current = await getRes.Content.ReadFromJsonAsync<SystemSettingDto>();
        Assert.NotNull(current);

        await client.PutAsJsonAsync("/api/v1/settings/circulation.max_days", new UpdateSettingCommand("circulation.max_days", "30"));

        // 2. Call reset to defaults
        var resetRes = await client.PostAsync("/api/v1/settings/reset-defaults", null);
        Assert.Equal(HttpStatusCode.OK, resetRes.StatusCode);

        // 3. Verify it was restored to 14
        var restoredRes = await client.GetAsync("/api/v1/settings/circulation.max_days");
        var restored = await restoredRes.Content.ReadFromJsonAsync<SystemSettingDto>();
        Assert.NotNull(restored);
        Assert.Equal("14", restored.Value);
    }
}
