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
}