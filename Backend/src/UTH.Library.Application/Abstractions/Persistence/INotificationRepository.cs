using UTH.Library.Application.Features.Notifications;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    // Template operations
    Task<IReadOnlyList<NotificationTemplate>> GetTemplatesAsync(CancellationToken cancellationToken);
    Task<NotificationTemplate?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<NotificationTemplate?> GetTemplateByCodeAsync(string code, CancellationToken cancellationToken);
    Task<bool> TemplateCodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken);
    Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken);
    Task UpdateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken);
    Task DeleteTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken);

    // Notification operations
    Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken);
    Task<Notification?> GetNotificationByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateNotificationAsync(Notification notification, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> GetPendingBatchAsync(DateTime nowUtc, int batchSize, CancellationToken cancellationToken);
    Task<bool> IdempotencyKeyExistsAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetNotificationHistoryAsync(
        string? channel,
        string? status,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Recipient & InApp operations
    Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetUserNotificationsAsync(
        Guid recipientId,
        bool unreadOnly,
        string? severity,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken);
    Task MarkAllReadAsync(Guid recipientId, DateTime readAtUtc, CancellationToken cancellationToken);

    // Recipient search
    Task<IReadOnlyList<NotificationRecipientDto>> SearchRecipientsAsync(
        RecipientType recipientType,
        string? keyword,
        int limit,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationRecipientDto>> ResolveStaffRecipientsAsync(
        string? roleName,
        string? permissionName,
        Guid? branchId,
        CancellationToken cancellationToken);
    Task<(string Name, string? Email, string? Phone)> GetRecipientDetailsAsync(
        RecipientType recipientType,
        Guid recipientId,
        CancellationToken cancellationToken);

    // Audit logging
    Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken);
}
