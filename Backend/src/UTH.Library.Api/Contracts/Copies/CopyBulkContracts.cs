using UTH.Library.Domain.Enums;
namespace UTH.Library.Api.Contracts.Copies;
public sealed record CopyBulkRow(Guid CopyId, Guid ConcurrencyToken, CopyStatus? Status = null,
    CopyCondition? Condition = null, Guid? ShelfId = null, string? Reason = null);
public sealed record CopyBulkRequest(IReadOnlyList<CopyBulkRow> Rows);
public sealed record CopyBulkResult(Guid CopyId, bool Succeeded, string? Error, CopyResponse? Copy);
public sealed record CopyImportRow(string Barcode, string Isbn, string ShelfCode, CopyCondition Condition = CopyCondition.Good);
public sealed record CopyImportRequest(IReadOnlyList<CopyImportRow> Rows);
public sealed record CopyImportPreviewRow(int RowNumber, string Barcode, bool Valid, string? Error);
