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
            query.BorrowerId,
            query.Type,
            query.Status,
            query.FromDate,
            query.ToDate,
            query.HasBalanceOnly,
            pageNumber,
            pageSize,
            cancellationToken);

        var models = new List<ViolationModel>(items.Count);
        foreach (var item in items)
        {
            models.Add(await MapAsync(item, cancellationToken));
        }

        return new ViolationPageModel(models, pageNumber, pageSize, totalCount);
    }

    public async Task<ViolationModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await violations.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return null;

        return await MapAsync(item, cancellationToken);
    }

    public async Task<ViolationDetailModel?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var violation = await violations.GetByIdAsync(id, cancellationToken);
        if (violation is null) return null;

        var book = violation.BookId.HasValue ? await books.GetByIdAsync(violation.BookId.Value, cancellationToken) : null;
        var violationModel = await MapAsync(violation, cancellationToken);

        var (payments, adjustments) = await violations.GetFinanceTransactionsAsync(violation.Id, cancellationToken);

        var paymentModels = payments.Select(p => new PaymentHistoryItemModel(
            p.Id,
            p.Amount,
            p.Method.ToString(),
            p.Reference,
            p.PaidAtUtc,
            p.ReceivedByUserId)).ToList();

        var adjustmentModels = adjustments.Select(a => new AdjustmentHistoryItemModel(
            a.Id,
            a.AmountDelta,
            a.Reason,
            a.AdjustedAtUtc,
            a.AdjustedByUserId)).ToList();

        return new ViolationDetailModel(
            violationModel,
            book?.Isbn,
            book?.Author,
            book?.Category,
            violation.ExtractCalculationBasis(),
            paymentModels,
            adjustmentModels);
    }

    public async Task<FinePreviewResult> CalculateFinePreviewAsync(FinePreviewCommand command, CancellationToken cancellationToken)
    {
        var borrower = await members.GetByIdAsync(command.BorrowerId, cancellationToken);
        var memberGroup = borrower?.MemberGroup;

        string? category = null;
        if (command.BookId.HasValue && command.BookId.Value != Guid.Empty)
        {
            var book = await books.GetByIdAsync(command.BookId.Value, cancellationToken);
            category = book?.Category;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var policy = await policyResolver.ResolveAsync(memberGroup, category, null, now, cancellationToken);

        var normType = command.Type.Trim().ToLowerInvariant();
        decimal calculatedFine;
        string formula;

        var dailyRate = policy.FinePerDay > 0 ? policy.FinePerDay : 5000m;
        var maxFine = policy.MaxFineAmount > 0 ? policy.MaxFineAmount : 100000m;
        var lostRatio = policy.LostBookPenaltyRatio > 0 ? policy.LostBookPenaltyRatio : 150m;

        switch (normType)
        {
            case "overdue":
                var overdueDays = Math.Max(1, command.OverdueDays);
                calculatedFine = policyResolver.CalculateFine(policy, overdueDays);
                formula = $"{overdueDays} ngày quá hạn × {dailyRate:N0} đ/ngày (Mức trần: {maxFine:N0} đ)";
                break;

            case "lost":
                var bookPrice = command.BookPrice > 0 ? command.BookPrice : 100000m;
                calculatedFine = policyResolver.CalculateLostPenalty(policy, bookPrice);
                formula = $"Giá sách {bookPrice:N0} đ × {lostRatio}% hệ số đền bù";
                break;

            case "damage":
                if (command.CustomAmount.HasValue && command.CustomAmount.Value > 0)
                {
                    calculatedFine = command.CustomAmount.Value;
                    formula = $"Mức phạt bồi thường theo mức độ: {calculatedFine:N0} đ";
                }
                else if (command.BookPrice > 0)
                {
                    var percent = command.DamageLevel?.ToLowerInvariant() switch
                    {
                        "minor" => 0.2m,
                        "severe" => 0.8m,
                        _ => 0.5m
                    };
                    calculatedFine = Math.Round(command.BookPrice * percent, 2);
                    formula = $"Bồi thường hư hỏng ({percent * 100}% giá sách {command.BookPrice:N0} đ)";
                }
                else
                {
                    calculatedFine = policy.FixedFineAmount > 0 ? policy.FixedFineAmount : 50000m;
                    formula = $"Mức phạt hư hỏng ấn định: {calculatedFine:N0} đ";
                }
                break;

            default:
                calculatedFine = command.CustomAmount.HasValue && command.CustomAmount.Value > 0
                    ? command.CustomAmount.Value
                    : (policy.FixedFineAmount > 0 ? policy.FixedFineAmount : 20000m);
                formula = $"Mức phạt theo quy định: {calculatedFine:N0} đ";
                break;
        }

        return new FinePreviewResult(
            calculatedFine,
            formula,
            "Chính sách lưu thông tiêu chuẩn",
            dailyRate,
            maxFine,
            lostRatio,
            policy.PolicyId,
            policy.Version);
    }

    public async Task<ViolationResult> CreateAsync(CreateViolationCommand command, CancellationToken cancellationToken)
    {
        // Kiểm tra Idempotency chống tạo vi phạm trùng cho cùng sự kiện
        var existing = await violations.GetExistingViolationAsync(command.BorrowingId, command.BookCopyId, command.Type, cancellationToken);
        if (existing is not null)
        {
            return ViolationResult.Success(await MapAsync(existing, cancellationToken));
        }

        var borrower = await members.GetByIdAsync(command.BorrowerId, cancellationToken);
        if (borrower is null)
            return ViolationResult.Fail(ViolationFailure.NotFound, "Không tìm thấy thông tin độc giả.");

        string bookTitle = string.Empty;
        Guid? bookId = command.BookId is Guid id && id != Guid.Empty ? id : null;
        if (bookId is not null)
        {
            var book = await books.GetByIdAsync(bookId.Value, cancellationToken);
            if (book is null)
                return ViolationResult.Fail(ViolationFailure.NotFound, "Không tìm thấy thông tin sách.");
            bookTitle = book.Title;
        }

        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var preview = await CalculateFinePreviewAsync(new FinePreviewCommand(
                command.BorrowerId,
                bookId,
                command.Type,
                command.OverdueDays,
                command.BookPrice,
                command.DamageLevel,
                command.FineAmount > 0 ? command.FineAmount : null), cancellationToken);

            var fineAmount = command.FineAmount > 0 ? command.FineAmount : preview.CalculatedFine;

            var calculationBasis = new
            {
                command.Type,
                command.OverdueDays,
                command.BookPrice,
                command.DamageLevel,
                preview.DailyRate,
                preview.MaxFine,
                preview.LostRatio,
                preview.Formula,
                CalculatedAmount = fineAmount,
                CalculatedAtUtc = now
            };

            var snapshotObject = new
            {
                command.BorrowingId,
                command.BookCopyId,
                CalculationBasis = calculationBasis,
                Policy = new
                {
                    preview.PolicyId,
                    preview.PolicyVersion,
                    preview.PolicyName
                }
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
                preview.PolicyId,
                preview.PolicyVersion,
                JsonSerializer.Serialize(snapshotObject));

            await violations.AddAsync(violation, cancellationToken);

            var auditLog = AuditLog.Create(
                command.ActorUserId ?? borrower.Id,
                "violation.created",
                "Violation",
                violation.Id,
                null,
                JsonSerializer.Serialize(new
                {
                    violation.Id,
                    violation.BorrowerId,
                    violation.BookId,
                    violation.Type,
                    violation.FineAmount,
                    violation.Note,
                    command.BorrowingId,
                    command.BookCopyId
                }),
                now);
            await violations.AddAuditLogAsync(auditLog, cancellationToken);

            await violations.SaveChangesAsync(cancellationToken);
            return ViolationResult.Success(await MapAsync(violation, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return ViolationResult.Fail(ViolationFailure.Validation, exception.Message);
        }
    }

    public Task<ViolationResult> PayAsync(Guid id, CancellationToken cancellationToken) =>
        ResolveAsync(id, violation => violation.MarkPaid(timeProvider.GetUtcNow().UtcDateTime), "violation.paid", cancellationToken);

    public Task<ViolationResult> WaiveAsync(Guid id, CancellationToken cancellationToken) =>
        ResolveAsync(id, violation => violation.MarkWaived(timeProvider.GetUtcNow().UtcDateTime), "violation.waived", cancellationToken);

    private async Task<ViolationResult> ResolveAsync(
        Guid id,
        Action<Violation> resolve,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var violation = await violations.GetByIdAsync(id, cancellationToken);
        if (violation is null)
            return ViolationResult.Fail(ViolationFailure.NotFound, "Không tìm thấy bản ghi vi phạm.");

        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var beforeJson = JsonSerializer.Serialize(new
            {
                violation.Id,
                violation.FineAmount,
                violation.ResolvedAtUtc,
                violation.Resolution,
                violation.ConcurrencyToken
            });

            resolve(violation);

            var afterJson = JsonSerializer.Serialize(new
            {
                violation.Id,
                violation.FineAmount,
                violation.ResolvedAtUtc,
                violation.Resolution,
                violation.ConcurrencyToken
            });

            var auditLog = AuditLog.Create(
                null,
                auditAction,
                "Violation",
                violation.Id,
                beforeJson,
                afterJson,
                now);
            await violations.AddAuditLogAsync(auditLog, cancellationToken);

            await violations.SaveChangesAsync(cancellationToken);
            return ViolationResult.Success(await MapAsync(violation, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return ViolationResult.Fail(ViolationFailure.Conflict, exception.Message);
        }
    }

    private async Task<ViolationModel> MapAsync(Violation violation, CancellationToken cancellationToken)
    {
        var (totalAdjusted, totalPaid, balance) = await violations.GetFinanceSummaryAsync(
            violation.Id,
            violation.FineAmount,
            cancellationToken);

        var member = await members.GetByIdAsync(violation.BorrowerId, cancellationToken);

        // Derive consistent status
        string status;
        if (violation.Resolution == "waived" || (!violation.IsOpen && violation.Resolution == "waived"))
            status = "waived";
        else if (violation.Resolution == "paid" || balance == 0)
            status = "paid";
        else if (totalPaid > 0)
            status = "partially_paid";
        else
            status = "open";

        return new ViolationModel(
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
            status,
            violation.AppliedPolicyId,
            violation.AppliedPolicyVersion,
            totalAdjusted,
            totalPaid,
            balance,
            violation.ExtractBorrowingId(),
            violation.ExtractBookCopyId(),
            violation.ExtractBookCopyBarcode(),
            member?.MemberCode,
            member?.MembershipCard?.CardNumber,
            violation.ConcurrencyToken);
    }
}
