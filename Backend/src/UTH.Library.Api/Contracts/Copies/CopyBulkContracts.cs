using System.ComponentModel.DataAnnotations;
using UTH.Library.Domain.Enums;
namespace UTH.Library.Api.Contracts.Copies;
public sealed record CopyBulkRow(Guid CopyId, Guid? ConcurrencyToken, CopyStatus? Status = null, Guid? ShelfId = null, string? Reason = null);
public sealed record CopyBulkRequest(IReadOnlyCollection<CopyBulkRow> Rows);
public sealed record CopyBulkResult(Guid CopyId, bool Succeeded, string? Error, Guid? ConcurrencyToken);
public sealed record CopyImportRow([Required] string Barcode, Guid BookId, Guid? ShelfId, CopyCondition Condition);
public sealed record CopyImportPreviewRequest(IReadOnlyCollection<CopyImportRow> Rows);
public sealed record CopyImportPreviewRow(int RowNumber, bool Valid, string? Error, string Barcode);
