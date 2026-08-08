namespace StayHub.Api.Controllers.Apartments;

public sealed record UpdateApartmentRequest(
    string Name,
    string Description,
    decimal PriceAmount,
    string PriceCurrency,
    decimal CleaningFeeAmount,
    string CleaningFeeCurrency);