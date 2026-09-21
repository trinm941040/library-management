using System.ComponentModel.DataAnnotations;
using UTH.Library.Application.Features.Reports;

namespace UTH.Library.Api.Contracts.Reports;

public sealed record ReportDefinitionResponse(
    string Code,
    string Name,
    string Description,
    string Type,
    IReadOnlyList<ReportColumnDefinition> Columns,
    IReadOnlyList<ReportFilterFieldDefinition> FilterFields);

public sealed record ReportPreviewApiRequest(
    [Required] string ReportCode,
    Dictionary<string, string?>? Filters = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortField = null,
    bool SortAscending = true,
    int? TimezoneOffsetMinutes = 0);

public sealed record ReportExportApiRequest(
    [Required] string ReportCode,
    Dictionary<string, string?>? Filters = null,
    string Format = "csv",
    string? SortField = null,
    bool SortAscending = true,
    int? TimezoneOffsetMinutes = 0);

public sealed record SavedFilterResponse(
    Guid Id,
    string Name,
    string Scope,
    string Criteria,
    string? Sort,
    DateTime CreatedAtUtc);

public sealed record CreateSavedFilterRequest(
    [Required, StringLength(150, MinimumLength = 1)] string Name,
    [Required, StringLength(100, MinimumLength = 1)] string Scope,
    string Criteria = "{}",
    string? Sort = null);

public sealed record UpdateSavedFilterRequest(
    [Required, StringLength(150, MinimumLength = 1)] string Name,
    string Criteria = "{}",
    string? Sort = null);
