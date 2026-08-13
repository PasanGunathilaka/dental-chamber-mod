using System.Net;
using System.Net.Http.Json;
using DentalManagement.Api.Tests.Harness;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalManagement.Api.Tests;

/// <summary>
/// <c>POST /api/patients</c> request validation — field-scoped 400
/// <see cref="ValidationProblemDetails"/>, and legacy's minimum lengths
/// deliberately not reintroduced (spec FR-10, FR-11).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class PatientRegistrationValidationTests(PostgresContainerFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Dictionary<string, object?> ValidRequestBody(string name = "New Patient") => new()
    {
        ["name"] = name,
        ["age"] = 30,
        ["gender"] = "Female",
        ["phone"] = "0771234567",
        ["email"] = "patient@example.com",
        ["address"] = "123 Main St",
        ["note"] = "First visit",
    };

    private async Task<(HttpResponseMessage Response, string ConnectionString)> PostAsync(
        Dictionary<string, object?> body)
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        using var factory = ApiFactory.CreateDevelopment(connectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/patients", body, Ct);
        return (response, connectionString);
    }

    /// <summary>
    /// A model-state key names the field either as the property name itself
    /// (attribute validation, e.g. <c>"Name"</c>) or as the JSON path a body
    /// deserialization failure reports (e.g. <c>"$.gender"</c> for an
    /// out-of-set enum value) — both count as "naming the field."
    /// </summary>
    private static void AssertNamesField(ValidationProblemDetails? problem, string fieldName)
    {
        Assert.NotNull(problem);
        Assert.Contains(
            problem!.Errors.Keys,
            key => key.Contains(fieldName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// AC-10 — <c>Gender: "Unknown"</c> returns 400 naming the <c>Gender</c>
    /// field, and no <c>Patient</c> row is created. The value never reaches
    /// the database, so <c>CK_Patient_Gender</c> is never exercised here.
    /// *(FR-10)*
    /// </summary>
    [Fact]
    public async Task Gender_outside_the_named_set_returns_400_and_creates_no_patient()
    {
        var body = ValidRequestBody();
        body["gender"] = "Unknown";

        var (response, connectionString) = await PostAsync(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        AssertNamesField(problem, "gender");

        await using var read = PostgresContainerFixture.CreateContext(connectionString);
        Assert.Equal(0, await read.Patients.CountAsync(Ct));
    }

    /// <summary>AC-11 — a 31-character Name (over the 30 maximum) returns 400 naming <c>Name</c>.</summary>
    [Fact]
    public async Task Name_over_thirty_characters_returns_400()
    {
        var body = ValidRequestBody(new string('a', 31));

        var (response, _) = await PostAsync(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        AssertNamesField(problem, "name");
    }

    /// <summary>AC-11 — a 101-character Email (over the 100 maximum) returns 400 naming <c>Email</c>.</summary>
    [Fact]
    public async Task Email_over_one_hundred_characters_returns_400()
    {
        var body = ValidRequestBody();
        body["email"] = new string('a', 101);

        var (response, _) = await PostAsync(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        AssertNamesField(problem, "email");
    }

    /// <summary>AC-11 — a missing Name returns 400 naming <c>Name</c>.</summary>
    [Fact]
    public async Task Missing_name_returns_400()
    {
        var body = ValidRequestBody();
        body.Remove("name");

        var (response, _) = await PostAsync(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        AssertNamesField(problem, "name");
    }

    /// <summary>
    /// AC-11 — a 3-character Name is accepted. Legacy's minimum lengths were
    /// deliberately not reintroduced (BL-001 left them out of the database
    /// because they would reject rows the migration must carry), so this
    /// positive case matters as much as the negative ones above.
    /// </summary>
    [Fact]
    public async Task Three_character_name_is_accepted()
    {
        var body = ValidRequestBody("Abc");

        var (response, _) = await PostAsync(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
