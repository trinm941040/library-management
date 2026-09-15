using UTH.Library.Domain.Enums;

namespace UTH.Library.Domain.Entities;

public sealed class SystemSetting
{
    private SystemSetting() { }

    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = "null";
    public SettingType ValueType { get; private set; }
    public SettingScope Scope { get; private set; }
    public bool IsSecret { get; private set; }
    public string? Description { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static SystemSetting Create(
        string key,
        string value,
        SettingType valueType,
        SettingScope scope,
        bool isSecret,
        string? description,
        Guid actorUserId,
        DateTime nowUtc) => new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            ValueType = valueType,
            Scope = scope,
            IsSecret = isSecret,
            Description = description,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = AsUtc(nowUtc),
            ConcurrencyToken = Guid.NewGuid()
        };

    public void Update(string value, Guid actorUserId, DateTime nowUtc)
    {
        Value = value;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = AsUtc(nowUtc);
        ConcurrencyToken = Guid.NewGuid();
    }

    public void SynchronizeDefinition(
        SettingType valueType,
        SettingScope scope,
        bool isSecret,
        string? description)
    {
        ValueType = valueType;
        Scope = scope;
        IsSecret = isSecret;
        Description = description;
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
