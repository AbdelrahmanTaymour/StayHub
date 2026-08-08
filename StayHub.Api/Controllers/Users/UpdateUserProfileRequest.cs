namespace StayHub.Api.Controllers.Users;

public sealed record UpdateUserProfileRequest(string? AvatarUrl, string? Bio, string? PhoneNumber);