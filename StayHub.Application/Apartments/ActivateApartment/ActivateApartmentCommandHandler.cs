using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.ActivateApartment;

internal sealed class ActivateApartmentCommandHandler(
    IApartmentRepository apartmentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ActivateApartmentCommand>
{
    public async Task<Result> Handle(ActivateApartmentCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure(ApartmentErrors.NotAuthorized);

        var result = apartment.Activate();

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}