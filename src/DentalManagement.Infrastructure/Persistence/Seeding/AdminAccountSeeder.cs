using DentalManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace DentalManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Creates the first administrator account, by a different route in development
/// than in production.
/// </summary>
/// <remarks>
/// The legacy seed shipped two accounts sharing the hardcoded password
/// <c>"123qwe"</c> with no forced rotation — a default-credential exposure if it
/// ever reached a real deployment. CQ-017 splits the two cases: known demo
/// credentials are fine in an explicitly development seed, and production must take
/// environment-specific credentials or force a first-login reset (spec FR-19).
/// </remarks>
public class AdminAccountSeeder(
    UserManager<ApplicationUser> userManager,
    AdminBootstrapOptions options)
{
    /// <summary>
    /// The two legacy demo usernames, kept so a developer's muscle memory still
    /// works locally.
    /// </summary>
    private static readonly string[] DemoUserNames = ["superadmin", "admin"];

    /// <summary>
    /// Development-only password. Never reachable unless
    /// <see cref="AdminBootstrapOptions.AllowDevelopmentDemoAccounts"/> is
    /// explicitly true, and never used for the production path below.
    /// </summary>
    private const string DevelopmentDemoPassword = "Dev!Local!Only!2026";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (options.AllowDevelopmentDemoAccounts)
        {
            await SeedDevelopmentDemoAccountsAsync();
            return;
        }

        await BootstrapProductionAdministratorAsync();
    }

    private async Task SeedDevelopmentDemoAccountsAsync()
    {
        foreach (var userName in DemoUserNames)
        {
            if (await userManager.FindByNameAsync(userName) is not null)
            {
                continue;
            }

            await CreateAsync(userName, $"{userName}@localhost", DevelopmentDemoPassword);
        }
    }

    /// <summary>
    /// Creates the production administrator from configured credentials.
    /// </summary>
    /// <remarks>
    /// Fails loudly when the credentials are absent rather than falling back to a
    /// default. A silent fallback is precisely the legacy defect (spec AC-18).
    /// </remarks>
    private async Task BootstrapProductionAdministratorAsync()
    {
        if (string.IsNullOrWhiteSpace(options.UserName)
            || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                "Cannot bootstrap the administrator account: " +
                $"'{AdminBootstrapOptions.ConfigurationSection}:UserName' and " +
                $"'{AdminBootstrapOptions.ConfigurationSection}:Password' must both be " +
                "supplied through environment configuration. There is deliberately no " +
                "default password — set both values, or set " +
                $"'{AdminBootstrapOptions.ConfigurationSection}:AllowDevelopmentDemoAccounts' " +
                "for a local development database only.");
        }

        if (await userManager.FindByNameAsync(options.UserName) is not null)
        {
            return;
        }

        await CreateAsync(options.UserName, options.UserName, options.Password);
    }

    private async Task CreateAsync(string userName, string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create administrator '{userName}': " +
                string.Join("; ", created.Errors.Select(error => error.Description)));
        }

        // CQ-015: exactly one primary role per user. The unique index on the
        // Identity user-roles join table enforces this at the database, so a second
        // assignment here would fail outright rather than silently widening access.
        var assigned = await userManager.AddToRoleAsync(user, SeedCatalog.SystemAdminRole);
        if (!assigned.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not assign '{SeedCatalog.SystemAdminRole}' to '{userName}': " +
                string.Join("; ", assigned.Errors.Select(error => error.Description)));
        }

        if (options.RequirePasswordChangeOnFirstLogin
            && !options.AllowDevelopmentDemoAccounts)
        {
            // Invalidating the stamp forces re-authentication; the login flow that
            // acts on it belongs to BL-002, so this records the requirement in the
            // account's own state rather than implementing a UI for it here.
            await userManager.UpdateSecurityStampAsync(user);
        }
    }
}
