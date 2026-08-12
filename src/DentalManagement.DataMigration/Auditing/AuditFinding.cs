namespace DentalManagement.DataMigration.Auditing;

/// <summary>
/// One legacy value the new schema cannot accept as-is.
/// </summary>
/// <remarks>
/// A finding is a report, never a repair. CQ-007 and CQ-008 both say to report and
/// let a human review rather than discard or coerce: "report any legacy values
/// outside the known set so they can be reviewed rather than silently discarded",
/// and "audit legacy Charge strings during migration and explicitly report values
/// that cannot be parsed" (spec FR-21).
/// </remarks>
public sealed record AuditFinding(
    string Code,
    string Entity,
    string LegacyId,
    string Column,
    string? LegacyValue,
    string Detail);

/// <summary>
/// Audit codes.
/// </summary>
/// <remarks>
/// <see cref="NonIntegerCharge"/> reuses the code already defined in
/// <c>.specclaw/baseline/error-map.md</c>, whose entry still reads
/// "Rebuild source: not yet mapped". Its legacy condition was narrower than the
/// rebuild's — legacy rejected <c>"10.50"</c> too, because <c>TotalCharge</c> ran
/// through <c>Convert.ToInt32</c>, whereas after CQ-008 a fractional charge is
/// perfectly valid and only a genuinely unparseable string is a finding. Reusing
/// the existing code rather than inventing one is deliberate: renaming it (to
/// something like <c>UNPARSABLE_CHARGE</c>) and filling in its rebuild source
/// belongs to <c>/specclaw:bf-baseline</c>, not to this item.
/// </remarks>
public static class AuditCodes
{
    /// <summary>A <c>Charge</c> string that will not parse as a decimal.</summary>
    public const string NonIntegerCharge = "NON_INTEGER_CHARGE";

    /// <summary>A <c>Gender</c> value outside Male/Female/Others (CQ-007).</summary>
    public const string UnknownGender = "UNKNOWN_GENDER";

    /// <summary>
    /// A <c>StatusId</c> that does not belong to the owning entity's typed set —
    /// the integrity hole the shared legacy lookup table left open (CQ-006).
    /// </summary>
    public const string UnmappableStatus = "UNMAPPABLE_STATUS";

    /// <summary>
    /// Two rows share a value the new schema indexes uniquely, so they cannot both
    /// migrate (GM-002 shows legacy could produce this for <c>Patient.Code</c>).
    /// </summary>
    public const string DuplicateUniqueValue = "DUPLICATE_UNIQUE_VALUE";

    /// <summary>A null in a column the rebuild makes required.</summary>
    public const string MissingRequiredValue = "MISSING_REQUIRED_VALUE";

    /// <summary>
    /// A row referencing a parent that does not exist. Reported, not deleted:
    /// GM-019 proves legacy produced orphans deliberately enough that removing them
    /// would destroy data outside this item's mandate.
    /// </summary>
    public const string OrphanedReference = "ORPHANED_REFERENCE";
}
