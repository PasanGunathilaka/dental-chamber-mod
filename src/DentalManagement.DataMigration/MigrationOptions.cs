namespace DentalManagement.DataMigration;

/// <summary>How one migration run is configured.</summary>
public sealed class MigrationOptions
{
    public required string SourceConnectionString { get; init; }

    public required string TargetConnectionString { get; init; }

    /// <summary>
    /// Read, audit, and reconcile without writing anything.
    /// </summary>
    /// <remarks>
    /// The audit is the whole reason this mode exists: it answers "what would this
    /// migration report?" before anyone touches the target.
    /// </remarks>
    public bool DryRun { get; init; }

    /// <summary>
    /// Permits a target that already holds domain rows.
    /// </summary>
    /// <remarks>
    /// Off by default. A one-way bulk data move onto a non-empty target is how you
    /// get a half-merged database that nobody can reason about, so the tool refuses
    /// unless told explicitly (spec FR-23, AC-22, design D-6). A target holding only
    /// seed data (roles, resources, the default doctor) is still considered empty —
    /// that is the expected state after a fresh migrate-and-seed.
    /// </remarks>
    public bool AllowNonEmptyTarget { get; init; }

    /// <summary>Directory the audit and reconciliation JSON reports are written to.</summary>
    public string? ReportDirectory { get; init; }
}

/// <summary>What a migration run produced.</summary>
public sealed record MigrationOutcome(
    bool Succeeded,
    string Message,
    Auditing.AuditReport? Audit = null,
    Reconciliation.ReconciliationReport? Reconciliation = null)
{
    /// <summary>Process exit code: 0 only when the run succeeded outright.</summary>
    public int ExitCode => Succeeded ? 0 : 1;
}
