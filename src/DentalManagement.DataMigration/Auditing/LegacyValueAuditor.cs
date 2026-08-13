using System.Globalization;
using DentalManagement.DataMigration.LegacyReaders;
using DentalManagement.Domain.Enums;

namespace DentalManagement.DataMigration.Auditing;

/// <summary>
/// Inspects every legacy value the new schema constrains, and reports the ones it
/// cannot accept.
/// </summary>
/// <remarks>
/// Runs before any write, so a migration never begins by discovering half way
/// through that a value will not fit (spec FR-21, AC-20).
/// </remarks>
public sealed class LegacyValueAuditor
{
    private static readonly Dictionary<string, Gender> KnownGenders = new(StringComparer.Ordinal)
    {
        ["Male"] = Gender.Male,
        ["Female"] = Gender.Female,
        ["Others"] = Gender.Others,
    };

    /// <summary>
    /// Parses a legacy <c>Charge</c> string.
    /// </summary>
    /// <remarks>
    /// Invariant culture only, and deliberately no <c>AllowThousands</c> or
    /// <c>AllowCurrencySymbol</c>: accepting <c>"Rs. 1,500"</c> would mean guessing
    /// both a currency and a grouping convention, and CQ-008 asks for a report, not
    /// a guess.
    /// </remarks>
    public static bool TryParseCharge(string? legacyCharge, out decimal charge)
    {
        charge = 0m;

        return !string.IsNullOrWhiteSpace(legacyCharge)
            && decimal.TryParse(
                legacyCharge,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out charge);
    }

    /// <summary>
    /// Maps a legacy gender string, matching case-sensitively against the three
    /// known values.
    /// </summary>
    /// <remarks>
    /// Case-sensitive on purpose. <c>"male"</c> is reported rather than silently
    /// normalized, because the legacy client hardcoded the exact strings
    /// <c>Male</c>/<c>Female</c>/<c>Others</c> — anything else was written by some
    /// other path, and CQ-007 asks that such values be reviewed. Normalizing would
    /// hide how the row got there.
    /// </remarks>
    public static bool TryMapGender(string? legacyGender, out Gender? gender)
    {
        gender = null;

        if (legacyGender is null)
        {
            // Legacy allowed a null gender and the rebuild's column is nullable, so
            // this is a legitimate value rather than a finding.
            return true;
        }

        if (KnownGenders.TryGetValue(legacyGender, out var mapped))
        {
            gender = mapped;
            return true;
        }

        return false;
    }

    public static bool TryMapStatus<TEnum>(int legacyStatusId, out TEnum status)
        where TEnum : struct, Enum
    {
        status = default;

        if (!Enum.IsDefined(typeof(TEnum), legacyStatusId))
        {
            return false;
        }

        status = (TEnum)Enum.ToObject(typeof(TEnum), legacyStatusId);
        return true;
    }

    public AuditReport Audit(LegacyDatabase legacy)
    {
        var findings = new List<AuditFinding>();

        AuditCharges(legacy, findings);
        AuditGenders(legacy, findings);
        AuditStatuses(legacy, findings);
        AuditRequiredValues(legacy, findings);
        AuditUniqueValues(legacy, findings);
        AuditOrphans(legacy, findings);

        return new AuditReport(findings);
    }

    private static void AuditCharges(LegacyDatabase legacy, List<AuditFinding> findings)
    {
        foreach (var service in legacy.MedicalServices)
        {
            if (!TryParseCharge(service.Charge, out _))
            {
                findings.Add(new AuditFinding(
                    AuditCodes.NonIntegerCharge,
                    nameof(legacy.MedicalServices),
                    service.Id.ToString(),
                    nameof(service.Charge),
                    service.Charge,
                    $"'{service.Charge ?? "<null>"}' is not a decimal currency value. "
                    + "Legacy stored Charge as a string; CQ-008 requires the value be "
                    + "reviewed rather than coerced to zero."));
            }
        }
    }

    private static void AuditGenders(LegacyDatabase legacy, List<AuditFinding> findings)
    {
        foreach (var patient in legacy.Patients)
        {
            if (!TryMapGender(patient.Gender, out _))
            {
                findings.Add(new AuditFinding(
                    AuditCodes.UnknownGender,
                    nameof(legacy.Patients),
                    patient.Id.ToString(),
                    nameof(patient.Gender),
                    patient.Gender,
                    $"'{patient.Gender}' is outside the known set "
                    + "(Male, Female, Others). CQ-007 requires it be reported for "
                    + "review, not discarded."));
            }
        }
    }

    private static void AuditStatuses(LegacyDatabase legacy, List<AuditFinding> findings)
    {
        foreach (var prescription in legacy.Prescriptions)
        {
            if (!TryMapStatus<BillStatus>(prescription.StatusId, out _))
            {
                findings.Add(StatusFinding(
                    nameof(legacy.Prescriptions),
                    prescription.Id.ToString(),
                    prescription.StatusId,
                    nameof(BillStatus)));
            }
        }

        foreach (var product in legacy.Products)
        {
            if (!TryMapStatus<ProductStatus>(product.StatusId, out _))
            {
                findings.Add(StatusFinding(
                    nameof(legacy.Products),
                    product.Id.ToString(),
                    product.StatusId,
                    nameof(ProductStatus)));
            }
        }

        foreach (var movement in legacy.Inventories)
        {
            if (!TryMapStatus<InventoryMovementStatus>(movement.StatusId, out _))
            {
                findings.Add(StatusFinding(
                    nameof(legacy.Inventories),
                    movement.Id.ToString(),
                    movement.StatusId,
                    nameof(InventoryMovementStatus)));
            }
        }

        foreach (var appointment in legacy.Appointments)
        {
            if (!TryMapStatus<AppointmentStatus>(appointment.StatusId, out _))
            {
                findings.Add(StatusFinding(
                    nameof(legacy.Appointments),
                    appointment.Id.ToString(),
                    appointment.StatusId,
                    nameof(AppointmentStatus)));
            }
        }
    }

