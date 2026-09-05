using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Conversations.SendMessage;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Conversations;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Conversations;

public class SendMessageTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task SendMessage_ShouldPersistMessageAndCreateNotificationForRecipient_ViaOutboxPipeline()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var conversation = Conversation.Start(apartment.Id, null, guest.Id, owner.Id, DateTime.UtcNow).Value;
        DbContext.Add(conversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new SendMessageCommand(conversation.Id, "What's the wifi password?");

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        DbContext.ChangeTracker.Clear();
        var persistedMessage = await DbContext.Set<Message>().SingleAsync(m => m.Id == result.Value);
        persistedMessage.SenderId.Should().Be(guest.Id);

        var notification = await DbContext.Set<Notification>().SingleAsync(n => n.UserId == owner.Id);
        notification.Type.Should().Be(NotificationType.NewMessage);
        notification.Payload.Should().Contain(conversation.Id.ToString());
        notification.Payload.Should().Contain(result.Value.ToString());
    }

    [Fact]
    public async Task SendMessage_ShouldReturnNotAuthorized_WhenSenderIsNotAConversationParticipant()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var conversation = Conversation.Start(apartment.Id, null, guest.Id, owner.Id, DateTime.UtcNow).Value;
        DbContext.Add(conversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        var command = new SendMessageCommand(conversation.Id, "I shouldn't be able to send this.");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MessageErrors.NotAuthorized);
    }
}