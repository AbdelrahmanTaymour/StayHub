using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Conversations.StartConversation;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Bookings;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Bookings;
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

        var command = new StartConversationCommand(apartment.Id, null, "Hi, is this place still available?");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedConversation = await DbContext.Set<Conversation>().SingleAsync(c => c.Id == result.Value);
        persistedConversation.GuestId.Should().Be(guest.Id);
        persistedConversation.OwnerId.Should().Be(owner.Id);
        persistedConversation.BookingId.Should().BeNull();
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

        var command = new StartConversationCommand(apartment.Id, null, "Following up on my earlier question.");

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

        var command = new StartConversationCommand(apartment.Id, null, "Message to myself?");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.CannotMessageSelf);
    }

    [Fact]
    public async Task StartConversation_ShouldLinkBooking_WhenBookingBelongsToGuestAndApartment()
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

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(apartment.Id, booking.Id, "Looking forward to my stay!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedConversation = await DbContext.Set<Conversation>().SingleAsync(c => c.Id == result.Value);
        persistedConversation.BookingId.Should().Be(booking.Id);
    }

    [Fact]
    public async Task StartConversation_ShouldReturnBookingNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(apartment.Id, Guid.CreateVersion7(), "Hi!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task StartConversation_ShouldReturnBookingNotFound_WhenBookingBelongsToAnotherGuest()
    {
        // Arrange — a guest must not be able to attach someone else's booking to their conversation.
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var otherGuest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, otherGuest, apartment);
        await DbContext.SaveChangesAsync();

        var othersBooking = BookingTestData.Reserve(
            apartment, otherGuest.Id, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15), PricingService);
        DbContext.Add(othersBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(apartment.Id, othersBooking.Id, "Hi!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task StartConversation_ShouldReturnBookingNotFound_WhenBookingBelongsToADifferentApartment()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment A");
        var otherApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment B");
        DbContext.AddRange(owner, guest, apartment, otherApartment);
        await DbContext.SaveChangesAsync();

        var bookingOnOtherApartment = BookingTestData.Reserve(
            otherApartment, guest.Id, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15), PricingService);
        DbContext.Add(bookingOnOtherApartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(apartment.Id, bookingOnOtherApartment.Id, "Hi!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }
}