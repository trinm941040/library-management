

namespace UTH.Library.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "UTH.Library.Api";
    public string Audience { get; set; } = "UTH.Library.Client";
    // public string PrivateKeyPem { get; private set; } = string.Empty;
    // public string PublicKeyPem { get; private set; } = string.Empty;
    // public string PrivateKeyPemPath
    // {
    //     set
    //     {
    //         if (!File.Exists(value))
    //             throw new FileNotFoundException($"Private key file not found: {value}");
    //         PrivateKeyPem = File.ReadAllText(value);
    //     }
    //
    //     get => PrivateKeyPem;
    // }
    //
    // public string PublicKeyPemPath
    // {
    //     set
    //     {
    //         if (!File.Exists(value))
    //             throw new FileNotFoundException($"Public key file not found: {value}");
    //         PublicKeyPem = File.ReadAllText(value);
    //     }
    //
    //     get => PublicKeyPem;
    // }
    public string Key { get; set; } = string.Empty;

    public string KeyId { get; set; } = "local-development";
    public int AccessTokenMinutes { get; set; } = 10;
    public int RefreshTokenDays { get; set; } = 30;
    public bool RequireConfirmedEmail { get; set; }


}
