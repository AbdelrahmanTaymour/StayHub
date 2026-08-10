using StayHub.Domain.Abstractions;
using StayHub.Domain.Users.Events;

namespace StayHub.Domain.Users;

public sealed class UserProfile : Entity
{
    private UserProfile(Guid id, Guid userId, DateTime utcNow) : base(id)
    {
        UserId = userId;
        CreatedOnUtc = utcNow;
    }

    private UserProfile()
    {
    }

    public Guid UserId { get; private set; }
    public Avatar? AvatarUrl { get; private set; }
    public Bio? Bio { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public static UserProfile Create(Guid userId, DateTime utcNow)
    {
        var userProfile = new UserProfile(Guid.CreateVersion7(), userId, utcNow);

        return userProfile;
    }

    public Result UpdateAvatar(Avatar avatar, DateTime utcNow)
    {
        AvatarUrl = avatar;
        UpdatedOnUtc = utcNow;

        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId));

        return Result.Success();
    }

    public Result UpdateBio(Bio bio, DateTime utcNow)
    {
        Bio = bio;
        UpdatedOnUtc = utcNow;

        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId));

        return Result.Success();
    }

    public Result UpdatePhoneNumber(PhoneNumber phoneNumber, DateTime utcNow)
    {
        PhoneNumber = phoneNumber;
        UpdatedOnUtc = utcNow;

        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(UserId));

        return Result.Success();
    }
}