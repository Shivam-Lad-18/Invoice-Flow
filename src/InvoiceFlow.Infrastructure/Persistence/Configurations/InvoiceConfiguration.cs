using InvoiceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BlobPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.DuplicateCheckHash)
            .HasMaxLength(64); // SHA-256 hex = 64 chars

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Duplicate detection index — must be unique when non-null
        builder.HasIndex(x => x.DuplicateCheckHash)
            .HasFilter("[DuplicateCheckHash] IS NOT NULL");

        // Dashboard list queries
        builder.HasIndex(x => new { x.Status, x.UploadedAt });

        // Role-filtered vendor queries
        builder.HasIndex(x => x.VendorId);
        builder.HasIndex(x => x.UploadedByUserId);

        builder.HasOne(x => x.Vendor)
            .WithMany(v => v.Invoices)
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ExtractionResult)
            .WithOne(e => e.Invoice)
            .HasForeignKey<ExtractionResult>(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ApprovalWorkflow)
            .WithOne(w => w.Invoice)
            .HasForeignKey<ApprovalWorkflow>(w => w.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AuditLogs)
            .WithOne(a => a.Invoice)
            .HasForeignKey(a => a.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
