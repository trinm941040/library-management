using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");
        builder.HasKey(branch => branch.Id);
        builder.Property(branch => branch.Code).HasMaxLength(30).IsRequired();
        builder.Property(branch => branch.Name).HasMaxLength(150).IsRequired();
        builder.Property(branch => branch.Address).HasMaxLength(500);
        builder.Property(branch => branch.CreatedAtUtc).IsRequired();
        builder.Property(branch => branch.UpdatedAtUtc).IsRequired();
        builder.Property(branch => branch.ConcurrencyToken)
            .IsConcurrencyToken()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.HasIndex(branch => branch.Code).IsUnique();
        builder.HasIndex(branch => branch.Name);
        builder.HasIndex(branch => branch.IsActive);

        var seededAt = new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(new
        {
            Id = Branch.MainBranchId,
            Code = "MAIN",
            Name = "Main Library",
            Address = (string?)null,
            IsActive = true,
            CreatedAtUtc = seededAt,
            UpdatedAtUtc = seededAt,
            ConcurrencyToken = new Guid("40000000-0000-0000-0000-000000000002")
        });
    }
}
