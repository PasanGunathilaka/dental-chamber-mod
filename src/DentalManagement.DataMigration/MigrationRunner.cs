using DentalManagement.DataMigration.Auditing;
using DentalManagement.DataMigration.LegacyReaders;
using DentalManagement.DataMigration.Reconciliation;
using DentalManagement.Domain.Entities;
using DentalManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.DataMigration;

/// <summary>
/// Runs one legacy-to-PostgreSQL migration: read, audit, write in FK-safe order,
/// reconcile.
/// </summary>
/// <remarks>
/// The audit runs before any write, and reconciliation after, so a run either
/// reports what it could not accept up front or proves afterwards that what moved
/// still adds up (spec FR-20, FR-22).
/// </remarks>
public sealed class MigrationRunner(
    ILegacyDataSource source,
    DentalDbContext target,
    MigrationOptions options)
{
    public async Task<MigrationOutcome> RunAsync(CancellationToken cancellationToken = default)
    {
        var legacy = await source.ReadAllAsync(cancellationToken);
        var audit = new LegacyValueAuditor().Audit(legacy);

        if (options.DryRun)
        {
            return new MigrationOutcome(
                Succeeded: true,
                Message: "Dry run — nothing written." + Environment.NewLine + audit.ToSummary(),
                Audit: audit);
        }

        if (!options.AllowNonEmptyTarget && await TargetHoldsDomainDataAsync(cancellationToken))
        {
            return new MigrationOutcome(
                Succeeded: false,
                Message: "Refusing to run: the target database already holds domain data. "
                    + "A bulk one-way migration onto a populated target risks a "
                    + "half-merged database. Re-run against an empty target, or pass "
                    + "--allow-non-empty if merging is genuinely intended.",
                Audit: audit);
        }

        await WriteAsync(legacy, audit, cancellationToken);

        var reconciliation = await new Reconciler(target)
            .ReconcileAsync(legacy, audit, cancellationToken);

        var message = string.Join(
            Environment.NewLine,
            audit.ToSummary(),
            reconciliation.ToSummary());

        return new MigrationOutcome(reconciliation.Passed, message, audit, reconciliation);
    }

    /// <summary>
    /// Whether the target holds real data as opposed to seed data.
    /// </summary>
    /// <remarks>
    /// Seeded roles, resources, and the default doctor are the expected state after a
    /// fresh migrate-and-seed, so they must not count as "non-empty" — otherwise the
    /// normal path would always be refused. Patients, bills, products, and payments
    /// are what indicate a target somebody has already migrated or used.
    /// </remarks>
    private async Task<bool> TargetHoldsDomainDataAsync(CancellationToken cancellationToken) =>
        await target.Patients.AnyAsync(cancellationToken)
        || await target.Prescriptions.AnyAsync(cancellationToken)
        || await target.Products.AnyAsync(cancellationToken)
        || await target.Payments.AnyAsync(cancellationToken)
        || await target.Appointments.AnyAsync(cancellationToken);

    /// <summary>
    /// Writes every entity in dependency order, inside one transaction.
    /// </summary>
    /// <remarks>
    /// One transaction is what makes AC-22's "never half-applied" true: a failure
    /// part-way leaves the target exactly as it was rather than in a state nobody
    /// can characterise.
    /// </remarks>
    private async Task WriteAsync(
        LegacyDatabase legacy,
        AuditReport audit,
        CancellationToken cancellationToken)
    {
        var plan = MigrationPlan.Build(legacy, audit);

        await using var transaction = await target.Database.BeginTransactionAsync(cancellationToken);

        await WriteIdentityAsync(legacy, cancellationToken);

        // Parents before children, matching the foreign keys the new schema declares.
        target.Patients.AddRange(legacy.Patients
            .Where(patient => plan.PatientIds.Contains(patient.Id))
            .Select(LegacyToRebuildMapper.ToPatient));

        target.MedicalServices.AddRange(legacy.MedicalServices
            .Where(service => plan.MedicalServiceIds.Contains(service.Id))
            .Select(LegacyToRebuildMapper.ToMedicalService));

        target.MedicalInfos.AddRange(legacy.MedicalInfos
            .Where(info => plan.MedicalInfoIds.Contains(info.Id))
            .Select(LegacyToRebuildMapper.ToMedicalInfo));

        target.Doctors.AddRange(legacy.Doctors
            .Where(doctor => plan.DoctorIds.Contains(doctor.Id))
            .Select(LegacyToRebuildMapper.ToDoctor));

        target.Products.AddRange(legacy.Products
            .Where(product => plan.ProductIds.Contains(product.Id))
            .Select(LegacyToRebuildMapper.ToProduct));

        await target.SaveChangesAsync(cancellationToken);

        target.Prescriptions.AddRange(legacy.Prescriptions
            .Where(bill => plan.PrescriptionIds.Contains(bill.Id))
            .Select(LegacyToRebuildMapper.ToPrescription));

        target.Inventories.AddRange(legacy.Inventories
            .Where(movement => plan.InventoryIds.Contains(movement.Id))
            .Select(LegacyToRebuildMapper.ToInventory));

        target.Appointments.AddRange(legacy.Appointments
            .Where(appointment => plan.AppointmentIds.Contains(appointment.Id))
            .Select(LegacyToRebuildMapper.ToAppointment));

        // No foreign keys on this table, so orphans migrate as-is — GM-019.
        target.PatientMedicalInfos.AddRange(legacy.PatientMedicalInfos
            .Where(tag => plan.PatientMedicalInfoIds.Contains(tag.Id))
            .Select(LegacyToRebuildMapper.ToPatientMedicalInfo));

        await target.SaveChangesAsync(cancellationToken);

        target.PatientMedicalServices.AddRange(legacy.PatientMedicalServices
            .Where(item => plan.LineItemIds.Contains(item.Id))
            .Select(LegacyToRebuildMapper.ToLineItem));

        target.Payments.AddRange(legacy.Payments
            .Where(payment => plan.PaymentIds.Contains(payment.Id))
            .Select(LegacyToRebuildMapper.ToPayment));

        await target.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Migrates roles, users, role assignments, resources, and permission grants.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Password hashes are carried across verbatim. They are ASP.NET Identity v2
    /// hashes, which ASP.NET Core Identity still verifies, so existing staff
    /// passwords keep working — re-hashing is impossible without the plaintext, and
    /// resetting every account would be a decision nobody made.
    /// </para>
    /// <para>
    /// Resources are matched by <c>Route</c>, not id: the seeder has already created
    /// the rebuild's catalog, so a legacy row with the same route must map onto the
    /// seeded row rather than duplicate it. Permission grants are then re-pointed at
    /// the surviving resource id.
    /// </para>
    /// </remarks>
    private async Task WriteIdentityAsync(
        LegacyDatabase legacy,
        CancellationToken cancellationToken)
    {
        // A role the seeder already created keeps its seeded id; grants are re-pointed
        // below so nothing ends up referencing a role that does not exist.
        var roleIdByLegacyId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var legacyRole in legacy.Roles)
        {
            var seeded = await target.Roles
                .SingleOrDefaultAsync(role => role.Name == legacyRole.Name, cancellationToken);

            if (seeded is not null)
            {
                roleIdByLegacyId[legacyRole.Id] = seeded.Id;
                continue;
            }

            target.Roles.Add(new IdentityRole
            {
                Id = legacyRole.Id,
                Name = legacyRole.Name,
                NormalizedName = legacyRole.Name.ToUpperInvariant(),
            });
            roleIdByLegacyId[legacyRole.Id] = legacyRole.Id;
        }

        foreach (var legacyUser in legacy.Users)
        {
            target.Users.Add(new Infrastructure.Identity.ApplicationUser
            {
                Id = legacyUser.Id,
                UserName = legacyUser.UserName,
                NormalizedUserName = legacyUser.UserName.ToUpperInvariant(),
                Email = legacyUser.Email,
                NormalizedEmail = legacyUser.Email?.ToUpperInvariant(),
                EmailConfirmed = legacyUser.EmailConfirmed,
                PasswordHash = legacyUser.PasswordHash,
                SecurityStamp = legacyUser.SecurityStamp,
                PhoneNumber = legacyUser.PhoneNumber,
                FirstName = legacyUser.FirstName,
                LastName = legacyUser.LastName,
            });
        }

        await target.SaveChangesAsync(cancellationToken);

        // CQ-015: one primary role per user. The unique index enforces it, so a
        // legacy user holding several would fail loudly here rather than migrate with
        // wider access than the rebuild's model allows.
        foreach (var assignment in legacy.UserRoles)
        {
            if (roleIdByLegacyId.TryGetValue(assignment.RoleId, out var roleId))
            {
                target.UserRoles.Add(new IdentityUserRole<string>
                {
                    UserId = assignment.UserId,
                    RoleId = roleId,
                });
            }
        }

        var resourceIdByLegacyId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var legacyResource in legacy.Resources)
        {
            var seeded = await target.Resources
                .SingleOrDefaultAsync(
                    resource => resource.Route == legacyResource.Route,
                    cancellationToken);

            if (seeded is not null)
            {
                resourceIdByLegacyId[legacyResource.Id] = seeded.Id;
                continue;
            }

            target.Resources.Add(LegacyToRebuildMapper.ToResource(legacyResource));
            resourceIdByLegacyId[legacyResource.Id] = legacyResource.Id;
        }

        await target.SaveChangesAsync(cancellationToken);

        var existingGrants = await target.Permissions
            .Select(permission => new { permission.RoleId, permission.ResourceId })
            .ToListAsync(cancellationToken);

        var grantKeys = existingGrants
            .Select(grant => $"{grant.RoleId}|{grant.ResourceId}")
            .ToHashSet(StringComparer.Ordinal);

        foreach (var legacyPermission in legacy.Permissions)
        {
            if (!roleIdByLegacyId.TryGetValue(legacyPermission.RoleId, out var roleId)
                || !resourceIdByLegacyId.TryGetValue(legacyPermission.ResourceId, out var resourceId))
            {
                continue;
            }

            // The seeder may already have granted SystemAdmin this resource; the
            // unique (RoleId, ResourceId) index means a second row would fail.
            if (!grantKeys.Add($"{roleId}|{resourceId}"))
            {
                continue;
            }

            target.Permissions.Add(
                LegacyToRebuildMapper.ToPermission(legacyPermission, roleId, resourceId));
        }

        await target.SaveChangesAsync(cancellationToken);
    }
}
