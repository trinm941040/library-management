namespace UTH.Library.Api.Infrastructure;

internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public async Task InvokeAsync(HttpContext context)
    {
        var requested = context.Request.Headers[HeaderName].ToString();
        context.TraceIdentifier = IsValid(requested) ? requested : Guid.NewGuid().ToString("N");
        context.Response.Headers[HeaderName] = context.TraceIdentifier;
        await next(context);
    }
    private static bool IsValid(string value) => value.Length is > 0 and <= 100 && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');
}
