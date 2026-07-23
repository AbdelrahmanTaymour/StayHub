using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.AddApartmentAmenity;

internal sealed class AddApartmentAmenityCommandHandler(
    IApartmentRepository apartmentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<AddApartmentAmenityCommand>
{
    public async Task<Result> Handle(AddApartmentAmenityCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure(ApartmentErrors.NotAuthorized);

        var result = apartment.AddAmenity(request.Amenity);

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}