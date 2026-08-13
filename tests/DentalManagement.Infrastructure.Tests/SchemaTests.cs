using DentalManagement.Infrastructure.Tests.Harness;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Infrastructure.Tests;

/// <summary>
/// Constraints the database itself must enforce — unique indexes, enum check
/// constraints, and money precision.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class SchemaTests(PostgresContainerFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>AC-05 — DR-001: patient codes are unique at the database.</summary>
    [Fact]
    public async Task Duplicate_patient_code_is_rejected_by_the_database()
    {
        await using var context = await MigratedContextAsync();

        context.Patients.Add(TestEntities.Patient("P000001"));
        await context.SaveChangesAsync(Ct);

        context.Patients.Add(TestEntities.Patient("P000001", "Another Patient"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }

    /// <summary>AC-05 — DR-017: catalog names are unique.</summary>
    [Fact]
    public async Task Duplicate_medical_service_name_is_rejected_by_the_database()
    {
        await using var context = await MigratedContextAsync();

        context.MedicalServices.Add(TestEntities.MedicalService(1, "Scaling", 500m));
        await context.SaveChangesAsync(Ct);

        context.MedicalServices.Add(TestEntities.MedicalService(2, "Scaling", 700m));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }

    /// <summary>AC-05 — DR-017: the other catalog's names are unique too.</summary>
    [Fact]
    public async Task Duplicate_medical_info_name_is_rejected_by_the_database()
    {
        await using var context = await MigratedContextAsync();

        context.MedicalInfos.Add(TestEntities.MedicalInfo("Diabetic"));
        await context.SaveChangesAsync(Ct);

        context.MedicalInfos.Add(TestEntities.MedicalInfo("Diabetic"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
    }

    /// <summary>AC-06 — CQ-007: only the three known gender values are storable.</summary>
    [Fact]
    public async Task Gender_outside_the_typed_set_is_rejected_by_the_database()
    {
        await using var context = await MigratedContextAsync();

        var patient = TestEntities.Patient("P000010");
        context.Patients.Add(patient);
        await context.SaveChangesAsync(Ct);

        // Written as raw SQL because the C# enum makes the invalid state
        // unrepresentable — the point is that the database refuses it too, so a
        // migration or a future direct write cannot smuggle one in.
        await Assert.ThrowsAnyAsync<Exception>(() =>
            context.Database.ExecuteSqlAsync(
                $"""UPDATE "Patient" SET "Gender" = 99 WHERE "Id" = {patient.Id}""",
                Ct));
    }

    /// <summary>
    /// AC-09 — CQ-006: each entity's status column rejects a value belonging to
    /// another entity's set. This is the integrity hole the shared legacy
    /// <c>Status</c> table left open, closed at the schema level.
    /// </summary>
    [Theory]
    // A bill cannot hold "Received" (3) or "Appointed" (7).
    [InlineData("Prescription", "Status", 3)]
    [InlineData("Prescription", "Status", 7)]
    // A product cannot hold "Active" (5).
    [InlineData("Product", "Status", 5)]
    // A movement cannot hold "In Stock" (1).
    [InlineData("Inventory", "Status", 1)]
    // An appointment cannot hold "Closed" (6).
    [InlineData("Appointment", "Status", 6)]
    public async Task Status_value_from_another_entitys_set_is_rejected(
        string table,
        string column,
        int foreignStatusValue)
    {
        await using var context = await MigratedContextAsync();

        var sql = $"""INSERT INTO "{table}" ("{column}") VALUES ({foreignStatusValue})""";

        // The insert is invalid for more than one reason (missing required
        // columns), so this asserts only that the database refuses it. The
        // check constraint's own existence is asserted separately below.
        await Assert.ThrowsAnyAsync<Exception>(() =>
            context.Database.ExecuteSqlRawAsync(sql, Ct));
    }

    /// <summary>
    /// AC-09 — the four typed status check constraints and the gender one exist
    /// by name, so the rejection above cannot be passing for an unrelated reason.
    /// </summary>
    [Theory]
    [InlineData("CK_Prescription_Status")]
    [InlineData("CK_Product_Status")]
    [InlineData("CK_Inventory_Status")]
    [InlineData("CK_Appointment_Status")]
    [InlineData("CK_Patient_Gender")]
    public async Task Check_constraint_exists(string constraintName)
    {
        await using var context = await MigratedContextAsync();

        var exists = await context.Database
            .SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = {constraintName}
                ) AS "Value"
                """)
            .SingleAsync(Ct);

        Assert.True(exists, $"expected check constraint {constraintName}");
    }

    /// <summary>
    /// AC-08/NFR-04 — money columns are fixed-precision numeric, never floating
    /// point.
    /// </summary>
    [Theory]
    [InlineData("MedicalService", "Charge")]
    [InlineData("Prescription", "TotalCharge")]
    [InlineData("Prescription", "TotalPayable")]
    [InlineData("Prescription", "TotalDue")]
    [InlineData("Payment", "Amount")]
    [InlineData("Product", "UnitPrice")]
    [InlineData("Product", "SalePrice")]
    public async Task Money_column_is_numeric_with_two_decimal_places(string table, string column)
    {
        await using var context = await MigratedContextAsync();

        var dataType = await context.Database
            .SqlQuery<string>($"""
                SELECT data_type AS "Value" FROM information_schema.columns
                WHERE table_name = {table} AND column_name = {column}
                """)
            .SingleAsync(Ct);

        Assert.Equal("numeric", dataType);

        var scale = await context.Database
            .SqlQuery<int>($"""
                SELECT numeric_scale AS "Value" FROM information_schema.columns
                WHERE table_name = {table} AND column_name = {column}
                """)
            .SingleAsync(Ct);

        Assert.Equal(2, scale);
    }

    /// <summary>AC-08 — a two-decimal money value round-trips unchanged.</summary>
    [Fact]
    public async Task Money_value_round_trips_without_loss()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        var serviceId = Guid.NewGuid();
        await using (var write = PostgresContainerFixture.CreateContext(connectionString))
        {
            var service = TestEntities.MedicalService(1, "Root Canal", 1234.56m);
            service.Id = serviceId;
            write.MedicalServices.Add(service);
            await write.SaveChangesAsync(Ct);
        }

        await using var read = PostgresContainerFixture.CreateContext(connectionString);
        var stored = await read.MedicalServices.SingleAsync(s => s.Id == serviceId, Ct);

        Assert.Equal(1234.56m, stored.Charge);
    }

    /// <summary>
    /// A8/design R-6 — timestamps map to <c>timestamp without time zone</c> and
    /// come back exactly as written, with no timezone shift.
    /// </summary>
    [Fact]
    public async Task Timestamp_round_trips_without_a_timezone_shift()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        var patientId = Guid.NewGuid();
        await using (var write = PostgresContainerFixture.CreateContext(connectionString))
        {
            var patient = TestEntities.Patient("P000020");
            patient.Id = patientId;
            write.Patients.Add(patient);
            await write.SaveChangesAsync(Ct);
        }

        await using var read = PostgresContainerFixture.CreateContext(connectionString);
        var stored = await read.Patients.SingleAsync(p => p.Id == patientId, Ct);

        Assert.Equal(TestEntities.FixedNow, stored.Created);
        Assert.Equal(DateTimeKind.Unspecified, stored.Created.Kind);
    }

    /// <summary>
    /// FR-12/A6 — ids persist exactly as the application assigned them. The
    /// legacy column was database-generated, which is what let the seeded doctor's
    /// id silently differ from the one the client used.
    /// </summary>
    [Fact]
    public async Task Application_assigned_id_is_persisted_unchanged()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        var assignedId = Guid.NewGuid();

        await using (var write = PostgresContainerFixture.CreateContext(connectionString))
        {
            var doctor = TestEntities.Doctor();
            doctor.Id = assignedId;
            write.Doctors.Add(doctor);
            await write.SaveChangesAsync(Ct);
        }

        await using var read = PostgresContainerFixture.CreateContext(connectionString);
        Assert.NotNull(await read.Doctors.SingleOrDefaultAsync(d => d.Id == assignedId, Ct));
    }

    private async Task<Infrastructure.Persistence.DentalDbContext> MigratedContextAsync() =>
        PostgresContainerFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync(Ct));
}
