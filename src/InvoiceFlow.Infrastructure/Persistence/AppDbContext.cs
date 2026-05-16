using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace InvoiceFlow.Infrastructure.Persistence;

/// <summary>
/// Main EF Core DbContext. Extends IdentityDbContext to include ASP.NET Core Identity tables.
/// All entity configurations are loaded from the Configurations/ folder via reflection.
/// Implements IApplicationDbContext so the Application layer can be tested without EF.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options),
      IApplicationDbContext
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<ExtractionResult> ExtractionResults => Set<ExtractionResult>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApprovalRule> ApprovalRules => Set<ApprovalRule>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Load all IEntityTypeConfiguration<T> classes from this assembly automatically
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Rename default Identity tables to a cleaner schema
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
    }
}
