using InvoiceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequiredRole)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        // "My pending approvals" query — most frequent read path
        builder.HasIndex(x => new { x.AssignedToUserId, x.Status });

        // Azure Function reminder queries — pending steps with no reminder yet
        builder.HasIndex(x => new { x.Status, x.CreatedAt });

        // Invoice-scoped workflow view
        builder.HasIndex(x => x.InvoiceId);
    }
}
