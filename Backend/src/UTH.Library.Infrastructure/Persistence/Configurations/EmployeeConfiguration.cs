using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.EmployeeCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(employee => employee.EmployeeCode).IsUnique();

        builder.Property(employee => employee.FullName).HasMaxLength(150).IsRequired();
        builder.Property(employee => employee.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(employee => employee.Email).IsUnique();

        builder.Property(employee => employee.PhoneNumber).HasMaxLength(30);
        builder.Property(employee => employee.Address).HasMaxLength(500);
        builder.Property(employee => employee.Position).HasMaxLength(100).IsRequired();
        builder.Property(employee => employee.Department).HasMaxLength(100).IsRequired();
        builder.Property(employee => employee.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        builder.Property(employee => employee.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(employee => employee.CreatedAtUtc).IsRequired();
        builder.Property(employee => employee.UpdatedAtUtc).IsRequired();

        builder.HasIndex(employee => employee.Department);
        builder.HasIndex(employee => employee.BranchId);
        builder.HasIndex(employee => employee.Status);
        builder.HasIndex(employee => employee.UserId).IsUnique();
        builder.HasOne(employee => employee.Branch)
            .WithMany()
            .HasForeignKey(employee => employee.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Employee>(employee => employee.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
