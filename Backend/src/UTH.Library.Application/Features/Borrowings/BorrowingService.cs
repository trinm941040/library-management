using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Borrowings;

public sealed class BorrowingService(
    IBorrowingRepository borrowings,
    IBookRepository books,
    IMemberRepository members,
    TimeProvider timeProvider)
{
    public async Task<BorrowingPageModel> GetAsync(BorrowingListQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var (items, totalCount) = await borrowings.GetPageAsync(
            query.Search,
            query.Status,
            pageNumber,
            pageSize,
            now,
            cancellationToken);

        var models = new List<BorrowingModel>(items.Count);
        foreach (var item in items)
            models.Add(await MapAsync(item, cancellationToken));

        return new BorrowingPageModel(models, pageNumber, pageSize, totalCount);
    }

    public async Task<BorrowingModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var borrowing = await borrowings.GetByIdAsync(id, cancellationToken);
        return borrowing is null ? null : await MapAsync(borrowing, cancellationToken);
    }

    public async Task<BorrowingResult> CreateAsync(CreateBorrowingCommand command, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(command.BookId, cancellationToken);
        if (book is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Book was not found.");

        var borrower = await members.GetByIdAsync(command.BorrowerId, cancellationToken);
        if (borrower is null || borrower.Status != MemberStatus.Active)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Borrower was not found.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (borrower.MembershipCard is null || borrower.MembershipCard.Status != MembershipCardStatus.Active || borrower.MembershipCard.ExpiresOn < DateOnly.FromDateTime(now))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Borrower's membership card is not active.");
        if (borrower.Restrictions.Any(x => x.RemovedAtUtc is null && x.StartsAtUtc <= now && (x.EndsAtUtc is null || x.EndsAtUtc > now) && x.Type is MemberRestrictionType.Borrowing or MemberRestrictionType.AllTransactions))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Borrower has an active borrowing restriction.");
        var history = await members.GetHistoryAsync(borrower.Id, cancellationToken);
        if (history.Borrowings.Count(x => !x.IsReturned) >= borrower.BorrowingLimit)
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "Borrowing limit has been reached.");

        if (await borrowings.HasActiveBorrowingAsync(command.BookId, command.BorrowerId, cancellationToken))
            return BorrowingResult.Fail(BorrowingFailure.Conflict, "This borrower already has this book on loan.");

        try
        {
            var loanDays = command.LoanDays <= 0 ? borrower.LoanPeriodDays : command.LoanDays;
            if (loanDays > borrower.LoanPeriodDays)
                return BorrowingResult.Fail(BorrowingFailure.Validation, "Loan period exceeds the member's allowed duration.");
            book.Checkout(now);
            var borrowing = Borrowing.Create(
                book.Id,
                borrower.Id,
                borrower.FullName,
                borrower.Email,
                now,
                loanDays);
            await borrowings.AddAsync(borrowing, cancellationToken);
            await borrowings.SaveChangesAsync(cancellationToken);
            return BorrowingResult.Success(ToModel(borrowing, book.Title, now));
        }
        catch (InvalidOperationException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Validation, exception.Message);
        }
    }

    public async Task<BorrowingResult> ReturnAsync(Guid id, CancellationToken cancellationToken)
    {
        var borrowing = await borrowings.GetByIdAsync(id, cancellationToken);
        if (borrowing is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Borrowing was not found.");

        var book = await books.GetByIdAsync(borrowing.BookId, cancellationToken);
        if (book is null)
            return BorrowingResult.Fail(BorrowingFailure.NotFound, "Book was not found.");

        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            borrowing.MarkReturned(now);
            book.CheckIn(now);
            await borrowings.SaveChangesAsync(cancellationToken);
            return BorrowingResult.Success(ToModel(borrowing, book.Title, now));
        }
        catch (InvalidOperationException exception)
        {
            return BorrowingResult.Fail(BorrowingFailure.Conflict, exception.Message);
        }
    }

    private async Task<BorrowingModel> MapAsync(Borrowing borrowing, CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(borrowing.BookId, cancellationToken);
        return ToModel(borrowing, book?.Title ?? "Unknown book", timeProvider.GetUtcNow().UtcDateTime);
    }

    private static BorrowingModel ToModel(Borrowing borrowing, string bookTitle, DateTime utcNow)
    {
        var status = borrowing.IsReturned
            ? "returned"
            : borrowing.IsOverdue(utcNow)
                ? "overdue"
                : "borrowed";

        return new BorrowingModel(
            borrowing.Id,
            borrowing.BookId,
            bookTitle,
            borrowing.BorrowerId,
            borrowing.BorrowerName,
            borrowing.BorrowerEmail,
            borrowing.BorrowedAtUtc,
            borrowing.DueAtUtc,
            borrowing.ReturnedAtUtc,
            status);
    }
}
