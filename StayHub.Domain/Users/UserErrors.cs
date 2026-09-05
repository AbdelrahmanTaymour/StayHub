using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "User.NotFound",
        "The user with the specified identifier was not found");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "User.EmailNotUnique",
        "The provided email is already in use");

    public static readonly Error NotAuthorized = Error.Forbidden(
        "User.NotAuthorized",
        "You can only access your own details");
}