namespace UTH.Library.Domain.Entities;
public sealed class FinePolicy { public Guid Id { get; private set; } public Guid CirculationPolicyId { get; private set; } public string ViolationType { get; private set; } = string.Empty; public decimal? AmountPerDay { get; private set; } public decimal? FixedAmount { get; private set; } public decimal? MaximumAmount { get; private set; } }
