using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DentalManagement.Api.Tests.Harness;

/// <summary>
/// Builds a <see cref="WebApplicationFactory{TEntryPoint}"/> over
/// <see cref="Program"/>, pointed at a real, already-migrated PostgreSQL
/// database. Configuration is supplied through real process environment
/// variables — the same channel <c>README.md</c>'s Configuration table
/// documents — never by touching <c>Program.cs</c> itself (spec FR-15, design
/// D-9). See <see cref="EnvironmentVariableScope"/> for why.
/// </summary>
public static class ApiFactory
{
    /// <summary>
    /// The Development boot with the dev authentication/authorization stubs
    /// (ST-002, ST-003) enabled — the boot AC-09 through AC-12 exercise, and
    /// the only one in which the endpoint is reachable at all today.
    /// </summary>
    /// <param name="connectionString">The real, already-migrated database this boot targets.</param>
    /// <param name="configureWebHost">
    /// Additional test-only host configuration (e.g. overriding the
    /// authentication scheme for <c>PatientRegistrationAuthorizationTests</c>'
    /// "no successful authentication" case) — applied before the boot this
    /// method triggers, never by touching <c>Program.cs</c> or
    /// <c>DevelopmentOnly/</c>.
    /// </param>
    public static WebApplicationFactory<Program> CreateDevelopment(
        string connectionString,
        Action<IWebHostBuilder>? configureWebHost = null) =>
        Create(connectionString, "Development", allowDevelopmentAuthenticationStub: null, configureWebHost);

    /// <summary>
    /// Boots <see cref="Program"/> in the given environment, with the opt-in
    /// flag set exactly as the caller specifies, or left unset so it falls
    /// back to whichever <c>appsettings*.json</c> file the environment loads.
    /// </summary>
    /// <remarks>
    /// The boot happens inside this call, while the environment variables are
    /// set — <see cref="WebApplicationFactory{TEntryPoint}.Services"/> is
    /// accessed here to force it, since <c>Program.cs</c>'s own code (the
    /// connection-string read and the auth gate alike) runs before
    /// <c>builder.Build()</c>, and this method's caller may run other tests
    /// that mutate the same environment variables before the factory would
    /// otherwise be triggered lazily. A boot that throws here still throws to
    /// the caller — the environment variables are restored either way via
    /// <see cref="EnvironmentVariableScope"/>, and the exception propagates
    /// unchanged for <c>StubScopingTests</c> to assert on.
    /// </remarks>
    public static WebApplicationFactory<Program> Create(
        string connectionString,
        string environment,
        bool? allowDevelopmentAuthenticationStub = null,
        Action<IWebHostBuilder>? configureWebHost = null)
    {
        var variables = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["ConnectionStrings__DentalManagement"] = connectionString,
            ["Database__MigrateOnStartup"] = "false",
        };

        if (allowDevelopmentAuthenticationStub is { } allow)
        {
            variables["DevelopmentAuth__AllowDevelopmentAuthenticationStub"] = allow ? "true" : "false";
        }

        using var scope = new EnvironmentVariableScope(variables);

        var factory = configureWebHost is null
            ? new WebApplicationFactory<Program>()
            : new WebApplicationFactory<Program>().WithWebHostBuilder(configureWebHost);

        try
        {
            _ = factory.Services;
        }
        catch
        {
            factory.Dispose();
            throw;
        }

        return factory;
    }
}
