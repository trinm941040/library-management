namespace UTH.Library.Domain.Entities;

public sealed class Branch
{
    public static readonly Guid MainBranchId = new("40000000-0000-0000-0000-000000000001");

    private Branch()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
