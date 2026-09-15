using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class Publisher { public Guid Id { get; private set; } public string Name { get; private set; } = string.Empty; public string? Address { get; private set; } public string? ContactInfo { get; private set; } public RecordStatus Status { get; private set; } public Guid ConcurrencyToken { get; private set; } }
