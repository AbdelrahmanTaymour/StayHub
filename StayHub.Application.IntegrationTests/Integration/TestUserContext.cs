using StayHub.Application.Abstractions.Authentication;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Integration;

public sealed class TestUserContext : IUserContext
{
    public Guid UserId { get; set; }

    public string IdentityId { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; set; } =
        Array.Empty<string>();

    public bool IsAdmin => Roles.Contains(Role.Admin.Name);
    public bool IsOwner(Guid ownerId) => UserId == ownerId;
}