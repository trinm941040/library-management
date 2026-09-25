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

    public async Task<IReadOnlyList<Notification>> GetPendingBatchAsync(
        DateTime nowUtc, int batchSize, CancellationToken cancellationToken) =>
        await dbContext.Notifications
            .Where(n => n.Status == NotificationStatus.Pending &&
                        (n.NextAttemptAtUtc == null || n.NextAttemptAtUtc <= nowUtc) &&
                        (n.ScheduledAtUtc == null || n.ScheduledAtUtc <= nowUtc))
            .OrderBy(n => n.NextAttemptAtUtc ?? n.ScheduledAtUtc ?? DateTime.MinValue)
            .Take(Math.Clamp(batchSize, 1, 200))
            .ToListAsync(cancellationToken);

    public Task<bool> IdempotencyKeyExistsAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        dbContext.Notifications.AsNoTracking().AnyAsync(n => n.IdempotencyKey == idempotencyKey, cancellationToken);

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
                n.EventCode,
                n.Severity,
                n.DeepLink,
                n.MetadataJson,
                n.CreatedAtUtc,
                n.ReadAtUtc,
                n.IsRead);
        }).ToList();

        return (dtos, totalCount);
    }

    public async Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetUserNotificationsAsync(
        Guid recipientId,
        bool unreadOnly,
        string? severity,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Internal notifications directed to this user/recipient
        var query = from n in dbContext.Notifications.AsNoTracking()
                    join t in dbContext.NotificationTemplates.AsNoTracking() on n.TemplateId equals t.Id
                    where n.RecipientId == recipientId && n.RecipientType == RecipientType.Staff &&
                          t.Channel == NotificationChannel.InApp && n.Status == NotificationStatus.Sent
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
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(x => x.Notification.Severity == severity);
        if (fromDateUtc.HasValue)
            query = query.Where(x => x.Notification.CreatedAtUtc >= DateTime.SpecifyKind(fromDateUtc.Value, DateTimeKind.Utc));
        if (toDateUtc.HasValue)
            query = query.Where(x => x.Notification.CreatedAtUtc <= DateTime.SpecifyKind(toDateUtc.Value, DateTimeKind.Utc));

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedList = await query
            .OrderByDescending(x => x.Notification.CreatedAtUtc)
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
            x.Notification.EventCode,
            x.Notification.Severity,
            x.Notification.DeepLink,
            x.Notification.MetadataJson,
            x.Notification.CreatedAtUtc,
            x.Notification.ReadAtUtc,
            x.Notification.IsRead)).ToList();

        return (dtos, totalCount);
    }

    public Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken)
    {
        return (from n in dbContext.Notifications.AsNoTracking()
                join t in dbContext.NotificationTemplates.AsNoTracking() on n.TemplateId equals t.Id
                where n.RecipientId == recipientId && n.RecipientType == RecipientType.Staff &&
                      n.ReadAtUtc == null && n.Status == NotificationStatus.Sent &&
                      t.Channel == NotificationChannel.InApp
                select n).CountAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid recipientId, DateTime readAtUtc, CancellationToken cancellationToken)
    {
        var unreadList = await dbContext.Notifications
            .Where(n => n.RecipientId == recipientId && n.RecipientType == RecipientType.Staff &&
                        n.ReadAtUtc == null && dbContext.NotificationTemplates
                            .Any(t => t.Id == n.TemplateId && t.Channel == NotificationChannel.InApp))
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
            var q = from employee in dbContext.Employees.AsNoTracking()
                    join user in dbContext.Users.AsNoTracking() on employee.UserId equals user.Id
                    where employee.Status == EmploymentStatus.Active && user.IsActive
                    select employee;
            if (!string.IsNullOrWhiteSpace(kw))
            {
                q = q.Where(e => e.FullName.ToLower().Contains(kw) || e.EmployeeCode.ToLower().Contains(kw) || e.Email.ToLower().Contains(kw));
            }

            return await q.OrderBy(e => e.FullName)
                .Take(limit)
                .Select(e => new NotificationRecipientDto(e.UserId!.Value, "Staff", e.EmployeeCode, e.FullName, e.Email, e.PhoneNumber))
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
            var user = await dbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == recipientId && u.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException("Tài khoản nhận thông báo không tồn tại hoặc đã ngừng hoạt động.");
            var emp = await dbContext.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == recipientId && e.Status == EmploymentStatus.Active, cancellationToken);
            if (emp is not null)
            {
                return (emp.FullName, emp.Email, emp.PhoneNumber);
            }
            throw new KeyNotFoundException("Tài khoản nhận thông báo không liên kết hồ sơ nhân viên đang hoạt động.");
        }

        return ("Người nhận", null, null);
    }

    public async Task<IReadOnlyList<NotificationRecipientDto>> ResolveStaffRecipientsAsync(
        string? roleName,
        string? permissionName,
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        var query = from employee in dbContext.Employees.AsNoTracking()
                    join user in dbContext.Users.AsNoTracking() on employee.UserId equals user.Id
                    where employee.Status == EmploymentStatus.Active && user.IsActive
                    select new { Employee = employee, User = user };

        if (branchId.HasValue)
            query = query.Where(row => row.Employee.BranchId == branchId.Value);
        if (!string.IsNullOrWhiteSpace(roleName))
        {
            var normalizedRole = roleName.Trim().ToUpperInvariant();
            query = query.Where(row => dbContext.UserRoles.Any(assignment =>
                assignment.UserId == row.User.Id && dbContext.Roles.Any(role =>
                    role.Id == assignment.RoleId && role.IsActive && role.NormalizedName == normalizedRole)));
        }
        if (!string.IsNullOrWhiteSpace(permissionName))
        {
            var normalizedPermission = permissionName.Trim().ToLowerInvariant();
            query = query.Where(row => dbContext.UserRoles.Any(userRole =>
                userRole.UserId == row.User.Id && dbContext.Roles.Any(role =>
                    role.Id == userRole.RoleId && role.IsActive && dbContext.RolePermissions.Any(rolePermission =>
                        rolePermission.RoleId == role.Id && rolePermission.Permission.Name.ToLower() == normalizedPermission))));
        }

        return await query
            .OrderBy(row => row.Employee.FullName)
            .Select(row => new NotificationRecipientDto(
                row.User.Id,
                "Staff",
                row.Employee.EmployeeCode,
                row.Employee.FullName,
                row.User.Email,
                row.User.PhoneNumber))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        await dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
