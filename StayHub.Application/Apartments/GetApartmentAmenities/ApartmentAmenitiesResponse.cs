namespace StayHub.Application.Apartments.GetApartmentAmenities;

public sealed record ApartmentAmenitiesResponse
{
    public IReadOnlyList<string> Amenities { get; init; } = [];
}