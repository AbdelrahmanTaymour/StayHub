using System.Text.Json.Serialization;

namespace StayHub.Infrastructure.Authentication.Models;

/// <summary>
///     Mirrors Keycloak's token endpoint response shape exactly (snake_case wire format).
///     Kept separate from Application's AccessTokenResponse so the rest of the system never
///     depends on Keycloak's exact JSON contract.
/// </summary>
public sealed class AuthorizationToken
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }

    [JsonPropertyName("refresh_expires_in")]
    public int RefreshExpiresIn { get; init; }

    [JsonPropertyName("refresh_token")] public string RefreshToken { get; init; } = string.Empty;

    [JsonPropertyName("token_type")] public string TokenType { get; init; } = string.Empty;

    [JsonPropertyName("scope")] public string Scope { get; init; } = string.Empty;
}