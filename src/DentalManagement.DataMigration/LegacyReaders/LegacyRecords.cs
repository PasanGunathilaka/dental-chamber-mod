namespace DentalManagement.DataMigration.LegacyReaders;

/// <summary>
/// Legacy rows exactly as SQL Server holds them, before any interpretation.
/// </summary>
/// <remarks>
/// <para>
/// These are deliberately not the domain entities. The whole point of the audit
/// (spec FR-21) is to see the legacy value <i>as stored</i> and decide whether it
/// can become a typed one — so <c>Charge</c> arrives as <see cref="string"/>,
/// <c>Gender</c> as <see cref="string"/>, and every status as a bare
/// <see cref="int"/>. Parsing into the new types happens in the auditor, where a
/// failure becomes a reported finding rather than an exception in a reader.
/// </para>
/// <para>
/// Nullable reference and value types are used wherever legacy allowed a null,
/// including columns the rebuild makes required — a null in one of those is itself
/// something to report.
/// </para>
/// </remarks>
public sealed record LegacyPatient(
    Guid Id,
    string? Code,
    string Name,
    int Age,
    string? Phone,
    string? Email,
    string? Address,
    string? Gender,
    string? Note,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyPrescription(
    Guid Id,
    string? Code,
    Guid PatientId,
    double TotalCharge,
    double DiscountPercent,
    double DiscountAmount,
    double FixedDiscount,
    double TotalPayable,
    double TotalPaid,
    double TotalDue,
    int StatusId,
    DateTime Created,
    DateTime LastUpdate);

/// <summary><c>Charge</c> is a string in legacy — see DR-019 and CQ-008.</summary>
public sealed record LegacyMedicalService(
    Guid Id,
    int Code,
    string Name,
    string? Charge,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyPatientMedicalService(
    Guid Id,
    Guid PatientId,
    Guid PrescriptionId,
    Guid MedicalServiceId,
    int Quantity,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyMedicalInfo(
    Guid Id,
    string Name,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyPatientMedicalInfo(
    Guid Id,
    Guid PatientId,
    Guid MedicalInfoId,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyPayment(
    Guid Id,
    Guid PrescriptionId,
    double Amount,
    string? Comment,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyProduct(
    Guid Id,
    string? Code,
    string Name,
    int StartingInventory,
    int Received,
    int Shipped,
    int OnHand,
    int MinimumRequired,
    double UnitPrice,
    double SalePrice,
    int StatusId,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyInventory(
    Guid Id,
    Guid ProductId,
    string CashMemoNo,
    int OnHand,
    int ReceivedOrShippedQuantity,
    int StatusId,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyDoctor(
    Guid Id,
    string? Code,
    string? Name,
    string? Phone,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyAppointment(
    Guid Id,
    string? Code,
    string PatientNameOrId,
    int Age,
    string? Phone,
    DateTime Date,
    DateTime Time,
    Guid DoctorId,
    int StatusId,
    DateTime Created,
    DateTime LastUpdate);

public sealed record LegacyRole(string Id, string Name);

public sealed record LegacyUser(
    string Id,
    string UserName,
    string? Email,
    bool EmailConfirmed,
    string? PasswordHash,
    string? SecurityStamp,
    string? PhoneNumber,
    string? FirstName,
    string? LastName);

public sealed record LegacyUserRole(string UserId, string RoleId);

public sealed record LegacyResource(string Id, string? Name, string Route, bool IsPublic);

public sealed record LegacyPermission(string Id, string RoleId, string? RoleName, string ResourceId);

/// <summary>Everything the migration reads from the legacy database, in one shape.</summary>
public sealed class LegacyDatabase
{
    public IReadOnlyList<LegacyPatient> Patients { get; init; } = [];

    public IReadOnlyList<LegacyPrescription> Prescriptions { get; init; } = [];

    public IReadOnlyList<LegacyMedicalService> MedicalServices { get; init; } = [];

    public IReadOnlyList<LegacyPatientMedicalService> PatientMedicalServices { get; init; } = [];

    public IReadOnlyList<LegacyMedicalInfo> MedicalInfos { get; init; } = [];

    public IReadOnlyList<LegacyPatientMedicalInfo> PatientMedicalInfos { get; init; } = [];

    public IReadOnlyList<LegacyPayment> Payments { get; init; } = [];

    public IReadOnlyList<LegacyProduct> Products { get; init; } = [];

    public IReadOnlyList<LegacyInventory> Inventories { get; init; } = [];

    public IReadOnlyList<LegacyDoctor> Doctors { get; init; } = [];

    public IReadOnlyList<LegacyAppointment> Appointments { get; init; } = [];

    public IReadOnlyList<LegacyRole> Roles { get; init; } = [];

    public IReadOnlyList<LegacyUser> Users { get; init; } = [];

    public IReadOnlyList<LegacyUserRole> UserRoles { get; init; } = [];

    public IReadOnlyList<LegacyResource> Resources { get; init; } = [];

    public IReadOnlyList<LegacyPermission> Permissions { get; init; } = [];
}
