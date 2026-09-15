namespace UTH.Library.Domain.Entities;
public sealed class CirculationPolicy { public Guid Id { get; private set; } public string Name { get; private set; } = string.Empty; public bool IsActive { get; private set; } public DateTime EffectiveFromUtc { get; private set; } public DateTime? EffectiveToUtc { get; private set; } public Guid ConcurrencyToken { get; private set; } }
