using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Bookings.ConfirmBooking;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class ConfirmBookingTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ConfirmBooking_ShouldPersistBookingStatusAndApartmentLastBooked_Together_WhenCallerIsOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new ConfirmBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert 
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedBooking = await DbContext.Set<Booking>().SingleAsync(b => b.Id == booking.Id);
        persistedBooking.Status.Should().Be(BookingStatus.Confirmed);

        var persistedApartment = await DbContext.Set<Apartment>().SingleAsync(a => a.Id == apartment.Id);
        persistedApartment.LastBookedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmBooking_ShouldReturnNotAuthorized_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        // The guest themselves is not authorized to confirm their own booking
        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new ConfirmBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotAuthorized);
    }

    [Fact]
    public async Task ConfirmBooking_ShouldSendConfirmationEmailToGuest_ViaOutboxPipeline()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new ConfirmBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        EmailService.SentEmails.Should().ContainSingle(e =>
            e.To.Value == guest.Email.Value &&
            e.Subject == "Booking confirmed!");
    }
}