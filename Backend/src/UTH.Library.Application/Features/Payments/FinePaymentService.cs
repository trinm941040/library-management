using System.Text.Json;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Payments;

public sealed class FinePaymentService(
    IViolationRepository violations,
    IMemberRepository members,
    TimeProvider timeProvider)
{
    private async Task<Member?> ResolveMemberAsync(Violation violation, CancellationToken cancellationToken)
    {
        return await members.GetByIdAsync(violation.BorrowerId, cancellationToken)
            ?? await members.GetByEmailAsync(violation.BorrowerEmail, cancellationToken);
    }

    public async Task<FinePaymentPreviewResult?> GetPreviewAsync(Guid violationId, CancellationToken cancellationToken)
    {
        var violation = await violations.GetByIdAsync(violationId, cancellationToken);
        if (violation is null) return null;

        var member = await ResolveMemberAsync(violation, cancellationToken);
        var (totalAdjusted, totalPaid, balance) = await violations.GetFinanceSummaryAsync(violation.Id, violation.FineAmount, cancellationToken);

        string status;
        if (violation.Resolution == "waived")
            status = "waived";
        else if (violation.Resolution == "paid" || balance == 0)
            status = "paid";
        else if (totalPaid > 0)
            status = "partially_paid";
        else
            status = "open";

        return new FinePaymentPreviewResult(
            violation.Id,
            violation.Type,
            violation.BookTitle,
            member?.Id ?? violation.BorrowerId,
            violation.BorrowerName,
            member?.MemberCode,
            violation.BorrowerEmail,
            violation.FineAmount,
            totalAdjusted,
            totalPaid,
            balance,
            SuggestedAmount: balance,
            IsOpen: violation.IsOpen && balance > 0,
            Status: status);
    }

    public async Task<FinePaymentResult> CreatePaymentAsync(CreateFinePaymentCommand command, CancellationToken cancellationToken)
    {
        if (command.Amount <= 0)
            return FinePaymentResult.Fail(FinePaymentFailure.Validation, "Số tiền thanh toán phải lớn hơn 0.");

        var violation = await violations.GetByIdAsync(command.ViolationId, cancellationToken);
        if (violation is null)
            return FinePaymentResult.Fail(FinePaymentFailure.NotFound, "Không tìm thấy thông tin vi phạm.");

        if (violation.Resolution == "waived")
            return FinePaymentResult.Fail(FinePaymentFailure.Conflict, "Vi phạm này đã được miễn giảm toàn bộ.");

        var (totalAdjusted, totalPaid, currentBalance) = await violations.GetFinanceSummaryAsync(
            violation.Id,
            violation.FineAmount,
            cancellationToken);

        if (currentBalance <= 0)
            return FinePaymentResult.Fail(FinePaymentFailure.Conflict, "Vi phạm này đã được thanh toán toàn bộ.");

        if (command.Amount > currentBalance)
            return FinePaymentResult.Fail(FinePaymentFailure.Validation, $"Số tiền thanh toán ({command.Amount:N0} đ) vượt quá số dư còn lại ({currentBalance:N0} đ).");

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var reference = !string.IsNullOrWhiteSpace(command.Reference)
            ? command.Reference.Trim()
            : (!string.IsNullOrWhiteSpace(command.IdempotencyKey)
                ? command.IdempotencyKey.Trim()
                : $"PMT-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}");

        // Chống submit lặp / Idempotency check
        var windowStart = now.AddSeconds(-60);
        var existing = await violations.GetExistingPaymentAsync(
            command.ViolationId,
            reference,
            command.Amount,
            windowStart,
            cancellationToken);

        var member = await ResolveMemberAsync(violation, cancellationToken);
        if (member is null)
            return FinePaymentResult.Fail(FinePaymentFailure.NotFound, "Không tìm thấy độc giả hợp lệ để ghi nhận thanh toán.");

        if (existing is not null)
        {
            var isPaid = currentBalance <= 0 || violation.Resolution == "paid";
            return FinePaymentResult.Success(new FinePaymentReceiptModel(
                existing.Id,
                violation.Id,
                violation.Type,
                violation.BookTitle,
                member.Id,
                violation.BorrowerName,
                member?.MemberCode,
                existing.Amount,
                currentBalance + existing.Amount,
                currentBalance,
                existing.Method.ToString(),
                existing.Reference,
                existing.PaidAtUtc,
                existing.ReceivedByUserId,
                command.ActorUserName,
                isPaid));
        }

        try
        {
            var payment = FinePayment.Create(
                member.Id,
                violation.Id,
                command.Amount,
                command.Method,
                reference,
                now,
                command.ActorUserId);

            await violations.AddPaymentAsync(payment, cancellationToken);

            var remainingBalance = currentBalance - command.Amount;
            var isFullyPaid = remainingBalance <= 0;

            if (isFullyPaid && violation.IsOpen)
            {
                violation.MarkPaid(now);
            }

            var auditLog = AuditLog.Create(
                command.ActorUserId,
                "fine-payment.recorded",
                "FinePayment",
                payment.Id,
                JsonSerializer.Serialize(new
                {
                    ViolationId = violation.Id,
                    PreviousBalance = currentBalance,
                    ViolationStatus = violation.IsOpen ? "open" : "resolved"
                }),
                JsonSerializer.Serialize(new
                {
                    PaymentId = payment.Id,
                    ViolationId = violation.Id,
                    Amount = payment.Amount,
                    Method = payment.Method.ToString(),
                    Reference = payment.Reference,
                    RemainingBalance = remainingBalance,
                    ViolationStatus = isFullyPaid ? "paid" : "partially_paid",
                    PaidAtUtc = now
                }),
                now);

            await violations.AddAuditLogAsync(auditLog, cancellationToken);
            await violations.SaveChangesAsync(cancellationToken);

            var receipt = new FinePaymentReceiptModel(
                payment.Id,
                violation.Id,
                violation.Type,
                violation.BookTitle,
                member.Id,
                violation.BorrowerName,
                member?.MemberCode,
                payment.Amount,
                currentBalance,
                remainingBalance,
                payment.Method.ToString(),
                payment.Reference,
                payment.PaidAtUtc,
                payment.ReceivedByUserId,
                command.ActorUserName,
                isFullyPaid);

            return FinePaymentResult.Success(receipt);
        }
        catch (InvalidOperationException ex)
        {
            return FinePaymentResult.Fail(FinePaymentFailure.Conflict, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return FinePaymentResult.Fail(FinePaymentFailure.Validation, ex.Message);
        }
        catch (OptimisticConcurrencyException)
        {
            return FinePaymentResult.Fail(FinePaymentFailure.Conflict, "Khoản phạt vừa được cập nhật bởi thao tác khác. Vui lòng tải lại số dư.");
        }
    }

    public async Task<FinePaymentReceiptModel?> GetReceiptAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await violations.GetPaymentByIdAsync(paymentId, cancellationToken);
        if (payment is null) return null;

        var violation = await violations.GetByIdAsync(payment.ViolationId, cancellationToken);
        var member = await members.GetByIdAsync(payment.MemberId, cancellationToken);

        var (totalAdjusted, totalPaid, balance) = violation is not null
            ? await violations.GetFinanceSummaryAsync(violation.Id, violation.FineAmount, cancellationToken)
            : (0m, payment.Amount, 0m);

        return new FinePaymentReceiptModel(
            payment.Id,
            payment.ViolationId,
            violation?.Type ?? "other",
            violation?.BookTitle ?? string.Empty,
            payment.MemberId,
            violation?.BorrowerName ?? member?.FullName ?? string.Empty,
            member?.MemberCode,
            payment.Amount,
            balance + payment.Amount,
            balance,
            payment.Method.ToString(),
            payment.Reference,
            payment.PaidAtUtc,
            payment.ReceivedByUserId,
            null,
            balance <= 0);
    }

    public async Task<FinePaymentPageModel> GetPageAsync(FinePaymentListQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await violations.GetPaymentsPageAsync(
            query.Search,
            query.ViolationId,
            query.MemberId,
            query.Method,
            query.FromDate,
            query.ToDate,
            pageNumber,
            pageSize,
            cancellationToken);

        var receiptList = new List<FinePaymentReceiptModel>(items.Count);
        foreach (var payment in items)
        {
            var violation = await violations.GetByIdAsync(payment.ViolationId, cancellationToken);
            var member = await members.GetByIdAsync(payment.MemberId, cancellationToken);

            var (_, _, balance) = violation is not null
                ? await violations.GetFinanceSummaryAsync(violation.Id, violation.FineAmount, cancellationToken)
                : (0m, 0m, 0m);

            receiptList.Add(new FinePaymentReceiptModel(
                payment.Id,
                payment.ViolationId,
                violation?.Type ?? "other",
                violation?.BookTitle ?? string.Empty,
                payment.MemberId,
                violation?.BorrowerName ?? member?.FullName ?? string.Empty,
                member?.MemberCode,
                payment.Amount,
                balance + payment.Amount,
                balance,
                payment.Method.ToString(),
                payment.Reference,
                payment.PaidAtUtc,
                payment.ReceivedByUserId,
                null,
                balance <= 0));
        }

        return new FinePaymentPageModel(receiptList, pageNumber, pageSize, totalCount);
    }
}
