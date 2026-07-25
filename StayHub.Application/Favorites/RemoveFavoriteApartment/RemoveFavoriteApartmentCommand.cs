using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Favorites.RemoveFavoriteApartment;

public sealed record RemoveFavoriteApartmentCommand(Guid UserId, Guid ApartmentId) : ICommand;