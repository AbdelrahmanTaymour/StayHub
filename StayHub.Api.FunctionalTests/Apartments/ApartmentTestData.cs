using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StayHub.Api.FunctionalTests.Apartments;

internal static class ApartmentTestData
{
    internal static object ValidCreateRequest(
        string? name = null,
        string? description = null,
        string street = "123 Main St",
        string city = "Cairo",
        string state = "Cairo Governorate",
        string zipCode = "11511",
        string country = "Egypt",
        decimal priceAmount = 100,
        string priceCurrency = "USD",
        decimal cleaningFeeAmount = 25,
        string cleaningFeeCurrency = "USD")
    {
        return new
        {
            Name = name ?? $"Apartment {Guid.NewGuid():N}",
            Description = description ?? "A lovely place to stay, close to everything.",
            Street = street,
            City = city,
            State = state,
            ZipCode = zipCode,
            Country = country,
            PriceAmount = priceAmount,
            PriceCurrency = priceCurrency,
            CleaningFeeAmount = cleaningFeeAmount,
            CleaningFeeCurrency = cleaningFeeCurrency
        };
    }

    internal static object ValidUpdateRequest(
        string? name = null,
        string? description = null,
        decimal priceAmount = 150,
        string priceCurrency = "USD",
        decimal cleaningFeeAmount = 30,
        string cleaningFeeCurrency = "USD")
    {
        return new
        {
            Name = name ?? $"Updated Apartment {Guid.NewGuid():N}",
            Description = description ?? "An updated, even lovelier description.",
            PriceAmount = priceAmount,
            PriceCurrency = priceCurrency,
            CleaningFeeAmount = cleaningFeeAmount,
            CleaningFeeCurrency = cleaningFeeCurrency
        };
    }

    internal static async Task<Guid> CreateApartmentAsOwnerAsync(HttpClient httpClient)
    {
        var response = await httpClient.PostAsJsonAsync(ApartmentRoutes.BaseRoute, ValidCreateRequest());

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>
    /// Creates an apartment as the CURRENTLY AUTHENTICATED caller (that caller becomes OwnerId).
    /// Caller is responsible for AuthenticateAs(...) before calling this, and for switching
    /// auth to a guest afterward before reserving a booking.
    /// </summary>
    internal static async Task<Guid> CreateApartmentAsCurrentUserAsync(HttpClient httpClient)
    {
        var response = await httpClient.PostAsJsonAsync(ApartmentRoutes.BaseRoute, ValidCreateRequest());

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    internal static async Task<Guid> AddImageAsync(HttpClient httpClient, Guid apartmentId, bool isPrimary = false)
    {
        var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent([0xFF, 0xD8, 0xFF]);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        content.Add(fileContent, "File", "photo.jpg");
        content.Add(new StringContent(isPrimary.ToString()), "IsPrimary");

        var response = await httpClient.PostAsync(ApartmentRoutes.Images(apartmentId), content);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    internal static MultipartFormDataContent BuildImageContent(
        byte[]? bytes = null,
        string fileName = "photo.jpg",
        string contentType = "image/jpeg",
        bool isPrimary = true)
    {
        bytes ??= [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

        var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        content.Add(fileContent, "File", fileName);
        content.Add(new StringContent(isPrimary.ToString()), "IsPrimary");

        return content;
    }

    internal static object BlockRequest(int startOffsetDays = 10, int durationDays = 3, string reason = "OwnerBlocked")
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(startOffsetDays));

        var end = start.AddDays(durationDays);

        return new { Start = start, End = end, Reason = reason };
    }
}