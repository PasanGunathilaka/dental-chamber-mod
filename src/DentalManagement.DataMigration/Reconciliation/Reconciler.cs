using DentalManagement.DataMigration.Auditing;
using DentalManagement.DataMigration.LegacyReaders;
using DentalManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.DataMigration.Reconciliation;

/// <summary>
/// Compares the migrated PostgreSQL database against the legacy source it came
/// from.
/// </summary>
/// <remarks>
/// Row counts alone would not catch a truncated or mis-scaled money column, and
/// money totals alone would not catch a missing row, so both run
/// (spec FR-22, AC-19, AC-21).
/// </remarks>
public sealed class Reconciler(DentalDbContext target)
{
    /// <summary>
    /// Runs every check.
    /// </summary>
    /// <param name="legacy">The source, as read before migration.</param>
    /// <param name="audit">
    /// The audit for the same source. Rows with a blocking finding were never
    /// migrated, so the expected counts must exclude them — otherwise every
    /// reported bad row would also read as a reconciliation failure and the two
    /// signals would be impossible to tell apart.
    /// </param>
    public async Task<ReconciliationReport> ReconcileAsync(
        LegacyDatabase legacy,
        AuditReport audit,
        CancellationToken cancellationToken = default)
    {
        var blockedIds = audit.Blocking
            .Select(finding => finding.LegacyId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool Migrated(Guid id) => !blockedIds.Contains(id.ToString());

        var checks = new List<ReconciliationCheck>
        {
            ReconciliationCheck.Count(
                "Patient rows",
                legacy.Patients.Count(patient => Migrated(patient.Id)),
                await target.Patients.CountAsync(cancellationToken),
                BlockedNote(legacy.Patients.Count(patient => !Migrated(patient.Id)))),

            ReconciliationCheck.Count(
                "Prescription rows",
                legacy.Prescriptions.Count(bill => Migrated(bill.Id)),
                await target.Prescriptions.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "MedicalService rows",
                legacy.MedicalServices.Count(service => Migrated(service.Id)),
                await target.MedicalServices.CountAsync(cancellationToken),
                BlockedNote(legacy.MedicalServices.Count(service => !Migrated(service.Id)))),

            ReconciliationCheck.Count(
                "PatientMedicalService rows",
                legacy.PatientMedicalServices.Count(item => Migrated(item.Id)),
                await target.PatientMedicalServices.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "MedicalInfo rows",
                legacy.MedicalInfos.Count(info => Migrated(info.Id)),
                await target.MedicalInfos.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "PatientMedicalInfo rows",
                legacy.PatientMedicalInfos.Count(tag => Migrated(tag.Id)),
                await target.PatientMedicalInfos.CountAsync(cancellationToken),
                "Includes orphaned rows, migrated as-is per GM-019."),

            ReconciliationCheck.Count(
                "Payment rows",
                legacy.Payments.Count(payment => Migrated(payment.Id)),
                await target.Payments.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "Product rows",
                legacy.Products.Count(product => Migrated(product.Id)),
                await target.Products.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "Inventory rows",
                legacy.Inventories.Count(movement => Migrated(movement.Id)),
                await target.Inventories.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "Doctor rows",
                legacy.Doctors.Count(doctor => Migrated(doctor.Id)),
                await target.Doctors.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "Appointment rows",
                legacy.Appointments.Count(appointment => Migrated(appointment.Id)),
                await target.Appointments.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "Resource rows",
                legacy.Resources.Count,
                await target.Resources.CountAsync(cancellationToken),
                "Seeded rebuild routes are matched by Route, so this counts distinct routes."),

            ReconciliationCheck.Count(
                "Permission rows",
                legacy.Permissions.Count,
                await target.Permissions.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "Role rows",
                legacy.Roles.Count,
                await target.Roles.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "User rows",
                legacy.Users.Count,
                await target.Users.CountAsync(cancellationToken)),
        };

        checks.AddRange(await MoneyChecksAsync(legacy, blockedIds, cancellationToken));

        return new ReconciliationReport(checks);
    }

    /// <summary>
    /// Money totals, the checks that would catch a precision or truncation mistake
    /// that row counts cannot see.
    /// </summary>
    private async Task<List<ReconciliationCheck>> MoneyChecksAsync(
        LegacyDatabase legacy,
        HashSet<string> blockedIds,
        CancellationToken cancellationToken)
    {
        var expectedPaymentTotal = legacy.Payments
            .Where(payment => !blockedIds.Contains(payment.Id.ToString()))
            .Sum(payment => (decimal)payment.Amount);

        var actualPaymentTotal = await target.Payments
            .SumAsync(payment => payment.Amount, cancellationToken);

        var migratedBills = legacy.Prescriptions
            .Where(bill => !blockedIds.Contains(bill.Id.ToString()))
            .ToList();

        var expectedPayableTotal = migratedBills.Sum(bill => (decimal)bill.TotalPayable);
        var actualPayableTotal = await target.Prescriptions
            .SumAsync(bill => bill.TotalPayable, cancellationToken);

        var expectedDueTotal = migratedBills.Sum(bill => (decimal)bill.TotalDue);
        var actualDueTotal = await target.Prescriptions
            .SumAsync(bill => bill.TotalDue, cancellationToken);

        // Charges only for rows that actually migrated: an unparseable Charge is a
        // blocking finding, so it contributes nothing to either side.
        var expectedChargeTotal = legacy.MedicalServices
            .Where(service => !blockedIds.Contains(service.Id.ToString()))
            .Sum(service =>
                LegacyValueAuditor.TryParseCharge(service.Charge, out var charge) ? charge : 0m);

        var actualChargeTotal = await target.MedicalServices
            .SumAsync(service => service.Charge, cancellationToken);

        return
        [
            ReconciliationCheck.Money("Payment amount total", expectedPaymentTotal, actualPaymentTotal),
            ReconciliationCheck.Money("Prescription payable total", expectedPayableTotal, actualPayableTotal),
            ReconciliationCheck.Money("Prescription due total", expectedDueTotal, actualDueTotal),
            ReconciliationCheck.Money(
                "MedicalService charge total",
                expectedChargeTotal,
                actualChargeTotal,
                "Fractional charges are preserved rather than truncated (CQ-008)."),
        ];
    }

    private static string? BlockedNote(int blockedCount) => blockedCount == 0
        ? null
        : $"{blockedCount} legacy row(s) excluded — see the audit report's blocking findings.";
}
