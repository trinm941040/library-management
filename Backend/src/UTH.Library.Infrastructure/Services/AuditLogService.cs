using System.Text;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Features.AuditLogs;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Services;

public sealed class AuditLogService(LibraryDbContext db) : IAuditLogService
{
    public async Task<AuditLogPageResponse> GetPageAsync(AuditLogListQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var queryable = ApplyFilters(db.AuditLogs.AsNoTracking(), query);

        var totalCount = await queryable.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var logs = await queryable
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var actorIds = logs.Where(l => l.ActorUserId.HasValue).Select(l => l.ActorUserId!.Value).Distinct().ToArray();
        var userDict = await db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var items = logs.Select(l =>
        {
            string? actorName = null;
            string? actorEmail = null;
            if (l.ActorUserId.HasValue && userDict.TryGetValue(l.ActorUserId.Value, out var u))
            {
                actorName = u.DisplayName;
                actorEmail = u.Email;
            }

            return new AuditLogDto(
                l.Id,
                l.ActorUserId,
                actorName,
                actorEmail,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.BeforeJson,
                l.AfterJson,
                l.CreatedAtUtc,
                l.CorrelationId,
                l.IpAddress);
        }).ToArray();

        return new AuditLogPageResponse(items, pageNumber, pageSize, totalCount, totalPages);
    }

    public async Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (log is null) return null;

        string? actorName = null;
        string? actorEmail = null;
        if (log.ActorUserId.HasValue)
        {
            var user = await db.Users.AsNoTracking()
                .Where(u => u.Id == log.ActorUserId.Value)
                .Select(u => new { u.DisplayName, u.Email })
                .FirstOrDefaultAsync(cancellationToken);
            if (user is not null)
            {
                actorName = user.DisplayName;
                actorEmail = user.Email;
            }
        }

        return new AuditLogDto(
            log.Id,
            log.ActorUserId,
            actorName,
            actorEmail,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.BeforeJson,
            log.AfterJson,
            log.CreatedAtUtc,
            log.CorrelationId,
            log.IpAddress);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetEntityHistoryAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var logs = await db.AuditLogs.AsNoTracking()
            .Where(l => l.EntityType.ToLower() == entityType.ToLower() && l.EntityId == entityId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(100)
            .ToArrayAsync(cancellationToken);

        var actorIds = logs.Where(l => l.ActorUserId.HasValue).Select(l => l.ActorUserId!.Value).Distinct().ToArray();
        var userDict = await db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return logs.Select(l =>
        {
            string? actorName = null;
            string? actorEmail = null;
            if (l.ActorUserId.HasValue && userDict.TryGetValue(l.ActorUserId.Value, out var u))
            {
                actorName = u.DisplayName;
                actorEmail = u.Email;
            }

            return new AuditLogDto(
                l.Id,
                l.ActorUserId,
                actorName,
                actorEmail,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.BeforeJson,
                l.AfterJson,
                l.CreatedAtUtc,
                l.CorrelationId,
                l.IpAddress);
        }).ToArray();
    }

    public async Task<byte[]> ExportCsvAsync(AuditLogListQuery query, CancellationToken cancellationToken = default)
    {
        var queryable = ApplyFilters(db.AuditLogs.AsNoTracking(), query);
        var logs = await queryable
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(5000)
            .ToArrayAsync(cancellationToken);

        var actorIds = logs.Where(l => l.ActorUserId.HasValue).Select(l => l.ActorUserId!.Value).Distinct().ToArray();
        var userDict = await db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var sb = new StringBuilder();
        // UTF-8 BOM for Excel Vietnamese compatibility
        sb.Append('\uFEFF');
        sb.AppendLine("Mã nhật ký;Thời gian (UTC);Người thực hiện;Email;Hành động;Đối tượng;Mã đối tượng;Địa chỉ IP;Correlation ID");

        foreach (var l in logs)
        {
            string actorName = string.Empty;
            string actorEmail = string.Empty;
            if (l.ActorUserId.HasValue && userDict.TryGetValue(l.ActorUserId.Value, out var u))
            {
                actorName = u.DisplayName ?? string.Empty;
                actorEmail = u.Email ?? string.Empty;
            }

            sb.AppendLine($"\"{EscapeCsv(l.Id.ToString())}\";\"{l.CreatedAtUtc:yyyy-MM-dd HH:mm:ss}\";\"{EscapeCsv(actorName)}\";\"{EscapeCsv(actorEmail)}\";\"{EscapeCsv(l.Action)}\";\"{EscapeCsv(l.EntityType)}\";\"{EscapeCsv(l.EntityId.ToString())}\";\"{EscapeCsv(l.IpAddress ?? string.Empty)}\";\"{EscapeCsv(l.CorrelationId ?? string.Empty)}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static IQueryable<Domain.Entities.AuditLog> ApplyFilters(IQueryable<Domain.Entities.AuditLog> queryable, AuditLogListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            queryable = queryable.Where(l =>
                l.Action.ToLower().Contains(search) ||
                l.EntityType.ToLower().Contains(search) ||
                (l.CorrelationId != null && l.CorrelationId.ToLower().Contains(search)) ||
                (l.IpAddress != null && l.IpAddress.ToLower().Contains(search)));
        }

        if (query.ActorUserId.HasValue)
        {
            queryable = queryable.Where(l => l.ActorUserId == query.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim().ToLower();
            queryable = queryable.Where(l => l.Action.ToLower() == action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType.Trim().ToLower();
            queryable = queryable.Where(l => l.EntityType.ToLower() == entityType);
        }

        if (query.EntityId.HasValue)
        {
            queryable = queryable.Where(l => l.EntityId == query.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.IpAddress))
        {
            var ip = query.IpAddress.Trim();
            queryable = queryable.Where(l => l.IpAddress != null && l.IpAddress.Contains(ip));
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            var corr = query.CorrelationId.Trim().ToLower();
            queryable = queryable.Where(l => l.CorrelationId != null && l.CorrelationId.ToLower() == corr);
        }

        if (query.FromDateUtc.HasValue)
        {
            queryable = queryable.Where(l => l.CreatedAtUtc >= query.FromDateUtc.Value);
        }

        if (query.ToDateUtc.HasValue)
        {
            queryable = queryable.Where(l => l.CreatedAtUtc <= query.ToDateUtc.Value);
        }

        return queryable;
    }

    public Task<AuditLogRetentionPolicyDto> GetRetentionPolicyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AuditLogRetentionPolicyDto(
            RetentionDays: 365,
            IsImmutable: true,
            Description: "Nhật ký kiểm toán là bất biến (immutable), được lưu trữ tối thiểu 365 ngày. Bản ghi sau thời hạn lưu trữ có thể được dọn dẹp bởi quản trị viên."));
    }

    public async Task<int> CleanupExpiredLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        if (retentionDays < 30) retentionDays = 30; // Ngưỡng an toàn tối thiểu 30 ngày
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var expiredLogs = await dbContext.AuditLogs
            .Where(x => x.CreatedAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (expiredLogs.Count > 0)
        {
            dbContext.AuditLogs.RemoveRange(expiredLogs);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return expiredLogs.Count;
    }

    private static string EscapeCsv(string s) => s.Replace("\"", "\"\"");
}
