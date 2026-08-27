using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Identity;
using UTH.Library.Infrastructure.Persistence.Configurations;

namespace UTH.Library.Infrastructure.Persistence;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Borrowing> Borrowings => Set<Borrowing>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshTokenSession> RefreshTokenSessions => Set<RefreshTokenSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUserPasskey<Guid>>(entity =>
        {
            entity.HasKey(passkey => new { passkey.UserId, passkey.CredentialId });
            entity.OwnsOne(passkey => passkey.Data);
        });

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.ToTable("todos");
            entity.HasKey(todo => todo.Id);
            entity.Property(todo => todo.Title).HasMaxLength(200).IsRequired();
            entity.Property(todo => todo.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("books");
            entity.HasKey(book => book.Id);
            entity.Property(book => book.Title).HasMaxLength(200).IsRequired();
            entity.Property(book => book.Author).HasMaxLength(200).IsRequired();
            entity.Property(book => book.Isbn).HasMaxLength(32).IsRequired();
            entity.Property(book => book.Category).HasMaxLength(100).IsRequired();
            entity.Property(book => book.Quantity).IsRequired();
            entity.Property(book => book.CreatedAtUtc).IsRequired();
            entity.HasIndex(book => book.Isbn).IsUnique();
            entity.HasIndex(book => book.Title);
        });

        modelBuilder.Entity<Borrowing>(entity =>
        {
            entity.ToTable("borrowings");
            entity.HasKey(borrowing => borrowing.Id);
            entity.Property(borrowing => borrowing.BorrowerName).HasMaxLength(200).IsRequired();
            entity.Property(borrowing => borrowing.BorrowerEmail).HasMaxLength(256).IsRequired();
            entity.Property(borrowing => borrowing.BorrowedAtUtc).IsRequired();
            entity.Property(borrowing => borrowing.DueAtUtc).IsRequired();
            entity.HasIndex(borrowing => new { borrowing.BookId, borrowing.BorrowerId, borrowing.ReturnedAtUtc });
            entity.HasIndex(borrowing => borrowing.DueAtUtc);
            entity.Ignore(borrowing => borrowing.IsReturned);
        });

        modelBuilder.Entity<ApplicationUser>().ToTable("users");
        modelBuilder.Entity<ApplicationRole>().ToTable("roles");
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(permission => permission.Name).IsUnique();
        });
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(value => new { value.RoleId, value.PermissionId });
            entity.HasOne(value => value.Role).WithMany().HasForeignKey(value => value.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(value => value.Permission).WithMany().HasForeignKey(value => value.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RefreshTokenSession>(entity =>
        {
            entity.ToTable("refresh_token_sessions");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });
            entity.Property(token => token.UserAgent).HasMaxLength(500);
            entity.Property(token => token.RevocationReason).HasMaxLength(100);
            entity.HasOne(token => token.User).WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(token => token.RowVersion).IsConcurrencyToken();
        });

        modelBuilder.ApplyConfiguration(new AdministratorUserSeedConfiguration());
        modelBuilder.ApplyConfiguration(new AdministratorRoleSeedConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionSeedConfiguration());
        modelBuilder.ApplyConfiguration(new AdministratorPermissionSeedConfiguration());
        modelBuilder.ApplyConfiguration(new AdministratorUserRoleSeedConfiguration());
    }
}
