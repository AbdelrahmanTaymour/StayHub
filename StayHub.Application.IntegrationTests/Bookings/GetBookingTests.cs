using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Bookings.GetBooking;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Bookings;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class GetBookingTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly Guid BookingId = Guid.CreateVersion7();

    [Fact]
    public async Task GetBooking_ShouldReturnFailure_WhenBookingIsNotFound()
    {
        // Arrange
        var command = new GetBookingQuery(BookingId);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnDetails_WhenCallerIsTheGuest()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnDetails_WhenCallerIsTheApartmentOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnNotFound_WhenCallerIsUnrelatedNonAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnDetails_WhenCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnApartmentAddressAndHostDetails()
    {
        // Arrange
        var owner = UserTestData.CreateUser(firstName: "Kenji", lastName: "Takahashi");
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id, name: "The Minimalist Machiya Loft", city: "Kyoto");
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApartmentId.Should().Be(apartment.Id);
        result.Value.ApartmentName.Should().Be("The Minimalist Machiya Loft");
        result.Value.Address.City.Should().Be("Kyoto");
        result.Value.Host.Id.Should().Be(owner.Id);
        result.Value.Host.FullName.Should().Be("Kenji Takahashi");
        result.Value.Host.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnNightsAndPricePerNight_DerivedFromTheBooking()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id, priceAmount: 100m, priceCurrency: "USD");
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService); // 4 nights
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Nights.Should().Be(4);
        result.Value.Currency.Should().Be("USD");
        result.Value.PriceForPeriodAmount.Should().Be(400m);
        result.Value.PricePerNight.Should().Be(100m);
        result.Value.TotalPriceAmount.Should().Be(booking.TotalPrice.Amount);
    }

    [Fact]
    public async Task GetBooking_ShouldKeepBookedPricePerNight_WhenApartmentPriceChangesLater()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id, priceAmount: 100m, priceCurrency: "USD");
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        await DbContext.Database.ExecuteSqlAsync(
            $"UPDATE apartments SET price_amount = 999 WHERE id = {apartment.Id}");

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PricePerNight.Should().Be(100m);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnUpdatedOnUtcAtLeastCreatedOnUtc()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;
        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService, baseTime);
        booking.Confirm(baseTime.AddMinutes(5));
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UpdatedOnUtc.Should().BeOnOrAfter(result.Value.CreatedOnUtc);
    }

    [Fact]
    public async Task GetBooking_ShouldReturnCanCancelTrue_WhenCallerIsTheGuest_AndBookingIsReservedAndNotStarted()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var booking = BookingTestData.Reserve(apartment, guest.Id, start, start.AddDays(5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CanCancel.Should().BeTrue();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnCanCancelFalse_WhenCallerIsTheApartmentOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var booking = BookingTestData.Reserve(apartment, guest.Id, start, start.AddDays(5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CanCancel.Should().BeFalse();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnCanCancelFalse_WhenBookingHasAlreadyStarted()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var pastStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var futureEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var booking = BookingTestData.Reserve(apartment, guest.Id, pastStart, futureEnd, PricingService);
        booking.Confirm(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CanCancel.Should().BeFalse();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnCanCancelFalse_WhenBookingIsAlreadyCancelled()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var booking = BookingTestData.Reserve(apartment, guest.Id, start, start.AddDays(5), PricingService);
        booking.Cancel(DateTime.UtcNow);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CanCancel.Should().BeFalse();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnNullConversationId_WhenNoConversationExists()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ConversationId.Should().BeNull();
    }

    [Fact]
    public async Task GetBooking_ShouldReturnConversationId_WhenConversationExistsBetweenTheParticipants()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var conversation = Conversation.Start(apartment.Id, null, guest.Id, owner.Id, DateTime.UtcNow).Value;
        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.AddRange(conversation, booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ConversationId.Should().Be(conversation.Id);
    }

    [Fact]
    public async Task GetBooking_ShouldNotReturnConversationId_WhenConversationIsAboutADifferentApartment()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment A");
        var otherApartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Apartment B");
        DbContext.AddRange(owner, guest, apartment, otherApartment);
        await DbContext.SaveChangesAsync();

        var unrelatedConversation =
            Conversation.Start(otherApartment.Id, null, guest.Id, owner.Id, DateTime.UtcNow).Value;
        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.AddRange(unrelatedConversation, booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingQuery(booking.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ConversationId.Should().BeNull();
    }
}