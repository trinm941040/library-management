using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class CirculationPolicyConfiguration : IEntityTypeConfiguration<CirculationPolicy>
{
    public void Configure(EntityTypeBuilder<CirculationPolicy> builder)
    {
        builder.ToTable("circulation_policies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Version).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();

        builder.Property(p => p.MemberGroup).HasMaxLength(100);
        builder.Property(p => p.DocumentType).HasMaxLength(100);
        builder.Property(p => p.BranchId);

        builder.Property(p => p.EffectiveFrom).IsRequired();
        builder.Property(p => p.EffectiveTo);

        builder.Property(p => p.MaxLoanBooks).IsRequired();
        builder.Property(p => p.LoanPeriodDays).IsRequired();
        builder.Property(p => p.MaxRenewals).IsRequired();
        builder.Property(p => p.RenewalPeriodDays).IsRequired();
        builder.Property(p => p.HoldDays).IsRequired();
        builder.Property(p => p.BlockIfOverdue).IsRequired();

        builder.Property(p => p.FinePerDay).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.FixedFineAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.MaxFineAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.LostBookPenaltyRatio).HasPrecision(18, 2).IsRequired();

        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.UpdatedAtUtc);
        builder.Property(p => p.CreatedByUserId);

        builder.HasIndex(p => new { p.IsActive, p.EffectiveFrom, p.EffectiveTo });
        builder.HasIndex(p => new { p.MemberGroup, p.DocumentType, p.BranchId });
    }
}
