using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Api.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(
    IAuthorizationStateService authorizationStateService,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var rawUserId = context.User.FindFirstValue("sub") ??
            context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId)) return;

        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ??
            CancellationToken.None;
        var state = await authorizationStateService.GetAsync(userId, cancellationToken);
        if (state?.CanAuthenticate == true && state.Permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
