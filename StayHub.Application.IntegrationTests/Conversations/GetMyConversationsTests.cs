using FluentAssertions;
using StayHub.Application.Conversations.GetMyConversations;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Conversations;

public class GetMyConversationsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetMyConversations_ShouldReturnEmpty_WhenUserHasNoConversations()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyConversationsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyConversations_ShouldReturnCorrectUnreadCount_ExcludingOwnMessages()
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

        // Two unread messages FROM the guest (should count toward the
        // owner's unread total) and one message the owner already sent
        // themselves (should NOT count toward their own unread total).
        var guestMessage1 = Message.Send(conversation.Id, guest.Id, new MessageBody("Hi, is this available?"),
            DateTime.UtcNow);
        var guestMessage2 = Message.Send(conversation.Id, guest.Id, new MessageBody("Also, what's the checkout time?"),
            DateTime.UtcNow);
        var ownerMessage = Message.Send(conversation.Id, owner.Id, new MessageBody("Sure, checkout is at 11am."),
            DateTime.UtcNow);
        DbContext.AddRange(guestMessage1, guestMessage2, ownerMessage);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyConversationsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Id.Should().Be(conversation.Id);
        result.Value[0].UnreadCount.Should().Be(2);
    }

    [Fact]
    public async Task GetMyConversations_ShouldOrderByLastMessageOnDescending_WithNullsLast()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var firstGuest = UserTestData.CreateUser();
        var secondGuest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, firstGuest, secondGuest, apartment);
        await DbContext.SaveChangesAsync();

        // Conversation with no messages yet — LastMessageOnUtc stays null.
        var conversationWithNoMessages =
            Conversation.Start(apartment.Id, null, firstGuest.Id, owner.Id, DateTime.UtcNow).Value;

        var conversationWithMessage =
            Conversation.Start(apartment.Id, null, secondGuest.Id, owner.Id, DateTime.UtcNow).Value;
        conversationWithMessage.RegisterMessage(DateTime.UtcNow);

        DbContext.AddRange(conversationWithNoMessages, conversationWithMessage);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyConversationsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(conversationWithMessage.Id);
        result.Value[1].Id.Should().Be(conversationWithNoMessages.Id);
    }
}