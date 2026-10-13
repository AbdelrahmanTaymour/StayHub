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

        var command = new StartConversationCommand(
            owner.Id, apartment.Id, null, "Hi, is this place still available?");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedConversation = await DbContext.Set<Conversation>().SingleAsync(c => c.Id == result.Value);
        persistedConversation.InitiatorId.Should().Be(guest.Id);
        persistedConversation.RecipientId.Should().Be(owner.Id);
        persistedConversation.ApartmentId.Should().Be(apartment.Id);
        persistedConversation.BookingId.Should().BeNull();
        persistedConversation.LastMessageOnUtc.Should().NotBeNull();

        var persistedMessage = await DbContext.Set<Message>().SingleAsync(m => m.ConversationId == result.Value);
        persistedMessage.SenderId.Should().Be(guest.Id);
        persistedMessage.Body.Message.Should().Be("Hi, is this place still available?");
    }

    [Fact]
    public async Task StartConversation_ShouldPersistConversation_WhenNoApartmentOrBookingIsGiven()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        DbContext.AddRange(owner, guest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(owner.Id, null, null, "Are you still hosting in Lisbon?");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedConversation = await DbContext.Set<Conversation>().SingleAsync(c => c.Id == result.Value);
        persistedConversation.InitiatorId.Should().Be(guest.Id);
        persistedConversation.RecipientId.Should().Be(owner.Id);
        persistedConversation.ApartmentId.Should().BeNull();
        persistedConversation.BookingId.Should().BeNull();
    }

    [Fact]
    public async Task StartConversation_ShouldReturnRecipientNotFound_WhenRecipientDoesNotExist()
    {
        // Arrange
        var guest = UserTestData.CreateUser();
        DbContext.Add(guest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(Guid.CreateVersion7(), null, null, "Hello?");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.RecipientNotFound);
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

        var existingConversation = Conversation.Start(guest.Id, owner.Id, apartment.Id, null, DateTime.UtcNow).Value;
        DbContext.Add(existingConversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(
            owner.Id, apartment.Id, null, "Following up on my earlier question.");

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
    public async Task
        StartConversation_ShouldReuseExistingConversation_WhenMessagingSameRecipientAboutADifferentApartment()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var firstApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Riverside Loft");
        var secondApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Downtown Studio");
        DbContext.AddRange(owner, guest, firstApartment, secondApartment);
        await DbContext.SaveChangesAsync();

        var existingConversation =
            Conversation.Start(guest.Id, owner.Id, firstApartment.Id, null, DateTime.UtcNow).Value;
        DbContext.Add(existingConversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new StartConversationCommand(
            owner.Id, secondApartment.Id, null, "Is the Downtown Studio available too?");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingConversation.Id);

        DbContext.ChangeTracker.Clear();
        var persistedConversation = await DbContext.Set<Conversation>().SingleAsync(c => c.Id == result.Value);
        persistedConversation.ApartmentId.Should().Be(firstApartment.Id);

        var totalConversationsBetweenThem = await DbContext.Set<Conversation>()
            .CountAsync(c => c.InitiatorId == guest.Id && c.RecipientId == owner.Id);
        totalConversationsBetweenThem.Should().Be(1);
    }

    [Fact]
    public async Task StartConversation_ShouldReuseExistingConversation_RegardlessOfWhoInitiatedItOriginally()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        DbContext.AddRange(owner, guest);
        await DbContext.SaveChangesAsync();

        var existingConversation = Conversation.Start(guest.Id, owner.Id, null, null, DateTime.UtcNow).Value;
        DbContext.Add(existingConversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new StartConversationCommand(guest.Id, null, null, "Replying from the other side.");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingConversation.Id);
    }

    [Fact]
    public async Task StartConversation_ShouldReturnCannotMessageSelf_WhenRecipientIsTheCallerThemself()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        DbContext.Add(owner);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new StartConversationCommand(owner.Id, null, null, "Message to myself?");

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

        var command = new StartConversationCommand(owner.Id, apartment.Id, booking.Id, "Looking forward to my stay!");

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

        var command = new StartConversationCommand(owner.Id, apartment.Id, Guid.CreateVersion7(), "Hi!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task StartConversation_ShouldReturnBookingNotFound_WhenBookingBelongsToAnotherGuest()
    {
        // Arrange
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

        var command = new StartConversationCommand(owner.Id, apartment.Id, othersBooking.Id, "Hi!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task
        StartConversation_ShouldReturnBookingNotFound_WhenBookingBelongsToADifferentApartmentThanTheOneGiven()
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

        var command = new StartConversationCommand(owner.Id, apartment.Id, bookingOnOtherApartment.Id, "Hi!");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task StartConversation_ShouldDeriveApartment_WhenOnlyBookingIdIsGiven()
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

        var command = new StartConversationCommand(owner.Id, null, booking.Id, "About my upcoming stay");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedConversation = await DbContext.Set<Conversation>().SingleAsync(c => c.Id == result.Value);
        persistedConversation.ApartmentId.Should().Be(apartment.Id);
        persistedConversation.BookingId.Should().Be(booking.Id);
    }
}