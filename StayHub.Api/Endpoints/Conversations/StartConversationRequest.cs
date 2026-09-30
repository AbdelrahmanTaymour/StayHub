namespace StayHub.Api.Endpoints.Conversations;

public sealed record StartConversationRequest(Guid ApartmentId, Guid? BookingId, string InitialMessage);