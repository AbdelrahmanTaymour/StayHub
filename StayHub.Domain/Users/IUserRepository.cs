namespace StayHub.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default);

    Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken = default);

    void Add(User user);
}