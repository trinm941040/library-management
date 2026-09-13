using UTH.Library.Application.Abstractions.Persistence;

namespace UTH.Library.Application.Features.AuditLogs;

public sealed class AuditLogService(IAuditLogRepository repository, TimeProvider timeProvider)
{
    public async Task<AuditLogPageModel> GetPageAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        var normalized = query with
        {
            FromUtc = AsUtc(query.FromUtc),
            ToUtc = AsUtc(query.ToUtc),
            PageNumber = Math.Max(1, query.PageNumber),
            PageSize = Math.Clamp(query.PageSize, 1, 100)
        };
        var (items, totalCount) = await repository.GetPageAsync(
            normalized,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
        return new AuditLogPageModel(items, normalized.PageNumber, normalized.PageSize, totalCount);
    }

    public Task<AuditLogModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

    private static DateTime? AsUtc(DateTime? value) => value?.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.Value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        _ => null
    };
}
