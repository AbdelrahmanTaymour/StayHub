namespace StayHub.Api.Endpoints.Conversations;

public sealed record StartConversationRequest(Guid ApartmentId, string InitialMessage);