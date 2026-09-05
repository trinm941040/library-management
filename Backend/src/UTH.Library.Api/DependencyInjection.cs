using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddProblemDetails();
        services.AddHealthChecks();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context => WriteProblemDetailsAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication is required to access this resource.", context.HandleResponse),
                OnForbidden = context => WriteProblemDetailsAsync(context.HttpContext, StatusCodes.Status403Forbidden, "Forbidden", "You do not have permission to access this resource.")
            };
        });
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<RsaJwtKeyProvider, IOptions<JwtOptions>>((options, keyProvider, jwtOptions) =>
            {
                var settings = jwtOptions.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = keyProvider.ValidationKey,
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role",
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
                };
            });

        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(context =>
                        context.User.IsInRole(RoleNames.Administrator) ||
                        context.User.HasClaim("permission", permission));
                });
            }
        });

        //Build swagger documentation with versioning
        services.AddSwaggerGen(config =>
        {
            config.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "UTH.Library API Development",
                Version = "v1",
                Description = "API documentation for UTH.Library",
            });
        });
        return services;
    }

    private static async Task WriteProblemDetailsAsync(HttpContext httpContext, int statusCode, string title, string detail, Action? handleResponse = null)
    {
        handleResponse?.Invoke();
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}"
        });
    }
}
