using System.Security.Claims;
using UTH.Library.Application.Abstractions;

namespace UTH.Library.Api.Infrastructure;

internal sealed class HttpRequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    private HttpContext? Context => accessor.HttpContext;
    public Guid? UserId => Guid.TryParse(Context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context?.User.FindFirstValue("sub"), out var id) ? id : null;
    public string CorrelationId => Context?.TraceIdentifier ?? string.Empty;
}
