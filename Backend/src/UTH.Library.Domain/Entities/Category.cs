using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class Category
{
    private Category(Guid id, string name)
    {
        Id = id;
        Name = name;
        Status = RecordStatus.Active;
        ConcurrencyToken = Guid.NewGuid();
    }

    private Category() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public RecordStatus Status { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static Category Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));
        return new Category(Guid.NewGuid(), name.Trim());
    }
}
