using StayHub.Application.Abstractions.Authentication;

namespace StayHub.Application.IntegrationTests.Integration;

public sealed class TestUserContext : IUserContext
{
    public Guid UserId { get; set; }

    public string IdentityId { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; set; } =
        Array.Empty<string>();
}