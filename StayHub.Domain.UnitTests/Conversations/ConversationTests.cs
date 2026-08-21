using FluentAssertions;
using StayHub.Domain.Conversations;
using StayHub.Domain.Conversations.Events;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Conversations;

public class ConversationTests : BaseTest
{
    [Fact]
    public void Start_Should_ReturnSuccessAndSetPropertyValues_WhenGuestAndOwnerDiffer()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();
        var guestId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();

        // Act
        var result = Conversation.Start(apartmentId, bookingId, guestId, ownerId, DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApartmentId.Should().Be(apartmentId);
        result.Value.BookingId.Should().Be(bookingId);
        result.Value.GuestId.Should().Be(guestId);
        result.Value.OwnerId.Should().Be(ownerId);
        result.Value.LastMessageOnUtc.Should().BeNull();
    }

    [Fact]
    public void Start_Should_AllowNullBookingId()
    {
        // Arrange — a guest can message a host about an apartment before
        // ever booking it (a pre-booking inquiry), so BookingId is nullable
        // by design, not an oversight.
        // Act
        var conversation = ConversationData.Start(bookingId: null);

        // Assert
        conversation.BookingId.Should().BeNull();
    }

    [Fact]
    public void Start_Should_RaiseConversationStartedDomainEvent_WhenGuestAndOwnerDiffer()
    {
        // Act
        var conversation = ConversationData.Start();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ConversationStartedDomainEvent>(conversation);
        domainEvent.ConversationId.Should().Be(conversation.Id);
    }

    [Fact]
    public void Start_Should_ReturnFailure_WhenGuestIdEqualsOwnerId()
    {
        // Arrange
        var sameUserId = Guid.CreateVersion7();

        // Act
        var result = Conversation.Start(
            Guid.CreateVersion7(),
            bookingId: null,
            guestId: sameUserId,
            ownerId: sameUserId,
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.CannotMessageSelf);
    }

    [Fact]
    public void Start_Should_NotRaiseDomainEvent_WhenGuestAndOwnerAreSame()
    {
        // Arrange
        var sameUserId = Guid.CreateVersion7();

        // Act
        var result = Conversation.Start(
            Guid.CreateVersion7(),
            null,
            sameUserId,
            sameUserId,
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RegisterMessage_Should_SetLastMessageOnUtc()
    {
        // Arrange
        var conversation = ConversationData.Start();
        var sentOnUtc = DateTime.UtcNow;

        // Act
        conversation.RegisterMessage(sentOnUtc);

        // Assert
        conversation.LastMessageOnUtc.Should().Be(sentOnUtc);
    }

    [Fact]
    public void RegisterMessage_Should_NotRaiseAnyDomainEvent()
    {
        // Arrange — tracking the latest message timestamp is bookkeeping;
        // the MessageSentDomainEvent raised by Message.Send is what actually
        // notifies the system a message went out.
        var conversation = ConversationData.Start();
        conversation.ClearDomainEvents();

        // Act
        conversation.RegisterMessage(DateTime.UtcNow);

        // Assert
        conversation.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void RegisterMessage_Should_OverwritePreviousValue_WhenCalledAgain()
    {
        // Arrange
        var conversation = ConversationData.Start();
        conversation.RegisterMessage(DateTime.UtcNow.AddMinutes(-5));
        var latestMessageTime = DateTime.UtcNow;

        // Act
        conversation.RegisterMessage(latestMessageTime);

        // Assert
        conversation.LastMessageOnUtc.Should().Be(latestMessageTime);
    }
}