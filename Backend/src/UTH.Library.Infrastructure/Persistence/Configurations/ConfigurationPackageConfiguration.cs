using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class ConfigurationPackageConfiguration : IEntityTypeConfiguration<ConfigurationPackage>
{
    public void Configure(EntityTypeBuilder<ConfigurationPackage> builder)
    {
        builder.ToTable("configuration_packages");
        builder.HasKey(package => package.Id);
        builder.Property(package => package.Version).HasMaxLength(50).IsRequired();
        builder.Property(package => package.Data).HasColumnType("jsonb").IsRequired();
        builder.Property(package => package.Checksum).HasMaxLength(64).IsRequired();
        builder.Property(package => package.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(package => package.Checksum);
        builder.HasIndex(package => package.CreatedAtUtc);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(package => package.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
