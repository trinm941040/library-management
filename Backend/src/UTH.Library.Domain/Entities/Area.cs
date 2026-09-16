namespace UTH.Library.Domain.Entities;

public sealed class Area
{
    private Area() { }

    public Guid Id { get; private set; }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static Area Create(Guid branchId, string code, string name)
    {
        if (branchId == Guid.Empty) throw new ArgumentException("Branch is required.", nameof(branchId));
        var area = new Area
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            IsActive = true,
            ConcurrencyToken = Guid.NewGuid()
        };
        area.Update(code, name);
        return area;
    }

    public void Update(string code, string name)
    {
        Code = Required(code, nameof(code), 30).ToUpperInvariant();
        Name = Required(name, nameof(name), 150);
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Activate() => SetActive(true);
    public void Deactivate() => SetActive(false);

    private void SetActive(bool isActive)
    {
        IsActive = isActive;
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
