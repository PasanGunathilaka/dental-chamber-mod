using DentalManagement.Domain.Abstractions;
using DentalManagement.Domain.Enums;
using DentalManagement.Domain.Patients;
using DentalManagement.Infrastructure.Patients;
using DentalManagement.Infrastructure.Persistence;
using DentalManagement.Infrastructure.Tests.Harness;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Infrastructure.Tests;

/// <summary>
/// <see cref="PatientRegistrationService"/> — the seam <c>GM-003</c> replays
/// against (design D-1). Real PostgreSQL via <see cref="PostgresContainerFixture"/>,
/// never HTTP — a test that needed a web host here would mean the logic had
/// leaked into the wrong layer (design R-4).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class PatientRegistrationTests(PostgresContainerFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static NewPatient SampleNewPatient(string name = "New Patient") =>
        new(name, 30, Gender.Female, "0771234567", "patient@example.com", "123 Main St", null);

    private static PatientRegistrationService CreateService(DentalDbContext context, DateTime now) =>
        new(context, new PatientCodeSequence(context), new FixedClock(now));

    /// <summary>
    /// AC-01/AC-02 — GM-003 replay: registering into an empty database creates
    /// exactly one <c>Patient</c> coded <c>P000001</c> and its <c>Prescription</c>
    /// at <c>Status = Active</c>, coded to the bill pattern, with <c>TotalDue</c>
    /// at zero.
    /// </summary>
    [Fact]
    public async Task Register_into_an_empty_database_replays_GM_003()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        await using var context = PostgresContainerFixture.CreateContext(connectionString);
        var service = CreateService(context, TestEntities.FixedNow);

        var result = await service.RegisterAsync(SampleNewPatient(), Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("P000001", result.PatientCode);
        Assert.Equal("BILL001-P000001", result.BillCode);

        await using var read = PostgresContainerFixture.CreateContext(connectionString);

        var patients = await read.Patients.ToListAsync(Ct);
        Assert.Single(patients);
        Assert.Equal("P000001", patients[0].Code);

        var prescriptions = await read.Prescriptions.ToListAsync(Ct);
        Assert.Single(prescriptions);
        var prescription = prescriptions[0];
        Assert.Equal(patients[0].Id, prescription.PatientId);
        Assert.Equal(BillStatus.Active, prescription.Status);
        Assert.Equal("BILL001-P000001", prescription.Code);
        Assert.Equal(0m, prescription.TotalDue);
        Assert.Equal(0m, prescription.TotalCharge);
        Assert.Equal(0m, prescription.DiscountPercent);
        Assert.Equal(0m, prescription.DiscountAmount);
        Assert.Equal(0m, prescription.FixedDiscount);
        Assert.Equal(0m, prescription.TotalPayable);
        Assert.Equal(0m, prescription.TotalPaid);
    }

    /// <summary>
    /// AC-05 — the DR-002 gap: forcing the second insert to fail (a real
    /// <c>IX_Prescription_Code</c> unique-index violation, design D-7) leaves no
    /// new <c>Patient</c> row committed. The pre-existing patient below exists
    /// only to satisfy the pre-inserted bill's foreign key — it is not the
    /// subject of this assertion, so the count captured before the attempt is
    /// compared against the count after, and the attempted patient's own code
    /// is checked absent.
    /// </summary>
    [Fact]
    public async Task Failing_prescription_insert_leaves_no_new_patient_committed()
    {
        var (connectionString, _, attemptedPatientCode) = await ArrangeCollidingRegistrationAsync();

        await using var read = PostgresContainerFixture.CreateContext(connectionString);

        // Only the pre-arranged patient (needed for the colliding bill's FK)
        // remains — the transaction's own patient never persisted.
        Assert.Equal(1, await read.Patients.CountAsync(Ct));
        Assert.False(await read.Patients.AnyAsync(p => p.Code == attemptedPatientCode, Ct));
    }

    /// <summary>
    /// AC-08 — the service reports failure, not success, when the write does
    /// not persist (closing GM-002's defect, FR-03/A2).
    /// </summary>
    [Fact]
    public async Task Failing_prescription_insert_reports_failure_not_success()
    {
        var (_, result, _) = await ArrangeCollidingRegistrationAsync();

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.FailureReason));
    }

    /// <summary>
    /// AC-06 — twenty simultaneous registrations against the same empty
    /// database produce twenty patients with twenty distinct codes and twenty
    /// bills, proving <c>patient_code_seq</c> is collision-safe under
    /// concurrency (spec FR-08, NFR-02).
    /// </summary>
    [Fact]
    public async Task Twenty_concurrent_registrations_produce_twenty_distinct_codes()
    {
        const int concurrency = 20;
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        var registrations = Enumerable.Range(0, concurrency).Select(async index =>
        {
            await using var context = PostgresContainerFixture.CreateContext(connectionString);
            var service = CreateService(context, TestEntities.FixedNow);
            return await service.RegisterAsync(SampleNewPatient($"Concurrent Patient {index}"), Ct);
        });

        var results = await Task.WhenAll(registrations);

        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(concurrency, results.Select(r => r.PatientCode).Distinct().Count());
        Assert.Equal(concurrency, results.Select(r => r.BillCode).Distinct().Count());

        await using var read = PostgresContainerFixture.CreateContext(connectionString);
        Assert.Equal(concurrency, await read.Patients.CountAsync(Ct));
        Assert.Equal(concurrency, await read.Prescriptions.CountAsync(Ct));
    }

    /// <summary>
    /// Arranges a database holding one patient and one bill whose code is
    /// exactly what the next registration attempt will generate, then makes
    /// that attempt. Returns the connection string, the attempt's result, and
    /// the patient code the attempt tried (and failed) to commit.
    /// </summary>
    private async Task<(string ConnectionString, RegistrationResult Result, string AttemptedPatientCode)>
        ArrangeCollidingRegistrationAsync()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        // The first call to patient_code_seq in a fresh database returns 1, so
        // the upcoming registration attempt will generate patient code
        // "P000001" and bill code "BILL001-P000001" — computed here, ahead of
        // time, purely from the pure formatters (no database access).
        var attemptedPatientCode = PatientCodeFormatter.Format(1);
        var collidingBillCode = BillCodeFormatter.Format(attemptedPatientCode, 1);

        await using (var arrange = PostgresContainerFixture.CreateContext(connectionString))
        {
            var existingPatient = TestEntities.Patient("P999999", "Existing Patient");
            arrange.Patients.Add(existingPatient);
            arrange.Prescriptions.Add(TestEntities.Prescription(existingPatient.Id, collidingBillCode));
            await arrange.SaveChangesAsync(Ct);
        }

        await using var context = PostgresContainerFixture.CreateContext(connectionString);
        var service = CreateService(context, TestEntities.FixedNow);

        var result = await service.RegisterAsync(SampleNewPatient(), Ct);

        return (connectionString, result, attemptedPatientCode);
    }

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime Now => now;
    }
}
