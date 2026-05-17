using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Functions.Data;

/// <summary>
/// Lightweight EF Core context for the Functions worker.
/// Contains only the tables needed by extraction and notification functions.
/// Schema matches the migration applied by InvoiceFlow.Infrastructure exactly.
/// </summary>
public sealed class FunctionsDbContext(DbContextOptions<FunctionsDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ExtractionResult> ExtractionResults => Set<ExtractionResult>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ignore domain event collection — not a DB column
        modelBuilder.Ignore<System.Collections.Generic.IReadOnlyCollection<InvoiceFlow.Domain.Common.IDomainEvent>>();

        // ── Invoice ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.BlobPath).HasMaxLength(1024);
            e.Property(x => x.OriginalFileName).HasMaxLength(512);
            e.Property(x => x.DuplicateCheckHash).HasMaxLength(64);
            e.Ignore(x => x.DomainEvents);
            // Navigation properties not needed — write-only from Functions
            e.Ignore(x => x.Vendor);
            e.Ignore(x => x.ExtractionResult);
            e.Ignore(x => x.ApprovalWorkflow);
            e.Ignore(x => x.AuditLogs);
        });

        // ── ExtractionResult ─────────────────────────────────────────────────
        modelBuilder.Entity<ExtractionResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.VendorName).HasMaxLength(256);
            e.Property(x => x.InvoiceNumber).HasMaxLength(128);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Ignore(x => x.DomainEvents);
            e.Ignore(x => x.Invoice);
            e.Ignore(x => x.LineItems);
        });

        // ── InvoiceLineItem ───────────────────────────────────────────────────
        modelBuilder.Entity<InvoiceLineItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Confidence).HasPrecision(5, 4);
            e.Property(x => x.Description).HasMaxLength(512);
            e.Ignore(x => x.DomainEvents);
        });

        // ── AuditLog ─────────────────────────────────────────────────────────
        // AuditLog does NOT extend BaseEntity — no DomainEvents to ignore.
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(128);
            e.Property(x => x.OldValue).HasMaxLength(4000);
            e.Property(x => x.NewValue).HasMaxLength(4000);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.CorrelationId).HasMaxLength(128);
            e.Ignore(x => x.Invoice);
        });
    }
}
