using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class NotificationTemplate { public Guid Id { get; private set; } public string Code { get; private set; } = string.Empty; public string Name { get; private set; } = string.Empty; public NotificationChannel Channel { get; private set; } public string? SubjectTemplate { get; private set; } public string BodyTemplate { get; private set; } = string.Empty; public bool IsActive { get; private set; } public DateTime UpdatedAtUtc { get; private set; } public Guid ConcurrencyToken { get; private set; } }
