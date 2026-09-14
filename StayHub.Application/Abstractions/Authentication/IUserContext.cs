namespace StayHub.Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid UserId { get; }
    string IdentityId { get; }
    IReadOnlyCollection<string> Roles { get; }

    bool IsAdmin { get; }
    public bool IsOwner(Guid ownerId);
}