using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DentalManagement.Api.Contracts;
using DentalManagement.Api.Tests.Harness;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Api.Tests;

/// <summary>
/// <c>POST /api/patients</c> over a real HTTP call, reachable only because the
/// dev authentication handler and permission checker (ST-002, ST-003) are in
/// the tree (spec AC-09's <c>[stub: ST-002, ST-003]</c> label) — the write
/// path underneath is real (<c>PatientRegistrationService</c>, already
/// covered without HTTP by <c>PatientRegistrationTests</c> in
/// <c>DentalManagement.Infrastructure.Tests</c>).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class PatientRegistrationEndpointTests(PostgresContainerFixture postgres)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object ValidRequestBody(string name = "New Patient") => new
    {
        name,
        age = 30,
        gender = "Female",
        phone = "0771234567",
        email = "patient@example.com",
        address = "123 Main St",
        note = "First visit",
    };

    /// <summary>
    /// AC-09 — a valid body returns 201 with the new patient's <c>Id</c>,
    /// <c>Code</c>, and the bill's <c>Code</c>, and the <c>Patient</c> and
    /// <c>Prescription</c> rows actually exist in the database. *(FR-09,
    /// FR-12, NFR-01)*
    /// </summary>
    [Fact]
    public async Task Valid_registration_returns_201_and_persists_patient_and_prescription()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        using var factory = ApiFactory.CreateDevelopment(connectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/patients", ValidRequestBody(), Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterPatientResponse>(JsonOptions, Ct);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal("P000001", body.Code);
        Assert.Equal("BILL001-P000001", body.BillCode);

        await using var read = PostgresContainerFixture.CreateContext(connectionString);

        var patients = await read.Patients.ToListAsync(Ct);
        Assert.Single(patients);
        Assert.Equal(body.Id, patients[0].Id);
        Assert.Equal("P000001", patients[0].Code);

        var prescriptions = await read.Prescriptions.ToListAsync(Ct);
        Assert.Single(prescriptions);
        Assert.Equal(patients[0].Id, prescriptions[0].PatientId);
        Assert.Equal("BILL001-P000001", prescriptions[0].Code);
    }
}
