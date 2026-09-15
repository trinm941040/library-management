using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.IntegrationTests;

public sealed class JwtKeyPairTests : IClassFixture<TodoApiFactory>
{
    private readonly TodoApiFactory factory;

    public JwtKeyPairTests(TodoApiFactory factory) => this.factory = factory;

    [Fact]
    public void CreateAccessToken_RsaPemPair_ProducesTokenAcceptedByBearerValidation()
    {
        var tokenService = factory.Services.GetRequiredService<IJwtTokenService>();
        var bearerOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var userId = Guid.NewGuid();

        var result = tokenService.CreateAccessToken(
            userId,
            "jwt-test@example.com",
            ["Administrator"],
            [Permissions.UsersRead]);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var encodedToken = handler.ReadJwtToken(result.Token);
        var principal = handler.ValidateToken(
            result.Token,
            bearerOptions.TokenValidationParameters,
            out var validatedToken);

        Assert.Equal(SecurityAlgorithms.RsaSha256, encodedToken.Header.Alg);
        Assert.Equal("uth-library-rsa-2026-01", encodedToken.Header.Kid);
        Assert.IsType<RsaSecurityKey>(validatedToken.SigningKey);
        Assert.Equal(userId.ToString(), principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(Permissions.UsersRead, principal.FindFirst("permission")?.Value);
    }

    [Fact]
    public void ValidateToken_ModifiedRsaSignature_ThrowsSecurityTokenException()
    {
        var tokenService = factory.Services.GetRequiredService<IJwtTokenService>();
        var bearerOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var token = tokenService.CreateAccessToken(
            Guid.NewGuid(),
            "jwt-test@example.com",
            ["Administrator"],
            [Permissions.UsersRead]).Token;
        var segments = token.Split('.');
        var signature = Base64UrlEncoder.DecodeBytes(segments[2]);
        signature[0] ^= 0x01;
        var modifiedToken = $"{segments[0]}.{segments[1]}.{Base64UrlEncoder.Encode(signature)}";

        var handler = new JwtSecurityTokenHandler();

        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(modifiedToken, bearerOptions.TokenValidationParameters, out _));
    }
}
