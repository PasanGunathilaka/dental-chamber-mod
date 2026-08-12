namespace DentalManagement.DataMigration.LegacyReaders;

/// <summary>
/// Reads the legacy database in full.
/// </summary>
/// <remarks>
/// An interface rather than a concrete reader so the audit and reconciliation
/// logic can be exercised against constructed data without standing up SQL Server,
/// while the end-to-end path still runs through
/// <see cref="SqlServerLegacyDataSource"/> against a real server (spec AC-19).
/// </remarks>
public interface ILegacyDataSource
{
    Task<LegacyDatabase> ReadAllAsync(CancellationToken cancellationToken = default);
}
