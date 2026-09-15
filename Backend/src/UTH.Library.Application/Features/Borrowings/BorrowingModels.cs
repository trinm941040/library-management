namespace UTH.Library.Application.Features.Borrowings;

public sealed record BorrowingModel(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    DateTime BorrowedAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    string Status,
    int RenewalCount,
    Guid? AppliedPolicyId,
    int AppliedPolicyVersion);

public sealed record BorrowingListQuery(string? Search, string? Status, int PageNumber, int PageSize);

public sealed record BorrowingPageModel(
    IReadOnlyList<BorrowingModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CreateBorrowingCommand(Guid BookId, Guid BorrowerId, int LoanDays);
public sealed record RenewBorrowingCommand(Guid ActorUserId, Guid ConcurrencyToken);

public enum BorrowingFailure
{
    None,
    NotFound,
    Conflict,
    Validation
}

public sealed record BorrowingResult(
    bool Succeeded,
    BorrowingFailure Failure,
    BorrowingModel? Borrowing,
    IReadOnlyList<string> Errors)
{
    public static BorrowingResult Success(BorrowingModel borrowing) => new(true, BorrowingFailure.None, borrowing, []);
    public static BorrowingResult Fail(BorrowingFailure failure, params string[] errors) =>
        new(false, failure, null, errors);
}
