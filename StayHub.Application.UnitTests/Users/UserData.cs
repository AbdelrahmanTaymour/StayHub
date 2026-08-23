using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

internal static class UserData
{
    public static readonly FirstName FirstName = new("First");
    public static readonly LastName LastName = new("Last");
    public static readonly Email Email = Email.Create("test@test.com").Value;
    public static User Create() => User.Create(FirstName, LastName, Email, DateTime.UtcNow);
}