using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Favorites.AddFavoriteApartment;

public sealed record AddFavoriteApartmentCommand(Guid ApartmentId) : ICommand;