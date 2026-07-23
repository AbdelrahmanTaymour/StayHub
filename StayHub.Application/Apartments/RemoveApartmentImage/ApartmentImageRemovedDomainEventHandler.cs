using Hangfire;
using MediatR;
using StayHub.Domain.Apartments.Events;

namespace StayHub.Application.Apartments.RemoveApartmentImage;

public class ApartmentImageRemovedDomainEventHandler(
    IBackgroundJobClient backgroundJobClient) : INotificationHandler<ApartmentImageRemovedDomainEvent>
{
    public Task Handle(ApartmentImageRemovedDomainEvent notification, CancellationToken cancellationToken)
    {
        backgroundJobClient.Enqueue<DeleteApartmentImageBlobJob>(job =>
            job.ExecuteAsync(notification.Url, CancellationToken.None));

        return Task.CompletedTask;
    }
}