namespace StayHub.Api.Endpoints.Users;

public sealed record RegisterUserRequest(string FirstName, string LastName, string Email, string Password);