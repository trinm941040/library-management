using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.Notifications;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(LibraryDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<NotificationTemplate>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.NotificationTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<NotificationTemplate?> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<NotificationTemplate?> GetTemplateByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return dbContext.NotificationTemplates.FirstOrDefaultAsync(t => t.Code == normalized, cancellationToken);
    }

    public Task<bool> TemplateCodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return dbContext.NotificationTemplates.AnyAsync(
            t => t.Code == normalized && (!excludeId.HasValue || t.Id != excludeId.Value),
            cancellationToken);
    }

    public async Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        dbContext.NotificationTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task UpdateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        dbContext.NotificationTemplates.Update(template);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken)
    {
        dbContext.NotificationTemplates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken)
    {
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public Task<Notification?> GetNotificationByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task UpdateNotificationAsync(Notification notification, CancellationToken cancellationToken)
    {
        dbContext.Notifications.Update(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetNotificationHistoryAsync(
        string? channel,
        string? status,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = from n in dbContext.Notifications.AsNoTracking()
                    join t in dbContext.NotificationTemplates.AsNoTracking() on n.TemplateId equals t.Id
                    select new
                    {
                        Notification = n,
                        TemplateCode = t.Code,
                        TemplateName = t.Name,
                        TemplateChannel = t.Channel
                    };

        if (!string.IsNullOrWhiteSpace(channel) && channel != "all" &&
            Enum.TryParse<NotificationChannel>(channel, true, out var parsedChannel))
        {
            query = query.Where(x => x.TemplateChannel == parsedChannel);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all" &&
            Enum.TryParse<NotificationStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Notification.Status == parsedStatus);
        }

        if (fromDateUtc.HasValue)
        {
            var fUtc = DateTime.SpecifyKind(fromDateUtc.Value, DateTimeKind.Utc);
            query = query.Where(x => x.Notification.SentAtUtc >= fUtc || (x.Notification.SentAtUtc == null && x.Notification.ScheduledAtUtc >= fUtc));
        }

        if (toDateUtc.HasValue)
        {
            var tUtc = DateTime.SpecifyKind(toDateUtc.Value, DateTimeKind.Utc);
            query = query.Where(x => x.Notification.SentAtUtc <= tUtc || (x.Notification.SentAtUtc == null && x.Notification.ScheduledAtUtc <= tUtc));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedList = await query
            .OrderByDescending(x => x.Notification.SentAtUtc ?? x.Notification.ScheduledAtUtc ?? DateTime.MinValue)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Fetch recipient names
        var memberRecipientIds = pagedList
            .Where(x => x.Notification.RecipientType == RecipientType.Member)
            .Select(x => x.Notification.RecipientId)
            .Distinct()
            .ToList();

        var memberNames = memberRecipientIds.Count > 0
            ? await dbContext.Members.AsNoTracking()
                .Where(m => memberRecipientIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.FullName, cancellationToken)
            : new Dictionary<Guid, string>();

        var staffRecipientIds = pagedList
            .Where(x => x.Notification.RecipientType == RecipientType.Staff)
            .Select(x => x.Notification.RecipientId)
            .Distinct()
            .ToList();

        var staffNames = staffRecipientIds.Count > 0
            ? await dbContext.Employees.AsNoTracking()
                .Where(e => staffRecipientIds.Contains(e.Id) || (e.UserId.HasValue && staffRecipientIds.Contains(e.UserId.Value)))
                .ToDictionaryAsync(e => e.UserId ?? e.Id, e => e.FullName, cancellationToken)
            : new Dictionary<Guid, string>();

        var dtos = pagedList.Select(x =>
        {
            var n = x.Notification;
            var rName = n.RecipientType == RecipientType.Member
                ? memberNames.GetValueOrDefault(n.RecipientId, "Độc giả")
                : staffNames.GetValueOrDefault(n.RecipientId, "Nhân viên");

            return new NotificationDto(
                n.Id,
                n.TemplateId,
                x.TemplateCode,
                x.TemplateName,
                x.TemplateChannel.ToString(),
                n.RecipientType.ToString(),
                n.RecipientId,
                rName,
                n.Destination,
                n.Subject,
                n.Body,
                n.Status.ToString(),
                n.ScheduledAtUtc,
                n.SentAtUtc,
                n.FailureReason,
                n.ReadAtUtc,
                n.IsRead);
        }).ToList();

        return (dtos, totalCount);
    }

    public async Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetUserNotificationsAsync(
        Guid recipientId,
        bool unreadOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Internal notifications directed to this user/recipient
        var query = from n in dbContext.Notifications.AsNoTracking()
                    where n.RecipientId == recipientId
                    join t in dbContext.NotificationTemplates.AsNoTracking() on n.TemplateId equals t.Id
                    select new
                    {
                        Notification = n,
                        TemplateCode = t.Code,
                        TemplateName = t.Name,
                        TemplateChannel = t.Channel
                    };

        if (unreadOnly)
        {
            query = query.Where(x => x.Notification.ReadAtUtc == null);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedList = await query
            .OrderByDescending(x => x.Notification.SentAtUtc ?? x.Notification.ScheduledAtUtc ?? DateTime.MinValue)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = pagedList.Select(x => new NotificationDto(
            x.Notification.Id,
            x.Notification.TemplateId,
            x.TemplateCode,
            x.TemplateName,
            x.TemplateChannel.ToString(),
            x.Notification.RecipientType.ToString(),
            x.Notification.RecipientId,
            "Tôi",
            x.Notification.Destination,
            x.Notification.Subject,
            x.Notification.Body,
            x.Notification.Status.ToString(),
            x.Notification.ScheduledAtUtc,
            x.Notification.SentAtUtc,
            x.Notification.FailureReason,
            x.Notification.ReadAtUtc,
            x.Notification.IsRead)).ToList();

        return (dtos, totalCount);
    }

    public Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken)
    {
        return dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientId == recipientId && n.ReadAtUtc == null && n.Status == NotificationStatus.Sent, cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid recipientId, DateTime readAtUtc, CancellationToken cancellationToken)
    {
        var unreadList = await dbContext.Notifications
            .Where(n => n.RecipientId == recipientId && n.ReadAtUtc == null)
            .ToListAsync(cancellationToken);

        var utcTime = DateTime.SpecifyKind(readAtUtc, DateTimeKind.Utc);
        foreach (var n in unreadList)
        {
            n.MarkRead(utcTime);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationRecipientDto>> SearchRecipientsAsync(
        RecipientType recipientType,
        string? keyword,
        int limit,
        CancellationToken cancellationToken)
    {
        var kw = keyword?.Trim().ToLowerInvariant() ?? string.Empty;

        if (recipientType == RecipientType.Member)
        {
            var q = dbContext.Members.AsNoTracking().Where(m => m.Status == MemberStatus.Active);
            if (!string.IsNullOrWhiteSpace(kw))
            {
                q = q.Where(m => m.FullName.ToLower().Contains(kw) || m.MemberCode.ToLower().Contains(kw) || m.Email.ToLower().Contains(kw));
            }

            return await q.OrderBy(m => m.FullName)
                .Take(limit)
                .Select(m => new NotificationRecipientDto(m.Id, "Member", m.MemberCode, m.FullName, m.Email, m.PhoneNumber))
                .ToListAsync(cancellationToken);
        }
        else
        {
            var q = dbContext.Employees.AsNoTracking().Where(e => e.Status == EmploymentStatus.Active);
            if (!string.IsNullOrWhiteSpace(kw))
            {
                q = q.Where(e => e.FullName.ToLower().Contains(kw) || e.EmployeeCode.ToLower().Contains(kw) || e.Email.ToLower().Contains(kw));
            }

            return await q.OrderBy(e => e.FullName)
                .Take(limit)
                .Select(e => new NotificationRecipientDto(e.UserId ?? e.Id, "Staff", e.EmployeeCode, e.FullName, e.Email, e.PhoneNumber))
                .ToListAsync(cancellationToken);
        }
    }

    public async Task<(string Name, string? Email, string? Phone)> GetRecipientDetailsAsync(
        RecipientType recipientType,
        Guid recipientId,
        CancellationToken cancellationToken)
    {
        if (recipientType == RecipientType.Member)
        {
            var member = await dbContext.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == recipientId, cancellationToken);
            if (member is not null)
            {
                return (member.FullName, member.Email, member.PhoneNumber);
            }
        }
        else
        {
            var emp = await dbContext.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == recipientId || e.UserId == recipientId, cancellationToken);
            if (emp is not null)
            {
                return (emp.FullName, emp.Email, emp.PhoneNumber);
            }

            var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == recipientId, cancellationToken);
            if (user is not null)
            {
                return (user.DisplayName ?? user.UserName ?? "Nhân viên", user.Email, user.PhoneNumber);
            }
        }

        return ("Người nhận", null, null);
    }

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        return dbContext.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();
    }
}
