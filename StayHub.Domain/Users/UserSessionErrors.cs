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

    public static readonly Error NotAuthorized = Error.Forbidden(
        "UserSession.NotAuthorized",
        "You can only revoke your own sessions");

    public static readonly Error Revoked = Error.Validation(
        "UserSession.Revoked",
        "The session has been revoked");

    public static readonly Error InvalidTimestamp = Error.Validation(
        "UserSession.InvalidTimestamp",
        "The timestamp is invalid for the current session state.");
}