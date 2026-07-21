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

    public static UserSession Create(Guid userId, DeviceInfo deviceInfo, IpAddress ipAddress, DateTime utcNow)
    {
        var session = new UserSession(Guid.CreateVersion7(), userId, deviceInfo, ipAddress, utcNow);

        session.RaiseDomainEvent(new UserSessionCreatedDomainEvent(session.Id, session.UserId));

        return session;
    }


    public Result Touch(DateTime utcNow)
    {
        LastSeenOnUtc = utcNow;

        return Result.Success();
    }

    public Result Revoke(DateTime utcNow)
    {
        if (RevokedOnUtc is not null) return Result.Failure(UserSessionErrors.AlreadyRevoked);

        RevokedOnUtc = utcNow;

        RaiseDomainEvent(new UserSessionRevokedDomainEvent(Id, UserId));

        return Result.Success();
    }
}