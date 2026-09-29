using System.Web;

namespace StayHub.Api.FunctionalTests.Apartments;

internal static class ApartmentRoutes
{
    public const string BaseRoute = "api/v1/apartments";

    public static string ById(Guid id) => $"{BaseRoute}/{id}";
    public static string Search(string query = "") => string.IsNullOrEmpty(query) ? BaseRoute : $"{BaseRoute}?{query}";

    public static string ByOwner(Guid ownerId, string query = "") =>
        string.IsNullOrEmpty(query) ? $"{BaseRoute}/by-owner/{ownerId}" : $"{BaseRoute}/by-owner/{ownerId}?{query}";

    public static string Mine(string query = "") =>
        string.IsNullOrEmpty(query)
            ? $"{BaseRoute}/mine"
            : $"{BaseRoute}/mine?{query}";

    public static string ForEdit(Guid apartmentId) => $"{BaseRoute}/{apartmentId}/edit";

    public static string MyDashboard(string? query = null) =>
        $"{BaseRoute}/mine/dashboard" + (query is null ? "" : $"?{query}");

    public static string Activate(Guid id) => $"{BaseRoute}/{id}/activate";
    public static string Deactivate(Guid id) => $"{BaseRoute}/{id}/deactivate";
    public static string Amenities(Guid id) => $"{BaseRoute}/{id}/amenities";
    public static string AddImages(Guid apartmentId) => $"{BaseRoute}/{apartmentId}/images";
    public static string ApartmentImages(Guid apartmentId) => $"{BaseRoute}/{apartmentId}/images";
    public static string ImageById(Guid imageId) => $"{BaseRoute}/images/{imageId}";
    public static string ImagesOrder(Guid id) => $"{BaseRoute}/{id}/images/order";
    public static string ImageAsPrimary(Guid id, Guid imageId) => $"{BaseRoute}/{id}/images/{imageId}/primary";
    public static string AvailabilityBlocks(Guid id) => $"{BaseRoute}/{id}/availability-blocks";

    public static string AvailabilityBlocks(Guid id, int year, int month) =>
        $"{BaseRoute}/{id}/availability-blocks?year={year}&month={month}";

    public static string AvailabilityBlockById(Guid blockId) => $"{BaseRoute}/availability-blocks/{blockId}";

    public static string StaffSearch(Guid apartmentId, string email) =>
        $"{BaseRoute}/{apartmentId}/staff/search?email={HttpUtility.UrlEncode(email)}";

    public static string Staff(Guid apartmentId) => $"{BaseRoute}/{apartmentId}/staff";
    public static string StaffById(Guid assignmentId) => $"{BaseRoute}/staff/{assignmentId}";

    public static string Pricing(Guid apartmentId, string? query = null) =>
        $"{BaseRoute}/{apartmentId}/pricing" + (query is null ? "" : $"?{query}");
}