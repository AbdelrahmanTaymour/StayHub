namespace StayHub.Domain.Users;

public sealed class UserRole
{
    public Guid UserId { get; init; }

    public int RoleId { get; init; }
}