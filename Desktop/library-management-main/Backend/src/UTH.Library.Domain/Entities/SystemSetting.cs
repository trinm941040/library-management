using UTH.Library.Domain.Enums;
namespace UTH.Library.Domain.Entities;
public sealed class SystemSetting { public Guid Id { get; private set; } public string Key { get; private set; } = string.Empty; public string Value { get; private set; } = "null"; public SettingType ValueType { get; private set; } public string? Description { get; private set; } public Guid UpdatedByUserId { get; private set; } public DateTime UpdatedAtUtc { get; private set; } public Guid ConcurrencyToken { get; private set; } }
