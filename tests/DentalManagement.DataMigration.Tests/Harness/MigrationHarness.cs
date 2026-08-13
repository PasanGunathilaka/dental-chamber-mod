using DentalManagement.Domain.Abstractions;
using DentalManagement.Infrastructure.Persistence;
using DentalManagement.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace DentalManagement.DataMigration.Tests.Harness;

/// <summary>
/// A real legacy SQL Server and a real target PostgreSQL, for the whole assembly.
/// </summary>
/// <remarks>
/// Both sides are the real engines. The legacy side has to be genuine SQL Server
/// because <c>SqlServerLegacyDataSource</c>'s value is precisely that its SQL
/// matches the legacy schema — testing it against a stand-in would verify nothing
/// about the reader (spec AC-19).
/// </remarks>
public sealed class MigrationHarness : IAsyncLifetime
{
    private const string RootDatabase = "dentalmanagement_root";

    private readonly MsSqlContainer _legacy =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private readonly PostgreSqlContainer _target =
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase(RootDatabase)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_legacy.StartAsync(), _target.StartAsync());
    }

    public async ValueTask DisposeAsync()
    {
        await _legacy.DisposeAsync();
        await _target.DisposeAsync();
    }

    /// <summary>
    /// Creates a legacy-shaped SQL Server database and applies the requested
    /// scripts.
    /// </summary>
    /// <remarks>
    /// The scripts under <c>SyntheticLegacy/</c> are the single source of truth for
    /// this data — the same files someone would run to rehearse against a real
    /// legacy schema — so the tests read them rather than duplicating the rows in C#.
    /// </remarks>
    public async Task<string> CreateLegacyDatabaseAsync(
        bool includeProblemData,
        CancellationToken cancellationToken)
    {
        var databaseName = $"legacy_{Guid.NewGuid():N}";

        await ExecuteOnLegacyAsync(
            _legacy.GetConnectionString(),
            $"CREATE DATABASE [{databaseName}]",
            cancellationToken);

        var connectionString = _legacy.GetConnectionString()
            .Replace("Database=master", $"Database={databaseName}", StringComparison.Ordinal);

        var scripts = includeProblemData
            ? new[] { "01-schema.sql", "02-clean-data.sql", "03-problem-data.sql" }
            : ["01-schema.sql", "02-clean-data.sql"];

        foreach (var script in scripts)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "SyntheticLegacy", script);
            var sql = await File.ReadAllTextAsync(path, cancellationToken);
            await ExecuteOnLegacyAsync(connectionString, sql, cancellationToken);
        }

        return connectionString;
    }

    /// <summary>Creates a fresh target database, migrated and seeded.</summary>
    public async Task<string> CreateSeededTargetAsync(CancellationToken cancellationToken)
    {
        var connectionString = await CreateMigratedTargetAsync(cancellationToken);
        await SeedAsync(connectionString, cancellationToken);

        return connectionString;
    }

    /// <summary>
    /// Runs the fresh-install seeder against an existing target.
    /// </summary>
    /// <remarks>
    /// Exposed separately because the migration runbook seeds <i>after</i> migrating,
    /// so its own guards fill only what the legacy database did not supply.
    /// </remarks>
    public static async Task SeedAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var provider = BuildSeedingProvider(connectionString);
        await provider.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
    }

    /// <summary>Creates a fresh target database with the schema but no seed data.</summary>
    public async Task<string> CreateMigratedTargetAsync(CancellationToken cancellationToken)
    {
        var databaseName = $"dm_{Guid.NewGuid():N}";

        await using (var root = CreateTargetContext(_target.GetConnectionString()))
        {
            // A database name cannot be a bound parameter in DDL; the value is a
            // generated GUID, so there is no injection surface.
#pragma warning disable EF1002
            await root.Database.ExecuteSqlRawAsync(
                $"CREATE DATABASE \"{databaseName}\"",
                cancellationToken);
#pragma warning restore EF1002
        }

        var connectionString = _target.GetConnectionString()
            .Replace($"Database={RootDatabase}", $"Database={databaseName}", StringComparison.Ordinal);

        await using var context = CreateTargetContext(connectionString);
        await context.Database.MigrateAsync(cancellationToken);

        return connectionString;
    }

    public static DentalDbContext CreateTargetContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DentalDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new DentalDbContext(options);
    }

    private static ServiceProvider BuildSeedingProvider(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<DentalDbContext>(options => options.UseNpgsql(connectionString));
        services
            .AddIdentityCore<Infrastructure.Identity.ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DentalDbContext>();
        services.AddSingleton<IClock>(new FixedClock());
        services.AddSingleton(new AdminBootstrapOptions { AllowDevelopmentDemoAccounts = true });
        services.AddScoped<DatabaseSeeder>();

        return services.BuildServiceProvider();
    }

    private static async Task ExecuteOnLegacyAsync(
        string connectionString,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime Now { get; } = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Unspecified);
    }
}

[CollectionDefinition(Name)]
public sealed class MigrationCollection : ICollectionFixture<MigrationHarness>
{
    public const string Name = "migration";
}
