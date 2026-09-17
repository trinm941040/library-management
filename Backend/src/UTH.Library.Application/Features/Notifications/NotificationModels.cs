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
    DateTime UpdatedAtUtc);

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
    bool IsActive);

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
    Dictionary<string, string> Variables);

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
