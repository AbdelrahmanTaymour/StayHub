using System.Net.Http.Json;
using StayHub.Api.FunctionalTests.Apartments;

namespace StayHub.Api.FunctionalTests.Maintenance;

internal static class MaintenanceTestFixtures
{
    /// <summary>Creates an apartment and an Open maintenance request on it, as the current caller (owner).</summary>
    internal static async Task<(Guid ApartmentId, Guid RequestId)> CreateOpenRequestAsOwnerAsync(HttpClient httpClient)
    {
        var apartmentId = await ApartmentTestFixtures.CreateApartmentAsCurrentUserAsync(httpClient);

        var createResponse = await httpClient.PostAsJsonAsync(
            MaintenanceRoutes.CreateRequest(apartmentId), MaintenanceTestData.ValidCreateRequest());
        createResponse.EnsureSuccessStatusCode();
        var requestId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        return (apartmentId, requestId);
    }
}