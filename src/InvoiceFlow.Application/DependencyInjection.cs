using FluentValidation;
using InvoiceFlow.Application.Common.Behaviours;
using InvoiceFlow.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace InvoiceFlow.Application;

/// <summary>Application layer service registration. Called from API Program.cs.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Domain services — pure logic, no external dependencies, safe as transients
        services.AddTransient<InvoiceDomainService>();
        services.AddTransient<ApprovalStepDomainService>();
        services.AddTransient<ApprovalWorkflowDomainService>();
        services.AddTransient<ExtractionResultDomainService>();
        services.AddTransient<VendorDomainService>();
        services.AddTransient<RefreshTokenDomainService>();
        services.AddTransient<AuditLogFactory>();
        services.AddTransient<InvoiceLineItemFactory>();
        services.AddTransient<ApprovalRuleDomainService>();

        return services;
    }
}
