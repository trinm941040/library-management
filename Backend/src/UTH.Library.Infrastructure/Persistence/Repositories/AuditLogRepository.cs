using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.AuditLogs;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(LibraryDbContext db) : IAuditLogRepository
{
    public async Task<(IReadOnlyList<AuditLogModel> Items, int TotalCount)> GetPageAsync(
        AuditLogQuery request,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var query = db.AuditLogs.AsNoTracking().Where(log => log.RetainUntilUtc > utcNow);
        if (request.ActorUserId is Guid actorId) query = query.Where(log => log.ActorUserId == actorId);
        if (request.EntityId is Guid entityId) query = query.Where(log => log.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(log => log.Action == request.Action.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(log => log.EntityType == request.EntityType.Trim());
        if (!string.IsNullOrWhiteSpace(request.CorrelationId)) query = query.Where(log => log.CorrelationId == request.CorrelationId.Trim());
        if (!string.IsNullOrWhiteSpace(request.IpAddress)) query = query.Where(log => log.IpAddress == request.IpAddress.Trim());
        if (request.FromUtc is DateTime from) query = query.Where(log => log.CreatedAtUtc >= from);
        if (request.ToUtc is DateTime to) query = query.Where(log => log.CreatedAtUtc < to);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await (
            from log in query
            join user in db.Users.AsNoTracking() on log.ActorUserId equals user.Id into actorUsers
            from actor in actorUsers.DefaultIfEmpty()
            orderby log.CreatedAtUtc descending, log.Id descending
            select new AuditLogModel(
                log.Id,
                log.ActorUserId,
                actor == null ? null : actor.DisplayName,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.BeforeJson,
                log.AfterJson,
                log.CreatedAtUtc,
                log.CorrelationId,
                log.IpAddress))
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<AuditLogModel?> GetByIdAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken) =>
        (from log in db.AuditLogs.AsNoTracking()
         join user in db.Users.AsNoTracking() on log.ActorUserId equals user.Id into actorUsers
         from actor in actorUsers.DefaultIfEmpty()
         where log.Id == id && log.RetainUntilUtc > utcNow
         select new AuditLogModel(
             log.Id, log.ActorUserId, actor == null ? null : actor.DisplayName, log.Action,
             log.EntityType, log.EntityId, log.BeforeJson, log.AfterJson, log.CreatedAtUtc,
             log.CorrelationId, log.IpAddress)).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditLogModel>> GetForExportAsync(
        AuditLogQuery request,
        DateTime utcNow,
        int maximumRows,
        CancellationToken cancellationToken)
    {
        var query = Filter(request, utcNow);
        return await Project(query
                .OrderByDescending(log => log.CreatedAtUtc)
                .ThenByDescending(log => log.Id)
                .Take(maximumRows))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<UTH.Library.Domain.Entities.AuditLog> Filter(AuditLogQuery request, DateTime utcNow)
    {
        var query = db.AuditLogs.AsNoTracking().Where(log => log.RetainUntilUtc > utcNow);
        if (request.ActorUserId is Guid actorId) query = query.Where(log => log.ActorUserId == actorId);
        if (request.EntityId is Guid entityId) query = query.Where(log => log.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(log => log.Action == request.Action.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(log => log.EntityType == request.EntityType.Trim());
        if (!string.IsNullOrWhiteSpace(request.CorrelationId)) query = query.Where(log => log.CorrelationId == request.CorrelationId.Trim());
        if (!string.IsNullOrWhiteSpace(request.IpAddress)) query = query.Where(log => log.IpAddress == request.IpAddress.Trim());
        if (request.FromUtc is DateTime from) query = query.Where(log => log.CreatedAtUtc >= from);
        if (request.ToUtc is DateTime to) query = query.Where(log => log.CreatedAtUtc < to);
        return query;
    }

    private IQueryable<AuditLogModel> Project(IQueryable<UTH.Library.Domain.Entities.AuditLog> query) =>
        from log in query
        join user in db.Users.AsNoTracking() on log.ActorUserId equals user.Id into actorUsers
        from actor in actorUsers.DefaultIfEmpty()
        select new AuditLogModel(log.Id, log.ActorUserId, actor == null ? null : actor.DisplayName,
            log.Action, log.EntityType, log.EntityId, log.BeforeJson, log.AfterJson,
            log.CreatedAtUtc, log.CorrelationId, log.IpAddress);
}
