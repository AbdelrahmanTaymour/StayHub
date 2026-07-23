using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.UpdateApartment;

public sealed record UpdateApartmentCommand(
    Guid ApartmentId,
    Guid RequestedByUserId,
    string Name,
    string Description,
    decimal PriceAmount,
    string PriceCurrency,
    decimal CleaningFeeAmount,
    string CleaningFeeCurrency) : ICommand;