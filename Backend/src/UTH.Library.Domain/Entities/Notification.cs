using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class Notification
{
    private Notification()
    {
        Destination = string.Empty;
        Body = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid TemplateId { get; private set; }
    public RecipientType RecipientType { get; private set; }
    public Guid RecipientId { get; private set; }
    public string Destination { get; private set; }
    public string? Subject { get; private set; }
    public string Body { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTime? ScheduledAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public bool IsRead => ReadAtUtc.HasValue;

    public static Notification Create(
        Guid templateId,
        RecipientType recipientType,
        Guid recipientId,
        string destination,
        string? subject,
        string body,
        DateTime? scheduledAtUtc = null)
    {
        if (templateId == Guid.Empty) throw new ArgumentException("Template ID là bắt buộc.", nameof(templateId));
        if (recipientId == Guid.Empty) throw new ArgumentException("Recipient ID là bắt buộc.", nameof(recipientId));
        if (string.IsNullOrWhiteSpace(destination)) throw new ArgumentException("Destination là bắt buộc.", nameof(destination));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Nội dung thông báo không được để trống.", nameof(body));

        return new Notification
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            RecipientType = recipientType,
            RecipientId = recipientId,
            Destination = destination.Trim(),
            Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim(),
            Body = body.Trim(),
            Status = NotificationStatus.Pending,
            ScheduledAtUtc = scheduledAtUtc.HasValue ? DateTime.SpecifyKind(scheduledAtUtc.Value, DateTimeKind.Utc) : null,
            SentAtUtc = null,
            FailureReason = null,
            ReadAtUtc = null,
            ConcurrencyToken = Guid.NewGuid()
        };
    }

    public void MarkSent(DateTime sentAtUtc)
    {
        Status = NotificationStatus.Sent;
        SentAtUtc = DateTime.SpecifyKind(sentAtUtc, DateTimeKind.Utc);
        FailureReason = null;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void MarkFailed(string failureReason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = failureReason;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void MarkCancelled()
    {
        Status = NotificationStatus.Cancelled;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void MarkRead(DateTime readAtUtc)
    {
        if (!ReadAtUtc.HasValue)
        {
            ReadAtUtc = DateTime.SpecifyKind(readAtUtc, DateTimeKind.Utc);
            ConcurrencyToken = Guid.NewGuid();
        }
    }

    public void Retry()
    {
        Status = NotificationStatus.Pending;
        FailureReason = null;
        ConcurrencyToken = Guid.NewGuid();
    }
}
