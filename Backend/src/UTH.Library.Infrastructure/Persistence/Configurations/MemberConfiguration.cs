using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;
namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> b) {
        b.ToTable("members"); b.HasKey(x=>x.Id);
        b.Property(x=>x.MemberCode).HasMaxLength(30).IsRequired(); b.HasIndex(x=>x.MemberCode).IsUnique();
        b.Property(x=>x.FullName).HasMaxLength(150).IsRequired(); b.Property(x=>x.Email).HasMaxLength(256).IsRequired(); b.HasIndex(x=>x.Email).IsUnique();
        b.Property(x=>x.PhoneNumber).HasMaxLength(30); b.Property(x=>x.Address).HasMaxLength(500); b.Property(x=>x.MemberGroup).HasMaxLength(100).IsRequired();
        b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(20); b.Property(x=>x.ConcurrencyToken).IsConcurrencyToken(); b.HasIndex(x=>x.Status); b.HasIndex(x=>x.MemberGroup);
        b.HasMany(x=>x.MembershipCards).WithOne().HasForeignKey(x=>x.MemberId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x=>x.Restrictions).WithOne().HasForeignKey(x=>x.MemberId).OnDelete(DeleteBehavior.Cascade);
    }
}
internal sealed class MembershipCardConfiguration : IEntityTypeConfiguration<MembershipCard> {
    public void Configure(EntityTypeBuilder<MembershipCard> b) { b.ToTable("membership_cards"); b.HasKey(x=>x.Id); b.Property(x=>x.CardNumber).HasMaxLength(50).IsRequired(); b.HasIndex(x=>x.CardNumber).IsUnique(); b.HasIndex(x=>x.MemberId); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(20); }
}
internal sealed class MemberRestrictionConfiguration : IEntityTypeConfiguration<MemberRestriction> {
    public void Configure(EntityTypeBuilder<MemberRestriction> b) { b.ToTable("member_restrictions"); b.HasKey(x=>x.Id); b.Property(x=>x.Type).HasConversion<string>().HasMaxLength(30); b.Property(x=>x.Reason).HasMaxLength(500).IsRequired(); b.Property(x=>x.RemovalReason).HasMaxLength(500); b.HasIndex(x=>new{x.MemberId,x.RemovedAtUtc}); }
}
internal sealed class FinePaymentConfiguration : IEntityTypeConfiguration<FinePayment> {
    public void Configure(EntityTypeBuilder<FinePayment> b) { b.ToTable("fine_payments"); b.HasKey(x=>x.Id); b.Property(x=>x.Amount).HasPrecision(18,2); b.Property(x=>x.Method).HasConversion<string>().HasMaxLength(30); b.Property(x=>x.Reference).HasMaxLength(200); b.HasIndex(x=>new{x.MemberId,x.ViolationId}); b.HasOne<Member>().WithMany().HasForeignKey(x=>x.MemberId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Violation>().WithMany().HasForeignKey(x=>x.ViolationId).OnDelete(DeleteBehavior.Cascade); }
}
internal sealed class FineAdjustmentConfiguration : IEntityTypeConfiguration<FineAdjustment> {
    public void Configure(EntityTypeBuilder<FineAdjustment> b) { b.ToTable("fine_adjustments"); b.HasKey(x=>x.Id); b.Property(x=>x.AmountDelta).HasPrecision(18,2); b.Property(x=>x.Reason).HasMaxLength(500).IsRequired(); b.HasIndex(x=>new{x.MemberId,x.ViolationId}); b.HasOne<Member>().WithMany().HasForeignKey(x=>x.MemberId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Violation>().WithMany().HasForeignKey(x=>x.ViolationId).OnDelete(DeleteBehavior.Cascade); }
}
