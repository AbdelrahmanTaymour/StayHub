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

    public static UserProfile Create(Guid userId)
    {
        var userProfile = new UserProfile(Guid.NewGuid(), userId, DateTime.UtcNow);

        return userProfile;
    }

    public Result UpdateAvatar(Avatar avatar)
    {
        Avatar = avatar;
        UpdatedOnUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public Result UpdateBio(Bio bio)
    {
        Bio = bio;
        UpdatedOnUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public Result UpdatePhoneNumber(PhoneNumber phoneNumber)
    {
        PhoneNumber = phoneNumber;
        UpdatedOnUtc = DateTime.UtcNow;

        return Result.Success();
    }
}