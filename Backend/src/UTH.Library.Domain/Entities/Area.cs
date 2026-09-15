namespace UTH.Library.Domain.Entities;
public sealed class Area { public Guid Id { get; private set; } public Guid BranchId { get; private set; } public string Code { get; private set; } = string.Empty; public string Name { get; private set; } = string.Empty; public Guid ConcurrencyToken { get; private set; } }
