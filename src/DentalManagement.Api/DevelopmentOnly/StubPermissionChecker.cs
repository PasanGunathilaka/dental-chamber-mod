using DentalManagement.Domain.Abstractions;

namespace DentalManagement.Api.DevelopmentOnly;

/// <summary>
/// ST-003 — grants every check unconditionally, standing in for BL-007's server-side
/// authorization until the real <c>Resource</c>/<c>Permission</c>-backed implementation lands.
/// </summary>
/// <remarks>
/// Registered only inside the same development-and-flag gate as
/// <see cref="StubCurrentUser"/> and <see cref="StubAuthenticationHandler"/> — see
/// <c>Program.cs</c>. <see cref="Authorization.PermissionAuthorizationHandler"/> is the only
/// caller; the endpoint's own <c>[Permission("root.patient-create")]</c> declaration does not
/// change when BL-007 substitutes a real implementation for this one (spec FR-13).
/// </remarks>
public sealed class StubPermissionChecker : IPermissionChecker
{
    public Task<bool> CheckAsync(string role, string resourceRoute, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
