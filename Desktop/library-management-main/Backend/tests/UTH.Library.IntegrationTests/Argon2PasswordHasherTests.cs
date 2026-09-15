using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.IntegrationTests;

public sealed class Argon2PasswordHasherTests
{
    private readonly ApplicationUser user = new() { Id = Guid.NewGuid() };
    private readonly Argon2PasswordHasher sut = new(Options.Create(new Argon2PasswordHasherOptions()));

    [Fact]
    public void HashPassword_SamePasswordTwice_UsesUniqueSalt()
    {
        var first = sut.HashPassword(user, "Correct-Horse-Battery-Staple!");
        var second = sut.HashPassword(user, "Correct-Horse-Battery-Staple!");

        Assert.NotEqual(first, second);
        Assert.StartsWith("$argon2id$v=19$", first, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyHashedPassword_CorrectPassword_ReturnsSuccess()
    {
        var hash = sut.HashPassword(user, "Correct-Horse-Battery-Staple!");

        var result = sut.VerifyHashedPassword(user, hash, "Correct-Horse-Battery-Staple!");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void VerifyHashedPassword_IncorrectPassword_ReturnsFailed()
    {
        var hash = sut.HashPassword(user, "Correct-Horse-Battery-Staple!");

        var result = sut.VerifyHashedPassword(user, hash, "incorrect");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void VerifyHashedPassword_LegacyIdentityHash_RequestsArgon2Rehash()
    {
        var legacyHasher = new PasswordHasher<ApplicationUser>();
        var legacyHash = legacyHasher.HashPassword(user, "Correct-Horse-Battery-Staple!");

        var result = sut.VerifyHashedPassword(user, legacyHash, "Correct-Horse-Battery-Staple!");

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("$argon2id$v=19$invalid")]
    [InlineData("$argon2id$v=19$m=999999999,t=3,p=2$YQ==$YQ==")]
    public void VerifyHashedPassword_MalformedHash_ReturnsFailed(string hash)
    {
        var result = sut.VerifyHashedPassword(user, hash, "password");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }
}
