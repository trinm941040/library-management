namespace UTH.Library.Application.Features.AuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string? ActorEmail,
    string Action,
    string EntityType,
    Guid EntityId,
    string? BeforeJson,
    string? AfterJson,
    DateTime CreatedAtUtc,
    string? CorrelationId,
    string? IpAddress);

public sealed record AuditLogListQuery(
    string? Search = null,
    Guid? ActorUserId = null,
    string? Action = null,
    string? EntityType = null,
    Guid? EntityId = null,
    string? IpAddress = null,
    string? CorrelationId = null,
    DateTime? FromDateUtc = null,
    DateTime? ToDateUtc = null,
    int PageNumber = 1,
    int PageSize = 20);

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
