namespace StayHub.Api.Endpoints.Users;

public sealed record UpdateUserProfileRequest(string? AvatarUrl, string? Bio, string? PhoneNumber);