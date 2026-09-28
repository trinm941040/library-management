namespace UTH.Library.Domain.Entities;

public sealed class Branch
{
    public static readonly Guid MainBranchId = new("40000000-0000-0000-0000-000000000001");

    private Branch()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public static Branch Create(string code, string name, string? address, DateTime createdAtUtc)
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            IsActive = false,
            CreatedAtUtc = EnsureUtc(createdAtUtc),
            ConcurrencyToken = Guid.NewGuid()
        };
        branch.Update(code, name, address, createdAtUtc);
        return branch;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public void Update(string code, string name, string? address, DateTime updatedAtUtc)
    {
        Code = Required(code, nameof(code), 30).ToUpperInvariant();
        Name = Required(name, nameof(name), 150);
        Address = Optional(address, nameof(address), 500);
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Activate(DateTime updatedAtUtc) => SetActive(true, updatedAtUtc);

    public void Deactivate(DateTime updatedAtUtc) => SetActive(false, updatedAtUtc);

    private void SetActive(bool isActive, DateTime updatedAtUtc)
    {
        IsActive = isActive;
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        ConcurrencyToken = Guid.NewGuid();
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Giá trị là bắt buộc.", parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"Giá trị không được vượt quá {maxLength} ký tự.", parameterName);
        return normalized;
    }

    private static string? Optional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"Giá trị không được vượt quá {maxLength} ký tự.", parameterName);
        return normalized;
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
