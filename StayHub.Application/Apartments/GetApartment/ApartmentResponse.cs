using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartment;

public sealed record ApartmentResponse
{
    public Guid Id { get; init; }

    public Guid OwnerId { get; init; }

    public string Name { get; init; }

    public string Description { get; init; }

    public AddressResponse Address { get; set; }

    public decimal PriceAmount { get; init; }

    public string PriceCurrency { get; init; }

    public decimal CleaningFeeAmount { get; init; }

    public string CleaningFeeCurrency { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<Amenity> Amenities { get; set; } = [];

    public IReadOnlyList<ApartmentImageResponse> Images { get; set; } = [];
}

public sealed class ApartmentImageResponse
{
    public Guid Id { get; init; }

    public string Url { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsPrimary { get; init; }
}