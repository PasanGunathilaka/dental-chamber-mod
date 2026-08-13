using DentalManagement.Domain.Abstractions;

namespace DentalManagement.Api.DevelopmentOnly;

/// <summary>
/// ST-002 — a fixed, obviously-fake identity standing in for BL-002's authenticated session.
/// </summary>
/// <remarks>
/// <para>
/// <c>admin@dev.local</c> / <c>Admin</c> are the values chosen at propose time (spec N-4): both
/// are already real values in the seeded catalog (<c>SeedCatalog.RoleNames</c> contains
/// <c>"Admin"</c>), and the <c>.local</c> address reads as fake on sight, satisfying
/// <c>references/stub-discipline.md</c>'s "recognisable as fake on sight" rule without inventing
/// a new never-real user.
/// </para>
/// <para>
/// Registered only inside the same <c>IsDevelopment() &amp;&amp;
/// DevelopmentAuthOptions.AllowDevelopmentAuthenticationStub</c> gate as
/// <see cref="StubAuthenticationHandler"/> and <see cref="StubPermissionChecker"/> — see
/// <c>Program.cs</c>. It never reads the request; every request that authenticates through
/// <see cref="StubAuthenticationHandler"/> resolves to this same fixed identity.
/// </para>
/// </remarks>
public sealed class StubCurrentUser : ICurrentUser
{
    /// <summary>The fixed dev-only user name (spec N-4).</summary>
    public const string DevelopmentUserName = "admin@dev.local";

    /// <summary>The fixed dev-only role — already a real value in <c>SeedCatalog.RoleNames</c>.</summary>
    public const string DevelopmentRole = "Admin";

    public string UserName => DevelopmentUserName;

    public string Role => DevelopmentRole;
}
