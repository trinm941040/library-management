namespace UTH.Library.Domain.Entities;

public sealed class Branch
{
    public static readonly Guid MainBranchId = new("40000000-0000-0000-0000-000000000001");

    private Branch()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public static Branch Create(string code, string name, string? address, DateTime now)
    {
        var branch = new Branch { Id = Guid.NewGuid(), CreatedAtUtc = now, IsActive = false };
        branch.Update(code, name, address, now);
        return branch;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Update(string code, string name, string? address, DateTime now)
    {
        Code = Required(code, nameof(code), 30).ToUpperInvariant();
        Name = Required(name, nameof(name), 150);
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        UpdatedAtUtc = now;
    }

    public void Activate(DateTime now)
    {
        IsActive = true;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        return normalized;
    }
}
