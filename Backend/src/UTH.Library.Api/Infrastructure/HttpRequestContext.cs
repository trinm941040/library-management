using System.Security.Claims;
using System.Net;
using UTH.Library.Application.Abstractions;

namespace UTH.Library.Api.Infrastructure;

internal sealed class HttpRequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    private HttpContext? Context => accessor.HttpContext;
    public Guid? UserId => Guid.TryParse(Context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context?.User.FindFirstValue("sub"), out var id) ? id : null;
    public string CorrelationId => Context?.TraceIdentifier ?? string.Empty;
    public string? IpAddress
    {
        get
        {
            var address = Context?.Connection.RemoteIpAddress;
            if (address is null) return null;
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            return IPAddress.TryParse(address.ToString(), out var normalized) ? normalized.ToString() : null;
        }
    }
}
