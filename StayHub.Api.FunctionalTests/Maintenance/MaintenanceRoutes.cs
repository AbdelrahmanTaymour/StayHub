namespace StayHub.Api.FunctionalTests.Maintenance;

internal static class MaintenanceRoutes
{
    private const string BaseApi = "api/v1/apartments";

    public static string ByApartment(Guid apartmentId, string query = "") =>
        string.IsNullOrEmpty(query)
            ? $"{BaseApi}/{apartmentId}/maintenance-requests"
            : $"{BaseApi}/{apartmentId}/maintenance-requests?{query}";

    public static string ById(Guid requestId) => $"{BaseApi}/maintenance-requests/{requestId}";
    public static string CreateRequest(Guid apartmentId) => $"{BaseApi}/{apartmentId}/maintenance-requests";
    public static string Start(Guid requestId) => $"{BaseApi}/maintenance-requests/{requestId}/start";
    public static string Resolve(Guid requestId) => $"{BaseApi}/maintenance-requests/{requestId}/resolve";
    public static string Close(Guid requestId) => $"{BaseApi}/maintenance-requests/{requestId}/close";
}