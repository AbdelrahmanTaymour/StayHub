namespace StayHub.Application.Users.GetOwnerProfile;

public sealed record UserProfileResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
    public int ActiveListingsCount { get; init; }
}