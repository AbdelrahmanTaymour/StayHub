namespace StayHub.Api.Endpoints.Conversations;

public sealed record StartConversationRequest(
    Guid RecipientId,
    Guid? ApartmentId,
    Guid? BookingId,
    string InitialMessage);