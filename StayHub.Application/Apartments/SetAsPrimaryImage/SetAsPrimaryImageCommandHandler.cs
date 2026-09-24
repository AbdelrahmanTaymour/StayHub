using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.SetAsPrimaryImage;

internal sealed class SetAsPrimaryImageCommandHandler(
    IApartmentRepository apartmentRepository,
    IApartmentImageRepository imageRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<SetAsPrimaryImageCommand>
{
    public async Task<Result> Handle(SetAsPrimaryImageCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (!userContext.IsOwner(apartment.OwnerId) && !userContext.IsAdmin)
            return Result.Failure(ApartmentErrors.NotAuthorized);

        var image = await imageRepository.GetByIdAsync(request.ImageId, cancellationToken);

        if (image is null || image.ApartmentId != request.ApartmentId)
            return Result.Failure(ApartmentImageErrors.NotFound);

        var currentPrimaryImage =
            await imageRepository.GetPrimaryByApartmentIdAsync(request.ApartmentId, cancellationToken);

        if (currentPrimaryImage?.Id != image.Id)
        {
            currentPrimaryImage?.UnsetAsPrimary();
            image.SetAsPrimary();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}