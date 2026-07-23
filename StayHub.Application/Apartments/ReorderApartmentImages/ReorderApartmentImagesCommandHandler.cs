using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.ReorderApartmentImages;

internal sealed class ReorderApartmentImagesCommandHandler(
    IApartmentRepository apartmentRepository,
    IApartmentImageRepository imageRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ReorderApartmentImagesCommand>
{
    public async Task<Result> Handle(ReorderApartmentImagesCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure(ApartmentErrors.NotAuthorized);

        var images = await imageRepository.GetByApartmentIdAsync(request.ApartmentId, cancellationToken);

        if (images.Count != request.OrderedImageIds.Count ||
            !images.Select(i => i.Id).ToHashSet().SetEquals(request.OrderedImageIds))
            return Result.Failure(ApartmentImageErrors.InvalidOrderPayload);

        for (var i = 0; i < request.OrderedImageIds.Count; i++)
        {
            var image = images.Single(x => x.Id == request.OrderedImageIds[i]);
            image.Reorder(i);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}