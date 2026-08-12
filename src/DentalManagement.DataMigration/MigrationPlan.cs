using DentalManagement.DataMigration.Auditing;
using DentalManagement.DataMigration.LegacyReaders;

namespace DentalManagement.DataMigration;

/// <summary>
/// Exactly which legacy rows a migration will carry across.
/// </summary>
/// <remarks>
/// <para>
/// Two rules combine. A row is excluded when it has a blocking audit finding of its
/// own — an unparseable charge, a missing required value, a unique-value collision.
/// A row is <i>also</i> excluded when a parent it depends on was excluded: the new
/// schema's foreign keys mean a bill whose patient did not migrate cannot migrate
/// either, and neither can that bill's line items or payments.
/// </para>
/// <para>
/// <b>Why the writer and the reconciler share this.</b> Both need the same answer,
/// and the transitive rule is fiddly enough that two independent implementations
/// would eventually disagree — at which point every reported bad row would also read
/// as a reconciliation failure and the two signals would be impossible to separate.
/// The cost is that reconciliation no longer independently re-derives the plan, so it
/// cannot catch a mistake in the plan itself; that is covered directly instead, by
/// AC-20's assertions that specific blocked rows are absent from the target and
/// specific kept rows are present.
/// </para>
/// </remarks>
public sealed class MigrationPlan
{
    private MigrationPlan(LegacyDatabase legacy, AuditReport audit)
    {
        var blocked = audit.Blocking
            .Select(finding => finding.LegacyId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool Allowed(Guid id) => !blocked.Contains(id.ToString());

        PatientIds = legacy.Patients.Where(p => Allowed(p.Id)).Select(p => p.Id).ToHashSet();
        MedicalServiceIds = legacy.MedicalServices.Where(s => Allowed(s.Id)).Select(s => s.Id).ToHashSet();
        MedicalInfoIds = legacy.MedicalInfos.Where(i => Allowed(i.Id)).Select(i => i.Id).ToHashSet();
        DoctorIds = legacy.Doctors.Where(d => Allowed(d.Id)).Select(d => d.Id).ToHashSet();
        ProductIds = legacy.Products.Where(p => Allowed(p.Id)).Select(p => p.Id).ToHashSet();

        // No foreign keys on this table, so an orphan is carried across as-is (GM-019).
        PatientMedicalInfoIds = legacy.PatientMedicalInfos
            .Where(t => Allowed(t.Id))
            .Select(t => t.Id)
            .ToHashSet();

        PrescriptionIds = legacy.Prescriptions
            .Where(bill => Allowed(bill.Id) && PatientIds.Contains(bill.PatientId))
            .Select(bill => bill.Id)
            .ToHashSet();

        InventoryIds = legacy.Inventories
            .Where(movement => Allowed(movement.Id) && ProductIds.Contains(movement.ProductId))
            .Select(movement => movement.Id)
            .ToHashSet();

        AppointmentIds = legacy.Appointments
            .Where(appointment => Allowed(appointment.Id) && DoctorIds.Contains(appointment.DoctorId))
            .Select(appointment => appointment.Id)
            .ToHashSet();

        LineItemIds = legacy.PatientMedicalServices
            .Where(item => Allowed(item.Id)
                && PrescriptionIds.Contains(item.PrescriptionId)
                && PatientIds.Contains(item.PatientId)
                && MedicalServiceIds.Contains(item.MedicalServiceId))
            .Select(item => item.Id)
            .ToHashSet();

        PaymentIds = legacy.Payments
            .Where(payment => Allowed(payment.Id) && PrescriptionIds.Contains(payment.PrescriptionId))
            .Select(payment => payment.Id)
            .ToHashSet();
    }

    public HashSet<Guid> PatientIds { get; }

    public HashSet<Guid> PrescriptionIds { get; }

    public HashSet<Guid> MedicalServiceIds { get; }

    public HashSet<Guid> LineItemIds { get; }

    public HashSet<Guid> MedicalInfoIds { get; }

    public HashSet<Guid> PatientMedicalInfoIds { get; }

    public HashSet<Guid> PaymentIds { get; }

    public HashSet<Guid> ProductIds { get; }

    public HashSet<Guid> InventoryIds { get; }

    public HashSet<Guid> DoctorIds { get; }

    public HashSet<Guid> AppointmentIds { get; }

    public static MigrationPlan Build(LegacyDatabase legacy, AuditReport audit) =>
        new(legacy, audit);
}
