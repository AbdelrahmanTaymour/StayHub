using StayHub.Api.Endpoints.Users;

namespace StayHub.Api.FunctionalTests.Users;

public class UserData
{
    public static readonly RegisterUserRequest RegisterTestUserAsync = new("test", "test", "test@test.com", "1234");
}