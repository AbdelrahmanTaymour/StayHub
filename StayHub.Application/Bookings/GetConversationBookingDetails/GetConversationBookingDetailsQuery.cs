using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Bookings.GetConversationBookingDetails;

public record GetConversationBookingDetailsQuery(Guid ConversationId)
    : IQuery<ConversationBookingDetailsResponse>;