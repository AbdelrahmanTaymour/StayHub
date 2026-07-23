using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.AddApartmentImage;

public class AddApartmentImageCommandHandler(
    IApartmentRepository apartmentRepository,
    IApartmentImageRepository imageRepository,
    IFileStorageService fileStorageService,
    IUnitOfWork unitOfWork) : ICommandHandler<AddApartmentImageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddApartmentImageCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

        if (apartment.OwnerId != request.RequestedByUserId) return Result.Failure<Guid>(ApartmentErrors.NotAuthorized);

        var countExistingImages = await imageRepository.CountByApartmentId(
            request.ApartmentId,
            cancellationToken);

        var url = await fileStorageService.UploadAsync(
            request.FileContent,
            request.FileName,
            request.ContentType,
            cancellationToken);

        var image = ApartmentImage.Create(
            request.ApartmentId,
            new ImageUrl(url),
            countExistingImages,
            request.IsPrimary);

        imageRepository.Add(image);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return image.Id;
    }
}