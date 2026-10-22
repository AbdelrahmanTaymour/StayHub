namespace StayHub.Application.Users.GetUser;

public sealed record UserResponse
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string? Bio { get; init; }

    public string? PhoneNumber { get; init; }
}