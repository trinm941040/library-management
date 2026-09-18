using UTH.Library.Application.Features.AuditLogs;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLogModel> Items, int TotalCount)> GetPageAsync(
        AuditLogQuery query,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<AuditLogModel?> GetByIdAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditLogModel>> GetForExportAsync(
        AuditLogQuery query,
        DateTime utcNow,
        int maximumRows,
        CancellationToken cancellationToken);
}
