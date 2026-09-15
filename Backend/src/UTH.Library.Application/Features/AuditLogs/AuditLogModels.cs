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

public sealed record AuditLogListQuery(
    string? Search = null,
    string? Action = null,
    string? EntityType = null,
    Guid? EntityId = null,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? IpAddress = null,
    DateTime? FromDateUtc = null,
    DateTime? ToDateUtc = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string? ActorName,
    string? ActorEmail,
    string Action,
    string EntityType,
    Guid EntityId,
    string? BeforeJson,
    string? AfterJson,
    DateTime CreatedAtUtc,
    string? CorrelationId,
    string? IpAddress);

public sealed record AuditLogPageResponse(
    IReadOnlyList<AuditLogDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record AuditLogRetentionPolicyDto(
    int RetentionDays,
    bool IsImmutable,
    string Description);
