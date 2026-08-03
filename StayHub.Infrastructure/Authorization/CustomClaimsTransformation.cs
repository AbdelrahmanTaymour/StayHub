using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using StayHub.Infrastructure.Authentication;

namespace StayHub.Infrastructure.Authorization;

internal sealed class CustomClaimsTransformation(
    AuthorizationService authorizationService)
    : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(claim => claim.Type == ClaimTypes.Role) &&
            principal.HasClaim(claim => claim.Type == JwtRegisteredClaimNames.Sub))
            return principal;

        var userId = principal.GetIdentityId();

        var userRoles = await authorizationService.GetRolesForUserAsync(userId);

        if (userRoles is null) return principal;

        var claimsIdentity = new ClaimsIdentity();

        claimsIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, userRoles.Id.ToString()));

        foreach (var role in userRoles.Roles) claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.Name));

        principal.AddIdentity(claimsIdentity);

        return principal;
    }
}