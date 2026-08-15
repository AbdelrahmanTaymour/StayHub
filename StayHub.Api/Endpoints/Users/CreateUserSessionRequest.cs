namespace StayHub.Api.Endpoints.Users;

public sealed record CreateUserSessionRequest(string DeviceInfo, string IpAddress);