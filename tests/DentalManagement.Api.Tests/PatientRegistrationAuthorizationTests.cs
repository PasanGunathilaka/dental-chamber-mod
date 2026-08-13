using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using DentalManagement.Api.Contracts;
using DentalManagement.Api.Tests.Harness;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalManagement.Api.Tests;

/// <summary>
/// <c>POST /api/patients</c> really is protected — <c>[Authorize]</c> plus the
/// <c>root.patient-create</c> permission requirement actually gate the
/// endpoint, and the request contract never accepts a client-supplied code
/// (spec AC-12, FR-09, FR-13).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class PatientRegistrationAuthorizationTests(PostgresContainerFixture postgres)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object ValidRequestBody() => new
    {
        name = "New Patient",
        age = 30,
        gender = "Female",
    };

    /// <summary>
    /// AC-12 — the endpoint carries <c>[Authorize]</c> and a permission-policy
    /// requirement naming <c>root.patient-create</c>: a request for which no
    /// scheme authenticates the caller fails with 401/403 rather than
    /// succeeding. The dev stub (ST-002) authenticates every request
    /// unconditionally, so this test replaces the default authentication
    /// scheme — for this boot only, via the test host's own
    /// <c>ConfigureTestServices</c>, never by touching <c>Program.cs</c> or
    /// <c>DevelopmentOnly/</c> — with a handler that never succeeds, standing
    /// in for "the dev authentication registration removed."
    /// </summary>
    [Fact]
    public async Task Request_with_no_successful_authentication_fails_rather_than_succeeds()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        using var factory = ApiFactory.CreateDevelopment(connectionString, builder =>
            builder.ConfigureTestServices(services =>
                services
                    .AddAuthentication(AlwaysUnauthenticatedHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, AlwaysUnauthenticatedHandler>(
                        AlwaysUnauthenticatedHandler.SchemeName, _ => { })));

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/patients", ValidRequestBody(), Ct);

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401 or 403 but got {(int)response.StatusCode} {response.StatusCode}.");
    }

    /// <summary>
    /// AC-12 — <c>Code</c> and <c>Id</c> are absent from
    /// <see cref="RegisterPatientRequest"/> entirely (spec FR-09,
    /// DR-001): a body carrying an extra <c>code</c> property is simply
    /// ignored by model binding, and the response still carries a
    /// server-generated code.
    /// </summary>
    [Fact]
    public async Task Client_supplied_code_in_the_request_body_is_ignored()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        using var factory = ApiFactory.CreateDevelopment(connectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/patients",
            new
            {
                name = "New Patient",
                age = 30,
                gender = "Female",
                code = "HACKED-CODE",
            },
            Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterPatientResponse>(JsonOptions, Ct);

        Assert.NotNull(body);
        Assert.Equal("P000001", body!.Code);
        Assert.NotEqual("HACKED-CODE", body.Code);
    }

    /// <summary>
    /// Never succeeds, standing in for "no authentication handler is
    /// registered at all" without touching <c>Program.cs</c> or
    /// <c>DevelopmentOnly/</c> — it is registered as the default scheme by
    /// this test's own <c>ConfigureTestServices</c> override, which runs after
    /// (and so takes priority over) <c>Program.cs</c>'s own
    /// <c>AddAuthentication(StubAuthenticationHandler.SchemeName)</c> call.
    /// </summary>
    private sealed class AlwaysUnauthenticatedHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "TestNoAuth";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}
