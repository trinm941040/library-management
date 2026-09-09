using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class Shelf { public Guid Id { get; private set; } public Guid AreaId { get; private set; } public string Code { get; private set; } = string.Empty; public string Label { get; private set; } = string.Empty; public ShelfStatus Status { get; private set; } public Guid ConcurrencyToken { get; private set; } }
