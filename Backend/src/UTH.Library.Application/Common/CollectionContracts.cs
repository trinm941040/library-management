namespace UTH.Library.Application.Common;

public enum SortDirection
{
    Asc,
    Desc
}

public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record BulkItemResult(Guid Id, bool Succeeded, string? Error = null);

public sealed record BulkResult(
    IReadOnlyList<BulkItemResult> Items,
    string CorrelationId)
{
    public int SucceededCount => Items.Count(item => item.Succeeded);
    public int FailedCount => Items.Count - SucceededCount;
}

public sealed record ImportFieldError(int RowNumber, string Field, string Message);

public sealed record ImportPreview<T>(
    IReadOnlyList<T> Rows,
    IReadOnlyList<ImportFieldError> Errors,
    string Checksum)
{
    public bool CanConfirm => Rows.Count > 0 && Errors.Count == 0;
}

public static class CollectionLimits
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;
    public const int MaximumBulkItems = 100;
    public const int MaximumImportRows = 5_000;
    public const int MaximumExportRows = 50_000;

    public static (int PageNumber, int PageSize) NormalizePage(int pageNumber, int pageSize) =>
        (Math.Max(1, pageNumber), Math.Clamp(pageSize, 1, MaximumPageSize));
}
