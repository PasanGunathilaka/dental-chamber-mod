using System.Text.Json;
using System.Text.Json.Serialization;

namespace DentalManagement.DataMigration.Auditing;

/// <summary>
/// Every legacy value the new schema cannot accept, grouped for reading.
/// </summary>
/// <remarks>
/// Serializes to JSON so the result can be attached to a verify report rather than
/// only scrolling past in a console (spec NFR-05).
/// </remarks>
public sealed class AuditReport(IReadOnlyList<AuditFinding> findings)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public IReadOnlyList<AuditFinding> Findings { get; } = findings;

    public bool HasFindings => Findings.Count > 0;

    /// <summary>
    /// Findings that block a row from migrating at all, as opposed to those recorded
    /// for review while the row still migrates.
    /// </summary>
    /// <remarks>
    /// An unparseable <c>Charge</c>, a missing required <c>Code</c>, and a
    /// unique-value collision each stop a row; an unknown gender, an unmappable
    /// status, and an orphaned reference do not — those rows migrate with the
    /// offending value left null or preserved as-is, and the finding is the record
    /// that a human must look at it.
    /// </remarks>
    public IReadOnlyList<AuditFinding> Blocking => Findings
        .Where(finding => finding.Code is AuditCodes.NonIntegerCharge
            or AuditCodes.MissingRequiredValue
            or AuditCodes.DuplicateUniqueValue)
        .ToList();

    public IReadOnlyDictionary<string, int> CountsByCode => Findings
        .GroupBy(finding => finding.Code, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    public IReadOnlyList<AuditFinding> WithCode(string code) => Findings
        .Where(finding => string.Equals(finding.Code, code, StringComparison.Ordinal))
        .ToList();

    public string ToJson() => JsonSerializer.Serialize(
        new
        {
            findingCount = Findings.Count,
            blockingCount = Blocking.Count,
            countsByCode = CountsByCode,
            findings = Findings,
        },
        JsonOptions);

    public string ToSummary()
    {
        if (!HasFindings)
        {
            return "Audit: no findings — every legacy value fits the rebuild schema.";
        }

        var lines = CountsByCode
            .OrderByDescending(entry => entry.Value)
            .Select(entry => $"  {entry.Key}: {entry.Value}");

        return $"Audit: {Findings.Count} finding(s), {Blocking.Count} blocking."
            + Environment.NewLine
            + string.Join(Environment.NewLine, lines);
    }
}
