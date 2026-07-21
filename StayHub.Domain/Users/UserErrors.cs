using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public static class UserErrors
{
    public static Error NotFound = new(
        "User.Found",
        "The user with the specified identifier was not found");

    public static Error InvalidCredentials = new(
        "User.InvalidCredentials",
        "The provided credentials were invalid");

    public static Error EmailNotUnique = new(
        "User.EmailNotUnique",
        "The provided email is already in use");
}