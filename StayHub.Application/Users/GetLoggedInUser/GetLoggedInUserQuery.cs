using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Caching;

namespace StayHub.Application.Users.GetLoggedInUser;

public sealed record GetLoggedInUserQuery(IUserContext UserContext) : ICachedQuery<LoggedInUserResponse>
{
    public string CacheKey => CacheKeys.LoggedInUser(UserContext.UserId);
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}