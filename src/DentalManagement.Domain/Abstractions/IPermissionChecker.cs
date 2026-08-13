namespace DentalManagement.Domain.Abstractions;

/// <summary>
/// Decides whether a role may act on a resource route.
/// </summary>
/// <remarks>
/// BL-007's seam (ST-003). Plain BCL types only, for the same reason as
/// <see cref="ICurrentUser"/> (learning L1). The dev-only implementation that
/// grants unconditionally until BL-007 lands lives in
/// <c>DentalManagement.Api.DevelopmentOnly</c> (spec FR-14, FR-15). The real
/// implementation will resolve <paramref name="resourceRoute"/> against the
/// seeded <c>Resource</c>/<c>Permission</c> catalog per DR-015/DR-016
/// (CQ-013) — this contract does not change when it does.
/// </remarks>
public interface IPermissionChecker
{
    /// <summary>
    /// Checks whether <paramref name="role"/> may act on <paramref name="resourceRoute"/>.
    /// </summary>
    /// <param name="role">The authenticated user's role, e.g. from <see cref="ICurrentUser.Role"/>.</param>
    /// <param name="resourceRoute">
    /// The resource route being accessed, e.g. <c>"root.patient-create"</c> — one of the
    /// route identities in <c>SeedCatalog.Resources</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the role may proceed.</returns>
    Task<bool> CheckAsync(string role, string resourceRoute, CancellationToken cancellationToken = default);
}
