using Microsoft.AspNetCore.Http;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Domain.Users;

namespace StayHub.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId =>
        httpContextAccessor
            .HttpContext?
            .User
            .GetUserId() ??
        throw new ApplicationException("User context is unavailable");

    public string IdentityId =>
        httpContextAccessor
            .HttpContext?
            .User
            .GetIdentityId() ??
        throw new ApplicationException("User context is unavailable");

    public IReadOnlyCollection<string> Roles =>
        httpContextAccessor
            .HttpContext?
            .User
            .GetRoles() ??
        [];


    public bool IsAdmin => Roles.Contains(Role.Admin.Name);

    public bool IsOwner(Guid ownerId) =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true && UserId == ownerId;
}