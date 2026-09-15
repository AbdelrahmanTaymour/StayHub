using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Abstractions.Authentication;

public interface IAuthenticationService
{
    /// <summary>
    ///     Registers a new user with the identity provider and returns its identity id (Keycloak's "sub"),
    ///     to be stored on the local User via User.SetIdentityId.
    /// </summary>
    Task<Result<string>> RegisterAsync(
        User user,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Triggers Keycloak's own password-reset email for the given identity id, via the Admin
    ///     API's execute-actions-email endpoint with the UPDATE_PASSWORD required action.
    /// </summary>
    Task<Result> ForgotPasswordAsync(
        string identityId,
        CancellationToken cancellationToken = default);
}