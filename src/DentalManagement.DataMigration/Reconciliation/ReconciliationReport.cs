using System.Globalization;
using System.Text.Json;

namespace DentalManagement.DataMigration.Reconciliation;

/// <summary>
/// One source-versus-target comparison.
/// </summary>
public sealed record ReconciliationCheck(
    string Name,
    string Expected,
    string Actual,
    bool Passed,
    string? Note = null)
{
    public static ReconciliationCheck Count(string name, int expected, int actual, string? note = null) =>
        new(name,
            expected.ToString(CultureInfo.InvariantCulture),
            actual.ToString(CultureInfo.InvariantCulture),
            expected == actual,
            note);

    public static ReconciliationCheck Money(string name, decimal expected, decimal actual, string? note = null) =>
        new(name,
            expected.ToString("F2", CultureInfo.InvariantCulture),
            actual.ToString("F2", CultureInfo.InvariantCulture),
            expected == actual,
            note);
}

/// <summary>
/// The pre-cutover verdict: did everything that should have moved actually move,
/// and do the money totals still agree?
/// </summary>
/// <remarks>
/// SQ-005 asks for "migration validation and reconciliation checks before
/// production cutover". A reconciliation that cannot report failure is not a
/// check, so <see cref="Passed"/> drives the tool's exit code
/// (spec FR-22, AC-21, design D-9).
/// </remarks>
public sealed class ReconciliationReport(IReadOnlyList<ReconciliationCheck> checks)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IReadOnlyList<ReconciliationCheck> Checks { get; } = checks;

    public bool Passed => Checks.All(check => check.Passed);

    public IReadOnlyList<ReconciliationCheck> Failures => Checks
        .Where(check => !check.Passed)
        .ToList();

    public string ToJson() => JsonSerializer.Serialize(
        new
        {
            passed = Passed,
            checkCount = Checks.Count,
            failureCount = Failures.Count,
            checks = Checks,
        },
        JsonOptions);

    public string ToSummary()
    {
        if (Passed)
        {
            return $"Reconciliation: PASSED — all {Checks.Count} checks agree.";
        }

        var lines = Failures.Select(failure =>
            $"  {failure.Name}: expected {failure.Expected}, got {failure.Actual}"
            + (failure.Note is null ? string.Empty : $" ({failure.Note})"));

        return $"Reconciliation: FAILED — {Failures.Count} of {Checks.Count} checks disagree."
            + Environment.NewLine
            + string.Join(Environment.NewLine, lines);
    }
}
