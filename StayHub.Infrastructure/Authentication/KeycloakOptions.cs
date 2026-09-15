namespace StayHub.Infrastructure.Authentication;

public sealed class KeycloakOptions
{
    public string AdminUrl { get; set; } = string.Empty;

    public string TokenUrl { get; set; } = string.Empty;

    public string AdminClientId { get; init; } = string.Empty;

    public string AdminClientSecret { get; init; } = string.Empty;

    public string AuthClientId { get; init; } = string.Empty;

    public string AuthClientSecret { get; init; } = string.Empty;

    public string PasswordResetClientId { get; init; } = string.Empty;

    public string PasswordResetRedirectUri { get; init; } = string.Empty;

    public int PasswordResetLinkLifespanSeconds { get; init; } = 900;
}