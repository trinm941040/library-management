using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class Category { public Guid Id { get; private set; } public string Name { get; private set; } = string.Empty; public string? Description { get; private set; } public RecordStatus Status { get; private set; } public Guid ConcurrencyToken { get; private set; } }
