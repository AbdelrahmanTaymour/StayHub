namespace StayHub.Api.FunctionalTests.Payments;

internal static class PaymentRoutes
{
    public const string BaseRoute = "api/v1/payments";
    public const string Webhook = $"{BaseRoute}/webhook";

    public static string ByBooking(Guid bookingId) => $"{BaseRoute}/by-booking/{bookingId}";
    public static string Refund(Guid paymentId) => $"{BaseRoute}/{paymentId}/refund";
}