using FluentAssertions;
using StayHub.Application.Conversations.GetConversationMessages;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Conversations;

public class GetConversationMessagesTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetMessagesByConversation_ShouldReturnEmpty_WhenCallerIsNotAParticipant()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var conversation = Conversation.Start(apartment.Id, null, guest.Id, owner.Id, DateTime.UtcNow).Value;
        var message = Message.Send(conversation.Id, guest.Id, new MessageBody("Hello!"), DateTime.UtcNow);
        DbContext.AddRange(conversation, message);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetConversationMessagesQuery(conversation.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessagesByConversation_ShouldReturnMessages_OrderedBySentOnDescending_ForParticipant()
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

        var baseTime = DateTime.UtcNow;
        var firstMessage = Message.Send(conversation.Id, guest.Id, new MessageBody("First message"), baseTime);
        var secondMessage = Message.Send(conversation.Id, owner.Id, new MessageBody("Second message"),
            baseTime.AddMinutes(1));
        DbContext.AddRange(firstMessage, secondMessage);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetConversationMessagesQuery(conversation.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(secondMessage.Id);
        result.Value[1].Id.Should().Be(firstMessage.Id);
    }

    [Fact]
    public async Task GetMessagesByConversation_ShouldReturnMessages_WhenConversationHasALinkedBooking()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var conversation = Conversation.Start(apartment.Id, booking.Id, guest.Id, owner.Id, DateTime.UtcNow).Value;
        var message = Message.Send(conversation.Id, guest.Id, new MessageBody("Looking forward to it!"),
            DateTime.UtcNow);
        DbContext.AddRange(conversation, message);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetConversationMessagesQuery(conversation.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(m => m.Id == message.Id);
    }
}