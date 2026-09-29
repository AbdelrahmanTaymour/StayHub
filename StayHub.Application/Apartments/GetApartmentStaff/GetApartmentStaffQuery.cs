using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.GetApartmentStaff;

public sealed record GetApartmentStaffQuery(Guid ApartmentId) : IQuery<IReadOnlyList<ApartmentStaffResponse>>;