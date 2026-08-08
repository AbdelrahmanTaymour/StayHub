namespace StayHub.Api.Controllers.Users;

public sealed record CreateUserSessionRequest(string DeviceInfo, string IpAddress);