using DentalManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DentalManagement.Api.Tests.Harness;

/// <summary>
/// One real PostgreSQL server for the whole test assembly, handing out a fresh
/// migrated database per test that needs one.
/// </summary>
/// <remarks>
/// Follows the same shape as
/// <c>DentalManagement.Infrastructure.Tests.Harness.PostgresContainerFixture</c>
/// (design D-9) — a second copy rather than a shared project reference,
/// because this assembly boots a web host over the database rather than
/// exercising <c>DentalDbContext</c> directly. PostgreSQL only, no SQL Server:
/// this suite joins the fast tier (spec R-5).
/// </remarks>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private const string RootDatabase = "dentalmanagement_api_root";

    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase(RootDatabase)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Creates a brand-new, genuinely empty database and returns its
    /// connection string.
    /// </summary>
    public async Task<string> CreateEmptyDatabaseAsync(CancellationToken cancellationToken)
    {
        var databaseName = $"dm_api_{Guid.NewGuid():N}";

        await using var rootContext = CreateContext(_container.GetConnectionString());

        // A database name cannot be a bound parameter in DDL, so the statement
        // is necessarily built as text. The value is a freshly generated GUID
        // with a fixed prefix, never caller-supplied — there is no injection
        // surface.
#pragma warning disable EF1002
        await rootContext.Database.ExecuteSqlRawAsync(
            $"CREATE DATABASE \"{databaseName}\"",
            cancellationToken);
#pragma warning restore EF1002

        return _container.GetConnectionString()
            .Replace($"Database={RootDatabase}", $"Database={databaseName}", StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a fresh empty database and applies the full migration chain to
    /// it — the database each test's <c>WebApplicationFactory&lt;Program&gt;</c>
    /// boots against.
    /// </summary>
    public async Task<string> CreateMigratedDatabaseAsync(CancellationToken cancellationToken)
    {
        var connectionString = await CreateEmptyDatabaseAsync(cancellationToken);

        await using var context = CreateContext(connectionString);
        await context.Database.MigrateAsync(cancellationToken);

        return connectionString;
    }

    public static DentalDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DentalDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new DentalDbContext(options);
    }
}
