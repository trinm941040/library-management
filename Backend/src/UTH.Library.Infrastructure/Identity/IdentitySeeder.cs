using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

public sealed class IdentitySeeder(IServiceProvider services, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
            await db.Database.MigrateAsync(cancellationToken);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var now = DateTime.UtcNow;
        var newPermissions = new List<Permission>();
        foreach (var permissionName in Permissions.All)
        {
            if (!await db.Permissions.AnyAsync(value => value.Name == permissionName, cancellationToken))
            {
                var permission = new Permission { Id = Guid.NewGuid(), Name = permissionName, Module = permissionName[..permissionName.IndexOf('.')], CreatedAtUtc = now };
                db.Permissions.Add(permission);
                newPermissions.Add(permission);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        // Bootstrap newly introduced permissions only; never restore an existing revoked grant.
        var administrator = await roleManager.FindByNameAsync(RoleNames.Administrator);
        if (administrator is not null)
        {
            db.RolePermissions.AddRange(newPermissions.Select(permission => new RolePermission { RoleId = administrator.Id, PermissionId = permission.Id }));
            await db.SaveChangesAsync(cancellationToken);
        }
        await EnsureRoleAsync(roleManager, db, RoleNames.Administrator, true, Permissions.All, cancellationToken);
        await EnsureRoleAsync(roleManager, db, RoleNames.User, true, [Permissions.TodosRead, Permissions.TodosCreate, Permissions.TodosUpdate, Permissions.TodosDelete], cancellationToken);
        await EnsureNotificationTemplatesAsync(db, cancellationToken);
    }

    private static async Task EnsureRoleAsync(RoleManager<ApplicationRole> roleManager, LibraryDbContext db, string name, bool systemRole, IEnumerable<string> permissionNames, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role is null)
        {
            role = new ApplicationRole { Id = Guid.NewGuid(), Name = name, NormalizedName = name.ToUpperInvariant(), IsSystemRole = systemRole, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Unable to seed role '{name}'.");
        }
        else return; // Preserve administrator changes to existing role grants on restart.
        var permissionIds = await db.Permissions.Where(value => permissionNames.Contains(value.Name)).Select(value => value.Id).ToListAsync(cancellationToken);
        var existing = await db.RolePermissions.Where(value => value.RoleId == role.Id).Select(value => value.PermissionId).ToListAsync(cancellationToken);
        db.RolePermissions.AddRange(permissionIds.Where(id => !existing.Contains(id)).Select(id => new RolePermission { RoleId = role.Id, PermissionId = id }));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureNotificationTemplatesAsync(LibraryDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.NotificationTemplates.AnyAsync(cancellationToken))
        {
            var nowUtc = DateTime.UtcNow;
            db.NotificationTemplates.AddRange(
                NotificationTemplate.Create(
                    "RESERVATION_READY",
                    "Thông báo tài liệu đặt trước đã sẵn sàng",
                    NotificationChannel.Email,
                    "Tài liệu '{{title}}' đã sẵn sàng tại thư viện",
                    "Xin chào {{name}}, tài liệu '{{title}}' bạn đã đặt trước hiện đã sẵn sàng nhận tại quầy thư viện. Vui lòng đến nhận trước ngày {{expiry_date}}.",
                    "name, recipient_name, title, expiry_date",
                    true,
                    nowUtc),
                NotificationTemplate.Create(
                    "BORROWING_DUE_REMINDER",
                    "Nhắc nhở hạn trả sách",
                    NotificationChannel.Email,
                    "Nhắc nhở: Sắp đến hạn trả sách '{{title}}'",
                    "Xin chào {{name}}, cuốn sách '{{title}}' bạn mượn sẽ đến hạn trả vào ngày {{due_date}}. Vui lòng sắp xếp trả sách đúng hạn.",
                    "name, recipient_name, title, due_date",
                    true,
                    nowUtc),
                NotificationTemplate.Create(
                    "VIOLATION_NOTICE",
                    "Thông báo vi phạm và phí phạt",
                    NotificationChannel.InApp,
                    "Thông báo phí phạt tài liệu quá hạn",
                    "Chào {{name}}, bạn có một khoản phí phạt quá hạn số tiền {{amount}} VNĐ cho tài liệu '{{title}}'. Vui lòng liên hệ thủ thư để giải quyết.",
                    "name, recipient_name, title, amount",
                    true,
                    nowUtc)
            );
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
