namespace UTH.Library.Application.Features.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken);
    IReadOnlyList<NotificationEventDefinition> GetEventDefinitions();
    Task<NotificationTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<NotificationTemplateDto> CreateTemplateAsync(
        CreateNotificationTemplateCommand command,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken);
    Task<NotificationTemplateDto> UpdateTemplateAsync(
        Guid id,
        UpdateNotificationTemplateCommand command,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken);
    Task<bool> DeleteTemplateAsync(
        Guid id,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<NotificationPreviewResult> PreviewAsync(
        RenderNotificationPreviewQuery query,
        CancellationToken cancellationToken);

    Task<NotificationDto> SendAsync(
        SendNotificationCommand command,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<BulkNotificationResult> SendBulkAsync(
        SendBulkNotificationCommand command,
        Guid actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<NotificationDto> RetryAsync(
        Guid notificationId,
        Guid? actorUserId,
        string? correlationId,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<NotificationPageResult> GetHistoryAsync(
        string? channel,
        string? status,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<NotificationPageResult> GetMyNotificationsAsync(
        Guid recipientId,
        bool unreadOnly,
        string? severity,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<NotificationDto?> GetMyNotificationAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken);

    Task<NotificationUnreadCountResult> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken);

    Task<bool> MarkReadAsync(
        Guid notificationId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken);

    Task MarkAllReadAsync(
        Guid recipientId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationRecipientDto>> SearchRecipientsAsync(
        string recipientType,
        string? keyword,
        IReadOnlyCollection<string> permissions,
        bool isAdmin,
        CancellationToken cancellationToken);
}
