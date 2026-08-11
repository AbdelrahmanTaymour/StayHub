using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.Apartments.AddApartmentImage;

internal sealed class AddApartmentImageCommandHandler(
    IApartmentRepository apartmentRepository,
    IApartmentImageRepository imageRepository,
    IFileStorageService fileStorageService,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddApartmentImageCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddApartmentImageCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<Guid>(ApartmentErrors.NotFound);

        if (apartment.OwnerId != userContext.UserId &&
            !userContext.Roles.Contains(Role.Admin.Name))
            return Result.Failure<Guid>(ApartmentErrors.NotAuthorized);

        var countExistingImages = await imageRepository.CountByApartmentId(
            request.ApartmentId,
            cancellationToken);

        // TODO: TO BACKGROUND JOB
        var url = await fileStorageService.UploadAsync(
            request.FileContent,
            request.FileName,
            request.ContentType,
            cancellationToken);

        var image = ApartmentImage.Create(
            request.ApartmentId,
            new ImageUrl(url),
            countExistingImages,
            dateTimeProvider.UtcNow,
            request.IsPrimary);

        imageRepository.Add(image);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return image.Id;
        }
        catch
        {
            // Clean up orphan file in cloud/storage if database commit fails
            await fileStorageService.DeleteAsync(url, cancellationToken);
            throw;
        }
    }
}