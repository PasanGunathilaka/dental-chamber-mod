namespace DentalManagement.Infrastructure.Tests.Harness;

/// <summary>
/// Shares one PostgreSQL container across every test class in this assembly.
/// Individual tests still get their own fresh database.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres";
}
