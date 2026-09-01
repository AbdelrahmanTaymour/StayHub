using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Users;

internal static class UserTestData
{
    public static User CreateUser(string firstName = "Test", string lastName = "User")
    {
        var email = Email.Create($"{Guid.NewGuid():N}@test.local").Value;

        var user = User.Create(new FirstName(firstName), new LastName(lastName), email, DateTime.UtcNow);
        user.SetIdentityId(Guid.NewGuid().ToString());

        return user;
    }

    public static UserProfile CreateProfile(Guid userId, DateTime? utcNow = null)
    {
        return UserProfile.Create(userId, utcNow ?? DateTime.UtcNow);
    }

    public static UserSession CreateSession(
        Guid userId,
        string deviceInfo = "Chrome on Windows",
        string ipAddress = "203.0.113.5",
        DateTime? utcNow = null)
    {
        return UserSession.Create(userId, new DeviceInfo(deviceInfo), IpAddress.Create(ipAddress).Value,
            utcNow ?? DateTime.UtcNow);
    }
}