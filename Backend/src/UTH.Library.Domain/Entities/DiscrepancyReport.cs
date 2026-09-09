using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class DiscrepancyReport { public Guid Id { get; private set; } public Guid StockReceiptId { get; private set; } public DiscrepancyType Type { get; private set; } public int ExpectedQuantity { get; private set; } public int ActualQuantity { get; private set; } public string Description { get; private set; } = string.Empty; public DateTime CreatedAtUtc { get; private set; } }
