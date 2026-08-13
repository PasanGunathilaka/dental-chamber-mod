namespace DentalManagement.Api.DevelopmentOnly;

/// <summary>
/// Whether the dev-only authentication/authorization stubs (ST-002, ST-003) may be registered.
/// </summary>
/// <remarks>
/// Same shape as <c>AdminBootstrapOptions.AllowDevelopmentDemoAccounts</c> — the mechanism this
/// repository already uses to keep known dev-only behaviour out of production (design D-4): a
/// flag defaulting to <c>false</c>, absent from <c>appsettings.json</c>, set only in
/// <c>appsettings.Development.json</c>. Unlike that flag, this one is not sufficient on its own —
/// <c>Program.cs</c> additionally requires <c>IHostEnvironment.IsDevelopment()</c> before
/// registering anything, so the flag alone cannot open the door (spec FR-15).
/// </remarks>
public class DevelopmentAuthOptions
{
    public const string ConfigurationSection = "DevelopmentAuth";

    /// <summary>
    /// When true — and only when the host environment is also Development — the dev-only
    /// <c>ICurrentUser</c> (ST-002) and <c>IPermissionChecker</c> (ST-003) implementations are
    /// registered in place of BL-002 and BL-007, which are not yet built.
    /// </summary>
    public bool AllowDevelopmentAuthenticationStub { get; set; }
}
