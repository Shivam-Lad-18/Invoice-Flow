using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Domain.Enums;
using InvoiceFlow.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class ApprovalRuleConfiguration : IEntityTypeConfiguration<ApprovalRule>
{
    public void Configure(EntityTypeBuilder<ApprovalRule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MaxAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.RequiredRoles)
            .IsRequired()
            .HasColumnType("nvarchar(500)");

        // Seed default approval rules (updatable by Admin at runtime via ApprovalRuleDomainService).
        // Static date is required — dynamic values (DateTime.UtcNow) cause EF PendingModelChangesWarning.
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var service = new ApprovalRuleDomainService();
        builder.HasData(
            service.Create(
                id: 1,
                maxAmount: 50_000m,
                requiredRolesJson: $"[{(int)UserRole.Manager}]",
                updatedByUserId: Guid.Empty,
                lastUpdatedAt: seedDate),

            service.Create(
                id: 2,
                maxAmount: 500_000m,
                requiredRolesJson: $"[{(int)UserRole.Manager},{(int)UserRole.FinanceHead}]",
                updatedByUserId: Guid.Empty,
                lastUpdatedAt: seedDate),

            service.Create(
                id: 3,
                maxAmount: 999_999_999_999_999.99m, // catch-all tier (~1 quadrillion); decimal.MaxValue overflows decimal(18,2)
                requiredRolesJson: $"[{(int)UserRole.Manager},{(int)UserRole.FinanceHead},{(int)UserRole.CFO}]",
                updatedByUserId: Guid.Empty,
                lastUpdatedAt: seedDate)
        );
    }
}
