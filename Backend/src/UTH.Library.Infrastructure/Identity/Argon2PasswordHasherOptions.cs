namespace UTH.Library.Infrastructure.Identity;

public sealed class Argon2PasswordHasherOptions
{
    public const string SectionName = "PasswordHashing:Argon2";

    public int MemorySizeKib { get; init; } = 65_536;
    public int Iterations { get; init; } = 3;
    public int DegreeOfParallelism { get; init; } = 2;
    public int SaltSizeBytes { get; init; } = 16;
    public int HashSizeBytes { get; init; } = 32;

    public static bool IsValid(Argon2PasswordHasherOptions options) =>
        options.MemorySizeKib is >= 8_192 and <= 1_048_576 &&
        options.Iterations is >= 1 and <= 10 &&
        options.DegreeOfParallelism is >= 1 and <= 16 &&
        options.SaltSizeBytes is >= 16 and <= 64 &&
        options.HashSizeBytes is >= 16 and <= 64;
}
