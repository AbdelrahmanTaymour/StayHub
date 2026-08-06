using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Favorites;

namespace StayHub.Application.Favorites.AddFavoriteApartment;

internal sealed class AddFavoriteApartmentCommandHandler(
    IApartmentRepository apartmentRepository,
    IFavoriteApartmentRepository favoriteApartmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddFavoriteApartmentCommand>
{
    public async Task<Result> Handle(AddFavoriteApartmentCommand request, CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure(ApartmentErrors.NotFound);

        var existing = await favoriteApartmentRepository.GetAsync(
            userContext.UserId,
            request.ApartmentId,
            cancellationToken);

        if (existing is not null) return Result.Failure(FavoriteApartmentErrors.AlreadyFavorited);

        var favorite = FavoriteApartment.Create(userContext.UserId, request.ApartmentId, dateTimeProvider.UtcNow);

        favoriteApartmentRepository.Add(favorite);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}