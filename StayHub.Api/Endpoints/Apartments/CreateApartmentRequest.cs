namespace StayHub.Api.Endpoints.Apartments;

public sealed record CreateApartmentRequest(
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
    string CleaningFeeCurrency);