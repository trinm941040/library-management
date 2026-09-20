using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class ShelfConfiguration : IEntityTypeConfiguration<Shelf>
{
    public void Configure(EntityTypeBuilder<Shelf> builder)
    {
        builder.ToTable("shelves");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ConcurrencyToken).IsConcurrencyToken().HasDefaultValueSql("gen_random_uuid()");
        builder.HasIndex(x => new { x.AreaId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.AreaId, x.Status });
        builder.HasOne<Area>().WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
    }
}
