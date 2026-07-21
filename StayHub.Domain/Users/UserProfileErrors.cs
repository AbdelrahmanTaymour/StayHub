using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public static class UserProfileErrors
{
    public static Error NotFound = new(
        "UserProfile.NotFound",
        "The profile for the specified user was not found");
}