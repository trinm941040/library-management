using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class Shelf
{
    private Shelf() { }

    public Guid Id { get; private set; }
    public Guid AreaId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public ShelfStatus Status { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static Shelf Create(Guid areaId, string code, string label)
    {
        if (areaId == Guid.Empty) throw new ArgumentException("Area is required.", nameof(areaId));
        var shelf = new Shelf
        {
            Id = Guid.NewGuid(),
            AreaId = areaId,
            Status = ShelfStatus.Active,
            ConcurrencyToken = Guid.NewGuid()
        };
        shelf.Update(code, label);
        return shelf;
    }

    public void Update(string code, string label)
    {
        Code = Required(code, nameof(code), 30).ToUpperInvariant();
        Label = Required(label, nameof(label), 150);
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Activate() => SetStatus(ShelfStatus.Active);
    public void Deactivate() => SetStatus(ShelfStatus.Inactive);

    private void SetStatus(ShelfStatus status)
    {
        Status = status;
        ConcurrencyToken = Guid.NewGuid();
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        return normalized;
    }
}
