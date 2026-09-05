using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Conversations.StartConversation;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Conversations;

public class StartConversationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task StartConversation_ShouldPersistConversationAndInitialMessage_WhenNoneExistsYet()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(apartment.Id, "Hi, is this place still available?");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedConversation = await DbContext.Set<Conversation>().SingleAsync(c => c.Id == result.Value);
        persistedConversation.GuestId.Should().Be(guest.Id);
        persistedConversation.OwnerId.Should().Be(owner.Id);
        persistedConversation.LastMessageOnUtc.Should().NotBeNull();

        var persistedMessage = await DbContext.Set<Message>().SingleAsync(m => m.ConversationId == result.Value);
        persistedMessage.SenderId.Should().Be(guest.Id);
        persistedMessage.Body.Message.Should().Be("Hi, is this place still available?");
    }

    [Fact]
    public async Task StartConversation_ShouldReuseExistingConversation_WhenOneAlreadyExistsForTheseParticipants()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var existingConversation = Conversation.Start(apartment.Id, null, guest.Id, owner.Id, DateTime.UtcNow).Value;
        DbContext.Add(existingConversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(apartment.Id, "Following up on my earlier question.");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingConversation.Id);

        DbContext.ChangeTracker.Clear();
        var conversationCount = await DbContext.Set<Conversation>().CountAsync(c => c.Id == existingConversation.Id);
        conversationCount.Should().Be(1);
    }

    [Fact]
    public async Task StartConversation_ShouldReturnCannotMessageSelf_WhenGuestIsTheApartmentOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new StartConversationCommand(apartment.Id, "Message to myself?");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.CannotMessageSelf);
    }
}