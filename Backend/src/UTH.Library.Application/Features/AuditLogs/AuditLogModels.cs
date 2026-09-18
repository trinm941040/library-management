namespace UTH.Library.Application.Features.AuditLogs;

public sealed record AuditLogQuery(
    Guid? ActorUserId,
    string? Action,
    string? EntityType,
    Guid? EntityId,
    string? CorrelationId,
    string? IpAddress,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int PageNumber,
    int PageSize);

public sealed record AuditLogModel(
    Guid Id,
    Guid? ActorUserId,
    string? ActorName,
    string Action,
    string EntityType,
    Guid EntityId,
    string? BeforeJson,
    string? AfterJson,
    DateTime CreatedAtUtc,
    string? CorrelationId,
    string? IpAddress);

public sealed record AuditLogPageModel(
    IReadOnlyList<AuditLogModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
