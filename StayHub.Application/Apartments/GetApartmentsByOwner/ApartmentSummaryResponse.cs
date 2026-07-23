namespace StayHub.Application.Apartments.GetApartmentsByOwner;

public sealed class ApartmentSummaryResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; }

    public string City { get; init; }

    public decimal Price { get; init; }

    public string Currency { get; init; }

    public string? PrimaryImageUrl { get; init; }
}