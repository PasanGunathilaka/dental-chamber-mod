using DentalManagement.Domain.Abstractions;
using DentalManagement.Infrastructure.Identity;
using DentalManagement.Infrastructure.Persistence;
using DentalManagement.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DentalManagement.Infrastructure.Tests.Harness;

/// <summary>
/// Builds the Identity service graph the seeders need, over a real database.
/// </summary>
/// <remarks>
/// <see cref="RoleManager{TRole}"/> and <see cref="UserManager{TUser}"/> are
/// concrete framework types with substantial dependency graphs, so a real service
/// provider is cheaper and more faithful than hand-mocking them — and the
/// behaviour under test (Identity's own writes, the unique index on the user-roles
/// join table) only exists when both are real.
/// </remarks>
public sealed class SeederHarness : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    private SeederHarness(ServiceProvider provider) => _provider = provider;

    public static SeederHarness Create(string connectionString, AdminBootstrapOptions? admin = null)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<DentalDbContext>(options => options.UseNpgsql(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DentalDbContext>();

        services.AddSingleton<IClock>(new FixedClock(TestEntities.FixedNow));
        services.AddSingleton(admin ?? new AdminBootstrapOptions
        {
            AllowDevelopmentDemoAccounts = true,
        });

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<AdminAccountSeeder>();

        return new SeederHarness(services.BuildServiceProvider());
    }

    public T Resolve<T>() where T : notnull => _provider.GetRequiredService<T>();

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime Now => now;
    }
}
