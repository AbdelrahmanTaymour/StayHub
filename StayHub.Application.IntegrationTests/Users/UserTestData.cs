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
}