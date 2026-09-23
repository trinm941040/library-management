namespace UTH.Library.Application.Features.Notifications;

public sealed record SmtpSettings(
    bool Enabled, string Host, int Port, string SecurityMode, string? Username,
    string? Password, string FromAddress, string FromName, string? ReplyToAddress,
    int TimeoutSeconds, int MaxRetryCount, int BatchSize);

public sealed record SmtpSettingsView(
    bool Enabled, string Host, int Port, string SecurityMode, string? Username,
    bool HasPassword, string FromAddress, string FromName, string? ReplyToAddress,
    int TimeoutSeconds, int MaxRetryCount, int BatchSize);

public sealed record SmtpTestCommand(string Recipient, bool SendMessage);
public sealed record SmtpTestResult(bool Success, string Message);

public interface ISmtpSettingsProvider
{
    Task<SmtpSettings> GetAsync(CancellationToken cancellationToken);
}

public interface ISmtpAdministrationService
{
    Task<SmtpSettingsView> GetAsync(CancellationToken cancellationToken);
    Task<SmtpTestResult> TestAsync(SmtpTestCommand command, Guid actorUserId, CancellationToken cancellationToken);
}
