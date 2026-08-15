using StayHub.Application.Abstractions.Caching;

namespace StayHub.Application.Users.GetUser;

public sealed record GetUserQuery(Guid UserId) : ICachedQuery<UserResponse>
{
    public string CacheKey => CacheKeys.User(UserId);
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}