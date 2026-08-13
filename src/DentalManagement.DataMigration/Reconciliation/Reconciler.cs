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
/// <para>
/// Row counts alone would not catch a truncated or mis-scaled money column, and
/// money totals alone would not catch a missing row, so both run
/// (spec FR-22, AC-19, AC-21).
/// </para>
/// <para>
/// Two shapes of check, because the target holds more than the migration put there.
/// Pure domain tables are compared by count: the target should contain exactly the
/// rows the plan carried across. Tables the seeder also writes — Doctor, Resource,
/// Permission, Role — are compared by <i>presence</i> instead: the fresh-install
/// seed legitimately adds rows that were never in the legacy database, so an equal
/// count would be the wrong expectation while "every legacy row arrived" is the right
/// one.
/// </para>
/// </remarks>
public sealed class Reconciler(DentalDbContext target)
{
    public async Task<ReconciliationReport> ReconcileAsync(
        LegacyDatabase legacy,
        AuditReport audit,
        CancellationToken cancellationToken = default)
    {
        var plan = MigrationPlan.Build(legacy, audit);

        var checks = new List<ReconciliationCheck>
        {
            ReconciliationCheck.Count(
                "Patient rows",
                plan.PatientIds.Count,
                await target.Patients.CountAsync(cancellationToken),
                ExcludedNote(legacy.Patients.Count, plan.PatientIds.Count)),

            ReconciliationCheck.Count(
                "Prescription rows",
                plan.PrescriptionIds.Count,
                await target.Prescriptions.CountAsync(cancellationToken),
                ExcludedNote(legacy.Prescriptions.Count, plan.PrescriptionIds.Count)),

            ReconciliationCheck.Count(
                "MedicalService rows",
                plan.MedicalServiceIds.Count,
                await target.MedicalServices.CountAsync(cancellationToken),
                ExcludedNote(legacy.MedicalServices.Count, plan.MedicalServiceIds.Count)),

            ReconciliationCheck.Count(
                "PatientMedicalService rows",
                plan.LineItemIds.Count,
                await target.PatientMedicalServices.CountAsync(cancellationToken),
                ExcludedNote(legacy.PatientMedicalServices.Count, plan.LineItemIds.Count)),

            ReconciliationCheck.Count(
                "MedicalInfo rows",
                plan.MedicalInfoIds.Count,
                await target.MedicalInfos.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "PatientMedicalInfo rows",
                plan.PatientMedicalInfoIds.Count,
                await target.PatientMedicalInfos.CountAsync(cancellationToken),
                "Includes orphaned rows, migrated as-is per GM-019."),

            ReconciliationCheck.Count(
                "Payment rows",
                plan.PaymentIds.Count,
                await target.Payments.CountAsync(cancellationToken),
                ExcludedNote(legacy.Payments.Count, plan.PaymentIds.Count)),

            ReconciliationCheck.Count(
                "Product rows",
                plan.ProductIds.Count,
                await target.Products.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "Inventory rows",
                plan.InventoryIds.Count,
                await target.Inventories.CountAsync(cancellationToken)),

            ReconciliationCheck.Count(
                "Appointment rows",
                plan.AppointmentIds.Count,
                await target.Appointments.CountAsync(cancellationToken),
                ExcludedNote(legacy.Appointments.Count, plan.AppointmentIds.Count)),
        };

        checks.AddRange(await PresenceChecksAsync(legacy, plan, cancellationToken));
        checks.AddRange(await MoneyChecksAsync(legacy, plan, cancellationToken));

        return new ReconciliationReport(checks);
    }

