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
using UTH.Library.Api.Infrastructure;
using UTH.Library.Application.Abstractions;
using UTH.Library.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace UTH.Library.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.RequireHeaderSymmetry = true;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var value in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
                if (IPAddress.TryParse(value, out var address)) options.KnownProxies.Add(address);
        });
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions.TryAdd("code", $"http.{context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode}");
            context.ProblemDetails.Extensions.TryAdd("correlationId", context.HttpContext.TraceIdentifier);
        });
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<IRequestContext, HttpRequestContext>();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.Errors
                        .Select(error => TranslateValidationMessage(entry.Key, error.ErrorMessage))
                        .ToArray() ?? []);
                var details = new ValidationProblemDetails(errors)
                {
                    Status = context.HttpContext.Request.Path.StartsWithSegments("/api/v1/me")
                        ? StatusCodes.Status422UnprocessableEntity
                        : StatusCodes.Status400BadRequest
                };
                details.Extensions["code"] = "validation.failed";
                details.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
                return details.Status == StatusCodes.Status422UnprocessableEntity
                    ? new UnprocessableEntityObjectResult(details)
                    : new BadRequestObjectResult(details);
            };
        });
        services.AddHealthChecks();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = 429, Title = "Quá nhiều yêu cầu", Detail = "Vui lòng thử lại sau một phút.",
                    Extensions = { ["code"] = "authentication.rate_limited", ["correlationId"] = context.HttpContext.TraceIdentifier }
                }, cancellationToken);
            };
            foreach (var (name, limit) in new[] { ("auth-login", 10), ("auth-refresh", 60) })
                options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var rawUserId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    if (!Guid.TryParse(rawUserId, out var userId))
                    {
                        context.Fail("Định danh tài khoản không hợp lệ.");
                        return;
                    }
                    var authorizationStateService = context.HttpContext.RequestServices
                        .GetRequiredService<IAuthorizationStateService>();
                    var state = await authorizationStateService.GetAsync(
                        userId,
                        context.HttpContext.RequestAborted);
                    if (state?.CanAuthenticate != true ||
                        !Guid.TryParse(context.Principal?.FindFirst("sid")?.Value, out var familyId) ||
                        !await authorizationStateService.IsSessionActiveAsync(userId, familyId, context.HttpContext.RequestAborted))
                        context.Fail("Tài khoản không khả dụng.");
                },
                OnChallenge = context => WriteProblemDetailsAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "Chưa xác thực", "Vui lòng đăng nhập để truy cập tài nguyên này.", context.HandleResponse),
                OnForbidden = context => WriteProblemDetailsAsync(context.HttpContext, StatusCodes.Status403Forbidden, "Không có quyền truy cập", "Bạn không có quyền truy cập tài nguyên này.")
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
                    policy.AddRequirements(new PermissionRequirement(permission));
                });
            }
        });
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

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
            Type = $"https://httpstatuses.com/{statusCode}",
            Extensions =
            {
                ["code"] = statusCode == StatusCodes.Status401Unauthorized ? "authentication.required" : "authorization.forbidden",
                ["correlationId"] = httpContext.TraceIdentifier
            }
        });
    }

    private static string TranslateValidationMessage(string field, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return $"Giá trị trường '{field}' không hợp lệ.";
        if (!message.Any(character => character > 127) && message.Contains("required", StringComparison.OrdinalIgnoreCase))
            return $"Trường '{field}' là bắt buộc.";
        if (!message.Any(character => character > 127))
            return $"Giá trị trường '{field}' không hợp lệ.";
        return message;
    }
}
