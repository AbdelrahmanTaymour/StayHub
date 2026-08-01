using System.Text.Json.Serialization;

namespace StayHub.Infrastructure.Authentication.Models;

public sealed class CredentialRepresentationModel
{
    [JsonPropertyName("type")] public string Type { get; init; } = "password";

    [JsonPropertyName("value")] public string Value { get; init; } = string.Empty;

    [JsonPropertyName("temporary")] public bool Temporary { get; init; }
}