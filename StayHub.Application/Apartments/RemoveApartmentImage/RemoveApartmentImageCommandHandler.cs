using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.RemoveApartmentImage;

internal sealed class RemoveApartmentImageCommandHandler(
    IApartmentImageRepository imageRepository,
    IApartmentRepository apartmentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveApartmentImageCommand>
{
    public async Task<Result> Handle(RemoveApartmentImageCommand request, CancellationToken cancellationToken)
    {
        var image = await imageRepository.GetByIdAsync(request.ImageId, cancellationToken);

        if (image is null) return Result.Failure(ApartmentImageErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(image.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure(ApartmentErrors.NotAuthorized);

        image.MarkForRemoval();

        imageRepository.Remove(image);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}