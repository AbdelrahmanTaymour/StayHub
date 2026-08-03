using Microsoft.AspNetCore.Authorization;
using StayHub.Infrastructure.Authentication;

namespace StayHub.Infrastructure.Authorization;

internal sealed class PermissionAuthorizationHandler(AuthorizationService authorizationService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity is not { IsAuthenticated: true }) return;

        var identityId = context.User.GetIdentityId();

        var permissions = await authorizationService.GetPermissionsForUserAsync(identityId);

        if (permissions.Contains(requirement.Permission)) context.Succeed(requirement);
    }
}