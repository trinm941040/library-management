using UTH.Library.Api;
using UTH.Library.Application;
using UTH.Library.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UTH.Library.Infrastructure.Identity;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

//Build swagger documentation with versioning
builder.Services.AddSwaggerGen(config =>
{
    config.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UTH.Library API Development",
        Version = "v1",
        Description = "API documentation for UTH.Library",
    });
});

// Configure trusted forwarded headers for accurate client IP resolution
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
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
