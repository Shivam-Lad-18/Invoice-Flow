using InvoiceFlow.Domain.Enums;
using InvoiceFlow.Infrastructure.Identity;
using InvoiceFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Seeds test users for local development. Runs only in the Development environment.
/// All seed accounts use the password: Test@12345
/// </summary>
public static class DevDataSeeder
{
    private const string DefaultPassword = "Test@12345";

    private static readonly (string Email, string FirstName, string LastName, UserRole Role)[] SeedUsers =
    [
        ("admin@invoiceflow.dev",       "Alice",  "Admin",   UserRole.Admin),
        ("manager@invoiceflow.dev",     "Bob",    "Manager", UserRole.Manager),
        ("financehead@invoiceflow.dev", "Carol",  "Finance", UserRole.FinanceHead),
        ("cfo@invoiceflow.dev",         "Dave",   "CFO",     UserRole.CFO),
        ("employee@invoiceflow.dev",    "Eve",    "Employee",UserRole.Employee),
        ("vendor@invoiceflow.dev",      "Frank",  "Vendor",  UserRole.Vendor),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Apply any pending migrations automatically in dev
        await db.Database.MigrateAsync();

        foreach (var (email, firstName, lastName, role) in SeedUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue; // already exists

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Failed to seed user '{email}': {errors}");
            }
        }
    }
}
