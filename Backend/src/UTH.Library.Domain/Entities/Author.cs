using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class Author
{
    private Author(Guid id, string fullName)
    {
        Id = id;
        FullName = fullName;
        Status = RecordStatus.Active;
        ConcurrencyToken = Guid.NewGuid();
    }

    private Author() { }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? Biography { get; private set; }
    public RecordStatus Status { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static Author Create(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Author name is required.", nameof(fullName));
        return new Author(Guid.NewGuid(), fullName.Trim());
    }
}
