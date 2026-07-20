namespace StayHub.Domain.Users;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(UserProfile profile);
}