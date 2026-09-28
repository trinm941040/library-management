namespace UTH.Library.Api.Contracts.Notifications;

public sealed record CreateNotificationTemplateApiRequest(
    string Code,
    string Name,
    string Channel,
    string? SubjectTemplate,
    string BodyTemplate,
    string? AllowedVariables,
    bool IsActive = true);

public sealed record UpdateNotificationTemplateApiRequest(
    string Name,
    string Channel,
    string? SubjectTemplate,
    string BodyTemplate,
    string? AllowedVariables,
    bool IsActive,
    Guid? ConcurrencyToken);

public sealed record NotificationPreviewApiRequest(
    string TemplateCode,
    Dictionary<string, string>? Variables = null);

public sealed record SendNotificationApiRequest(
    string TemplateCode,
    string RecipientType,
    Guid RecipientId,
    string? Destination = null,
    Dictionary<string, string>? Variables = null,
    string? EventCode = null,
    string? IdempotencyKey = null,
    string Severity = "Info",
    string? DeepLink = null,
    string? MetadataJson = null);

public sealed record SendBulkNotificationApiRequest(
    string TemplateCode,
    IReadOnlyList<Guid>? RecipientIds,
    bool AllStaff,
    string? RoleName,
    string? PermissionName,
    Guid? BranchId,
    Dictionary<string, string>? Variables,
    string EventCode,
    string IdempotencyKey,
    string Severity = "Info",
    string? DeepLink = null,
    string? MetadataJson = null);

public sealed record NotificationHistoryFilterRequest(
    string? Channel = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record UserNotificationFilterRequest(
    bool UnreadOnly = false,
    string? Severity = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10);
