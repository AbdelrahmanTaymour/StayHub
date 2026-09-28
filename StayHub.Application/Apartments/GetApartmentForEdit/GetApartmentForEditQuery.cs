using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.GetApartmentForEdit;

public sealed record GetApartmentForEditQuery(Guid ApartmentId) : IQuery<ApartmentForEditResponse>;