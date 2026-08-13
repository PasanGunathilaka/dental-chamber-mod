using Microsoft.AspNetCore.Authorization;

namespace DentalManagement.Api.Authorization;

/// <summary>
/// An authorization requirement naming the resource route being accessed.
/// </summary>
/// <remarks>
/// Carried by <see cref="PermissionAttribute"/> and satisfied by
/// <see cref="PermissionAuthorizationHandler"/>, which resolves it through
/// <c>IPermissionChecker</c> — the seam BL-007 replaces without touching this
/// type or the endpoint's own attribute declaration (spec FR-13, design D-1
/// architecture diagram).
/// </remarks>
public sealed class PermissionRequirement(string resourceRoute) : IAuthorizationRequirement
{
    /// <summary>
    /// The resource route this requirement authorizes, e.g. <c>"root.patient-create"</c>.
    /// </summary>
    public string ResourceRoute { get; } = resourceRoute;
}
