using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Violations;

public sealed class ViolationService(
    IViolationRepository violations,
    IBookRepository books,
    IUserManagementService users,
    TimeProvider timeProvider)
{
    public async Task<ViolationPageModel> GetAsync(ViolationListQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, totalCount) = await violations.GetPageAsync(
            query.Search,
            query.Status,
            pageNumber,
            pageSize,
            cancellationToken);

        return new ViolationPageModel(items.Select(Map).ToArray(), pageNumber, pageSize, totalCount);
    }

    public async Task<ViolationResult> CreateAsync(CreateViolationCommand command, CancellationToken cancellationToken)
    {
        var borrower = await users.GetByIdAsync(command.BorrowerId, cancellationToken);
        if (borrower is null || !borrower.IsActive)
            return ViolationResult.Fail(ViolationFailure.NotFound, "Borrower was not found.");

        string bookTitle = string.Empty;
        Guid? bookId = command.BookId is Guid id && id != Guid.Empty ? id : null;
        if (bookId is not null)
        {
            var book = await books.GetByIdAsync(bookId.Value, cancellationToken);
            if (book is null)
                return ViolationResult.Fail(ViolationFailure.NotFound, "Book was not found.");
            bookTitle = book.Title;
        }

        try
        {
            var violation = Violation.Create(
                borrower.Id,
                borrower.DisplayName,
                borrower.Email,
                bookId,
                bookTitle,
                command.Type,
                command.Note,
                command.FineAmount,
                timeProvider.GetUtcNow().UtcDateTime);
            await violations.AddAsync(violation, cancellationToken);
            await violations.SaveChangesAsync(cancellationToken);
            return ViolationResult.Success(Map(violation));
        }
        catch (ArgumentException exception)
        {
            return ViolationResult.Fail(ViolationFailure.Validation, exception.Message);
        }
    }

    public Task<ViolationResult> PayAsync(Guid id, CancellationToken cancellationToken) =>
        ResolveAsync(id, violation => violation.MarkPaid(timeProvider.GetUtcNow().UtcDateTime), cancellationToken);

    public Task<ViolationResult> WaiveAsync(Guid id, CancellationToken cancellationToken) =>
        ResolveAsync(id, violation => violation.MarkWaived(timeProvider.GetUtcNow().UtcDateTime), cancellationToken);

    private async Task<ViolationResult> ResolveAsync(
        Guid id,
        Action<Violation> resolve,
        CancellationToken cancellationToken)
    {
        var violation = await violations.GetByIdAsync(id, cancellationToken);
        if (violation is null)
            return ViolationResult.Fail(ViolationFailure.NotFound, "Violation was not found.");

        try
        {
            resolve(violation);
            await violations.SaveChangesAsync(cancellationToken);
            return ViolationResult.Success(Map(violation));
        }
        catch (InvalidOperationException exception)
        {
            return ViolationResult.Fail(ViolationFailure.Conflict, exception.Message);
        }
    }

    private static ViolationModel Map(Violation violation) =>
        new(
            violation.Id,
            violation.BorrowerId,
            violation.BorrowerName,
            violation.BorrowerEmail,
            violation.BookId,
            violation.BookTitle,
            violation.Type,
            violation.Note,
            violation.FineAmount,
            violation.RecordedAtUtc,
            violation.ResolvedAtUtc,
            violation.IsOpen ? "open" : violation.Resolution ?? "resolved");
}
