using InvoiceFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Application.Common.Interfaces;

/// <summary>
/// Abstracts EF Core DbContext from the Application layer.
/// Implemented in Infrastructure; mockable for unit tests.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Invoice> Invoices { get; }
    DbSet<Vendor> Vendors { get; }
    DbSet<ExtractionResult> ExtractionResults { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<ApprovalWorkflow> ApprovalWorkflows { get; }
    DbSet<ApprovalStep> ApprovalSteps { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ApprovalRule> ApprovalRules { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
