using System.Text.Json;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Features.Adjustments;

public sealed class FineAdjustmentService(
    IViolationRepository violations,
    TimeProvider timeProvider)
{
    public async Task<FineAdjustmentPreviewResult?> GetPreviewAsync(
        Guid violationId,
        decimal amountDelta,
        CancellationToken cancellationToken)
    {
        var violation = await violations.GetByIdAsync(violationId, cancellationToken);
        if (violation is null)
            return null;

        var totalAdjusted = await violations.GetTotalAdjustedAsync(violationId, cancellationToken);
        var totalPaid = await violations.GetTotalPaidAsync(violationId, cancellationToken);
        var currentBalance = Math.Max(0m, violation.FineAmount + totalAdjusted - totalPaid);

        var projectedBalance = currentBalance + amountDelta;
        var isAllowed = violation.IsOpen && projectedBalance >= 0;
        string? validationMessage = null;

        if (!violation.IsOpen)
        {
            validationMessage = "Vi phạm này đã được giải quyết hoặc miễn toàn bộ.";
        }
        else if (projectedBalance < 0)
        {
            validationMessage = $"Số tiền giảm vượt quá số dư hiện tại ({currentBalance:N0} VNĐ).";
        }

        var projectedStatus = !violation.IsOpen
            ? (violation.Resolution ?? "resolved")
            : projectedBalance == 0
                ? "waived"
                : totalPaid > 0
                    ? "partially_paid"
                    : "open";

        return new FineAdjustmentPreviewResult(
            violation.Id,
            violation.FineAmount,
            totalAdjusted,
            totalPaid,
            currentBalance,
            amountDelta,
            Math.Max(0m, projectedBalance),
            projectedStatus,
            isAllowed,
            validationMessage);
    }

    public async Task<FineAdjustmentResult> AdjustAsync(
        CreateFineAdjustmentCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.Validation, "Lý do điều chỉnh là bắt buộc.");

        if (command.AmountDelta == 0)
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.Validation, "Số tiền điều chỉnh không được bằng 0.");

        var violation = await violations.GetByIdAsync(command.ViolationId, cancellationToken);
        if (violation is null)
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.NotFound, "Không tìm thấy thông tin vi phạm.");

        if (!violation.IsOpen)
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.Conflict, "Vi phạm này đã được đóng (đã thanh toán hoặc đã được miễn).");

        var totalAdjusted = await violations.GetTotalAdjustedAsync(violation.Id, cancellationToken);
        var totalPaid = await violations.GetTotalPaidAsync(violation.Id, cancellationToken);
        var currentBalance = Math.Max(0m, violation.FineAmount + totalAdjusted - totalPaid);
        var projectedBalance = currentBalance + command.AmountDelta;

        if (projectedBalance < 0)
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.Validation, $"Số tiền giảm vượt quá số dư nợ còn lại ({currentBalance:N0} VNĐ).");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var adjustment = FineAdjustment.Create(
            violation.BorrowerId,
            violation.Id,
            command.AmountDelta,
            command.Reason.Trim(),
            now,
            command.ActorUserId);

        var beforeState = JsonSerializer.Serialize(new
        {
            violation.FineAmount,
            TotalAdjusted = totalAdjusted,
            TotalPaid = totalPaid,
            Balance = currentBalance,
            Status = violation.IsOpen ? (totalPaid > 0 ? "partially_paid" : "open") : (violation.Resolution ?? "resolved")
        });

        if (projectedBalance == 0)
        {
            violation.MarkWaived(now);
        }

        var newTotalAdjusted = totalAdjusted + command.AmountDelta;
        var newStatus = !violation.IsOpen
            ? (violation.Resolution ?? "waived")
            : (totalPaid > 0 ? "partially_paid" : "open");

        var afterState = JsonSerializer.Serialize(new
        {
            violation.FineAmount,
            TotalAdjusted = newTotalAdjusted,
            TotalPaid = totalPaid,
            Balance = projectedBalance,
            Status = newStatus,
            AdjustmentId = adjustment.Id,
            AmountDelta = command.AmountDelta,
            Reason = command.Reason.Trim(),
            AdjustedByUserId = command.ActorUserId,
            AdjustedAtUtc = now
        });

        var actionName = projectedBalance == 0 && command.AmountDelta < 0 ? "violation.waived" : "violation.adjusted";
        var auditLog = AuditLog.Create(
            command.ActorUserId,
            actionName,
            nameof(Violation),
            violation.Id,
            beforeState,
            afterState,
            now);

        await violations.AddAdjustmentAsync(adjustment, cancellationToken);
        await violations.AddAuditLogAsync(auditLog, cancellationToken);

        try
        {
            await violations.SaveChangesAsync(cancellationToken);
        }
        catch (OptimisticConcurrencyException)
        {
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.Conflict, "Dữ liệu vi phạm đã bị thay đổi bởi thao tác khác (thanh toán hoặc điều chỉnh đồng thời). Vui lòng làm mới trang.");
        }

        return FineAdjustmentResult.Success(
            new FineAdjustmentItemModel(
                adjustment.Id,
                adjustment.MemberId,
                adjustment.ViolationId,
                adjustment.AmountDelta,
                adjustment.Reason,
                adjustment.AdjustedAtUtc,
                adjustment.AdjustedByUserId),
            projectedBalance,
            newStatus);
    }

    public async Task<FineAdjustmentResult> WaiveAsync(
        WaiveViolationCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.Validation, "Lý do miễn giảm là bắt buộc.");

        var violation = await violations.GetByIdAsync(command.ViolationId, cancellationToken);
        if (violation is null)
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.NotFound, "Không tìm thấy thông tin vi phạm.");

        if (!violation.IsOpen)
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.Conflict, "Vi phạm này đã được đóng (đã thanh toán hoặc đã được miễn).");

        var totalAdjusted = await violations.GetTotalAdjustedAsync(violation.Id, cancellationToken);
        var totalPaid = await violations.GetTotalPaidAsync(violation.Id, cancellationToken);
        var currentBalance = Math.Max(0m, violation.FineAmount + totalAdjusted - totalPaid);

        if (currentBalance <= 0)
            return FineAdjustmentResult.Fail(FineAdjustmentFailure.Validation, "Khoản phạt đã không còn số dư nợ để miễn.");

        // Tạo điều chỉnh giảm bằng đúng số dư nợ còn lại
        return await AdjustAsync(
            new CreateFineAdjustmentCommand(
                command.ViolationId,
                -currentBalance,
                command.Reason.Trim(),
                command.ActorUserId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<FineAdjustmentItemModel>> GetAdjustmentsAsync(
        Guid violationId,
        CancellationToken cancellationToken)
    {
        var list = await violations.GetAdjustmentsByViolationIdAsync(violationId, cancellationToken);
        return list.Select(item => new FineAdjustmentItemModel(
            item.Id,
            item.MemberId,
            item.ViolationId,
            item.AmountDelta,
            item.Reason,
            item.AdjustedAtUtc,
            item.AdjustedByUserId)).ToArray();
    }
}
