using FluentAssertions;
using StayHub.Application.Bookings.GetBookingsByUser;
using StayHub.Application.Bookings.GetMyBookings;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Bookings;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class GetBookingsByUserTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetBookingsByUser_ShouldReturnNotAuthorized_WhenCallerIsNotSelfOrAdmin()
    {
        // Arrange
        var guest = UserTestData.CreateUser();
        DbContext.Add(guest);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetBookingsByUserQuery(guest.Id, Page: 1, PageSize: 10));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotAuthorized);
    }

    [Fact]
    public async Task GetBookingsByUser_ShouldReturnOwnBookings_OrderedByCreatedOnDescending()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var olderBooking = BookingTestData.Reserve(
            apartment,
            guest.Id,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 5),
            PricingService,
            baseTime);

        var newerBooking = BookingTestData.Reserve(
            apartment,
            guest.Id,
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 5),
            PricingService,
            baseTime.AddMinutes(1));

        DbContext.AddRange(olderBooking, newerBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(
            new GetBookingsByUserQuery(
                guest.Id,
                Page: 1,
                PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        result.Value[0].Id.Should().Be(newerBooking.Id);
        result.Value[1].Id.Should().Be(olderBooking.Id);
    }

    [Fact]
    public async Task GetMyBookings_ShouldResolveFromUserContext_NotFromClientInput()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var loggedInGuest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, loggedInGuest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, loggedInGuest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(loggedInGuest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyBookingsQuery(Page: 1, PageSize: 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(b => b.Id == booking.Id);
    }
}