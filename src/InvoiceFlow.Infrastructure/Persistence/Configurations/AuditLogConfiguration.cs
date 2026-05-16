using InvoiceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OldValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.NewValue)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(36); // GUID string

        // Audit trail for a specific invoice, sorted by time
        builder.HasIndex(x => new { x.InvoiceId, x.Timestamp });

        // Admin audit log search by action + time
        builder.HasIndex(x => new { x.Action, x.Timestamp });

        // Append-only — no update or delete tracking needed
        builder.ToTable(tb => tb.HasComment("Append-only audit log. No UPDATE or DELETE is permitted."));
    }
}
