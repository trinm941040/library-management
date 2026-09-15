using System.Text.Json;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.CirculationPolicies;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Violations;

public sealed class ViolationService(
    IViolationRepository violations,
    IBookRepository books,
    IMemberRepository members,
    ICirculationPolicyResolver policyResolver,
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
        var borrower = await members.GetByIdAsync(command.BorrowerId, cancellationToken);
        if (borrower is null)
            return ViolationResult.Fail(ViolationFailure.NotFound, "Borrower was not found.");

        string bookTitle = string.Empty;
        string? documentType = null;
        Guid? bookId = command.BookId is Guid id && id != Guid.Empty ? id : null;
        if (bookId is not null)
        {
            var book = await books.GetByIdAsync(bookId.Value, cancellationToken);
            if (book is null)
                return ViolationResult.Fail(ViolationFailure.NotFound, "Book was not found.");
            bookTitle = book.Title;
            documentType = book.Category;
        }

        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var policy = await policyResolver.ResolveAsync(borrower.MemberGroup, documentType, null, now, cancellationToken);
            var fineAmount = command.Type.Trim().ToLowerInvariant() switch
            {
                "overdue" => policyResolver.CalculateFine(policy, command.OverdueDays),
                "lost" => policyResolver.CalculateLostPenalty(policy, command.BookPrice),
                _ when command.FineAmount > 0 && policy.MaxFineAmount > 0 => Math.Min(command.FineAmount, policy.MaxFineAmount),
                _ when command.FineAmount > 0 => command.FineAmount,
                _ => policy.FixedFineAmount
            };
            var violation = Violation.Create(
                borrower.Id,
                borrower.FullName,
                borrower.Email,
                bookId,
                bookTitle,
                command.Type,
                command.Note,
                fineAmount,
                now,
                policy.PolicyId,
                policy.Version,
                JsonSerializer.Serialize(policy));
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
            violation.IsOpen ? "open" : violation.Resolution ?? "resolved",
            violation.AppliedPolicyId,
            violation.AppliedPolicyVersion);
}
