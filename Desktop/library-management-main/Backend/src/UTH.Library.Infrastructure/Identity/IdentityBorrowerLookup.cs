using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.Infrastructure.Identity;

public sealed class IdentityBorrowerLookup(UserManager<ApplicationUser> userManager) : IBorrowerLookup
{
    public async Task<BorrowerRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return null;

        var name = string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.Email ?? user.Id.ToString()
            : user.DisplayName;
        return new BorrowerRecord(user.Id, name, user.Email ?? string.Empty, user.IsActive);
    }

    public async Task<IReadOnlyList<BorrowerRecord>> ListActiveAsync(int take, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, 100);
        return await userManager.Users
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .Take(limit)
            .Select(user => new BorrowerRecord(
                user.Id,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.IsActive))
            .ToListAsync(cancellationToken);
    }
}
