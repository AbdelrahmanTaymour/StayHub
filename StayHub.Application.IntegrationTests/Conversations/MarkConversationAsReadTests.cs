using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Conversations.MarkConversationAsRead;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Conversations;

public class MarkConversationAsReadTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task MarkConversationAsRead_ShouldMarkOnlyMessagesFromTheOtherParticipantAsRead()
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

        var messageFromGuest = Message.Send(conversation.Id, guest.Id, new MessageBody("Question from guest"),
            DateTime.UtcNow);
        var messageFromOwnerAlready = Message.Send(conversation.Id, owner.Id, new MessageBody("Owner's own message"),
            DateTime.UtcNow);
        DbContext.AddRange(messageFromGuest, messageFromOwnerAlready);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new MarkConversationAsReadCommand(conversation.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedGuestMessage = await DbContext.Set<Message>().SingleAsync(m => m.Id == messageFromGuest.Id);
        persistedGuestMessage.ReadOnUtc.Should().NotBeNull();

        // The owner's own message should never be touched by marking their
        // OWN inbox as read — GetUnreadForRecipientAsync must exclude
        // messages the caller sent themselves.
        var persistedOwnerMessage = await DbContext.Set<Message>().SingleAsync(m => m.Id == messageFromOwnerAlready.Id);
        persistedOwnerMessage.ReadOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task MarkConversationAsRead_ShouldReturnNotAuthorized_WhenCallerIsNotAParticipant()
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

        var command = new MarkConversationAsReadCommand(conversation.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.NotAuthorized);
    }
}