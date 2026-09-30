using StayHub.Application.Abstractions.Caching;

namespace StayHub.Application.Users.GetOwnerProfile;

public sealed record GetOwnerProfileQuery(Guid OwnerId) : ICachedQuery<UserProfileResponse>
{
    public string CacheKey => CacheKeys.OwnerProfile(OwnerId);
    public TimeSpan? Expiration => TimeSpan.FromMinutes(1);
}