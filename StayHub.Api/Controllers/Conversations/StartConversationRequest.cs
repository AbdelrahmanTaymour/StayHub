namespace StayHub.Api.Controllers.Conversations;

public sealed record StartConversationRequest(Guid ApartmentId, string InitialMessage);