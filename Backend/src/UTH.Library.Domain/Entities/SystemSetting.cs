using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class SystemSetting
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public SettingType ValueType { get; private set; }
    public string? Description { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    private SystemSetting() { }

    public static SystemSetting Create(
        string key,
        string value,
        SettingType valueType,
        string? description,
        Guid updatedByUserId,
        DateTime updatedAtUtc)
    {
        return new SystemSetting
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            ValueType = valueType,
            Description = description,
            UpdatedByUserId = updatedByUserId,
            UpdatedAtUtc = updatedAtUtc,
            ConcurrencyToken = Guid.NewGuid()
        };
    }

    public void Update(
        string value,
        SettingType valueType,
        string? description,
        Guid updatedByUserId,
        DateTime updatedAtUtc)
    {
        Value = value;
        ValueType = valueType;
        Description = description;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = updatedAtUtc;
        ConcurrencyToken = Guid.NewGuid();
    }
}