    private static AuditFinding StatusFinding(
        string entity,
        string legacyId,
        int statusId,
        string expectedEnum) =>
        new(AuditCodes.UnmappableStatus,
            entity,
            legacyId,
            "StatusId",
            statusId.ToString(CultureInfo.InvariantCulture),
            $"StatusId {statusId} is not a valid {expectedEnum}. The legacy shared "
            + "Status table had no per-entity partition, so a value belonging to "
            + "another entity could be stored here (CQ-006).");

    private static void AuditRequiredValues(LegacyDatabase legacy, List<AuditFinding> findings)
    {
        foreach (var patient in legacy.Patients.Where(p => string.IsNullOrWhiteSpace(p.Code)))
        {
            findings.Add(new AuditFinding(
                AuditCodes.MissingRequiredValue,
                nameof(legacy.Patients),
                patient.Id.ToString(),
                nameof(patient.Code),
                patient.Code,
                "Patient.Code is required in the rebuild (DR-001) but is null or "
                + "blank in the legacy row."));
        }

        foreach (var bill in legacy.Prescriptions.Where(p => string.IsNullOrWhiteSpace(p.Code)))
        {
            findings.Add(new AuditFinding(
                AuditCodes.MissingRequiredValue,
                nameof(legacy.Prescriptions),
                bill.Id.ToString(),
                nameof(bill.Code),
                bill.Code,
                "Prescription.Code is required in the rebuild (DR-003) but is null "
                + "or blank in the legacy row."));
        }
    }

    private static void AuditUniqueValues(LegacyDatabase legacy, List<AuditFinding> findings)
    {
        AddDuplicateFindings(
            legacy.Patients.Where(p => !string.IsNullOrWhiteSpace(p.Code)),
            patient => patient.Code!,
            patient => patient.Id.ToString(),
            nameof(legacy.Patients),
            "Code",
            "DR-001 indexes Patient.Code uniquely, so these rows cannot both "
            + "migrate. GM-002 shows legacy could produce this collision through a "
            + "manual Code edit while still returning 200 OK.",
            findings);

        AddDuplicateFindings(
            legacy.Prescriptions.Where(p => !string.IsNullOrWhiteSpace(p.Code)),
            bill => bill.Code!,
            bill => bill.Id.ToString(),
            nameof(legacy.Prescriptions),
            "Code",
            "Prescription.Code is uniquely indexed in the rebuild.",
            findings);

        AddDuplicateFindings(
            legacy.MedicalServices,
            service => service.Name,
            service => service.Id.ToString(),
            nameof(legacy.MedicalServices),
            "Name",
            "DR-017 indexes MedicalService.Name uniquely.",
            findings);

        AddDuplicateFindings(
            legacy.MedicalInfos,
            info => info.Name,
            info => info.Id.ToString(),
            nameof(legacy.MedicalInfos),
            "Name",
            "DR-017 indexes MedicalInfo.Name uniquely.",
            findings);

        AddDuplicateFindings(
            legacy.Products,
            product => product.Name,
            product => product.Id.ToString(),
            nameof(legacy.Products),
            "Name",
            "Product.Name is uniquely indexed in the rebuild.",
            findings);
    }

    private static void AddDuplicateFindings<T>(
        IEnumerable<T> rows,
        Func<T, string> keySelector,
        Func<T, string> idSelector,
        string entity,
        string column,
        string detail,
        List<AuditFinding> findings)
    {
        var collisions = rows
            .GroupBy(keySelector, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (var collision in collisions)
        {
            foreach (var row in collision)
            {
                findings.Add(new AuditFinding(
                    AuditCodes.DuplicateUniqueValue,
                    entity,
                    idSelector(row),
                    column,
                    collision.Key,
                    detail));
            }
        }
    }

    private static void AuditOrphans(LegacyDatabase legacy, List<AuditFinding> findings)
    {
        // PatientMedicalInfo is the one table with no legacy foreign keys, so it is
        // the one that can actually hold orphans (GM-019).
        var patientIds = legacy.Patients.Select(patient => patient.Id).ToHashSet();
        var medicalInfoIds = legacy.MedicalInfos.Select(info => info.Id).ToHashSet();

        foreach (var tag in legacy.PatientMedicalInfos)
        {
            if (!patientIds.Contains(tag.PatientId))
            {
                findings.Add(new AuditFinding(
                    AuditCodes.OrphanedReference,
                    nameof(legacy.PatientMedicalInfos),
                    tag.Id.ToString(),
                    nameof(tag.PatientId),
                    tag.PatientId.ToString(),
                    "References a Patient that does not exist. Migrated as-is: "
                    + "GM-019 pins that legacy deliberately orphans these rows, so "
                    + "deleting them would destroy data outside this item's mandate."));
            }

            if (!medicalInfoIds.Contains(tag.MedicalInfoId))
            {
                findings.Add(new AuditFinding(
                    AuditCodes.OrphanedReference,
                    nameof(legacy.PatientMedicalInfos),
                    tag.Id.ToString(),
                    nameof(tag.MedicalInfoId),
                    tag.MedicalInfoId.ToString(),
                    "References a MedicalInfo that does not exist. Migrated as-is."));
            }
        }
    }
}
