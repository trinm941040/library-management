using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace UTH.Library.Infrastructure.Identity;

public sealed class Argon2PasswordHasher(IOptions<Argon2PasswordHasherOptions> options)
    : IPasswordHasher<ApplicationUser>
{
    private const string Algorithm = "argon2id";
    private const int Version = 19;
    private readonly Argon2PasswordHasherOptions settings = options.Value;
    private readonly PasswordHasher<ApplicationUser> legacyHasher = new();

    public string HashPassword(ApplicationUser user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(settings.SaltSizeBytes);
        var hash = DeriveKey(
            password,
            salt,
            settings.MemorySizeKib,
            settings.Iterations,
            settings.DegreeOfParallelism,
            settings.HashSizeBytes);

        return $"${Algorithm}$v={Version}$m={settings.MemorySizeKib},t={settings.Iterations},p={settings.DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user,
        string hashedPassword,
        string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(hashedPassword);
        ArgumentNullException.ThrowIfNull(providedPassword);

        if (!hashedPassword.StartsWith($"${Algorithm}$", StringComparison.Ordinal))
        {
            var legacyResult = legacyHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return legacyResult == PasswordVerificationResult.Failed
                ? PasswordVerificationResult.Failed
                : PasswordVerificationResult.SuccessRehashNeeded;
        }

        try
        {
            if (!TryParse(hashedPassword, out var parsed))
                return PasswordVerificationResult.Failed;

            var actualHash = DeriveKey(
                providedPassword,
                parsed.Salt,
                parsed.MemorySizeKib,
                parsed.Iterations,
                parsed.DegreeOfParallelism,
                parsed.Hash.Length);

            if (!CryptographicOperations.FixedTimeEquals(actualHash, parsed.Hash))
                return PasswordVerificationResult.Failed;

            return UsesCurrentParameters(parsed)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or CryptographicException)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private static byte[] DeriveKey(
        string password,
        byte[] salt,
        int memorySizeKib,
        int iterations,
        int degreeOfParallelism,
        int hashSizeBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySizeKib,
            Iterations = iterations,
            DegreeOfParallelism = degreeOfParallelism
        };

        return argon2.GetBytes(hashSizeBytes);
    }

    private bool UsesCurrentParameters(ParsedHash parsed) =>
        parsed.MemorySizeKib == settings.MemorySizeKib &&
        parsed.Iterations == settings.Iterations &&
        parsed.DegreeOfParallelism == settings.DegreeOfParallelism &&
        parsed.Salt.Length == settings.SaltSizeBytes &&
        parsed.Hash.Length == settings.HashSizeBytes;

    private static bool TryParse(string encodedHash, out ParsedHash parsed)
    {
        parsed = default;
        var segments = encodedHash.Split('$', StringSplitOptions.None);
        if (segments.Length != 6 ||
            segments[0].Length != 0 ||
            !string.Equals(segments[1], Algorithm, StringComparison.Ordinal) ||
            !string.Equals(segments[2], $"v={Version}", StringComparison.Ordinal))
            return false;

        var parameters = segments[3];
        var encodedSalt = segments[4];
        var encodedHashValue = segments[5];
        var parameterValues = parameters.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parameterValues.Length != 3 ||
            !TryReadParameter(parameterValues[0], "m", out var memorySizeKib) ||
            !TryReadParameter(parameterValues[1], "t", out var iterations) ||
            !TryReadParameter(parameterValues[2], "p", out var degreeOfParallelism))
            return false;

        var salt = Convert.FromBase64String(encodedSalt);
        var hash = Convert.FromBase64String(encodedHashValue);
        var candidate = new ParsedHash(memorySizeKib, iterations, degreeOfParallelism, salt, hash);

        if (candidate.MemorySizeKib is < 8_192 or > 1_048_576 ||
            candidate.Iterations is < 1 or > 10 ||
            candidate.DegreeOfParallelism is < 1 or > 16 ||
            candidate.Salt.Length is < 16 or > 64 ||
            candidate.Hash.Length is < 16 or > 64)
            return false;

        parsed = candidate;
        return true;
    }

    private static bool TryReadParameter(string value, string name, out int result)
    {
        result = 0;
        var prefix = $"{name}=";
        return value.StartsWith(prefix, StringComparison.Ordinal) &&
               int.TryParse(value.AsSpan(prefix.Length), out result);
    }

    private readonly record struct ParsedHash(
        int MemorySizeKib,
        int Iterations,
        int DegreeOfParallelism,
        byte[] Salt,
        byte[] Hash);
}
