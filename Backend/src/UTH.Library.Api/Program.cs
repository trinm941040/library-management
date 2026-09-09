using UTH.Library.Api;
using UTH.Library.Application;
using UTH.Library.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UTH.Library.Infrastructure.Identity;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

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

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-ID";
    var requestedId = context.Request.Headers[headerName].ToString();
    if (!string.IsNullOrWhiteSpace(requestedId) && requestedId.Length <= 100)
        context.TraceIdentifier = requestedId;
    context.Response.Headers[headerName] = context.TraceIdentifier;
    await next(context);
});
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
