namespace StayHub.Application.Apartments.SearchApartments;

public sealed record ApartmentSearchResult
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string City { get; init; }
    public string Country { get; init; }
    public decimal PricePerNight { get; init; }
    public decimal? TotalPrice { get; init; }
    public string Currency { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
}