using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.AuditLogs;

public sealed class AuditLogFilterRequest
{
    public Guid? ActorUserId { get; init; }
    [StringLength(100)] public string? Action { get; init; }
    [StringLength(100)] public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    [StringLength(100)] public string? CorrelationId { get; init; }
    [StringLength(45)] public string? IpAddress { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    [Range(1, 1_000_000)] public int PageNumber { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
}

public sealed record AuditLogResponse(
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

public sealed record AuditLogPageResponse(
    IReadOnlyList<AuditLogResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
