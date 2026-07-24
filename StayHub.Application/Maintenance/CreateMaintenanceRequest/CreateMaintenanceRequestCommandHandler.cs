using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.CreateMaintenanceRequest;

internal sealed class CreateMaintenanceRequestCommandHandler(
    IApartmentRepository apartmentRepository,
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateMaintenanceRequestCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateMaintenanceRequestCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

        var maintenanceRequest = MaintenanceRequest.Create(
            request.ApartmentId,
            request.ReportedByUserId,
            new Title(request.Title),
            new Description(request.Description));

        maintenanceRequestRepository.Add(maintenanceRequest);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return maintenanceRequest.Id;
    }
}