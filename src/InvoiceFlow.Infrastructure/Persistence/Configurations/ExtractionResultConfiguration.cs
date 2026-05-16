using InvoiceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class ExtractionResultConfiguration : IEntityTypeConfiguration<ExtractionResult>
{
    public void Configure(EntityTypeBuilder<ExtractionResult> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VendorName).HasMaxLength(200);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(100);
        builder.Property(x => x.Currency).HasMaxLength(10);

        builder.Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.SubTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.TaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.ConfidenceScores)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.HasMany(x => x.LineItems)
            .WithOne(l => l.ExtractionResult)
            .HasForeignKey(l => l.ExtractionResultId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
