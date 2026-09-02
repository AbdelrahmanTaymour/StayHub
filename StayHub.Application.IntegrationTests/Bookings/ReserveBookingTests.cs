using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Bookings.ReserveBooking;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class ReserveBookingTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ReserveBooking_ShouldPersistBookingWithRealPricing_WhenNoOverlapExists()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id, priceAmount: 200m);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command = new ReserveBookingCommand(apartment.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5));

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedBooking = await DbContext.Set<Booking>().SingleAsync(b => b.Id == result.Value);
        persistedBooking.ApartmentId.Should().Be(apartment.Id);
        persistedBooking.UserId.Should().Be(guest.Id);
        persistedBooking.Status.Should().Be(BookingStatus.Reserved);
        persistedBooking.TotalPrice.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ReserveBooking_ShouldReturnOverlap_WhenAnotherActiveBookingCoversTheSameDates()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var firstGuest = UserTestData.CreateUser();
        var secondGuest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, firstGuest, secondGuest, apartment);
        await DbContext.SaveChangesAsync();

        var existingBooking = BookingTestData.Reserve(
            apartment, firstGuest.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 10), PricingService);
        DbContext.Add(existingBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(secondGuest.Id, Role.Guest.Name);

        var command = new ReserveBookingCommand(apartment.Id, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 8));

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.Overlap);
    }

    [Fact]
    public async Task ReserveBooking_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        var guest = UserTestData.CreateUser();
        DbContext.Add(guest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        var command =
            new ReserveBookingCommand(Guid.CreateVersion7(), new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5));

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }
}