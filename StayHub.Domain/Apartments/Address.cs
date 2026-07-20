namespace StayHub.Domain.Apartments;

public sealed record Address
{
    public string Street { get; init; }

    public string City { get; init; }

    public string State { get; init; }

    public string ZipCode { get; init; }

    public string Country { get; init; }

    public static Address Create(string street, string city, string state, string zipCode, string country)
    {
        return new Address
        {
            Street = street,
            City = city,
            State = state,
            ZipCode = zipCode,
            Country = country
        };
    }
}