using StayHub.Domain.Abstractions;
using StayHub.Domain.Users.Events;

namespace StayHub.Domain.Users;

public sealed class UserSession : Entity
{
    private UserSession(
        Guid id,
        Guid userId,
        DeviceInfo deviceInfo,
        IpAddress ipAddress,
        DateTime createdOnUtc) : base(id)
    {
        UserId = userId;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
        CreatedOnUtc = createdOnUtc;
        LastSeenOnUtc = createdOnUtc;
    }

    public Guid UserId { get; }
    public DeviceInfo DeviceInfo { get; private set; }
    public IpAddress IpAddress { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime LastSeenOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }

    public static UserSession Create(Guid userId, DeviceInfo deviceInfo, IpAddress ipAddress)
    {
        var session = new UserSession(Guid.NewGuid(), userId, deviceInfo, ipAddress, DateTime.UtcNow);

        session.RaiseDomainEvent(new UserSessionCreatedDomainEvent(session.Id, session.UserId));

        return session;
    }


    public Result Touch()
    {
        LastSeenOnUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Revoke()
    {
        if (RevokedOnUtc is not null) return Result.Failure(UserSessionErrors.AlreadyRevoked);

        RevokedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new UserSessionRevokedDomainEvent(Id, UserId));

        return Result.Success();
    }
}