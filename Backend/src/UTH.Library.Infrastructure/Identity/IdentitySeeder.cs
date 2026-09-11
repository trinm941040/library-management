using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class IdentitySeeder(IServiceProvider services, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DB MIGRATION NOTICE]: {ex.Message}");
                if (ex.InnerException is not null)
                {
                    Console.WriteLine($"[DB MIGRATION INNER]: {ex.InnerException.Message}");
                }
                Console.ResetColor();
            }
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var now = DateTime.UtcNow;
        foreach (var permissionName in Permissions.All)
        {
            if (!await db.Permissions.AnyAsync(value => value.Name == permissionName, cancellationToken))
                db.Permissions.Add(new Permission { Id = Guid.NewGuid(), Name = permissionName, Module = permissionName[..permissionName.IndexOf('.')], CreatedAtUtc = now });
        }
        await db.SaveChangesAsync(cancellationToken);
        await EnsureRoleAsync(roleManager, db, RoleNames.Administrator, true, Permissions.All, cancellationToken);
        await EnsureRoleAsync(roleManager, db, RoleNames.User, true, [Permissions.TodosRead, Permissions.TodosCreate, Permissions.TodosUpdate, Permissions.TodosDelete], cancellationToken);
    }

    private static async Task EnsureRoleAsync(RoleManager<ApplicationRole> roleManager, LibraryDbContext db, string name, bool systemRole, IEnumerable<string> permissionNames, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role is null)
        {
            role = new ApplicationRole { Id = Guid.NewGuid(), Name = name, NormalizedName = name.ToUpperInvariant(), IsSystemRole = systemRole, CreatedAtUtc = DateTime.UtcNow };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Unable to seed role '{name}'.");
        }
        var permissionIds = await db.Permissions.Where(value => permissionNames.Contains(value.Name)).Select(value => value.Id).ToListAsync(cancellationToken);
        var existing = await db.RolePermissions.Where(value => value.RoleId == role.Id).Select(value => value.PermissionId).ToListAsync(cancellationToken);
        db.RolePermissions.AddRange(permissionIds.Where(id => !existing.Contains(id)).Select(id => new RolePermission { RoleId = role.Id, PermissionId = id }));
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
