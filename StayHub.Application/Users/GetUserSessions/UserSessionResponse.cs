namespace StayHub.Application.Users.GetUserSessions;

public sealed class UserSessionResponse
{
    public Guid Id { get; init; }

    public string DeviceInfo { get; init; }

    public string IpAddress { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public DateTime LastSeenOnUtc { get; init; }
}