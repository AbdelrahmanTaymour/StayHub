using FluentAssertions;
using StayHub.Application.Bookings.GetMyBookings;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Bookings;

public class GetMyBookingsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
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
        result.Value.Items.Should().ContainSingle(b => b.Id == booking.Id);
    }

    [Fact]
    public async Task GetMyBookings_ShouldReturnEmptyPage_WhenCallerHasNoBookings()
    {
        // Arrange
        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyBookingsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMyBookings_ShouldReturnApartmentDetails_AlongsideBooking()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id, name: "Nile View Studio", city: "Cairo", priceAmount: 100m);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var booking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyBookingsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.Should().ContainSingle().Which;

        item.ApartmentId.Should().Be(apartment.Id);
        item.ApartmentName.Should().Be("Nile View Studio");
        item.ApartmentCity.Should().Be("Cairo");
        item.PricePerNight.Should().Be(100m);
        item.Nights.Should().Be(4);
    }

    [Fact]
    public async Task GetMyBookings_ShouldReturnCanCancelTrue_WhenBookingIsReservedAndNotYetStarted()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var futureStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var futureEnd = futureStart.AddDays(5);

        var booking = BookingTestData.Reserve(apartment, guest.Id, futureStart, futureEnd, PricingService);
        DbContext.Add(booking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyBookingsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle().Which.CanCancel.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyBookings_ShouldReturnCanCancelFalse_WhenBookingHasAlreadyStarted()
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
        var result = await Sender.Send(new GetMyBookingsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle().Which.CanCancel.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyBookings_ShouldOnlyReturnCompletedBookings_WhenFilterIsCompleted()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var completedBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService, baseTime);
        completedBooking.Confirm(baseTime);
        completedBooking.Complete(baseTime);

        var upcomingBooking = BookingTestData.Reserve(
            apartment, guest.Id,
            DateOnly.FromDateTime(baseTime.AddDays(30)), DateOnly.FromDateTime(baseTime.AddDays(35)),
            PricingService, baseTime.AddMinutes(1));

        DbContext.AddRange(completedBooking, upcomingBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyBookingsQuery(MyBookingsFilter.Completed));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == completedBooking.Id);
    }

    [Fact]
    public async Task GetMyBookings_ShouldOnlyReturnCancelledBookings_WhenFilterIsCancelled()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var cancelledBooking = BookingTestData.Reserve(
            apartment, guest.Id,
            DateOnly.FromDateTime(baseTime.AddDays(10)), DateOnly.FromDateTime(baseTime.AddDays(15)),
            PricingService, baseTime);
        cancelledBooking.Cancel(baseTime);

        var activeBooking = BookingTestData.Reserve(
            apartment, guest.Id,
            DateOnly.FromDateTime(baseTime.AddDays(30)), DateOnly.FromDateTime(baseTime.AddDays(35)),
            PricingService, baseTime.AddMinutes(1));

        DbContext.AddRange(cancelledBooking, activeBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyBookingsQuery(MyBookingsFilter.Cancelled));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == cancelledBooking.Id);
    }

    [Fact]
    public async Task GetMyBookings_ShouldReturnOnlyReservedOrConfirmedFutureBookings_WhenFilterIsUpcoming()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var guest = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, guest, apartment);
        await DbContext.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        var upcomingBooking = BookingTestData.Reserve(
            apartment, guest.Id,
            DateOnly.FromDateTime(baseTime.AddDays(30)), DateOnly.FromDateTime(baseTime.AddDays(35)),
            PricingService, baseTime);

        var completedBooking = BookingTestData.Reserve(
            apartment, guest.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), PricingService,
            baseTime.AddMinutes(1));
        completedBooking.Confirm(baseTime);
        completedBooking.Complete(baseTime);

        DbContext.AddRange(upcomingBooking, completedBooking);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(guest.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetMyBookingsQuery(MyBookingsFilter.Upcoming));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(b => b.Id == upcomingBooking.Id);
    }
}