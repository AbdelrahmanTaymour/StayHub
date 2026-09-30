using FluentAssertions;
using StayHub.Application.Bookings.GetConversationBookingDetails;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Bookings;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class GetConversationBookingDetailsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetByConversation_ShouldReturnNotFound_WhenConversationDoesNotExist()
    {
        // Arrange
        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        var query = new GetConversationBookingDetailsQuery(Guid.CreateVersion7());

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnNotFound_WhenConversationHasNoLinkedBooking()
    {
        // Arrange — an inquiry-only conversation, started before any booking exists.
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var conversation = Conversation.Start(apartment.Id, null, guest.Id, owner.Id, DateTime.UtcNow).Value;
        DbContext.Add(conversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var query = new GetConversationBookingDetailsQuery(conversation.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnNotFound_WhenCallerIsNotAParticipant()
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
        DbContext.Add(conversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        var query = new GetConversationBookingDetailsQuery(conversation.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnFullDetails_WhenCallerIsTheGuest()
    {
        // Arrange
        var owner = UserTestData.CreateUser(firstName: "Kenji", lastName: "Takahashi");
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id, name: "The Minimalist Machiya Loft", city: "Kyoto");
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 18), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        var conversation = Conversation.Start(apartment.Id, booking.Id, guest.Id, owner.Id, DateTime.UtcNow).Value;
        DbContext.Add(conversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var query = new GetConversationBookingDetailsQuery(conversation.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BookingId.Should().Be(booking.Id);
        result.Value.ApartmentId.Should().Be(apartment.Id);
        result.Value.ApartmentName.Should().Be("The Minimalist Machiya Loft");
        result.Value.CheckIn.Should().Be(new DateOnly(2026, 10, 12));
        result.Value.CheckOut.Should().Be(new DateOnly(2026, 10, 18));
        result.Value.Nights.Should().Be(6);
        result.Value.HostName.Should().Be("Kenji Takahashi");
        result.Value.Status.Should().Be(booking.Status);
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnFullDetails_WhenCallerIsTheOwner()
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
        DbContext.Add(conversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var query = new GetConversationBookingDetailsQuery(conversation.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BookingId.Should().Be(booking.Id);
    }

    [Fact]
    public async Task GetByConversation_ShouldReturnNullHostAvatarAndPhone_WhenHostHasNoProfile()
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
        DbContext.Add(conversation);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var query = new GetConversationBookingDetailsQuery(conversation.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HostAvatarUrl.Should().BeNull();
        result.Value.HostPhoneNumber.Should().BeNull();
    }
}