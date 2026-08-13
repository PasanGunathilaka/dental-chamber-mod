using DentalManagement.Infrastructure.Tests.Harness;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Infrastructure.Tests;

/// <summary>
/// The direct guard against the defect that motivated BL-001: the legacy EF6
/// chain could not build a fresh database at all.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class MigrationTests(PostgresContainerFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>AC-03 — the chain applies to an empty database in one pass.</summary>
    [Fact]
    public async Task Migration_chain_applies_to_a_genuinely_empty_database()
    {
        var connectionString = await postgres.CreateEmptyDatabaseAsync(Ct);

        await using var context = PostgresContainerFixture.CreateContext(connectionString);

        // Legacy's equivalent step threw here every time: InitialCreate created
        // Patient's unique IX_Code, and a later migration ran CreateIndex on the
        // same column with no preceding DropIndex.
        await context.Database.MigrateAsync(Ct);

        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync(Ct));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(Ct));
    }

    /// <summary>AC-03 — every expected table exists after migrating.</summary>
    [Theory]
    [InlineData("public", "Patient")]
    [InlineData("public", "Prescription")]
    [InlineData("public", "PatientMedicalService")]
    [InlineData("public", "MedicalService")]
    [InlineData("public", "MedicalInfo")]
    [InlineData("public", "PatientMedicalInfo")]
    [InlineData("public", "Payment")]
    [InlineData("public", "Product")]
    [InlineData("public", "Inventory")]
    [InlineData("public", "Doctor")]
    [InlineData("public", "Appointment")]
    [InlineData("identity", "Resource")]
    [InlineData("identity", "Permission")]
    [InlineData("identity", "AspNetUsers")]
    [InlineData("identity", "AspNetRoles")]
    [InlineData("identity", "AspNetUserRoles")]
    public async Task Migrated_schema_contains_table(string schema, string table)
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var context = PostgresContainerFixture.CreateContext(connectionString);

        var exists = await context.Database
            .SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = {schema} AND table_name = {table}
                ) AS "Value"
                """)
            .SingleAsync(Ct);

        Assert.True(exists, $"expected table {schema}.{table} to exist after migration");
    }

    /// <summary>
    /// AC-09 — CQ-006 replaced the shared lookup table with typed enums, so the
    /// table itself must be gone rather than merely unused.
    /// </summary>
    [Fact]
    public async Task Migrated_schema_has_no_Status_lookup_table()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var context = PostgresContainerFixture.CreateContext(connectionString);

        var statusTables = await context.Database
            .SqlQuery<string>($"""
                SELECT table_name AS "Value" FROM information_schema.tables
                WHERE table_name = 'Status'
                """)
            .ToListAsync(Ct);

        Assert.Empty(statusTables);
    }

    /// <summary>
    /// AC-04 — no index is created twice across the whole chain. This is the
    /// legacy defect stated as an assertion: duplicate index creation with no
    /// intervening drop is exactly what made <c>Update()</c> fail on an empty
    /// database.
    /// </summary>
    [Fact]
    public async Task No_index_is_created_more_than_once_across_the_chain()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var context = PostgresContainerFixture.CreateContext(connectionString);

        var duplicated = await context.Database
            .SqlQuery<string>($"""
                SELECT indexname AS "Value" FROM pg_indexes
                WHERE schemaname IN ('public', 'identity')
                GROUP BY indexname HAVING COUNT(*) > 1
                """)
            .ToListAsync(Ct);

        Assert.Empty(duplicated);
    }

    /// <summary>
    /// AC-02 — CQ-002 requires one context and one migration history. A second
    /// history would show up as a second migrations table.
    /// </summary>
    [Fact]
    public async Task Exactly_one_migration_history_table_exists()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var context = PostgresContainerFixture.CreateContext(connectionString);

        var historyTables = await context.Database
            .SqlQuery<string>($"""
                SELECT table_name AS "Value" FROM information_schema.tables
                WHERE table_name LIKE '%MigrationsHistory%'
                """)
            .ToListAsync(Ct);

        Assert.Single(historyTables);
    }

    /// <summary>
    /// AC-03/FR-16 — the chain is authored fresh for PostgreSQL, so it must not
    /// carry a migration named after the legacy EF6 chain's own steps.
    /// </summary>
    [Fact]
    public async Task Chain_does_not_reuse_the_legacy_EF6_migration_history()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        await using var context = PostgresContainerFixture.CreateContext(connectionString);

        var applied = (await context.Database.GetAppliedMigrationsAsync(Ct)).ToList();

        Assert.DoesNotContain(applied, migration =>
            migration.Contains("Patient_Code_Unique", StringComparison.OrdinalIgnoreCase));
    }
}
