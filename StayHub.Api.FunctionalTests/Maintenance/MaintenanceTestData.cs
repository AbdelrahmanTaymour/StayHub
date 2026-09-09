using System.Net.Http.Json;
using StayHub.Api.FunctionalTests.Apartments;

namespace StayHub.Api.FunctionalTests.Maintenance;

internal static class MaintenanceTestData
{
    internal static object ValidCreateRequest(string? title = null, string? description = null)
    {
        return new
        {
            Title = title ?? $"Leaky faucet {Guid.NewGuid():N}",
            Description = description ?? "The kitchen faucet has been dripping constantly for two days."
        };
    }

    /// <summary>Creates an apartment and an Open maintenance request on it, as the current caller (owner).</summary>
    internal static async Task<(Guid ApartmentId, Guid RequestId)> CreateOpenRequestAsOwnerAsync(HttpClient httpClient)
    {
        var apartmentId = await ApartmentTestData.CreateApartmentAsCurrentUserAsync(httpClient);

        var createResponse = await httpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), ValidCreateRequest());
        createResponse.EnsureSuccessStatusCode();
        var requestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        return (apartmentId, requestId);
    }
}