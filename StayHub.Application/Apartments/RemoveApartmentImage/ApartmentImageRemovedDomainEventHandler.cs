using MediatR;
using StayHub.Application.Abstractions.BackgroundJobs;
using StayHub.Domain.Apartments.Events;

namespace StayHub.Application.Apartments.RemoveApartmentImage;

public class ApartmentImageRemovedDomainEventHandler(
    IBackgroundJobScheduler backgroundJobScheduler) : INotificationHandler<ApartmentImageRemovedDomainEvent>
{
    public Task Handle(ApartmentImageRemovedDomainEvent notification, CancellationToken cancellationToken)
    {
        backgroundJobScheduler.Enqueue<DeleteApartmentImageBlobJob>(job =>
            job.ExecuteAsync(notification.Url, CancellationToken.None));

        return Task.CompletedTask;
    }
}