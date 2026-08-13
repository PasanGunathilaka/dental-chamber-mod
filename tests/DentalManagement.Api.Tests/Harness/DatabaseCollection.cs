namespace DentalManagement.Api.Tests.Harness;

/// <summary>
/// Shares one PostgreSQL container across every test class in this assembly.
/// Individual tests still get their own freshly migrated database. Every test
/// class in this assembly declares this collection (spec R-5) so xunit never
/// runs two of them concurrently against the same container.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres-api";
}
