using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public sealed class UserSessionErrors
{
    public static Error NotFound = new(
        "UserSession.NotFound",
        "The session with the specified identifier was not found");

    public static Error AlreadyRevoked = new(
        "UserSession.AlreadyRevoked",
        "The session has already been revoked");
}