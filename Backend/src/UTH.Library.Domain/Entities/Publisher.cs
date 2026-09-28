using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class Publisher
{
    private Publisher(Guid id, string name)
    {
        Id = id;
        Name = name;
        Status = RecordStatus.Active;
        ConcurrencyToken = Guid.NewGuid();
    }

    private Publisher() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? ContactInfo { get; private set; }
    public RecordStatus Status { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static Publisher Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên nhà xuất bản là bắt buộc.", nameof(name));
        return new Publisher(Guid.NewGuid(), name.Trim());
    }
}
