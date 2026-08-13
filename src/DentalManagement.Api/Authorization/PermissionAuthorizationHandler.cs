using DentalManagement.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace DentalManagement.Api.Authorization;

/// <summary>
/// Satisfies a <see cref="PermissionRequirement"/> by asking <see cref="IPermissionChecker"/>
/// whether the current user's role may act on the requirement's resource route.
/// </summary>
/// <remarks>
/// This handler is the plumbing BL-007 keeps unchanged (spec FR-13, design D-1 request-path
/// diagram): today <c>IPermissionChecker</c> resolves to the dev-only <c>StubPermissionChecker</c>
/// (ST-003), which grants unconditionally; BL-007 substitutes the real
/// <c>Resource</c>/<c>Permission</c>-backed implementation with no change here.
/// </remarks>
public sealed class PermissionAuthorizationHandler(
    ICurrentUser currentUser,
    IPermissionChecker permissionChecker)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var granted = await permissionChecker.CheckAsync(
            currentUser.Role,
            requirement.ResourceRoute);

        if (granted)
        {
            context.Succeed(requirement);
        }
    }
}
