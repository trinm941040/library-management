using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Notifications;

public sealed record NotificationTemplateDto(
    Guid Id,
    string Code,
    string Name,
    string Channel,
    string? SubjectTemplate,
    string BodyTemplate,
    string? AllowedVariables,
    bool IsActive,
    DateTime UpdatedAtUtc,
    Guid ConcurrencyToken);

public sealed record CreateNotificationTemplateCommand(
    string Code,
    string Name,
    string Channel,
    string? SubjectTemplate,
    string BodyTemplate,
    string? AllowedVariables,
    bool IsActive);

public sealed record UpdateNotificationTemplateCommand(
    string Name,
    string Channel,
    string? SubjectTemplate,
    string BodyTemplate,
    string? AllowedVariables,
    bool IsActive,
    Guid? ConcurrencyToken);

public sealed record RenderNotificationPreviewQuery(
    string TemplateCode,
    Dictionary<string, string> Variables);

public sealed record NotificationPreviewResult(
    string TemplateCode,
    string Channel,
    string? RenderedSubject,
    string RenderedBody);

public sealed record SendNotificationCommand(
    string TemplateCode,
    string RecipientType,
    Guid RecipientId,
    string? Destination,
    Dictionary<string, string> Variables,
    string? EventCode = null,
    string? IdempotencyKey = null,
    string Severity = "Info",
    string? DeepLink = null,
    string? MetadataJson = null);

public sealed record NotificationEventDefinition(
    string Code,
    IReadOnlyList<string> AllowedVariables,
    IReadOnlyList<string> RequiredVariables,
    string RecipientRule);

public sealed record NotificationDto(
    Guid Id,
    Guid TemplateId,
    string TemplateCode,
    string TemplateName,
    string Channel,
    string RecipientType,
    Guid RecipientId,
    string RecipientName,
    string Destination,
    string? Subject,
    string Body,
    string Status,
    DateTime? ScheduledAtUtc,
    DateTime? SentAtUtc,
    string? FailureReason,
    string? EventCode,
    string Severity,
    string? DeepLink,
    string? MetadataJson,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc,
    bool IsRead);

public sealed record NotificationPageResult(
    IReadOnlyList<NotificationDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record NotificationRecipientDto(
    Guid Id,
    string RecipientType,
    string Code,
    string Name,
    string? Email,
    string? PhoneNumber);

public sealed record NotificationUnreadCountResult(
    int UnreadCount);

public sealed record SendBulkNotificationCommand(
    string TemplateCode,
    string? RoleName,
    string? PermissionName,
    Guid? BranchId,
    Dictionary<string, string> Variables,
    string EventCode,
    string IdempotencyKey,
    string Severity = "Info",
    string? DeepLink = null,
    string? MetadataJson = null);

public sealed record BulkNotificationResult(int RecipientCount, IReadOnlyList<Guid> NotificationIds);
