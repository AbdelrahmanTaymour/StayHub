namespace StayHub.Application.Apartments.GetApartmentPricing;

public sealed record ApartmentPricingResponse
{
    public bool IsAvailable { get; init; }
    public int Nights { get; init; }
    public decimal PricePerNight { get; init; }
    public decimal SubtotalForStay { get; init; }
    public decimal CleaningFee { get; init; }
    public decimal AmenitiesUpcharge { get; init; }
    public decimal TotalPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
}