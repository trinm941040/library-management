using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Identity;
using UTH.Library.Infrastructure.Persistence.Configurations;
using UTH.Library.Application.Common;

namespace UTH.Library.Infrastructure.Persistence;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { return await base.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException exception)
        { throw new OptimisticConcurrencyException("The resource was modified by another request.", exception); }
    }
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Borrowing> Borrowings => Set<Borrowing>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Violation> Violations => Set<Violation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipCard> MembershipCards => Set<MembershipCard>();
    public DbSet<MemberRestriction> MemberRestrictions => Set<MemberRestriction>();
    public DbSet<FinePayment> FinePayments => Set<FinePayment>();
    public DbSet<FineAdjustment> FineAdjustments => Set<FineAdjustment>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshTokenSession> RefreshTokenSessions => Set<RefreshTokenSession>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Shelf> Shelves => Set<Shelf>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<InventoryAudit> InventoryAudits => Set<InventoryAudit>();
    public DbSet<InventoryAuditItem> InventoryAuditItems => Set<InventoryAuditItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockReceiptItem> StockReceiptItems => Set<StockReceiptItem>();
    public DbSet<DiscrepancyReport> DiscrepancyReports => Set<DiscrepancyReport>();
    public DbSet<Renewal> Renewals => Set<Renewal>();
    public DbSet<CirculationPolicy> CirculationPolicies => Set<CirculationPolicy>();
    public DbSet<BorrowingLimitPolicy> BorrowingLimitPolicies => Set<BorrowingLimitPolicy>();
    public DbSet<FinePolicy> FinePolicies => Set<FinePolicy>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<ConfigurationPackage> ConfigurationPackages => Set<ConfigurationPackage>();

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
            entity.Property(book => book.ConcurrencyToken).IsConcurrencyToken().HasDefaultValueSql("gen_random_uuid()");
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
            entity.Property(borrowing => borrowing.ConcurrencyToken).IsConcurrencyToken().HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.ReserverName).HasMaxLength(200).IsRequired();
            entity.Property(reservation => reservation.ReserverEmail).HasMaxLength(256).IsRequired();
            entity.Property(reservation => reservation.ReservedAtUtc).IsRequired();
            entity.Property(reservation => reservation.ExpiresAtUtc).IsRequired();
            entity.HasIndex(reservation => new { reservation.BookId, reservation.ReserverId, reservation.FulfilledAtUtc, reservation.CancelledAtUtc });
            entity.HasIndex(reservation => reservation.ExpiresAtUtc);
            entity.Ignore(reservation => reservation.IsFulfilled);
            entity.Ignore(reservation => reservation.IsCancelled);
            entity.Ignore(reservation => reservation.IsOpen);
            entity.Property(reservation => reservation.ConcurrencyToken).IsConcurrencyToken().HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<Violation>(entity =>
        {
            entity.ToTable("violations");
            entity.HasKey(violation => violation.Id);
            entity.Property(violation => violation.BorrowerName).HasMaxLength(200).IsRequired();
            entity.Property(violation => violation.BorrowerEmail).HasMaxLength(256).IsRequired();
            entity.Property(violation => violation.BookTitle).HasMaxLength(200).IsRequired();
            entity.Property(violation => violation.Type).HasMaxLength(20).IsRequired();
            entity.Property(violation => violation.Note).HasMaxLength(500).IsRequired();
            entity.Property(violation => violation.FineAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(violation => violation.RecordedAtUtc).IsRequired();
            entity.Property(violation => violation.Resolution).HasMaxLength(20);
            entity.HasIndex(violation => new { violation.BorrowerId, violation.ResolvedAtUtc });
            entity.HasIndex(violation => violation.RecordedAtUtc);
            entity.Ignore(violation => violation.IsOpen);
            entity.Property(violation => violation.ConcurrencyToken).IsConcurrencyToken().HasDefaultValueSql("gen_random_uuid()");
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

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
