using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public static class UserSessionErrors
{
    public static Error NotFound = new(
        "UserSession.NotFound",
        "The session with the specified identifier was not found");

    public static Error AlreadyRevoked = new(
        "UserSession.AlreadyRevoked",
        "The session has already been revoked");

    public static Error NotAuthorized = new(
        "UserSession.NotAuthorized",
        "You can only revoke your own sessions");
}