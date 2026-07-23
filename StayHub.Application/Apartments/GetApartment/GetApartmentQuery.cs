using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.GetApartment;

public sealed record GetApartmentQuery(Guid ApartmentId) : IQuery<ApartmentResponse>;