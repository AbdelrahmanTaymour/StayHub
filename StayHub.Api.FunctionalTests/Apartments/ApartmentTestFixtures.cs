using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StayHub.Api.FunctionalTests.Apartments;

internal static class ApartmentTestFixtures
{
    internal static async Task<Guid> CreateApartmentAsOwnerAsync(HttpClient httpClient)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest());

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    internal static async Task<Guid> CreateApartmentAsCurrentUserAsync(HttpClient httpClient)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApartmentRoutes.BaseRoute, ApartmentTestData.ValidCreateRequest());

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
}