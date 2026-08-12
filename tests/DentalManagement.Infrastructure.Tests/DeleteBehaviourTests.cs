using DentalManagement.Infrastructure.Tests.Harness;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Infrastructure.Tests;

/// <summary>
/// Replays the three captured persistence-layer fixtures that pin this schema's
/// delete behaviour.
/// </summary>
/// <remarks>
/// <para>
/// GM-012, GM-019, and GM-024 were captured at the <c>persistence</c> seam layer
/// against the legacy EF6/SQL Server configuration, which is why they can be
/// replayed here against a schema alone — no controller or service is involved on
/// either side. Each test arranges through the persistence path, acts by deleting,
/// and asserts the fixture's own captured counts.
/// </para>
/// <para>
/// A failure here means this schema's delete configuration is wrong, not that the
/// fixture is wrong.
/// </para>
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class DeleteBehaviourTests(PostgresContainerFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// AC-10 / GM-012 — deleting a patient cascades through bills to every line
    /// item and payment.
    /// </summary>
    [Fact]
    public async Task GM012_deleting_a_patient_cascades_to_bills_line_items_and_payments()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        // Arrange: the fixture's own shape — 2 prescriptions, each with 1 line
        // item and 1 payment, so 4 dependent rows in total.
        var patientId = Guid.NewGuid();
        await using (var arrange = PostgresContainerFixture.CreateContext(connectionString))
        {
            var patient = TestEntities.Patient("P000001");
            patient.Id = patientId;
            arrange.Patients.Add(patient);

            var service = TestEntities.MedicalService(1, "Scaling", 500m);
            arrange.MedicalServices.Add(service);

            foreach (var billNumber in new[] { 1, 2 })
            {
                var bill = TestEntities.Prescription(patientId, $"BILL00{billNumber}-P000001");
                arrange.Prescriptions.Add(bill);
                arrange.PatientMedicalServices.Add(
                    TestEntities.LineItem(patientId, bill.Id, service.Id));
                arrange.Payments.Add(TestEntities.Payment(bill.Id, 100m));
            }

            await arrange.SaveChangesAsync(Ct);
        }

        // Act
        await using (var act = PostgresContainerFixture.CreateContext(connectionString))
        {
            var patient = await act.Patients.SingleAsync(p => p.Id == patientId, Ct);
            act.Patients.Remove(patient);
            await act.SaveChangesAsync(Ct);
        }

        // Assert: the fixture's captured output, field for field.
        await using var assert = PostgresContainerFixture.CreateContext(connectionString);

        Assert.Null(await assert.Patients.SingleOrDefaultAsync(p => p.Id == patientId, Ct));
        Assert.Equal(0, await assert.Prescriptions.CountAsync(Ct));
        Assert.Equal(0, await assert.PatientMedicalServices.CountAsync(Ct));
        Assert.Equal(0, await assert.Payments.CountAsync(Ct));
    }

    /// <summary>
    /// AC-12 / GM-019 — deleting a patient leaves their tagged medical conditions
    /// behind, orphaned.
    /// </summary>
    /// <remarks>
    /// The direct contrast to GM-012, and the reason
    /// <c>PatientMedicalInfo</c> carries no foreign key. If someone "fixes" that
    /// omission, this test is what fails — loudly, instead of a captured fixture
    /// quietly diverging later (spec A5, design R-4).
    /// </remarks>
    [Fact]
    public async Task GM019_deleting_a_patient_leaves_tagged_conditions_orphaned()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        var patientId = Guid.NewGuid();
        await using (var arrange = PostgresContainerFixture.CreateContext(connectionString))
        {
            var patient = TestEntities.Patient("P000002");
            patient.Id = patientId;
            arrange.Patients.Add(patient);

            var diabetic = TestEntities.MedicalInfo("Diabetic");
            var asthmatic = TestEntities.MedicalInfo("Asthmatic");
            arrange.MedicalInfos.AddRange(diabetic, asthmatic);

            arrange.PatientMedicalInfos.AddRange(
                TestEntities.PatientMedicalInfo(patientId, diabetic.Id),
                TestEntities.PatientMedicalInfo(patientId, asthmatic.Id));

            await arrange.SaveChangesAsync(Ct);
        }

        await using (var act = PostgresContainerFixture.CreateContext(connectionString))
        {
            var patient = await act.Patients.SingleAsync(p => p.Id == patientId, Ct);
            act.Patients.Remove(patient);
            await act.SaveChangesAsync(Ct);
        }

        await using var assert = PostgresContainerFixture.CreateContext(connectionString);

        Assert.Null(await assert.Patients.SingleOrDefaultAsync(p => p.Id == patientId, Ct));

        // The fixture's captured count is 2 — unchanged.
        var orphaned = await assert.PatientMedicalInfos
            .Where(tag => tag.PatientId == patientId)
            .ToListAsync(Ct);

        Assert.Equal(2, orphaned.Count);
        Assert.All(orphaned, tag => Assert.Equal(patientId, tag.PatientId));
    }

    /// <summary>
    /// AC-12 / GM-019 — asserts the mechanism, not just the outcome: no foreign
    /// key constraint exists on the table at all.
    /// </summary>
    [Fact]
    public async Task PatientMedicalInfo_has_no_foreign_key_constraint()
    {
        await using var context = PostgresContainerFixture.CreateContext(
            await postgres.CreateMigratedDatabaseAsync(Ct));

        var foreignKeys = await context.Database
            .SqlQuery<string>($"""
                SELECT conname AS "Value" FROM pg_constraint
                WHERE contype = 'f'
                  AND conrelid = '"PatientMedicalInfo"'::regclass
                """)
            .ToListAsync(Ct);

        Assert.Empty(foreignKeys);
    }

    /// <summary>
    /// AC-11 / GM-024 — deleting a product cascades to all of its movement rows.
    /// </summary>
    [Fact]
    public async Task GM024_deleting_a_product_cascades_to_its_inventory_movements()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        var productId = Guid.NewGuid();
        await using (var arrange = PostgresContainerFixture.CreateContext(connectionString))
        {
            var product = TestEntities.Product("Gloves", onHand: 10);
            product.Id = productId;
            arrange.Products.Add(product);

            // The fixture's own shape: 3 movement rows.
            foreach (var quantity in new[] { 3, 4, 5 })
            {
                arrange.Inventories.Add(TestEntities.InventoryMovement(productId, quantity));
            }

            await arrange.SaveChangesAsync(Ct);
        }

        await using (var act = PostgresContainerFixture.CreateContext(connectionString))
        {
            var product = await act.Products.SingleAsync(p => p.Id == productId, Ct);
            act.Products.Remove(product);
            await act.SaveChangesAsync(Ct);
        }

        await using var assert = PostgresContainerFixture.CreateContext(connectionString);

        Assert.Null(await assert.Products.SingleOrDefaultAsync(p => p.Id == productId, Ct));
        Assert.Equal(0, await assert.Inventories.CountAsync(Ct));
    }
}
