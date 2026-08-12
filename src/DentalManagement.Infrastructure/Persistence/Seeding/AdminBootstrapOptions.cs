namespace DentalManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// How the first administrator account comes into existence.
/// </summary>
/// <remarks>
/// CQ-017: "allow simple known demo credentials only in explicitly
/// local/development seed data. Production deployments must never create accounts
/// with shared hardcoded passwords; production admin bootstrap must use secure
/// environment-specific credentials or a forced first-login password reset."
///
/// Passed in as options rather than read from <c>IHostEnvironment</c> so the
/// infrastructure project keeps no dependency on the hosting stack; the API host
/// binds these from environment configuration (spec FR-19).
/// </remarks>
public class AdminBootstrapOptions
{
    public const string ConfigurationSection = "AdminBootstrap";

    /// <summary>
    /// When true, the seeder creates the two known demo accounts. Must only ever
    /// be true for local/development environments.
    /// </summary>
    public bool AllowDevelopmentDemoAccounts { get; set; }

    /// <summary>Production administrator username, from environment configuration.</summary>
    public string? UserName { get; set; }

    /// <summary>Production administrator password, from environment configuration.</summary>
    public string? Password { get; set; }

    /// <summary>
    /// When true, the bootstrapped administrator must change their password on
    /// first login — CQ-017's second sanctioned option.
    /// </summary>
    public bool RequirePasswordChangeOnFirstLogin { get; set; } = true;
}
