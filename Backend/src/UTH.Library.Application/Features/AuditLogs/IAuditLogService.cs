namespace UTH.Library.Application.Features.AuditLogs;

public interface IAuditLogService
{
    Task<AuditLogPageResponse> GetPageAsync(AuditLogListQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogDto>> GetEntityHistoryAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(AuditLogListQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogRetentionPolicyDto> GetRetentionPolicyAsync(CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredLogsAsync(int retentionDays, CancellationToken cancellationToken = default);
}
