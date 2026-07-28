using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public static class UserProfileErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "UserProfile.NotFound",
        "The profile for the specified user was not found");
}