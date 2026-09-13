using UTH.Library.Api;
using UTH.Library.Application;
using UTH.Library.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UTH.Library.Infrastructure.Identity;
using UTH.Library.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapControllers();
app.Map("/", () => Results.Redirect("/swagger"));

// Handle swagger for development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(config =>
    {
        config.SwaggerEndpoint("/openapi/v1.json", "UTH.Library API");
    });
}

app.Run();

public partial class Program;
