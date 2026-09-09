using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class Author { public Guid Id { get; private set; } public string FullName { get; private set; } = string.Empty; public string? Biography { get; private set; } public RecordStatus Status { get; private set; } public Guid ConcurrencyToken { get; private set; } }
