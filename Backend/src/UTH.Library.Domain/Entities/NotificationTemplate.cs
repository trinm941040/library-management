using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class NotificationTemplate
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public string? SubjectTemplate { get; private set; }
    public string BodyTemplate { get; private set; } = string.Empty;
    public string? AllowedVariables { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    private NotificationTemplate() { }

    public static NotificationTemplate Create(
        string code,
        string name,
        NotificationChannel channel,
        string? subjectTemplate,
        string bodyTemplate,
        string? allowedVariables,
        bool isActive,
        DateTime now)
    {
        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Channel = channel,
            SubjectTemplate = subjectTemplate?.Trim(),
            BodyTemplate = bodyTemplate,
            AllowedVariables = allowedVariables,
            IsActive = isActive,
            UpdatedAtUtc = now,
            ConcurrencyToken = Guid.NewGuid()
        };
    }

    public void Update(
        string name,
        NotificationChannel channel,
        string? subjectTemplate,
        string bodyTemplate,
        string? allowedVariables,
        bool isActive,
        DateTime now)
    {
        Name = name.Trim();
        Channel = channel;
        SubjectTemplate = subjectTemplate?.Trim();
        BodyTemplate = bodyTemplate;
        AllowedVariables = allowedVariables;
        IsActive = isActive;
        UpdatedAtUtc = now;
        ConcurrencyToken = Guid.NewGuid();
    }
}
