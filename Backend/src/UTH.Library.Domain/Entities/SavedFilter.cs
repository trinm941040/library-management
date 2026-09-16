namespace UTH.Library.Domain.Entities;

public sealed class SavedFilter
{
    private SavedFilter() { }

    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;
    public string Criteria { get; private set; } = "{}";
    public string? Sort { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static SavedFilter Create(
        Guid ownerUserId,
        string name,
        string scope,
        string criteria,
        string? sort,
        DateTime now)
    {
        return new SavedFilter
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = name.Trim(),
            Scope = scope.Trim().ToLowerInvariant(),
            Criteria = string.IsNullOrWhiteSpace(criteria) ? "{}" : criteria,
            Sort = sort,
            CreatedAtUtc = now
        };
    }

    public void Update(string name, string criteria, string? sort)
    {
        Name = name.Trim();
        Criteria = string.IsNullOrWhiteSpace(criteria) ? "{}" : criteria;
        Sort = sort;
    }
}
