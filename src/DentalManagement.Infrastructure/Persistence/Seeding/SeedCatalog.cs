namespace DentalManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// The fixed catalogs a fresh install starts with.
/// </summary>
public static class SeedCatalog
{
    /// <summary>
    /// The eight legacy role names, preserved verbatim so migrated
    /// <c>Permission</c> rows keep meaning (domain-model.md Enumerations item 3).
    /// </summary>
    public static readonly string[] RoleNames =
    [
        "SystemAdmin",
        "Admin",
        "Manager",
        "User",
        "Inventory",
        "Patient",
        "Doctor",
        "Compounder",
    ];

    public const string SystemAdminRole = "SystemAdmin";

    /// <summary>
    /// The protected-screen catalog DR-015 resolves against, keyed by the legacy
    /// UI-Router state name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Route names are the legacy ones on purpose: <c>Permission</c> rows reference
    /// resources by identity, so changing the vocabulary here would orphan every
    /// migrated grant. The rebuild's own routing is a later item's concern.
    /// </para>
    /// <para>
    /// <c>root.about</c> and <c>root.contact</c> are deliberately absent — CQ-003
    /// excluded both broken legacy screens from the rebuild scope.
    /// </para>
    /// <para>
    /// <b>Inference, flagged rather than asserted:</b> no analysis artifact records
    /// which legacy resources carried <c>IsPublic = true</c>. Login and the
    /// access-denied screen must be reachable before any grant exists — DR-015's
    /// gate runs on every state transition, so a private login screen would be
    /// unreachable on a fresh install — so those two are seeded public and
    /// everything else private. GM-039 is replayed against the resulting
    /// private-resource count rather than a hardcoded number, so this inference
    /// does not silently change what that fixture proves.
    /// </para>
    /// </remarks>
    public static readonly SeedResource[] Resources =
    [
        new("Login", "root.login", IsPublic: true),
        new("Access Denied", "root.access-denied", IsPublic: true),

        new("Profile", "root.profile", IsPublic: false),
        new("Manage Users", "root.user", IsPublic: false),
        new("Manage Roles", "root.role", IsPublic: false),
        new("Manage Resources", "root.resource", IsPublic: false),
        new("Manage Permissions", "root.permission", IsPublic: false),

        new("Patient List", "root.patient", IsPublic: false),
        new("New Patient", "root.patient-create", IsPublic: false),
        new("Patient Detail", "root.patient-detail", IsPublic: false),
        new("Patient Payment Report", "root.patient-report", IsPublic: false),

        new("Manage Dental Services", "root.patient-service", IsPublic: false),
        new("Manage Medical Conditions", "root.patient-info", IsPublic: false),

        new("Product Catalog", "root.product", IsPublic: false),
        new("Stock Movement", "root.stock", IsPublic: false),
        new("Stock Report", "root.stock-report", IsPublic: false),
        new("Dashboard", "root.dashboard", IsPublic: false),

        new("Appointments", "root.patient-appointment", IsPublic: false),
    ];

    /// <summary>
    /// The single doctor a fresh install starts with, mirroring the legacy seed's
    /// <c>DR001</c> / "Dental Doctor".
    /// </summary>
    /// <remarks>
    /// The id is deterministic and is persisted exactly as assigned. Legacy marked
    /// the column database-generated, so EF discarded the seeder's GUID and the
    /// client's hardcoded doctor id matched no row — every appointment booking on a
    /// freshly migrated database failed
    /// <c>FK_dbo.Appointment_dbo.Doctor_DoctorId</c>. The legacy literal GUID is
    /// deliberately not reused here; nothing in the rebuild should depend on a
    /// hardcoded doctor id (spec FR-18, AC-16).
    /// </remarks>
    public static Guid SeededDoctorId => DeterministicGuid.From("Doctor:DR001");

    public const string SeededDoctorCode = "DR001";

    public const string SeededDoctorName = "Dental Doctor";
}

/// <summary>One entry in the protected-screen catalog.</summary>
public sealed record SeedResource(string Name, string Route, bool IsPublic);
