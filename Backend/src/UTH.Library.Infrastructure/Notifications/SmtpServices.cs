using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Features.Notifications;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Notifications;

public sealed class SmtpSettingsProvider(
    LibraryDbContext db,
    IDataProtectionProvider dataProtectionProvider) : ISmtpSettingsProvider
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("UTH.Library.SystemSettingSecret.v1");

    public async Task<SmtpSettings> GetAsync(CancellationToken cancellationToken)
    {
        var values = await db.SystemSettings.AsNoTracking()
            .Where(x => x.Key.StartsWith("notifications."))
            .ToDictionaryAsync(x => x.Key, x => x, cancellationToken);

        string Text(string key, string fallback = "") => values.TryGetValue(key, out var value)
            ? ReadString(value.Value, fallback)
            : fallback;
        int Number(string key, int fallback) => values.TryGetValue(key, out var value) &&
            int.TryParse(value.Value, out var number) ? number : fallback;
        bool Flag(string key, bool fallback) => values.TryGetValue(key, out var value) &&
            bool.TryParse(value.Value, out var flag) ? flag : fallback;
        string? Secret(string key)
        {
            if (!values.TryGetValue(key, out var value) || value.Value == "null") return null;
            try
            {
                var encrypted = JsonSerializer.Deserialize<string>(value.Value);
                return encrypted is null ? null : ReadString(protector.Unprotect(encrypted), string.Empty);
            }
            catch (Exception ex) when (ex is JsonException or System.Security.Cryptography.CryptographicException)
            {
                throw new InvalidOperationException("Không thể đọc bí mật SMTP. Hãy nhập lại mật khẩu SMTP.");
            }
        }

        return new SmtpSettings(
            Flag("notifications.email.enabled", false),
            Text("notifications.smtp.host", "smtp.gmail.com"),
            Number("notifications.smtp.port", 587),
            Text("notifications.smtp.security-mode", "StartTls"),
            Secret("notifications.smtp.username") is { Length: > 0 } user ? user : null,
            Secret("notifications.smtp.password"),
            Text("notifications.smtp.from-address", "no-reply@example.com"),
            Text("notifications.smtp.from-name", "Thư viện UTH"),
            Text("notifications.smtp.reply-to-address") is { Length: > 0 } replyTo ? replyTo : null,
            Number("notifications.smtp.timeout-seconds", 30),
            Number("notifications.smtp.max-retry-count", 5),
            Number("notifications.email.batch-size", 25));
    }

    private static string ReadString(string json, string fallback)
    {
        try { return JsonSerializer.Deserialize<string>(json)?.Trim() ?? fallback; }
        catch (JsonException) { return fallback; }
    }
}

public sealed class SmtpAdministrationService(
    ISmtpSettingsProvider settingsProvider,
    LibraryDbContext db,
    TimeProvider timeProvider) : ISmtpAdministrationService
{
    public async Task<SmtpSettingsView> GetAsync(CancellationToken cancellationToken)
    {
        var value = await settingsProvider.GetAsync(cancellationToken);
        return new SmtpSettingsView(value.Enabled, value.Host, value.Port, value.SecurityMode,
            value.Username, !string.IsNullOrWhiteSpace(value.Password), value.FromAddress,
            value.FromName, value.ReplyToAddress, value.TimeoutSeconds, value.MaxRetryCount, value.BatchSize);
    }

    public async Task<SmtpTestResult> TestAsync(
        SmtpTestCommand command, Guid actorUserId, CancellationToken cancellationToken)
    {
        var settings = await settingsProvider.GetAsync(cancellationToken);
        Validate(settings);
        try
        {
            if (command.SendMessage)
            {
                _ = new MailAddress(command.Recipient);
                using var message = SmtpMessageFactory.Create(settings, command.Recipient,
                    "Kiểm tra SMTP - Thư viện UTH", "Cấu hình SMTP đã hoạt động.", "Cấu hình SMTP đã hoạt động.");
                using var client = SmtpMessageFactory.CreateClient(settings);
                await client.SendMailAsync(message, cancellationToken);
            }
            else
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync(settings.Host, settings.Port, cancellationToken).AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(settings.TimeoutSeconds), cancellationToken);
            }

            await AuditAsync(actorUserId, true, command.SendMessage, cancellationToken);
            return new SmtpTestResult(true, command.SendMessage
                ? "Đã gửi email kiểm thử thành công."
                : "Kết nối tới máy chủ SMTP thành công.");
        }
        catch (Exception ex) when (ex is SmtpException or SocketException or TimeoutException or FormatException)
        {
            await AuditAsync(actorUserId, false, command.SendMessage, cancellationToken);
            return new SmtpTestResult(false, "Không thể kết nối hoặc gửi qua SMTP. Kiểm tra lại cấu hình và thông tin xác thực.");
        }
    }

    private async Task AuditAsync(Guid actorUserId, bool success, bool sendMessage, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        db.AuditLogs.Add(AuditLog.Create(actorUserId, sendMessage ? "smtp.test-send" : "smtp.test-connection",
            "SmtpConfiguration", Guid.Empty, null, JsonSerializer.Serialize(new { success }), now));
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static void Validate(SmtpSettings value)
    {
        if (string.IsNullOrWhiteSpace(value.Host) || value.Port is < 1 or > 65535)
            throw new InvalidOperationException("Máy chủ hoặc cổng SMTP không hợp lệ.");
        if (value.SecurityMode is not ("None" or "StartTls" or "SslTls"))
            throw new InvalidOperationException("Chế độ bảo mật SMTP không hợp lệ.");
        _ = new MailAddress(value.FromAddress);
        if (value.ReplyToAddress is not null) _ = new MailAddress(value.ReplyToAddress);
        if (value.TimeoutSeconds is < 5 or > 120 || value.MaxRetryCount is < 0 or > 20)
            throw new InvalidOperationException("Timeout hoặc số lần gửi lại SMTP không hợp lệ.");
    }
}

internal static class SmtpMessageFactory
{
    internal static SmtpClient CreateClient(SmtpSettings value)
    {
        var client = new SmtpClient(value.Host, value.Port)
        {
            EnableSsl = value.SecurityMode != "None",
            Timeout = value.TimeoutSeconds * 1000,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(value.Username))
            client.Credentials = new NetworkCredential(value.Username, value.Password ?? string.Empty);
        return client;
    }

    internal static MailMessage Create(SmtpSettings value, string destination, string subject, string html, string plainText)
    {
        var message = new MailMessage
        {
            From = new MailAddress(value.FromAddress, value.FromName),
            Subject = subject,
            Body = html,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(destination));
        if (value.ReplyToAddress is not null) message.ReplyToList.Add(value.ReplyToAddress);
        if (!string.IsNullOrWhiteSpace(plainText))
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainText, null, "text/plain"));
        return message;
    }
}
