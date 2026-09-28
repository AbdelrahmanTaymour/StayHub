using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.GetApartmentAmenities;

public sealed record GetApartmentAmenitiesQuery(Guid ApartmentId) : IQuery<ApartmentAmenitiesResponse>;