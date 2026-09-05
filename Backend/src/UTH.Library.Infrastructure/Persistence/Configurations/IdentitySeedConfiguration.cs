using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal static class IdentitySeedData
{
    public static readonly Guid AdministratorUserId = new("10000000-0000-0000-0000-000000000001");
    public static readonly Guid AdministratorRoleId = new("20000000-0000-0000-0000-000000000001");
    public static readonly DateTime CreatedAtUtc = new(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc);

    public const string AdministratorPasswordHash =
        "$argon2id$v=19$m=65536,t=3,p=2$gbASegqg+aS71bLWtCPomQ==$3AHGi//TyjMTP6Rj34P/L5yNoeZWZoEHMs2qZ+zMHd0=";

    public static readonly IReadOnlyList<SeedPermission> Permissions =
    [
        new(new Guid("30000000-0000-0000-0000-000000000001"), "users.read", "Read users.", "users"),
        new(new Guid("30000000-0000-0000-0000-000000000002"), "users.create", "Create users.", "users"),
        new(new Guid("30000000-0000-0000-0000-000000000003"), "users.update", "Update users.", "users"),
        new(new Guid("30000000-0000-0000-0000-000000000004"), "users.deactivate", "Deactivate users.", "users"),
        new(new Guid("30000000-0000-0000-0000-000000000005"), "roles.read", "Read roles.", "roles"),
        new(new Guid("30000000-0000-0000-0000-000000000006"), "roles.create", "Create roles.", "roles"),
        new(new Guid("30000000-0000-0000-0000-000000000007"), "roles.update", "Update roles.", "roles"),
        new(new Guid("30000000-0000-0000-0000-000000000008"), "roles.assign", "Assign roles and permissions.", "roles"),
        new(new Guid("30000000-0000-0000-0000-000000000009"), "permissions.read", "Read permissions.", "permissions"),
        new(new Guid("30000000-0000-0000-0000-000000000010"), "todos.read", "Read todos.", "todos"),
        new(new Guid("30000000-0000-0000-0000-000000000011"), "todos.create", "Create todos.", "todos"),
        new(new Guid("30000000-0000-0000-0000-000000000012"), "todos.update", "Update todos.", "todos"),
        new(new Guid("30000000-0000-0000-0000-000000000013"), "todos.delete", "Delete todos.", "todos"),
        new(new Guid("30000000-0000-0000-0000-000000000018"), "books.read", "Read books.", "books"),
        new(new Guid("30000000-0000-0000-0000-000000000019"), "books.create", "Create books.", "books"),
        new(new Guid("30000000-0000-0000-0000-000000000020"), "books.update", "Update books.", "books"),
        new(new Guid("30000000-0000-0000-0000-000000000021"), "books.delete", "Delete books.", "books"),
        new(new Guid("30000000-0000-0000-0000-000000000014"), "employees.read", "Read employees.", "employees"),
        new(new Guid("30000000-0000-0000-0000-000000000015"), "employees.create", "Create employees.", "employees"),
        new(new Guid("30000000-0000-0000-0000-000000000016"), "employees.update", "Update employees.", "employees"),
        new(new Guid("30000000-0000-0000-0000-000000000017"), "employees.delete", "Delete employees.", "employees"),
        new(new Guid("30000000-0000-0000-0000-000000000022"), "borrowings.read", "Read borrowings.", "borrowings"),
        new(new Guid("30000000-0000-0000-0000-000000000023"), "borrowings.create", "Create borrowings.", "borrowings"),
        new(new Guid("30000000-0000-0000-0000-000000000024"), "borrowings.return", "Return borrowings.", "borrowings")
    ];

    internal sealed record SeedPermission(Guid Id, string Name, string Description, string Module);
}

internal sealed class AdministratorUserSeedConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder) =>
        builder.HasData(new ApplicationUser
        {
            Id = IdentitySeedData.AdministratorUserId,
            UserName = "admin@example.com",
            NormalizedUserName = "ADMIN@EXAMPLE.COM",
            Email = "admin@example.com",
            NormalizedEmail = "ADMIN@EXAMPLE.COM",
            EmailConfirmed = true,
            PasswordHash = IdentitySeedData.AdministratorPasswordHash,
            SecurityStamp = "10000000-0000-0000-0000-000000000002",
            ConcurrencyStamp = "10000000-0000-0000-0000-000000000003",
            DisplayName = "System Administrator",
            IsActive = true,
            CreatedAtUtc = IdentitySeedData.CreatedAtUtc,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0
        });
}

internal sealed class AdministratorRoleSeedConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder) =>
        builder.HasData(new ApplicationRole
        {
            Id = IdentitySeedData.AdministratorRoleId,
            Name = "Administrator",
            NormalizedName = "ADMINISTRATOR",
            ConcurrencyStamp = "20000000-0000-0000-0000-000000000002",
            Description = "Full system access.",
            IsSystemRole = true,
            CreatedAtUtc = IdentitySeedData.CreatedAtUtc
        });
}

internal sealed class PermissionSeedConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder) =>
        builder.HasData(IdentitySeedData.Permissions.Select(permission => new Permission
        {
            Id = permission.Id,
            Name = permission.Name,
            Description = permission.Description,
            Module = permission.Module,
            CreatedAtUtc = IdentitySeedData.CreatedAtUtc
        }));
}

internal sealed class AdministratorPermissionSeedConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder) =>
        builder.HasData(IdentitySeedData.Permissions.Select(permission => new RolePermission
        {
            RoleId = IdentitySeedData.AdministratorRoleId,
            PermissionId = permission.Id
        }));
}

internal sealed class AdministratorUserRoleSeedConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) =>
        builder.HasData(new IdentityUserRole<Guid>
        {
            UserId = IdentitySeedData.AdministratorUserId,
            RoleId = IdentitySeedData.AdministratorRoleId
        });
}