    /// <summary>
    /// Checks that every legacy row arrived, for the tables the seeder also writes.
    /// </summary>
    private async Task<List<ReconciliationCheck>> PresenceChecksAsync(
        LegacyDatabase legacy,
        MigrationPlan plan,
        CancellationToken cancellationToken)
    {
        var targetDoctorIds = await target.Doctors
            .Select(doctor => doctor.Id)
            .ToListAsync(cancellationToken);

        var arrivedDoctors = plan.DoctorIds.Count(id => targetDoctorIds.Contains(id));

        var targetRoutes = await target.Resources
            .Select(resource => resource.Route)
            .ToListAsync(cancellationToken);

        var arrivedResources = legacy.Resources
            .Count(resource => targetRoutes.Contains(resource.Route));

        var targetRoleNames = await target.Roles
            .Select(role => role.Name!)
            .ToListAsync(cancellationToken);

        var arrivedRoles = legacy.Roles.Count(role => targetRoleNames.Contains(role.Name));

        var targetUserNames = await target.Users
            .Select(user => user.UserName!)
            .ToListAsync(cancellationToken);

        var arrivedUsers = legacy.Users.Count(user => targetUserNames.Contains(user.UserName));

        // A grant survives if the target holds one for the same role name and route,
        // since both role ids and resource ids may be the seeder's rather than legacy's.
        var targetGrants = await target.Permissions
            .Join(
                target.Resources,
                permission => permission.ResourceId,
                resource => resource.Id,
                (permission, resource) => new { permission.RoleName, resource.Route })
            .ToListAsync(cancellationToken);

        var legacyResourceRouteById = legacy.Resources
            .ToDictionary(resource => resource.Id, resource => resource.Route, StringComparer.Ordinal);

        var arrivedGrants = legacy.Permissions.Count(grant =>
            legacyResourceRouteById.TryGetValue(grant.ResourceId, out var route)
            && targetGrants.Any(target =>
                string.Equals(target.RoleName, grant.RoleName, StringComparison.Ordinal)
                && string.Equals(target.Route, route, StringComparison.Ordinal)));

        const string seedNote =
            "Presence check: the fresh-install seed adds rows the legacy database never "
            + "had, so an equal count would be the wrong expectation.";

        return
        [
            ReconciliationCheck.Count("Doctor rows present", plan.DoctorIds.Count, arrivedDoctors, seedNote),
            ReconciliationCheck.Count("Resource rows present", legacy.Resources.Count, arrivedResources, seedNote),
            ReconciliationCheck.Count("Permission grants present", legacy.Permissions.Count, arrivedGrants, seedNote),
            ReconciliationCheck.Count("Role rows present", legacy.Roles.Count, arrivedRoles, seedNote),
            ReconciliationCheck.Count("User rows present", legacy.Users.Count, arrivedUsers, seedNote),
        ];
    }

    /// <summary>
    /// Money totals — the checks that would catch a precision or truncation mistake
    /// row counts cannot see.
    /// </summary>
    private async Task<List<ReconciliationCheck>> MoneyChecksAsync(
        LegacyDatabase legacy,
        MigrationPlan plan,
        CancellationToken cancellationToken)
    {
        var expectedPaymentTotal = legacy.Payments
            .Where(payment => plan.PaymentIds.Contains(payment.Id))
            .Sum(payment => (decimal)payment.Amount);

        var actualPaymentTotal = await target.Payments
            .SumAsync(payment => payment.Amount, cancellationToken);

        var migratedBills = legacy.Prescriptions
            .Where(bill => plan.PrescriptionIds.Contains(bill.Id))
            .ToList();

        var expectedPayableTotal = migratedBills.Sum(bill => (decimal)bill.TotalPayable);
        var actualPayableTotal = await target.Prescriptions
            .SumAsync(bill => bill.TotalPayable, cancellationToken);

        var expectedDueTotal = migratedBills.Sum(bill => (decimal)bill.TotalDue);
        var actualDueTotal = await target.Prescriptions
            .SumAsync(bill => bill.TotalDue, cancellationToken);

        var expectedChargeTotal = legacy.MedicalServices
            .Where(service => plan.MedicalServiceIds.Contains(service.Id))
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

    private static string? ExcludedNote(int legacyCount, int plannedCount) =>
        legacyCount == plannedCount
            ? null
            : $"{legacyCount - plannedCount} legacy row(s) excluded — either a blocking "
              + "audit finding, or a parent row that was itself excluded.";
}
