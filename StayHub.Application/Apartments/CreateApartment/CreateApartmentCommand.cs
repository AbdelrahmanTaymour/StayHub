using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.CreateApartment;

public sealed record CreateApartmentCommand(
    Guid OwnerId,
    string Name,
    string Description,
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    decimal PriceAmount,
    string PriceCurrency,
    decimal CleaningFeeAmount,
    string CleaningFeeCurrency) : ICommand<Guid>;