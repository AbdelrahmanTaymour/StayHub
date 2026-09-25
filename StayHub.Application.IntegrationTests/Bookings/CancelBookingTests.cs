using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Bookings.CancelBooking;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class CancelBookingTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CancelBooking_ShouldPersistCancelledStatusAndRemoveAvailabilityBlock_WhenCallerIsGuest()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.ReserveAndConfirm(
            apartment,
            guest.Id,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 5),
            PricingService);

        DbContext.Add(booking);

        var availabilityBlock = ApartmentAvailabilityBlock.Create(
            apartment.Id,
            booking.Duration.Start,
            booking.Duration.End,
            ApartmentUnavailabilityReason.Booked,
            DateTime.UtcNow);

        DbContext.Add(availabilityBlock);

        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new CancelBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();

        var persistedBooking = await DbContext
            .Set<Booking>()
            .SingleAsync(b => b.Id == booking.Id);

        persistedBooking.Status.Should().Be(BookingStatus.Cancelled);

        var persistedBlock = await DbContext
            .Set<ApartmentAvailabilityBlock>()
            .SingleOrDefaultAsync(b =>
                b.Id == availabilityBlock.Id);

        persistedBlock.Should().BeNull();
    }

    [Fact]
    public async Task CancelBooking_ShouldReturnNotAuthorized_WhenCallerIsNeitherGuestNorAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var otherUser = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, guest, otherUser, apartment);

        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.ReserveAndConfirm(
            apartment,
            guest.Id,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 5),
            PricingService);

        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(otherUser.Id, Role.Guest.Name);

        var command = new CancelBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotAuthorized);

        DbContext.ChangeTracker.Clear();

        var persistedBooking = await DbContext
            .Set<Booking>()
            .SingleAsync(b => b.Id == booking.Id);

        persistedBooking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task CancelBooking_ShouldReturnAlreadyStarted_WhenBookingHasStarted()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();

        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.ReserveAndConfirm(
            apartment,
            guest.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 5),
            PricingService);

        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new CancelBookingCommand(booking.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.AlreadyStarted);
    }
}