using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class NotificationTemplate
{
    private NotificationTemplate()
    {
        Code = string.Empty;
        Name = string.Empty;
        BodyTemplate = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string? SubjectTemplate { get; private set; }
    public string BodyTemplate { get; private set; }
    public string? AllowedVariables { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static NotificationTemplate Create(
        string code,
        string name,
        NotificationChannel channel,
        string? subjectTemplate,
        string bodyTemplate,
        string? allowedVariables,
        bool isActive,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Mã template không được để trống.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên template không được để trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(bodyTemplate)) throw new ArgumentException("Nội dung template không được để trống.", nameof(bodyTemplate));

        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Channel = channel,
            SubjectTemplate = string.IsNullOrWhiteSpace(subjectTemplate) ? null : subjectTemplate.Trim(),
            BodyTemplate = bodyTemplate.Trim(),
            AllowedVariables = string.IsNullOrWhiteSpace(allowedVariables) ? null : allowedVariables.Trim(),
            IsActive = isActive,
            UpdatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
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
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên template không được để trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(bodyTemplate)) throw new ArgumentException("Nội dung template không được để trống.", nameof(bodyTemplate));

        Name = name.Trim();
        Channel = channel;
        SubjectTemplate = string.IsNullOrWhiteSpace(subjectTemplate) ? null : subjectTemplate.Trim();
        BodyTemplate = bodyTemplate.Trim();
        AllowedVariables = string.IsNullOrWhiteSpace(allowedVariables) ? null : allowedVariables.Trim();
        IsActive = isActive;
        UpdatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        ConcurrencyToken = Guid.NewGuid();
    }

    public void ToggleActive(bool isActive, DateTime nowUtc)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        ConcurrencyToken = Guid.NewGuid();
    }
}
