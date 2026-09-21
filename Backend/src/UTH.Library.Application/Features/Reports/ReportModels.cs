using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Reports;

public sealed record ReportColumnDefinition(
    string Key,
    string Label,
    string Type, // "string" | "number" | "currency" | "datetime" | "badge"
    string? Align = "left");

public sealed record ReportFilterFieldDefinition(
    string Key,
    string Label,
    string Type, // "select" | "date" | "branch" | "daterange"
    IReadOnlyList<ReportFilterOption>? Options = null);

public sealed record ReportFilterOption(
    string Value,
    string Label);

public sealed record ReportDefinition(
    string Code,
    string Name,
    string Description,
    ReportType Type,
    string RequiredPermission,
    IReadOnlyList<ReportColumnDefinition> Columns,
    IReadOnlyList<ReportFilterFieldDefinition> FilterFields);

public sealed record ReportPreviewRequest(
    string ReportCode,
    Dictionary<string, string?>? Filters = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortField = null,
    bool SortAscending = true,
    int? TimezoneOffsetMinutes = 0);

public sealed record ReportPreviewResult(
    string ReportCode,
    string ReportName,
    IReadOnlyList<ReportColumnDefinition> Columns,
    IReadOnlyList<Dictionary<string, object?>> Rows,
    int TotalRows,
    int PageNumber,
    int PageSize,
    int TotalPages,
    Dictionary<string, object?>? SummaryStats,
    DateTime GeneratedAtUtc);

public sealed record ReportExportRequest(
    string ReportCode,
    Dictionary<string, string?>? Filters = null,
    string Format = "csv",
    string? SortField = null,
    bool SortAscending = true,
    int? TimezoneOffsetMinutes = 0);

public sealed record ReportExportResult(
    Guid ReportId,
    string ReportCode,
    string FileName,
    string ContentType,
    int TotalRecords,
    DateTime ExpiresAtUtc,
    string DownloadUrl);

public sealed record ReportDownloadResult(
    string FileName,
    string ContentType,
    byte[] FileBytes);

public sealed record SavedFilterDto(
    Guid Id,
    string Name,
    string Scope,
    string Criteria,
    string? Sort,
    DateTime CreatedAtUtc);

public sealed record CreateSavedFilterCommand(
    string Name,
    string Scope,
    string Criteria,
    string? Sort);

public sealed record UpdateSavedFilterCommand(
    string Name,
    string Criteria,
    string? Sort);
