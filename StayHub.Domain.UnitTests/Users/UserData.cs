using StayHub.Domain.Users;

namespace StayHub.Domain.UnitTests.Users;

internal static class UserData
{
    public static readonly FirstName FirstName = new("First");
    public static readonly LastName LastName = new("Last");
    public static readonly Email Email = Email.Create("test@test.com").Value;

    public static readonly Guid OwnerId = Guid.Parse("019ffabd-d288-74fb-8304-bbfe442d9309");
    public static readonly Guid UserId = Guid.Parse("019ffabe-b124-7ae2-8ace-0ecddd114839");

    public static User CreateUser(DateTime? utcNow = null)
    {
        return User.Create(FirstName, LastName, Email, utcNow ?? DateTime.UtcNow);
    }
}