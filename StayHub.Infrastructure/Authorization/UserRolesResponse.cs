namespace StayHub.Infrastructure.Authorization;

public sealed class UserRolesResponse
{
    public Guid Id { get; init; }

    public IList<RoleResponse> Roles { get; init; } = [];
}

public sealed class RoleResponse
{
    public int RoleId { get; init; }
    public string Name { get; init; } = string.Empty;
}