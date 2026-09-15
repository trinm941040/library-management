using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace UTH.Library.Infrastructure.Identity;

public sealed class RsaJwtKeyProvider : IDisposable
{
    private const int MinimumKeySizeBits = 2_048;
    private readonly RSA privateKey;
    private readonly RSA publicKey;

    public RsaJwtKeyProvider(IOptions<JwtOptions> options, IHostEnvironment environment)
    {
        var settings = options.Value;
        privateKey = LoadPrivateKey(ResolvePath(settings.PrivateKeyPemPath, environment.ContentRootPath));
        publicKey = LoadPublicKey(ResolvePath(settings.PublicKeyPemPath, environment.ContentRootPath));

        EnsureKeyStrength(privateKey, "private");
        EnsureKeyStrength(publicKey, "public");
        EnsureMatchingPair(privateKey, publicKey);

        SigningKey = new RsaSecurityKey(privateKey) { KeyId = settings.KeyId };
        ValidationKey = new RsaSecurityKey(publicKey) { KeyId = settings.KeyId };
    }

    public RsaSecurityKey SigningKey { get; }
    public RsaSecurityKey ValidationKey { get; }

    public void Dispose()
    {
        privateKey.Dispose();
        publicKey.Dispose();
    }

    private static RSA LoadPrivateKey(string path)
    {
        var pem = ReadPem(path, "private");
        if (!pem.Contains("PRIVATE KEY", StringComparison.Ordinal))
            throw new InvalidOperationException($"JWT private key file '{path}' does not contain a private key.");

        return ImportKey(pem, path, isPrivate: true);
    }

    private static RSA LoadPublicKey(string path)
    {
        var pem = ReadPem(path, "public");
        if (pem.Contains("PRIVATE KEY", StringComparison.Ordinal))
            throw new InvalidOperationException($"JWT public key file '{path}' must not contain private key material.");

        return ImportKey(pem, path, isPrivate: false);
    }

    private static RSA ImportKey(string pem, string path, bool isPrivate)
    {
        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            if (isPrivate)
                _ = rsa.ExportParameters(includePrivateParameters: true);
            return rsa;
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException)
        {
            throw new InvalidOperationException($"JWT key file '{path}' is not a valid RSA PEM key.", exception);
        }
    }

    private static string ReadPem(string path, string keyType)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"JWT {keyType} key file was not found at '{path}'.");

        return File.ReadAllText(path);
    }

    private static string ResolvePath(string configuredPath, string contentRootPath) =>
        Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(configuredPath, contentRootPath);

    private static void EnsureKeyStrength(RSA key, string keyType)
    {
        if (key.KeySize < MinimumKeySizeBits)
            throw new InvalidOperationException(
                $"JWT {keyType} RSA key must be at least {MinimumKeySizeBits} bits; actual size is {key.KeySize} bits.");
    }

    private static void EnsureMatchingPair(RSA privateRsa, RSA publicRsa)
    {
        var privatePublic = privateRsa.ExportParameters(includePrivateParameters: false);
        var publicParameters = publicRsa.ExportParameters(includePrivateParameters: false);

        if (privatePublic.Modulus is null ||
            privatePublic.Exponent is null ||
            publicParameters.Modulus is null ||
            publicParameters.Exponent is null ||
            !CryptographicOperations.FixedTimeEquals(privatePublic.Modulus, publicParameters.Modulus) ||
            !CryptographicOperations.FixedTimeEquals(privatePublic.Exponent, publicParameters.Exponent))
        {
            throw new InvalidOperationException("JWT private and public PEM files do not form a matching RSA key pair.");
        }
    }
}

public sealed class JwtKeyValidationHostedService(RsaJwtKeyProvider keyProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = keyProvider.SigningKey;
        _ = keyProvider.ValidationKey;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
