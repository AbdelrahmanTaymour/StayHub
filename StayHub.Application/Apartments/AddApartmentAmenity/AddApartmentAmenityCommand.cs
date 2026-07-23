using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.AddApartmentAmenity;

public sealed record AddApartmentAmenityCommand(
    Guid ApartmentId,
    Amenity Amenity,
    Guid RequestedByUserId) : ICommand;