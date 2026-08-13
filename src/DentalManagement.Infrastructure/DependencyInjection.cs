using DentalManagement.Domain.Abstractions;
using DentalManagement.Infrastructure.Identity;
using DentalManagement.Infrastructure.Persistence;
using DentalManagement.Infrastructure.Persistence.Seeding;
using DentalManagement.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DentalManagement.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the single DbContext, ASP.NET Core Identity, the clock, and the
    /// seeders.
    /// </summary>
    /// <remarks>
    /// The connection string is required, not defaulted: a missing one is a
    /// misconfigured deployment and should fail at startup rather than silently
    /// point somewhere unintended (spec FR-03).
    /// </remarks>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DentalManagement")
            ?? throw new InvalidOperationException(
                "Connection string 'DentalManagement' is not configured. Supply it through "
                + "environment configuration (ConnectionStrings__DentalManagement); it is "
                + "deliberately absent from appsettings.json.");

        services.AddDbContext<DentalDbContext>(options => options.UseNpgsql(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DentalDbContext>();

        services.AddSingleton<IClock, SystemClock>();

        var adminBootstrap = new AdminBootstrapOptions();
        configuration.GetSection(AdminBootstrapOptions.ConfigurationSection).Bind(adminBootstrap);
        services.AddSingleton(adminBootstrap);

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<AdminAccountSeeder>();

        return services;
    }
}
