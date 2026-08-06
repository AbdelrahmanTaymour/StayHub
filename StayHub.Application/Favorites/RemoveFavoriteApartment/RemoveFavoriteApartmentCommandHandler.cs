using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Favorites;

namespace StayHub.Application.Favorites.RemoveFavoriteApartment;

internal sealed class RemoveFavoriteApartmentCommandHandler(
    IFavoriteApartmentRepository favoriteApartmentRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveFavoriteApartmentCommand>
{
    public async Task<Result> Handle(RemoveFavoriteApartmentCommand request, CancellationToken cancellationToken)
    {
        var favorite = await favoriteApartmentRepository.GetAsync(
            userContext.UserId,
            request.ApartmentId,
            cancellationToken);

        if (favorite is null) return Result.Failure(FavoriteApartmentErrors.NotFound);

        favoriteApartmentRepository.Remove(favorite);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}