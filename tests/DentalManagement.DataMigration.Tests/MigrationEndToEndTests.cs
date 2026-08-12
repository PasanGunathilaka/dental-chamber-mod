using DentalManagement.DataMigration.Auditing;
using DentalManagement.DataMigration.LegacyReaders;
using DentalManagement.DataMigration.Tests.Harness;
using DentalManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.DataMigration.Tests;

/// <summary>
/// The one-time migration, end to end: real legacy SQL Server in, real PostgreSQL
/// out.
/// </summary>
[Collection(MigrationCollection.Name)]
public class MigrationEndToEndTests(MigrationHarness harness)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// AC-19 — clean legacy data migrates in full and reconciliation agrees on every
    /// count and money total.
    /// </summary>
    [Fact]
    public async Task AC19_clean_legacy_data_migrates_and_reconciles()
    {
        var outcome = await RunAsync(includeProblemData: false);

        Assert.True(
            outcome.Result.Succeeded,
            $"migration should have succeeded: {outcome.Result.Message}");

        Assert.NotNull(outcome.Result.Reconciliation);
        Assert.True(
            outcome.Result.Reconciliation.Passed,
            outcome.Result.Reconciliation.ToSummary());

        // Nothing in the clean script should have produced a finding.
        Assert.NotNull(outcome.Result.Audit);
        Assert.False(outcome.Result.Audit.HasFindings, outcome.Result.Audit.ToSummary());

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        Assert.Equal(2, await target.Patients.CountAsync(Ct));
        Assert.Equal(3, await target.Prescriptions.CountAsync(Ct));
        Assert.Equal(3, await target.MedicalServices.CountAsync(Ct));
        Assert.Equal(3, await target.Payments.CountAsync(Ct));
        Assert.Equal(2, await target.Products.CountAsync(Ct));
        Assert.Equal(3, await target.Inventories.CountAsync(Ct));
        Assert.Equal(1, await target.Doctors.CountAsync(Ct));
        Assert.Equal(2, await target.Appointments.CountAsync(Ct));
    }

    /// <summary>
    /// AC-19/CQ-008 — a fractional legacy charge survives as a decimal instead of
    /// being truncated or rejected.
    /// </summary>
    /// <remarks>
    /// The legacy row holds the string <c>"1250.50"</c>, which legacy itself could
    /// not compute a <c>TotalCharge</c> from at all (GM-017 captured the
    /// <c>FormatException</c>). This is the CQ-008 fix visible in migrated data.
    /// </remarks>
    [Fact]
    public async Task AC19_fractional_legacy_charge_migrates_as_a_decimal()
    {
        var outcome = await RunAsync(includeProblemData: false);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        var consultation = await target.MedicalServices
            .SingleAsync(service => service.Name == "Consultation", Ct);

        Assert.Equal(1250.50m, consultation.Charge);
        Assert.Equal(3751.50m, consultation.Charge * 3);
    }

    /// <summary>AC-19 — money totals survive the double-to-decimal conversion exactly.</summary>
    [Fact]
    public async Task AC19_money_totals_survive_the_conversion_exactly()
    {
        var outcome = await RunAsync(includeProblemData: false);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        Assert.Equal(5800.50m, await target.Payments.SumAsync(payment => payment.Amount, Ct));
        Assert.Equal(1400.00m, await target.Prescriptions.SumAsync(bill => bill.TotalDue, Ct));
    }

    /// <summary>AC-19 — typed statuses arrive with their legacy meaning intact.</summary>
    [Fact]
    public async Task AC19_legacy_status_ids_map_onto_the_typed_enums()
    {
        var outcome = await RunAsync(includeProblemData: false);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        Assert.Equal(2, await target.Prescriptions.CountAsync(b => b.Status == BillStatus.Closed, Ct));
        Assert.Equal(1, await target.Prescriptions.CountAsync(b => b.Status == BillStatus.Active, Ct));
        Assert.Equal(1, await target.Appointments.CountAsync(a => a.Status == AppointmentStatus.Appointed, Ct));
        Assert.Equal(1, await target.Appointments.CountAsync(a => a.Status == AppointmentStatus.Visited, Ct));
        Assert.Equal(1, await target.Products.CountAsync(p => p.Status == ProductStatus.OutOfStock, Ct));
        Assert.Equal(2, await target.Inventories.CountAsync(i => i.Status == InventoryMovementStatus.Shipped, Ct));
    }

    /// <summary>AC-19/CQ-007 — known gender strings become the typed enum.</summary>
    [Fact]
    public async Task AC19_known_gender_strings_become_the_typed_enum()
    {
        var outcome = await RunAsync(includeProblemData: false);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        Assert.Equal(Gender.Female, (await target.Patients.SingleAsync(p => p.Code == "P000001", Ct)).Gender);
        Assert.Equal(Gender.Male, (await target.Patients.SingleAsync(p => p.Code == "P000002", Ct)).Gender);
    }

    /// <summary>A8 — legacy timestamps arrive unshifted.</summary>
    [Fact]
    public async Task AC19_legacy_timestamps_are_not_shifted()
    {
        var outcome = await RunAsync(includeProblemData: false);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);
        var patient = await target.Patients.SingleAsync(p => p.Code == "P000001", Ct);

        Assert.Equal(new DateTime(2026, 1, 5, 9, 15, 0, DateTimeKind.Unspecified), patient.Created);
    }

    /// <summary>
    /// AC-19 — a runtime permission grant to a non-SystemAdmin role survives, and
    /// migrated users keep their password hashes so existing logins keep working.
    /// </summary>
    [Fact]
    public async Task AC19_identity_rows_and_runtime_grants_survive()
    {
        var outcome = await RunAsync(includeProblemData: false);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        Assert.NotNull(await target.Users.SingleOrDefaultAsync(u => u.UserName == "superadmin", Ct));
        Assert.NotNull(await target.Users.SingleOrDefaultAsync(u => u.UserName == "reception", Ct));

        var reception = await target.Users.SingleAsync(u => u.UserName == "reception", Ct);
        Assert.Equal("AQAAAAEAACcQAAAAE-legacy-hash-reception", reception.PasswordHash);

        // The "User" role's grant on root.patient was created through the Permission
        // screen, not by DR-016's seed, so it only survives if runtime grants migrate.
        var userRole = await target.Roles.SingleAsync(role => role.Name == "User", Ct);
        var patientResource = await target.Resources.SingleAsync(r => r.Route == "root.patient", Ct);

        Assert.NotNull(await target.Permissions.SingleOrDefaultAsync(
            permission => permission.RoleId == userRole.Id
                && permission.ResourceId == patientResource.Id,
            Ct));
    }

    /// <summary>
    /// AC-19 — legacy resources match the seeded catalog by route rather than
    /// duplicating it.
    /// </summary>
    [Fact]
    public async Task AC19_legacy_resources_match_the_seeded_catalog_by_route()
    {
        var outcome = await RunAsync(includeProblemData: false);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        var routes = await target.Resources.Select(resource => resource.Route).ToListAsync(Ct);

        Assert.Equal(routes.Count, routes.Distinct().Count());
        Assert.Contains("root.patient", routes);
    }

    /// <summary>
    /// AC-20 — every planted problem value is named in the audit report, and the
    /// fractional charge is not among them.
    /// </summary>
    [Fact]
    public async Task AC20_every_planted_problem_value_is_reported()
    {
        var outcome = await RunAsync(includeProblemData: true);

        var audit = outcome.Result.Audit;
        Assert.NotNull(audit);

        // 1. Unparseable charges: 'abc', '', NULL, 'Rs. 1,500'.
        var chargeFindings = audit.WithCode(AuditCodes.NonIntegerCharge);
        Assert.Equal(4, chargeFindings.Count);
        Assert.Contains(chargeFindings, finding => finding.LegacyValue == "abc");
        Assert.Contains(chargeFindings, finding => finding.LegacyValue == "Rs. 1,500");

        // '1250.50' is valid after CQ-008 and must NOT be a finding.
        Assert.DoesNotContain(chargeFindings, finding => finding.LegacyValue == "1250.50");

        // 2. Out-of-set genders: 'Unknown', 'male', ''.
        var genderFindings = audit.WithCode(AuditCodes.UnknownGender);
        Assert.Equal(3, genderFindings.Count);
        Assert.Contains(genderFindings, finding => finding.LegacyValue == "Unknown");
        Assert.Contains(genderFindings, finding => finding.LegacyValue == "male");

        // 3. Statuses belonging to another entity's set.
        var statusFindings = audit.WithCode(AuditCodes.UnmappableStatus);
        Assert.Equal(2, statusFindings.Count);
        Assert.Contains(statusFindings, finding => finding.Entity == "Appointments");
        Assert.Contains(statusFindings, finding => finding.Entity == "Prescriptions");

        // 4. The duplicate Patient.Code pair — both rows reported.
        var duplicates = audit.WithCode(AuditCodes.DuplicateUniqueValue);
        Assert.Equal(2, duplicates.Count);
        Assert.All(duplicates, finding => Assert.Equal("P000001", finding.LegacyValue));

        // 5. Orphaned condition tags.
        Assert.Equal(2, audit.WithCode(AuditCodes.OrphanedReference).Count);

        // 6. The null Patient.Code.
        var missing = audit.WithCode(AuditCodes.MissingRequiredValue);
        Assert.Single(missing);
        Assert.Equal("Code", missing[0].Column);
    }

    /// <summary>
    /// AC-20 — an unknown gender is stored as null and the row still migrates; the
    /// finding is the record that a human must look at it.
    /// </summary>
    [Fact]
    public async Task AC20_row_with_unknown_gender_still_migrates_with_a_null_gender()
    {
        var outcome = await RunAsync(includeProblemData: true);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        var patient = await target.Patients.SingleOrDefaultAsync(p => p.Code == "P000003", Ct);

        Assert.NotNull(patient);
        Assert.Null(patient.Gender);
    }

    /// <summary>
    /// AC-20 — orphaned condition tags migrate as-is rather than being cleaned up.
    /// GM-019 is why: legacy produces them deliberately.
    /// </summary>
    [Fact]
    public async Task AC20_orphaned_condition_tags_migrate_as_is()
    {
        var outcome = await RunAsync(includeProblemData: true);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        var orphans = await target.PatientMedicalInfos
            .Where(tag => tag.PatientId == new Guid("99999999-9999-9999-9999-999999999999"))
            .ToListAsync(Ct);

        Assert.Equal(2, orphans.Count);
    }

    /// <summary>
    /// AC-20 — a row with a blocking finding does not migrate, and the reconciliation
    /// still passes because its expected count excludes exactly those rows.
    /// </summary>
    [Fact]
    public async Task AC20_blocked_rows_are_excluded_without_failing_reconciliation()
    {
        var outcome = await RunAsync(includeProblemData: true);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        // Unparseable charges never reach the target: nothing is silently zeroed.
        Assert.Null(await target.MedicalServices
            .SingleOrDefaultAsync(service => service.Name == "Unparseable Charge", Ct));
        Assert.Null(await target.MedicalServices
            .SingleOrDefaultAsync(service => service.Name == "Currency Symbol", Ct));

        Assert.NotNull(outcome.Result.Reconciliation);
        Assert.True(
            outcome.Result.Reconciliation.Passed,
            outcome.Result.Reconciliation.ToSummary());
    }

    /// <summary>
    /// AC-21 — a deliberately planted discrepancy makes reconciliation fail. A check
    /// that cannot fail is not a check.
    /// </summary>
    [Fact]
    public async Task AC21_a_planted_discrepancy_makes_reconciliation_fail()
    {
        var legacyConnectionString = await harness.CreateLegacyDatabaseAsync(false, Ct);
        var targetConnectionString = await harness.CreateSeededTargetAsync(Ct);

        var source = new SqlServerLegacyDataSource(legacyConnectionString);
        var legacy = await source.ReadAllAsync(Ct);
        var audit = new LegacyValueAuditor().Audit(legacy);

        await using var target = MigrationHarness.CreateTargetContext(targetConnectionString);

        await new MigrationRunner(
            source,
            target,
            new MigrationOptions
            {
                SourceConnectionString = legacyConnectionString,
                TargetConnectionString = targetConnectionString,
            }).RunAsync(Ct);

        // Delete a migrated payment behind reconciliation's back, then re-check.
        var payment = await target.Payments.FirstAsync(Ct);
        target.Payments.Remove(payment);
        await target.SaveChangesAsync(Ct);

        var recheck = await new Reconciliation.Reconciler(target).ReconcileAsync(legacy, audit, Ct);

        Assert.False(recheck.Passed);
        Assert.Contains(recheck.Failures, failure => failure.Name == "Payment rows");
        Assert.Contains(recheck.Failures, failure => failure.Name == "Payment amount total");
    }

    /// <summary>
    /// AC-22 — a second run against a populated target is refused, and refusing
    /// changes nothing.
    /// </summary>
    [Fact]
    public async Task AC22_second_run_against_a_populated_target_is_refused()
    {
        var outcome = await RunAsync(includeProblemData: false);
        Assert.True(outcome.Result.Succeeded);

        int patientsAfterFirstRun;
        await using (var before = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString))
        {
            patientsAfterFirstRun = await before.Patients.CountAsync(Ct);
        }

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        var second = await new MigrationRunner(
            new SqlServerLegacyDataSource(outcome.LegacyConnectionString),
            target,
            new MigrationOptions
            {
                SourceConnectionString = outcome.LegacyConnectionString,
                TargetConnectionString = outcome.TargetConnectionString,
            }).RunAsync(Ct);

        Assert.False(second.Succeeded);
        Assert.Contains("already holds domain data", second.Message, StringComparison.Ordinal);
        Assert.Equal(1, second.ExitCode);

        // Refusing left the target exactly as it was — never half-applied.
        Assert.Equal(patientsAfterFirstRun, await target.Patients.CountAsync(Ct));
    }

    /// <summary>AC-22 — a dry run reports without writing anything.</summary>
    [Fact]
    public async Task AC22_dry_run_writes_nothing_but_still_audits()
    {
        var legacyConnectionString = await harness.CreateLegacyDatabaseAsync(true, Ct);
        var targetConnectionString = await harness.CreateSeededTargetAsync(Ct);

        await using var target = MigrationHarness.CreateTargetContext(targetConnectionString);

        var result = await new MigrationRunner(
            new SqlServerLegacyDataSource(legacyConnectionString),
            target,
            new MigrationOptions
            {
                SourceConnectionString = legacyConnectionString,
                TargetConnectionString = targetConnectionString,
                DryRun = true,
            }).RunAsync(Ct);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Audit);
        Assert.True(result.Audit.HasFindings);

        Assert.Equal(0, await target.Patients.CountAsync(Ct));
        Assert.Equal(0, await target.Prescriptions.CountAsync(Ct));
    }

    /// <summary>
    /// Documents what happens if the seed runs before the migration instead of after:
    /// it works, but leaves a duplicate default doctor.
    /// </summary>
    /// <remarks>
    /// Kept as a test rather than left implicit because an operator may well seed
    /// first, and the outcome should be a known, harmless duplicate rather than a
    /// surprise. Neither legacy nor the rebuild indexes <c>Doctor.Code</c> uniquely,
    /// so nothing rejects it. The runbook prescribes migrate-then-seed for exactly
    /// this reason.
    /// </remarks>
    [Fact]
    public async Task Migrating_onto_an_already_seeded_target_succeeds_but_duplicates_the_default_doctor()
    {
        var legacyConnectionString = await harness.CreateLegacyDatabaseAsync(false, Ct);
        var targetConnectionString = await harness.CreateSeededTargetAsync(Ct);

        await using var target = MigrationHarness.CreateTargetContext(targetConnectionString);

        var result = await new MigrationRunner(
            new SqlServerLegacyDataSource(legacyConnectionString),
            target,
            new MigrationOptions
            {
                SourceConnectionString = legacyConnectionString,
                TargetConnectionString = targetConnectionString,
            }).RunAsync(Ct);

        Assert.True(result.Succeeded, result.Message);

        var doctorsNamedDefault = await target.Doctors
            .CountAsync(doctor => doctor.Code == "DR001", Ct);

        Assert.Equal(2, doctorsNamedDefault);
    }

    /// <summary>
    /// AC-19 — legacy appointments still point at a doctor that exists after migration.
    /// </summary>
    /// <remarks>
    /// The legacy doctor's id is the one SQL Server actually generated, not the GUID
    /// the legacy seeder wrote and EF discarded. Carrying the real id across is what
    /// keeps existing appointments valid — the defect BL-001 must not reproduce.
    /// </remarks>
    [Fact]
    public async Task AC19_migrated_appointments_still_reference_an_existing_doctor()
    {
        var outcome = await RunAsync(includeProblemData: false);

        await using var target = MigrationHarness.CreateTargetContext(outcome.TargetConnectionString);

        var doctorIds = await target.Doctors.Select(doctor => doctor.Id).ToListAsync(Ct);
        var referenced = await target.Appointments.Select(a => a.DoctorId).Distinct().ToListAsync(Ct);

        Assert.NotEmpty(referenced);
        Assert.All(referenced, id => Assert.Contains(id, doctorIds));
    }

    /// <summary>
    /// Runs a migration the way the runbook prescribes: onto a schema-only target,
    /// seeding afterwards.
    /// </summary>
    /// <remarks>
    /// Order matters, and a test is what surfaced it. Seeding first creates the
    /// fresh-install default doctor (<c>DR001</c> / "Dental Doctor"), and the legacy
    /// database supplies its own row with the same code and name — leaving two
    /// identical-looking doctors, since neither legacy nor the rebuild indexes
    /// <c>Doctor.Code</c> uniquely. A migration is not a fresh install, so the seed
    /// belongs afterwards, where its own guards make it fill only what legacy did not
    /// supply. <see cref="Migrating_onto_an_already_seeded_target_succeeds_but_duplicates_the_default_doctor"/>
    /// pins what the other order actually does.
    /// </remarks>
    private async Task<(MigrationOutcome Result, string LegacyConnectionString, string TargetConnectionString)>
        RunAsync(bool includeProblemData)
    {
        var legacyConnectionString = await harness.CreateLegacyDatabaseAsync(includeProblemData, Ct);
        var targetConnectionString = await harness.CreateMigratedTargetAsync(Ct);

        MigrationOutcome result;
        await using (var target = MigrationHarness.CreateTargetContext(targetConnectionString))
        {
            result = await new MigrationRunner(
                new SqlServerLegacyDataSource(legacyConnectionString),
                target,
                new MigrationOptions
                {
                    SourceConnectionString = legacyConnectionString,
                    TargetConnectionString = targetConnectionString,
                }).RunAsync(Ct);
        }

        await MigrationHarness.SeedAsync(targetConnectionString, Ct);

        return (result, legacyConnectionString, targetConnectionString);
    }
}
