using System.Text.Json.Serialization;

namespace StayHub.Infrastructure.Authentication.Models;

public sealed class UserRepresentationModel
{
    [JsonPropertyName("username")] public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")] public string Email { get; init; } = string.Empty;

    [JsonPropertyName("firstName")] public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("lastName")] public string LastName { get; init; } = string.Empty;

    [JsonPropertyName("enabled")] public bool Enabled { get; init; } = true;

    [JsonPropertyName("emailVerified")] public bool EmailVerified { get; init; } = true;

    [JsonPropertyName("credentials")] public CredentialRepresentationModel[] Credentials { get; init; } = [];
}