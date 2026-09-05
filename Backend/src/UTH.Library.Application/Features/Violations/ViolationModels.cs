namespace UTH.Library.Application.Features.Violations;

public sealed record ViolationModel(
    Guid Id,
    Guid BorrowerId,
    string BorrowerName,
    string BorrowerEmail,
    Guid? BookId,
    string BookTitle,
    string Type,
    string Note,
    decimal FineAmount,
    DateTime RecordedAtUtc,
    DateTime? ResolvedAtUtc,
    string Status);

public sealed record ViolationListQuery(string? Search, string? Status, int PageNumber, int PageSize);

public sealed record ViolationPageModel(
    IReadOnlyList<ViolationModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record CreateViolationCommand(
    Guid BorrowerId,
    Guid? BookId,
    string Type,
    string Note,
    decimal FineAmount);

public enum ViolationFailure
{
    None,
    NotFound,
    Conflict,
    Validation
}

public sealed record ViolationResult(
    bool Succeeded,
    ViolationFailure Failure,
    ViolationModel? Violation,
    IReadOnlyList<string> Errors)
{
    public static ViolationResult Success(ViolationModel violation) =>
        new(true, ViolationFailure.None, violation, []);

    public static ViolationResult Fail(ViolationFailure failure, params string[] errors) =>
        new(false, failure, null, errors);
}
