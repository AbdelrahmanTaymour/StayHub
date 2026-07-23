using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.RemoveApartmentAmenity;

public sealed record RemoveApartmentAmenityCommand(
    Guid ApartmentId,
    Amenity Amenity,
    Guid RequestedByUserId) : ICommand;