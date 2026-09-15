using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(setting => setting.Id);
        builder.Property(setting => setting.Key).HasMaxLength(150).IsRequired();
        builder.Property(setting => setting.Value).HasColumnType("jsonb").IsRequired();
        builder.Property(setting => setting.ValueType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(setting => setting.Scope).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(setting => setting.IsSecret).IsRequired();
        builder.Property(setting => setting.Description).HasMaxLength(1000);
        builder.Property(setting => setting.UpdatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(setting => setting.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        builder.HasIndex(setting => setting.Key).IsUnique();
        builder.HasIndex(setting => new { setting.Scope, setting.Key });
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(setting => setting.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
