using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments.Events;

namespace StayHub.Domain.Apartments;

public sealed class MaintenanceRequest : Entity
{
    public MaintenanceRequest(
        Guid id,
        Guid apartmentId,
        Guid reportedByUserId,
        Title title,
        Description description,
        MaintenanceRequestStatus status,
        DateTime createdOnUtc)
        : base(id)
    {
        ApartmentId = apartmentId;
        ReportedByUserId = reportedByUserId;
        Title = title;
        Description = description;
        Status = status;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid ApartmentId { get; private set; }
    public Guid ReportedByUserId { get; private set; }
    public Title Title { get; private set; }
    public Description Description { get; private set; }
    public MaintenanceRequestStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ResolvedOnUtc { get; private set; }
    public DateTime? ClosedOnUtc { get; private set; }

    public static MaintenanceRequest Create(
        Guid apartmentId,
        Guid reportedByUserId,
        Title title,
        Description description)
    {
        var request = new MaintenanceRequest(
            Guid.NewGuid(),
            apartmentId,
            reportedByUserId,
            title,
            description,
            MaintenanceRequestStatus.Open,
            DateTime.UtcNow);

        request.RaiseDomainEvent(new MaintenanceRequestCreatedDomainEvent(request.Id));

        return request;
    }

    public Result Start()
    {
        if (Status != MaintenanceRequestStatus.Open) return Result.Failure(MaintenanceRequestErrors.NotOpen);

        Status = MaintenanceRequestStatus.InProgress;

        RaiseDomainEvent(new MaintenanceRequestStartedDomainEvent(Id));

        return Result.Success();
    }

    public Result Resolve(DateTime utcNow)
    {
        if (Status != MaintenanceRequestStatus.InProgress)
            return Result.Failure(MaintenanceRequestErrors.NotInProgress);

        Status = MaintenanceRequestStatus.Resolved;
        ResolvedOnUtc = utcNow;

        RaiseDomainEvent(new MaintenanceRequestResolvedDomainEvent(Id));

        return Result.Success();
    }

    public Result Close(DateTime utcNow)
    {
        if (Status != MaintenanceRequestStatus.Resolved) return Result.Failure(MaintenanceRequestErrors.NotResolved);

        Status = MaintenanceRequestStatus.Closed;
        ClosedOnUtc = utcNow;

        RaiseDomainEvent(new MaintenanceRequestClosedDomainEvent(Id));

        return Result.Success();
    }
}