namespace StayHub.Api.FunctionalTests.Apartments;

internal static class ApartmentRoutes
{
    public const string BaseRoute = "api/v1/apartments";

    public static string ById(Guid id) => $"{BaseRoute}/{id}";
    public static string Search(string query = "") => string.IsNullOrEmpty(query) ? BaseRoute : $"{BaseRoute}?{query}";

    public static string ByOwner(Guid ownerId, string query = "") =>
        string.IsNullOrEmpty(query) ? $"{BaseRoute}/by-owner/{ownerId}" : $"{BaseRoute}/by-owner/{ownerId}?{query}";

    public static string Activate(Guid id) => $"{BaseRoute}/{id}/activate";
    public static string Deactivate(Guid id) => $"{BaseRoute}/{id}/deactivate";
    public static string Amenities(Guid id) => $"{BaseRoute}/{id}/amenities";
    public static string Images(Guid id) => $"{BaseRoute}/{id}/images";
    public static string ImageById(Guid imageId) => $"{BaseRoute}/images/{imageId}";
    public static string ImagesOrder(Guid id) => $"{BaseRoute}/{id}/images/order";
    public static string AvailabilityBlocks(Guid id) => $"{BaseRoute}/{id}/availability-blocks";
    public static string AvailabilityBlockById(Guid blockId) => $"{BaseRoute}/availability-blocks/{blockId}";
    public static string Staff(Guid id) => $"{BaseRoute}/{id}/staff";
    public static string StaffById(Guid assignmentId) => $"{BaseRoute}/staff/{assignmentId}";
}