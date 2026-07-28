using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public static class UserSessionErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "UserSession.NotFound",
        "The session with the specified identifier was not found");

    public static readonly Error AlreadyRevoked = Error.Conflict(
        "UserSession.AlreadyRevoked",
        "The session has already been revoked");

    public static readonly Error NotAuthorized = Error.Unauthorized(
        "UserSession.NotAuthorized",
        "You can only revoke your own sessions");
}