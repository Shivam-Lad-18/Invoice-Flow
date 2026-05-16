using InvoiceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.RevokedReason)
            .HasMaxLength(100);

        builder.Property(x => x.ReplacedByToken)
            .HasMaxLength(256);

        // Token lookup on refresh endpoint
        builder.HasIndex(x => x.Token).IsUnique();

        // Cleanup queries — find expired or revoked tokens by user
        builder.HasIndex(x => new { x.UserId, x.IsRevoked, x.ExpiresAt });
    }
}
