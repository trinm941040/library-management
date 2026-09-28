namespace UTH.Library.Domain.Entities;

public sealed class TodoItem
{
    private TodoItem(Guid id, string title, DateTime createdAtUtc)
    {
        Id = id;
        Title = title;
        CreatedAtUtc = createdAtUtc;
    }

    private TodoItem()
    {
        Title = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static TodoItem Create(string title, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Tiêu đề công việc là bắt buộc.", nameof(title));
        }

        return new TodoItem(Guid.NewGuid(), title.Trim(), createdAtUtc);
    }

    public void MarkCompleted() => IsCompleted = true;
}
