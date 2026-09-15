namespace UTH.Library.Domain.ValueObjects;

public readonly record struct IsbnValue
{
    private IsbnValue(string value) => Value = value;

    public string Value { get; }

    public static IsbnValue Create(string value)
    {
        var normalized = Normalize(value);
        if (!IsValid(normalized))
            throw new ArgumentException("ISBN must be a valid ISBN-10 or ISBN-13.", nameof(value));

        return new IsbnValue(normalized);
    }

    public static string Normalize(string value) =>
        value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    private static bool IsValid(string value) => value.Length switch
    {
        10 => IsValidIsbn10(value),
        13 => IsValidIsbn13(value),
        _ => false
    };

    private static bool IsValidIsbn10(string value)
    {
        if (value[..9].Any(character => !char.IsDigit(character)) ||
            (value[9] != 'X' && !char.IsDigit(value[9])))
            return false;

        var sum = value[..9].Select((character, index) => (10 - index) * (character - '0')).Sum();
        sum += value[9] == 'X' ? 10 : value[9] - '0';
        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string value)
    {
        if (value.Any(character => !char.IsDigit(character)))
            return false;

        var sum = value[..12].Select((character, index) => (character - '0') * (index % 2 == 0 ? 1 : 3)).Sum();
        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == value[12] - '0';
    }
}