using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Action).HasMaxLength(100).IsRequired();
        builder.Property(log => log.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(log => log.BeforeJson).HasColumnType("jsonb");
        builder.Property(log => log.AfterJson).HasColumnType("jsonb");
        builder.Property(log => log.CreatedAtUtc).IsRequired();
        builder.Property(log => log.CorrelationId).HasMaxLength(100);
        builder.Property(log => log.IpAddress).HasMaxLength(64);

        builder.HasIndex(log => new { log.EntityType, log.EntityId, log.CreatedAtUtc });
        builder.HasIndex(log => new { log.ActorUserId, log.CreatedAtUtc });
        builder.HasIndex(log => log.CorrelationId);
        builder.HasIndex(log => log.CreatedAtUtc);
        builder.HasIndex(log => log.Action);
    }
}
