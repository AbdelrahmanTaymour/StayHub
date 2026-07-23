using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.AddApartmentImage;

public sealed record AddApartmentImageCommand(
    Guid ApartmentId,
    Guid RequestedByUserId,
    Stream FileContent,
    string FileName,
    string ContentType,
    bool IsPrimary) : ICommand<Guid>;