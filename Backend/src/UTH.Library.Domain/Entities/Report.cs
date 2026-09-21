using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class Report
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ReportType Type { get; private set; }
    public string Definition { get; private set; } = "{}";
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    private Report() { }

    public static Report Create(
        string code,
        string name,
        ReportType type,
        string definition,
        Guid createdByUserId,
        DateTime now)
    {
        return new Report
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Type = type,
            Definition = definition,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now,
            ConcurrencyToken = Guid.NewGuid()
        };
    }
}
