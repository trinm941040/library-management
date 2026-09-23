using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; private set; }
    public Guid TemplateId { get; private set; }
    public RecipientType RecipientType { get; private set; }
    public Guid RecipientId { get; private set; }
    public string Destination { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public DateTime? ScheduledAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? EventCode { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public bool IsRead => ReadAtUtc.HasValue;
    public Guid ConcurrencyToken { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid templateId,
        RecipientType recipientType,
        Guid recipientId,
        string destination,
        string? subject,
        string body,
        DateTime? scheduledAtUtc = null,
        string? eventCode = null,
        string? idempotencyKey = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            RecipientType = recipientType,
            RecipientId = recipientId,
            Destination = destination,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            ScheduledAtUtc = scheduledAtUtc,
            EventCode = eventCode?.Trim(),
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey.Trim(),
            NextAttemptAtUtc = scheduledAtUtc,
            ConcurrencyToken = Guid.NewGuid()
        };
    }

    public void MarkSent(DateTime sentAtUtc)
    {
        Status = NotificationStatus.Sent;
        SentAtUtc = sentAtUtc;
        FailureReason = null;
        NextAttemptAtUtc = null;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void MarkFailed(string reason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = reason;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void MarkProcessing()
    {
        if (Status != NotificationStatus.Pending) throw new InvalidOperationException("Email không ở trạng thái chờ gửi.");
        Status = NotificationStatus.Processing;
        AttemptCount++;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void ScheduleRetry(string reason, DateTime nextAttemptAtUtc)
    {
        Status = NotificationStatus.Pending;
        FailureReason = reason;
        NextAttemptAtUtc = nextAttemptAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Cancel(string reason)
    {
        Status = NotificationStatus.Cancelled;
        FailureReason = reason;
        NextAttemptAtUtc = null;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Retry()
    {
        Status = NotificationStatus.Pending;
        FailureReason = null;
        NextAttemptAtUtc = null;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void MarkRead(DateTime readAtUtc)
    {
        ReadAtUtc = readAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }
}
