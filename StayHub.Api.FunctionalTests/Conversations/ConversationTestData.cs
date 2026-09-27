namespace StayHub.Api.FunctionalTests.Conversations;

internal static class ConversationTestData
{
    internal static object ValidStartRequest(Guid apartmentId, Guid? bookingId = null, string? initialMessage = null)
    {
        return new
        {
            ApartmentId = apartmentId,
            BookingId = bookingId,
            InitialMessage = initialMessage ?? "Hi, is this apartment available next month?"
        };
    }

    internal static object ValidSendMessageRequest(string? body = null)
    {
        return new { Body = body ?? "Just following up on my earlier question." };
    }
}