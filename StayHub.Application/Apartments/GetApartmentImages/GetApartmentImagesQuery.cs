using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.GetApartmentImages;

public sealed record GetApartmentImagesQuery(Guid ApartmentId) : IQuery<ApartmentImagesResponse>;