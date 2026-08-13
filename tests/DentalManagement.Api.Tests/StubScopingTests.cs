using DentalManagement.Api.DevelopmentOnly;
using DentalManagement.Api.Tests.Harness;
using DentalManagement.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DentalManagement.Api.Tests;

/// <summary>
/// The stub-scoping gate that keeps ST-002 and ST-003 out of production —
/// spec AC-13, FR-15, design D-4/D-5, risk R-3. This is the criterion the
/// whole bypass mechanism rests on.
/// </summary>
/// <remarks>
/// Every test here supplies a real, valid connection string even for the
/// boots it expects to fail: <c>AddInfrastructure</c>
/// (<c>DependencyInjection.cs:29</c>) reads it and throws first, at line 17 of
/// <c>Program.cs</c>, before the environment-and-flag gate at lines 50-76 ever
/// runs. Without a valid connection string, a non-Development boot would
/// throw for that unrelated reason instead of the one under test here — a
/// green test for the wrong cause.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class StubScopingTests(PostgresContainerFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// AC-13 — booting with <c>ASPNETCORE_ENVIRONMENT</c> set to anything
    /// other than Development throws <see cref="InvalidOperationException"/>
    /// at startup, naming BL-002 and BL-007 — never starting an unprotected
    /// host.
    /// </summary>
    [Fact]
    public async Task Non_development_boot_throws_naming_BL_002_and_BL_007()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        var exception = Assert.Throws<InvalidOperationException>(
            () => ApiFactory.Create(connectionString, "Production"));

        Assert.Contains("BL-002", exception.Message, StringComparison.Ordinal);
        Assert.Contains("BL-007", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// AC-13 — the opt-in flag alone does not open the door outside
    /// Development. The same non-Development boot, with the flag explicitly
    /// set <c>true</c>, still throws naming BL-002 and BL-007 (design D-4 —
    /// "the flag alone cannot open the door").
    /// </summary>
    [Fact]
    public async Task Non_development_boot_with_the_opt_in_flag_explicitly_true_still_throws()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        var exception = Assert.Throws<InvalidOperationException>(
            () => ApiFactory.Create(connectionString, "Production", allowDevelopmentAuthenticationStub: true));

        Assert.Contains("BL-002", exception.Message, StringComparison.Ordinal);
        Assert.Contains("BL-007", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// AC-13 — the dev stub types are absent from the service collection in a
    /// non-Development boot. <c>Program.cs</c>'s environment-and-flag gate
    /// throws before <c>builder.Build()</c> ever runs, so the branch that adds
    /// <see cref="StubCurrentUser"/>, <see cref="StubPermissionChecker"/>, and
    /// <see cref="StubAuthenticationHandler"/> to the service collection never
    /// executes, and no <see cref="IServiceProvider"/> is ever produced for
    /// this boot to query — <see cref="ApiFactory.Create"/> itself throws
    /// before returning a factory. There is no container in which those types
    /// could be found; failing to even obtain one is the strongest available
    /// proof of their absence for a boot that never finishes constructing a
    /// container in the first place.
    /// </summary>
    [Fact]
    public async Task Non_development_boot_never_produces_a_service_provider_at_all()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);

        Assert.Throws<InvalidOperationException>(
            () => ApiFactory.Create(connectionString, "Production"));
    }

    /// <summary>
    /// Contrast case: the same three stub types ARE registered — and
    /// resolvable — in the Development boot AC-09 through AC-12 exercise,
    /// confirming the gate actually distinguishes the two environments rather
    /// than simply always failing.
    /// </summary>
    [Fact]
    public async Task Development_boot_registers_the_dev_stub_types()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(Ct);
        using var factory = ApiFactory.CreateDevelopment(connectionString);

        Assert.IsType<StubCurrentUser>(factory.Services.GetRequiredService<ICurrentUser>());
        Assert.IsType<StubPermissionChecker>(factory.Services.GetRequiredService<IPermissionChecker>());
    }
}
