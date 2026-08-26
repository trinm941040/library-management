using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, JwtOptions jwtOptions)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHealthChecks();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context => WriteProblemDetailsAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized", "Authentication is required to access this resource.", context.HandleResponse),
                OnForbidden = context => WriteProblemDetailsAsync(context.HttpContext, StatusCodes.Status403Forbidden, "Forbidden", "You do not have permission to access this resource.")
            };
        });

        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
                options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
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
