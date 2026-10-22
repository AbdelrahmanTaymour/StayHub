using FluentAssertions;
using StayHub.Domain.Conversations;
using StayHub.Domain.Conversations.Events;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Conversations;

public class ConversationTests : BaseTest
{
    [Fact]
    public void Start_Should_ReturnSuccessAndSetPropertyValues_WhenInitiatorAndRecipientDiffer()
    {
        // Arrange
        var initiatorId = Guid.CreateVersion7();
        var recipientId = Guid.CreateVersion7();
        var apartmentId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();

        // Act
        var result = Conversation.Start(initiatorId, recipientId, apartmentId, bookingId, DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.InitiatorId.Should().Be(initiatorId);
        result.Value.RecipientId.Should().Be(recipientId);
        result.Value.ApartmentId.Should().Be(apartmentId);
        result.Value.BookingId.Should().Be(bookingId);
        result.Value.LastMessageOnUtc.Should().BeNull();
    }

    [Fact]
    public void Start_Should_AllowNullBookingId()
    {
        // Act
        var conversation = ConversationData.Start(bookingId: null);

        // Assert
        conversation.BookingId.Should().BeNull();
    }

    [Fact]
    public void Start_Should_AllowNullApartmentId()
    {
        // Arrange — the core new capability: a conversation with no
        // apartment/booking context at all, e.g. messaging an owner
        // straight from their profile page.
        var initiatorId = Guid.CreateVersion7();
        var recipientId = Guid.CreateVersion7();

        // Act
        var result = Conversation.Start(initiatorId, recipientId, apartmentId: null, bookingId: null, DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApartmentId.Should().BeNull();
        result.Value.BookingId.Should().BeNull();
    }

    [Fact]
    public void Start_Should_RaiseConversationStartedDomainEvent_WhenInitiatorAndRecipientDiffer()
    {
        // Act
        var conversation = ConversationData.Start();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<ConversationStartedDomainEvent>(conversation);
        domainEvent.ConversationId.Should().Be(conversation.Id);
    }

    [Fact]
    public void Start_Should_ReturnFailure_WhenInitiatorIdEqualsRecipientId()
    {
        // Arrange
        var sameUserId = Guid.CreateVersion7();

        // Act
        var result = Conversation.Start(
            initiatorId: sameUserId,
            recipientId: sameUserId,
            apartmentId: Guid.CreateVersion7(),
            bookingId: null,
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.CannotMessageSelf);
    }

    [Fact]
    public void Start_Should_NotRaiseDomainEvent_WhenInitiatorAndRecipientAreSame()
    {
        // Arrange
        var sameUserId = Guid.CreateVersion7();

        // Act
        var result = Conversation.Start(
            sameUserId,
            sameUserId,
            Guid.CreateVersion7(),
            null,
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
        // Arrange
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

    [Fact]
    public void AttachBooking_Should_SetBookingId_WhenNoneWasLinkedYet()
    {
        // Arrange
        var conversation = ConversationData.Start(bookingId: null);
        var bookingId = Guid.CreateVersion7();

        // Act
        conversation.AttachBooking(bookingId);

        // Assert
        conversation.BookingId.Should().Be(bookingId);
    }

    [Fact]
    public void AttachBooking_Should_NotOverwriteExistingBookingId()
    {
        // Arrange
        var originalBookingId = Guid.CreateVersion7();
        var conversation = ConversationData.Start(bookingId: originalBookingId);
        var otherBookingId = Guid.CreateVersion7();

        // Act
        conversation.AttachBooking(otherBookingId);

        // Assert
        conversation.BookingId.Should().Be(originalBookingId);
    }

    [Fact]
    public void AttachBooking_Should_NotRaiseAnyDomainEvent()
    {
        // Arrange
        var conversation = ConversationData.Start(bookingId: null);
        conversation.ClearDomainEvents();

        // Act
        conversation.AttachBooking(Guid.CreateVersion7());

        // Assert
        conversation.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void AttachApartment_Should_SetApartmentId_WhenNoneWasLinkedYet()
    {
        // Arrange — the scenario that unblocks this whole refactor: a
        // conversation started with no apartment (e.g. from an owner
        // profile) later gets one attached once a specific listing comes
        // into the picture.
        var conversation = ConversationData.Start(apartmentId: null);
        var apartmentId = Guid.CreateVersion7();

        // Act
        conversation.AttachApartment(apartmentId);

        // Assert
        conversation.ApartmentId.Should().Be(apartmentId);
    }

    [Fact]
    public void AttachApartment_Should_NotOverwriteExistingApartmentId()
    {
        // Arrange
        var originalApartmentId = Guid.CreateVersion7();
        var conversation = ConversationData.Start(apartmentId: originalApartmentId);
        var otherApartmentId = Guid.CreateVersion7();

        // Act
        conversation.AttachApartment(otherApartmentId);

        // Assert
        conversation.ApartmentId.Should().Be(originalApartmentId);
    }

    [Fact]
    public void AttachApartment_Should_NotRaiseAnyDomainEvent()
    {
        // Arrange
        var conversation = ConversationData.Start(apartmentId: null);
        conversation.ClearDomainEvents();

        // Act
        conversation.AttachApartment(Guid.CreateVersion7());

        // Assert
        conversation.GetDomainEvents().Should().BeEmpty();
    }
}