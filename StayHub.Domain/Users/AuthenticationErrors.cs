using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Users;

public static class AuthenticationErrors
{
    public static readonly Error IdentityProviderUnavailable = Error.Failure(
        "Authentication.IdentityProviderUnavailable",
        "Unable to reach the identity provider");

    public static readonly Error RegistrationFailed = Error.Failure(
        "Authentication.RegistrationFailed",
        "User registration with the identity provider failed");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Authentication.InvalidCredentials",
        "The provided email or password is incorrect");
}