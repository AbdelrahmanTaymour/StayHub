using FluentAssertions;
using StayHub.Domain.Conversations;
using StayHub.Domain.Conversations.Events;
using StayHub.Domain.UnitTests.Infrastructure;

namespace StayHub.Domain.UnitTests.Conversations;

public class MessageTests : BaseTest
{
    [Fact]
    public void Send_Should_SetPropertyValues()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        var senderId = Guid.CreateVersion7();
        var body = new MessageBody("Is the apartment pet-friendly?");
        var sentOnUtc = DateTime.UtcNow;

        // Act
        var message = Message.Send(conversationId, senderId, body, sentOnUtc);

        // Assert
        message.ConversationId.Should().Be(conversationId);
        message.SenderId.Should().Be(senderId);
        message.Body.Should().Be(body);
        message.SentOnUtc.Should().Be(sentOnUtc);
        message.ReadOnUtc.Should().BeNull();
    }

    [Fact]
    public void Send_Should_RaiseMessageSentDomainEvent()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        var senderId = Guid.CreateVersion7();

        // Act
        var message = Message.Send(conversationId, senderId, new MessageBody("Hello"), DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<MessageSentDomainEvent>(message);
        domainEvent.MessageId.Should().Be(message.Id);
        domainEvent.ConversationId.Should().Be(conversationId);
        domainEvent.SenderId.Should().Be(senderId);
    }

    [Fact]
    public void MarkAsRead_Should_SetReadOnUtcAndReturnSuccess_WhenNotAlreadyRead()
    {
        // Arrange
        var message = Message.Send(Guid.CreateVersion7(), Guid.CreateVersion7(), new MessageBody("Hi"),
            DateTime.UtcNow);
        var readOnUtc = DateTime.UtcNow;

        // Act
        var result = message.MarkAsRead(readOnUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        message.ReadOnUtc.Should().Be(readOnUtc);
    }

    [Fact]
    public void MarkAsRead_Should_ReturnFailure_WhenAlreadyRead()
    {
        // Arrange
        var message = Message.Send(Guid.CreateVersion7(), Guid.CreateVersion7(), new MessageBody("Hi"),
            DateTime.UtcNow);
        message.MarkAsRead(DateTime.UtcNow);

        // Act
        var result = message.MarkAsRead(DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MessageErrors.AlreadyRead);
    }

    [Fact]
    public void MarkAsRead_Should_NotRaiseAnyDomainEvent()
    {
        // Arrange — only Send raises a notification-worthy event; being read
        // isn't something else in the system currently reacts to.
        var message = Message.Send(Guid.CreateVersion7(), Guid.CreateVersion7(), new MessageBody("Hi"),
            DateTime.UtcNow);
        message.ClearDomainEvents();

        // Act
        message.MarkAsRead(DateTime.UtcNow);

        // Assert
        message.GetDomainEvents().Should().BeEmpty();
    }
}