using StayHub.Domain.Abstractions;

namespace StayHub.Application.Abstractions.Authentication;

public interface IJwtService
{
    /// <summary>
    ///     Retrieves an access token and refresh token for a user, enabling authenticated access
    ///     to secured resources. Authentication is performed using the provided email and password.
    /// </summary>
    /// <param name="email">The email address of the user attempting to authenticate.</param>
    /// <param name="password">The password associated with the specified email address.</param>
    /// <param name="cancellationToken">Optional cancellation token for controlling the operation's lifetime.</param>
    /// <returns>A result containing the access token, refresh token, and expiration details if authentication is successful.</returns>
    Task<Result<AccessTokenResponse>> GetAccessTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Refreshes an access token using the provided refresh token. The operation results in a new access token
    ///     and refresh token being issued, extending the user's authenticated session.
    /// </summary>
    /// <param name="refreshToken">The refresh token previously issued to the client, used to request a new access token.</param>
    /// <param name="cancellationToken">An optional cancellation token that can be used to cancel the operation.</param>
    /// <returns>A result containing the newly issued access token, refresh token, and expiration details.</returns>
    Task<Result<AccessTokenResponse>> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revokes the given refresh token at the identity provider - this ends the underlying session
    ///     (future refreshes with this token will fail), but does NOT retroactively invalidate an
    ///     already-issued access token still within its own (short) expiry window. See the chat notes
    ///     on why that's the accepted trade-off for stateless JWTs.
    /// </summary>
    Task<Result> LogOutAsync(string refreshToken, CancellationToken cancellationToken = default);
}