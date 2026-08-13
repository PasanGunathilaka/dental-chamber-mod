using Microsoft.AspNetCore.Authorization;

namespace DentalManagement.Api.Authorization;

/// <summary>
/// Declares the resource route an action or controller requires permission for.
/// </summary>
/// <remarks>
/// <para>
/// Used alongside <c>[Authorize]</c> — e.g. <c>[Authorize] [Permission("root.patient-create")]</c>
/// — never in place of it. Implementing <see cref="IAuthorizationRequirementData"/> lets ASP.NET
/// Core fold the <see cref="PermissionRequirement"/> straight into the endpoint's authorization
/// policy without a named policy registered ahead of time, so a new resource route needs no
/// change anywhere but the attribute's argument.
/// </para>
/// <para>
/// This is the plumbing spec FR-13 and design D-6 describe as unchanged when BL-007 substitutes
/// the real <c>IPermissionChecker</c> for <see cref="PermissionAuthorizationHandler"/>'s current
/// dev-only one (ST-003).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PermissionAttribute(string resourceRoute) : Attribute, IAuthorizationRequirementData
{
    /// <summary>The resource route this action or controller requires permission for.</summary>
    public string ResourceRoute { get; } = resourceRoute;

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new PermissionRequirement(ResourceRoute);
    }
}
