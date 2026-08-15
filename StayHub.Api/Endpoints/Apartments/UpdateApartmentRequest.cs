namespace StayHub.Api.Endpoints.Apartments;

public sealed record UpdateApartmentRequest(
    string Name,
    string Description,
    decimal PriceAmount,
    string PriceCurrency,
    decimal CleaningFeeAmount,
    string CleaningFeeCurrency);