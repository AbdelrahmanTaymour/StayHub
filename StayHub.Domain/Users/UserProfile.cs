using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public sealed class UserProfile : Entity
{
    private UserProfile(Guid id, Guid userId, DateTime utcNow) : base(id)
    {
        UserId = userId;
        CreatedOnUtc = utcNow;
    }

    public Guid UserId { get; private set; }
    public Avatar? Avatar { get; private set; }
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
        Avatar = avatar;
        UpdatedOnUtc = utcNow;

        return Result.Success();
    }

    public Result UpdateBio(Bio bio, DateTime utcNow)
    {
        Bio = bio;
        UpdatedOnUtc = utcNow;

        return Result.Success();
    }

    public Result UpdatePhoneNumber(PhoneNumber phoneNumber, DateTime utcNow)
    {
        PhoneNumber = phoneNumber;
        UpdatedOnUtc = utcNow;

        return Result.Success();
    }
}