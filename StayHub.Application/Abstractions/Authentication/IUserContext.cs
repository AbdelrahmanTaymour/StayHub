namespace StayHub.Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid UserId { get; }
    string IdentityId { get; }
    IReadOnlyCollection<string> Roles { get; }
}