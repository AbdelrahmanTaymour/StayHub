using StayHub.Infrastructure.Authorization;

namespace StayHub.Api.Extensions;

/// <summary>
/// Thin wrapper so endpoint registration reads the same as the
/// [HasPermission(Permissions.X)] attribute did. HasPermissionAttribute is
/// an AuthorizeAttribute (IAuthorizeData), which RequireAuthorization
/// accepts directly — PermissionAuthorizationPolicyProvider needs no
/// changes at all.
/// </summary>
internal static class EndpointAuthorizationExtensions
{
    public static RouteHandlerBuilder HasPermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder.RequireAuthorization(new HasPermissionAttribute(permission));
    }
}