using DentalManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DentalManagement.Infrastructure.Tests.Harness;

/// <summary>
/// One real PostgreSQL server for the whole test assembly, handing out a fresh
/// empty database per test that needs one.
/// </summary>
/// <remarks>
/// Deliberately not the EF in-memory provider. Every acceptance criterion in this
/// assembly turns on behaviour in-memory does not model: cascade chains
/// (GM-012/GM-024), the absence of a cascade (GM-019), unique-index rejection at
/// the database, check constraints, and <c>numeric</c> precision. Testing those
/// against in-memory would produce green tests that prove nothing
/// (spec NFR-07, design D-7).
/// </remarks>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private const string RootDatabase = "dentalmanagement_root";

    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase(RootDatabase)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Creates a brand-new, genuinely empty database and returns its connection
    /// string.
    /// </summary>
    /// <remarks>
    /// "Genuinely empty" is the point, not a convenience. The legacy EF6 chain
    /// failed precisely and only against an empty database — it re-created
    /// <c>IX_Code</c> with no preceding <c>DropIndex</c> — and that defect stayed
    /// invisible for as long as anyone tested against an already-seeded database
    /// (spec FR-16, AC-03).
    /// </remarks>
    public async Task<string> CreateEmptyDatabaseAsync(CancellationToken cancellationToken)
    {
        var databaseName = $"dm_{Guid.NewGuid():N}";

        await using var rootContext = CreateContext(_container.GetConnectionString());

        // A database name cannot be a bound parameter in DDL, so the statement is
        // necessarily built as text. The value is a freshly generated GUID with a
        // fixed prefix, never caller-supplied — there is no injection surface.
#pragma warning disable EF1002
        await rootContext.Database.ExecuteSqlRawAsync(
            $"CREATE DATABASE \"{databaseName}\"",
            cancellationToken);
#pragma warning restore EF1002

        return _container.GetConnectionString()
            .Replace($"Database={RootDatabase}", $"Database={databaseName}", StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a fresh empty database and applies the full migration chain to it.
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
