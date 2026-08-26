

namespace UTH.Library.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "UTH.Library.Api";
    public string Audience { get; set; } = "UTH.Library.Client";
    public string PrivateKeyPemPath { get; set; } = string.Empty;
    public string PublicKeyPemPath { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 10;
    public int RefreshTokenDays { get; set; } = 30;
    public bool RequireConfirmedEmail { get; set; }

    public static bool IsValid(JwtOptions options) =>
        !string.IsNullOrWhiteSpace(options.Issuer) &&
        !string.IsNullOrWhiteSpace(options.Audience) &&
        !string.IsNullOrWhiteSpace(options.PrivateKeyPemPath) &&
        !string.IsNullOrWhiteSpace(options.PublicKeyPemPath) &&
        !string.IsNullOrWhiteSpace(options.KeyId) &&
        options.AccessTokenMinutes is >= 1 and <= 60 &&
        options.RefreshTokenDays is >= 1 and <= 90;
}
