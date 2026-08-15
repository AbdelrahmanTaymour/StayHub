using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.Users.GetUser;

namespace StayHub.Application.Users.GetLoggedInUser;

public sealed record GetLoggedInUserQuery(IUserContext UserContext) : ICachedQuery<UserResponse>
{
    public string CacheKey => CacheKeys.LoggedInUser(UserContext.UserId);
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}